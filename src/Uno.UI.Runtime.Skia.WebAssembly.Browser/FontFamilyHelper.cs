using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Documents.TextFormatting;
using Microsoft.UI.Xaml.Media;
using SkiaSharp;
using Uno.Foundation.Logging;
using Windows.Storage;

namespace Uno.UI.Xaml.Media;

public static class FontFamilyHelper
{
	private static async Task<SKTypeface> LoadTypefaceAsync(Uri uri)
	{
		var storageFile = await StorageFile.GetFileFromApplicationUriAsync(uri);
		var stream = await storageFile.OpenStreamForReadAsync();
		return SKTypeface.FromStream(stream);
	}

	/// <summary>
	/// Pre-loads a font to minimize loading time and prevent potential text re-layouts.
	/// </summary>
	/// <returns>True if the font loaded successfully, otherwise false.</returns>
	public static Task<bool> PreloadAsync(FontFamily family)
	{
		Task<SKTypeface> task;
		if (Uri.TryCreate(family.Source, UriKind.Absolute, out var uri) && uri.Scheme == "ms-appx")
		{
			task = LoadTypefaceAsync(uri);

			// We won't find the details of the typeface until it has already loaded (unlike FontDetailsCache),
			// so unfortunately, we can't register a Task with FontDetailsCache (again, unlike FontDetailsCache) and
			// instead we register the typeface directly after it has already been loaded.
			return task.ContinueWith(t => t.IsCompletedSuccessfully && FontDetailsCache.RegisterTypeface(t.Result));
		}
		else
		{
			if (typeof(FontFamilyHelper).Log().IsEnabled(LogLevel.Error))
			{
				typeof(FontFamilyHelper).Log().LogError("Font preloading on Skia Wasm only supports ms-appx");
			}

			return Task.FromResult(false);
		}
	}

	/// <summary>
	/// Pre-loads a font to minimize loading time and prevent potential text re-layouts.
	/// </summary>
	/// <returns>True if the font loaded successfully, otherwise false.</returns>
	public static Task<bool> PreloadAsync(string familyName)
		=> PreloadAsync(new FontFamily(familyName));
}
