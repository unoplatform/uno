using System;
using System.Diagnostics;
using System.IO;
using System.Net.Mime;
using System.Text;
using Microsoft.CodeAnalysis;
using Uno.Roslyn;
using Uno.UI.SourceGenerators.Telemetry;

#if NETFRAMEWORK
using Uno.SourceGeneration;
#endif

namespace Uno.UI.SourceGenerators.XamlGenerator
{
#if NETFRAMEWORK
	[GenerateAfter("Uno.UI.SourceGenerators.DependencyObject." + nameof(DependencyObject.DependencyPropertyGenerator))]
#endif
	[Generator]
	public class XamlCodeGenerator : ISourceGenerator
	{
		public void Initialize(GeneratorInitializationContext context)
		{
		}

		public void Execute(GeneratorExecutionContext context)
		{
			DependenciesInitializer.Init(context);

			// No initialization required for this one
			//if (!Process.GetCurrentProcess().ProcessName.Equals("devenv", StringComparison.OrdinalIgnoreCase))
			{
				// Debugger.Launch();
			}

			var gen = new XamlCodeGeneration(context);

			if (PlatformHelper.IsValidPlatform(context))
			{
				var genereratedTrees = gen.Generate();

				foreach (var tree in genereratedTrees)
				{
					context.AddSource(tree.Key, tree.Value);

					// Uncomment to output the generated files to a separate folder
					// var intermediatePath = context.GetMSBuildPropertyValue("IntermediateOutputPath");
					// var path = Path.Combine(intermediatePath, "generated");
					// Directory.CreateDirectory(path);
					// File.WriteAllText(Path.Combine(path, tree.Key), tree.Value);
				}
			}
		}
	}
}
