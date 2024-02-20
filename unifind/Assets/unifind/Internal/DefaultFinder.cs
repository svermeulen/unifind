using UnityEditor;

namespace Unifind.Internal
{
    public static class DefaultFinder
    {
        [FuzzyFinderAction(Name = "Unifind Examples")]
        public static async void ShowExamples()
        {
            var entries = FuzzyFinder.GenerateEntriesForGroup("UnifindExample");
            var choice = await FuzzyFinder.UserSelect("Unifind Examples", entries);

            if (choice == null)
            {
                // Cancelled
            }
            else
            {
                choice.Value();
            }
        }
    }
}
