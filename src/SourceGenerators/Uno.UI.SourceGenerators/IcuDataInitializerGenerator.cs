using System;
using Microsoft.CodeAnalysis;

namespace Uno.UI.SourceGenerators;

[Generator]
public class IcuDataInitializerGenerator : IIncrementalGenerator
{
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		var shouldGenerateProvider = context.AnalyzerConfigOptionsProvider.Select(static (provider, ct) =>
		{
			provider.GlobalOptions.TryGetValue("build_property.IsUnoHead", out var isUnoHead);
			provider.GlobalOptions.TryGetValue("build_property.SkipIcuDataInitializerGenerator", out var skipIcuDataInitializerGenerator);
			return isUnoHead?.Equals("true", StringComparison.InvariantCultureIgnoreCase) == true && (skipIcuDataInitializerGenerator?.Equals("false", StringComparison.InvariantCultureIgnoreCase) ?? true);
		});

		context.RegisterSourceOutput(shouldGenerateProvider, (productionContext, shouldGenerate) =>
		{
			if (!shouldGenerate)
			{
				return;
			}
			var code = """
						internal static class __IcuDataInitializer
						{
							[global::System.Runtime.CompilerServices.ModuleInitializerAttribute]
							internal static void Initialize()
							{
								var unoAssembly = typeof(Microsoft.UI.Xaml.UIElement).Assembly;
								var unicodeTextType = unoAssembly.GetType("Microsoft.UI.Xaml.Documents.UnicodeText");
								var icuType = unicodeTextType?.GetNestedType("ICU",
									System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
								var setMethod = icuType?.GetMethod("SetDataAssembly",
									System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
								setMethod?.Invoke(null, new object[] { typeof(__IcuDataInitializer).Assembly });
							}
						}
						""";

			productionContext.AddSource("IcuDataInitializer.g.cs", code);
		});
	}
}

