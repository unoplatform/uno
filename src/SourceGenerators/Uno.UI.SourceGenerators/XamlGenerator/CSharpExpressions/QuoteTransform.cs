#nullable enable

using System.Text;

namespace Uno.UI.SourceGenerators.XamlGenerator.CSharpExpressions;

/// <summary>
/// Single-quoted → double-quoted string translation with escape-sequence preservation
/// and char-literal re-detection. See <c>contracts/expression-grammar.md</c> §Literals.
/// </summary>
internal static class QuoteTransform
{
	public static string Transform(string expression)
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
				sb.Append('$');
				i++;
				RewriteSingleQuotedSpan(expression, ref i, sb, isInterpolated: true);
				continue;
			}

			if (c == '\'')
			{
				RewriteSingleQuotedSpan(expression, ref i, sb, isInterpolated: false);
				continue;
			}

			sb.Append(c);
			i++;
		}

		return sb.ToString();
	}

	public static string TransformQuotesWithSemantics(string expression) => expression;

	private static void RewriteSingleQuotedSpan(string expression, ref int i, StringBuilder sb, bool isInterpolated)
	{
		sb.Append('"');
		i++; // skip opening '

		var len = expression.Length;
		while (i < len)
		{
			var c = expression[i];

			if (c == '\\' && i + 1 < len)
			{
				var next = expression[i + 1];
				if (next == '\'')
				{
					sb.Append('\''); // unescape
					i += 2;
					continue;
				}
				// preserve other escapes verbatim (\\, \n, \t, \0, \uXXXX, \")
				sb.Append(c);
				sb.Append(next);
				i += 2;
				continue;
			}

			if (c == '\'')
			{
				sb.Append('"');
				i++;
				return;
			}

			if (c == '"')
			{
				// Bare double-quote inside original single-quoted string becomes
				// content in a double-quoted string and must be escaped.
				sb.Append('\\');
				sb.Append('"');
				i++;
				continue;
			}

			if (isInterpolated && c == '{')
			{
				// Walk the interpolation hole so an embedded ':' or nested quote rewriting
				// happens recursively.
				sb.Append('{');
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
					if (ic == '\'')
					{
						RewriteSingleQuotedSpan(expression, ref i, sb, isInterpolated: false);
						continue;
					}
					if (ic == '$' && i + 1 < len && expression[i + 1] == '\'')
					{
						sb.Append('$');
						i++;
						RewriteSingleQuotedSpan(expression, ref i, sb, isInterpolated: true);
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
}
