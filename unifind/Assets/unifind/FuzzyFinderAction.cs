using System;

namespace Unifind
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class FuzzyFinderAction : Attribute
    {
        public bool IsEntryProvider { get; set; }

        public string? Name { get; set; }
        public string? Icon { get; set; }
        public string? Summary { get; set; }
        public string? Tooltip { get; set; }
        public bool Enabled { get; set; } = true;

        // When null, this action will be added to the root unifind popup
        public string? GroupId { get; set; }
    }
}
