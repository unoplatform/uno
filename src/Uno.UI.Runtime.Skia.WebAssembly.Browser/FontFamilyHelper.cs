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
	/// <returns>True is the font loaded successfully, otherwise false.</returns>
	public static Task<bool> PreloadAsync(FontFamily family)
	{
		if (Uri.TryCreate(family.Source, UriKind.Absolute, out var uri) && uri.Scheme == "ms-appx")
		{
			var task = LoadTypefaceAsync(uri);

			if (task.IsCompleted)
			{
				if (task.IsCompletedSuccessfully)
				{
					// The font is loaded synchronously. This is very unlikely (impossible?) to happen on Wasm?
					var stream = task.Result;
					FontDetailsCache.OnFontLoaded(family.Source, stream);
				}
				else
				{
					if (typeof(FontFamilyHelper).Log().IsEnabled(LogLevel.Error))
					{
						typeof(FontFamilyHelper).Log().LogError($"Font {family.Source} could not be loaded. {task.Exception}");
					}
					FontDetailsCache.OnFontLoaded(family.Source, null);
				}
			}
			else
			{
				task.ContinueWith(task =>
				{
					if (task.IsCompletedSuccessfully)
					{
						var stream = task.Result;
						FontDetailsCache.OnFontLoaded(family.Source, stream);
					}
					else
					{
						if (typeof(FontFamilyHelper).Log().IsEnabled(LogLevel.Error))
						{
							typeof(FontFamilyHelper).Log().LogError($"Font {family.Source} could not be loaded. {task.Exception}");
						}
						FontDetailsCache.OnFontLoaded(family.Source, null);
					}
				});
			}

			return Task.FromResult(true);
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
	/// <returns>True is the font loaded successfully, otherwise false.</returns>
	public static Task<bool> PreloadAsync(string familyName)
		=> PreloadAsync(new FontFamily(familyName));

}
