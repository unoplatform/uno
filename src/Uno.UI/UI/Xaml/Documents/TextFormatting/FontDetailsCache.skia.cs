#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SkiaSharp;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI;
using Uno.UI.Xaml.Media;
using Windows.Storage;
using Windows.Storage.Helpers;
using Windows.UI.Text;
using Uno.Helpers;
using SKFontStyleWidth = SkiaSharp.SKFontStyleWidth;

namespace Microsoft.UI.Xaml.Documents.TextFormatting;

/// <remarks>
/// Skia uses the word "typeface" to mean a specific style of a typographic family (e.g. OpenSans with Bold weight, Normal width and Italic slant)
/// and the word "font" to mean a typeface + a specific font size. This is different from the literature where "typeface"
/// means a typographic family (e.g. OpenSans or Segoe UI) and "font" means what Skia means by "typeface".
/// We try to use Skia's wording for code and the accurate wording for logging.
/// </remarks>
internal static class FontDetailsCache
{
	private readonly record struct FontEntry(
		string Name,
		SKFontStyleWeight Weight,
		SKFontStyleWidth Width,
		SKFontStyleSlant Slant);

	private static readonly Dictionary<FontEntry, Task<SKTypeface?>> _fontCache = new();
	private static readonly object _fontCacheGate = new();

	private static async Task<SKTypeface?> LoadTypefaceFromApplicationUriAsync(Uri uri, FontWeight weight, FontStyle style, FontStretch stretch)
	{
		try
		{
			var manifestUri = new Uri(uri.OriginalString + ".manifest");
			var path = Uri.UnescapeDataString(manifestUri.PathAndQuery).TrimStart('/');
			if (await StorageFileHelper.ExistsInPackage(path))
			{
				var manifestFile = await StorageFile.GetFileFromApplicationUriAsync(manifestUri);
				using var manifestStream = await manifestFile.OpenStreamForReadAsync();
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

		try
		{
			var file = await StorageFile.GetFileFromApplicationUriAsync(uri);
			using var stream = await file.OpenStreamForReadAsync();
			return stream is null ? null : SKTypeface.FromStream(stream);
		}
		catch (Exception e)
		{
			if (typeof(FontDetailsCache).Log().IsEnabled(LogLevel.Error))
			{
				typeof(FontDetailsCache).Log().LogError($"Failed to load font {uri}: {e}");
			}

			throw;
		}
	}

	private static Task<SKTypeface?> GetFontInternal(
		string name,
		FontWeight weight,
		FontStretch stretch,
		FontStyle style)
	{
		var skWeight = weight.ToSkiaWeight();
		var skWidth = stretch.ToSkiaWidth();
		var skSlant = style.ToSkiaSlant();

		var hashIndex = name.IndexOf('#');
		if (hashIndex > 0)
		{
			name = name.Substring(0, hashIndex);
		}

		if (Uri.TryCreate(name, UriKind.Absolute, out var uri))
		{
			return LoadTypefaceFromApplicationUriAsync(uri, weight, style, stretch);
		}
		else
		{
			// FromFontFamilyName may return null: https://github.com/mono/SkiaSharp/issues/1058
			return Task.FromResult<SKTypeface?>(SKTypeface.FromFamilyName(name, skWeight, skWidth, skSlant));
		}
	}

	private static readonly Func<string?, float, FontWeight, FontStretch, FontStyle, (FontDetails details, Task<FontDetails> loadedTask)> _getFont = FuncMemoizeExtensions.AsLockedMemoized((
		string? name,
		float fontSize,
		FontWeight weight,
		FontStretch stretch,
		FontStyle style) =>
	{
		if (name == null || string.Equals(name, "XamlAutoFontFamily", StringComparison.OrdinalIgnoreCase))
		{
			name = FeatureConfiguration.Font.DefaultTextFontFamily;
		}

		var (skWeight, skWidth, skSlant) = (weight.ToSkiaWeight(), stretch.ToSkiaWidth(), style.ToSkiaSlant());
		var fontCacheKey = new FontEntry(name, skWeight, skWidth, skSlant);

		var typefaceTask = GetTypefaceTask(name, weight, stretch, style, fontCacheKey);

		var canChange = !typefaceTask.IsCompleted; // don't read from task.IsCompleted again, it could've changed
		var wasSuccessfullyLoaded = typefaceTask.IsCompletedSuccessfully;

		var typeface = !canChange && wasSuccessfullyLoaded ? typefaceTask.Result : null;

		if (typeface == null)
		{
			if (typeof(Inline).Log().IsEnabled(LogLevel.Debug))
			{
				if (canChange)
				{
					typeof(Inline).Log().LogDebug($"{fontCacheKey} is still loading, using system default for now.");
				}
				else
				{
					typeof(Inline).Log().LogDebug($"{fontCacheKey} could not be found, using system default");
				}
			}

			// Try to fall back to default font family (which could be a ms-appx:/// Uri).
			if (name != FeatureConfiguration.Font.DefaultTextFontFamily)
			{
				var fallbackFontCacheKey = new FontEntry(FeatureConfiguration.Font.DefaultTextFontFamily, skWeight, skWidth, skSlant);
				Task<SKTypeface?> fallbackTypefaceTask = GetTypefaceTask(FeatureConfiguration.Font.DefaultTextFontFamily, weight, stretch, style, fallbackFontCacheKey);
				canChange = !fallbackTypefaceTask.IsCompleted;
				wasSuccessfullyLoaded = fallbackTypefaceTask.IsCompletedSuccessfully;

				typeface = !canChange && wasSuccessfullyLoaded ? fallbackTypefaceTask.Result : null;
			}

			// If we still don't have a typeface, we try to load directly via SKTypeface.
			if (typeface is null)
			{
				typeface = SKTypeface.FromFamilyName(FeatureConfiguration.Font.DefaultTextFontFamily, skWeight, skWidth, skSlant)
							?? SKTypeface.FromFamilyName(null, skWeight, skWidth, skSlant)
							?? SKTypeface.FromFamilyName(null);
			}
		}

		var details = FontDetails.Create(typeface, fontSize);

		var detailsTask = typefaceTask.ContinueWith(t =>
		{
			var loadedTypeface = t.IsCompletedSuccessfully ? t.Result : null;

			if (loadedTypeface is null)
			{
				if (typeof(FontDetailsCache).Log().IsEnabled(LogLevel.Error))
				{
					if (typeof(FontDetailsCache).Log().IsEnabled(LogLevel.Error))
					{
						typeof(FontDetailsCache).Log().LogError($"Failed to load {fontCacheKey}", t.Exception);
					}
				}

				return details;
			}
			else
			{
				return FontDetails.Create(loadedTypeface, details.SKFontSize);
			}
		});
		return (details, detailsTask);
	});

	private static Task<SKTypeface?> GetTypefaceTask(string name, FontWeight weight, FontStretch stretch, FontStyle style, FontEntry key)
	{
		Task<SKTypeface?> typefaceTask;
		lock (_fontCacheGate)
		{
			if (!_fontCache.TryGetValue(key, out var nullableTask))
			{
				_fontCache[key] = nullableTask = GetFontInternal(name, weight, stretch, style);
			}
			typefaceTask = nullableTask;
		}

		return typefaceTask;
	}

	public static (FontDetails details, Task<SKTypeface?> loadedTask) GetFont(
		string? name,
		float fontSize,
		FontWeight weight,
		FontStretch stretch,
		FontStyle style) => _getFont(name, fontSize, weight, stretch, style);
}
