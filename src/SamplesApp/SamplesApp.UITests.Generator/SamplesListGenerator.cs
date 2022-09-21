using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Uno.Extensions;

namespace Uno.Samples.UITest.Generator
{
	[Generator]
	public class SamplesListGenerator : ISourceGenerator
	{
		public void Initialize(GeneratorInitializationContext context)
		{
		}

		public void Execute(GeneratorExecutionContext context)
		{
#if DEBUG
			// Debugger.Launch();
#endif
			if (context.Compilation.Assembly.Name != "SamplesApp.UITests")
			{
				GenerateTests(context);
			}
		}

		private void GenerateTests(GeneratorExecutionContext context)
		{
			var sampleControlInfoSymbol = context.Compilation.GetTypeByMetadataName("Uno.UI.Samples.Controls.SampleControlInfoAttribute");
			var sampleSymbol = context.Compilation.GetTypeByMetadataName("Uno.UI.Samples.Controls.SampleAttribute");

			var query = from typeSymbol in context.Compilation.SourceModule.GlobalNamespace.GetNamespaceTypes()
						let info = typeSymbol.FindAttributeFlattened(sampleSymbol) ?? typeSymbol.FindAttributeFlattened(sampleControlInfoSymbol)
						where info != null
						select typeSymbol;

			query = query.Distinct();

			GenerateSamplesList(context, query);
		}

		private void GenerateSamplesList(GeneratorExecutionContext context, IEnumerable<INamedTypeSymbol> query)
		{
			var builder = new IndentedStringBuilder();

			builder.AppendLineIndented("using System;");

			using (builder.BlockInvariant($"namespace SampleControl.Presentation"))
			{
				using (builder.BlockInvariant($"partial class SampleChooserViewModel"))
				{
					using (builder.BlockInvariant($"internal Type[] _allSamples = new Type[]"))
					{
						foreach (var type in query)
						{
							builder.AppendLineIndented($"typeof({type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}),");
						}
					}

					builder.AppendLineIndented(";");
				}
			}

			context.AddSource("AllSamplesList", builder.ToString());
		}
	}
}
