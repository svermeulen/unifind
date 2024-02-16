using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;

namespace Unifind.Internal
{
    public class FuzzyFinderMenu
    {
        [MenuItem("Window/General/Fuzzy Finder &d", false, -1000)]
        public static async void OpenRoot()
        {
            var choice = await FuzzyFinderWindow.Select("Fuzzy Finder", GenerateRootEntries());

            if (choice == null)
            {
                // Cancelled
            }
            else
            {
                choice.Value();
            }
        }

        static List<FuzzyFinderEntry<Action>> GenerateRootEntries()
        {
            var result = new List<FuzzyFinderEntry<Action>>();

            using (Log.SpanDebug("Looking up all methods with FuzzyFinder attribute"))
            {
                foreach (
                    var info in AssemblyUtil.FindUserMethodsWithAttribute<FuzzyFinderMethod>(
                        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic
                    )
                )
                {
                    if (info.Attribute.IsEntryProvider)
                    {
                        var entries =
                            info.Method.Invoke(null, null) as List<FuzzyFinderEntry<Action>>;

                        if (entries == null)
                        {
                            Log.Error(
                                "Expected method {0} to return a List<FuzzyFinderEntry<Action>>",
                                info.Method.Name
                            );
                        }
                        else
                        {
                            result.AddRange(entries);
                        }
                    }
                    else
                    {
                        Action action = () => info.Method.Invoke(null, null);
                        result.Add(
                            new FuzzyFinderEntry<Action>(
                                name: info.Attribute.Name ?? info.Method.Name,
                                value: action,
                                summary: info.Attribute.Summary,
                                icon: info.Attribute.Icon,
                                tooltip: info.Attribute.Tooltip,
                                enabled: info.Attribute.Enabled
                            )
                        );
                    }
                }
            }

            result = result.OrderBy(i => i.Name).ToList();
            return result;
        }
    }
}
