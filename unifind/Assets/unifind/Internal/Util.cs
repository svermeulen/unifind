
namespace Unifind.Internal
{
    public static class Util
    {
        public static bool IsSubsequenceMatch(string completion, string partial)
        {
            if (partial == string.Empty)
            {
                return true;
            }

            var firstLetter = partial.ToLower()[0];

            if (!(firstLetter >= 'a' && firstLetter <= 'z'))
                return false;

            completion = completion.ToUpper();
            partial = partial.ToUpper();

            int index = 0;
            foreach (char c in partial)
            {
                bool found = false;

                while (index < completion.Length)
                {
                    if (completion[index] == c)
                    {
                        found = true;
                        index++;
                        break;
                    }

                    index++;
                }

                if (!found)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
