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
using Uno.Foundation.Extensibility;
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
	private static readonly IFontFallbackService? _fontFallbackService = ApiExtensibility.CreateInstance<IFontFallbackService>(typeof(UnicodeText), out var service) ? service : null;

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
			using var stream = await AppDataUriEvaluator.ToStream(uri, CancellationToken.None);
			return SKTypeface.FromStream(stream);
		}
		catch (Exception e)
		{
			typeof(FontDetailsCache).LogError()?.Error($"Loading font from {uri} failed: {e}");
			return null;
		}
	}

	private static async Task<SKTypeface?> GetFontInternal(
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
			return await LoadTypefaceFromApplicationUriAsync(uri, weight, style, stretch);
		}
		else
		{
			if (_fontFallbackService is not null && await _fontFallbackService.GetTypefaceForFontName(name, weight, stretch, style) is { } typeface)
			{
				return typeface;
			}
			// FromFontFamilyName may return null: https://github.com/mono/SkiaSharp/issues/1058
			return SKTypeface.FromFamilyName(name, skWeight, skWidth, skSlant);
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
		var key = new FontEntry(name, skWeight, skWidth, skSlant);

		Task<SKTypeface?> typefaceTask;
		lock (_fontCacheGate)
		{
			if (!_fontCache.TryGetValue(key, out var nullableTask))
			{
				_fontCache[key] = nullableTask = GetFontInternal(name, weight, stretch, style);
			}
			typefaceTask = nullableTask;
		}

		var canChange = !typefaceTask.IsCompleted; // don't read from task.IsCompleted again, it could've changed
		var typeface = !canChange ? typefaceTask.Result : null;

		if (typeface == null)
		{
			if (typeof(Inline).Log().IsEnabled(LogLevel.Debug))
			{
				if (canChange)
				{
					typeof(Inline).Log().LogDebug($"{key} is still loading, using system default for now.");
				}
				else
				{
					typeof(Inline).Log().LogDebug($"{key} could not be found, using system default");
				}
			}

			typeface = SKTypeface.FromFamilyName(FeatureConfiguration.Font.DefaultTextFontFamily, skWeight, skWidth, skSlant)
						?? SKTypeface.FromFamilyName(null, skWeight, skWidth, skSlant)
						?? SKTypeface.FromFamilyName(null);
		}

		var details = FontDetails.Create(typeface, fontSize);
		var detailsTask = AwaitDetails(typefaceTask);
		return (details, detailsTask);

		async Task<FontDetails> AwaitDetails(Task<SKTypeface?> t)
		{
			SKTypeface? loadedTypeface = null;
			Exception? exception = null;

			try
			{
				loadedTypeface = await t;
			}
			catch (Exception e)
			{
				exception = e;
			}

			if (loadedTypeface is null)
			{
				if (typeof(FontDetailsCache).Log().IsEnabled(LogLevel.Error))
				{
					typeof(FontDetailsCache).Log().LogError($"Failed to load {key}", exception);
				}

				return details;
			}
			else
			{
				return FontDetails.Create(loadedTypeface, details.SKFontSize);
			}
		}
	});

	public static (FontDetails details, Task<FontDetails> loadedTask) GetFont(
		string? name,
		float fontSize,
		FontWeight weight,
		FontStretch stretch,
		FontStyle style) => _getFont(name, fontSize, weight, stretch, style);

	public static bool GetFontOrDefault(
		string? name,
		float fontSize,
		FontWeight weight,
		FontStretch stretch,
		FontStyle style,
		Action onFontLoaded,
		out FontDetails fontDetails)
	{
		var (tempFont, task) = GetFont(name, fontSize, weight, stretch, style);
		if (!task.IsCompleted)
		{
			async void Wait()
			{
				try
				{
					await task;
				}
				catch
				{
					// Ignore
				}

				onFontLoaded();
			}

			Wait();
		}

		var completedSuccessfully = task.IsCompletedSuccessfully;
		fontDetails = completedSuccessfully ? task.Result : tempFont;
		return completedSuccessfully;
	}
}
