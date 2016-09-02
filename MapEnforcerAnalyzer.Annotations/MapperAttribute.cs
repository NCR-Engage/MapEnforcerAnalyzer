using System;

namespace NCR.Engage.RoslynAnalysis
{
    [AttributeUsage(AttributeTargets.Class)]
    public class MapperAttribute : Attribute
    {
        public Type From { get; set; }

        public MapperAttribute(Type from)
        {
            From = from;
        }
    }
}
