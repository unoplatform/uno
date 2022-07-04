#nullable enable

using Uno.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Microsoft.CodeAnalysis;
using Uno.Roslyn;
using Uno.UI.SourceGenerators.XamlGenerator;
using Uno.UI.SourceGenerators.Helpers;
using System.Xml;

#if NETFRAMEWORK
using Uno.SourceGeneration;
#endif

namespace Uno.UI.SourceGenerators.BindableTypeProviders
{
#if NETFRAMEWORK
	[GenerateAfter("Uno.ImmutableGenerator")]
#endif
	[Generator]
	public class DependencyObjectAvailabilityGenerator : ISourceGenerator
	{
		public void Initialize(GeneratorInitializationContext context)
		{
			DependenciesInitializer.Init();
		}

		public void Execute(GeneratorExecutionContext context)
		{
			new Generator().Generate(context);
		}

		class Generator
		{
			private string? _defaultNamespace;
			private string? _projectFullPath;
			private string? _projectDirectory;
			private string? _baseIntermediateOutputPath;
			private string? _intermediatePath;
			private string? _assemblyName;
			private INamedTypeSymbol? _dependencyObjectSymbol;
			private IReadOnlyDictionary<string, INamedTypeSymbol[]>? _namedSymbolsLookup;
			private bool _xamlResourcesTrimming;

			internal void Generate(GeneratorExecutionContext context)
			{
				try
				{
					var validPlatform = PlatformHelper.IsValidPlatform(context);
					var isDesignTime = DesignTimeHelper.IsDesignTime(context);
					var isApplication = PlatformHelper.IsApplication(context);

					if (!bool.TryParse(context.GetMSBuildPropertyValue("UnoXamlResourcesTrimming"), out _xamlResourcesTrimming))
					{
						_xamlResourcesTrimming = false;
					}

					if (validPlatform && _xamlResourcesTrimming)
					{
						_defaultNamespace = context.GetMSBuildPropertyValue("RootNamespace");

						_projectFullPath = context.GetMSBuildPropertyValue("MSBuildProjectFullPath");
						_projectDirectory = Path.GetDirectoryName(_projectFullPath)
							?? throw new InvalidOperationException($"MSBuild property MSBuildProjectFullPath value {_projectFullPath} is not valid");

						_baseIntermediateOutputPath = context.GetMSBuildPropertyValue("BaseIntermediateOutputPath");
						_intermediatePath = Path.Combine(
							_projectDirectory,
							_baseIntermediateOutputPath
						);
						_assemblyName = context.GetMSBuildPropertyValue("AssemblyName");
						_namedSymbolsLookup = context.Compilation.GetSymbolNameLookup();
						_dependencyObjectSymbol = context.Compilation.GetTypeByMetadataName("Windows.UI.Xaml.DependencyObject");
						var modules = from ext in context.Compilation.ExternalReferences
									  let sym = context.Compilation.GetAssemblyOrModuleSymbol(ext) as IAssemblySymbol
									  where sym != null
									  from module in sym.Modules
									  select module;

						modules = modules.Concat(context.Compilation.SourceModule);

						var bindableTypes = from module in modules
											from type in module.GlobalNamespace.GetNamespaceTypes()
											where (
												(
													type.GetAllInterfaces().Any(i => SymbolEqualityComparer.Default.Equals(i, _dependencyObjectSymbol))
												)
											)
											select type;

						bindableTypes = bindableTypes.ToArray();

						context.AddSource("DependencyObjectAvailability", GenerateTypeProviders(bindableTypes));

						GenerateLinkerSubstitutionDefinition(bindableTypes, isApplication);
					}
				}
				catch (OperationCanceledException)
				{
					throw;
				}
				catch (Exception e)
				{
					string? message = e.Message + e.StackTrace;

					if (e is AggregateException)
					{
						message = (e as AggregateException)?.InnerExceptions.Select(ex => ex.Message + e.StackTrace).JoinBy("\r\n");
					}

#if NETSTANDARD
					var diagnostic = Diagnostic.Create(
						XamlCodeGenerationDiagnostics.GenericXamlErrorRule,
						null,
						$"Failed to generate dependency objects. ({e.Message})");

					context.ReportDiagnostic(diagnostic);
#else
					Console.WriteLine("Failed to generate type providers.", new Exception("Failed to generate type providers." + message, e));
#endif
				}
			}

			private void GenerateLinkerSubstitutionDefinition(IEnumerable<INamedTypeSymbol> bindableTypes, bool isApplication)
			{
				// <linker>
				//   <assembly fullname="Uno.UI">
				// 	<type fullname="Uno.UI.GlobalStaticResources">
				// 	  <method signature="System.Void Initialize()" body="remove" />
				// 	  <method signature="System.Void RegisterDefaultStyles()" body="remove" />
				// 	</type>
				//   </assembly>
				// </linker>
				var doc = new XmlDocument();

				var xmlDeclaration = doc.CreateXmlDeclaration("1.0", "UTF-8", null);

				var root = doc.DocumentElement;
				doc.InsertBefore(xmlDeclaration, root);

				var linkerNode = doc.CreateElement(string.Empty, "linker", string.Empty);
				doc.AppendChild(linkerNode);

				var assemblyNode = doc.CreateElement(string.Empty, "assembly", string.Empty);
				assemblyNode.SetAttribute("fullname", _assemblyName);
				linkerNode.AppendChild(assemblyNode);


				//
				// Linker hints features from dependency objects
				//
				var typeNode = doc.CreateElement(string.Empty, "type", string.Empty);
				typeNode.SetAttribute("fullname", LinkerHintsHelpers.GetLinkerHintsClassName(_defaultNamespace));
				assemblyNode.AppendChild(typeNode);

				foreach (var type in bindableTypes)
				{
					var propertyName = LinkerHintsHelpers.GetPropertyAvailableName(type.GetFullMetadataName());

					var methodNode = doc.CreateElement(string.Empty, "method", string.Empty);
					methodNode.SetAttribute("signature", $"System.Boolean get_{propertyName}()");
					methodNode.SetAttribute("body", "stub");
					methodNode.SetAttribute("value", "false");
					methodNode.SetAttribute("feature", propertyName);
					methodNode.SetAttribute("featurevalue", "false");
					typeNode.AppendChild(methodNode);
				}

				var fileName = Path.Combine(_intermediatePath, "Substitutions", "DependencyObjectHints.Substitutions.xml");
				Directory.CreateDirectory(Path.GetDirectoryName(fileName));

				doc.Save(fileName);
			}

			private string GenerateTypeProviders(IEnumerable<INamedTypeSymbol> bindableTypes)
			{
				var writer = new IndentedStringBuilder();

				writer.AppendLineInvariant("// <auto-generated>");
				writer.AppendLineInvariant("// *****************************************************************************");
				writer.AppendLineInvariant("// This file has been generated by Uno.UI (BindableTypeProvidersSourceGenerator)");
				writer.AppendLineInvariant("// *****************************************************************************");
				writer.AppendLineInvariant("// </auto-generated>");
				writer.AppendLine();
				writer.AppendLineInvariant("#pragma warning disable 618  // Ignore obsolete members warnings");
				writer.AppendLineInvariant("#pragma warning disable 1591 // Ignore missing XML comment warnings");
				writer.AppendLineInvariant("using System;");
				writer.AppendLineInvariant("using System.Linq;");
				writer.AppendLineInvariant("using System.Diagnostics;");

				writer.AppendLineInvariant($"// _intermediatePath: {_intermediatePath}");
				writer.AppendLineInvariant($"// _baseIntermediateOutputPath: {_baseIntermediateOutputPath}");

				using (writer.BlockInvariant("namespace {0}", _defaultNamespace))
				{
					using (writer.BlockInvariant("internal class " + LinkerHintsHelpers.GetLinkerHintsClassName()))
					{
						foreach(var type in bindableTypes)
						{
							var safeTypeName = LinkerHintsHelpers.GetPropertyAvailableName(type.GetFullMetadataName());

							writer.AppendLineInvariant($"internal static bool {safeTypeName} => true;");
						}
					}
				}

				return writer.ToString();
			}
		}
	}
}
