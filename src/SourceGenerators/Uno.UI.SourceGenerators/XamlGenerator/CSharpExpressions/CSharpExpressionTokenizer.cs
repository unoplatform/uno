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
	public static IReadOnlyList<Token> Tokenize(string expression)
	{
		var tokens = new List<Token>();
		if (string.IsNullOrEmpty(expression))
		{
			return tokens;
		}

		var i = 0;
		var len = expression.Length;

		while (i < len)
		{
			var c = expression[i];

			if (c == '$' && i + 1 < len && expression[i + 1] == '\'')
			{
				TokenizeInterpolatedString(expression, ref i, tokens);
				continue;
			}

			if (c == '\'')
			{
				TokenizeSingleQuotedString(expression, ref i, tokens);
				continue;
			}

			if (char.IsWhiteSpace(c))
			{
				var start = i;
				while (i < len && char.IsWhiteSpace(expression[i]))
				{
					i++;
				}
				tokens.Add(new Token(TokenKind.Whitespace, start, i - start));
				continue;
			}

			if (IsIdentifierStart(c))
			{
				var start = i;
				i++;
				while (i < len && IsIdentifierPart(expression[i]))
				{
					i++;
				}
				tokens.Add(new Token(TokenKind.Identifier, start, i - start));
				continue;
			}

			if (char.IsDigit(c))
			{
				var start = i;
				i++;
				while (i < len && (char.IsDigit(expression[i]) || expression[i] == '.'))
				{
					i++;
				}
				tokens.Add(new Token(TokenKind.Number, start, i - start));
				continue;
			}

			if (IsOperatorChar(c))
			{
				var start = i;
				i++;
				while (i < len && IsOperatorChar(expression[i]))
				{
					i++;
				}
				tokens.Add(new Token(TokenKind.Operator, start, i - start));
				continue;
			}

			// Punctuation: . , ( ) [ ] ; : ?
			tokens.Add(new Token(TokenKind.Punctuation, i, 1));
			i++;
		}

		return tokens;
	}

	private static void TokenizeSingleQuotedString(string expression, ref int i, List<Token> tokens)
	{
		var start = i;
		i++; // skip opening quote
		var len = expression.Length;
		while (i < len)
		{
			var c = expression[i];
			if (c == '\\' && i + 1 < len)
			{
				i += 2;
				continue;
			}
			if (c == '\'')
			{
				i++;
				break;
			}
			i++;
		}
		tokens.Add(new Token(TokenKind.StringLiteral, start, i - start));
	}

	private static void TokenizeInterpolatedString(string expression, ref int i, List<Token> tokens)
	{
		var len = expression.Length;
		tokens.Add(new Token(TokenKind.InterpolatedStringStart, i, 2));
		i += 2; // skip $'

		while (i < len)
		{
			var c = expression[i];

			if (c == '\\' && i + 1 < len)
			{
				i += 2;
				continue;
			}

			if (c == '\'')
			{
				tokens.Add(new Token(TokenKind.InterpolatedStringEnd, i, 1));
				i++;
				return;
			}

			if (c == '{')
			{
				tokens.Add(new Token(TokenKind.InterpolationExpressionStart, i, 1));
				i++;

				// Tokenize interpolation hole up to matching '}' (ignoring nested strings / format spec ':').
				while (i < len && expression[i] != '}')
				{
					var ic = expression[i];
					if (ic == '\'')
					{
						TokenizeSingleQuotedString(expression, ref i, tokens);
						continue;
					}
					if (ic == '$' && i + 1 < len && expression[i + 1] == '\'')
					{
						TokenizeInterpolatedString(expression, ref i, tokens);
						continue;
					}
					if (char.IsWhiteSpace(ic))
					{
						var ws = i;
						while (i < len && char.IsWhiteSpace(expression[i]) && expression[i] != '}')
						{
							i++;
						}
						tokens.Add(new Token(TokenKind.Whitespace, ws, i - ws));
						continue;
					}
					if (IsIdentifierStart(ic))
					{
						var idStart = i;
						i++;
						while (i < len && IsIdentifierPart(expression[i]))
						{
							i++;
						}
						tokens.Add(new Token(TokenKind.Identifier, idStart, i - idStart));
						continue;
					}
					if (char.IsDigit(ic))
					{
						var nStart = i;
						i++;
						while (i < len && (char.IsDigit(expression[i]) || expression[i] == '.'))
						{
							i++;
						}
						tokens.Add(new Token(TokenKind.Number, nStart, i - nStart));
						continue;
					}
					if (IsOperatorChar(ic))
					{
						var oStart = i;
						i++;
						while (i < len && IsOperatorChar(expression[i]))
						{
							i++;
						}
						tokens.Add(new Token(TokenKind.Operator, oStart, i - oStart));
						continue;
					}
					tokens.Add(new Token(TokenKind.Punctuation, i, 1));
					i++;
				}

				if (i < len && expression[i] == '}')
				{
					tokens.Add(new Token(TokenKind.InterpolationExpressionEnd, i, 1));
					i++;
				}
				continue;
			}

			i++;
		}
	}

	private static bool IsIdentifierStart(char c) => char.IsLetter(c) || c == '_';

	private static bool IsIdentifierPart(char c) => char.IsLetterOrDigit(c) || c == '_';

	private static bool IsOperatorChar(char c)
	{
		switch (c)
		{
			case '+':
			case '-':
			case '*':
			case '/':
			case '%':
			case '<':
			case '>':
			case '=':
			case '!':
			case '&':
			case '|':
			case '^':
			case '~':
			case '?':
				return true;
			default:
				return false;
		}
	}

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
