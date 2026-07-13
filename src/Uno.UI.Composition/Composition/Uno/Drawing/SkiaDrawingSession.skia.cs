#nullable enable

using System;
using System.Collections.Generic;
using System.Numerics;
using Microsoft.UI.Composition;
using SkiaSharp;
using Windows.Foundation;
using Windows.UI;

namespace Uno.UI.Composition.Drawing;

/// <summary>SkiaSharp-backed <see cref="IDrawingSession"/> wrapping an <see cref="SKCanvas"/>.</summary>
internal class SkiaDrawingSession : IDrawingSession
{
	// Reused per drawing thread to avoid allocating a native SKPaint per draw. Rendering configures it
	// fully from PaintParams on every call, so no state leaks between draws.
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

	public IRecordingSession CreateRecording(Rect cullBounds)
	{
		var recorder = RentRecorder();
		var recordingCanvas = recorder.BeginRecording(cullBounds.ToSKRect());
		return new SkiaRecordingSession(recorder, recordingCanvas);
	}

	public void Draw(IRenderData data)
	{
		if (data is SkiaRenderData { Picture: var picture } && picture != IntPtr.Zero)
		{
			unsafe
			{
				UnoSkiaApi.sk_canvas_draw_picture(_canvas.Handle, picture, null, IntPtr.Zero);
			}
		}
	}

	public Matrix4x4 TotalMatrix => _canvas.TotalMatrix.ToMatrix4x4();

	public void SetMatrix(in Matrix4x4 matrix) => _canvas.SetMatrix(matrix.ToSKMatrix());

	public void Concat(in Matrix4x4 matrix) => _canvas.Concat(matrix.ToSKMatrix());

	public void Translate(float dx, float dy) => _canvas.Translate(dx, dy);

	public void Scale(float sx, float sy) => _canvas.Scale(sx, sy);

	public int Save() => _canvas.Save();

	public int SaveCount => _canvas.SaveCount;

	public void Restore() => _canvas.Restore();

	public void RestoreToCount(int count) => _canvas.RestoreToCount(count);

	public void SaveLayer(Rect? bounds, PaintParams? paint)
	{
		if (paint is { } p)
		{
			_canvas.SaveLayer(BuildPaint(p));
		}
		else
		{
			_canvas.SaveLayer();
		}
	}

	public void ClipRect(in Rect rect, ClipOperation operation = ClipOperation.Intersect, bool antialias = false)
		=> _canvas.ClipRect(rect.ToSKRect(), ToSK(operation), antialias);

	public void ClipRoundRect(in RoundRectangle roundRect, ClipOperation operation = ClipOperation.Intersect, bool antialias = false)
		=> _canvas.ClipRoundRect(ToSK(roundRect), ToSK(operation), antialias);

	public void ClipPath(IGeometry geometry, ClipOperation operation = ClipOperation.Intersect, bool antialias = false)
		=> _canvas.ClipPath(((SkiaGeometrySource2D)geometry).Geometry, ToSK(operation), antialias);

	public void Clear(Color color) => _canvas.Clear(color.ToSKColor());

	public void DrawRect(in Rect rect, in PaintParams paint)
		=> _canvas.DrawRect(rect.ToSKRect(), BuildPaint(paint));

	public void DrawPath(IGeometry geometry, in PaintParams paint)
		=> _canvas.DrawPath(((SkiaGeometrySource2D)geometry).Geometry, BuildPaint(paint));

	public void DrawLine(Vector2 p0, Vector2 p1, in PaintParams paint)
		=> _canvas.DrawLine(p0.X, p0.Y, p1.X, p1.Y, BuildPaint(paint));

	public void DrawCircle(Vector2 center, float radius, in PaintParams paint)
		=> _canvas.DrawCircle(center.X, center.Y, radius, BuildPaint(paint));

	public void DrawImage(IImage image, float x, float y, ImageSampling sampling, in PaintParams paint)
		=> _canvas.DrawImage(((SkiaImage)image).Image, x, y, ToSK(sampling), BuildPaint(paint));

	private static SKPaint BuildPaint(in PaintParams p)
	{
		var paint = _sparePaint ??= new SKPaint();
		paint.Reset();
		paint.Color = p.Color.ToSKColor(p.Opacity);
		paint.IsAntialias = p.IsAntialias;
		paint.BlendMode = ToSKBlendMode(p.BlendMode);
		paint.Style = p.Style == PaintStyle.Stroke ? SKPaintStyle.Stroke : SKPaintStyle.Fill;
		if (p.Style == PaintStyle.Stroke)
		{
			paint.StrokeWidth = p.StrokeWidth;
			paint.StrokeCap = ToSK(p.StrokeCap);
			paint.StrokeJoin = ToSK(p.StrokeJoin);
			paint.StrokeMiter = p.StrokeMiter;
		}
		paint.Shader = (p.Shader as SkiaShader)?.Shader;
		paint.ColorFilter = (p.ColorFilter as SkiaColorFilter)?.ColorFilter;
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

	private static SKStrokeCap ToSK(StrokeCap cap) => cap switch
	{
		StrokeCap.Round => SKStrokeCap.Round,
		StrokeCap.Square => SKStrokeCap.Square,
		_ => SKStrokeCap.Butt,
	};

	private static SKStrokeJoin ToSK(StrokeJoin join) => join switch
	{
		StrokeJoin.Round => SKStrokeJoin.Round,
		StrokeJoin.Bevel => SKStrokeJoin.Bevel,
		_ => SKStrokeJoin.Miter,
	};

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
