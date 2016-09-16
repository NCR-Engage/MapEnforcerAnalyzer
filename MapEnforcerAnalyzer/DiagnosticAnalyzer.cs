using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace NCR.Engage.RoslynAnalysis
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class MapEnforcerAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor PropertyNotMapped =
            new DiagnosticDescriptor(
                "MEA001",
                "All properties should be mapped.",
                "Property {0} was not mapped by {1}. Decide whether this is the intended behavior -- you should consider adding proper mapping code into {1} so the content of {0} won't get lost. If you are sure this property should not be mapped, mark it with ExcludeFromMapping attribute.",
                "Naming",
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(PropertyNotMapped);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeAttribute, SyntaxKind.Attribute);
        }
        
        private static void AnalyzeAttribute(SyntaxNodeAnalysisContext context)
        {
            var mapperAttribute = context.Node as AttributeSyntax;
            var semModel = context.SemanticModel;
            
            foreach (var d in Analyze(mapperAttribute, semModel))
            {
                context.ReportDiagnostic(d);
            }
        }

        private static IEnumerable<Diagnostic> Analyze(AttributeSyntax mapperAttribute, SemanticModel semModel)
        {
            if (mapperAttribute?.Name.ToString() != "Mapper")
            {
                yield break;
            }

            var symbol = semModel.GetSymbolInfo(mapperAttribute).Symbol as IMethodSymbol;
            if (!symbol?.ToString().StartsWith("NCR.Engage.RoslynAnalysis.MapperAttribute") ?? true)
            {
                yield break;
            }
            
            var sourceClass = GetSourceSymbols(semModel, mapperAttribute);

            if (sourceClass == null)
            {
                yield break;
            }

            var sourceProperties = GetSourceProperties(semModel, sourceClass.Item1, sourceClass.Item2);

            var mapperClass = GetMapperClass(semModel, mapperAttribute);

            if (mapperClass == null)
            {
                yield break;
            }

            foreach (var sourceProperty in sourceProperties)
            {
                if (IsPropertyMentioned(sourceProperty, mapperAttribute))
                {
                    continue;
                }

                var sourcePropertyName = $"'{sourceProperty.Type} {sourceClass.Item1.Name}.{sourceProperty.Name}'";
                var mapperClassName = $"'{mapperClass.Name}'";

                yield return Diagnostic.Create(PropertyNotMapped, mapperAttribute.GetLocation(), sourcePropertyName, mapperClassName);
            }
        }

        private static Tuple<ITypeSymbol, ITypeSymbol> GetSourceSymbols(SemanticModel semModel, AttributeSyntax mapperAttribute)
        {
            var attrArguments = mapperAttribute.ArgumentList.Arguments;

            var toExpr = attrArguments[0].Expression as TypeOfExpressionSyntax;
            var toMetadataExpr = attrArguments.Count > 1 ? attrArguments[1].Expression as TypeOfExpressionSyntax : null;

            var to = semModel.GetSymbolInfo(toExpr?.Type).Symbol as ITypeSymbol;
            var toMetadata = toMetadataExpr == null ? null : semModel.GetSymbolInfo(toMetadataExpr.Type).Symbol as ITypeSymbol;

            return Tuple.Create(to, toMetadata);
        }

        private static IEnumerable<IPropertySymbol> GetSourceProperties(SemanticModel semModel, ITypeSymbol sourceClass, ITypeSymbol sourceMetadataClass)
        {
            return sourceClass
                .GetMembers()
                .Where(m => m.Kind == SymbolKind.Property)
                .Cast<IPropertySymbol>()
                .Where(p => !IsExcludedFromMapping(p, sourceMetadataClass));
        }

        private static bool IsExcludedFromMapping(IPropertySymbol property, ITypeSymbol metadataClass)
        {
            if (metadataClass != null)
            {
                property = (metadataClass.GetMembers(property.Name).FirstOrDefault() as IPropertySymbol) ?? property;
            }

            return property
                .GetAttributes()
                .Any(a => a.AttributeClass.ToString() == "NCR.Engage.RoslynAnalysis.ExcludeFromMappingAttribute");
        }

        private static ITypeSymbol GetMapperClass(SemanticModel semModel, AttributeSyntax mapperAttribute)
        {
            var classDeclaration = mapperAttribute.Parent.Parent as ClassDeclarationSyntax;

            var identifierToken = classDeclaration?.ChildTokens().FirstOrDefault(t => t.IsKind(SyntaxKind.IdentifierToken));

            if (identifierToken == null)
            {
                return null;
            }

            var typeSymbol = semModel.GetDeclaredSymbol(classDeclaration) as ITypeSymbol;

            return typeSymbol;
        }

        private static bool IsPropertyMentioned(IPropertySymbol sourceProperty, AttributeSyntax mapperAttribute)
        {
            var mv = new MentionVisitor(sourceProperty);
            var classDeclaration = mapperAttribute.Parent.Parent as ClassDeclarationSyntax;
            mv.Visit(classDeclaration);
            return mv.MentionFound;
        }

        private class MentionVisitor : CSharpSyntaxWalker
        {
            public bool MentionFound;

            private IPropertySymbol PropertyToWatch { get; set; }
            
            public MentionVisitor(IPropertySymbol propertyToWatch)
            {
                PropertyToWatch = propertyToWatch;
            }

            public override void VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
            {
                if (node.Name.ToString() == PropertyToWatch.Name)
                {
                    // todo: check semantic model if this is really the same property

                    MentionFound = true;
                }
                else
                {
                    base.VisitMemberAccessExpression(node);
                }
            }
        }
    }
}
