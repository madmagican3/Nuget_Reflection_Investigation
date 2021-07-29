using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NugetInvestigation.Models;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace NugetInvestigation
{
    class Program
    {
        public static string WorkingDir { get; set; }

        static void Main(string[] args)
        {
            Console.WriteLine($"Please enter the directoy you want to use as a working directory");
            var dir = Console.ReadLine();
            WorkingDir = dir;

            Console.WriteLine($"To start the download system, enter 1, to interpret the results, enter 2");
            var actionKey = Console.ReadLine();
            if (actionKey == "1")
            {
                DownloadFunction();
            }
            else if (actionKey == "2")
            {
                InterprationFunction();
            }
            else
            {
                Console.WriteLine($"Invalid input: Exiting");
            }
        }

        public static void InterprationFunction()
        {
            Console.WriteLine($"Please enter the folder you're wanting to interpret the results from");
            var filePath = Console.ReadLine();
            var files = Directory.GetFiles(filePath);
            var allResults = new List<Results>();
            foreach (var catalog in files)
            {
                Console.WriteLine($"Grabbing next catalog");
                var catalogString = File.ReadAllText(catalog);
                var result = JsonSerializer.Deserialize<List<Results>>(catalogString);
                allResults.AddRange(result);
            }

            var dataTrackingList = new List<DataTracker>();
            foreach (var res in allResults.OrderBy(x => x.NugetVersion))
            {
                Console.WriteLine($"Dealing with result {res.NugetId}");
                //First list is dll list, second list is reflection instances in said dll list
                var hasReflection = res.ReflectionInstances.Any(x => x.Any());
                var instancesOfReflection = res.ReflectionInstances.Select(x => x.Count).Sum();

                var reflectionTypes = res.ReflectionInstances.SelectMany(x => x).ToList();

                if (dataTrackingList.Any(x => x.NugetName == res.NugetId))
                {
                    var instance = dataTrackingList.First(x => x.NugetName == res.NugetId);
                    instance.UsesReflection = hasReflection || instance.UsesReflection;
                    if (instancesOfReflection > instance.LastAmountOfInstancesOfReflection)
                    {
                        instance.IncreasedInstancesOfReflection += 1;
                    }
                    else if (instancesOfReflection < instance.LastAmountOfInstancesOfReflection)
                    {
                        instance.DecreasedInstancesOfReflection += 1;
                    }

                    instance.LastAmountOfInstancesOfReflection = instancesOfReflection;
                    instance.Instances.AddRange(reflectionTypes);
                }
                else
                {
                    dataTrackingList.Add(new DataTracker()
                    {
                        NugetName = res.NugetId,
                        UsesReflection = hasReflection,
                        NumberOfVersions = 1,
                        DecreasedInstancesOfReflection = 0,
                        IncreasedInstancesOfReflection = 0,
                        LastAmountOfInstancesOfReflection = instancesOfReflection,
                        Instances = reflectionTypes
                    });
                }
            }

            var tupleList = new List<ReflectionCommonality>();

            foreach (var res in dataTrackingList.GroupBy(x => x.NugetName))
            {
                Console.WriteLine($"Dealing with nuget key {res.Key}");
                var allDupeDataTracking = dataTrackingList.Where(x => x.NugetName == res.Key);
                var allInstances = allDupeDataTracking.SelectMany(x => x.Instances).ToList().Distinct();
                foreach (var uniqueInstance in allInstances)
                {
                    if (tupleList.Any(x => x.Name == uniqueInstance.ReflectionCL))
                    {
                        tupleList.First(x => x.Name == uniqueInstance.ReflectionCL).Uses += 1;
                    }
                    else
                    {
                        tupleList.Add(new ReflectionCommonality()
                        {
                            Name = uniqueInstance.ReflectionCL,
                            Uses = 1
                        });
                    }
                }
            }

            var dataInterprationResults = new DataInterprationResult()
            {
                TotalUniqueNugetsWithoutReflection = dataTrackingList.Count(x => !x.UsesReflection),
                TotalUniqueNugetsWithReflection = dataTrackingList.Count(x => x.UsesReflection),
                InstancesOfAmountOfReflectionDecreasing =
                    dataTrackingList.Select(x => x.DecreasedInstancesOfReflection).Sum(),
                InstancesOfAmountOfReflectionIncreasing =
                    dataTrackingList.Select(x => x.IncreasedInstancesOfReflection).Sum(),
                TotalNumberOfNugetsWithReflection = allResults.Count(x =>
                    x.ReflectionInstances.Count > 0 &&
                    Enumerable.Any<List<ReflectionInstance>>(x.ReflectionInstances, x => x.Any())),
                TypesOfReflectionUsedAndCommonality = tupleList,
                TotalNumberOfNugetsWithNoReflection = allResults.Count(x =>
                    x.ReflectionInstances == null || x.ReflectionInstances.Count == 0 ||
                    Enumerable.Any<List<ReflectionInstance>>(
                        x.ReflectionInstances, x => !x.Any()))
            };
            File.WriteAllText($"{WorkingDir}/FinalResults.txt", JsonSerializer.Serialize(dataInterprationResults));

            Console.WriteLine($"Finished");
        }

        public static void DownloadFunction()
        {
            //initialise the nuget handler
            var nugetHandler = new NugetHandler();

            //setup the folders required
            var resultFolder = $"{WorkingDir}\\Results";
            var stopFolder = $"{WorkingDir}\\insertFilesHereToStopAfterLatestRun";
            var catalogs = $"{WorkingDir}\\catalogs";
            var errorFolder = $"{WorkingDir}\\Errors";

            //delete the stop folder, as we dont know if it's used and we want it empty
            try
            {
                Directory.Delete(stopFolder, true);
            }
            catch (Exception ex)
            {
            }

            //create the working dirs if they're not working
            Directory.CreateDirectory(stopFolder);
            Directory.CreateDirectory(resultFolder);
            Directory.CreateDirectory(catalogs);
            Directory.CreateDirectory(errorFolder);

            var hasHitEndOfCatalogs = false;
            var counter = 0;
            //setup the default details
            var client = new HttpClient();

            if (!Directory.GetFiles(catalogs).Any())
            {
                try
                {
                    do
                    {
                        var result = client.GetAsync($"https://api.nuget.org/v3/catalog0/page{counter}.json").Result;
                        if (!result.IsSuccessStatusCode) //Once it hits a 404 we're done
                        {
                            hasHitEndOfCatalogs = true;
                        }

                        var content = result.Content.ReadAsStringAsync().Result;
                        var catalogPage = JsonConvert.DeserializeObject<CatalogPage>(content);
                        File.WriteAllText($"{catalogs}\\{counter}.json", JsonSerializer.Serialize(catalogPage));
                        Console.WriteLine($"Got catalog {counter} and continuing");
                        counter += 1;
                    } while (!hasHitEndOfCatalogs);
                }
                catch (Exception ex)
                {
                    File.WriteAllText($"{errorFolder}\\ErrorsCatalog{counter}.txt", JsonSerializer.Serialize(new
                    {
                        message = ex.Message,
                        stacktrace = ex.StackTrace,
                        innerStackMessage = ex.InnerException?.Message,
                        innerStack = ex.InnerException?.StackTrace,
                        catalogNum = catalogs,
                    }));
                }
            }

            //get all the ints currently in the result list
            var resultList = Directory.GetFiles(resultFolder).Select(x => x.Split('\\').Last().Replace(".json", ""));
            var catalogList = Directory.GetFiles(catalogs).Select(x => x.Split('\\').Last().Replace(".json", ""));

            var notDoneList = new List<int>();

            foreach (var catalog in catalogList)
            {
                if (!resultList.Contains(catalog))
                {
                    notDoneList.Add(int.Parse(catalog));
                }
            }

            var taskList = new List<Task>();

            foreach (var catalog in notDoneList)
            {
                //get the id, and set it because for the task it will be a pointer
                var id = catalog;

                //setup the working folders
                var nugetArchiveFolder = $"{WorkingDir}\\Archive{id}";
                var nugetDownloadFolder = $"{WorkingDir}\\Initial{id}";

                //start the task and it too the list
                var task = Task.Run(async () =>
                    await nugetHandler.MainMethod(nugetDownloadFolder, nugetArchiveFolder, resultFolder, id,
                        errorFolder));
                taskList.Add(task);

                //we dont want to go over 20 working threads at a time so stop at 20
                if (taskList.Count % 10 == 0)
                {
                    try
                    {
                        var finishedTask = Task.WhenAny(taskList).Result;
                        taskList.Remove(finishedTask);

                        //if we've got a file in the stop folder then stop running the code
                        if (Directory.GetFiles(stopFolder).Any())
                        {
                            Task.WaitAll();
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        //track the errors in a anon object
                        File.WriteAllText($"{errorFolder}\\Errors{id}.txt", JsonSerializer.Serialize(new
                        {
                            message = ex.Message,
                            stacktrace = ex.StackTrace,
                            innerStackMessage = ex.InnerException?.Message,
                            innerStack = ex.InnerException?.StackTrace,
                        }));

                        //if we've got a file in the stop folder then stop running the code
                        if (Directory.GetFiles(stopFolder).Any())
                        {
                            break;
                        }
                    }
                }
            }

            Task.WaitAll();

            Console.WriteLine("Stopped");

            Console.ReadLine();
        }
    }
}