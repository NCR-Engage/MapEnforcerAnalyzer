using System;

namespace NCR.Engage.RoslynAnalysis.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class MapperAttribute : Attribute
    {
        public Type From { get; set; }
        
        public string[] ExcludedPropertyNames { get; set; }

        public MapperAttribute(Type from, params string[] excludedPropertyNames)
        {
            From = from;
            ExcludedPropertyNames = excludedPropertyNames;
        }
    }
}
