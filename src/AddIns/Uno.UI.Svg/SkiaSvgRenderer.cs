#nullable enable

using System;
using System.IO;
using SkiaSharp;
using Svg.Skia;
using Uno.UI.Composition.Drawing;
using Windows.Foundation;

namespace Uno.UI.Svg;

/// <summary>
/// A Svg.Skia-backed <see cref="ISvgRenderer"/>. Shipped as the optional <c>Uno.UI.Svg</c> add-in: when referenced it
/// becomes the default SVG renderer; otherwise the framework uses its managed (SkiaSharp-free) engine.
/// </summary>
internal sealed class SkiaSvgRenderer : ISvgRenderer
{
	public ISvgDocument? Parse(byte[] svg, IGeometryFactory geometry, IDrawingFactory drawing)
	{
		try
		{
			var skSvg = new SKSvg();
			using var stream = new MemoryStream(svg);
			skSvg.Load(stream);
			if (skSvg.Picture is null)
			{
				skSvg.Dispose();
				return null;
			}

			return new SkiaSvgDocument(skSvg);
		}
		catch (Exception)
		{
			// Malformed / unsupported SVG — the caller treats a null document as "not loaded".
			return null;
		}
	}
}

/// <summary>
/// A parsed SVG (a Svg.Skia <see cref="SKPicture"/>). <see cref="Render"/> replays it as VECTOR straight into the
/// session's live <c>SKCanvas</c> when the backend is Skia (crisp at any scale, honoring the session's current
/// transform/clip); it rasterizes to a neutral texture only as a cross-backend fallback (e.g. WebGPU).
/// </summary>
internal sealed class SkiaSvgDocument : ISvgDocument
{
	private readonly SKSvg _svg;

	public SkiaSvgDocument(SKSvg svg) => _svg = svg;

	public Size SourceSize => _svg.Picture is { CullRect: var cull } ? new Size(cull.Width, cull.Height) : default;

	public unsafe void Render(IDrawingSession session, Size targetSize)
	{
		if (_svg.Picture is not { } picture || targetSize.Width <= 0 || targetSize.Height <= 0)
		{
			return;
		}

		var cull = picture.CullRect;
		var sx = cull.Width > 0 ? (float)(targetSize.Width / cull.Width) : 1f;
		var sy = cull.Height > 0 ? (float)(targetSize.Height / cull.Height) : 1f;

		// Vector path: draw the picture straight into the backend's live canvas at the session's current transform.
		if (session.NativeSurface is SKCanvas canvas)
		{
			var save = session.Save();
			session.Scale(sx, sy);
			canvas.DrawPicture(picture);
			session.RestoreToCount(save);
			return;
		}

		// Cross-backend fallback (no SKCanvas, e.g. WebGPU): rasterize to an offscreen, then let the SESSION's own
		// backend mint a native texture from the pixels — a foreign texture wouldn't be accepted by its DrawImage.
		var width = Math.Max(1, (int)Math.Ceiling(targetSize.Width));
		var height = Math.Max(1, (int)Math.Ceiling(targetSize.Height));
		var info = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
		using var surface = SKSurface.Create(info);
		if (surface is null)
		{
			return;
		}

		surface.Canvas.Clear(SKColors.Transparent);
		surface.Canvas.Scale(sx, sy);
		surface.Canvas.DrawPicture(picture);
		surface.Canvas.Flush();

		using var pixmap = surface.PeekPixels();
		if (pixmap is null)
		{
			return;
		}

		using var texture = session.Factory.CreateTexture(width, height, pixmap.GetPixelSpan());
		session.DrawImage(texture, 0, 0, ImageSampling.Linear, antialias: true);
	}
}
