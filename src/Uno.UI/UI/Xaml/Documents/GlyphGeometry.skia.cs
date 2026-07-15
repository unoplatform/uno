#nullable enable

using System;
using Microsoft.UI.Composition;
using SkiaSharp;
using Uno.UI.Composition.Drawing;

namespace Microsoft.UI.Xaml.Documents;

internal static class GlyphGeometry
{
	/// <summary>
	/// Assembles the filled outlines of a positioned glyph run into a single neutral <see cref="IGeometry"/>.
	/// Extracting glyph outlines is font (shaping) work and stays on Skia; the resulting geometry is drawn
	/// through the backend-neutral <see cref="IDrawingSession.DrawPath"/>, so it is just a path as far as the
	/// renderer is concerned.
	/// </summary>
	public static IGeometry Build(SKFont font, ReadOnlySpan<ushort> glyphs, ReadOnlySpan<SKPoint> positions, float yOffset)
	{
		var builder = new SKPathBuilder();

		for (var i = 0; i < glyphs.Length; i++)
		{
			using var glyphPath = font.GetGlyphPath(glyphs[i]);
			if (glyphPath is { IsEmpty: false })
			{
				glyphPath.Transform(SKMatrix.CreateTranslation(positions[i].X, positions[i].Y + yOffset));
				builder.AddPath(glyphPath, SKPathAddMode.Append);
			}
		}

		return new SkiaGeometrySource2D(builder.Detach());
	}
}
