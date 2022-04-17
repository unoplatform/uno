#if NET6_0_OR_GREATER
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
		IReadOnlyCollection<string> preprocessorSymbols,
		CancellationToken cancellationToken = default)
	{
		var referenceAssemblies = ReferenceAssemblies.Net.Net60
			.WithPackages(ImmutableArray.Create(new PackageIdentity("Uno.UI", "4.1.9")));
		var references = await referenceAssemblies.ResolveAsync(null, cancellationToken);
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
#endif
