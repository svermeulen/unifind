using System;

namespace Unifind
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class FuzzyFinderMethod : Attribute
    {
        public bool IsEntryProvider { get; set; }

        public string? Name { get; set; }
        public string? Icon { get; set; }
        public string? Summary { get; set; }
        public string? Tooltip { get; set; }
        public bool Enabled { get; set; } = true;
    }
}
