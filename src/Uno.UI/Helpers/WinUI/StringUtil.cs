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
	}
}
