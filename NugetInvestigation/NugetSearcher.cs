using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Collections.Generic;

namespace NugetInvestigation
{
    public class NugetSearcher
    {
        private readonly string _assemblyPath;

        public NugetSearcher(string assemblyPath)
        {
            _assemblyPath = assemblyPath;
        }

        public List<ReflectionInstance> FindInstancesOfReflection()
        {
            using (var module = ModuleDefinition.ReadModule(_assemblyPath))
            {
                var classList = ScanForClassFiles(module);
                var reflectionInstances = new List<ReflectionInstance>();
                foreach (var @class in classList)
                {
                    reflectionInstances.AddRange(ScanMethodsForReflection(@class.Methods));
                }

                return reflectionInstances;
            }
        }

        private IEnumerable<ReflectionInstance> ScanMethodsForReflection(Collection<MethodDefinition> classMethods)
        {
            if (classMethods == null || !classMethods.Any())
                return new List<ReflectionInstance>();
            var results = new List<ReflectionInstance>();
            foreach (var meth in classMethods)
            {
                foreach (var command in meth.Body.Instructions)
                {
                    if (command.Operand is MethodReference mr)
                    {
                        if (mr.FullName.StartsWith("System.Reflection."))
                        {
                            results.Add(new ReflectionInstance()
                            {
                                ClassName = meth.DeclaringType.Name,
                                ReflectionCL = mr.FullName,
                                ReflectionLocation = meth.Name
                            });
                        }
                    }
                }
            }

            return results;
        }

        private List<TypeDefinition> ScanForClassFiles(ModuleDefinition moduleDefinition)
        {
            var classes = (from type in moduleDefinition.Types
                where type.MetadataType == MetadataType.Class
                select type).ToList();
            return classes;
        }
    }

    public class ReflectionInstance
    {
        public string DllName { get; set; }
        public string ClassName { get; set; }
        public string ReflectionLocation { get; set; }
        public string ReflectionCL { get; set; }
    }
}