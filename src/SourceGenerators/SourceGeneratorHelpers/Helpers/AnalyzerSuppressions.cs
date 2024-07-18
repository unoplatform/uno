using System;
using System.Linq;
using Uno.Extensions;

namespace Uno.UI.SourceGenerators
{
	internal static class AnalyzerSuppressionsGenerator
	{
		/// <summary>
		/// Outputs #pragma warning disable statements using the provided <see cref="IndentedStringBuilder"/>, and a list of suppressions.
		/// </summary>
		/// <param name="writer">An <see cref="IndentedStringBuilder"/> instance.</param>
		/// <param name="analyzerSuppressions">A list of analyzers definitions in the format of "Category|AnalyzerId"</param>
		internal static void Generate(IIndentedStringBuilder writer, string[] analyzerSuppressions)
		{
			var suppresses = from suppress in analyzerSuppressions
							 let parts = suppress.Split('|', '-')
							 where parts.Length == 2
							 let category = parts[0]
							 let id = parts[1]
							 select $"#pragma warning disable {id}";

			foreach (var suppress in suppresses)
			{
				writer.AppendLineIndented(suppress);
			}
		}
	}
}
