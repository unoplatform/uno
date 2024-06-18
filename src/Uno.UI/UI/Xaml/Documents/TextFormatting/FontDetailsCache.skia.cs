#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using HarfBuzzSharp;
using SkiaSharp;
using Uno.Foundation.Logging;
using Uno.UI;
using Uno.UI.Xaml;
using Uno.UI.Xaml.Media;
using Windows.ApplicationModel;
using Windows.Storage;
using Windows.Storage.Helpers;
using Windows.UI.Text;

namespace Microsoft.UI.Xaml.Documents.TextFormatting;

internal static class FontDetailsCache
{
	private record struct FontCacheEntry(
		string? Name,
		float FontSize,
		FontWeight Weight,
		FontStretch Stretch,
		FontStyle Style);

	private static readonly ConcurrentDictionary<string, SKTypeface?> _typefaceCache = new();
	private static Dictionary<FontCacheEntry, FontDetails> _fontCache = new();
	private static object _fontCacheGate = new();

	private static JsonSerializerOptions _options = new JsonSerializerOptions()
	{
		AllowTrailingCommas = true,
		PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
		ReadCommentHandling = JsonCommentHandling.Skip,
		Converters =
		{
			new JsonStringEnumConverter(),
		},
		TypeInfoResolver = FontManifestSerializerContext.Default,
	};

	internal static void OnFontLoaded(string font, SKTypeface? typeface)
	{
		_typefaceCache[font] = typeface;
		lock (_fontCacheGate)
		{
			foreach (var key in _fontCache.Keys)
			{
				if (key.Name == font)
				{
					if (_fontCache.TryGetValue(key, out var details))
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
	}

	private static string GetFamilyNameFromManifest(Stream jsonStream, FontWeight weight, FontStyle style, FontStretch stretch)
	{
		/*
		 {
			"fonts": [
				{
					"font_style": "normal",
					"font_weight": 400,
					"font_stretch": "normal",
					"family_name": "ms-appx:///path/to/ExampleFontFamily-Regular.ttf"
				},
				{
					"font_style": "italic",
					"font_weight": "Normal",
					"font_stretch": "normal",
					"family_name": "ms-appx:///path/to/ExampleFontFamily-Italic.ttf"
				},
				{
					"font_style": "normal",
					"font_weight": 700,
					"font_stretch": "normal",
					"family_name": "ms-appx:///path/to/ExampleFontFamily-Bold.ttf"
				}
			]
		} 
		 */

		var manifest = JsonSerializer.Deserialize<FontManifest>(jsonStream, _options);
		if (manifest?.Fonts is null || manifest.Fonts.Length == 0)
		{
			throw new ArgumentException("Font manifest file is incorrect.");
		}

		var bestSoFar = manifest.Fonts[0];
		for (int i = 1; i < manifest.Fonts.Length; i++)
		{
			var candidateMatch = manifest.Fonts[i];
			if (candidateMatch.FontWeight != bestSoFar.FontWeight && Math.Abs(candidateMatch.FontWeight - weight.Weight) < Math.Abs(bestSoFar.FontWeight - weight.Weight))
			{
				// candidateMatch is a better match than bestSoFar. So it's now our new bestSoFar.
				bestSoFar = candidateMatch;
			}
			else if (candidateMatch.FontStyle != bestSoFar.FontStyle && candidateMatch.FontStyle == style)
			{
				// The current bestSoFar
				bestSoFar = candidateMatch;
			}
			else if (stretch != FontStretch.Undefined && candidateMatch.FontStretch != bestSoFar.FontStretch && Math.Abs(candidateMatch.FontStretch - stretch) < Math.Abs(bestSoFar.FontStretch - stretch))
			{
				// candidateMatch is a better match than bestSoFar. So it's now our new bestSoFar.
				bestSoFar = candidateMatch;
			}
		}

		return bestSoFar.FamilyName;
	}

	private static async Task<SKTypeface?> LoadTypefaceFromApplicationUriAsync(Uri uri, FontWeight weight, FontStyle style, FontStretch stretch)
	{
		try
		{
			// TODO (This comment should be resolved during code review):
			// Should the user be responsible for adding ".manifest" to the ms-appx path himself?
			// The benefit of the user doing so is that we will not have to first check if a manifest exists.
			var path = Uri.UnescapeDataString(uri.PathAndQuery).TrimStart('/');
			if (await StorageFileHelper.ExistsInPackage(path))
			{
				var manifestUri = new Uri(uri.OriginalString + ".manifest");
				var manifestFile = await StorageFile.GetFileFromApplicationUriAsync(manifestUri);
				var manifestStream = await manifestFile.OpenStreamForReadAsync();
				uri = new Uri(GetFamilyNameFromManifest(manifestStream, weight, style, stretch));
			}
		}
		catch
		{
			if (typeof(FontDetailsCache).Log().IsEnabled(LogLevel.Error))
			{
				typeof(FontDetailsCache).Log().LogError($"Failed to load font manifest for {uri}");
			}
		}

		var file = await StorageFile.GetFileFromApplicationUriAsync(uri);
		var stream = await file.OpenStreamForReadAsync();
		return stream is null ? null : SKTypeface.FromStream(stream);
	}

	private static FontDetails GetFontInternal(
		string? name,
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

		if (name == null || string.Equals(name, "XamlAutoFontFamily", StringComparison.OrdinalIgnoreCase))
		{
			name = FeatureConfiguration.Font.DefaultTextFontFamily;
		}

		if (Uri.TryCreate(name, UriKind.Absolute, out var uri) && uri.Scheme == "ms-appx")
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
					OnFontLoaded(name, null);
					skTypeFace = null;
				}
			}
			else
			{
				temporaryDefaultFont = true;
				skTypeFace = null;
				task.ContinueWith(task =>
				{
					try
					{
						if (task.IsCompletedSuccessfully)
						{
							OnFontLoaded(name, task.Result);
						}
						else
						{
							// Load failed.
							OnFontLoaded(name, null);
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
