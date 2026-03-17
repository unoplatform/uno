#nullable enable

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Uno.UI.SourceGenerators.XamlGenerator;

/// <summary>
/// Standalone IIncrementalGenerator for auto code-behind generation on WinAppSDK targets.
/// This generator always runs when loaded. It is only loaded on WinAppSDK builds
/// via the Uno.UI.SourceGenerators.WinAppSDK analyzer assembly.
/// On Uno Platform targets, code-behind generation is handled by the integrated XamlCodeGeneration pipeline.
/// </summary>
[Generator]
public sealed class XamlCodeBehindGenerator : IIncrementalGenerator
{
	private static readonly string[] ValidSourceItemGroups = { "Page", "ApplicationDefinition" };

	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		// Read global UnoGenerateCodeBehind property
		var globalEnabled = context.AnalyzerConfigOptionsProvider.Select(static (provider, ct) =>
		{
			provider.GlobalOptions.TryGetValue("build_property.UnoGenerateCodeBehind", out var value);
			// Default is true when not specified
			return string.IsNullOrEmpty(value) || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
		});

		// Collect XAML files from AdditionalFiles with appropriate SourceItemGroup metadata
		var xamlFiles = context.AdditionalTextsProvider
			.Combine(context.AnalyzerConfigOptionsProvider)
			.Select(static (pair, ct) =>
			{
				var (file, optionsProvider) = pair;
				var options = optionsProvider.GetOptions(file);

				options.TryGetValue("build_metadata.AdditionalFiles.SourceItemGroup", out var sourceItemGroup);
				options.TryGetValue("build_metadata.AdditionalFiles.UnoGenerateCodeBehind", out var perFileOverride);

				if (!ValidSourceItemGroups.Contains(sourceItemGroup))
				{
					return default;
				}

				var content = file.GetText(ct)?.ToString();
				if (content is null)
				{
					return default;
				}

				return (Path: file.Path, Content: content, PerFileOverride: perFileOverride);
			})
			.Where(static x => x.Path is not null);

		// Combine all inputs
		var combined = xamlFiles
			.Combine(globalEnabled)
			.Combine(context.CompilationProvider);

		context.RegisterSourceOutput(combined, static (ctx, input) =>
		{
			var ((xamlFile, globalEnabled), compilation) = input;

			if (xamlFile.Path is null)
			{
				return;
			}

			Execute(ctx, xamlFile.Content, xamlFile.Path, xamlFile.PerFileOverride, globalEnabled, compilation);
		});
	}

	private static void Execute(
		SourceProductionContext context,
		string xamlContent,
		string filePath,
		string? perFileOverride,
		bool globalEnabled,
		Compilation compilation)
	{
		// Check configuration (per-file override takes precedence)
		if (!XamlCodeBehindParser.ShouldGenerateCodeBehind(perFileOverride, globalEnabled ? "true" : "false"))
		{
			return;
		}

		// Parse the XAML file
		var classInfo = XamlCodeBehindParser.Parse(xamlContent, out var errorMessage);

		if (errorMessage is not null)
		{
			// Report diagnostic for malformed x:Class
			context.ReportDiagnostic(
				Diagnostic.Create(
					XamlCodeGenerationDiagnostics.InvalidXClassRule,
					Location.None,
					$"{errorMessage} in '{filePath}'"));
			return;
		}

		if (classInfo is null)
		{
			// No x:Class attribute - skip
			return;
		}

		var info = classInfo.Value;

		// Check if the class already exists in user-authored source.
		// On WinAppSDK the WinUI XAML compiler emits partial-class declarations
		// (*.g.cs / *.g.i.cs) inside the obj directory.  Those must be ignored;
		// we only skip generation when the developer has a real code-behind file.
		var existingType = compilation.GetTypeByMetadataName(info.FullClassName);
		if (existingType is not null && HasUserAuthoredDeclaration(existingType))
		{
			return;
		}

		// Generate code-behind
		var sourceText = XamlCodeBehindEmitter.Emit(info, filePath);
		var hintName = XamlCodeBehindEmitter.GetHintName(info);

		context.AddSource(hintName, SourceText.From(sourceText, Encoding.UTF8));
	}

	/// <summary>
	/// Returns true when the type has at least one declaration in a user-authored
	/// source file (i.e. NOT inside the obj directory where the WinUI XAML compiler
	/// places its *.g.cs / *.g.i.cs output).
	/// </summary>
	private static bool HasUserAuthoredDeclaration(INamedTypeSymbol type)
	{
		foreach (var syntaxRef in type.DeclaringSyntaxReferences)
		{
			var filePath = syntaxRef.SyntaxTree.FilePath;
			if (string.IsNullOrEmpty(filePath))
			{
				continue;
			}

			// WinUI XAML compiler output lives in the obj directory
			if (filePath.IndexOf("\\obj\\", StringComparison.OrdinalIgnoreCase) >= 0
				|| filePath.IndexOf("/obj/", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				continue;
			}

			return true;
		}

		return false;
	}
}
