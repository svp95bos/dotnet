// Peter Rundqvist, 2023
using System.Collections.Immutable;
using System.Linq;
using CommunityToolkit.Mvvm.SourceGenerators.ComponentModel.Models;
using CommunityToolkit.Mvvm.SourceGenerators.Extensions;
using CommunityToolkit.Mvvm.SourceGenerators.Helpers;
using CommunityToolkit.Mvvm.SourceGenerators.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CommunityToolkit.Mvvm.SourceGenerators;

[Generator(LanguageNames.CSharp)]
public sealed partial class DTOMemberGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Gather info for all annotated fields
        IncrementalValuesProvider<(HierarchyInfo Hierarchy, Result<PropertyInfo?> Info)> propertyInfoWithErrors =
            context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "CommunityToolkit.Mvvm.ComponentModel.DTOMemberAttribute",
                static (node, _) => node is VariableDeclaratorSyntax { Parent: VariableDeclarationSyntax { Parent: FieldDeclarationSyntax { Parent: ClassDeclarationSyntax or RecordDeclarationSyntax, AttributeLists.Count: > 0 } } },
                static (context, token) =>
                {
                    if (!context.SemanticModel.Compilation.HasLanguageVersionAtLeastEqualTo(LanguageVersion.CSharp8))
                    {
                        return default;
                    }

                    PropertyDeclarationSyntax fieldDeclaration = (PropertyDeclarationSyntax)context.TargetNode.Parent!.Parent!;
                    IPropertySymbol propertySymbol = (IPropertySymbol)context.TargetSymbol;

                    // Get the hierarchy info for the target symbol, and try to gather the property info
                    HierarchyInfo hierarchy = HierarchyInfo.From(propertySymbol.ContainingType);

                    _ = Execute.TryGetInfo(fieldDeclaration, propertySymbol, context.SemanticModel, token, out PropertyInfo? propertyInfo, out ImmutableArray<DiagnosticInfo> diagnostics);

                    return (Hierarchy: hierarchy, new Result<PropertyInfo?>(propertyInfo, diagnostics));
                })
            .Where(static item => item.Hierarchy is not null);
    }
}

