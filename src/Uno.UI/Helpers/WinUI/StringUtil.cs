using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Uno.UI.Helpers.WinUI
{
	internal static partial class StringUtil
	{
		/// <summary>
		/// Format string using C++ formatted resources, used for compatibility with WinUI C++ code base.
		/// Mirrors Win32 <c>FormatMessage</c> with <c>FORMAT_MESSAGE_FROM_STRING</c>: <c>%1</c>..<c>%99</c>
		/// (optionally followed by a <c>!printf!</c> spec, which is ignored) insert the matching parameter,
		/// <c>%%</c> is a percent sign, <c>%0</c> ends the output, <c>%n</c>, <c>%t</c> and <c>%r</c> are a
		/// line break, a tab and a carriage return, any other <c>%x</c> is <c>x</c>, and braces are literal.
		/// </summary>
		/// <param name="format"></param>
		/// <param name="parms"></param>
		/// <returns>The formatted string, or an empty string when <c>FormatMessage</c> would fail: a parameter index past the supplied values, an unterminated <c>!spec!</c> or a trailing lone <c>%</c>.</returns>
		/// <remarks>Do not use this method in another context.</remarks>
		internal static string FormatString(string format, params object[] parms)
		{
			if (string.IsNullOrEmpty(format))
			{
				return string.Empty;
			}

			var sb = new StringBuilder(format.Length + 16);
			var i = 0;
			while (i < format.Length)
			{
				var c = format[i++];
				if (c != '%')
				{
					sb.Append(c);
					continue;
				}

				if (i == format.Length)
				{
					return string.Empty;
				}

				var next = format[i++];
				if (next == '0')
				{
					break;
				}

				if (next is >= '1' and <= '9')
				{
					var index = next - '0';
					if (i < format.Length && char.IsAsciiDigit(format[i]))
					{
						index = index * 10 + (format[i++] - '0');
					}

					if (i < format.Length && format[i] == '!')
					{
						var close = format.IndexOf('!', i + 1);
						if (close < 0)
						{
							return string.Empty;
						}

						i = close + 1;
					}

					if (index > parms.Length)
					{
						return string.Empty;
					}

					sb.Append(Convert.ToString(parms[index - 1], CultureInfo.CurrentCulture));
					continue;
				}

				sb.Append(next switch
				{
					'n' => "\r\n",
					't' => "\t",
					'r' => "\r",
					_ => next.ToString(),
				});
			}

			return sb.ToString();
		}

		internal static string Swprintf_s(string input, params object[] inpVars)
		{
			int i = 0;
			input = SimpleCppFormatRegex().Replace(input, m => ("{" + i++/*increase have to be on right side*/ + "}"));
			return string.Format(CultureInfo.CurrentCulture, input, inpVars);
		}

		[GeneratedRegex("%.")]
		private static partial Regex SimpleCppFormatRegex();

#nullable enable
		/// <summary>
		/// Substitutes Win32 <c>FormatMessage</c>-style placeholders <c>%1</c>..<c>%9</c>
		/// with the corresponding values from <paramref name="args"/>. Mirrors WinUI's
		/// <c>FormatMsg</c> used with UIA resource strings (e.g. <c>"%1 %2 time picker"</c>).
		/// </summary>
		/// <remarks>
		/// <list type="bullet">
		/// <item><c>%%</c> is preserved as-is (matches FormatMessage's literal-percent behavior).</item>
		/// <item><c>%0</c> and indices past the supplied args are left untouched.</item>
		/// <item>If <paramref name="format"/> contains no <c>%n</c> sequences it is returned as-is (no allocation).</item>
		/// </list>
		/// </remarks>
		internal static string FormatMsg(string format, params string?[] args)
		{
			if (string.IsNullOrEmpty(format))
			{
				return format ?? string.Empty;
			}

			StringBuilder? sb = null;
			var lastCopied = 0;
			for (var i = 0; i < format.Length - 1; i++)
			{
				var c = format[i];
				if (c != '%')
				{
					continue;
				}

				var next = format[i + 1];
				if (next == '%')
				{
					// %% escape: leave as-is (do not collapse — matches WinUI FormatMsg behavior).
					i++;
					continue;
				}

				if (next < '1' || next > '9')
				{
					continue;
				}

				var argIndex = next - '1';
				if (argIndex >= args.Length)
				{
					continue;
				}

				sb ??= new StringBuilder(format.Length + 32);
				sb.Append(format, lastCopied, i - lastCopied);
				sb.Append(args[argIndex] ?? string.Empty);

				i++; // consume the digit
				lastCopied = i + 1;
			}

			if (sb is null)
			{
				return format;
			}

			if (lastCopied < format.Length)
			{
				sb.Append(format, lastCopied, format.Length - lastCopied);
			}

			return sb.ToString();
		}
#nullable restore
	}
}
