using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using TestHelper;

namespace NCR.Engage.RoslynAnalysis.Test
{
    [TestClass]
    public class UnitTest : CodeFixVerifier
    {
        [TestMethod]
        public void ShouldNotFailOnEmptyFile()
        {
            var test = @"";

            VerifyCSharpDiagnostic(test);
        }
        
        [TestMethod]
        public void ShouldFindBothUnmentionedProperties()
        {
            var test = @"
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
                AA = source.A
            };
        }
    }

    public class SourceDto
    {
        public int A { get; set; }

        public int B { get; set; }

        public int C { get; set; }
    }

    public class TargetDto
    {
        public int AA { get; set; }
    }
}";

            var expected1 = new DiagnosticResult
            {
                Id = "MEA001",
                Message = String.Format(
                    "Property {0} was not mapped by {1}. Decide whether this is the intended behavior -- you should consider adding proper mapping code into {1} so the content of {0} won't get lost. If you are sure this property should not be mapped, mark it with ExcludeFromMapping attribute.",
                    "'int SourceDto.B'",
                    "'MyMapper'"),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 6, 6)
                        }
            };

            var expected2 = new DiagnosticResult
            {
                Id = "MEA001",
                Message = String.Format(
                    "Property {0} was not mapped by {1}. Decide whether this is the intended behavior -- you should consider adding proper mapping code into {1} so the content of {0} won't get lost. If you are sure this property should not be mapped, mark it with ExcludeFromMapping attribute.",
                    "'int SourceDto.C'",
                    "'MyMapper'"),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 6, 6)
                        }
            };

            VerifyCSharpDiagnostic(test, expected1, expected2);
        }

        [TestMethod]
        public void ShouldFindUnmentionedPropertyWithRespectToExcludeFromMappingAttribute()
        {
            var test = @"
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
                AA = source.A
            };
        }
    }

    public class SourceDto
    {
        public int A { get; set; }

        public int B { get; set; }

        [ExcludeFromMapping]
        public int C { get; set; }
    }

    public class TargetDto
    {
        public int AA { get; set; }
    }
}";
            
            var expected2 = new DiagnosticResult
            {
                Id = "MEA001",
                Message = String.Format(
                    "Property {0} was not mapped by {1}. Decide whether this is the intended behavior -- you should consider adding proper mapping code into {1} so the content of {0} won't get lost. If you are sure this property should not be mapped, mark it with ExcludeFromMapping attribute.",
                    "'int SourceDto.B'",
                    "'MyMapper'"),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 6, 6)
                        }
            };

            VerifyCSharpDiagnostic(test, expected2);
        }

        [TestMethod]
        public void MentionWithSomeContinuationIsSufficient()
        {
            var test = @"
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
                AA = source.A.Length
            };
        }
    }

    public class SourceDto
    {
        public string A { get; set; }
    }

    public class TargetDto
    {
        public int AA { get; set; }
    }
}";
            
            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void ClassNotMarkedAsMapperShouldNotBeSubjectToReporting()
        {
            var test = @"
using NCR.Engage.RoslynAnalysis;

namespace MyApp
{
    public class MyMapper
    {
        public TargetDto Map(SourceDto source)
        {
            return new TargetDto
            {
                AA = source.A
            };
        }
    }

    public class SourceDto
    {
        public int A { get; set; }

        public int B { get; set; }

        [ExcludeFromMapping]
        public int C { get; set; }
    }

    public class TargetDto
    {
        public int AA { get; set; }
    }
}";
            
            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void ShouldFindUnmentionedPropertyWithRespectToExcludeFromMappingAttributePresentAtMetadatedType()
        {
            var test = @"
using NCR.Engage.RoslynAnalysis;

namespace MyApp
{
    [Mapper(typeof(SourceDto), typeof(SourceDtoMetadata))]
    public class MyMapper
    {
        public TargetDto Map(SourceDto source)
        {
            return new TargetDto
            {
                AA = source.A
            };
        }
    }

    public partial class SourceDto
    {
        public int A { get; set; }

        public int B { get; set; }

        public int C { get; set; }
    }

    public partial class SourceDto
    {

    }

    public class SourceDtoMetadata
    {
        [ExcludeFromMapping]
        public int C { get; set; }
    }

    public class TargetDto
    {
        public int AA { get; set; }
    }
}";

            var expected2 = new DiagnosticResult
            {
                Id = "MEA001",
                Message = String.Format(
                    "Property {0} was not mapped by {1}. Decide whether this is the intended behavior -- you should consider adding proper mapping code into {1} so the content of {0} won't get lost. If you are sure this property should not be mapped, mark it with ExcludeFromMapping attribute.",
                    "'int SourceDto.B'",
                    "'MyMapper'"),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 6, 6)
                        }
            };

            VerifyCSharpDiagnostic(test, expected2);
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new MapEnforcerAnalyzer();
        }
    }
}