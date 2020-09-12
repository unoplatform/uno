using System;
using System.IO;
using System.Text;
using Uno.SourceGeneration;
using Uno.UI.SourceGenerators.Telemetry;

namespace Uno.UI.SourceGenerators.XamlGenerator
{
	[GenerateAfter("Uno.UI.SourceGenerators.DependencyObject." + nameof(DependencyObject.DependencyPropertyGenerator))]
	public class XamlCodeGenerator : SourceGenerator
	{
		public override void Execute(SourceGeneratorContext context)
		{
			var gen = new XamlCodeGeneration(
				context.Compilation,
				context.GetProjectInstance(),
				context.Project
			);

			if (PlatformHelper.IsValidPlatform(context))
			{
				var genereratedTrees = gen.Generate();

				foreach (var tree in genereratedTrees)
				{
					context.AddCompilationUnit(tree.Key, tree.Value);
				}
			}
		}
	}
}
