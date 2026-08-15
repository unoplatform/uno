#nullable enable

using System;
using System.Collections.Generic;
using System.Numerics;
using Windows.UI;
using Uno.UI.Composition.Drawing;

namespace Microsoft.UI.Xaml.Documents;

/// <summary>
/// Draws a shaped glyph run by building it into neutral <see cref="GlyphRunElement"/>s and rendering each: a
/// monochrome outline (filled with the text colour), COLR vector layers (each filled with its own colour), or a
/// rasterized colour glyph whose neutral BGRA pixels are turned into an image (via the registered image decoder) and
/// uploaded to a texture. The font never touches the render backend; that upload happens here. Any geometry produced
/// by the font is disposed once drawing completes.
/// </summary>
internal static class GlyphRunRenderer
{
	public static void Draw(IDrawingSession session, IFont font, ReadOnlySpan<ushort> glyphs, ReadOnlySpan<Vector2> positions, float baselineY, Color color)
	{
		var elements = new List<GlyphRunElement>();
		font.BuildGlyphRun(glyphs, positions, baselineY, elements);

		try
		{
			foreach (var element in elements)
			{
				switch (element)
				{
					case GlyphOutline outline:
						session.DrawPath(outline.Outline, color, antialias: true);
						break;

					case GlyphColorLayers colorLayers:
						foreach (var layer in colorLayers.Layers)
						{
							session.DrawPath(layer.Geometry, layer.Color, antialias: true);
						}
						break;

					case GlyphImage image:
					{
						var decoded = ImageDecoder.Current.CreateImage(image.PixelWidth, image.PixelHeight, image.Pixels);
						var texture = DrawingFactory.Current.CreateTexture(decoded);
						try
						{
							session.DrawImage(texture, image.X, image.Y, ImageSampling.Linear, antialias: true);
						}
						finally
						{
							texture.Dispose();
						}
						break;
					}
				}
			}
		}
		finally
		{
			// Dispose the geometries the font handed us (a mid-loop throw must not leak them).
			foreach (var element in elements)
			{
				switch (element)
				{
					case GlyphOutline outline:
						outline.Outline.Dispose();
						break;
					case GlyphColorLayers colorLayers:
						foreach (var layer in colorLayers.Layers)
						{
							layer.Geometry.Dispose();
						}
						break;
				}
			}
		}
	}
}
