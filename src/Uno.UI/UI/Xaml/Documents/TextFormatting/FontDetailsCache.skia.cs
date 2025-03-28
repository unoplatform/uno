#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using SkiaSharp;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI;
using Uno.UI.Dispatching;
using Uno.UI.Xaml.Media;
using Windows.Storage;
using Windows.Storage.Helpers;
using Windows.UI.Text;

namespace Windows.UI.Xaml.Documents.TextFormatting;

internal static class FontDetailsCache
{
	private record struct FontCacheEntry(
		string? Name,
		float FontSize,
		FontWeight Weight,
		FontStretch Stretch,
		FontStyle Style);

	private static Dictionary<FontCacheEntry, FontDetails> _fontCache = new();
	private static object _fontCacheGate = new();

	internal static void OnFontLoaded(string font, SKTypeface? typeface)
	{
		lock (_fontCacheGate)
		{
			foreach (var (key, details) in _fontCache)
			{
				if (key.Name == font)
				{
					if (typeface is null)
					{
						// font load failed.
						details.LoadFailed();
					}
					else
					{
						details.Update(typeface);
					}
				}
			}
		}
	}

	private static async Task<SKTypeface?> LoadTypefaceFromApplicationUriAsync(Uri uri, FontWeight weight, FontStyle style, FontStretch stretch)
	{
		try
		{
			var manifestUri = new Uri(uri.OriginalString + ".manifest");
			var path = Uri.UnescapeDataString(manifestUri.PathAndQuery).TrimStart('/');
			if (await StorageFileHelper.ExistsInPackage(path))
			{
				var manifestFile = await StorageFile.GetFileFromApplicationUriAsync(manifestUri);
				var manifestStream = await manifestFile.OpenStreamForReadAsync();
				uri = new Uri(FontManifestHelpers.GetFamilyNameFromManifest(manifestStream, weight, style, stretch));
			}
		}
		catch (Exception e)
		{
			if (typeof(FontDetailsCache).Log().IsEnabled(LogLevel.Error))
			{
				typeof(FontDetailsCache).Log().LogError($"Failed to load font manifest for {uri}: {e}");
			}
		}

		if (typeof(FontDetailsCache).Log().IsEnabled(LogLevel.Debug))
		{
			typeof(FontDetailsCache).Log().LogDebug($"Fetching font from {uri}");
		}

		var file = await StorageFile.GetFileFromApplicationUriAsync(uri);
		var stream = await file.OpenStreamForReadAsync();
		return stream is null ? null : SKTypeface.FromStream(stream);
	}

	private static FontDetails GetFontInternal(
		string name,
		float fontSize,
		FontWeight weight,
		FontStretch stretch,
		FontStyle style)
	{
		var skWeight = weight.ToSkiaWeight();
		var skWidth = stretch.ToSkiaWidth();
		var skSlant = style.ToSkiaSlant();

		SKTypeface? skTypeFace;
		bool temporaryDefaultFont = false;
		var originalName = name;
		var hashIndex = name.IndexOf('#');
		if (hashIndex > 0)
		{
			name = name.Substring(0, hashIndex);
		}

		if (Uri.TryCreate(name, UriKind.Absolute, out var uri) && uri.IsLocalResource())
		{
			var task = LoadTypefaceFromApplicationUriAsync(uri, weight, style, stretch);
			if (task.IsCompleted)
			{
				if (task.IsCompletedSuccessfully)
				{
					skTypeFace = task.Result;
				}
				else
				{
					// Load failed.
					OnFontLoaded(originalName, null);
					skTypeFace = null;
				}
			}
			else
			{
				temporaryDefaultFont = true;
				skTypeFace = null;
				task.ContinueWith(task =>
				{
					NativeDispatcher.Main.Enqueue(() =>
					{
						try
						{
							if (task.IsCompletedSuccessfully)
							{
								OnFontLoaded(originalName, task.Result);
							}
							else
							{
								// Load failed.
								OnFontLoaded(originalName, null);
							}
						}
						catch (Exception e)
						{
							if (typeof(FontDetailsCache).Log().IsEnabled(LogLevel.Error))
							{
								typeof(FontDetailsCache).Log().LogError($"Failed to load font {name} from ms-appx: {e}");
							}
						}
					});
				});
			}
		}
		else
		{
			// FromFontFamilyName may return null: https://github.com/mono/SkiaSharp/issues/1058
			skTypeFace = SKTypeface.FromFamilyName(name, skWeight, skWidth, skSlant);
		}

		if (skTypeFace == null)
		{
			if (typeof(Inline).Log().IsEnabled(LogLevel.Warning))
			{
				typeof(Inline).Log().LogWarning($"The font {name} could not be found, using system default");
			}

			skTypeFace = SKTypeface.FromFamilyName(FeatureConfiguration.Font.DefaultTextFontFamily, skWeight, skWidth, skSlant)
				?? SKTypeface.FromFamilyName(null, skWeight, skWidth, skSlant)
				?? SKTypeface.FromFamilyName(null);
		}

		return FontDetails.Create(skTypeFace, fontSize, temporaryDefaultFont);
	}

	public static FontDetails GetFont(
		string? name,
		float fontSize,
		FontWeight weight,
		FontStretch stretch,
		FontStyle style)
	{
		if (name == null || string.Equals(name, "XamlAutoFontFamily", StringComparison.OrdinalIgnoreCase))
		{
			name = FeatureConfiguration.Font.DefaultTextFontFamily;
		}

		var key = new FontCacheEntry(name, fontSize, weight, stretch, style);

		lock (_fontCacheGate)
		{
			if (!_fontCache.TryGetValue(key, out var value))
			{
				_fontCache[key] = value = GetFontInternal(name, fontSize, weight, stretch, style);
			}
			return value;
		}
	}
}
