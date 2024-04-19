#nullable enable

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Uno.Roslyn
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

		public static bool TryGetOptionValue(this GeneratorExecutionContext context, AdditionalText textFile, string key, [NotNullWhen(true)] out string? value)
		{
			return context.AnalyzerConfigOptions.GetOptions(textFile).TryGetValue(key, out value);
		}

		public static IEnumerable<Uno.Roslyn.MSBuildItem> GetMSBuildItemsWithAdditionalFiles(this GeneratorExecutionContext context, string name)
		=> context
			.AdditionalFiles
			.Where(f => context.TryGetOptionValue(f, SourceItemGroupMetadata, out var sourceItemGroup)
				&& sourceItemGroup == name)
			.Select(f => new MSBuildItem(f, context))
			.ToArray();
	}

	/// <summary>
	/// Defines an Item provided by MSBuild
	/// </summary>
	public class MSBuildItem
	{
		private readonly GeneratorExecutionContext Context;
		private static MSBuildItemIdentityComparer? _identityComparer;

		internal MSBuildItem(AdditionalText file, GeneratorExecutionContext context)
		{
			File = file;
			Context = context;
		}

		/// <summary>
		/// Gets the Identity (EvalutatedInclude) of the item
		/// </summary>
		public string Identity => File.Path;

		public AdditionalText File { get; }

		/// <summary>
		/// Gets a metadata for this item
		/// </summary>
		/// <param name="name">The name of the metadata</param>
		/// <returns>The metadata value</returns>
		public string GetMetadataValue(string name)
		{
			Context.TryGetOptionValue(File, "build_metadata.AdditionalFiles." + name, out var metadataValue);

			return string.IsNullOrEmpty(metadataValue) ? "" : metadataValue!;
		}

		public static IEqualityComparer<MSBuildItem> IdentityComparer
			=> _identityComparer ??= new MSBuildItemIdentityComparer();

		private class MSBuildItemIdentityComparer : IEqualityComparer<MSBuildItem>
		{
			public bool Equals(MSBuildItem x, MSBuildItem y)
				=> string.Equals(x.Identity, y.Identity, System.StringComparison.OrdinalIgnoreCase);

			public int GetHashCode(MSBuildItem obj)
				=> obj.Identity.GetHashCode();
		}
	}
}
