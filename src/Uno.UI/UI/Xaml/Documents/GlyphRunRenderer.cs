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
	// Reused per render thread (Draw runs to completion before returning, so it is never reentrant on one thread).
	[ThreadStatic]
	private static List<GlyphRunElement>? _elements;

	// Placements for the run being drawn, reused per render thread (Draw runs to completion before returning).
	[ThreadStatic]
	private static List<PathInstance>? _pending;

	public static void Draw(IDrawingSession session, IFont font, ReadOnlySpan<ushort> glyphs, ReadOnlySpan<Vector2> positions, float baselineY, Color color)
	{
		var elements = _elements ??= new List<GlyphRunElement>();
		elements.Clear();
		List<PathInstance>? pending = null;
		font.BuildGlyphRun(GeometryFactory.Current, glyphs, positions, baselineY, elements);

		try
		{
			foreach (var element in elements)
			{
				switch (element)
				{
					case GlyphOutlineRef glyph:
						// Collected, not drawn here: the run goes to the backend in ONE DrawPaths call below, which
						// is what lets Skia merge it into a single canvas draw and WebGPU batch its atlas quads.
						(pending ??= _pending ??= new List<PathInstance>()).Add(new PathInstance(glyph.Outline, glyph.Offset));
						break;

					case GlyphOutline outline:
						session.DrawPath(outline.Outline, color);
						break;

					case GlyphColorLayers colorLayers:
						foreach (var layer in colorLayers.Layers)
						{
							session.DrawPath(layer.Geometry, layer.Color);
						}
						break;

					case GlyphImage image:
						// The colour-glyph texture is cached per (font, glyph) — the font hands back a stable pixel
						// buffer per glyph, so its reference identity keys the texture — sparing a decode + GPU upload
						// on every repaint. Cache-owned (not disposed here).
						session.DrawImage(GlyphTextureCache.Get(session.Factory, image.Pixels, image.PixelWidth, image.PixelHeight), image.X, image.Y);
						break;
				}
			}

			if (pending is { Count: > 0 })
			{
				session.DrawPaths(global::System.Runtime.InteropServices.CollectionsMarshal.AsSpan(pending), color);
			}
		}
		finally
		{
			pending?.Clear();
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

			elements.Clear();
		}
	}

	// Per-render-thread cache of rasterized colour-glyph (emoji) textures, keyed by the font's stable per-glyph pixel
	// buffer (reference identity). ThreadStatic so no lock is needed and a texture can't be freed mid-draw by another
	// thread; bounded so GPU memory can't grow without limit; flushed when the drawing backend is re-registered
	// (device reset) since the cached textures belong to the old device.
	private static class GlyphTextureCache
	{
		private const int Cap = 512;

		[ThreadStatic]
		private static Dictionary<byte[], ITexture>? _textures;
		[ThreadStatic]
		private static IDrawingFactory? _factory;

		public static ITexture Get(IDrawingFactory factory, byte[] pixels, int width, int height)
		{
			var map = _textures ??= new Dictionary<byte[], ITexture>(ReferenceEqualityComparer.Instance);
			if (!ReferenceEquals(factory, _factory))
			{
				Flush(map);
				_factory = factory;
			}

			if (map.TryGetValue(pixels, out var texture))
			{
				return texture;
			}

			if (map.Count >= Cap)
			{
				Flush(map);
			}

			var decoded = ImageEncoderDecoder.Current.CreateImage(width, height, pixels);
			texture = factory.CreateTexture(decoded);
			map[pixels] = texture;
			return texture;
		}

		private static void Flush(Dictionary<byte[], ITexture> map)
		{
			foreach (var texture in map.Values)
			{
				texture.Dispose();
			}

			map.Clear();
		}
	}
}
