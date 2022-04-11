#if NET6_0_OR_GREATER
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Uno.UI.SourceGenerators.XamlGenerator;

namespace Uno.UI.SourceGenerators.IntegrationTests;

internal static class XamlCodeGeneratorHelper
{
	public static async Task<IReadOnlyCollection<Diagnostic>> RunXamlCodeGeneratorForFileAsync(
		string xamlFileName,
		string subFolder,
		Dictionary<string, string>? globalOptions = null,
		Dictionary<string, string>? additionalTextOptions = null,
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

		var baseGlobalOptions = new Dictionary<string, string>
		{
			{ "build_property.MSBuildProjectFullPath", folder },
			{ "build_property.RootNamespace", "RandomNamespace" },
		};
		var baseAdditionalTextOptions = new Dictionary<string, string>
		{
			{ "build_metadata.AdditionalFiles.SourceItemGroup", "Page" },
		};
		if (globalOptions != null)
		{
			foreach (var pair in globalOptions)
			{
				baseGlobalOptions[pair.Key] = pair.Value;
			}
		}
		if (additionalTextOptions != null)
		{
			foreach (var pair in additionalTextOptions)
			{
				baseAdditionalTextOptions[pair.Key] = pair.Value;
			}
		}
		var options = new DictionaryAnalyzerConfigOptionsProvider(
			globalOptions: baseGlobalOptions,
			additionalTextOptions: new Dictionary<string, Dictionary<string, string>>
			{
				{
					xaml.Path,
					baseAdditionalTextOptions
				},
			});

		var (generator, diagnostics) = await new XamlCodeGenerator().RunAsync(
			options,
			syntaxTrees: new[] { CSharpSyntaxTree.ParseText(cs, cancellationToken: cancellationToken) },
			additionalTexts: new[] { xaml },
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
#endif
