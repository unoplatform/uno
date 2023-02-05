using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media;

namespace Uno.UI.Xaml.Media;

/// <summary>
/// WebAssembly specific <see cref="FontFamily"/> helper
/// </summary>
public static class FontFamilyHelper
{
	/// <summary>
	/// Pre-loads a font to minimize loading time and prevent potential text re-layouts.
	/// </summary>
	/// <returns>True is the font loaded successfuly, otherwise false.</returns>
	public static Task<bool> PreloadAsync(FontFamily family)
		=> FontFamily.PreloadAsync(family);

	/// <summary>
	/// Pre-loads a font to minimize loading time and prevent potential text re-layouts.
	/// </summary>
	/// <returns>True is the font loaded successfuly, otherwise false.</returns>
	public static Task<bool> PreloadAsync(string familyName)
		=> PreloadAsync(new FontFamily(familyName));
}
