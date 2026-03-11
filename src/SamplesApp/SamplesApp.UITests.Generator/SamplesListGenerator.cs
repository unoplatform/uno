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

		private static IncrementalValuesProvider<string> GetProviderForAttributedClasses(IncrementalGeneratorInitializationContext context, string attributeFullyQualifiedMetadataName)
		{
			return context.SyntaxProvider.ForAttributeWithMetadataName(
				attributeFullyQualifiedMetadataName,
				static (node, _) => node.IsKind(SyntaxKind.ClassDeclaration),
				static (context, _) => context.TargetSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
			);
		}

		private static IncrementalValuesProvider<string> IgnoreAssembly(IncrementalValueProvider<string> assemblyNameProvider, string assemblyName, IncrementalValuesProvider<string> classesProvider)
			=> classesProvider.Combine(assemblyNameProvider).Where(x => x.Right != assemblyName).Select((x, _) => x.Left);

		private static void GenerateSource(SourceProductionContext context, ImmutableArray<string> attributedTypes)
		{
			var builder = new IndentedStringBuilder();

			builder.AppendLineIndented("using System;");

			using (builder.BlockInvariant("namespace SampleControl.Presentation"))
			{
				using (builder.BlockInvariant("partial class SampleChooserViewModel"))
				{
					using (builder.BlockInvariant("internal Type[] _allSamples = new Type[]"))
					{
						foreach (var type in attributedTypes)
						{
							builder.AppendLineIndented($"typeof({type}),");
						}
					}

					builder.AppendLineIndented(";");
				}
			}

			context.AddSource("AllSamplesList.g.cs", builder.ToString());
		}
	}
}
