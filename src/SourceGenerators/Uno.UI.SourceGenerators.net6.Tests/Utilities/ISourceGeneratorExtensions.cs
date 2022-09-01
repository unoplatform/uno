using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace Uno.UI.SourceGenerators.Tests;

internal static class ISourceGeneratorExtensions
{
	private class TestDiagnosticComparer : IEqualityComparer<DiagnosticResult>
	{
		public static TestDiagnosticComparer Instance { get; } = new();

		public bool Equals(DiagnosticResult x, DiagnosticResult y)
		{
			return x.Id == y.Id && x.Severity == y.Severity && x.Message == y.Message;
		}

		public int GetHashCode(DiagnosticResult obj) => HashCode.Combine(obj.Id, obj.Severity, obj.Message);
	}

	private static string ConstructString(this IEnumerable<DiagnosticResult> diagnostics)
	{
		var builder = new StringBuilder();
		foreach (var diagnostic in diagnostics)
		{
			builder.AppendLine($"new DiagnosticResult(\"{diagnostic.Id}\", DiagnosticSeverity.{diagnostic.Severity}).WithMessage(@\"{diagnostic.Message}\"),");
		}

		return builder.ToString();
	}

	public static void AssertDiagnostics(this IEnumerable<Diagnostic> actualDiagnostics, params DiagnosticResult[] expectedDiagnostics)
	{
		var actualDiagnosticResults = actualDiagnostics.Select(d => new DiagnosticResult(d.Id, d.Severity).WithMessage(d.GetMessage()));
		var areEquivalent = expectedDiagnostics.SequenceEqual(actualDiagnosticResults, TestDiagnosticComparer.Instance);
		if (!areEquivalent)
		{
			Assert.Fail($@"
Expected:
{expectedDiagnostics.ConstructString()}

Actual:
{actualDiagnosticResults.ConstructString()}");
		}
	}

	public static async Task<(GeneratorDriver Driver, ImmutableArray<Diagnostic> Diagnostics)> RunAsync(
		this ISourceGenerator generator,
		AnalyzerConfigOptionsProvider options,
		SyntaxTree[] syntaxTrees,
		AdditionalText[] additionalTexts,
		IEnumerable<string>? preprocessorSymbols,
		CancellationToken cancellationToken = default)
	{
		var referenceAssemblies = ReferenceAssemblies.Net.Net60.AddPackages(ImmutableArray.Create(
			new PackageIdentity("Uno.UI", "4.5.0-dev.697")));
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
			.AddAdditionalTexts(additionalTexts.ToImmutableArray())
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
