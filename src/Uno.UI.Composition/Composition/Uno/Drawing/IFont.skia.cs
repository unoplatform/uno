#nullable enable

using System;
using System.Collections.Generic;
using System.Numerics;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// Backend font handle used at render time to turn a shaped glyph run into drawable output. Outline glyphs
/// become a single filled <see cref="IGeometry"/>; color glyphs (emoji — COLR/CBDT/sbix/SVG) become
/// positioned images. Font loading and shaping stay backend-internal; this is only the render-time handle.
/// </summary>
internal interface IFont
{
	/// <summary>
	/// Builds the combined filled outline of the run's outline glyphs (color glyphs are excluded — draw those
	/// via <see cref="AppendColorGlyphImages"/>). Each glyph is placed at its position, shifted by <paramref name="baselineY"/>.
	/// </summary>
	IGeometry BuildGlyphRunOutline(ReadOnlySpan<ushort> glyphs, ReadOnlySpan<Vector2> positions, float baselineY);

	/// <summary>Whether the font may contain color glyphs; when false, callers can skip <see cref="AppendColorGlyphImages"/>.</summary>
	bool HasColorGlyphs { get; }

	/// <summary>Appends the run's color glyphs as positioned images (no-op when <see cref="HasColorGlyphs"/> is false).</summary>
	void AppendColorGlyphImages(ReadOnlySpan<ushort> glyphs, ReadOnlySpan<Vector2> positions, float baselineY, IList<PositionedGlyphImage> output);
}

/// <summary>A color glyph rasterized to an <see cref="IImage"/>, with the destination rectangle to draw it at.</summary>
internal readonly record struct PositionedGlyphImage(IImage Image, float X, float Y, float Width, float Height);
