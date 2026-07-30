using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Uno.UI.Helpers.WinUI
{
	internal static partial class StringUtil
	{
		/// <summary>
		/// Format string using C++ formatted resources, used for compatibility with WinUI C++ code base.
		/// </summary>
		/// <param name="format"></param>
		/// <param name="parms"></param>
		/// <returns>A .NET formatted string</returns>
		/// <remarks>Do not use this method in another context.</remarks>
		internal static string FormatString(string format, params object[] parms)
		{
			var dotnetFormat = CppFormat().Replace(format, "{$1}");

			var list = parms.ToList();

			// Skip the first parameter so we don't spend time parsing
			// the output string, as the C++ index is staring at 1.
			list.Insert(0, null);

			return string.Format(CultureInfo.CurrentCulture, dotnetFormat.Replace("%%", "%"), list.ToArray());
		}

		[GeneratedRegex(@"\%(\d+)!.*?!", RegexOptions.Singleline)]
		private static partial Regex CppFormat();

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
