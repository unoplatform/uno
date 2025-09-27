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
using Uno.UI.Xaml.Media;

namespace Microsoft.UI.Xaml;

internal static partial class FontFamilyHelper
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

	/// <param name="uri">The URI of the font (ending with.ttf without .manifest)</param>
	public static async Task<bool> PreloadAllFontsInManifest(Uri uri)
	{
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

		var tasks = manifest.Fonts
			.Select(fontInfo => PreloadAsync(fontInfo.FamilyName, new FontWeight(fontInfo.FontWeight), fontInfo.FontStretch, fontInfo.FontStyle));

		return await Task.WhenAll(tasks).ContinueWith(combinedTask => combinedTask.Result.All(t => t));
	}
}
