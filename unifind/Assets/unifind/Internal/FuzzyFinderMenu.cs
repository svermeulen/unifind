using UnityEditor;

namespace Unifind.Internal
{
    public class FuzzyFinderMenu
    {
        [MenuItem("Window/General/Fuzzy Finder &d", false, -1000)]
        public static async void OpenRoot()
        {
            var choice = await FuzzyFinder.UserSelect("Fuzzy Finder", FuzzyFinder.GenerateEntriesForGroup(null));

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
