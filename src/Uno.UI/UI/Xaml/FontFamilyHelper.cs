using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.UI.Xaml.Media;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Documents.TextFormatting;
using Windows.Storage;
using Windows.Storage.Helpers;
using Windows.UI.Text;
using Microsoft.UI.Xaml.Controls;
using Uno.UI;
using Uno.UI.Xaml.Media;

namespace Microsoft.UI.Xaml;

internal static partial class FontFamilyHelper
{
	/// <summary>
	/// The weight flags, i.e. the ones that can select a face on their own. <see cref="FontPreloadVariants.Italic"/>
	/// and <see cref="FontPreloadVariants.Condensed"/> only widen an existing weight selection.
	/// </summary>
	private const FontPreloadVariants AnyWeight =
		FontPreloadVariants.Thin | FontPreloadVariants.ExtraLight | FontPreloadVariants.Light |
		FontPreloadVariants.Normal | FontPreloadVariants.Medium | FontPreloadVariants.SemiBold |
		FontPreloadVariants.Bold | FontPreloadVariants.ExtraBold | FontPreloadVariants.Black;

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

	/// <param name="uri">The URI of the font (ending with.ttf without .manifest)</param>
	public static Task<bool> PreloadAllFontsInManifest(Uri uri)
		=> PreloadFontsInManifest(uri, FontPreloadVariants.All);

	/// <param name="uri">The URI of the font (ending with.ttf without .manifest)</param>
	/// <param name="variants">
	/// The variants to preload. A full family can declare dozens of weight/width/style combinations,
	/// and on platforms that fetch fonts individually each one is a separate request on the startup path.
	/// </param>
	internal static async Task<bool> PreloadFontsInManifest(Uri uri, FontPreloadVariants variants)
	{
		if (!SelectsAnyFace(variants))
		{
			// Nothing can be selected, so don't even fetch the manifest.
			return true;
		}

		var manifestUri = new Uri(uri.OriginalString + ".manifest");
		var path = Uri.UnescapeDataString(manifestUri.PathAndQuery).TrimStart('/');
		if (!await StorageFileHelper.ExistsInPackage(path))
		{
			return false;
		}

		var manifestFile = await StorageFile.GetFileFromApplicationUriAsync(manifestUri);
		FontManifest manifest = null;
		using (var manifestStream = await manifestFile.OpenStreamForReadAsync())
		{
			manifest = FontManifestHelpers.DeserializeManifest(manifestStream);
		}

		if (manifest is null)
		{
			return false;
		}

		var tasks = SelectVariants(manifest.Fonts, variants)
			.Select(fontInfo => PreloadAsync(fontInfo.FamilyName, new FontWeight(fontInfo.FontWeight), fontInfo.FontStretch, fontInfo.FontStyle));

		return await Task.WhenAll(tasks).ContinueWith(combinedTask => combinedTask.Result.All(t => t));
	}

	/// <summary>
	/// Whether <paramref name="variants"/> can match a face at all. Covers <see cref="FontPreloadVariants.None"/>
	/// as well as a width/style-only selection, which would otherwise match nothing and trip the fallback below.
	/// </summary>
	internal static bool SelectsAnyFace(FontPreloadVariants variants) => (variants & AnyWeight) != 0;

	internal static IReadOnlyList<FontInfo> SelectVariants(IReadOnlyList<FontInfo> fonts, FontPreloadVariants variants)
	{
		if (!SelectsAnyFace(variants))
		{
			return Array.Empty<FontInfo>();
		}

		if (variants == FontPreloadVariants.All)
		{
			return fonts;
		}

		var selected = fonts.Where(font => IsVariantSelected(font, variants)).ToArray();
		// A manifest declaring none of the selected variants would otherwise preload nothing at all.
		return selected.Length > 0 ? selected : fonts;
	}

	internal static bool IsVariantSelected(FontInfo font, FontPreloadVariants variants)
	{
		if (font.FontStyle == FontStyle.Italic && !variants.HasFlag(FontPreloadVariants.Italic))
		{
			return false;
		}

		// Undefined means the manifest omitted the width, which describes a normal-width face.
		if (font.FontStretch is not (FontStretch.Normal or FontStretch.Undefined) && !variants.HasFlag(FontPreloadVariants.Condensed))
		{
			return false;
		}

		return variants.HasFlag(ToVariant(font.FontWeight));
	}

	private static FontPreloadVariants ToVariant(ushort weight) => weight switch
	{
		<= 100 => FontPreloadVariants.Thin,
		<= 200 => FontPreloadVariants.ExtraLight,
		<= 300 => FontPreloadVariants.Light,
		<= 400 => FontPreloadVariants.Normal,
		<= 500 => FontPreloadVariants.Medium,
		<= 600 => FontPreloadVariants.SemiBold,
		<= 700 => FontPreloadVariants.Bold,
		<= 800 => FontPreloadVariants.ExtraBold,
		_ => FontPreloadVariants.Black,
	};
}
