# MapEnforcerAnalyzer

[![Build status](https://ci.appveyor.com/api/projects/status/1s1tk06al96am59g?svg=true)](https://ci.appveyor.com/project/NCREngage/mapenforceranalyzer)

## Problem

In a complicated mapping code, we are translating properties from one big object with many properties into another big object with many properties. Most of the "translation" is consisted of dumb assignments, sometimes it's more difficult.

The question is, how to future proof translator class against adding new properties into source? How to enforce exhaustives of the process with respect to the list of properties on the input?

## Solution

This analyzer warns you any time source object property is not mentioned in the mapper class implementation. This requires annotating mapper class in the following way:

    using NCR.Engage.RoslynAnalysis;

    namespace MyApp
    {
        [Mapper(typeof(SourceDto))]
        public class MyMapper
        {
            public TargetDto Map(SourceDto source)
            {
                return new TargetDto
                {
                    UserId = source.Id,
                    FirstName = source.Name.Split(' ')[0]
                };
            }
        }

        public class SourceDto
        {
            public int Id { get; set; }

            public string Name { get; set; }

            [ExcludeFromMapping]
            public string Nick { get; set; }
        }

        public class TargetDto
        {
            public int UserId { get; set; }
            
            public string FirstName { get; set; }
        }
    }
    
You can see a correct situation from the analyzer's point of view - both ``Id`` and ``Name`` properties of ``SourceDto`` are mapped, ``Nick`` property is explicitly marked as exluded. If you add any property to the SourceDto object, for example ``public int Age { get; set; }``, error is going to be raised warning you about danger:

*Property SourceDto.Age was not mapped by MyMapper. Decide whether this is the intended behavior -- you should consider adding proper mapping code into MyMapper so the content of SourceDto.Age won't get lost. If you are sure this property should not be mapped, mark it with ExcludeFromMapping attribute.*
