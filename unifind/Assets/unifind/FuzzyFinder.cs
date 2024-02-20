
using System;
using System.Collections.Generic;
using System.Reflection;
using Unifind.Internal;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;

namespace Unifind
{
    /// <summary>
    /// API for unifind
    /// </summary>
    public static class FuzzyFinder
    {
        public static List<FuzzyFinderEntry<Action>> GenerateEntriesForGroup(string? groupId)
        {
            var result = new List<FuzzyFinderEntry<Action>>();

            using (Log.SpanDebug("Looking up all methods with FuzzyFinder attribute"))
            {
                foreach (
                    var info in AssemblyUtil.FindUserMethodsWithAttribute<FuzzyFinderAction>(
                        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic
                    )
                )
                {
                    if (info.Attribute.GroupId != groupId)
                    {
                        continue;
                    }

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

        public static async Task<T?> UserSelect<T>(string title, IEnumerable<T> entries)
        {
            var fuzzyEntries = entries
                .Select(e => new FuzzyFinderEntry<T>(
                    name: e!.ToString(),
                    value: e
                ))
                .ToList();

            var result = await UserSelect<T>(title, fuzzyEntries);

            if (result != null)
            {
                return result.Value;
            }

            return default;
        }

        public static Task<FuzzyFinderEntry<T>?> UserSelect<T>(
            string title,
            List<FuzzyFinderEntry<T>> entries
        )
        {
            var window = EditorWindow.GetWindow<FuzzyFinderWindow>("Fuzzy Finder");
            return window.SelectImpl<T>(title, entries);
        }
    }
}
