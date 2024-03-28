using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media;

namespace Windows.UI.Xaml;

internal partial class FontFamilyHelper
{
	/// <summary>
	/// Pre-loads a font to minimize loading time and prevent potential text re-layouts.
	/// </summary>
	/// <returns>True is the font loaded successfuly, otherwise false.</returns>
	internal static Task<bool> PreloadAsync(FontFamily family)
		=> FontFamily.PreloadAsync(family);

	/// <summary>
	/// Pre-loads a font to minimize loading time and prevent potential text re-layouts.
	/// </summary>
	/// <returns>True is the font loaded successfuly, otherwise false.</returns>
	internal static Task<bool> PreloadAsync(string familyName)
		=> PreloadAsync(new FontFamily(familyName));
}
