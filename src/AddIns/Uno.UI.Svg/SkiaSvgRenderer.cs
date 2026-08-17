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

		// Cross-backend fallback (no SKCanvas, e.g. WebGPU): rasterize to a Skia-owned texture and let the session
		// materialize it via the neutral ITexture.CopyPixels contract.
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

		using var texture = new SvgTexture(surface.Snapshot());
		session.DrawImage(texture, 0, 0, ImageSampling.Linear, antialias: true);
	}
}

/// <summary>
/// SkiaSharp-backed <see cref="ITexture"/> so a Skia-rendered SVG can be drawn into a non-Skia session (e.g. WebGPU)
/// through the neutral <see cref="ITexture.CopyPixels"/> contract. (The add-in can't see the core backend's internal
/// texture type, so it brings its own — it already depends on SkiaSharp via Svg.Skia.)
/// </summary>
internal sealed class SvgTexture : ITexture
{
	private readonly SKImage _image;

	public SvgTexture(SKImage image) => _image = image;

	public int PixelWidth => _image.Width;

	public int PixelHeight => _image.Height;

	public unsafe void CopyPixels(Span<byte> destination)
	{
		var info = new SKImageInfo(_image.Width, _image.Height, SKColorType.Bgra8888, SKAlphaType.Premul);
		fixed (byte* dst = destination)
		{
			_image.ReadPixels(info, (nint)dst, info.RowBytes, 0, 0);
		}
	}

	public void Dispose() => _image.Dispose();
}
