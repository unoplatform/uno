#nullable enable

using System.Collections.Generic;

namespace Uno.UI.SourceGenerators.XamlGenerator.CSharpExpressions;

/// <summary>
/// Single-pass tokenizer recognising single-quoted strings, escape sequences,
/// interpolation token boundaries, and operator-alias candidates.
/// Consumed by <see cref="CSharpExpressionParser"/> and <see cref="OperatorAliases"/>.
/// See <c>contracts/expression-grammar.md</c>.
/// </summary>
internal static class CSharpExpressionTokenizer
{
	// TODO (T030): implement single-pass tokenization. Required to classify
	// string-literal vs expression-code regions so OperatorAliases and QuoteTransform
	// don't accidentally rewrite inside strings. Drive implementation via
	// Given_ExpressionTokenizer tests (T018).
	public static IReadOnlyList<Token> Tokenize(string expression) => System.Array.Empty<Token>();

	internal readonly record struct Token(TokenKind Kind, int Start, int Length);

	internal enum TokenKind
	{
		Identifier,
		Number,
		StringLiteral,
		InterpolatedStringStart,
		InterpolatedStringEnd,
		InterpolationExpressionStart,
		InterpolationExpressionEnd,
		Operator,
		Punctuation,
		Whitespace,
		Unknown,
	}
}
