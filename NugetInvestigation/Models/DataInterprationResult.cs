using System.Collections.Generic;

namespace NugetInvestigation.Models
{
    public class DataInterprationResult
    {
        public int TotalNumberOfNugetsWithReflection { get; set; } = 0;
        public int TotalNumberOfNugetsWithNoReflection { get; set; } = 0;
        public int TotalUniqueNugetsWithReflection { get; set; } = 0;
        public int TotalUniqueNugetsWithoutReflection { get; set; } = 0;

        public List<ReflectionCommonality> TypesOfReflectionUsedAndCommonality { get; set; } =
            new List<ReflectionCommonality>();

        public int InstancesOfAmountOfReflectionIncreasing { get; set; } = 0;
        public int InstancesOfAmountOfReflectionDecreasing { get; set; } = 0;
    }

    public class ReflectionCommonality
    {
        public string Name { get; set; }
        public int Uses { get; set; }
    }
}