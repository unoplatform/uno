#if NET6_0_OR_GREATER
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Uno.UI.SourceGenerators.IntegrationTests;

internal static class ISourceGeneratorExtensions
{
	public static async Task<(GeneratorDriver Driver, ImmutableArray<Diagnostic> Diagnostics)> RunAsync(
		this ISourceGenerator generator,
		AnalyzerConfigOptionsProvider options,
		SyntaxTree[] syntaxTrees,
		AdditionalText[] additionalTexts,
		CancellationToken cancellationToken = default)
	{
		var dotNetFolder = Path.GetDirectoryName(typeof(object).Assembly.Location) ?? string.Empty;
		var unoFolder = Path.GetDirectoryName(typeof(ApplicationHelper).Assembly.Location) ?? string.Empty;
		var compilation = (Compilation)CSharpCompilation.Create(
			assemblyName: "Tests",
			syntaxTrees: new[]
			{
				CSharpSyntaxTree.ParseText(@"
namespace RandomNamespace
{
    public class Program
    {
        public static void Main(string[] args)
        {
        }
    }
}
", cancellationToken: cancellationToken),
			},
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
			})
			.AddSyntaxTrees(syntaxTrees);

		var driver = CSharpGeneratorDriver
			.Create(generator)
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
