#nullable enable

using System;
using System.Text;

namespace Uno.UI.SourceGenerators.XamlGenerator.CSharpExpressions;

/// <summary>
/// Whitespace-bounded, case-insensitive, string-literal-aware replacement of
/// <c>AND → &amp;&amp;</c>, <c>OR → ||</c>, <c>LT → &lt;</c>, <c>LTE → &lt;=</c>,
/// <c>GT → &gt;</c>, <c>GTE → &gt;=</c>.
/// See <c>contracts/expression-grammar.md</c> §Operator-aliases.
/// </summary>
/// <remarks>
/// Replacement rules:
/// <list type="number">
/// <item>Applied outside of string literals only (single-quoted and interpolated strings are skipped token-by-token).</item>
/// <item>Each alias must be surrounded by whitespace or expression boundary (<c>CountGT0</c> is not a match for <c>GT</c>).</item>
/// <item><c>LTE</c>/<c>GTE</c> are replaced before <c>LT</c>/<c>GT</c> so the longer form wins.</item>
/// </list>
/// </remarks>
internal static class OperatorAliases
{
	public static string Replace(string expression)
	{
		if (string.IsNullOrEmpty(expression))
		{
			return expression;
		}

		var sb = new StringBuilder(expression.Length);
		var i = 0;
		var len = expression.Length;

		while (i < len)
		{
			var c = expression[i];

			if (c == '$' && i + 1 < len && expression[i + 1] == '\'')
			{
				CopyInterpolatedString(expression, ref i, sb);
				continue;
			}

			if (c == '\'')
			{
				CopySingleQuotedString(expression, ref i, sb);
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
				var word = expression.Substring(start, i - start);
				var replacement = TryMapAlias(word);
				sb.Append(replacement ?? word);
				continue;
			}

			sb.Append(c);
			i++;
		}

		return sb.ToString();
	}

	private static string? TryMapAlias(string word)
	{
		// Longer forms first (LTE/GTE before LT/GT). Since we compare full identifier words
		// they are unambiguous, but we keep the order for clarity.
		if (string.Equals(word, "LTE", StringComparison.OrdinalIgnoreCase))
		{
			return "<=";
		}
		if (string.Equals(word, "GTE", StringComparison.OrdinalIgnoreCase))
		{
			return ">=";
		}
		if (string.Equals(word, "LT", StringComparison.OrdinalIgnoreCase))
		{
			return "<";
		}
		if (string.Equals(word, "GT", StringComparison.OrdinalIgnoreCase))
		{
			return ">";
		}
		if (string.Equals(word, "AND", StringComparison.OrdinalIgnoreCase))
		{
			return "&&";
		}
		if (string.Equals(word, "OR", StringComparison.OrdinalIgnoreCase))
		{
			return "||";
		}
		return null;
	}

	private static void CopySingleQuotedString(string expression, ref int i, StringBuilder sb)
	{
		var len = expression.Length;
		sb.Append(expression[i]);
		i++;
		while (i < len)
		{
			var c = expression[i];
			if (c == '\\' && i + 1 < len)
			{
				sb.Append(c);
				sb.Append(expression[i + 1]);
				i += 2;
				continue;
			}
			if (c == '\'')
			{
				sb.Append(c);
				i++;
				return;
			}
			sb.Append(c);
			i++;
		}
	}

	private static void CopyInterpolatedString(string expression, ref int i, StringBuilder sb)
	{
		var len = expression.Length;
		sb.Append('$');
		sb.Append('\'');
		i += 2;

		while (i < len)
		{
			var c = expression[i];

			if (c == '\\' && i + 1 < len)
			{
				sb.Append(c);
				sb.Append(expression[i + 1]);
				i += 2;
				continue;
			}

			if (c == '\'')
			{
				sb.Append(c);
				i++;
				return;
			}

			if (c == '{')
			{
				sb.Append(c);
				i++;
				var depth = 1;
				while (i < len && depth > 0)
				{
					var ic = expression[i];
					if (ic == '{')
					{
						depth++;
						sb.Append(ic);
						i++;
						continue;
					}
					if (ic == '}')
					{
						depth--;
						sb.Append(ic);
						i++;
						continue;
					}
					// Inside interpolation hole, apply alias replacement recursively by
					// piggybacking on the outer scanner via a substring copy.
					if (ic == '\'')
					{
						CopySingleQuotedString(expression, ref i, sb);
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
						var word = expression.Substring(idStart, i - idStart);
						sb.Append(TryMapAlias(word) ?? word);
						continue;
					}
					sb.Append(ic);
					i++;
				}
				continue;
			}

			sb.Append(c);
			i++;
		}
	}

	private static bool IsIdentifierStart(char c) => char.IsLetter(c) || c == '_';

	private static bool IsIdentifierPart(char c) => char.IsLetterOrDigit(c) || c == '_';
}
