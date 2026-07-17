#nullable enable

using System;
using System.Collections.Generic;
using System.Numerics;
using Microsoft.UI.Composition;
using SkiaSharp;
using Windows.Foundation;
using Windows.UI;

namespace Uno.UI.Composition.Drawing;

/// <summary>SkiaSharp-backed <see cref="IDrawingSession"/> wrapping an <see cref="SKCanvas"/>. The Skia
/// backend also advertises the optional <see cref="IRetainedRenderingSession"/> capability (SKPicture).</summary>
internal class SkiaDrawingSession : IDrawingSession, IRetainedRenderingSession
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

	public SkiaDrawingSession(SKCanvas canvas) => _canvas = canvas;

	/// <summary>The underlying canvas. Transitional accessor for render code not yet migrated off SkiaSharp.</summary>
	internal SKCanvas Canvas => _canvas;

	private protected static SKPictureRecorder RentRecorder()
	{
		var pool = _recorderPool ??= new();
		return pool.Count > 0 ? pool.Pop() : new SKPictureRecorder();
	}

	private protected static void ReturnRecorder(SKPictureRecorder recorder)
		=> (_recorderPool ??= new()).Push(recorder);

	public IRecordingSession CreateRecording(Rect cullBounds) => StartRecording(cullBounds);

	/// <summary>Creates a root recording session (no pre-existing session), used to record a whole frame.</summary>
	internal static SkiaRecordingSession StartRecording(Rect cullBounds)
	{
		var recorder = RentRecorder();
		var recordingCanvas = recorder.BeginRecording(cullBounds.ToSKRect());
		return new SkiaRecordingSession(recorder, recordingCanvas);
	}

	public void Replay(IRenderData data)
	{
		if (data is SkiaRenderData { Picture: var picture } && picture != IntPtr.Zero)
		{
			unsafe
			{
				UnoSkiaApi.sk_canvas_draw_picture(_canvas.Handle, picture, null, IntPtr.Zero);
			}
		}
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
		// Matrix4x4 as an SKMatrix44, matching the previous raw sk_canvas_set_matrix path.
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
		=> _canvas.ClipPath(((SkiaGeometrySource2D)geometry).Geometry, ToSK(operation), antialias);

	public void Clear(Color color) => _canvas.Clear(color.ToSKColor());

	public void DrawRect(in Rect rect, Color color, bool antialias)
		=> _canvas.DrawRect(rect.ToSKRect(), FillPaint(color, antialias));

	public void DrawRect(in Rect rect, IShader shader, bool antialias)
		=> _canvas.DrawRect(rect.ToSKRect(), ShaderPaint(shader, antialias));

	public void DrawPath(IGeometry geometry, Color color, bool antialias)
		=> _canvas.DrawPath(((SkiaGeometrySource2D)geometry).Geometry, FillPaint(color, antialias));

	public void DrawShadow(IGeometry silhouette, Color color, float sigmaX, float sigmaY, bool additive, bool antialias)
	{
		var skPath = ((SkiaGeometrySource2D)silhouette).Geometry;
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
		=> _canvas.DrawPath(((SkiaGeometrySource2D)geometry).Geometry, StrokePaint(color, strokeWidth, antialias));

	public void DrawLine(Vector2 p0, Vector2 p1, Color color, float strokeWidth, bool antialias)
		=> _canvas.DrawLine(p0.X, p0.Y, p1.X, p1.Y, StrokePaint(color, strokeWidth, antialias));

	public void DrawImage(IImage image, float x, float y, ImageSampling sampling, bool antialias)
		=> _canvas.DrawImage(((SkiaImage)image).Image, x, y, ToSK(sampling), ImagePaint(antialias, colorFilter: null));

	public void DrawImage(IImage image, float x, float y, ImageSampling sampling, IColorFilter colorFilter, bool antialias)
		=> _canvas.DrawImage(((SkiaImage)image).Image, x, y, ToSK(sampling), ImagePaint(antialias, colorFilter));

	public void DrawImageNineSlice(IImage image, in Rect centerSlice, in Rect destination, bool centerHollow, bool antialias)
	{
		var skImage = ((SkiaImage)image).Image;
		var center = new SKRectI((int)centerSlice.Left, (int)centerSlice.Top, (int)centerSlice.Right, (int)centerSlice.Bottom);
		var dst = destination.ToSKRect();
		var skPaint = ImagePaint(antialias, colorFilter: null);
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

	private static SKPaint ImagePaint(bool antialias, IColorFilter? colorFilter)
	{
		var paint = Spare();
		// The image is the source; keep the paint opaque (RGB ignored, alpha would modulate the image).
		paint.Color = SKColors.White;
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
		_ => SKBlendMode.SrcOver,
	};

	private static SKSamplingOptions ToSK(ImageSampling sampling)
		=> new(sampling == ImageSampling.Linear ? SKFilterMode.Linear : SKFilterMode.Nearest);
}
