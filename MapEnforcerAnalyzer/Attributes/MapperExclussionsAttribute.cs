using System;
using System.Collections.Generic;

namespace NCR.Engage.RoslynAnalysis.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class MapperExclussionsAttribute : Attribute
    {
        public IEnumerable<string> PropertyNames { get; protected set; }

        public MapperExclussionsAttribute(params string[] propertyNames)
        {
            PropertyNames = propertyNames;
        }
    }
}
