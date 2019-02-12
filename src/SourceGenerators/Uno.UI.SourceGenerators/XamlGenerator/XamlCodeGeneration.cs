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
using Microsoft.Build.Execution;
using Uno.Logging;

namespace Uno.UI.SourceGenerators.XamlGenerator
{
	internal class XamlCodeGeneration
	{
		private string[] _xamlSourceFiles;
		private string _targetPath;
		private readonly string _defaultLanguage;
		private bool _isWasm;
		private string _defaultNamespace;
		private string[] _assemblySearchPaths;
		private RoslynMetadataHelper _medataHelper;
		private string[] _excludeXamlNamespaces;
		private string[] _includeXamlNamespaces;
		private string[] _analyzerSuppressions;
		private string[] _resourceFiles;
		private Dictionary<string, string[]> _uiAutomationMappings;
		private readonly ProjectInstance _projectInstance;
		private readonly string _configuration;
		private readonly bool _isDebug;
		private readonly bool _outputSourceComments = true;

		private static DateTime _buildTasksBuildDate = File.GetLastWriteTime(new Uri(typeof(XamlFileGenerator).Assembly.CodeBase).LocalPath);
		private INamedTypeSymbol[] _ambientGlobalResources;
		private readonly bool _isUiAutomationMappingEnabled;
		private Dictionary<string, string> _legacyTypes;

#pragma warning disable 649 // Unused member
		private readonly bool _forceGeneration;
#pragma warning restore 649 // Unused member

		public XamlCodeGeneration(Compilation sourceCompilation, ProjectInstance msbProject, Project roslynProject)
		{
			_legacyTypes = msbProject
				.GetItems("LegacyTypes")
				.Select(i => i.EvaluatedInclude)
				.ToList()
				.ToDictionary(fullyQualifiedName => fullyQualifiedName.Split('.').Last());

			_medataHelper = new RoslynMetadataHelper("Debug", sourceCompilation, msbProject, roslynProject, null, _legacyTypes);
			_assemblySearchPaths = new string[0];
			_projectInstance = msbProject;

			_configuration = msbProject.GetProperty("Configuration")?.EvaluatedValue
				?? throw new InvalidOperationException("The configuration property must be provided");

			_isDebug = string.Equals(_configuration, "Debug", StringComparison.OrdinalIgnoreCase);

			var xamlPages = msbProject.GetItems("Page")
				.Select(d => d.EvaluatedInclude);

			_xamlSourceFiles = msbProject.GetItems("ApplicationDefinition")
				.Select(d => d.EvaluatedInclude)
				.Concat(xamlPages)
				.ToArray();

			_excludeXamlNamespaces = msbProject
				.GetItems("ExcludeXamlNamespaces")
				.Select(i => i.EvaluatedInclude)
				.ToArray();

			_includeXamlNamespaces = msbProject
				.GetItems("IncludeXamlNamespaces")
				.Select(i => i.EvaluatedInclude)
				.ToArray();

			_analyzerSuppressions = msbProject
				.GetItems("XamlGeneratorAnalyzerSuppressions")
				.Select(i => i.EvaluatedInclude)
				.ToArray();

			_resourceFiles = msbProject
				.GetItems("PRIResource")
				.Select(i => i.EvaluatedInclude)
				.ToArray();

			if(bool.TryParse(msbProject.GetProperty("UseUnoXamlParser")?.EvaluatedValue, out var useUnoXamlParser) && useUnoXamlParser)
			{
				XamlRedirection.XamlConfig.IsUnoXaml = useUnoXamlParser || XamlRedirection.XamlConfig.IsMono;
			}

			if (bool.TryParse(msbProject.GetProperty("ShouldWriteErrorOnInvalidXaml")?.EvaluatedValue, out var shouldWriteErrorOnInvalidXaml))
			{
				XamlFileGenerator.ShouldWriteErrorOnInvalidXaml = shouldWriteErrorOnInvalidXaml;
			}

			if (!bool.TryParse(msbProject.GetProperty("IsUiAutomationMappingEnabled")?.EvaluatedValue ?? "", out _isUiAutomationMappingEnabled))
			{
				_isUiAutomationMappingEnabled = false;
			}

			_targetPath = Path.Combine(
				Path.GetDirectoryName(msbProject.FullPath),
				msbProject.GetProperty("IntermediateOutputPath").EvaluatedValue
			);

			_defaultLanguage = msbProject.GetProperty("DefaultLanguage")?.EvaluatedValue;

			_analyzerSuppressions = msbProject
				.GetItems("XamlGeneratorAnalyzerSuppressions")
				.Select(i => i.EvaluatedInclude)
				.ToArray();

			_uiAutomationMappings = msbProject
				.GetItems("CustomUiAutomationMemberMapping")
				.Select(i => new
				{
					Key = i.EvaluatedInclude,
					Value = i.MetadataNames
						.Select(i.GetMetadataValue)
						.FirstOrDefault()
						?.Split()
						.Select(m => m.Trim())
						.Where(m => m.HasValueTrimmed())
				})
				.GroupBy(p => p.Key)
				.ToDictionary(p => p.Key, p => p.SelectMany(x => x.Value.Safe()).ToArray());

			_defaultNamespace = msbProject.GetPropertyValue("RootNamespace");

			_isWasm = msbProject.GetProperty("DefineConstants").EvaluatedValue?.Contains("__WASM__") ?? false;
		}

		public KeyValuePair<string, string>[] Generate()
		{
			this.Log().InfoFormat("Xaml Source Generation is using the {0} Xaml Parser", XamlRedirection.XamlConfig.IsUnoXaml ? "Uno.UI" : "System");

			var lastBinaryUpdateTime = _forceGeneration ? DateTime.MaxValue : GetLastBinaryUpdateTime();

			var resourceKeys = GetResourceKeys();
			var files = new XamlFileParser(_excludeXamlNamespaces, _includeXamlNamespaces).ParseFiles(_xamlSourceFiles);

			var globalStaticResourcesMap = BuildAssemblyGlobalStaticResourcesMap(files);

			var filesQuery = files
				.ToArray();

			var outputFiles = filesQuery
#if !DEBUG
				.AsParallel()
#endif
				.Select(file => new KeyValuePair<string, string>(
						file.UniqueID,
						new XamlFileGenerator(
							file: file,
							targetPath: _targetPath,
							defaultNamespace: _defaultNamespace,
							medataHelper: _medataHelper,
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
							isDebug: _isDebug
						)
						.GenerateFile()
					)
				)
				.ToList();


			outputFiles.Add(new KeyValuePair<string, string>("GlobalStaticResources", GenerateGlobalResources(files)));

			return outputFiles.ToArray();
		}

		private XamlGlobalStaticResourcesMap BuildAssemblyGlobalStaticResourcesMap(XamlFileDefinition[] files)
		{
			var map = new XamlGlobalStaticResourcesMap();

			BuildLocalProjectResources(files, map);
			BuildAmbientResources(files, map);

			return map;
		}

		private void BuildAmbientResources(XamlFileDefinition[] files, XamlGlobalStaticResourcesMap map)
		{
			// Lookup for GlobalStaticResources classes in external assembly
			// references only, and in Uno.UI itself for generic.xaml-like resources.

			var query = from ext in _medataHelper.Compilation.ExternalReferences
					let sym = _medataHelper.Compilation.GetAssemblyOrModuleSymbol(ext) as IAssemblySymbol
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
					var resources = new Dictionary<string, XamlObjectDefinition>();

					BuildResourceMap(topLevelControl, map);

					var themeResources = topLevelControl.Members.FirstOrDefault(m => m.Member.Name == "ThemeDictionaries");

					if (themeResources != null)
					{
						// Theme resources are not supported for now, so we take the default key
						// and consider everthing inside as a standard StaticResource.

						var defaultTheme = themeResources
							.Objects
							.FirstOrDefault(o => o
								.Members
								.Any(m =>
									m.Member.Name == "Key"
									&& m.Value.ToString() == "Default"
								)
							);

						if (defaultTheme != null)
						{
							BuildResourceMap(defaultTheme, map);
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
						&& resource.Type.Name != "StaticResource"
					)
					{
						map.Add(key.Value.ToString(), _defaultNamespace, XamlGlobalStaticResourcesMap.ResourcePrecedence.Local);
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
					XmlDocument doc = new XmlDocument();
					doc.Load(file);
					XmlNode root = doc.DocumentElement;

					//extract all localization keys from Win10 resource file
					resourceKeys = resourceKeys
						.Concat(doc
							.SelectNodes("//data")
							.Cast<XmlElement>()
							.Select(node => node.GetAttribute("name"))
							.ToArray()
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

		private string GenerateGlobalResources(IEnumerable<XamlFileDefinition> files)
		{
			var outputFile = Path.Combine(_targetPath, "GlobalStaticResources.g.cs");

			var writer = new IndentedStringBuilder();

			writer.AppendLineInvariant("// <autogenerated />");
			writer.AppendLineInvariant("#pragma warning disable 618 // Ignore obsolete members warnings");
			writer.AppendLineInvariant("#pragma warning disable 105 // Ignore duplicate namespaces");
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

			using (writer.BlockInvariant("namespace {0}", _defaultNamespace))
			{
				writer.AppendLineInvariant("/// <summary>");
				writer.AppendLineInvariant("/// Contains all the static resources defined for the application");
				writer.AppendLineInvariant("/// </summary>");

				AnalyzerSuppressionsGenerator.Generate(writer, _analyzerSuppressions);
				using (writer.BlockInvariant("public sealed partial class GlobalStaticResources"))
				{
					writer.AppendLineInvariant("static bool _initialized;");

					using (writer.BlockInvariant("static GlobalStaticResources()"))
					{
						writer.AppendLineInvariant("Initialize();");
					}

					using (writer.BlockInvariant("public static void Initialize()"))
					{
						using (writer.BlockInvariant("if(!_initialized)"))
						{
							writer.AppendLineInvariant("_initialized = true;");

							foreach (var ambientResource in _ambientGlobalResources)
							{
								if (ambientResource.GetMethods().Any(m => m.Name == "Initialize"))
								{
									writer.AppendLineInvariant("global::{0}.Initialize();", ambientResource.GetFullName());
								}

								writer.AppendLineInvariant("AddResolver(global::{0}.FindResource);", ambientResource.GetFullName());
							}

							foreach (var file in files)
							{
								writer.AppendLineInvariant("RegisterResources_{0}();", file.UniqueID);
								writer.AppendLineInvariant("RegisterImplicitStylesResources_{0}();", file.UniqueID);
							}
						}
					}

					// Generate all the partial methods, even if they don't exist. That avoids
					// having to sync the generation of the files with this global table.
					foreach (var file in files)
					{
						writer.AppendLineInvariant("static partial void RegisterResources_{0}();", file.UniqueID);
						writer.AppendLineInvariant("static partial void RegisterImplicitStylesResources_{0}();", file.UniqueID);
					}

					writer.AppendLineInvariant("");

					writer.AppendLineInvariant("/// <summary>");
					writer.AppendLineInvariant("/// Finds a resource instance in the Global Static Resources");
					writer.AppendLineInvariant("/// </summary>");
					writer.AppendLineInvariant("/// <param name=\"name\">The name of the resource</param>");
					writer.AppendLineInvariant("/// <returns>The instance of the resources, otherwise null.</returns>");
					using (writer.BlockInvariant("public static object FindResource(string name)"))
					{
						using (writer.BlockInvariant("foreach(var resolver in _resolvers)"))
						{
							writer.AppendLineInvariant("var resource = resolver(name);");
							writer.AppendLineInvariant("if(resource != null){{ return resource; }}");
						}
						writer.AppendLineInvariant("return null;");
					}

					writer.AppendLineInvariant("");

					writer.AppendLineInvariant("private static List<Func<string, object>> _resolvers = new List<Func<string, object>>();");

					using (writer.BlockInvariant("private static void AddResolver(Func<string, object> resolver)"))
					{
						writer.AppendLineInvariant("_resolvers.Add(resolver);");
					}
				}
			}

			return writer.ToString();
		}
	}
}
