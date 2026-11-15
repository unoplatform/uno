using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Documents.TextFormatting;
using Microsoft.UI.Xaml.Media;
using Windows.Storage;
using Windows.Storage.Helpers;
using Windows.UI.Text;
using Microsoft.UI.Xaml.Controls;
using InternalFontFamilyHelper = Microsoft.UI.Xaml.FontFamilyHelper;

namespace Uno.UI.Xaml.Media;

public static partial class FontFamilyHelper
{
	/// <summary>
	/// Pre-loads a font to minimize loading time and prevent potential text re-layouts.
	/// </summary>
	/// <returns>True if the font loaded successfully, otherwise false.</returns>
	public static Task<bool> PreloadAsync(
		FontFamily family,
		FontWeight weight,
		FontStretch stretch,
		FontStyle style) => InternalFontFamilyHelper.PreloadAsync(family, weight, stretch, style);

	/// <summary>
	/// Pre-loads a font to minimize loading time and prevent potential text re-layouts.
	/// </summary>
	/// <returns>True if the font loaded successfully, otherwise false.</returns>
	public static Task<bool> PreloadAsync(
		string familyName,
		FontWeight weight,
		FontStretch stretch,
		FontStyle style)
		=> PreloadAsync(new FontFamily(familyName), weight, stretch, style);

	/// <summary>
	/// Preloads all fonts listed in a given font manifest file.
	/// </summary>
	/// <param name="uri">The URI of the font (ending with.ttf without .manifest)</param>
	/// <returns>True if the font loaded successfully, otherwise false.</returns>
	public static async Task<bool> PreloadAllFontsInManifest(Uri uri) => await InternalFontFamilyHelper.PreloadAllFontsInManifest(uri);
}
