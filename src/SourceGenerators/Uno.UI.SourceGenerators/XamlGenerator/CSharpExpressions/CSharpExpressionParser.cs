#nullable enable

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Uno.UI.SourceGenerators.XamlGenerator.CSharpExpressions;

/// <summary>
/// Parses a classified C# expression text into a Roslyn <see cref="SyntaxTree"/>
/// using <see cref="SourceCodeKind.Script"/> so single-expression bodies parse without
/// needing a surrounding method. Roslyn parse diagnostics are later surfaced as
/// <c>UNO2006</c> by the analyzer with the original XAML location.
/// </summary>
internal static class CSharpExpressionParser
{
	// TODO (T032): implement. Expected flow:
	//   1. Apply OperatorAliases.Replace.
	//   2. Apply QuoteTransform.Transform.
	//   3. CSharpSyntaxTree.ParseText(innerCSharp, options).
	//   4. Return tree + any script-mode parse diagnostics for the caller to convert to UNO2006.
	public static (SyntaxTree Tree, bool HasErrors) Parse(string innerCSharp)
	{
		var options = CSharpParseOptions.Default.WithKind(SourceCodeKind.Script);
		var tree = CSharpSyntaxTree.ParseText(innerCSharp, options);
		var hasErrors = false;

		foreach (var diag in tree.GetDiagnostics())
		{
			if (diag.Severity == DiagnosticSeverity.Error)
			{
				hasErrors = true;
				break;
			}
		}

		return (tree, hasErrors);
	}
}
