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
			var sampleControlInfoAttributeProvider = GetProviderForAttributedClasses(context, "Uno.UI.Samples.Controls.SampleControlInfoAttribute");
			var sampleAttributeProvider = GetProviderForAttributedClasses(context, "Uno.UI.Samples.Controls.SampleAttribute");

			sampleControlInfoAttributeProvider = IgnoreAssembly(assemblyNameProvider, "SamplesApp.UITests", sampleControlInfoAttributeProvider);
			sampleAttributeProvider = IgnoreAssembly(assemblyNameProvider, "SamplesApp.UITests", sampleAttributeProvider);

			var combined = sampleControlInfoAttributeProvider.Collect().Combine(sampleAttributeProvider.Collect());

			context.RegisterSourceOutput(combined, GenerateSource);
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

		private static void GenerateSource(SourceProductionContext context, (ImmutableArray<string> Left, ImmutableArray<string> Right) attributedTypes)
		{
			var builder = new IndentedStringBuilder();

			builder.AppendLineIndented("using System;");

			using (builder.BlockInvariant("namespace SampleControl.Presentation"))
			{
				using (builder.BlockInvariant("partial class SampleChooserViewModel"))
				{
					using (builder.BlockInvariant("internal Type[] _allSamples = new Type[]"))
					{
						foreach (var type in attributedTypes.Left)
						{
							builder.AppendLineIndented($"typeof({type}),");
						}
						foreach (var type in attributedTypes.Right)
						{
							builder.AppendLineIndented($"typeof({type}),");
						}
					}

					builder.AppendLineIndented(";");
				}
			}

			context.AddSource("AllSamplesList", builder.ToString());
		}
	}
}
