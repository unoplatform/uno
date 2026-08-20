#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SkiaSharp;
using Windows.UI.Text;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// Default <see cref="IFontProvider"/>: resolves fonts through SkiaSharp (<c>SKTypeface</c>/<c>SKFontManager</c>)
/// and wraps the result in a <see cref="SkiaFont"/>, so the text layer above talks only to <see cref="IFont"/>.
/// </summary>
internal sealed class SkiaFontProvider : IFontProvider
{
	// Standard OpenType variation axes that map to the WinUI font properties.
	private static readonly SKFourByteTag WeightAxis = SKFourByteTag.Parse("wght");
	private static readonly SKFourByteTag WidthAxis = SKFourByteTag.Parse("wdth");
	private static readonly SKFourByteTag ItalicAxis = SKFourByteTag.Parse("ital");
	private static readonly SKFourByteTag SlantAxis = SKFourByteTag.Parse("slnt");

	// Upper bound on the number of faces probed in a font collection; guards against a malformed file
	// (or a backend that doesn't return null past the last face) causing an unbounded loop.
	private const int MaxFontCollectionFaces = 256;

	// Caches codepoint fallback resolution: the SKFontManager.MatchCharacter lookup is comparatively expensive,
	// and a stable IFont instance lets the FontDetails cache dedupe. Keyed by codepoint + requested style + size.
	private readonly Dictionary<(int Codepoint, int Weight, FontStretch Stretch, FontStyle Style, float Size), IFont?> _matchCharacterCache = new();
	private readonly object _matchCharacterGate = new();

	public IFont? CreateFont(byte[] data, string? familyNameHint, FontWeight weight, FontStretch stretch, FontStyle style, float fontSize)
	{
		using var skData = SKData.CreateCopy(data);
		var typeface = string.IsNullOrEmpty(familyNameHint)
			? SKTypeface.FromData(skData, 0)
			: SelectFaceByFamily(skData, familyNameHint);

		if (typeface is null)
		{
			return null;
		}

		return MakeFont(ApplyVariableFontAxes(typeface, weight, stretch, style), fontSize);
	}

	public IFont? MatchFamily(string familyName, FontWeight weight, FontStretch stretch, FontStyle style, float fontSize)
	{
		// FromFamilyName may return null (https://github.com/mono/SkiaSharp/issues/1058) or an empty typeface
		// on some platforms when the family isn't found; treat both as "not found".
		var typeface = SKTypeface.FromFamilyName(familyName, weight.ToSkiaWeight(), stretch.ToSkiaWidth(), style.ToSkiaSlant());
		if (typeface is null || typeface.IsEmpty)
		{
			return null;
		}

		return MakeFont(ApplyVariableFontAxes(typeface, weight, stretch, style), fontSize);
	}

	public ValueTask<IFont?> MatchCharacterAsync(int codepoint, FontWeight weight, FontStretch stretch, FontStyle style, float fontSize)
	{
		var key = (codepoint, weight.Weight, stretch, style, fontSize);
		lock (_matchCharacterGate)
		{
			if (_matchCharacterCache.TryGetValue(key, out var cached))
			{
				return new ValueTask<IFont?>(cached);
			}
		}

		var typeface = SKFontManager.Default.MatchCharacter(codepoint);
		if (typeface is null)
		{
			// Not among the installed fonts — hand off to the shared platform fallback (browser Noto / Android system).
			return FontFallback.MatchCharacterAsync(this, codepoint, weight, stretch, style, fontSize);
		}

		var font = MakeFont(ApplyVariableFontAxes(typeface, weight, stretch, style), fontSize);
		lock (_matchCharacterGate)
		{
			_matchCharacterCache[key] = font;
		}

		return new ValueTask<IFont?>((IFont?)font);
	}

	public IFont GetDefaultFont(FontWeight weight, FontStretch stretch, FontStyle style, float fontSize)
	{
		var typeface = SKTypeface.FromFamilyName(null, weight.ToSkiaWeight(), stretch.ToSkiaWidth(), style.ToSkiaSlant())
			?? SKTypeface.FromFamilyName(null);
		return MakeFont(typeface, fontSize);
	}

	private static IFont MakeFont(SKTypeface typeface, float fontSize)
	{
		var skFont = new SKFont(typeface, fontSize)
		{
			Edging = SKFontEdging.SubpixelAntialias,
			Subpixel = true,
		};
		return new SkiaFont(skFont);
	}

	/// <summary>
	/// Loads face 0 of <paramref name="data"/>. If the file is a TrueType/OpenType collection (.ttc/.otc),
	/// returns the face whose family or PostScript name matches <paramref name="familyNameHint"/>, falling back
	/// to face 0 when none match.
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
					// The slnt axis is the slant in counter-clockwise degrees; italic/oblique fonts slant the
					// other way, so a typical oblique sits around -10° (clamped to what the font allows).
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
		catch
		{
			return typeface;
		}
	}
}

internal static class SkiaFontStyleExtensions
{
	public static SKFontStyleWeight ToSkiaWeight(this FontWeight weight) => (SKFontStyleWeight)weight.Weight;

	public static SKFontStyleSlant ToSkiaSlant(this FontStyle style) => style switch
	{
		FontStyle.Italic => SKFontStyleSlant.Italic,
		FontStyle.Oblique => SKFontStyleSlant.Oblique,
		_ => SKFontStyleSlant.Upright,
	};

	public static SKFontStyleWidth ToSkiaWidth(this FontStretch stretch) => stretch switch
	{
		FontStretch.UltraCondensed => SKFontStyleWidth.UltraCondensed,
		FontStretch.ExtraCondensed => SKFontStyleWidth.ExtraCondensed,
		FontStretch.Condensed => SKFontStyleWidth.Condensed,
		FontStretch.SemiCondensed => SKFontStyleWidth.SemiCondensed,
		FontStretch.SemiExpanded => SKFontStyleWidth.SemiExpanded,
		FontStretch.Expanded => SKFontStyleWidth.Expanded,
		FontStretch.ExtraExpanded => SKFontStyleWidth.ExtraExpanded,
		FontStretch.UltraExpanded => SKFontStyleWidth.UltraExpanded,
		_ => SKFontStyleWidth.Normal,
	};

	public static float ToVariableFontWidth(this FontStretch stretch) => stretch switch
	{
		FontStretch.UltraCondensed => 50f,
		FontStretch.ExtraCondensed => 62.5f,
		FontStretch.Condensed => 75f,
		FontStretch.SemiCondensed => 87.5f,
		FontStretch.SemiExpanded => 112.5f,
		FontStretch.Expanded => 125f,
		FontStretch.ExtraExpanded => 150f,
		FontStretch.UltraExpanded => 200f,
		_ => 100f, // Normal / Undefined
	};
}
