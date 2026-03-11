using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Uno.Extensions;

namespace Uno.Samples.UITest.Generator
{
	[Generator]
	public class SamplesListGenerator : IIncrementalGenerator
	{
		public void Initialize(IncrementalGeneratorInitializationContext context)
		{
			var assemblyNameProvider = context.CompilationProvider.Select((compilation, _) => compilation.Assembly.Name);
			var sampleAttributeProvider = GetProviderForAttributedClasses(context, "Uno.UI.Samples.Controls.SampleAttribute");

			sampleAttributeProvider = IgnoreAssembly(assemblyNameProvider, "SamplesApp.UITests", sampleAttributeProvider);

			var samples = sampleAttributeProvider.Collect();

			context.RegisterSourceOutput(samples, GenerateSource);
		}

		private static IncrementalValuesProvider<(string TypeName, string FilePath)> GetProviderForAttributedClasses(IncrementalGeneratorInitializationContext context, string attributeFullyQualifiedMetadataName)
		{
			return context.SyntaxProvider.ForAttributeWithMetadataName(
				attributeFullyQualifiedMetadataName,
				static (node, _) => node.IsKind(SyntaxKind.ClassDeclaration),
				static (context, _) =>
				{
					var typeName = context.TargetSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
					var filePath = NormalizeFilePath(context.TargetNode.SyntaxTree.FilePath);
					return (TypeName: typeName, FilePath: filePath);
				}
			);
		}

		private static string NormalizeFilePath(string filePath)
		{
			if (string.IsNullOrEmpty(filePath))
			{
				return "";
			}

			// Normalize separators
			filePath = filePath.Replace('\\', '/');

			// Extract portion after UITests.Shared/
			var marker = "UITests.Shared/";
			var index = filePath.IndexOf(marker);
			if (index < 0)
			{
				return "";
			}

			var relativePath = filePath.Substring(index + marker.Length);

			// For .xaml.cs files, strip the trailing .cs to link to the XAML file
			if (relativePath.EndsWith(".xaml.cs"))
			{
				relativePath = relativePath.Substring(0, relativePath.Length - 3);
			}

			return relativePath;
		}

		private static IncrementalValuesProvider<(string TypeName, string FilePath)> IgnoreAssembly(IncrementalValueProvider<string> assemblyNameProvider, string assemblyName, IncrementalValuesProvider<(string TypeName, string FilePath)> classesProvider)
			=> classesProvider.Combine(assemblyNameProvider).Where(x => x.Right != assemblyName).Select((x, _) => x.Left);

		private static void GenerateSource(SourceProductionContext context, ImmutableArray<(string TypeName, string FilePath)> attributedTypes)
		{
			var builder = new IndentedStringBuilder();

			builder.AppendLineIndented("using System;");

			using (builder.BlockInvariant("namespace SampleControl.Presentation"))
			{
				using (builder.BlockInvariant("partial class SampleChooserViewModel"))
				{
					using (builder.BlockInvariant("internal Type[] _allSamples = new Type[]"))
					{
						foreach (var entry in attributedTypes)
						{
							builder.AppendLineIndented($"typeof({entry.TypeName}),");
						}
					}

					builder.AppendLineIndented(";");

					builder.AppendLineIndented("");

					using (builder.BlockInvariant("internal static readonly System.Collections.Generic.Dictionary<Type, string> _allSamplePaths = new()"))
					{
						foreach (var entry in attributedTypes)
						{
							if (!string.IsNullOrEmpty(entry.FilePath))
							{
								builder.AppendLineIndented($"{{ typeof({entry.TypeName}), \"{entry.FilePath}\" }},");
							}
						}
					}

					builder.AppendLineIndented(";");
				}
			}

			context.AddSource("AllSamplesList.g.cs", builder.ToString());
		}
	}
}
