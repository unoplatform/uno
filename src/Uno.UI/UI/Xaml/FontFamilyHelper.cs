using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.UI.Xaml.Media;

namespace Microsoft.UI.Xaml;

internal static partial class FontFamilyHelper
{
#if __APPLE_UIKIT__ || __MACOS__
	internal static string RemoveUri(string familyName)
	{
		var slashIndex = familyName.LastIndexOf('/');

		if (slashIndex != -1)
		{
			familyName = familyName.Substring(slashIndex + 1);
		}
		return familyName;
	}
#endif

#if __ANDROID__
	internal static string RemoveHashFamilyName(string familyName)
	{
		var hashIndex = familyName.IndexOf('#');

		if (hashIndex != -1)
		{
			familyName = familyName.Substring(0, hashIndex);
		}
		return familyName;
	}
#endif
}
