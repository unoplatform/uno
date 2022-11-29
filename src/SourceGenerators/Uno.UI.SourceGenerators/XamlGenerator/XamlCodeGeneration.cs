extern alias __uno;
#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Globalization;
using System.Xml;
using Uno.Roslyn;
using Microsoft.CodeAnalysis;
using Uno.Extensions;
using Uno.UI.SourceGenerators.Telemetry;
using Uno.UI.Xaml;
using System.Drawing;
using __uno::Uno.Xaml;
using Microsoft.CodeAnalysis.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Concurrent;
using System.Collections.Immutable;

#if NETFRAMEWORK
using Microsoft.Build.Execution;
using Uno.SourceGeneration;
using GeneratorExecutionContext = Uno.SourceGeneration.GeneratorExecutionContext;
#endif

namespace Uno.UI.SourceGenerators.XamlGenerator
{
	internal partial class XamlCodeGeneration
	{
		internal const string ParseContextPropertyName = "__ParseContext_";
		internal const string ParseContextPropertyType = "global::Uno.UI.Xaml.XamlParseContext";

		private readonly Uno.Roslyn.MSBuildItem[] _xamlSourceFiles;
		private readonly string[] _xamlSourceLinks;
		private readonly string _targetPath;
		private readonly string _defaultLanguage;
		private readonly bool _isWasm;
		private readonly bool _isDesignTimeBuild;
		private readonly string _defaultNamespace;
		private readonly string[] _assemblySearchPaths;
		private readonly string _excludeXamlNamespaces;
		private readonly string _includeXamlNamespaces;
		private readonly string[] _analyzerSuppressions;
		private readonly Uno.Roslyn.MSBuildItem[] _resourceFiles;
		private readonly Dictionary<string, string[]> _uiAutomationMappings;
		private readonly string _configuration;
		private readonly bool _isDebug;
		/// <summary>
		/// Should hot reload-related calls be generated? By default this is true iff building in debug, but it can be forced to always true or false using the "UnoForceHotReloadCodeGen" project flag.
		/// </summary>
		private readonly bool _isHotReloadEnabled;
		private readonly string _projectDirectory;
		private readonly string _projectFullPath;
		private readonly bool _xamlResourcesTrimming;
		private bool _shouldWriteErrorOnInvalidXaml;
		private readonly RoslynMetadataHelper _metadataHelper;

		/// <summary>
		/// If set, code generated from XAML will be annotated with the source method and line # in XamlFileGenerator, for easier debugging.
		/// </summary>
		private readonly bool _shouldAnnotateGeneratedXaml;

		/// <summary>
		/// When set, Visual State Manager children will be initialized lazily for performance
		/// </summary>
		private readonly bool _isLazyVisualStateManagerEnabled = true;

		private static DateTime _buildTasksBuildDate = File.GetLastWriteTime(new Uri(typeof(XamlFileGenerator).Assembly.Location).LocalPath);
		private INamedTypeSymbol[]? _ambientGlobalResources;
		private readonly bool _isUiAutomationMappingEnabled;
		private Dictionary<string, string> _legacyTypes;

		// Determines if the source generator will skip the inclusion of UseControls in the
		// visual tree. See https://github.com/unoplatform/uno/issues/61
		private bool _skipUserControlsInVisualTree;
		private readonly GeneratorExecutionContext _generatorContext;

		private bool IsUnoAssembly
			=> _defaultNamespace == "Uno.UI";

		private bool IsUnoFluentAssembly
			=> _defaultNamespace == "Uno.UI.FluentTheme" || _defaultNamespace.StartsWith("Uno.UI.FluentTheme.v", StringComparison.Ordinal);

		private const string WinUIThemeResourcePathSuffixFormatString = "themeresources_v{0}.xaml";
		private static string WinUICompactPathSuffix = Path.Combine("DensityStyles", "Compact.xaml");

#pragma warning disable 649 // Unused member
		private readonly bool _forceGeneration;
#pragma warning restore 649 // Unused member

		public XamlCodeGeneration(GeneratorExecutionContext context)
		{
			// To easily debug XAML code generation:
			// Add <UnoUISourceGeneratorDebuggerBreak>True</UnoUISourceGeneratorDebuggerBreak> to your project

			if (!Helpers.DesignTimeHelper.IsDesignTime(context)
				&& (context.GetMSBuildPropertyValue("UnoUISourceGeneratorDebuggerBreak")?.Equals("True", StringComparison.OrdinalIgnoreCase) ?? false))
			{
				Debugger.Launch();
			}

			_generatorContext = context;
			InitTelemetry(context);

			_legacyTypes = context
				.GetMSBuildPropertyValue("LegacyTypesProperty")
				.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
				.ToList()
				.ToDictionary(k => k, fullyQualifiedName => fullyQualifiedName.Split('.').Last());

			_metadataHelper = new RoslynMetadataHelper(context, _legacyTypes);
			_assemblySearchPaths = Array.Empty<string>();

			_configuration = context.GetMSBuildPropertyValue("Configuration")
				?? throw new InvalidOperationException("The configuration property must be provided");

			_isDebug = string.Equals(_configuration, "Debug", StringComparison.OrdinalIgnoreCase);

			_projectFullPath = context.GetMSBuildPropertyValue("MSBuildProjectFullPath");
			_projectDirectory = Path.GetDirectoryName(_projectFullPath)
				?? throw new InvalidOperationException($"MSBuild property MSBuildProjectFullPath value {_projectFullPath} is not valid");

			var pageItems = GetWinUIItems("Page").Concat(GetWinUIItems("UnoPage"));
			var applicationDefinitionItems = GetWinUIItems("ApplicationDefinition").Concat(GetWinUIItems("UnoApplicationDefinition"));

			var xamlItems = pageItems
				.Concat(applicationDefinitionItems)
				.Distinct()
				.ToArray();

			_xamlSourceFiles = xamlItems;

			_xamlSourceLinks = xamlItems.Select(GetSourceLink).ToArray();

			_excludeXamlNamespaces = context.GetMSBuildPropertyValue("ExcludeXamlNamespacesProperty");

			_includeXamlNamespaces = context.GetMSBuildPropertyValue("IncludeXamlNamespacesProperty");

			_analyzerSuppressions = context.GetMSBuildPropertyValue("XamlGeneratorAnalyzerSuppressionsProperty").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

			_resourceFiles = context.GetMSBuildItemsWithAdditionalFiles("PRIResource").ToArray();

			if (bool.TryParse(context.GetMSBuildPropertyValue("UseUnoXamlParser"), out var useUnoXamlParser) && useUnoXamlParser)
			{
				XamlRedirection.XamlConfig.IsUnoXaml = useUnoXamlParser || XamlRedirection.XamlConfig.IsMono;
			}

			if (bool.TryParse(context.GetMSBuildPropertyValue("UnoSkipUserControlsInVisualTree"), out var skipUserControlsInVisualTree))
			{
				_skipUserControlsInVisualTree = skipUserControlsInVisualTree;
			}

			if (bool.TryParse(context.GetMSBuildPropertyValue("ShouldWriteErrorOnInvalidXaml"), out var shouldWriteErrorOnInvalidXaml))
			{
				_shouldWriteErrorOnInvalidXaml = shouldWriteErrorOnInvalidXaml;
			}

			if (!bool.TryParse(context.GetMSBuildPropertyValue("IsUiAutomationMappingEnabled") ?? "", out _isUiAutomationMappingEnabled))
			{
				_isUiAutomationMappingEnabled = false;
			}

			if (bool.TryParse(context.GetMSBuildPropertyValue("ShouldAnnotateGeneratedXaml"), out var shouldAnnotateGeneratedXaml))
			{
				_shouldAnnotateGeneratedXaml = shouldAnnotateGeneratedXaml;
			}

			if (bool.TryParse(context.GetMSBuildPropertyValue("UnoXamlLazyVisualStateManagerEnabled"), out var isLazyVisualStateManagerEnabled))
			{
				_isLazyVisualStateManagerEnabled = isLazyVisualStateManagerEnabled;
			}

			if (bool.TryParse(context.GetMSBuildPropertyValue("UnoXamlResourcesTrimming"), out var xamlResourcesTrimming))
			{
				_xamlResourcesTrimming = xamlResourcesTrimming;
			}

			if (bool.TryParse(context.GetMSBuildPropertyValue("UnoForceHotReloadCodeGen"), out var isHotReloadEnabled))
			{
				_isHotReloadEnabled = isHotReloadEnabled;
			}
			else
			{
				_isHotReloadEnabled = _isDebug;
			}

			_targetPath = Path.Combine(
				_projectDirectory,
				context.GetMSBuildPropertyValue("IntermediateOutputPath")
			);

			_defaultLanguage = context.GetMSBuildPropertyValue("DefaultLanguage");

			_uiAutomationMappings = context.GetMSBuildItemsWithAdditionalFiles("CustomUiAutomationMemberMappingAdjusted")
				.Select(i => new
				{
					Key = i.Identity,
					Value = i.GetMetadataValue("Mappings")
						?.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
						.Select(m => m.Trim())
						.Where(m => m.HasValueTrimmed())
				})
				.GroupBy(p => p.Key)
				.ToDictionary(p => p.Key, p => p.SelectMany(x => x.Value.Safe()).ToArray());

			_defaultNamespace = context.GetMSBuildPropertyValue("RootNamespace");

			_isWasm = context.GetMSBuildPropertyValue("DefineConstantsProperty")?.Contains("__WASM__") ?? false;
			_isDesignTimeBuild = Helpers.DesignTimeHelper.IsDesignTime(context);
		}

		private static bool IsWinUIItem(Uno.Roslyn.MSBuildItem item)
			=> item.GetMetadataValue("XamlRuntime") is { } xamlRuntime
				? xamlRuntime == "WinUI" || string.IsNullOrWhiteSpace(xamlRuntime)
				: true;

		private IEnumerable<Uno.Roslyn.MSBuildItem> GetWinUIItems(string name)
			=> _generatorContext.GetMSBuildItemsWithAdditionalFiles(name).Where(IsWinUIItem);

		/// <summary>
		/// Get the file location as seen in the IDE, used for ResourceDictionary.Source resolution.
		/// </summary>
		private string GetSourceLink(Uno.Roslyn.MSBuildItem projectItemInstance)
		{
			var link = projectItemInstance.GetMetadataValue("Link");
			var definingProjectFullPath = projectItemInstance.GetMetadataValue("DefiningProjectFullPath");
			var fullPath = projectItemInstance.GetMetadataValue("FullPath");

			// Reproduce the logic from https://github.com/dotnet/msbuild/blob/e70a3159d64f9ed6ec3b60253ef863fa883a99b1/src/Tasks/AssignLinkMetadata.cs
			if (link.IsNullOrEmpty())
			{
				if (definingProjectFullPath.IsNullOrEmpty())
				{
					// Both Uno.SourceGeneration uses relative paths and Roslyn Generators provide
					// full paths. Dependents need specific portions so adjust the paths here for now.
					// For the case of Roslyn generators, DefiningProjectFullPath is not populated on purpose
					// so that we can adjust paths properly.
					if (link.IsNullOrEmpty())
					{
						return Path.IsPathRooted(projectItemInstance.Identity)
							? projectItemInstance.Identity.TrimStart(_projectDirectory).TrimStart(Path.DirectorySeparatorChar)
							: projectItemInstance.Identity;
					}
				}
				else
				{
					var definingProjectDirectory = Path.GetDirectoryName(definingProjectFullPath) + Path.DirectorySeparatorChar;

					if (fullPath.StartsWith(definingProjectDirectory, StringComparison.OrdinalIgnoreCase))
					{
						link = fullPath.Substring(definingProjectDirectory.Length);
					}
					else
					{
						link = projectItemInstance.GetMetadataValue("Identity");
					}
				}
			}

			return link;
		}

		public List<KeyValuePair<string, string>> Generate(GenerationRunInfo generationRunInfo)
		{
			var stopwatch = Stopwatch.StartNew();

			try
			{
#if DEBUG
				Console.WriteLine("Xaml Source Generation is using the {0} Xaml Parser", XamlRedirection.XamlConfig.IsUnoXaml ? "Uno.UI" : "System");
#endif

				var lastBinaryUpdateTime = _forceGeneration ? DateTime.MaxValue : GetLastBinaryUpdateTime();

				var resourceKeys = GetResourceKeys(_generatorContext.CancellationToken);
				var filesFull = new XamlFileParser(_excludeXamlNamespaces, _includeXamlNamespaces, _metadataHelper)
					.ParseFiles(_xamlSourceFiles, _generatorContext.CancellationToken);

				var xamlTypeToXamlTypeBaseMap = new ConcurrentDictionary<INamedTypeSymbol, XamlRedirection.XamlType>();
				Parallel.ForEach(filesFull, file =>
				{
					var topLevelControl = file.Objects.FirstOrDefault();
					if (topLevelControl is null)
					{
						return;
					}

					var xClassSymbol = XamlFileGenerator.FindClassSymbol(topLevelControl, _metadataHelper);
					if (xClassSymbol is not null)
					{
						xamlTypeToXamlTypeBaseMap.TryAdd(xClassSymbol, topLevelControl.Type);
					}
				});


				var files = filesFull
					.Trim()
					.OrderBy(f => f.UniqueID)
					.ToArray();

				TrackStartGeneration(files);

				var globalStaticResourcesMap = BuildAssemblyGlobalStaticResourcesMap(files, filesFull, _xamlSourceLinks, _generatorContext.CancellationToken);

				var filesToProcess = files.AsParallel();

				filesToProcess = filesToProcess
					.WithCancellation(_generatorContext.CancellationToken);

				if (Debugger.IsAttached)
				{
					filesToProcess = filesToProcess
						.WithDegreeOfParallelism(1);
				}

				var outputFiles = filesToProcess.Select(file => new KeyValuePair<string, string>(

							file.UniqueID,
							new XamlFileGenerator(
									file: file,
									targetPath: _targetPath,
									defaultNamespace: _defaultNamespace,
									metadataHelper: _metadataHelper,
									fileUniqueId: file.UniqueID,
									lastReferenceUpdateTime: lastBinaryUpdateTime,
									analyzerSuppressions: _analyzerSuppressions,
									globalStaticResourcesMap: globalStaticResourcesMap,
									resourceKeys: resourceKeys,
									isUiAutomationMappingEnabled: _isUiAutomationMappingEnabled,
									uiAutomationMappings: _uiAutomationMappings,
									defaultLanguage: _defaultLanguage,
									shouldWriteErrorOnInvalidXaml: _shouldWriteErrorOnInvalidXaml,
									isWasm: _isWasm,
									isDebug: _isDebug,
									isHotReloadEnabled: _isHotReloadEnabled,
									isDesignTimeBuild: _isDesignTimeBuild,
									skipUserControlsInVisualTree: _skipUserControlsInVisualTree,
									shouldAnnotateGeneratedXaml: _shouldAnnotateGeneratedXaml,
									isUnoAssembly: IsUnoAssembly,
									isUnoFluentAssembly: IsUnoFluentAssembly,
									isLazyVisualStateManagerEnabled: _isLazyVisualStateManagerEnabled,
									generatorContext: _generatorContext,
									xamlResourcesTrimming: _xamlResourcesTrimming,
									generationRunFileInfo: generationRunInfo.GetRunFileInfo(file.UniqueID),
									xamlTypeToXamlTypeBaseMap: xamlTypeToXamlTypeBaseMap
								)
								.GenerateFile()
						)
					)
					.ToList();


				outputFiles.Add(new KeyValuePair<string, string>("GlobalStaticResources", GenerateGlobalResources(files, globalStaticResourcesMap)));

				TrackGenerationDone(stopwatch.Elapsed);

				return outputFiles;
			}
			catch (OperationCanceledException)
			{
				throw;
			}
			catch (Exception e)
			{
				TrackGenerationFailed(e, stopwatch.Elapsed);

#if NETFRAMEWORK
				throw;
#else
				return ProcessParsingException(e);
#endif
			}
			finally
			{
				_telemetry.Flush();
				_telemetry.Dispose();
			}
		}

		private List<KeyValuePair<string, string>> ProcessParsingException(Exception e)
		{
			IEnumerable<Exception> Flatten(Exception ex)
			{
				if (ex is AggregateException agg)
				{
					foreach (var inner in agg.InnerExceptions)
					{
						foreach (var inner2 in Flatten(inner))
						{
							yield return inner2;
						}
					}
				}
				else
				{
					if (ex.InnerException != null)
					{
						foreach (var inner2 in Flatten(ex.InnerException))
						{
							yield return inner2;
						}
					}

					yield return ex;
				}
			}

			foreach (var exception in Flatten(e))
			{
				var diagnostic = Diagnostic.Create(
					XamlCodeGenerationDiagnostics.GenericXamlErrorRule,
					GetExceptionFileLocation(exception),
					exception.Message);

				_generatorContext.ReportDiagnostic(diagnostic);
			}

			return new List<KeyValuePair<string, string>>();
		}

		private Location? GetExceptionFileLocation(Exception exception)
		{
			if (exception is XamlParsingException xamlParsingException)
			{
				var xamlFile = _generatorContext.AdditionalFiles.FirstOrDefault(f => f.Path == xamlParsingException.FilePath);

				if (xamlFile != null
					&& xamlFile.GetText() is { } xamlText
					&& xamlParsingException.LineNumber.HasValue
					&& xamlParsingException.LinePosition.HasValue)
				{
					var linePosition = new LinePosition(
						Math.Max(0, xamlParsingException.LineNumber.Value - 1),
						Math.Max(0, xamlParsingException.LinePosition.Value - 1)
					);

					return Location.Create(
						xamlFile.Path,
						xamlText.Lines.ElementAtOrDefault(xamlParsingException.LineNumber.Value - 1).Span,
						new LinePositionSpan(linePosition, linePosition)
					);
				}
			}

			return null;
		}

		private XamlGlobalStaticResourcesMap BuildAssemblyGlobalStaticResourcesMap(XamlFileDefinition[] files, XamlFileDefinition[] filesFull, string[] links, CancellationToken ct)
		{
			var map = new XamlGlobalStaticResourcesMap();

			BuildLocalProjectResources(files, map, ct);
			BuildAmbientResources(map, ct);
			map.BuildResourceDictionaryMap(filesFull, links);

			return map;
		}

		private void BuildAmbientResources(XamlGlobalStaticResourcesMap map, CancellationToken ct)
		{
			// Lookup for GlobalStaticResources classes in external assembly
			// references only, and in Uno.UI itself for generic.xaml-like resources.

			var assembliesQuery =
				from ext in _metadataHelper.Compilation.ExternalReferences
				let sym = _metadataHelper.Compilation.GetAssemblyOrModuleSymbol(ext) as IAssemblySymbol
				where sym != null
				select sym;

			var query = from sym in assembliesQuery
						from module in sym.Modules

						// Only consider assemblies that reference Uno.UI
						where module.ReferencedAssemblies.Any(r => r.Name == "Uno.UI") || sym.Name == "Uno.UI"

						// Don't consider Uno.UI.FluentTheme assemblies, as they manage their own initialization
						where sym.Name != "Uno.UI.FluentTheme" && !sym.Name.StartsWith("Uno.UI.FluentTheme.v", StringComparison.InvariantCulture)

						from typeName in sym.GlobalNamespace.GetNamespaceTypes()
						where typeName.Name.EndsWith("GlobalStaticResources", StringComparison.Ordinal)
						select typeName;

			_ambientGlobalResources = query.Distinct().ToArray();

			foreach (var ambientResources in _ambientGlobalResources)
			{
				ct.ThrowIfCancellationRequested();

				var publicProperties = from member in ambientResources.GetAllProperties()
									   where member.DeclaredAccessibility == Microsoft.CodeAnalysis.Accessibility.Public
									   select member;

				foreach (var member in publicProperties)
				{
					map.Add(member.Name, ambientResources.ContainingNamespace.ToDisplayString(), XamlGlobalStaticResourcesMap.ResourcePrecedence.System);
				}
			}
		}

		private void BuildLocalProjectResources(XamlFileDefinition[] files, XamlGlobalStaticResourcesMap map, CancellationToken ct)
		{
			foreach (var file in files)
			{
				ct.ThrowIfCancellationRequested();

				var topLevelControl = file.Objects.FirstOrDefault();

				if (topLevelControl?.Type.Name == "ResourceDictionary")
				{
					BuildResourceMap(topLevelControl, map);

					var themeDictionaries = topLevelControl.Members.FirstOrDefault(m => m.Member.Name == "ThemeDictionaries");

					if (themeDictionaries != null)
					{
						// We extract all distinct keys of all themed resource dictionaries defined and add them to global map

						IEnumerable<string> GetResources(XamlObjectDefinition themeDictionary)
						{
							if (!(themeDictionary.Members
								.FirstOrDefault(x => x.Member.Name.Equals("Key"))
								?.Value is string))
							{
								yield break;
							}

							var resources = themeDictionary.Members
								.FirstOrDefault(x => x.Member.Name.Equals("_UnknownContent"))
								?.Objects;

							if (resources != null)
							{
								foreach (var resource in resources)
								{
									if (resource.Members.FirstOrDefault(x => x.Member.Name.Equals("Key"))
										?.Value is string resourceKey)
									{
										yield return resourceKey;
									}
								}
							}
						}

						var themeResources = themeDictionaries
							.Objects
							.SelectMany(GetResources)
							.Distinct();

						foreach (var themeResource in themeResources)
						{
							map.Add(themeResource, _defaultNamespace, XamlGlobalStaticResourcesMap.ResourcePrecedence.Local);
						}
					}
				}
			}
		}

		private void BuildResourceMap(XamlObjectDefinition parentNode, XamlGlobalStaticResourcesMap map)
		{
			var contentNode = parentNode.Members.FirstOrDefault(m => m.Member.Name == "_UnknownContent");

			if (contentNode != null)
			{
				foreach (var resource in contentNode.Objects)
				{
					var key = resource.Members.FirstOrDefault(m => m.Member.Name == "Key");

					if (
						key != null
						&& key.Value?.ToString() is { } value
						&& resource.Type.Name != "StaticResource"
					)
					{
						map.Add(value, _defaultNamespace, XamlGlobalStaticResourcesMap.ResourcePrecedence.Local);
					}
				}
			}
		}

		//get keys of localized strings
		private ImmutableHashSet<string> GetResourceKeys(CancellationToken ct)
		{
			ImmutableHashSet<string> resourceKeys = _resourceFiles
				.AsParallel()
				.WithCancellation(ct)
				.SelectMany(file => {
#if DEBUG
					Console.WriteLine("Parse resource file : " + file.Identity);
#endif
					try
					{
						var sourceText = file.File.GetText(ct)!;
						var cachedFileKey = new ResourceCacheKey(file.Identity, sourceText.GetChecksum());
						if (_cachedResources.TryGetValue(cachedFileKey, out var cachedResource))
						{
							_cachedResources[cachedFileKey] = cachedResource.WithUpdatedLastTimeUsed();
							ScavengeCache();
							return cachedResource.ResourceKeys;
						}

						ScavengeCache();

						//load document
						var doc = new XmlDocument();
						doc.LoadXml(sourceText.ToString());

						var rewriterBuilder = new StringBuilder();

						//extract all localization keys from Win10 resource file
						// https://docs.microsoft.com/en-us/dotnet/standard/data/xml/compiled-xpath-expressions?redirectedfrom=MSDN#higher-performance-xpath-expressions
						// Per this documentation, /root/data should be more performant than //data
						var keys = doc.SelectNodes("/root/data")
							?.Cast<XmlElement>()
							.Select(node => RewriteResourceKeyName(rewriterBuilder, node.GetAttribute("name")))
							.ToArray() ?? Array.Empty<string>();
						_cachedResources[cachedFileKey] = new CachedResource(DateTimeOffset.Now, keys);
						return keys;
					}
					catch (Exception e)
					{
						var message = $"Unable to parse resource file [{file.Identity}], make sure it is a valid resw file. ({e.Message})";

#if NETSTANDARD
							var diagnostic = Diagnostic.Create(
								XamlCodeGenerationDiagnostics.ResourceParsingFailureRule,
								null,
								message);

							_generatorContext.ReportDiagnostic(diagnostic);

							return Array.Empty<string>();
#else
						throw new InvalidOperationException(message, e);
#endif
					}
				})
				.Distinct()
				.ToImmutableHashSet();

#if DEBUG
			Console.WriteLine(resourceKeys.Count + " localization keys found");
#endif
			return resourceKeys;
		}

		private string RewriteResourceKeyName(StringBuilder builder, string keyName)
		{
			var firstDotIndex = keyName.IndexOf('.');
			if (firstDotIndex != -1)
			{
				builder.Clear();
				builder.Append(keyName);

				builder[firstDotIndex] = '/';

				return builder.ToString();
			}

			return keyName;
		}

		private DateTime GetLastBinaryUpdateTime()
		{
			// Determine the last update time, to allow for the re-generation of the files.
			// Include the current assembly, as it might have been updated since the last generation.

			return _assemblySearchPaths
				.Select(File.GetLastWriteTime)
				.Concat(_buildTasksBuildDate)
				.Max();
		}

		private string GenerateGlobalResources(IEnumerable<XamlFileDefinition> files, XamlGlobalStaticResourcesMap map)
		{
			var outputFile = Path.Combine(_targetPath, "GlobalStaticResources.g.cs");

			var writer = new IndentedStringBuilder();

			writer.AppendLineIndented("// <autogenerated />");
			AnalyzerSuppressionsGenerator.GenerateCSharpPragmaSupressions(writer, _analyzerSuppressions);
			writer.AppendLineIndented("using System;");
			writer.AppendLineIndented("using System.Linq;");
			writer.AppendLineIndented("using System.Collections.Generic;");
			writer.AppendLineIndented("using Uno.Extensions;");
			writer.AppendLineIndented("using Uno;");
			writer.AppendLineIndented("using System.Diagnostics;");

			//TODO Determine the list of namespaces to use
			writer.AppendLineIndented($"using {XamlConstants.BaseXamlNamespace};");
			writer.AppendLineIndented($"using {XamlConstants.Namespaces.Controls};");
			writer.AppendLineIndented($"using {XamlConstants.Namespaces.Data};");
			writer.AppendLineIndented($"using {XamlConstants.Namespaces.Documents};");
			writer.AppendLineIndented($"using {XamlConstants.Namespaces.Media};");
			writer.AppendLineIndented($"using {XamlConstants.Namespaces.MediaAnimation};");
			writer.AppendLineIndented($"using {_defaultNamespace};");
			writer.AppendLineIndented("");

			// If a failure happens here, this means that the _isWasm was not properly set as the DefineConstants msbuild property
			// was not populated. This can happen when the property is set through a target with the "CreateProperty" task, and the
			// Uno.SourceGeneration tasks do not execute this task properly.
			writer.AppendLineIndented("#if __WASM__");
			writer.AppendLineIndented(_isWasm ? "" : "#error invalid internal source generator state. The __WASM__ DefineConstant was not propagated properly.");
			writer.AppendLineIndented("#endif");

			using (writer.BlockInvariant("namespace {0}", _defaultNamespace))
			{
				writer.AppendLineIndented("/// <summary>");
				writer.AppendLineIndented("/// Contains all the static resources defined for the application");
				writer.AppendLineIndented("/// </summary>");

				AnalyzerSuppressionsGenerator.Generate(writer, _analyzerSuppressions);
				using (writer.BlockInvariant("public sealed partial class GlobalStaticResources"))
				{
					writer.AppendLineIndented("static bool _initialized;");
					writer.AppendLineIndented("private static bool _stylesRegistered;");
					writer.AppendLineIndented("private static bool _dictionariesRegistered;");

					using (writer.BlockInvariant("internal static {0} {1} {{get; }} = new {0}()", ParseContextPropertyType, ParseContextPropertyName))
					{
						writer.AppendLineIndented($"AssemblyName = \"{_metadataHelper.AssemblyName}\",");
					}

					writer.AppendLineIndented(";");
					writer.AppendLine();

					using (writer.BlockInvariant("static GlobalStaticResources()"))
					{
						writer.AppendLineIndented("Initialize();");
					}

					using (writer.BlockInvariant("public static void Initialize()"))
					{
						using (writer.BlockInvariant("if (!_initialized)"))
						{
							using (IsUnoAssembly ? writer.BlockInvariant("using (ResourceResolver.WriteInitiateGlobalStaticResourcesEventActivity())") : null)
							{
								writer.AppendLineIndented("_initialized = true;");

								if (_ambientGlobalResources != null)
								{
									foreach (var ambientResource in _ambientGlobalResources)
									{
										if (ambientResource.GetFirstMethodWithName("Initialize") is not null)
										{
											writer.AppendLineIndented($"global::{ambientResource.GetFullName()}.Initialize();");
										}
									}

									foreach (var ambientResource in _ambientGlobalResources)
									{
										// Note: we do *not* call RegisterDefaultStyles for the current assembly, because those styles are treated as implicit styles, not default styles
										if (ambientResource.GetFirstMethodWithName("RegisterDefaultStyles") is not null)
										{
											writer.AppendLineIndented($"global::{ambientResource.GetFullName()}.RegisterDefaultStyles();");
										}
									}

									foreach (var ambientResource in _ambientGlobalResources)
									{
										if (ambientResource.GetFirstMethodWithName("RegisterResourceDictionariesBySource") is not null)
										{
											writer.AppendLineIndented($"global::{ambientResource.GetFullName()}.RegisterResourceDictionariesBySource();");
										}
									}
								}

								if (IsUnoAssembly && _xamlSourceFiles.Any())
								{
									// Build master dictionary
									foreach (var dictProperty in map.GetAllDictionaryProperties())
									{
										writer.AppendLineIndented($"MasterDictionary.MergedDictionaries.Add({dictProperty});");
									}
								}
							}
						}
					}

					using (writer.BlockInvariant("public static void RegisterDefaultStyles()"))
					{
						using (writer.BlockInvariant("if(!_stylesRegistered)"))
						{
							writer.AppendLineIndented("_stylesRegistered = true;");
							foreach (var file in files.Select(f => f.UniqueID).Distinct())
							{
								writer.AppendLineIndented($"RegisterDefaultStyles_{file}();");
							}
						}
					}

					writer.AppendLineIndented("// Register ResourceDictionaries using ms-appx:/// syntax, this is called for external resources");
					using (writer.BlockInvariant("public static void RegisterResourceDictionariesBySource()"))
					{
						using (writer.BlockInvariant("if(!_dictionariesRegistered)"))
						{
							writer.AppendLineIndented("_dictionariesRegistered = true;");

							if (!IsUnoAssembly && !IsUnoFluentAssembly)
							{
								// For third-party libraries, expose all files using standard uri
								foreach (var file in files.Where(IsResourceDictionary))
								{
									var url = "{0}/{1}".InvariantCultureFormat(_metadataHelper.AssemblyName, map.GetSourceLink(file));
									RegisterForXamlFile(file, url);
								}
							}
							else if (files.Any() && IsUnoFluentAssembly)
							{
								// For Uno assembly, we expose WinUI resources using same uri as on Windows
								for (int fluentVersion = 1; fluentVersion <= XamlConstants.MaxFluentResourcesVersion; fluentVersion++)
								{
									RegisterForFile(string.Format(CultureInfo.InvariantCulture, WinUIThemeResourcePathSuffixFormatString, fluentVersion), XamlFilePathHelper.GetWinUIThemeResourceUrl(fluentVersion));
								}
								RegisterForFile(WinUICompactPathSuffix, XamlFilePathHelper.WinUICompactURL);
							}

							void RegisterForFile(string baseFilePath, string url)
							{
								var file = files.FirstOrDefault(f =>
									f.FilePath.Substring(_projectDirectory.Length + 1).Equals(baseFilePath, StringComparison.OrdinalIgnoreCase));

								if (file != null)
								{
									RegisterForXamlFile(file, url);
								}
							}

							void RegisterForXamlFile(XamlFileDefinition file, string url)
							{
								if (file != null)
								{
									writer.AppendLineIndented($"global::Uno.UI.ResourceResolver.RegisterResourceDictionaryBySource(uri: \"ms-appx:///{url}\", context: {ParseContextPropertyName}, dictionary: () => {file.UniqueID}_ResourceDictionary);");
								}
							}
						}
					}

					writer.AppendLineIndented("// Register ResourceDictionaries using ms-resource:/// syntax, this is called for local resources");
					using (writer.BlockInvariant("internal static void RegisterResourceDictionariesBySourceLocal()"))
					{
						foreach (var file in files.Where(IsResourceDictionary))
						{
							// Make ResourceDictionary retrievable by Hot Reload
							var filePath = _isDebug ? $"\"{file.FilePath.Replace("\\", "/")}\"" : "null";

							// We leave context null because local resources should be found through Application.Resources
							writer.AppendLineIndented($"global::Uno.UI.ResourceResolver.RegisterResourceDictionaryBySource(uri: \"{XamlFilePathHelper.LocalResourcePrefix}{map.GetSourceLink(file)}\", context: null, dictionary: () => {file.UniqueID}_ResourceDictionary, {filePath});");
							// Local resources can also be found through the ms-appx:/// prefix
							writer.AppendLineIndented($"global::Uno.UI.ResourceResolver.RegisterResourceDictionaryBySource(uri: \"{XamlFilePathHelper.AppXIdentifier}{map.GetSourceLink(file)}\", context: null, dictionary: () => {file.UniqueID}_ResourceDictionary);");
						}
					}

					if (IsUnoAssembly)
					{
						// Declare master dictionary
						writer.AppendLine();
						writer.AppendLineIndented("internal static ResourceDictionary MasterDictionary {get; } = new ResourceDictionary();");
					}

					// Generate all the partial methods, even if they don't exist. That avoids
					// having to sync the generation of the files with this global table.
					foreach (var file in files.Select(f => f.UniqueID).Distinct())
					{
						writer.AppendLineIndented($"static partial void RegisterDefaultStyles_{file}();");
					}

					writer.AppendLineIndented("[global::System.Obsolete(\"This method is provided for binary backward compatibility. It will always return null.\")]");
					writer.AppendLineIndented("[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]");
					writer.AppendLineIndented("public static object FindResource(string name) => null;");

					writer.AppendLineIndented("");
				}
			}

			return writer.ToString();
		}

		private static bool IsResourceDictionary(XamlFileDefinition fileDefinition) => fileDefinition.Objects.FirstOrDefault()?.Type.Name == "ResourceDictionary";
	}
}
