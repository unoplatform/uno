#nullable enable

using System;
using System.Collections.Generic;
using System.Numerics;
using Microsoft.UI.Composition;
using SkiaSharp;
using Windows.Foundation;
using Windows.UI;

namespace Uno.UI.Composition.Drawing;

/// <summary>SkiaSharp-backed <see cref="IDrawingSession"/> wrapping an <see cref="SKCanvas"/>. Recordings
/// (<see cref="SkiaCommandRecorder"/>) produce an SKPicture <see cref="SkiaRenderRecord"/> that replays back into
/// one of these sessions via <see cref="SkiaRenderRecord.Replay"/>.</summary>
internal class SkiaDrawingSession : IDrawingSession
{
	// Reused per drawing thread to avoid allocating a native SKPaint per draw. Each Build*Paint resets and
	// fully reconfigures it from the verb's arguments, so no state leaks between draws.
	[ThreadStatic]
	private static SKPaint? _sparePaint;

	// Recording happens on the render/UI thread and can nest (a subtree recording contains per-visual
	// content recordings), so recorders are pooled per thread and rented/returned around each recording.
	[ThreadStatic]
	private static Stack<SKPictureRecorder>? _recorderPool;

	private readonly SKCanvas _canvas;
	// The factory that created this session, surfaced as IDrawingSession.Factory. Carried on the session (not an
	// ambient global) so its resource methods (CreateTexture etc.) are scoped to a live Paint callback.
	private readonly IDrawingFactory _factory;

	public SkiaDrawingSession(SKCanvas canvas, IDrawingFactory factory)
	{
		_canvas = canvas;
		_factory = factory;
	}

	/// <summary>The underlying canvas. Transitional accessor for render code not yet migrated off SkiaSharp.</summary>
	internal SKCanvas Canvas => _canvas;

	public object? NativeSurface => _canvas;

	public IDrawingFactory Factory => _factory;

	private protected static SKPictureRecorder RentRecorder()
	{
		var pool = _recorderPool ??= new();
		return pool.Count > 0 ? pool.Pop() : new SKPictureRecorder();
	}

	private protected static void ReturnRecorder(SKPictureRecorder recorder)
		=> (_recorderPool ??= new()).Push(recorder);

	/// <summary>Creates a recording session (no pre-existing session); records a whole frame or a nested subtree.</summary>
	// Cull bounds for the SKPictureRecorder — large enough to encompass any recorded content (the real clip is
	// applied at replay). Matches the framework's SafeEdge (SK_MaxS32FitsInFloat / 4 - 1).
	private const float SafeEdge = 2147483520f / 4f - 1f;
	private static readonly SKRect RecordingBounds = new(-SafeEdge, -SafeEdge, SafeEdge, SafeEdge);

	internal static SkiaCommandRecorder StartRecording(IDrawingFactory factory)
	{
		var recorder = RentRecorder();
		var recordingCanvas = recorder.BeginRecording(RecordingBounds);
		return new SkiaCommandRecorder(recorder, recordingCanvas, factory);
	}

	public void SaveLayer(IEffectFilter filter)
	{
		using var paint = new SKPaint { ImageFilter = ((SkiaEffectFilter)filter).Filter };
		_canvas.SaveLayer(paint);
	}

	public void DrawEffectBackdrop(IEffectFilter filter, float opacity)
	{
		var rec = new SKCanvasSaveLayerRec { Backdrop = ((SkiaEffectFilter)filter).Filter };
		SKPaint? opacityPaint = null;
		if (opacity < 1)
		{
			opacityPaint = new SKPaint { Color = new SKColor(0xFF, 0xFF, 0xFF, (byte)(0xFF * opacity)) };
			rec.Paint = opacityPaint;
		}

		_canvas.SaveLayer(rec);
		_canvas.Restore();
		opacityPaint?.Dispose();
	}

	public Matrix4x4 TotalMatrix => _canvas.TotalMatrix.ToMatrix4x4();

	public void SetMatrix(in Matrix4x4 matrix)
	{
		// Preserve the full 4x4 (visual transforms may include 3D rotation) by reinterpreting the
		// Matrix4x4 as an SKMatrix44.
		var m = matrix;
		unsafe
		{
			UnoSkiaApi.sk_canvas_set_matrix(_canvas.Handle, (SKMatrix44*)&m);
		}
	}

	public void Concat(in Matrix4x4 matrix) => _canvas.Concat(matrix.ToSKMatrix());

	public void Translate(float dx, float dy) => _canvas.Translate(dx, dy);

	public void Scale(float sx, float sy) => _canvas.Scale(sx, sy);

	public int Save() => _canvas.Save();

	public int SaveCount => _canvas.SaveCount;

	public void Restore() => _canvas.Restore();

	public void RestoreToCount(int count) => _canvas.RestoreToCount(count);

	public void SaveLayer(bool antialias)
	{
		if (antialias)
		{
			_canvas.SaveLayer(LayerPaint(antialias, colorFilter: null, BlendMode.SrcOver));
		}
		else
		{
			_canvas.SaveLayer();
		}
	}

	public void SaveLayer(IColorFilter colorFilter, bool antialias)
		=> _canvas.SaveLayer(LayerPaint(antialias, colorFilter, BlendMode.SrcOver));

	public void SaveLayer(BlendMode blendMode, bool antialias)
		=> _canvas.SaveLayer(LayerPaint(antialias, colorFilter: null, blendMode));

	public void ClipRect(in Rect rect, ClipOperation operation = ClipOperation.Intersect, bool antialias = false)
		=> _canvas.ClipRect(rect.ToSKRect(), ToSK(operation), antialias);

	public void ClipRoundRect(in RoundRectangle roundRect, ClipOperation operation = ClipOperation.Intersect, bool antialias = false)
		=> _canvas.ClipRoundRect(ToSK(roundRect), ToSK(operation), antialias);

	public void ClipPath(IGeometry geometry, ClipOperation operation = ClipOperation.Intersect, bool antialias = false)
	{
		using var lease = SkiaGeometryInterop.Lease(geometry);
		_canvas.ClipPath(lease.Path, ToSK(operation), antialias);
	}

	public void Clear(Color color) => _canvas.Clear(color.ToSKColor());

	public void DrawRect(in Rect rect, Color color, bool antialias)
		=> _canvas.DrawRect(rect.ToSKRect(), FillPaint(color, antialias));

	public void DrawRect(in Rect rect, IShader shader, bool antialias)
		=> _canvas.DrawRect(rect.ToSKRect(), ShaderPaint(shader, antialias));

	public void DrawRoundedRect(in Rect rect, Vector4 radii, Color color, bool antialias)
	{
		// radii = (TopLeft, TopRight, BottomRight, BottomLeft); SKRoundRect.SetRectRadii uses the same corner order.
		var rr = new SKRoundRect();
		rr.SetRectRadii(rect.ToSKRect(), new[]
		{
			new SKPoint(radii.X, radii.X), new SKPoint(radii.Y, radii.Y),
			new SKPoint(radii.Z, radii.Z), new SKPoint(radii.W, radii.W),
		});
		_canvas.DrawRoundRect(rr, FillPaint(color, antialias));
	}

	public void DrawRoundedRectBorder(in Rect outer, Vector4 outerRadii, in Rect inner, Vector4 innerRadii, Color color, bool antialias)
	{
		// Annulus = outer round rect with the inner round rect clipped OUT (Difference), then filled.
		_canvas.Save();
		_canvas.ClipRoundRect(RoundRect(inner, innerRadii), SKClipOperation.Difference, antialias);
		_canvas.DrawRoundRect(RoundRect(outer, outerRadii), FillPaint(color, antialias));
		_canvas.Restore();
	}

	private static SKRoundRect RoundRect(in Rect r, Vector4 radii)
	{
		var rr = new SKRoundRect();
		rr.SetRectRadii(r.ToSKRect(), new[]
		{
			new SKPoint(radii.X, radii.X), new SKPoint(radii.Y, radii.Y),
			new SKPoint(radii.Z, radii.Z), new SKPoint(radii.W, radii.W),
		});
		return rr;
	}

	public void DrawPath(IGeometry geometry, Color color, bool antialias)
	{
		using var lease = SkiaGeometryInterop.Lease(geometry);
		_canvas.DrawPath(lease.Path, FillPaint(color, antialias));
	}

	public void DrawShadow(IGeometry silhouette, Color color, float sigmaX, float sigmaY, bool additive, bool antialias)
	{
		using var lease = SkiaGeometryInterop.Lease(silhouette);
		var skPath = lease.Path;
		var paint = Spare();
		paint.Style = SKPaintStyle.Fill;
		paint.Color = color.ToSKColor();
		paint.IsAntialias = antialias;
		paint.BlendMode = additive ? SKBlendMode.Plus : SKBlendMode.SrcOver;
		paint.MaskFilter = sigmaX > 0f ? BlurFilter(sigmaX) : null;

		if (sigmaX.Equals(sigmaY) || sigmaX.Equals(0f))
		{
			_canvas.DrawPath(skPath, paint);
		}
		else
		{
			// Anisotropic blur via respectCTM: the mask blur (isotropic, sigma = sigmaX) is scaled by the CTM
			// per axis. Scaling the canvas Y by sigmaY/sigmaX makes the device Y-blur = sigmaY, and pre-scaling
			// the path Y by the inverse cancels the visual stretch so the shape lands at its original position.
			var syOverSx = sigmaY / sigmaX;
			_canvas.Save();
			_canvas.Scale(1f, syOverSx);
			using var scaled = new SKPath();
			skPath.Transform(SKMatrix.CreateScale(1f, 1f / syOverSx), scaled);
			_canvas.DrawPath(scaled, paint);
			_canvas.Restore();
		}
	}

	public void StrokePath(IGeometry geometry, Color color, float strokeWidth, bool antialias)
	{
		using var lease = SkiaGeometryInterop.Lease(geometry);
		_canvas.DrawPath(lease.Path, StrokePaint(color, strokeWidth, antialias));
	}

	public void DrawLine(Vector2 p0, Vector2 p1, Color color, float strokeWidth, bool antialias)
		=> _canvas.DrawLine(p0.X, p0.Y, p1.X, p1.Y, StrokePaint(color, strokeWidth, antialias));

	// A Skia session only draws Skia-created textures: every texture reaching a draw comes from the session's own
	// factory. A foreign (e.g. WebGPU) texture would need a cross-backend GPU readback, which is not supported —
	// readback is async-only, via IDrawingFactory.SnapshotAsync.
	private static SKImage ResolveImage(ITexture texture)
		=> texture is SkiaTexture s
			? s.Image
			: throw new NotSupportedException($"The Skia backend cannot draw a {texture.GetType().Name}: a texture created by another backend cannot be drawn directly.");

	public void DrawImage(ITexture texture, float x, float y, ImageSampling sampling, float opacity, bool antialias)
		=> _canvas.DrawImage(ResolveImage(texture), x, y, ToSK(sampling), ImagePaint(antialias, opacity, colorFilter: null));

	public void DrawImage(ITexture texture, float x, float y, ImageSampling sampling, IColorFilter colorFilter, bool antialias)
		=> _canvas.DrawImage(ResolveImage(texture), x, y, ToSK(sampling), ImagePaint(antialias, opacity: 1f, colorFilter));

	public void DrawImageNineSlice(ITexture texture, in Rect centerSlice, in Rect destination, bool centerHollow, bool antialias)
	{
		var skImage = ResolveImage(texture);
		var center = new SKRectI((int)centerSlice.Left, (int)centerSlice.Top, (int)centerSlice.Right, (int)centerSlice.Bottom);
		var dst = destination.ToSKRect();
		var skPaint = ImagePaint(antialias, opacity: 1f, colorFilter: null);
		if (centerHollow)
		{
			_canvas.Save();
			_canvas.ClipRect(center, SKClipOperation.Difference, antialias: true);
			_canvas.DrawImageNinePatch(skImage, center, dst, skPaint);
			_canvas.Restore();
		}
		else
		{
			_canvas.DrawImageNinePatch(skImage, center, dst, skPaint);
		}
	}

	private static SKPaint Spare()
	{
		var paint = _sparePaint ??= new SKPaint();
		paint.Reset();
		return paint;
	}

	private static SKPaint FillPaint(Color color, bool antialias)
	{
		var paint = Spare();
		paint.Style = SKPaintStyle.Fill;
		paint.Color = color.ToSKColor();
		paint.IsAntialias = antialias;
		return paint;
	}

	// The blur mask filter is immutable and its sigma is constant across a shadow's regions/frames, so a
	// single sigma-keyed instance is cached per thread and rebuilt only when the sigma changes.
	[ThreadStatic]
	private static SKMaskFilter? _spareBlur;
	[ThreadStatic]
	private static float _spareBlurSigma;

	private static SKMaskFilter BlurFilter(float sigma)
	{
		if (_spareBlur is null || !_spareBlurSigma.Equals(sigma))
		{
			_spareBlur?.Dispose();
			_spareBlur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, sigma);
			_spareBlurSigma = sigma;
		}
		return _spareBlur;
	}

	private static SKPaint ShaderPaint(IShader shader, bool antialias)
	{
		var paint = Spare();
		paint.Style = SKPaintStyle.Fill;
		// The shader is the fill source and already carries its own alpha; keep the paint opaque so it isn't
		// further modulated (RGB is ignored under a shader).
		paint.Color = SKColors.White;
		paint.IsAntialias = antialias;
		paint.Shader = ((SkiaShader)shader).Shader;
		return paint;
	}

	private static SKPaint StrokePaint(Color color, float strokeWidth, bool antialias)
	{
		var paint = Spare();
		paint.Style = SKPaintStyle.Stroke;
		paint.Color = color.ToSKColor();
		paint.StrokeWidth = strokeWidth;
		paint.IsAntialias = antialias;
		return paint;
	}

	private static SKPaint ImagePaint(bool antialias, float opacity, IColorFilter? colorFilter)
	{
		var paint = Spare();
		// The image is the source; RGB is ignored and the paint's alpha modulates it (so opacity rides on alpha).
		paint.Color = SKColors.White.WithAlpha((byte)(0xFF * opacity));
		paint.IsAntialias = antialias;
		paint.ColorFilter = (colorFilter as SkiaColorFilter)?.ColorFilter;
		return paint;
	}

	private static SKPaint LayerPaint(bool antialias, IColorFilter? colorFilter, BlendMode blendMode)
	{
		var paint = Spare();
		paint.IsAntialias = antialias;
		paint.BlendMode = ToSKBlendMode(blendMode);
		paint.ColorFilter = (colorFilter as SkiaColorFilter)?.ColorFilter;
		return paint;
	}

	private static SKClipOperation ToSK(ClipOperation op)
		=> op == ClipOperation.Difference ? SKClipOperation.Difference : SKClipOperation.Intersect;

	private static SKRoundRect ToSK(in RoundRectangle rr)
	{
		var skRoundRect = new SKRoundRect();
		Span<SKPoint> radii = stackalloc SKPoint[]
		{
			new SKPoint(rr.TopLeft.X, rr.TopLeft.Y),
			new SKPoint(rr.TopRight.X, rr.TopRight.Y),
			new SKPoint(rr.BottomRight.X, rr.BottomRight.Y),
			new SKPoint(rr.BottomLeft.X, rr.BottomLeft.Y),
		};
		skRoundRect.SetRectRadii(rr.Rect.ToSKRect(), radii);
		return skRoundRect;
	}

	internal static SKBlendMode ToSKBlendMode(BlendMode mode) => mode switch
	{
		BlendMode.Src => SKBlendMode.Src,
		BlendMode.Plus => SKBlendMode.Plus,
		BlendMode.Modulate => SKBlendMode.Modulate,
		BlendMode.Multiply => SKBlendMode.Multiply,
		BlendMode.DstIn => SKBlendMode.DstIn,
		BlendMode.DstOut => SKBlendMode.DstOut,
		BlendMode.SrcIn => SKBlendMode.SrcIn,
		BlendMode.DstOver => SKBlendMode.DstOver,
		BlendMode.SrcOut => SKBlendMode.SrcOut,
		BlendMode.SrcATop => SKBlendMode.SrcATop,
		BlendMode.DstATop => SKBlendMode.DstATop,
		BlendMode.Xor => SKBlendMode.Xor,
		BlendMode.Screen => SKBlendMode.Screen,
		BlendMode.Darken => SKBlendMode.Darken,
		BlendMode.Lighten => SKBlendMode.Lighten,
		BlendMode.ColorBurn => SKBlendMode.ColorBurn,
		BlendMode.ColorDodge => SKBlendMode.ColorDodge,
		BlendMode.Overlay => SKBlendMode.Overlay,
		BlendMode.SoftLight => SKBlendMode.SoftLight,
		BlendMode.HardLight => SKBlendMode.HardLight,
		BlendMode.Difference => SKBlendMode.Difference,
		BlendMode.Exclusion => SKBlendMode.Exclusion,
		BlendMode.Hue => SKBlendMode.Hue,
		BlendMode.Saturation => SKBlendMode.Saturation,
		BlendMode.Color => SKBlendMode.Color,
		BlendMode.Luminosity => SKBlendMode.Luminosity,
		_ => SKBlendMode.SrcOver,
	};

	private static SKSamplingOptions ToSK(ImageSampling sampling)
		=> new(sampling == ImageSampling.Linear ? SKFilterMode.Linear : SKFilterMode.Nearest);
}
