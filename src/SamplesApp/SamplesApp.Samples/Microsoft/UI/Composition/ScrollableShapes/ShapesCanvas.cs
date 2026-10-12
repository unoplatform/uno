#nullable enable

using System;
using System.Diagnostics;
using System.Numerics;
using SkiaSharp;
using Uno.WinUI.Graphics2DSK;
using Windows.Foundation;

namespace UITests.Shared.Windows_UI_Composition.ScrollableShapes;

internal sealed partial class ShapesCanvas : SKCanvasElement
{
	private const float Distance = 350f;
	private const float TiltRadians = 0.2f;
	private const float TurnSeconds = 10f;

	private static readonly float TiltCos = MathF.Cos(TiltRadians);
	private static readonly float TiltSin = MathF.Sin(TiltRadians);

	private readonly Vector3[][] _shapes = Shapes.Build();
	private readonly Stopwatch _clock = Stopwatch.StartNew();
	private double _lastSettle;

	private readonly SKPoint[] _near = new SKPoint[Shapes.PointCount];
	private readonly SKPoint[] _mid = new SKPoint[Shapes.PointCount];
	private readonly SKPoint[] _far = new SKPoint[Shapes.PointCount];

	internal int ShapeCount => _shapes.Length;

	private float _morph;
	private float _target;

	internal double PageWidth { get; set; } = 1;

	internal float MorphPosition => _morph;

	/// <summary>Where the flip is headed; the cloud eases there over the next few frames.</summary>
	internal void GoTo(int index) => _target = Math.Clamp(index, 0, _shapes.Length - 1);

	/// <summary>
	/// Exact position mid-swipe, read from the flip's own scroller, so the morph follows the finger
	/// instead of waiting for the page to land.
	/// </summary>
	internal void Track(double pages)
	{
		_morph = (float)Math.Clamp(pages, 0, _shapes.Length - 1);
		_target = _morph;
	}

	protected override void RenderOverride(SKCanvas canvas, Size area)
	{
		var w = (float)area.Width;
		var h = (float)area.Height;
		canvas.Clear(SKColors.Black);
		if (w <= 0 || h <= 0)
		{
			return;
		}

		var cx = w * 0.5f;
		var cy = h * 0.5f;

		DrawGlow(canvas, cx, cy, w);

		if (MathF.Abs(_morph - _target) > 0.0005f)
		{
			// Ease to the nearest shape on release. Frame-rate independent, so a slow frame travels
			// the same distance as several quick ones instead of snapping.
			var dt = MathF.Min(0.05f, (float)(_clock.Elapsed.TotalSeconds - _lastSettle));
			_morph += (_target - _morph) * (1f - MathF.Exp(-12f * dt));
			if (MathF.Abs(_morph - _target) <= 0.0005f)
			{
				_morph = _target;
			}
		}

		_lastSettle = _clock.Elapsed.TotalSeconds;

		var pos = MorphPosition;
		var idx = Math.Clamp((int)MathF.Floor(pos), 0, _shapes.Length - 1);
		var next = Math.Min(idx + 1, _shapes.Length - 1);
		var t = Math.Clamp(pos - idx, 0f, 1f);

		var time = (float)(_clock.Elapsed.TotalSeconds / TurnSeconds * Math.PI * 2);
		var cosT = MathF.Cos(time);
		var sinT = MathF.Sin(time);

		// Shapes are authored 200 units tall for a phone. Zoom after projection rather than scaling
		// the cloud, so the perspective term keeps working in the units it was tuned for.
		var fit = MathF.Max(0.6f, MathF.Min(w, h) * 0.45f / 200f);

		var a = _shapes[idx];
		var b = _shapes[next];

		var nearCount = 0;
		var midCount = 0;
		var farCount = 0;

		for (var i = 0; i < Shapes.PointCount; i++)
		{
			var p = Vector3.Lerp(a[i], b[i], t);

			// Fixed tilt about X, then the turntable about Y - the order the reference uses, and it
			// matters: tilting after the spin would wobble the whole cloud instead of leaning it.
			var y1 = p.Y * TiltCos - p.Z * TiltSin;
			var z1 = p.Y * TiltSin + p.Z * TiltCos;
			var x2 = p.X * cosT + z1 * sinT;
			var z2 = -p.X * sinT + z1 * cosT;

			var scale = Distance / (Distance + z2);
			var pt = new SKPoint(cx + x2 * scale * fit, cy + y1 * scale * fit);

			if (scale > 1.1f)
			{
				_near[nearCount++] = pt;
			}
			else if (scale > 0.9f)
			{
				_mid[midCount++] = pt;
			}
			else
			{
				_far[farCount++] = pt;
			}
		}

		// Span the gradient across the shape, not the window: on a wide desktop canvas a window-wide
		// gradient leaves the whole cloud sitting on the white midpoint.
		var span = 130f * fit;
		using var shader = SKShader.CreateLinearGradient(
			new SKPoint(cx - span, cy - span),
			new SKPoint(cx + span, cy + span),
			new[] { new SKColor(0x00, 0xD9, 0xFF), SKColors.White, new SKColor(0xFF, 0x00, 0x6E) },
			null,
			SKShaderTileMode.Clamp);

		using var paint = new SKPaint
		{
			IsAntialias = true,
			Style = SKPaintStyle.Stroke,
			StrokeCap = SKStrokeCap.Round,
			Shader = shader,
		};

		// Far to near, so the nearest points land on top and read as the front of the cloud.
		DrawBin(canvas, paint, _far, farCount, 0.9f * fit);
		DrawBin(canvas, paint, _mid, midCount, 0.9f * fit);
		DrawBin(canvas, paint, _near, nearCount, 1.0f * fit);

		DrawPaginator(canvas, cx, h, pos);
	}

	private static void DrawBin(SKCanvas canvas, SKPaint paint, SKPoint[] points, int count, float width)
	{
		if (count == 0)
		{
			return;
		}

		paint.StrokeWidth = width;
		var slice = new SKPoint[count];
		Array.Copy(points, slice, count);
		canvas.DrawPoints(SKPointMode.Points, slice, paint);
	}

	/// <summary>
	/// A wide radial wash behind the cloud. The reference blurs a filled circle on its own static
	/// canvas; a gradient with the same falloff costs nothing per frame and reads the same.
	/// </summary>
	private static void DrawGlow(SKCanvas canvas, float cx, float cy, float w)
	{
		var radius = w * 0.6f;
		using var shader = SKShader.CreateRadialGradient(
			new SKPoint(cx, cy),
			radius,
			new[] { new SKColor(0xFF, 0xFF, 0xFF, 0x40), new SKColor(0xFF, 0xFF, 0xFF, 0x00) },
			new[] { 0f, 1f },
			SKShaderTileMode.Clamp);

		using var paint = new SKPaint { Shader = shader, IsAntialias = true };
		canvas.DrawCircle(cx, cy, radius, paint);
	}

	private void DrawPaginator(SKCanvas canvas, float cx, float h, float pos)
	{
		const float Dot = 6f;
		const float Spacing = 18f;

		var total = (_shapes.Length - 1) * Spacing;
		var x = cx - total * 0.5f;
		var y = h - 48f;

		using var paint = new SKPaint { IsAntialias = true };
		for (var i = 0; i < _shapes.Length; i++)
		{
			// Nearness to this page, so the active dot grows and brightens as the morph crosses it.
			var near = Math.Clamp(1f - MathF.Abs(pos - i), 0f, 1f);
			paint.Color = new SKColor(0xFF, 0xFF, 0xFF, (byte)(60 + 195 * near));
			canvas.DrawCircle(x + i * Spacing, y, Dot * (0.5f + 0.5f * near), paint);
		}
	}
}
