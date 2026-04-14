#nullable enable

namespace Uno.UI.SourceGenerators.XamlGenerator.CSharpExpressions;

/// <summary>
/// Single-quoted → double-quoted string translation with escape-sequence preservation
/// and char-literal re-detection. See <c>contracts/expression-grammar.md</c> §Literals.
/// </summary>
internal static class QuoteTransform
{
	// TODO (T031): implement the full transform. Accepted escape sequences:
	// \' \" \\ \n \t \0 \uXXXX. Char literals are re-detected after semantic analysis
	// (hook is TransformQuotesWithSemantics — separate entry point).
	public static string Transform(string expression) => expression;

	// TODO (T031 follow-up): re-run quote transform after method symbol resolution so a
	// single-character single-quoted literal in a 'char' parameter position becomes a
	// C# char literal instead of a single-character string.
	public static string TransformQuotesWithSemantics(string expression) => expression;
}
