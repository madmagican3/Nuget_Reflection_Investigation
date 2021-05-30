using System.Collections.Generic;

namespace NugetInvestigation.Models
{
    public class Results
    {
        public string NugetId { get; set; }
        public string NugetVersion { get; set; }
        public List<List<ReflectionInstance>> ReflectionInstances { get; set; }
    }
}