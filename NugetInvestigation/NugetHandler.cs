using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NugetInvestigation.Models;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace NugetInvestigation
{
    public class NugetHandler
    {
        public async Task MainMethod(string nugetDownloadFolder, string nugetArchiveFolder, string resultFolder,
            int i)
        {
            //setup the default details
            var client = new HttpClient();

            //create this threads working dirs
            Directory.CreateDirectory(nugetArchiveFolder);
            Directory.CreateDirectory(nugetDownloadFolder);

            //get the catalog 
            var results = new List<Results>();
            int counter = 0;
            var result = await client.GetAsync($"https://api.nuget.org/v3/catalog0/page{i}.json");
            if (!result.IsSuccessStatusCode) //Once it hits a 404 we're done
            {
                throw new Exception($"404 means done");
            }

            var content = await result.Content.ReadAsStringAsync();
            var catalogPage = JsonConvert.DeserializeObject<CatalogPage>(content);
            Console.WriteLine($"Got catalog page {i}");


            //download each nuget from the catalog details
            foreach (var nuget in catalogPage.items)
            {
                Console.WriteLine($"Got nuget {nuget.NugetId}");
                var url =
                    $"https://www.nuget.org/api/v2/package/{nuget.NugetId}/{nuget.NugetVersion}";

                var getActualNuget = await client.GetAsync(url);
                if (!getActualNuget.IsSuccessStatusCode)
                {
                }

                await SaveZippedDllsTo(getActualNuget, nugetDownloadFolder, nuget.NugetVersion, nuget.NugetId);

                Console.WriteLine($"Nuget elements have been downloaded");
            }

            //foreach nuget extract it and inspect it for references to system.reflection
            foreach (var folder in Directory.GetDirectories(nugetDownloadFolder))
            {
                var nugetId = folder.Split('\\').Last().Split('^').First();
                var nugetVersion = folder.Split('\\').Last().Split('^').Last();
                var value = new Results()
                {
                    NugetId = nugetId,
                    ReflectionInstances = new List<List<ReflectionInstance>>(),
                    NugetVersion = nugetVersion
                };

                foreach (var dll in Directory.GetFiles(folder))
                {
                    try
                    {
                        var nugetSearcher = new ReflectionInspectorGadget(dll);
                        var reflectionInstances = nugetSearcher.FindInstancesOfReflection();
                        reflectionInstances.ForEach(x => x.DllName = dll.Split('\\').Last().Replace(".dll", ""));
                        value.ReflectionInstances.Add(reflectionInstances);
                    }
                    catch (Exception ex)
                    {
                    }
                }

                results.Add(value);
            }

            //cleanup elements
            Console.WriteLine($"Finished catalog {i} and cleaning up");

            Directory.Delete(nugetArchiveFolder, true);
            Directory.Delete(nugetDownloadFolder, true);
            File.WriteAllText($"{resultFolder}\\{i}.json", JsonSerializer.Serialize(results));
        }

        private async Task SaveZippedDllsTo(HttpResponseMessage getActualNuget, string nugetFilePath,
            string nugetVersion, string nugetName)
        {
            using var archive = new ZipArchive(getActualNuget.Content.ReadAsStream(), ZipArchiveMode.Read);

            nugetFilePath += $"\\{nugetName}^{nugetVersion}";
            foreach (var entry in archive.Entries)
            {
                if (!entry.Name.ToLower().EndsWith(".dll")) continue;

                //It's a .dll! Letsa save it
                await using var zipEntryStream = entry.Open();
                if (!Directory.Exists($"{nugetFilePath}"))
                    Directory.CreateDirectory($"{nugetFilePath}");
                await using var writeStream =
                    new FileStream($"{nugetFilePath}\\{entry.Name}{nugetVersion}", FileMode.Create);

                await zipEntryStream.CopyToAsync(writeStream);
            }
        }
    }
}