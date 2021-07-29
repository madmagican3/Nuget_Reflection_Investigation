using System.Collections.Generic;

namespace NugetInvestigation.Models
{
    public class DataTracker
    {
        public string NugetName { get; set; }
        public bool UsesReflection { get; set; }
        public int NumberOfVersions { get; set; }
        public int DecreasedInstancesOfReflection { get; set; }
        public int IncreasedInstancesOfReflection { get; set; }
        public int LastAmountOfInstancesOfReflection { get; set; }
        public List<ReflectionInstance> Instances { get; set; }
    }
}