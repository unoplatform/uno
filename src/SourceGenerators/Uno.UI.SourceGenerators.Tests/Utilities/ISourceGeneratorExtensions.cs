#if NET6_0_OR_GREATER
extern alias UnoAlias;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Uno.UI.SourceGenerators.Tests;

internal static class ISourceGeneratorExtensions
{
	public static async Task<(GeneratorDriver Driver, ImmutableArray<Diagnostic> Diagnostics)> RunAsync(
		this ISourceGenerator generator,
		AnalyzerConfigOptionsProvider options,
		SyntaxTree[] syntaxTrees,
		AdditionalText[] additionalTexts,
		IReadOnlyCollection<string> preprocessorSymbols,
		CancellationToken cancellationToken = default)
	{
		var dotNetFolder = Path.GetDirectoryName(typeof(object).Assembly.Location) ?? string.Empty;
		var unoFolder = Path.GetDirectoryName(typeof(UnoAlias::Uno.UI.ApplicationHelper).Assembly.Location) ?? string.Empty;
		var compilation = (Compilation)CSharpCompilation
			.Create(
				assemblyName: "Tests",
				references: new[]
				{
					MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
					MetadataReference.CreateFromFile(Path.Combine(dotNetFolder, "System.Runtime.dll")),
					MetadataReference.CreateFromFile(Path.Combine(dotNetFolder, "System.ObjectModel.dll")),
					MetadataReference.CreateFromFile(Path.Combine(dotNetFolder, "netstandard.dll")),
					MetadataReference.CreateFromFile(Path.Combine(unoFolder, "Uno.dll")),
					MetadataReference.CreateFromFile(Path.Combine(unoFolder, "Uno.Foundation.dll")),
					MetadataReference.CreateFromFile(Path.Combine(unoFolder, "Uno.UI.dll")),
					MetadataReference.CreateFromFile(Path.Combine(unoFolder, "Uno.UI.Composition.dll")),
				},
				options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
			.AddSyntaxTrees(syntaxTrees);

		var driver = CSharpGeneratorDriver
			.Create(generator)
			.WithUpdatedParseOptions(new CSharpParseOptions(
				preprocessorSymbols: preprocessorSymbols))
			.AddAdditionalTexts(ImmutableArray.Create(additionalTexts))
			.WithUpdatedAnalyzerConfigOptions(options)
			.RunGeneratorsAndUpdateCompilation(
				compilation,
				out compilation,
				out _, cancellationToken);

		var diagnostics = compilation
			.GetDiagnostics(cancellationToken)
			.AddRange(driver.GetRunResult().Diagnostics);

		return (driver, diagnostics);
	}
}
#endif
