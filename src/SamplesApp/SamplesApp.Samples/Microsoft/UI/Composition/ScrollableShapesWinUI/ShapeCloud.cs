#nullable enable

using System;
using System.Numerics;
using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Hosting;
using UITests.Shared.Windows_UI_Composition.ScrollableShapes;
using Windows.UI;

namespace UITests.Shared.Windows_UI_Composition.ScrollableShapesWinUI;

/// <summary>
/// The morphing cloud of the Skia sample rebuilt out of Composition shapes: one ellipse per point,
/// spread over three ShapeVisuals that stand in for the Skia version's depth bins. Nothing is drawn
/// by hand, and the geometry, sizes and bin thresholds are the ones the Skia canvas uses.
/// </summary>
internal sealed class ShapeCloud
{
	private const float Distance = 350f;
	private const float TiltRadians = 0.2f;
	private const float TurnSeconds = 10f;

	/// <summary>Enough steps that the banding is finer than a dot; the Skia version spans one shader
	/// across the cloud, which no per-shape brush can reproduce.</summary>
	private const int PaletteSteps = 64;

	private static readonly float TiltCos = MathF.Cos(TiltRadians);
	private static readonly float TiltSin = MathF.Sin(TiltRadians);

	private readonly Vector3[][] _shapes = Shapes.Build();
	private readonly CompositionSpriteShape[] _dots = new CompositionSpriteShape[Shapes.PointCount];
	private readonly CompositionEllipseGeometry[] _geometry = new CompositionEllipseGeometry[3];
	private readonly ShapeVisual[] _bins = new ShapeVisual[3];
	private readonly CompositionColorBrush[] _palette = new CompositionColorBrush[PaletteSteps];
	private readonly int[] _tint = new int[Shapes.PointCount];
	private readonly int[] _bin = new int[Shapes.PointCount];

	private float _morph;
	private float _target;
	private float _fit;

	internal ShapeCloud(UIElement host)
	{
		var compositor = ElementCompositionPreview.GetElementVisual(host).Compositor;
		var root = compositor.CreateContainerVisual();

		for (var i = 0; i < PaletteSteps; i++)
		{
			_palette[i] = compositor.CreateColorBrush(Ramp(i / (float)(PaletteSteps - 1)));
		}

		// Far, then mid, then near: child order is the painter's order, so the nearest points land on
		// top and read as the front of the cloud.
		for (var b = 0; b < _bins.Length; b++)
		{
			_geometry[b] = compositor.CreateEllipseGeometry();
			_bins[b] = compositor.CreateShapeVisual();
			root.Children.InsertAtTop(_bins[b]);
		}

		var start = PaletteSteps / 2;
		for (var i = 0; i < _dots.Length; i++)
		{
			var dot = compositor.CreateSpriteShape(_geometry[1]);
			dot.FillBrush = _palette[start];
			_tint[i] = start;
			_bin[i] = 1;
			_dots[i] = dot;
			_bins[1].Shapes.Add(dot);
		}

		ElementCompositionPreview.SetElementChildVisual(host, root);
	}

	internal int ShapeCount => _shapes.Length;

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

	internal void Update(float w, float h, double seconds, float dt)
	{
		if (w <= 0 || h <= 0)
		{
			return;
		}

		if (MathF.Abs(_morph - _target) > 0.0005f)
		{
			// Frame-rate independent, so a slow frame travels the same distance as several quick ones.
			_morph += (_target - _morph) * (1f - MathF.Exp(-12f * MathF.Min(0.05f, dt)));
			if (MathF.Abs(_morph - _target) <= 0.0005f)
			{
				_morph = _target;
			}
		}

		var cx = w * 0.5f;
		var cy = h * 0.5f;

		// Shapes are authored 200 units tall for a phone; zoom after projection so the perspective
		// term keeps working in the units it was tuned for.
		var fit = MathF.Max(0.6f, MathF.Min(w, h) * 0.45f / 200f);
		if (fit != _fit)
		{
			_fit = fit;
			foreach (var bin in _bins)
			{
				bin.Size = new Vector2(w, h);
			}

			// Diameters 0.9 and 1.0 of the fit, the stroke widths the Skia version gives its three
			// bins - the dot does not shrink with depth there, so it does not here either.
			_geometry[0].Radius = _geometry[1].Radius = new Vector2(0.45f * fit, 0.45f * fit);
			_geometry[2].Radius = new Vector2(0.5f * fit, 0.5f * fit);
		}

		var idx = Math.Clamp((int)MathF.Floor(_morph), 0, _shapes.Length - 1);
		var next = Math.Min(idx + 1, _shapes.Length - 1);
		var blend = Math.Clamp(_morph - idx, 0f, 1f);

		var time = (float)(seconds / TurnSeconds * Math.PI * 2);
		var cosT = MathF.Cos(time);
		var sinT = MathF.Sin(time);

		var span = 130f * fit;
		var a = _shapes[idx];
		var b = _shapes[next];

		for (var i = 0; i < _dots.Length; i++)
		{
			var p = Vector3.Lerp(a[i], b[i], blend);

			// Fixed tilt about X, then the turntable about Y - tilting after the spin would wobble
			// the whole cloud instead of leaning it.
			var y1 = p.Y * TiltCos - p.Z * TiltSin;
			var z1 = p.Y * TiltSin + p.Z * TiltCos;
			var x2 = p.X * cosT + z1 * sinT;
			var z2 = -p.X * sinT + z1 * cosT;

			var depth = Distance / (Distance + z2);
			var px = cx + x2 * depth * fit;
			var py = cy + y1 * depth * fit;

			var dot = _dots[i];
			dot.Offset = new Vector2(px, py);

			var bin = depth > 1.1f ? 2 : depth > 0.9f ? 1 : 0;
			if (bin != _bin[i])
			{
				_bins[_bin[i]].Shapes.Remove(dot);
				_bin[i] = bin;
				dot.Geometry = _geometry[bin];
				_bins[bin].Shapes.Add(dot);
			}

			// The Skia version spans one gradient across the cloud; a per-shape brush cannot, so each
			// point picks the palette entry its own position lands on.
			var u = ((px - cx) + (py - cy) + 2f * span) / (4f * span);
			var tint = Math.Clamp((int)(u * (PaletteSteps - 1) + 0.5f), 0, PaletteSteps - 1);
			if (tint != _tint[i])
			{
				_tint[i] = tint;
				dot.FillBrush = _palette[tint];
			}
		}
	}

	private static Color Ramp(float u) => u < 0.5f
		? Mix(Color.FromArgb(255, 0x00, 0xD9, 0xFF), Colors.White, u * 2f)
		: Mix(Colors.White, Color.FromArgb(255, 0xFF, 0x00, 0x6E), (u - 0.5f) * 2f);

	private static Color Mix(Color a, Color b, float t) => Color.FromArgb(
		255,
		(byte)(a.R + (b.R - a.R) * t),
		(byte)(a.G + (b.G - a.G) * t),
		(byte)(a.B + (b.B - a.B) * t));
}
