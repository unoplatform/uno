#nullable enable

using System;
using System.Collections.Generic;
using System.Numerics;
using Windows.Foundation;
using Windows.UI;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// The default <see cref="IDrawingSession.DrawGlyphRun"/>, which works with any <see cref="IFont"/>: builds the run
/// into neutral <see cref="GlyphRunElement"/>s and renders each: a
/// monochrome outline (filled with the text colour), COLR vector layers (each filled with its own colour), or a
/// rasterized colour glyph whose neutral BGRA pixels are turned into an image (via the registered image decoder) and
/// uploaded to a texture. On this path the font never touches the render backend; that upload happens here. Any
/// geometry produced by the font is disposed once drawing completes. A backend overriding <see cref="IDrawingSession.DrawGlyphRun"/>
/// calls this for fonts it cannot draw natively.
/// </summary>
public static class GlyphRunRenderer
{
	// Reused per render thread (Draw runs to completion before returning, so it is never reentrant on one thread).
	[ThreadStatic]
	private static List<GlyphRunElement>? _elements;

	// Placements for the run being drawn, reused per render thread (Draw runs to completion before returning).
	[ThreadStatic]
	private static List<PathInstance>? _pending;

	/// <param name="outlineStrokeWidth">When set, monochrome glyphs are stroked at this width instead of filled.</param>
	public static void Draw(IDrawingSession session, IFont font, ReadOnlySpan<ushort> glyphs, ReadOnlySpan<Vector2> positions, float baselineY, Color color, float? outlineStrokeWidth = null)
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
					case GlyphOutlineRef glyph when outlineStrokeWidth is { } strokeWidth:
						session.Save();
						session.Translate(glyph.Offset.X, glyph.Offset.Y);
						session.StrokePath(glyph.Outline, color, strokeWidth);
						session.Restore();
						break;

					case GlyphOutlineRef glyph:
						// Collected, not drawn here: the run goes to the backend in ONE DrawPaths call below, which
						// is what lets Skia merge it into a single canvas draw and WebGPU batch its atlas quads.
						(pending ??= _pending ??= new List<PathInstance>()).Add(new PathInstance(glyph.Outline, glyph.Offset));
						break;

					case GlyphOutline outline when outlineStrokeWidth is { } strokeWidth:
						session.StrokePath(outline.Outline, color, strokeWidth);
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

	/// <summary>Shapes <paramref name="text"/> left-to-right into glyphs positioned from the origin on the baseline.</summary>
	public static (ushort[] glyphs, Vector2[] positions) Layout(IFont font, string text)
		=> Layout(font, text, out _);

	public static (ushort[] glyphs, Vector2[] positions) Layout(IFont font, string text, out float advance)
	{
		var run = font.Shape(text, TextDirection.LeftToRight);
		var positions = new Vector2[run.Count];
		var x = 0f;
		for (var i = 0; i < run.Count; i++)
		{
			positions[i] = new Vector2(x + run.Offsets[i].X, run.Offsets[i].Y);
			x += run.Advances[i];
		}

		advance = x;
		return ((ushort[])run.Glyphs.Clone(), positions);
	}

	/// <summary>Ink bounds of <paramref name="text"/> laid out from the origin on the baseline (y grows down).</summary>
	public static Rect MeasureInk(IFont font, string text)
	{
		var (glyphs, positions) = Layout(font, text);
		return MeasureInk(font, glyphs, positions);
	}

	/// <summary>Union of the glyphs' ink bounds, or an empty rect when no glyph has ink.</summary>
	public static Rect MeasureInk(IFont font, ReadOnlySpan<ushort> glyphs, ReadOnlySpan<Vector2> positions)
	{
		var elements = new List<GlyphRunElement>();
		font.BuildGlyphRun(GeometryFactory.Current, glyphs, positions, 0, elements);
		Rect? bounds = null;
		void Union(Rect rect)
		{
			if (rect.Width > 0 || rect.Height > 0)
			{
				if (bounds is { } b)
				{
					b.Union(rect);
					bounds = b;
				}
				else
				{
					bounds = rect;
				}
			}
		}

		foreach (var element in elements)
		{
			switch (element)
			{
				case GlyphOutlineRef glyph:
					var outlineBounds = glyph.Outline.Bounds;
					Union(new Rect(outlineBounds.X + glyph.Offset.X, outlineBounds.Y + glyph.Offset.Y, outlineBounds.Width, outlineBounds.Height));
					break;
				case GlyphOutline outline:
					Union(outline.Outline.Bounds);
					outline.Outline.Dispose();
					break;
				case GlyphColorLayers colorLayers:
					foreach (var layer in colorLayers.Layers)
					{
						Union(layer.Geometry.Bounds);
						layer.Geometry.Dispose();
					}
					break;
				case GlyphImage image:
					Union(new Rect(image.X, image.Y, image.PixelWidth, image.PixelHeight));
					break;
			}
		}

		return bounds ?? Rect.Empty;
	}

	/// <summary>
	/// Draws a decoded image stretched into <paramref name="destination"/>. Images with the same
	/// <paramref name="key"/> must have the same pixels; they share one cached texture.
	/// </summary>
	public static void DrawImage(IDrawingSession session, IImage image, object key, Rect destination, float opacity)
	{
		if (image.PixelWidth <= 0 || image.PixelHeight <= 0 || destination.Width <= 0 || destination.Height <= 0)
		{
			return;
		}

		session.Save();
		session.Translate((float)destination.X, (float)destination.Y);
		session.Scale((float)destination.Width / image.PixelWidth, (float)destination.Height / image.PixelHeight);
		session.DrawImage(ImageTextureCache.Get(session, key, image), 0, 0, opacity);
		session.Restore();
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

	// Per-render-thread textures for decoded images (inline objects), evicted least recently used past a count and
	// byte budget. Entries the current session has drawn are never evicted: a paint showing more images than the
	// budget would otherwise evict each one just before it is drawn again, missing on every lookup.
	internal static class ImageTextureCache
	{
		internal const int Cap = 64;
		internal const long ByteBudget = 256L * 1024 * 1024;

		private sealed class Entry(object key, ITexture texture, long bytes)
		{
			public object Key { get; } = key;
			public ITexture Texture { get; } = texture;
			public long Bytes { get; } = bytes;
			public int Generation { get; set; }
		}

		[ThreadStatic]
		private static Dictionary<object, LinkedListNode<Entry>>? _textures;
		[ThreadStatic]
		private static LinkedList<Entry>? _recency;
		[ThreadStatic]
		private static IDrawingFactory? _factory;
		[ThreadStatic]
		private static IDrawingSession? _session;
		[ThreadStatic]
		private static int _generation;
		[ThreadStatic]
		private static long _bytes;

		internal static int Count => _textures?.Count ?? 0;

		public static ITexture Get(IDrawingSession session, object key, IImage image)
		{
			var map = _textures ??= new(ReferenceEqualityComparer.Instance);
			var recency = _recency ??= new();
			var factory = session.Factory;
			if (!ReferenceEquals(factory, _factory))
			{
				foreach (var entry in recency)
				{
					entry.Texture.Dispose();
				}

				map.Clear();
				recency.Clear();
				_bytes = 0;
				_factory = factory;
			}

			if (!ReferenceEquals(session, _session))
			{
				_session = session;
				_generation++;
			}

			if (map.TryGetValue(key, out var node))
			{
				recency.Remove(node);
				recency.AddFirst(node);
				node.Value.Generation = _generation;
				return node.Value.Texture;
			}

			var bytes = (long)image.PixelWidth * image.PixelHeight * 4;
			while ((map.Count >= Cap || _bytes + bytes > ByteBudget)
				&& recency.Last is { } oldest
				&& oldest.Value.Generation != _generation)
			{
				recency.RemoveLast();
				map.Remove(oldest.Value.Key);
				_bytes -= oldest.Value.Bytes;
				oldest.Value.Texture.Dispose();
			}

			node = recency.AddFirst(new Entry(key, factory.CreateTexture(image), bytes) { Generation = _generation });
			map[key] = node;
			_bytes += bytes;
			return node.Value.Texture;
		}
	}
}
