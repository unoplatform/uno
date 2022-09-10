using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Uno.RoslynHelpers;

namespace Uno.Samples.UITest.Generator
{
	[Generator]
	public class SamplesListGenerator : ISourceGenerator
	{
		private INamedTypeSymbol _sampleControlInfoSymbol;
		private INamedTypeSymbol _sampleSymbol;

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
			_sampleControlInfoSymbol = context.Compilation.GetTypeByMetadataName("Uno.UI.Samples.Controls.SampleControlInfoAttribute");
			_sampleSymbol = context.Compilation.GetTypeByMetadataName("Uno.UI.Samples.Controls.SampleAttribute");

			var query = from typeSymbol in context.Compilation.SourceModule.GlobalNamespace.GetNamespaceTypes()
						let info = typeSymbol.FindAttributeFlattened(_sampleSymbol) ?? typeSymbol.FindAttributeFlattened(_sampleControlInfoSymbol)
						where info != null
						select typeSymbol;

			query = query.Distinct();

			GenerateSamplesList(context, query);
		}

		private void GenerateSamplesList(GeneratorExecutionContext context, IEnumerable<INamedTypeSymbol> query)
		{
			var builder = new IndentedStringBuilder();

			builder.AppendLineInvariant("using System;");

			using (builder.BlockInvariant($"namespace SampleControl.Presentation"))
			{
				using (builder.BlockInvariant($"partial class SampleChooserViewModel"))
				{
					using (builder.BlockInvariant($"internal Type[] _allSamples = new Type[]"))
					{
						foreach (var type in query)
						{
							builder.AppendLineInvariant($"typeof({type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}),");
						}
					}

					builder.AppendLineInvariant(";");
				}
			}

			context.AddSource("AllSamplesList", builder.ToString());
		}
	}
}
