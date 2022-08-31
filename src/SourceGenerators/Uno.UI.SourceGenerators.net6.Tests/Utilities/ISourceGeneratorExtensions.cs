using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace Uno.UI.SourceGenerators.Tests;

internal static class ISourceGeneratorExtensions
{
	public static async Task<(GeneratorDriver Driver, ImmutableArray<Diagnostic> Diagnostics)> RunAsync(
		this ISourceGenerator generator,
		AnalyzerConfigOptionsProvider options,
		SyntaxTree[] syntaxTrees,
		AdditionalText[] additionalTexts,
		IEnumerable<string>? preprocessorSymbols,
		CancellationToken cancellationToken = default)
	{
		var skiaFolder = Path.Combine("..", "..", "..", "..", "..", "Uno.UI", "bin", "Uno.UI.Skia", "Debug", "netstandard2.0");
		if (!Directory.Exists(skiaFolder))
		{
			throw new InvalidOperationException(
				"These tests require a project built by Uno.UI.Skia in Debug mode. Please build it using Uno.UI-Skia-only.slnf");
		}

		var referenceAssemblies = ReferenceAssemblies.Net.Net60;
		var references = await referenceAssemblies.ResolveAsync(null, cancellationToken);
		references = references
			.Add(MetadataReference.CreateFromFile(Path.Combine(skiaFolder, "Uno.dll")))
			.Add(MetadataReference.CreateFromFile(Path.Combine(skiaFolder, "Uno.Foundation.dll")))
			.Add(MetadataReference.CreateFromFile(Path.Combine(skiaFolder, "Uno.UI.dll")))
			.Add(MetadataReference.CreateFromFile(Path.Combine(skiaFolder, "Uno.UI.Composition.dll")));

		var compilation = (Compilation)CSharpCompilation
			.Create(
				assemblyName: "Tests",
				references: references,
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
