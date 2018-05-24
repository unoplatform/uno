#if __ANDROID__
using System;
using System.Collections.Generic;
using System.Text;
using Java.Util;
using Uno.UI;

namespace Windows.Globalization
{
	public static partial class ApplicationLanguages
	{
		public static IReadOnlyList<string> Languages => new[] { ContextHelper.Current.Resources.Configuration.Locale.ToLanguageTag() };

		public static IReadOnlyList<string> ManifestLanguages { get { throw new NotImplementedException(); } }
	}
}
#endif