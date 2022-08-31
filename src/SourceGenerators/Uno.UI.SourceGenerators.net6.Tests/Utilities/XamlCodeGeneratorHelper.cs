using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Uno.UI.SourceGenerators.XamlGenerator;

namespace Uno.UI.SourceGenerators.Tests;

internal static class XamlCodeGeneratorHelper
{
	public static async Task<IReadOnlyCollection<Diagnostic>> RunXamlCodeGeneratorForFileAsync(
		string xamlFileName,
		string subFolder,
		Dictionary<string, string>? globalOptions = null,
		Dictionary<string, string>? additionalTextOptions = null,
		string[]? preprocessorSymbols = null,
		[CallerMemberName] string testName = "",
		CancellationToken cancellationToken = default)
	{
		var projectFolder = Path.GetFullPath(Path.Combine("..", "..", ".."));
		var solutionFolder = Path.GetFullPath(Path.Combine(projectFolder, "..", ".."));
		var folder = Path.GetFullPath(Path.Combine(solutionFolder, subFolder));
		var xaml = new FileAdditionalText(
			Path.Combine(folder, xamlFileName));
		var cs = File.ReadAllText(
			Path.Combine(folder, xamlFileName + ".cs"));

		globalOptions ??= new Dictionary<string, string>();
		globalOptions.Add("build_property.MSBuildProjectFullPath", folder);
		globalOptions.Add("build_property.RootNamespace", "RandomNamespace");

		additionalTextOptions ??= new Dictionary<string, string>();
		additionalTextOptions.Add("build_metadata.AdditionalFiles.SourceItemGroup", "Page");

		var options = new DictionaryAnalyzerConfigOptionsProvider(
			globalOptions: globalOptions,
			additionalTextOptions: new Dictionary<string, Dictionary<string, string>>
			{
				[xaml.Path] = additionalTextOptions,
			});

		var (generator, diagnostics) = await new XamlCodeGenerator().RunAsync(
			options,
			syntaxTrees: new[] { CSharpSyntaxTree.ParseText(
				text: cs,
				cancellationToken: cancellationToken) },
			additionalTexts: new[] { xaml },
			preprocessorSymbols: preprocessorSymbols,
			cancellationToken);
		var runResult = generator.GetRunResult();

		foreach (var tree in runResult.GeneratedTrees)
		{
			var path = Path.Combine(projectFolder, "Results", testName, Path.GetFileName(tree.FilePath));
			Directory.CreateDirectory(Path.Combine(projectFolder, "Results", testName));

			File.WriteAllText(path, tree.ToString());
		}

		return diagnostics
			// I temporarily removed the hidden diagnostics, but potentially there is a problem in that they are
			// All hidden diagnostics are unnecessary using directives
			// I think the problem here is that the generated code shouldn't use using directives at all
			.Where(static diagnostic => diagnostic.Severity != DiagnosticSeverity.Hidden)
			.ToArray();
	}
}
