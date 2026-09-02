#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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

	// Standard OpenType variation axes that map to the WinUI font properties.
	private static readonly SKFourByteTag WeightAxis = SKFourByteTag.Parse("wght");
	private static readonly SKFourByteTag WidthAxis = SKFourByteTag.Parse("wdth");
	private static readonly SKFourByteTag ItalicAxis = SKFourByteTag.Parse("ital");
	private static readonly SKFourByteTag SlantAxis = SKFourByteTag.Parse("slnt");

	private static readonly Dictionary<FontEntry, Task<SKTypeface?>> _fontCache = new();
	private static readonly object _fontCacheGate = new();

	// Data of loaded manifest-free embedded fonts, keyed by source uri. A variable font is a single file
	// shared by every weight, so caching the data lets a new weight be resolved synchronously instead of
	// reloading the file asynchronously (which briefly renders the default font — flicker when animating
	// FontWeight).
	private static readonly Dictionary<string, SKData> _fontDataByUri = new();
	private static readonly object _fontDataGate = new();
	private static readonly IFontFallbackService? _fontFallbackService =
		FeatureConfiguration.Font.FallbackService
		?? (ApiExtensibility.CreateInstance<IFontFallbackService>(typeof(FontDetailsCache), out var service) ? service : null);

	private static async Task<SKTypeface?> LoadTypefaceFromApplicationUriAsync(Uri uri, FontWeight weight, FontStyle style, FontStretch stretch, string? familyNameHint)
	{
		var sourceUri = uri.OriginalString;
		var usedManifest = false;
		try
		{
			var manifestUri = new Uri(uri.OriginalString + ".manifest");
			var path = Uri.UnescapeDataString(manifestUri.PathAndQuery).TrimStart('/');
			if (await StorageFileHelper.ExistsInPackage(path))
			{
				var manifestFile = await StorageFile.GetFileFromApplicationUriAsync(manifestUri);
				using var manifestStream = await manifestFile.OpenStreamForReadAsync();
				uri = new Uri(FontManifestHelpers.GetFamilyNameFromManifest(manifestStream, weight, style, stretch));
				usedManifest = true;
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
			SKData data;
			using (var stream = await AppDataUriEvaluator.ToStream(uri, CancellationToken.None))
			using (var buffer = new MemoryStream())
			{
				await stream.CopyToAsync(buffer, CancellationToken.None);
				data = SKData.CreateCopy(buffer.ToArray());
			}

			// A manifest maps each weight/style to a different file, so only cache when there's none: then
			// the source uri identifies a single file shared by all weights, letting other weights resolve
			// synchronously via the fast path in GetFontInternal.
			if (!usedManifest)
			{
				lock (_fontDataGate)
				{
					_fontDataByUri[sourceUri] = data;
				}
			}

			return CreateTypeface(data, familyNameHint);
		}
		catch (Exception e)
		{
			typeof(FontDetailsCache).LogError()?.Error($"Loading font from {uri} failed: {e}");
			return null;
		}
	}

	// Upper bound on the number of faces probed in a font collection; guards against a malformed file
	// (or a backend that doesn't return null past the last face) causing an unbounded loop.
	private const int MaxFontCollectionFaces = 256;

	/// <summary>
	/// Builds the requested face from already-loaded font <paramref name="data"/>: face 0, or — when a
	/// family-name hint is given — the collection face whose family/PostScript name matches it.
	/// </summary>
	private static SKTypeface? CreateTypeface(SKData data, string? familyNameHint) =>
		string.IsNullOrEmpty(familyNameHint)
			? SKTypeface.FromData(data, 0)
			: SelectFaceByFamily(data, familyNameHint);

	/// <summary>
	/// Loads face 0 of <paramref name="data"/>. If the file is a TrueType/OpenType collection (.ttc/.otc),
	/// probes its faces and returns the one whose family or PostScript name matches <paramref name="familyNameHint"/>,
	/// falling back to face 0 when none match. <paramref name="data"/> is not disposed (the returned typeface
	/// keeps the underlying data alive, and it may be shared by the data cache).
	/// </summary>
	private static SKTypeface? SelectFaceByFamily(SKData data, string familyNameHint)
	{
		var typeface = SKTypeface.FromData(data, 0);
		if (typeface is null || FaceMatches(typeface, familyNameHint))
		{
			return typeface;
		}

		for (var index = 1; index < MaxFontCollectionFaces; index++)
		{
			var candidate = SKTypeface.FromData(data, index);
			if (candidate is null)
			{
				break; // No more faces in the collection.
			}

			if (FaceMatches(candidate, familyNameHint))
			{
				typeface.Dispose();
				return candidate;
			}

			candidate.Dispose();
		}

		return typeface; // The hint didn't match any face; use the default face.
	}

	private static bool FaceMatches(SKTypeface typeface, string familyNameHint) =>
		string.Equals(typeface.FamilyName, familyNameHint, StringComparison.OrdinalIgnoreCase) ||
		string.Equals(typeface.PostScriptName, familyNameHint, StringComparison.OrdinalIgnoreCase);

	private static async Task<SKTypeface?> GetFontInternal(
		string name,
		FontWeight weight,
		FontStretch stretch,
		FontStyle style)
	{
		var skWeight = weight.ToSkiaWeight();
		var skWidth = stretch.ToSkiaWidth();
		var skSlant = style.ToSkiaSlant();

		// A font family can be specified as "<file-uri>#<family name>". The part after '#' selects a
		// specific family within the file, which matters for TrueType/OpenType collections (.ttc/.otc).
		var hashIndex = name.IndexOf('#');
		string? familyNameHint = null;
		if (hashIndex > 0)
		{
			familyNameHint = name.Substring(hashIndex + 1);
			name = name.Substring(0, hashIndex);
		}

		if (Uri.TryCreate(name, UriKind.Absolute, out var uri))
		{
			SKData? cachedData;
			lock (_fontDataGate)
			{
				_fontDataByUri.TryGetValue(uri.OriginalString, out cachedData);
			}

			// If the font file is already loaded, build this weight's face synchronously so we don't fall
			// back to the default font for a frame (flicker) while an async reload completes.
			var uriTypeface = cachedData is not null
				? CreateTypeface(cachedData, familyNameHint)
				: await LoadTypefaceFromApplicationUriAsync(uri, weight, style, stretch, familyNameHint);
			return uriTypeface is null ? null : ApplyVariableFontAxes(uriTypeface, weight, stretch, style);
		}

		SKTypeface? fallbackTypeface = null;
		if (_fontFallbackService is { } fallbackService)
		{
			try
			{
				if (await fallbackService.GetFontStreamForFontFamily(name, weight, stretch, style) is { } fallbackStream)
				{
					fallbackTypeface = SKTypeface.FromStream(fallbackStream);
				}
			}
			catch (Exception e)
			{
				typeof(FontDetailsCache).LogError()?.Error($"Font fallback service threw resolving {name}", e);
			}
		}

		// FromFontFamilyName may return null: https://github.com/mono/SkiaSharp/issues/1058
		// It can also return the empty typeface on some platforms when the family isn't found; treat both
		// as "not found" so the caller falls back to the default font.
		var typeface = fallbackTypeface ?? SKTypeface.FromFamilyName(name, skWeight, skWidth, skSlant);
		if (typeface is null || typeface.IsEmpty)
		{
			return null;
		}

		return ApplyVariableFontAxes(typeface, weight, stretch, style);
	}

	/// <summary>
	/// If <paramref name="typeface"/> is a variable font, returns an instance positioned on its weight/width/
	/// slant/italic axes to match the requested style. Static fonts (and fonts already at the requested
	/// position) are returned unchanged.
	/// </summary>
	private static SKTypeface ApplyVariableFontAxes(SKTypeface typeface, FontWeight weight, FontStretch stretch, FontStyle style)
	{
		try
		{
			var axes = typeface.VariationDesignParameters;
			if (axes is not { Length: > 0 })
			{
				return typeface; // Not a variable font.
			}

			List<SKFontVariationPositionCoordinate>? coordinates = null;
			foreach (var axis in axes)
			{
				float target;
				if (axis.Tag == WeightAxis)
				{
					target = weight.Weight;
				}
				else if (axis.Tag == WidthAxis)
				{
					target = stretch.ToVariableFontWidth();
				}
				else if (axis.Tag == ItalicAxis)
				{
					target = style == FontStyle.Italic ? 1f : 0f;
				}
				else if (axis.Tag == SlantAxis)
				{
					// The slnt axis is the slant in counter-clockwise degrees; italic/oblique fonts slant
					// the other way, so a typical oblique sits around -10° (clamped to what the font allows).
					target = style is FontStyle.Italic or FontStyle.Oblique ? -10f : 0f;
				}
				else
				{
					continue; // Leave any other axis (e.g. opsz) at its default.
				}

				target = Math.Clamp(target, Math.Min(axis.Min, axis.Max), Math.Max(axis.Min, axis.Max));
				if (Math.Abs(target - axis.Default) < 0.01f)
				{
					continue; // Already at the default for this axis.
				}

				(coordinates ??= new()).Add(new SKFontVariationPositionCoordinate { Axis = axis.Tag, Value = target });
			}

			if (coordinates is null)
			{
				return typeface; // Nothing to adjust.
			}

			var arguments = new SKFontArguments();
			arguments.VariationDesignPosition = coordinates.ToArray();

			// For a face inside a collection (.ttc/.otc), Clone() silently ignores the requested variation
			// unless the source face's collection index is carried in the arguments (it defaults to 0).
			using (typeface.OpenStream(out var ttcIndex))
			{
				arguments.CollectionIndex = ttcIndex;
			}

			return typeface.Clone(arguments) ?? typeface;
		}
		catch (Exception e)
		{
			typeof(FontDetailsCache).LogError()?.Error("Failed to apply variable font axes", e);
			return typeface;
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
			else
			{
				var fallbackFont = await GetFont(fallbackServiceResult, fontSize, weight, stretch, style).loadedTask;
				return fallbackFont;
			}
		}
		else
		{
			return null;
		}
	}
}
