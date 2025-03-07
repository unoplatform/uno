#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using Microsoft.CodeAnalysis;
using Uno.Extensions;
using Uno.Roslyn;
using Uno.UI.SourceGenerators.Helpers;
using Uno.UI.SourceGenerators.Utils;
using Uno.UI.SourceGenerators.XamlGenerator;

namespace Uno.UI.SourceGenerators.BindableTypeProviders
{
	[Generator]
	public class DependencyObjectAvailabilityGenerator : ISourceGenerator
	{
		public void Initialize(GeneratorInitializationContext context)
		{
		}

		public void Execute(GeneratorExecutionContext context)
		{
			new Generator().Generate(context);
		}

		private sealed class Generator
		{
			private string? _defaultNamespace;
			private string? _projectFullPath;
			private string? _projectDirectory;
			private string? _intermediateOutputPath;
			private string? _intermediatePath;
			private string? _assemblyName;
			private INamedTypeSymbol? _dependencyObjectSymbol;
			private INamedTypeSymbol? _additionalLinkerHintAttributeSymbol;
			private bool _xamlResourcesTrimming;
			private bool _isUnoUISolution;

			internal void Generate(GeneratorExecutionContext context)
			{
				try
				{
					var validPlatform = PlatformHelper.IsValidPlatform(context);

					if (!bool.TryParse(context.GetMSBuildPropertyValue("UnoXamlResourcesTrimming"), out _xamlResourcesTrimming))
					{
						_xamlResourcesTrimming = false;
					}

					if (!bool.TryParse(context.GetMSBuildPropertyValue("_IsUnoUISolution"), out _isUnoUISolution))
					{
						_isUnoUISolution = false;
					}

					if (validPlatform && (_xamlResourcesTrimming || _isUnoUISolution))
					{
						_defaultNamespace = context.GetMSBuildPropertyValue("RootNamespace");

						_projectFullPath = context.GetMSBuildPropertyValue("MSBuildProjectFullPath");
						_projectDirectory = Path.GetDirectoryName(_projectFullPath)
							?? throw new InvalidOperationException($"MSBuild property MSBuildProjectFullPath value {_projectFullPath} is not valid");

						_intermediateOutputPath = context.GetMSBuildPropertyValue("IntermediateOutputPath");
						_intermediatePath = Path.Combine(
							_projectDirectory,
							_intermediateOutputPath
						);
						_assemblyName = context.GetMSBuildPropertyValue("AssemblyName");
						_dependencyObjectSymbol = context.Compilation.GetTypeByMetadataName("Windows.UI.Xaml.DependencyObject");
						_additionalLinkerHintAttributeSymbol = context.Compilation.GetTypeByMetadataName("Uno.Foundation.Diagnostics.CodeAnalysis.AdditionalLinkerHintAttribute");

						var additionalLinkerHintSymbols = FindAdditionalLinkerHints(context);

						var modules = from ext in context.Compilation.ExternalReferences
									  let sym = context.Compilation.GetAssemblyOrModuleSymbol(ext) as IAssemblySymbol
									  where sym != null
									  from module in sym.Modules
									  select module;

						modules = modules.Concat(context.Compilation.SourceModule);

						var propertyNames = (from module in modules
											 from type in module.GlobalNamespace.GetNamespaceTypes()
											 where (
												 type.GetAllInterfaces().Any(i => SymbolEqualityComparer.Default.Equals(i, _dependencyObjectSymbol))
												 || additionalLinkerHintSymbols.Contains(type)
											 )
											 select LinkerHintsHelpers.GetPropertyAvailableName(type.GetFullMetadataName())).ToArray();

						context.AddSource("DependencyObjectAvailability", GenerateTypeProviders(propertyNames));

						GenerateLinkerSubstitutionDefinition(propertyNames);
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

					var diagnostic = Diagnostic.Create(
						XamlCodeGenerationDiagnostics.GenericXamlErrorRule,
						null,
						$"Failed to generate linker hints. ({e.Message})");

					context.ReportDiagnostic(diagnostic);
				}
			}

			private HashSet<INamedTypeSymbol> FindAdditionalLinkerHints(GeneratorExecutionContext context)
			{
				HashSet<INamedTypeSymbol> types = new(SymbolEqualityComparer.Default);

				if (_additionalLinkerHintAttributeSymbol != null)
				{
					var attributes = context
						.Compilation
						.Assembly
						.GetAttributes()
						.Where(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, _additionalLinkerHintAttributeSymbol));

					foreach (var attribute in attributes)
					{
						if (attribute.ConstructorArguments.FirstOrDefault().Value is string targetType)
						{
							if (context.Compilation.GetTypeByMetadataName(targetType) is { } targetSymbol)
							{
								types.Add(targetSymbol);
							}
							else
							{
								var diagnostic = Diagnostic.Create(
									XamlCodeGenerationDiagnostics.GenericXamlErrorRule,
									null,
									$"Failed to find type {targetType} in the current context.");

								context.ReportDiagnostic(diagnostic);
							}
						}
						else
						{
							var diagnostic = Diagnostic.Create(
								XamlCodeGenerationDiagnostics.GenericXamlErrorRule,
								null,
								$"Type attribute AdditionalLinkerHintAttribute must have exactly one parameter");

							context.ReportDiagnostic(diagnostic);
						}
					}
				}

				return types;
			}

			private void GenerateLinkerSubstitutionDefinition(string[] propertyNames)
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

				foreach (var propertyName in propertyNames)
				{
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

			private StringBuilderBasedSourceText GenerateTypeProviders(string[] propertyNames)
			{
				var writer = new IndentedStringBuilder();

				writer.AppendLineIndented("// <auto-generated>");
				writer.AppendLineIndented("// *****************************************************************************");
				writer.AppendLineIndented("// This file has been generated by Uno.UI (DependencyObjectAvailabilityGenerator)");
				writer.AppendLineIndented("// *****************************************************************************");
				writer.AppendLineIndented("// </auto-generated>");
				writer.AppendLine();
				writer.AppendLineIndented("#pragma warning disable 618  // Ignore obsolete members warnings");

				writer.AppendLineIndented($"// _intermediatePath: {_intermediatePath}");
				writer.AppendLineIndented($"// _intermediateOutputPath: {_intermediateOutputPath}");

				using (writer.BlockInvariant("namespace {0}", _defaultNamespace))
				{
					using (writer.BlockInvariant("internal class " + LinkerHintsHelpers.GetLinkerHintsClassName()))
					{
						foreach (var propertyName in propertyNames)
						{
							writer.AppendLineIndented($"internal static bool {propertyName} => true;");
						}
					}
				}

				return new StringBuilderBasedSourceText(writer.Builder);
			}
		}
	}
}
