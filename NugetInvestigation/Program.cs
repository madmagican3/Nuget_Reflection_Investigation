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
        public static string WorkingDir = "C:\\Users\\madma\\Desktop\\Nuget working dir";

        static void Main(string[] args)
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
                if (taskList.Count % 20 == 0)
                {
                    try
                    {
                        //wait for all the tasks to be done
                        Task.WaitAll(taskList.ToArray());

                        //clear the task list
                        taskList.Clear();

                        //if we've got a file in the stop folder then stop running the code
                        if (Directory.GetFiles(stopFolder).Any())
                        {
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

            Console.WriteLine("Stopped");

            Console.ReadLine();
        }
    }
}