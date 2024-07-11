using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Uno.UI.SourceGenerators;

[Generator]
internal sealed class UseOpenSansGenerator : IIncrementalGenerator
{
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		var useOpenSansProvider = context.AnalyzerConfigOptionsProvider.Select(static (provider, ct) =>
		{
			provider.GlobalOptions.TryGetValue("build_property.UnoDefaultFont", out var value);
			return value?.Equals("OpenSans", StringComparison.OrdinalIgnoreCase) == true;
		});

		context.RegisterSourceOutput(useOpenSansProvider, (context, useOpenSans) =>
		{
			if (useOpenSans)
			{
				context.AddSource("UnoUseOpenSansGenerator.g.cs", """
					internal static class __UnoUseOpenSansInitializer
					{
						[global::System.Runtime.CompilerServices.ModuleInitializerAttribute]
						internal static void Initialize()
						{
							global::Uno.UI.FeatureConfiguration.Font.DefaultTextFontFamily = "ms-appx:///Uno.Fonts.OpenSans/Fonts/OpenSans.ttf";
						}
					}
					""");
			}
		});
	}
}
