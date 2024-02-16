
namespace Unifind
{
    public class FuzzyFinderEntry
    {
        public FuzzyFinderEntry(string name, string? icon = null, string? summary = null, string? tooltip = null, bool enabled = true)
        {
            Name = name;
            Icon = icon;
            Summary = summary;
            Tooltip = tooltip;
            Enabled = enabled;
        }

        public string Name { get; private set; }
        public string? Icon { get; private set; }
        public string? Summary { get; private set; }
        public string? Tooltip { get; private set; }
        public bool Enabled { get; private set; }
    }

    public class FuzzyFinderEntry<T> : FuzzyFinderEntry
    {
        public FuzzyFinderEntry(string name, T value, string? icon = null, string? summary = null, string? tooltip = null, bool enabled = true)
            : base(name, icon, summary, tooltip, enabled)
        {
            Value = value;
        }

        public T Value { get; private set; }
    }
}
