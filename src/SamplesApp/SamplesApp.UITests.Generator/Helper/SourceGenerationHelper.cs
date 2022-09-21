using Microsoft.CodeAnalysis;

namespace Uno.Samples.UITest.Generator.Helper
{
	internal static class GeneratorExecutionContextExtensions
	{
		private const string SourceItemGroupMetadata = "build_metadata.AdditionalFiles.SourceItemGroup";

		public static string GetMSBuildPropertyValue(
			this GeneratorExecutionContext context,
			string name,
			string defaultValue = "")
		{
			context.AnalyzerConfigOptions.GlobalOptions.TryGetValue($"build_property.{name}", out var value);
			return value ?? defaultValue;
		}

		public static bool TryGetOptionValue(this GeneratorExecutionContext context, AdditionalText textFile, string key, out string? value)
		{
			return context.AnalyzerConfigOptions.GetOptions(textFile).TryGetValue(key, out value);
		}
	}
}
