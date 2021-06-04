using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace NugetInvestigation
{
    class Program
    {
        public static string WorkingDir = "C:\\Users\\madma\\Desktop\\Nuget working dir";

        static int GetLastFileNo(string[] getLastFileNo)
        {
            int highestNo = -1;
            foreach (var t in getLastFileNo)
            {
                var fileName = t.Split('\\').Last().Replace(".json", "");
                highestNo = Math.Max(highestNo, int.Parse(fileName));
            }

            return highestNo + 1;
        }

        static void Main(string[] args)
        {
            //initialise the nuget handler
            var nugetHandler = new NugetHandler();

            //setup the folders required
            var resultFolder = $"{WorkingDir}\\Results";
            var stopFolder = $"{WorkingDir}\\insertFilesHereToStopAfterLatestRun";

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

            //setup the start of the redo functionality
            Console.WriteLine($"Handling redos");
            var taskListRedo = new List<Task>();
            var lastNum = int.Parse(Directory.GetFiles(resultFolder).Last().Replace(".json", ""));
            var numbersToDo = new List<int>();
            for (int i = 0; i < lastNum; i++)
            {
                if (!Directory.GetFiles(resultFolder).Any(x => x == $"{i}.json"))
                {
                    numbersToDo.Add(i);
                }
            }

            foreach (var id in numbersToDo)
            {
                //work out the archive details
                var nugetArchiveFolder = $"{WorkingDir}\\Archive{id}";
                var nugetDownloadFolder = $"{WorkingDir}\\Initial{id}";

                //try and remove them if they exist, dont try and reuse existing elements, we dont know what bits failed
                try
                {
                    Directory.Delete(nugetArchiveFolder, true);
                }
                catch (Exception ex)
                {
                }

                try
                {
                    Directory.Delete(nugetDownloadFolder, true);
                }
                catch (Exception ex)
                {
                }

                //start the task of handling the catalog
                var task = Task.Run(async () =>
                    await nugetHandler.MainMethod(nugetDownloadFolder, nugetArchiveFolder, resultFolder, id));
                //add it to the task list
                taskListRedo.Add(task);
                if (taskListRedo.Count % 20 == 0)
                {
                    try
                    {
                        //wait for all the tasks to be done
                        Task.WaitAll(taskListRedo.ToArray());

                        //clear the task list
                        taskListRedo.Clear();

                        //if we've got a file in the stop folder then stop running the code
                        if (Directory.GetFiles(stopFolder).Any())
                        {
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        //track the errors in a anon object
                        Directory.CreateDirectory($"{WorkingDir}\\Errors");
                        File.WriteAllText($"{WorkingDir}\\Errors\\Errors{id}.txt", JsonSerializer.Serialize(new
                        {
                            message = ex.Message,
                            stacktrace = ex.StackTrace,
                            innerStack = ex.InnerException
                        }));

                        //if we've got a file in the stop folder then stop running the code
                        if (Directory.GetFiles(stopFolder).Any())
                        {
                            break;
                        }
                    }
                }
            }


            var taskList = new List<Task>();

            //get the latest files from the results
            var files = Directory.GetFiles(resultFolder);

            for (int i = GetLastFileNo(files);; i++)
            {
                //get the id, and set it because for the task it will be a pointer
                var id = i;

                //setup the working folders
                var nugetArchiveFolder = $"{WorkingDir}\\Archive{id}";
                var nugetDownloadFolder = $"{WorkingDir}\\Initial{id}";

                //start the task and it too the list
                var task = Task.Run(async () =>
                    await nugetHandler.MainMethod(nugetDownloadFolder, nugetArchiveFolder, resultFolder, id));
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
                        Directory.CreateDirectory($"{WorkingDir}\\Errors");
                        File.WriteAllText($"{WorkingDir}\\Errors\\Errors{id}.txt", JsonSerializer.Serialize(new
                        {
                            message = ex.Message,
                            stacktrace = ex.StackTrace,
                            innerStack = ex.InnerException
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