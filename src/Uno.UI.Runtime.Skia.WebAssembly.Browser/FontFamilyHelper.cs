using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Documents.TextFormatting;
using Microsoft.UI.Xaml.Media;
using Uno.Foundation.Logging;
using Windows.Storage;

namespace Uno.UI.Xaml.Media;

public static class FontFamilyHelper
{
	/// <summary>
	/// Pre-loads a font to minimize loading time and prevent potential text re-layouts.
	/// </summary>
	/// <returns>True is the font loaded successfully, otherwise false.</returns>
	public static Task<bool> PreloadAsync(FontFamily family)
	{
		if (Uri.TryCreate(family.Source, UriKind.Absolute, out var uri) && uri.Scheme == "ms-appx")
		{
			var task = StorageFile.GetFileFromApplicationUriAsync(uri).AsTask().ContinueWith(task =>
			{
				var storageFile = task.Result;
				return storageFile.OpenStreamForReadAsync().AsTask();
			});

			// TODO: Handle task failures. On failures, we should call OnFontLoaded(name, null)
			if (task.IsCompleted && task.Result.IsCompleted)
			{
				// The font is loaded synchronously. This is very unlikely (impossible?) to happen on Wasm?
				var stream = task.Result.Result;
				FontDetailsCache.OnFontLoaded(family.Source, stream);
			}
			else
			{
				task.ContinueWith(task =>
				{
					task.Result.ContinueWith(task =>
					{
						task.ContinueWith(tas =>
						{
							FontDetailsCache.OnFontLoaded(family.Source, task.Result);
						});
					});
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
