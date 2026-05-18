#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Uno.Roslyn;
using Uno.UI.SourceGenerators.Helpers;
using Uno.DevTools.Telemetry;

namespace Uno.UI.SourceGenerators.XamlGenerator
{
	[Generator]
	public partial class XamlCodeGenerator : IIncrementalGenerator
	{
		public void Initialize(IncrementalGeneratorInitializationContext context)
		{
			//var process = Process.GetCurrentProcess().ProcessName;
			//if (process.IndexOf("VBCSCompiler", StringComparison.OrdinalIgnoreCase) is not -1
			//	|| process.IndexOf("csc", StringComparison.OrdinalIgnoreCase) is not -1)
			//{
			//	Debugger.Launch();
			//}

			var additionalFilesProvider = context.AdditionalTextsProvider.Collect();

			var combined = context.CompilationProvider
				.Combine(additionalFilesProvider)
				.Combine(context.AnalyzerConfigOptionsProvider);

			context.RegisterSourceOutput(combined, static (spc, data) =>
			{
				var ((compilation, additionalFiles), optionsProvider) = data;

				if (!IsValidPlatform(compilation))
				{
					return;
				}

				var sourceContext = XamlSourceContext.FromIncrementalInputs(
					compilation: compilation,
					cancellationToken: spc.CancellationToken,
					additionalFiles: additionalFiles,
					optionsProvider: optionsProvider,
					reportDiagnostic: spc.ReportDiagnostic,
					addSource: spc.AddSource);

				var isDesignTimeBuild = IsDesignTimeBuild(optionsProvider);
				var gen = new XamlCodeGeneration(sourceContext, isDesignTimeBuild);
				var generatedTrees = gen.Generate();

				foreach (var tree in generatedTrees)
				{
					spc.AddSource(tree.Key, tree.Value);
				}

				DumpXamlSourceGeneratorState(sourceContext, generatedTrees);
			});
		}

		private static bool IsValidPlatform(Compilation compilation)
			=> compilation.Options.OutputKind != OutputKind.WindowsRuntimeApplication
				&& compilation.Options.OutputKind != OutputKind.WindowsRuntimeMetadata;

		private static bool IsDesignTimeBuild(AnalyzerConfigOptionsProvider optionsProvider)
		{
			optionsProvider.GlobalOptions.TryGetValue("build_property.BuildingProject", out var buildingProject);
			optionsProvider.GlobalOptions.TryGetValue("build_property.DesignTimeBuild", out var designTimeBuild);

			return string.Equals(buildingProject, "false", StringComparison.OrdinalIgnoreCase)
				|| string.Equals(designTimeBuild, "true", StringComparison.OrdinalIgnoreCase);
		}
	}
}
