using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Media;

namespace Microsoft.UI.Xaml;

internal partial class FontFamilyHelper
{
	/// <summary>
	/// Pre-loads a font to minimize loading time and prevent potential text re-layouts.
	/// </summary>
	/// <returns>True is the font loaded successfully, otherwise false.</returns>
	internal static Task<bool> PreloadAsync(FontFamily family)
		=> FontFamily.PreloadAsync(family);

	/// <summary>
	/// Pre-loads a font to minimize loading time and prevent potential text re-layouts.
	/// </summary>
	/// <returns>True is the font loaded successfully, otherwise false.</returns>
	internal static Task<bool> PreloadAsync(string familyName)
		=> PreloadAsync(new FontFamily(familyName));
}
