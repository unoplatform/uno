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

	// FontSize is continuous, so an animation requests an unbounded number of entries. Two generations keep the cache
	// O(1) and bounded: a full generation becomes the previous one, and whatever is still in use moves back on its next
	// lookup. Dropped fonts are only re-created; the typefaces behind them stay with the font provider.
	private const int MaxFontsPerGeneration = 512;

	private static Dictionary<FontEntry, (FontDetails details, Task<FontDetails> loadedTask)> _fonts = new();
	private static Dictionary<FontEntry, (FontDetails details, Task<FontDetails> loadedTask)> _previousFonts = new();
	private static readonly object _fontCacheGate = new();

	// Loaded URI font files, keyed by the manifest-resolved uri (which is the source uri when there is no
	// manifest). An IFont bakes the size, so a new size of an already-loaded file must resolve synchronously —
	// otherwise it briefly renders the default font (flicker) and the memoized sync FontDetails stays wrong.
	private static readonly Dictionary<string, IFontFile> _fontFileByUri = new();
	// Manifest resolution per (source uri, weight, style, stretch): lets a warm font skip the async
	// .manifest probe so the whole resolution completes synchronously.
	private static readonly Dictionary<(string SourceUri, int Weight, FontStyle Style, FontStretch Stretch), Uri> _manifestUriCache = new();
	private static readonly object _fontDataGate = new();

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

	private static async Task<IFontFile?> LoadFontFileAsync(Uri uri)
	{
		if (typeof(FontDetailsCache).Log().IsEnabled(LogLevel.Debug))
		{
			typeof(FontDetailsCache).Log().LogDebug($"Fetching font from {uri}");
		}

		try
		{
			using var stream = await AppDataUriEvaluator.ToStream(uri, CancellationToken.None);
			// LoadFontFile reads synchronously; keep that file I/O off the UI thread, as the async copy did.
			return await Task.Run(() => FontProvider.LoadFontFile(stream));
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

			IFontFile? fontFile;
			lock (_fontDataGate)
			{
				_fontFileByUri.TryGetValue(resolvedUri.OriginalString, out fontFile);
			}

			if (fontFile is null)
			{
				fontFile = await LoadFontFileAsync(resolvedUri);
				if (fontFile is null)
				{
					return null;
				}

				lock (_fontDataGate)
				{
					// Concurrent first loads of one file (e.g. several preloaded styles) must share one copy.
					if (_fontFileByUri.TryGetValue(resolvedUri.OriginalString, out var raced))
					{
						fontFile.Dispose();
						fontFile = raced;
					}
					else
					{
						_fontFileByUri[resolvedUri.OriginalString] = fontFile;
					}
				}
			}

			return fontFile.CreateFont(familyNameHint, weight, stretch, style, fontSize);
		}

		// The default family may be one the platform doesn't have (Segoe UI outside Windows): the provider's default font
		// then stands in for it, rather than the default text font being reported as failing to load.
		return await manager.MatchFamilyAsync(name, weight, stretch, style, fontSize)
			?? (string.Equals(name, FeatureConfiguration.Font.DefaultTextFontFamily, StringComparison.OrdinalIgnoreCase)
				? manager.GetDefaultFont(weight, stretch, style, fontSize)
				: null);
	}

	private static (FontDetails details, Task<FontDetails> loadedTask) CreateFont(FontEntry key, FontWeight weight, FontStretch stretch, FontStyle style)
	{
		var fontTask = GetFontInternal(key.Name, weight, stretch, style, key.FontSize);

		var canChange = !fontTask.IsCompleted; // don't read from task.IsCompleted again, it could've changed
		var font = !canChange ? fontTask.Result : null;

		if (font is not null)
		{
			var details = FontDetails.Create(font, key.FontSize);
			return (details, AwaitDetails(fontTask, null, details, key, key.FontSize));
		}

		if (typeof(Inline).Log().IsEnabled(LogLevel.Debug))
		{
			typeof(Inline).Log().LogDebug(canChange
				? $"{key} is still loading, using the default text font for now."
				: $"{key} could not be found, using the default text font");
		}

		// The default text font is often a URI-backed font that only the loader resolves: MatchFamily finds nothing
		// and this would end on the provider's built-in default (Skia's embedded monospace font on WASM).
		if (GetDefaultTextFont(key, weight, stretch, style) is { } defaultFont)
		{
			// The entry's details stay its placeholder once it has loaded, so take the loaded font when it is there.
			var details = defaultFont.loadedTask.IsCompletedSuccessfully ? defaultFont.loadedTask.Result : defaultFont.details;
			return (details, AwaitDetails(fontTask, defaultFont.loadedTask, details, key, key.FontSize));
		}

		var fallback = FontDetails.Create(
			FontProvider.MatchFamily(FeatureConfiguration.Font.DefaultTextFontFamily, weight, stretch, style, key.FontSize)
				?? FontProvider.GetDefaultFont(weight, stretch, style, key.FontSize),
			key.FontSize);
		return (fallback, AwaitDetails(fontTask, null, fallback, key, key.FontSize));
	}

	/// <summary>
	/// The default text font, for a family that could not be resolved, or null when the family is the default text
	/// font itself. Called with <see cref="_fontCacheGate"/> held, which is reentrant.
	/// </summary>
	private static (FontDetails details, Task<FontDetails> loadedTask)? GetDefaultTextFont(FontEntry key, FontWeight weight, FontStretch stretch, FontStyle style)
	{
		var defaultName = FeatureConfiguration.Font.DefaultTextFontFamily;
		if (string.IsNullOrEmpty(defaultName) || string.Equals(key.Name, defaultName, StringComparison.OrdinalIgnoreCase))
		{
			return null;
		}

		return GetFont(defaultName, key.FontSize, weight, stretch, style);
	}

	private static async Task<FontDetails> AwaitDetails(Task<IFont?> task, Task<FontDetails>? defaultTask, FontDetails fallback, FontEntry key, float fontSize)
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
			// A family name the platform doesn't have quietly gets the default text font, but a provider that threw or a
			// font file that didn't load is still reported.
			if ((defaultTask is null || exception is not null || Uri.TryCreate(key.Name, UriKind.Absolute, out _))
				&& typeof(FontDetailsCache).Log().IsEnabled(LogLevel.Error))
			{
				typeof(FontDetailsCache).Log().LogError($"Failed to load {key}", exception);
			}

			return defaultTask is not null ? await defaultTask : fallback;
		}

		return FontDetails.Create(loadedFont, fontSize);
	}

	public static (FontDetails details, Task<FontDetails> loadedTask) GetFont(
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

		var key = new FontEntry(name, weight.Weight, stretch, style, fontSize);

		lock (_fontCacheGate)
		{
			if (_fonts.TryGetValue(key, out var font))
			{
				return font;
			}

			if (!_previousFonts.Remove(key, out font))
			{
				font = CreateFont(key, weight, stretch, style);
			}

			if (_fonts.Count >= MaxFontsPerGeneration)
			{
				_previousFonts = _fonts;
				_fonts = new();
			}

			_fonts[key] = font;
			return font;
		}
	}

	public static Task<FontDetails?> GetFontForCodepoint(
		int codepoint,
		float fontSize,
		FontWeight weight,
		FontStretch stretch,
		FontStyle style)
	{
		// The provider resolves the codepoint through the installed fonts first, then its platform fallback (e.g. the
		// browser's on-demand Noto fetch). An installed match completes synchronously, so the caller's IsCompleted
		// fast-path keeps working; only a fetch defers.
		var match = FontProvider.MatchCharacterAsync(codepoint, weight, stretch, style, fontSize);
		if (match.IsCompletedSuccessfully)
		{
			var font = match.Result;
			return Task.FromResult(font is null ? null : FontDetails.Create(font, fontSize));
		}

		return Awaited(match, fontSize);

		static async Task<FontDetails?> Awaited(ValueTask<IFont?> match, float fontSize)
		{
			IFont? font;
			try
			{
				font = await match;
			}
			catch (Exception e)
			{
				typeof(UnicodeText).LogError()?.Error("Font provider failed to resolve a fallback for a codepoint", e);
				return null;
			}

			return font is null ? null : FontDetails.Create(font, fontSize);
		}
	}
}
