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
        public static string WorkingDir = "C:\\Users\\Bomie\\Desktop\\NugetWorkingDir";

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
            var redoFolder = $"{WorkingDir}\\Redo";
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
            Directory.CreateDirectory(redoFolder);

            //setup the start of the redo functionality
            Console.WriteLine($"Handling redos");
            var taskListRedo = new List<Task>();
            foreach (var file in Directory.GetFiles(redoFolder))
            {
                //get the file number
                var fileName = file.Split('\\').Last().Replace(".json", "");
                var id = int.Parse(fileName);

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
                    await nugetHandler.MainMethod(nugetDownloadFolder, nugetArchiveFolder, resultFolder, id,
                        redoFolder));

                //add it to the task list
                //  taskListRedo.Add(task);
            }

            //wait until all the tasks for redo are done
            try
            {
                Task.WaitAll(taskListRedo.ToArray());
                Console.WriteLine($"Finished doing all the redos");
            }
            catch (Exception ex)
            {
                //track the errors in a anon object
                Directory.CreateDirectory($"{WorkingDir}\\Errors");
                File.WriteAllText($"{WorkingDir}\\Errors\\Errors{"redo"}.txt", JsonSerializer.Serialize(new
                {
                    message = ex.Message,
                    stacktrace = ex.StackTrace,
                    innerStack = ex.InnerException
                }));
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
                    await nugetHandler.MainMethod(nugetDownloadFolder, nugetArchiveFolder, resultFolder, id,
                        redoFolder));
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