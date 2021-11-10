using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Uno.Foundation.Runtime.WebAssembly.Helpers
{
	internal static class StringExtensions
	{
		private static readonly Lazy<Regex> _newLineRegex = new Lazy<Regex>(() => new Regex(@"^", RegexOptions.Multiline));

		public static string Indent(this string text, int indentCount = 1)
		{
			return _newLineRegex.Value.Replace(text, new String('\t', indentCount));
		}

	}
}
