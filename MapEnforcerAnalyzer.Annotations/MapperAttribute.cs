using System;

namespace NCR.Engage.RoslynAnalysis
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple=true)]
    public class MapperAttribute : Attribute
    {
        public Type From { get; set; }

        public Type FromMetadata { get; set; }

        public MapperAttribute(Type from, Type fromMetadata = null)
        {
            From = from;
            FromMetadata = fromMetadata ?? from;
        }
    }
}
