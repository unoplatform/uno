#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI;
using Uno.UI.Composition.Drawing;
using Uno.UI.Xaml.Media;
using Windows.Storage;
using Windows.Storage.Helpers;
using Windows.UI.Text;
using Uno.Foundation.Extensibility;
using Uno.Helpers;

namespace Microsoft.UI.Xaml.Documents.TextFormatting;

/// <remarks>
/// Font <em>resolution</em> (family/style/bytes → <see cref="IFont"/>) is delegated to the backend's
/// <see cref="IFontProvider"/> (Skia or managed system-font lookup); this cache owns the URI/manifest byte
/// loading, per-request caching, and building the <see cref="FontDetails"/> (font handle + HarfBuzz font).
/// </remarks>
internal static class FontDetailsCache
{
	private readonly record struct FontEntry(
		string Name,
		int Weight,
		FontStretch Stretch,
		FontStyle Style,
		float FontSize);

	private static readonly Dictionary<FontEntry, Task<IFont?>> _fontCache = new();
	private static readonly object _fontCacheGate = new();

	// Bytes of loaded URI fonts, keyed by the manifest-resolved uri (which is the source uri when there is no
	// manifest). An IFont bakes the size, so a new size of an already-loaded file must resolve synchronously —
	// otherwise it briefly renders the default font (flicker) and the memoized sync FontDetails stays wrong.
	private static readonly Dictionary<string, byte[]> _fontDataByUri = new();
	// Manifest resolution per (source uri, weight, style, stretch): lets a warm font skip the async
	// .manifest probe so the whole resolution completes synchronously.
	private static readonly Dictionary<(string SourceUri, int Weight, FontStyle Style, FontStretch Stretch), Uri> _manifestUriCache = new();
	private static readonly object _fontDataGate = new();

	private static readonly IFontFallbackService? _fontFallbackService =
		FeatureConfiguration.Font.FallbackService
		?? (ApiExtensibility.CreateInstance<IFontFallbackService>(typeof(FontDetailsCache), out var service) ? service : null);

	private static IFontProvider FontProvider => global::Uno.UI.Composition.Drawing.FontProvider.Current;

	/// <summary>
	/// Resolves a <c>.manifest</c> sidecar to the weight/style-specific font file when present, returning the
	/// source uri unchanged otherwise. Resolutions are cached so a warm font skips the async package probe.
	/// </summary>
	private static async Task<Uri> ResolveManifestUriAsync(Uri uri, FontWeight weight, FontStyle style, FontStretch stretch)
	{
		var key = (uri.OriginalString, weight.Weight, style, stretch);
		lock (_fontDataGate)
		{
			if (_manifestUriCache.TryGetValue(key, out var cached))
			{
				return cached;
			}
		}

		var resolved = uri;
		try
		{
			var manifestUri = new Uri(uri.OriginalString + ".manifest");
			var path = Uri.UnescapeDataString(manifestUri.PathAndQuery).TrimStart('/');
			if (await StorageFileHelper.ExistsInPackage(path))
			{
				var manifestFile = await StorageFile.GetFileFromApplicationUriAsync(manifestUri);
				using var manifestStream = await manifestFile.OpenStreamForReadAsync();
				resolved = new Uri(FontManifestHelpers.GetFamilyNameFromManifest(manifestStream, weight, style, stretch));
			}
		}
		catch (Exception e)
		{
			if (typeof(FontDetailsCache).Log().IsEnabled(LogLevel.Error))
			{
				typeof(FontDetailsCache).Log().LogError($"Failed to load font manifest for {uri}: {e}");
			}
		}

		lock (_fontDataGate)
		{
			_manifestUriCache[key] = resolved;
		}

		return resolved;
	}

	private static async Task<byte[]?> LoadFontBytesAsync(Uri uri)
	{
		if (typeof(FontDetailsCache).Log().IsEnabled(LogLevel.Debug))
		{
			typeof(FontDetailsCache).Log().LogDebug($"Fetching font from {uri}");
		}

		try
		{
			using var stream = await AppDataUriEvaluator.ToStream(uri, CancellationToken.None);
			using var buffer = new MemoryStream();
			await stream.CopyToAsync(buffer, CancellationToken.None);
			return buffer.ToArray();
		}
		catch (Exception e)
		{
			typeof(FontDetailsCache).LogError()?.Error($"Loading font from {uri} failed: {e}");
			return null;
		}
	}

	private static async Task<IFont?> GetFontInternal(
		string name,
		FontWeight weight,
		FontStretch stretch,
		FontStyle style,
		float fontSize)
	{
		var manager = FontProvider;

		// A font family can be specified as "<file-uri>#<family name>". The part after '#' selects a specific
		// family within the file, which matters for TrueType/OpenType collections (.ttc/.otc).
		var hashIndex = name.IndexOf('#');
		string? familyNameHint = null;
		if (hashIndex > 0)
		{
			familyNameHint = name.Substring(hashIndex + 1);
			name = name.Substring(0, hashIndex);
		}

		if (Uri.TryCreate(name, UriKind.Absolute, out var uri))
		{
			// Both steps complete synchronously when warm, so a new size of an already-loaded font builds
			// its IFont without ever falling back to the default font.
			var resolvedUri = await ResolveManifestUriAsync(uri, weight, style, stretch);

			byte[]? cachedData;
			lock (_fontDataGate)
			{
				_fontDataByUri.TryGetValue(resolvedUri.OriginalString, out cachedData);
			}

			if (cachedData is null)
			{
				cachedData = await LoadFontBytesAsync(resolvedUri);
				if (cachedData is null)
				{
					return null;
				}

				lock (_fontDataGate)
				{
					_fontDataByUri[resolvedUri.OriginalString] = cachedData;
				}
			}

			return manager.CreateFont(cachedData, familyNameHint, weight, stretch, style, fontSize);
		}

		if (_fontFallbackService is { } fallbackService)
		{
			try
			{
				if (await fallbackService.GetFontStreamForFontFamily(name, weight, stretch, style) is { } fallbackStream)
				{
					using (fallbackStream)
					{
						return manager.CreateFont(ReadAllBytes(fallbackStream), familyNameHint, weight, stretch, style, fontSize);
					}
				}
			}
			catch (Exception e)
			{
				typeof(FontDetailsCache).LogError()?.Error($"Font fallback service threw resolving {name}", e);
			}
		}

		return manager.MatchFamily(name, weight, stretch, style, fontSize);
	}

	private static byte[] ReadAllBytes(Stream stream)
	{
		if (stream is MemoryStream ms)
		{
			return ms.ToArray();
		}

		using var buffer = new MemoryStream();
		stream.CopyTo(buffer);
		return buffer.ToArray();
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

		var key = new FontEntry(name, weight.Weight, stretch, style, fontSize);

		Task<IFont?> fontTask;
		lock (_fontCacheGate)
		{
			if (!_fontCache.TryGetValue(key, out var nullableTask))
			{
				_fontCache[key] = nullableTask = GetFontInternal(name, weight, stretch, style, fontSize);
			}
			fontTask = nullableTask;
		}

		var canChange = !fontTask.IsCompleted; // don't read from task.IsCompleted again, it could've changed
		var font = !canChange ? fontTask.Result : null;

		if (font is null)
		{
			if (typeof(Inline).Log().IsEnabled(LogLevel.Debug))
			{
				typeof(Inline).Log().LogDebug(canChange
					? $"{key} is still loading, using system default for now."
					: $"{key} could not be found, using system default");
			}

			font = FontProvider.MatchFamily(FeatureConfiguration.Font.DefaultTextFontFamily, weight, stretch, style, fontSize)
				?? FontProvider.GetDefaultFont(weight, stretch, style, fontSize);
		}

		var details = FontDetails.Create(font, fontSize);
		var detailsTask = AwaitDetails(fontTask, details, key, fontSize);
		return (details, detailsTask);
	});

	private static async Task<FontDetails> AwaitDetails(Task<IFont?> task, FontDetails fallback, FontEntry key, float fontSize)
	{
		IFont? loadedFont = null;
		Exception? exception = null;

		try
		{
			loadedFont = await task;
		}
		catch (Exception e)
		{
			exception = e;
		}

		if (loadedFont is null)
		{
			if (typeof(FontDetailsCache).Log().IsEnabled(LogLevel.Error))
			{
				typeof(FontDetailsCache).Log().LogError($"Failed to load {key}", exception);
			}

			return fallback;
		}

		return FontDetails.Create(loadedFont, fontSize);
	}

	public static (FontDetails details, Task<FontDetails> loadedTask) GetFont(
		string? name,
		float fontSize,
		FontWeight weight,
		FontStretch stretch,
		FontStyle style) => _getFont(name, fontSize, weight, stretch, style);

	public static async Task<FontDetails?> GetFontForCodepoint(
		int codepoint,
		float fontSize,
		FontWeight weight,
		FontStretch stretch,
		FontStyle style)
	{
		if (_fontFallbackService is { } fallbackService)
		{
			string? fallbackServiceResult = null;
			try
			{
				fallbackServiceResult = await fallbackService.GetFontFamilyForCodepoint(codepoint);
			}
			catch (Exception e)
			{
				typeof(UnicodeText).LogError()?.Error($"Font fallback service failed to get font for codepoint U+{codepoint:X4}", e);
			}

			if (fallbackServiceResult is null)
			{
				return null;
			}

			return await GetFont(fallbackServiceResult, fontSize, weight, stretch, style).loadedTask;
		}

		return null;
	}
}
