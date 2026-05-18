#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Uno.Roslyn;
using Uno.UI.SourceGenerators.Helpers;
using Uno.DevTools.Telemetry;

namespace Uno.UI.SourceGenerators.XamlGenerator
{
	[Generator]
	public partial class XamlCodeGenerator : IIncrementalGenerator
	{
		private static readonly char[] s_commaArray = new[] { ',' };

		public void Initialize(IncrementalGeneratorInitializationContext context)
		{
			//var process = Process.GetCurrentProcess().ProcessName;
			//if (process.IndexOf("VBCSCompiler", StringComparison.OrdinalIgnoreCase) is not -1
			//	|| process.IndexOf("csc", StringComparison.OrdinalIgnoreCase) is not -1)
			//{
			//	Debugger.Launch();
			//}

			RegisterLocalizationResources(context);

			// Value-equal per-file source descriptors. This node caches per file:
			// when nothing in (path, link, target path, content, metadata) changes,
			// the downstream parsing step sees byte-identical input and Roslyn
			// skips invocation entirely.
			var xamlSourceFilesProvider = context.AdditionalTextsProvider
				.Combine(context.AnalyzerConfigOptionsProvider)
				.Select(static (pair, ct) => BuildXamlSourceFile(pair.Left, pair.Right, ct))
				.Where(static x => x is not null)
				.Select(static (x, _) => x!);

			// Parser settings extracted from MSBuild + compilation-derived implicit
			// prefixes. The implicit-prefix list rarely changes across compilations
			// (assembly references are stable across typing), and EquatableArray gives
			// it structural equality so the settings output is value-equal when nothing
			// material actually changed.
			var parserSettingsProvider = context.AnalyzerConfigOptionsProvider
				.Combine(context.CompilationProvider)
				.Select(static (pair, ct) => BuildParserSettings(pair.Left, pair.Right));

			// Per-file XAML parsing. Files that don't use ?IsTypePresent/IsTypeNotPresent
			// go through a compilation-free path: input is (XamlSourceFile, XamlParserSettings)
			// — both value-equal — so Roslyn caches the parse entirely across compilation
			// changes (i.e. C# edits don't reparse XAML).
			var fastParsedFilesProvider = xamlSourceFilesProvider
				.Where(static f => !f.RequiresCompilationDuringParse)
				.Combine(parserSettingsProvider)
				.Select(static (pair, ct) => new ParsedXamlFile(pair.Left, XamlFileParser.ParseSingle(pair.Left, pair.Right, metadataHelper: null, ct)));

			// Files that DO use ?IsTypePresent / ?IsTypeNotPresent need the compilation
			// to resolve the type-presence checks. These re-parse on every compilation
			// change (no Roslyn-level caching), but the parser's internal static
			// content-checksum cache still avoids redoing the actual XAML work.
			var slowParsedFilesProvider = xamlSourceFilesProvider
				.Where(static f => f.RequiresCompilationDuringParse)
				.Combine(parserSettingsProvider)
				.Combine(context.CompilationProvider)
				.Select(static (triple, ct) =>
				{
					var ((source, settings), compilation) = triple;
					var metadataHelper = new RoslynMetadataHelper(compilation);
					return new ParsedXamlFile(source, XamlFileParser.ParseSingle(source, settings, metadataHelper, ct));
				});

			// Collect both halves; the merged collection's value equality is element-wise
			// over each ParsedXamlFile reference. Since the parser's internal cache returns
			// the same XamlFileDefinition instance for unchanged content, an unchanged file
			// produces a reference-equal ParsedXamlFile output.
			var allParsedFilesProvider = fastParsedFilesProvider.Collect()
				.Combine(slowParsedFilesProvider.Collect())
				.Select(static (pair, _) => pair.Left.AddRange(pair.Right));

			var additionalFilesProvider = context.AdditionalTextsProvider.Collect();

			var combined = context.CompilationProvider
				.Combine(allParsedFilesProvider)
				.Combine(additionalFilesProvider)
				.Combine(context.AnalyzerConfigOptionsProvider);

			context.RegisterSourceOutput(combined, static (spc, data) =>
			{
				var (((compilation, parsedFiles), additionalFiles), optionsProvider) = data;

				if (!IsValidPlatform(compilation))
				{
					return;
				}

				var sourceContext = XamlSourceContext.FromIncrementalInputs(
					compilation: compilation,
					cancellationToken: spc.CancellationToken,
					additionalFiles: additionalFiles,
					optionsProvider: optionsProvider,
					reportDiagnostic: spc.ReportDiagnostic,
					addSource: spc.AddSource);

				var preparsed = parsedFiles
					.Where(static p => p.Definition is not null)
					.Select(static p => p.Definition!)
					.ToArray();

				var isDesignTimeBuild = IsDesignTimeBuild(optionsProvider);
				var gen = new XamlCodeGeneration(sourceContext, isDesignTimeBuild);
				var generatedTrees = gen.Generate(preparsed);

				foreach (var tree in generatedTrees)
				{
					spc.AddSource(tree.Key, tree.Value);
				}

				DumpXamlSourceGeneratorState(sourceContext, generatedTrees);
			});
		}

		/// <summary>
		/// Builds a value-equal <see cref="XamlSourceFile"/> from an AdditionalText
		/// and its analyzer-config metadata. Returns <c>null</c> for files that aren't
		/// Page/UnoPage/ApplicationDefinition/UnoApplicationDefinition or that opt out
		/// via a non-WinUI XamlRuntime.
		/// </summary>
		private static XamlSourceFile? BuildXamlSourceFile(AdditionalText file, AnalyzerConfigOptionsProvider optionsProvider, System.Threading.CancellationToken ct)
		{
			var options = optionsProvider.GetOptions(file);
			if (!options.TryGetValue("build_metadata.AdditionalFiles.SourceItemGroup", out var sourceItemGroup))
			{
				return null;
			}

			var isApplicationDefinition = sourceItemGroup == "ApplicationDefinition" || sourceItemGroup == "UnoApplicationDefinition";
			var isPage = sourceItemGroup == "Page" || sourceItemGroup == "UnoPage";
			if (!isPage && !isApplicationDefinition)
			{
				return null;
			}

			// Mirror IsWinUIItem: include items with no XamlRuntime metadata or with "WinUI"
			options.TryGetValue("build_metadata.AdditionalFiles.XamlRuntime", out var xamlRuntime);
			if (!string.IsNullOrEmpty(xamlRuntime) && xamlRuntime != "WinUI")
			{
				return null;
			}

			var sourceText = file.GetText(ct);
			if (sourceText is null)
			{
				return null;
			}

			options.TryGetValue("build_metadata.AdditionalFiles.Link", out var link);
			options.TryGetValue("build_metadata.AdditionalFiles.TargetPath", out var targetPath);
			options.TryGetValue("build_metadata.AdditionalFiles.UnoGenerateCodeBehind", out var generateCodeBehindOverride);

			var sourceLink = ResolveSourceLink(file, options, optionsProvider);
			var resolvedTargetPath = !string.IsNullOrEmpty(targetPath)
				? targetPath!
				: !string.IsNullOrEmpty(link)
					? link!
					: file.Path;

			return new XamlSourceFile(
				FilePath: file.Path,
				SourceLink: sourceLink,
				TargetFilePath: resolvedTargetPath.Replace('\\', '/'),
				Content: sourceText.ToString(),
				Checksum: sourceText.GetChecksum(),
				IsApplicationDefinition: isApplicationDefinition,
				GenerateCodeBehindOverride: generateCodeBehindOverride ?? string.Empty);
		}

		/// <summary>
		/// Mirrors the MSBuild AssignLinkMetadata logic used by the legacy
		/// XamlCodeGeneration.GetSourceLink path so source-link values are
		/// computed identically across the two parsing paths.
		/// </summary>
		private static string ResolveSourceLink(AdditionalText file, AnalyzerConfigOptions options, AnalyzerConfigOptionsProvider optionsProvider)
		{
			options.TryGetValue("build_metadata.AdditionalFiles.Link", out var link);
			options.TryGetValue("build_metadata.AdditionalFiles.DefiningProjectFullPath", out var definingProjectFullPath);
			options.TryGetValue("build_metadata.AdditionalFiles.FullPath", out var fullPath);

			optionsProvider.GlobalOptions.TryGetValue("build_property.MSBuildProjectFullPath", out var projectFullPath);
			var projectDirectory = string.IsNullOrEmpty(projectFullPath)
				? ""
				: Path.GetDirectoryName(projectFullPath) ?? "";

			if (!string.IsNullOrEmpty(link))
			{
				return link!;
			}

			if (string.IsNullOrEmpty(definingProjectFullPath))
			{
				return Path.IsPathRooted(file.Path)
					? file.Path.TrimStart(projectDirectory.ToCharArray()).TrimStart(Path.DirectorySeparatorChar)
					: file.Path;
			}

			var definingProjectDirectory = Path.GetDirectoryName(definingProjectFullPath) + Path.DirectorySeparatorChar;

			if (!string.IsNullOrEmpty(fullPath) && fullPath!.StartsWith(definingProjectDirectory, StringComparison.OrdinalIgnoreCase))
			{
				return fullPath.Substring(definingProjectDirectory.Length);
			}

			return file.Path;
		}

		private static XamlParserSettings BuildParserSettings(AnalyzerConfigOptionsProvider optionsProvider, Compilation compilation)
		{
			var globalOptions = optionsProvider.GlobalOptions;
			globalOptions.TryGetValue("build_property.ExcludeXamlNamespacesProperty", out var excludeXamlNamespacesProperty);
			globalOptions.TryGetValue("build_property.IncludeXamlNamespacesProperty", out var includeXamlNamespacesProperty);
			globalOptions.TryGetValue("build_property.UnoEnableImplicitXamlNamespaces", out var enableImplicitRaw);

			excludeXamlNamespacesProperty ??= "";
			includeXamlNamespacesProperty ??= "";

			var excludeArray = excludeXamlNamespacesProperty.Split(s_commaArray, StringSplitOptions.RemoveEmptyEntries);
			var includeArray = includeXamlNamespacesProperty.Split(s_commaArray, StringSplitOptions.RemoveEmptyEntries);

			var enableImplicit = bool.TryParse(enableImplicitRaw, out var v) && v;

			ImmutableArray<(string Prefix, string Uri)> implicitPrefixes;
			if (enableImplicit)
			{
				var prefixes = GlobalNamespaceResolver.GetImplicitPrefixes(compilation);
				implicitPrefixes = ImmutableArray.CreateRange(prefixes);
			}
			else
			{
				implicitPrefixes = ImmutableArray<(string, string)>.Empty;
			}

			return new XamlParserSettings(
				ExcludeXamlNamespacesProperty: excludeXamlNamespacesProperty,
				IncludeXamlNamespacesProperty: includeXamlNamespacesProperty,
				ExcludeXamlNamespaces: new EquatableArray<string>(ImmutableArray.CreateRange(excludeArray)),
				IncludeXamlNamespaces: new EquatableArray<string>(ImmutableArray.CreateRange(includeArray)),
				EnableImplicitNamespaces: enableImplicit,
				ImplicitPrefixes: new EquatableArray<(string Prefix, string Uri)>(implicitPrefixes));
		}

		/// <summary>
		/// Wires the LocalizationResources output as its own pipeline node so the
		/// AssemblyMetadata("UnoHasLocalizationResources", ...) attribute is only
		/// recomputed when a PRIResource AdditionalText (or the platform-validity
		/// of the compilation) actually changes.
		/// </summary>
		private static void RegisterLocalizationResources(IncrementalGeneratorInitializationContext context)
		{
			var priResourceFiles = context.AdditionalTextsProvider
				.Combine(context.AnalyzerConfigOptionsProvider)
				.Select(static (pair, ct) =>
				{
					var (file, optionsProvider) = pair;
					var options = optionsProvider.GetOptions(file);
					return options.TryGetValue("build_metadata.AdditionalFiles.SourceItemGroup", out var group)
						&& group == "PRIResource"
						? file
						: null;
				})
				.Where(static file => file is not null)
				.Select(static (file, ct) => HasAnyResourceEntries(file!, ct))
				.Collect();

			var localizationProvider = priResourceFiles
				.Select(static (results, _) => results.Any(static r => r))
				.Combine(context.CompilationProvider.Select(static (c, _) => IsValidPlatform(c)));

			context.RegisterSourceOutput(localizationProvider, static (spc, data) =>
			{
				var (hasResources, isValidPlatform) = data;
				if (!isValidPlatform)
				{
					return;
				}

				spc.AddSource(
					"LocalizationResources",
					$"""
					// <auto-generated />
					[assembly: global::System.Reflection.AssemblyMetadata("UnoHasLocalizationResources", "{hasResources.ToString(System.Globalization.CultureInfo.InvariantCulture)}")]
					""");
			});
		}

		private static bool HasAnyResourceEntries(AdditionalText file, System.Threading.CancellationToken ct)
		{
			try
			{
				if (file.GetText(ct) is not { } sourceText)
				{
					return false;
				}

				var doc = new System.Xml.XmlDocument();
				doc.LoadXml(sourceText.ToString());
				return doc.SelectNodes("/root/data") is { Count: > 0 };
			}
			catch
			{
				// Mirrors XamlCodeGeneration.BuildLocalResourceDetails: malformed PRIResource files are
				// reported there during ResourceDetailsCollection construction; here we just refuse
				// to claim "has resources" so the LocalizationResources attribute stays false.
				return false;
			}
		}

		private static bool IsValidPlatform(Compilation compilation)
			=> compilation.Options.OutputKind != OutputKind.WindowsRuntimeApplication
				&& compilation.Options.OutputKind != OutputKind.WindowsRuntimeMetadata;

		private static bool IsDesignTimeBuild(AnalyzerConfigOptionsProvider optionsProvider)
		{
			optionsProvider.GlobalOptions.TryGetValue("build_property.BuildingProject", out var buildingProject);
			optionsProvider.GlobalOptions.TryGetValue("build_property.DesignTimeBuild", out var designTimeBuild);

			return string.Equals(buildingProject, "false", StringComparison.OrdinalIgnoreCase)
				|| string.Equals(designTimeBuild, "true", StringComparison.OrdinalIgnoreCase);
		}
	}
}
