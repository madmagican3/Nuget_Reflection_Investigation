using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace NugetInvestigation.Models
{
    public class Item
    {
        [JsonProperty("@id")] public string Id { get; set; }

        [JsonProperty("@type")] public string Type { get; set; }

        public DateTime commitTimeStamp { get; set; }

        [JsonProperty("nuget:id")] public string NugetId { get; set; }

        [JsonProperty("nuget:version")] public string NugetVersion { get; set; }

        public string commitId { get; set; }

        [JsonProperty("@container")] public string Container { get; set; }
    }

    public class CatalogPage
    {
        [JsonProperty("@id")] public string Id { get; set; }

        [JsonProperty("@type")] public string Type { get; set; }

        public string commitId { get; set; }
        public DateTime commitTimeStamp { get; set; }
        public int count { get; set; }
        public List<Item> items { get; set; }
        public string parent { get; set; }
    }
}