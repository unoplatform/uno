#nullable enable
#if NETSTANDARD
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Uno.Roslyn
{
	internal static class GeneratorExecutionContextExtensions
	{
		private const string SourceItemGroupMetadata = "build_metadata.AdditionalFiles.SourceItemGroup";
		public static void AddCompilationUnit(this GeneratorExecutionContext context, string name, string source)
			=> context.AddSource(name, SourceText.From(source, Encoding.UTF8));

		public static string GetMSBuildPropertyValue(
			this GeneratorExecutionContext context,
			string name,
			string defaultValue = "")
		{
			context.AnalyzerConfigOptions.GlobalOptions.TryGetValue($"build_property.{name}", out var value);
			return value ?? defaultValue;
		}
		public static IEnumerable<MSBuildItem> GetMSBuildItems(this GeneratorExecutionContext context, string name)
		=> context
			.AdditionalFiles
			.Where(f => context.AnalyzerConfigOptions
				.GetOptions(f)
				.TryGetValue(SourceItemGroupMetadata, out var sourceItemGroup)
				&& sourceItemGroup == name)
			.Select(f => new MSBuildItem(f, context))
			.ToArray();
	}

	/// <summary>
	/// Defines an Item provided by MSBuild
	/// </summary>
	public class MSBuildItem
	{
		private readonly AdditionalText File;
		private readonly GeneratorExecutionContext Context;

		internal MSBuildItem(AdditionalText file, GeneratorExecutionContext context)
		{
			File = file;
			Context = context;
		}

		/// <summary>
		/// Gets the Identity (EvalutatedInclude) of the item
		/// </summary>
		public string Identity => File.Path;

		/// <summary>
		/// Gets a metadata for this item
		/// </summary>
		/// <param name="name">The name of the metadata</param>
		/// <returns>The metadata value</returns>
		public string GetMetadataValue(string name)
		{
			Context.AnalyzerConfigOptions
				.GetOptions(File)
				.TryGetValue("build_metadata.AdditionalFiles." + name, out var metadataValue);

			return string.IsNullOrEmpty(metadataValue) ? "" : metadataValue!;
		}
	}
}
#endif
