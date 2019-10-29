using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Uno.UI.Helpers.WinUI
{
	internal static class StringUtil
	{
		private static Regex _cppFormat = new Regex(@"\$(\d+)!.*?!", RegexOptions.Singleline);

		internal static string FormatString(string format, params object[] parms)
			=> string.Format(_cppFormat.Replace(format, "{$1}"), parms);
	}
}
