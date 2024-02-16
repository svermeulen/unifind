using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Unifind.Internal
{
    public static class AssemblyUtil
    {
        public class MethodAttributePair<T>
            where T : Attribute
        {
            public MethodAttributePair(MethodInfo method, T attribute)
            {
                Method = method;
                Attribute = attribute;
            }

            public MethodInfo Method { get; private set; }
            public T Attribute { get; private set; }
        }

        public static List<MethodAttributePair<T>> FindUserMethodsWithAttribute<T>(
            BindingFlags flags
        )
            where T : Attribute
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var result = new List<MethodAttributePair<T>>();

            // We're only interested in user code, so any known unity default ones
            var skipPrefixes = new string[]
            {
                "System.",
                "System,",
                "Microsoft.",
                "UnityEngine,",
                "UnityEngine.",
                "UnityEditor,",
                "UnityEditor.",
                "ReportGeneratorMerged,",
                "mscorlib,",
                "netstandard,",
                "Mono.",
                "Unity.",
                "JetBrains.",
                "nunit.",
                "unityplastic,",
            };

            foreach (var assembly in assemblies)
            {
                if (skipPrefixes.Any(prefix => assembly.FullName.StartsWith(prefix)))
                {
                    continue;
                }

                Log.Trace(
                    "Checking assembly {0} for methods with attribute '{1}'",
                    assembly.FullName, typeof(T).Name
                );

                try
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        var methods = type.GetMethods(flags);

                        foreach (var method in methods)
                        {
                            var attribute = method.GetCustomAttribute<T>();

                            if (attribute != null)
                            {
                                result.Add(
                                    new MethodAttributePair<T>( method: method, attribute: attribute )
                                );
                            }
                        }
                    }
                }
                catch (Exception exception)
                {
                    Log.Error(
                        "Failure when trying to get types from assembly {0}: {1}",
                        assembly.FullName,
                        exception
                    );
                }
            }

            return result;
        }
    }
}
