using Unifind;
using UnityEditor;
using UnityEngine;

namespace UnifindSample
{
    public static class SampleActions
    {
        [FuzzyFinderAction]
        [MenuItem("MyMenu/Fuzzy Finders/Bar &s", false, -1000)]
        public static async void Bar()
        {
            var widgets = new string[] { "Widget 1", "Widget 2", "Widget 3" };
            var choice = await FuzzyFinder.UserSelect("Choose Widget", widgets);
            Debug.LogFormat("You chose {0}", choice);
        }

        [FuzzyFinderAction(Name = "Choose Foo")]
        public static async void ChooseFoos()
        {
            var choice = await FuzzyFinder.UserSelect("Fuzzy Finder", FuzzyFinder.GenerateEntriesForGroup("foo"));
            choice?.Value();
        }

        [FuzzyFinderAction(GroupId = "foo")]
        public static void Foo1()
        {
            Debug.Log("Selected Foo1");
        }

        [FuzzyFinderAction(GroupId = "foo")]
        public static void Foo2()
        {
            Debug.Log("Selected Foo2");
        }
    }
}
