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
using Uno.Logging;
using Uno.UI.SourceGenerators.Telemetry;
using Uno.UI.Xaml;
using System.Drawing;

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

		private readonly string[] _xamlSourceFiles;
		private readonly string[] _xamlSourceLinks;
		private readonly string _targetPath;
		private readonly string _defaultLanguage;
		private readonly bool _isWasm;
		private readonly bool _isDesignTimeBuild;
		private readonly string _defaultNamespace;
		private readonly string[] _assemblySearchPaths;
		private readonly string[] _excludeXamlNamespaces;
		private readonly string[] _includeXamlNamespaces;
		private readonly string[] _analyzerSuppressions;
		private readonly string[] _resourceFiles;
		private readonly Dictionary<string, string[]> _uiAutomationMappings;
		private readonly string _configuration;
		private readonly bool _isDebug;
		private readonly string _projectDirectory;
		private readonly string _projectFullPath;
		private readonly bool _outputSourceComments = true;
		private readonly RoslynMetadataHelper _metadataHelper;

		/// <summary>
		/// If set, code generated from XAML will be annotated with the source method and line # in XamlFileGenerator, for easier debugging.
		/// </summary>
		private readonly bool _shouldAnnotateGeneratedXaml = false;

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
		private bool _skipUserControlsInVisualTree = false;
		private readonly GeneratorExecutionContext _generatorContext;

		private bool IsUnoAssembly => _defaultNamespace == "Uno.UI";
		private bool IsUnoFluentAssembly => _defaultNamespace == "Uno.UI.FluentTheme";

		/// <summary>
		/// Resource files that should be initialized first, in given order, because other resource declarations depend on them.
		/// </summary>
		private static readonly string[] _baseResourceDependencies = new[]
		{
			"SystemResources.xaml",
			"Generic.xaml",
			"Generic.Native.xaml",
		};

		private const string WinUIThemeResourcePathSuffix = "themeresources.xaml";
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

			_metadataHelper = new RoslynMetadataHelper("Debug", context, _legacyTypes);
			_assemblySearchPaths = new string[0];

			_configuration = context.GetMSBuildPropertyValue("Configuration")
				?? throw new InvalidOperationException("The configuration property must be provided");

			_isDebug = string.Equals(_configuration, "Debug", StringComparison.OrdinalIgnoreCase);

			_projectFullPath = context.GetMSBuildPropertyValue("MSBuildProjectFullPath");
			_projectDirectory = Path.GetDirectoryName(_projectFullPath)
				?? throw new InvalidOperationException($"MSBuild property MSBuildProjectFullPath value {_projectFullPath} is not valid");

			var xamlItems = context.GetMSBuildItems("Page")
				.Concat(context.GetMSBuildItems("ApplicationDefinition"));

			_xamlSourceFiles = xamlItems.Select(i => i.Identity).ToArray();

			_xamlSourceLinks = xamlItems.Select(GetSourceLink)
				.ToArray();

			_excludeXamlNamespaces = context.GetMSBuildPropertyValue("ExcludeXamlNamespacesProperty").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

			_includeXamlNamespaces = context.GetMSBuildPropertyValue("IncludeXamlNamespacesProperty").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

			_analyzerSuppressions = context.GetMSBuildPropertyValue("XamlGeneratorAnalyzerSuppressionsProperty").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

			_resourceFiles = context.GetMSBuildItems("PRIResource").Select(i => i.Identity).ToArray();

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
				XamlFileGenerator.ShouldWriteErrorOnInvalidXaml = shouldWriteErrorOnInvalidXaml;
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

			_targetPath = Path.Combine(
				_projectDirectory,
				context.GetMSBuildPropertyValue("IntermediateOutputPath")
			);

			_defaultLanguage = context.GetMSBuildPropertyValue("DefaultLanguage");

			_analyzerSuppressions = context.GetMSBuildItems("XamlGeneratorAnalyzerSuppressions").Select(i => i.Identity).ToArray();

			_uiAutomationMappings = context.GetMSBuildItems("CustomUiAutomationMemberMappingAdjusted")
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

		/// <summary>
		/// Get the file location as seen in the IDE, used for ResourceDictionary.Source resolution.
		/// </summary>
		private string GetSourceLink(MSBuildItem projectItemInstance)
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

		public KeyValuePair<string, string>[] Generate()
		{
			var stopwatch = Stopwatch.StartNew();

			try
			{
				this.Log().InfoFormat("Xaml Source Generation is using the {0} Xaml Parser", XamlRedirection.XamlConfig.IsUnoXaml ? "Uno.UI" : "System");

				var lastBinaryUpdateTime = _forceGeneration ? DateTime.MaxValue : GetLastBinaryUpdateTime();

				var resourceKeys = GetResourceKeys();
				var filesFull = new XamlFileParser(_excludeXamlNamespaces, _includeXamlNamespaces, _metadataHelper).ParseFiles(_xamlSourceFiles);
				var files = filesFull
					.Trim()
					.OrderBy(f => f.UniqueID)
					.ToArray();

				for (int i = 0; i < files.Length; i++)
				{
					files[i].ShortId = i;
				}

				TrackStartGeneration(files);


				var globalStaticResourcesMap = BuildAssemblyGlobalStaticResourcesMap(files, filesFull, _xamlSourceLinks);

				var filesQuery = files
					.ToArray();

				var filesToProcess = filesQuery.AsParallel();

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
									outputSourceComments: _outputSourceComments,
									resourceKeys: resourceKeys,
									isUiAutomationMappingEnabled: _isUiAutomationMappingEnabled,
									uiAutomationMappings: _uiAutomationMappings,
									defaultLanguage: _defaultLanguage,
									isWasm: _isWasm,
									isDebug: _isDebug,
									skipUserControlsInVisualTree: _skipUserControlsInVisualTree,
									shouldAnnotateGeneratedXaml: _shouldAnnotateGeneratedXaml,
									isUnoAssembly: IsUnoAssembly,
									isLazyVisualStateManagerEnabled: _isLazyVisualStateManagerEnabled
								)
								.GenerateFile()
						)
					)
					.ToList();


				outputFiles.Add(new KeyValuePair<string, string>("GlobalStaticResources", GenerateGlobalResources(files, globalStaticResourcesMap)));

				TrackGenerationDone(stopwatch.Elapsed);

				return outputFiles.ToArray();

			}
			catch (Exception e)
			{
				TrackGenerationFailed(e, stopwatch.Elapsed);

				throw;
			}
			finally
			{
				_telemetry.Flush();
				_telemetry.Dispose();
			}
		}

		private XamlGlobalStaticResourcesMap BuildAssemblyGlobalStaticResourcesMap(XamlFileDefinition[] files, XamlFileDefinition[] filesFull, string[] links)
		{
			var map = new XamlGlobalStaticResourcesMap();

			BuildLocalProjectResources(files, map);
			BuildAmbientResources(files, map);
			map.BuildResourceDictionaryMap(filesFull, links);

			return map;
		}

		private void BuildAmbientResources(XamlFileDefinition[] files, XamlGlobalStaticResourcesMap map)
		{
			// Lookup for GlobalStaticResources classes in external assembly
			// references only, and in Uno.UI itself for generic.xaml-like resources.

			var query = from ext in _metadataHelper.Compilation.ExternalReferences
						let sym = _metadataHelper.Compilation.GetAssemblyOrModuleSymbol(ext) as IAssemblySymbol
						where sym != null
						from module in sym.Modules
						from reference in module.ReferencedAssemblies
						where reference.Name == "Uno.UI" || sym.Name == "Uno.UI"
						from typeName in sym.GlobalNamespace.GetNamespaceTypes()
						where typeName.Name.EndsWith("GlobalStaticResources")
						select typeName;

			_ambientGlobalResources = query.Distinct().ToArray();

			foreach (var ambientResources in _ambientGlobalResources)
			{
				var publicProperties = from member in ambientResources.GetAllProperties()
									   where member.DeclaredAccessibility == Microsoft.CodeAnalysis.Accessibility.Public
									   select member;


				foreach (var member in publicProperties)
				{
					map.Add(member.Name, ambientResources.ContainingNamespace.ToDisplayString(), XamlGlobalStaticResourcesMap.ResourcePrecedence.System);
				}
			}
		}

		private void BuildLocalProjectResources(XamlFileDefinition[] files, XamlGlobalStaticResourcesMap map)
		{
			foreach (var file in files)
			{
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
		private string[] GetResourceKeys()
		{
			string[] resourceKeys = new string[0];

			if (_resourceFiles != null)
			{
				foreach (var file in _resourceFiles)
				{
					this.Log().Info("Parse resource file : " + file);

					//load document
					var doc = new XmlDocument();
					doc.Load(file);

					//extract all localization keys from Win10 resource file
					resourceKeys = resourceKeys
						.Concat(doc
							.SelectNodes("//data")
							?.Cast<XmlElement>()
							.Select(node => node.GetAttribute("name"))
							.ToArray() ?? Array.Empty<string>()
						)
						.Distinct()
						.Select(k => k.Replace(".", "/"))
						.ToArray();
				}
			}

			this.Log().Info(resourceKeys.Count() + " localization keys found");
			return resourceKeys;
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

			writer.AppendLineInvariant("// <autogenerated />");
			writer.AppendLineInvariant("#pragma warning disable 618  // Ignore obsolete members warnings");
			writer.AppendLineInvariant("#pragma warning disable 105  // Ignore duplicate namespaces");
			writer.AppendLineInvariant("#pragma warning disable 1591 // Ignore missing XML comment warnings");
			writer.AppendLineInvariant("using System;");
			writer.AppendLineInvariant("using System.Linq;");
			writer.AppendLineInvariant("using System.Collections.Generic;");
			writer.AppendLineInvariant("using Uno.Extensions;");
			writer.AppendLineInvariant("using Uno;");
			writer.AppendLineInvariant("using System.Diagnostics;");

			//TODO Determine the list of namespaces to use
			writer.AppendLineInvariant($"using {XamlConstants.BaseXamlNamespace};");
			writer.AppendLineInvariant($"using {XamlConstants.Namespaces.Controls};");
			writer.AppendLineInvariant($"using {XamlConstants.Namespaces.Data};");
			writer.AppendLineInvariant($"using {XamlConstants.Namespaces.Documents};");
			writer.AppendLineInvariant($"using {XamlConstants.Namespaces.Media};");
			writer.AppendLineInvariant($"using {XamlConstants.Namespaces.MediaAnimation};");
			writer.AppendLineInvariant("using {0};", _defaultNamespace);
			writer.AppendLineInvariant("");

			// If a failure happens here, this means that the _isWasm was not properly set as the DefineConstants msbuild property
			// was not populated. This can happen when the property is set through a target with the "CreateProperty" task, and the
			// Uno.SourceGeneration tasks do not execute this task properly.
			writer.AppendLineInvariant("#if __WASM__");
			writer.AppendLineInvariant(_isWasm ? "" : "#error invalid internal source generator state. The __WASM__ DefineConstant was not propagated properly.");
			writer.AppendLineInvariant("#endif");

			using (writer.BlockInvariant("namespace {0}", _defaultNamespace))
			{
				writer.AppendLineInvariant("/// <summary>");
				writer.AppendLineInvariant("/// Contains all the static resources defined for the application");
				writer.AppendLineInvariant("/// </summary>");

				AnalyzerSuppressionsGenerator.Generate(writer, _analyzerSuppressions);
				using (writer.BlockInvariant("public sealed partial class GlobalStaticResources"))
				{
					writer.AppendLineInvariant("static bool _initialized;");
					writer.AppendLineInvariant("private static bool _stylesRegistered;");
					writer.AppendLineInvariant("private static bool _dictionariesRegistered;");

					using (writer.BlockInvariant("internal static {0} {1} {{get; }} = new {0}()", ParseContextPropertyType, ParseContextPropertyName))
					{
						writer.AppendLineInvariant("AssemblyName = \"{0}\",", _metadataHelper.AssemblyName);
					}

					writer.AppendLineInvariant(";");
					writer.AppendLine();

					using (writer.BlockInvariant("static GlobalStaticResources()"))
					{
						writer.AppendLineInvariant("Initialize();");
					}

					using (writer.BlockInvariant("public static void Initialize()"))
					{
						using (writer.BlockInvariant("if (!_initialized)"))
						{
							using (IsUnoAssembly ? writer.BlockInvariant("using (ResourceResolver.WriteInitiateGlobalStaticResourcesEventActivity())") : null)
							{
								writer.AppendLineInvariant("_initialized = true;");

								if (_ambientGlobalResources != null)
								{
									foreach (var ambientResource in _ambientGlobalResources)
									{
										if (ambientResource.GetMethods().Any(m => m.Name == "Initialize"))
										{
											writer.AppendLineInvariant("global::{0}.Initialize();", ambientResource.GetFullName());
										}
									}

									foreach (var ambientResource in _ambientGlobalResources)
									{
										// Note: we do *not* call RegisterDefaultStyles for the current assembly, because those styles are treated as implicit styles, not default styles
										if (ambientResource.GetMethods().Any(m => m.Name == "RegisterDefaultStyles"))
										{
											writer.AppendLineInvariant("global::{0}.RegisterDefaultStyles();", ambientResource.GetFullName());
										}
									}

									foreach (var ambientResource in _ambientGlobalResources)
									{
										if (ambientResource.GetMethods().Any(m => m.Name == "RegisterResourceDictionariesBySource"))
										{
											writer.AppendLineInvariant("global::{0}.RegisterResourceDictionariesBySource();", ambientResource.GetFullName());
										}
									}
								}

								if (IsUnoAssembly && _xamlSourceFiles.Any())
								{
									// Build master dictionary
									foreach (var dictProperty in map.GetAllDictionaryProperties(_baseResourceDependencies))
									{
										writer.AppendLineInvariant("MasterDictionary.MergedDictionaries.Add({0});", dictProperty);
									}
								}
							}
						}
					}

					using (writer.BlockInvariant("public static void RegisterDefaultStyles()"))
					{
						using (writer.BlockInvariant("if(!_stylesRegistered)"))
						{
							writer.AppendLineInvariant("_stylesRegistered = true;");
							foreach (var file in files.Select(f => f.UniqueID).Distinct())
							{
								writer.AppendLineInvariant("RegisterDefaultStyles_{0}();", file);
							}
						}
					}

					writer.AppendLineInvariant("// Register ResourceDictionaries using ms-appx:/// syntax, this is called for external resources");
					using (writer.BlockInvariant("public static void RegisterResourceDictionariesBySource()"))
					{
						using (writer.BlockInvariant("if(!_dictionariesRegistered)"))
						{
							writer.AppendLineInvariant("_dictionariesRegistered = true;");

							if(!IsUnoAssembly && !IsUnoFluentAssembly)
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
								RegisterForFile(WinUIThemeResourcePathSuffix, XamlFilePathHelper.WinUIThemeResourceURL);
								RegisterForFile(WinUICompactPathSuffix, XamlFilePathHelper.WinUICompactURL);
							}

							void RegisterForFile(string baseFilePath, string url)
							{
								var file = files.FirstOrDefault(f =>
									f.FilePath.Substring(_projectDirectory.Length+1).Equals(baseFilePath, StringComparison.OrdinalIgnoreCase));

								if (file != null)
								{
									RegisterForXamlFile(file, url);
								}
							}

							void RegisterForXamlFile(XamlFileDefinition file, string url)
							{
								if (file != null)
								{
									writer.AppendLineInvariant("global::Uno.UI.ResourceResolver.RegisterResourceDictionaryBySource(uri: \"ms-appx:///{0}\", context: {1}, dictionary: () => {2}_ResourceDictionary);",
										url,
										ParseContextPropertyName,
										file.UniqueID
										);
								}
							}
						}
					}

					writer.AppendLineInvariant("// Register ResourceDictionaries using ms-resource:/// syntax, this is called for local resources");
					using (writer.BlockInvariant("internal static void RegisterResourceDictionariesBySourceLocal()"))
					{
						foreach (var file in files.Where(IsResourceDictionary))
						{
							// We leave context null because local resources should be found through Application.Resources
							writer.AppendLineInvariant("global::Uno.UI.ResourceResolver.RegisterResourceDictionaryBySource(uri: \"{0}{1}\", context: null, dictionary: () => {2}_ResourceDictionary);",
								XamlFilePathHelper.LocalResourcePrefix,
								map.GetSourceLink(file),
								file.UniqueID
							);
							// Local resources can also be found through the ms-appx:/// prefix
							writer.AppendLineInvariant("global::Uno.UI.ResourceResolver.RegisterResourceDictionaryBySource(uri: \"{0}{1}\", context: null, dictionary: () => {2}_ResourceDictionary);",
								XamlFilePathHelper.AppXIdentifier,
								map.GetSourceLink(file),
								file.UniqueID
							);
						}
					}

					if (IsUnoAssembly)
					{
						// Declare master dictionary
						writer.AppendLine();
						writer.AppendLineInvariant("internal static ResourceDictionary MasterDictionary {{get; }} = new ResourceDictionary();");
					}

					// Generate all the partial methods, even if they don't exist. That avoids
					// having to sync the generation of the files with this global table.
					foreach (var file in files.Select(f=>f.UniqueID).Distinct())
					{
						writer.AppendLineInvariant("static partial void RegisterDefaultStyles_{0}();", file);
					}

					writer.AppendLineInvariant("[global::System.Obsolete(\"This method is provided for binary backward compatibility. It will always return null.\")]");
					writer.AppendLineInvariant("[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]");
					writer.AppendLineInvariant("public static object FindResource(string name) => null;");

					writer.AppendLineInvariant("");
				}
			}

			return writer.ToString();
		}

		private bool IsResourceDictionary(XamlFileDefinition fileDefinition) => fileDefinition.Objects.FirstOrDefault()?.Type.Name == "ResourceDictionary";
	}
}
