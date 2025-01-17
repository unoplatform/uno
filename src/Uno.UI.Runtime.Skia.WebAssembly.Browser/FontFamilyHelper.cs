using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Documents.TextFormatting;
using Microsoft.UI.Xaml.Media;
using SkiaSharp;
using Uno.Foundation.Logging;
using Windows.Storage;
using Windows.UI.Text;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.Xaml.Media;

public static class FontFamilyHelper
{
	/// <summary>
	/// Pre-loads a font to minimize loading time and prevent potential text re-layouts.
	/// </summary>
	/// <returns>True if the font loaded successfully, otherwise false.</returns>
	public static Task<bool> PreloadAsync(
		FontFamily family,
		FontWeight weight,
		FontStretch stretch,
		FontStyle style)
	{
		// size doesn't matter here, we're just preloading the typeface
		// Default value of the font is of type double and boxed in object
		var fontSize = (float)(double)TextBlock.FontSizeProperty.Metadata.DefaultValue;
		return FontDetailsCache.GetFont(family.Source, fontSize, weight, stretch, style)
			.loadedTask
			.ContinueWith(t => t is { IsCompletedSuccessfully: true, Result: not null });
	}

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
}
