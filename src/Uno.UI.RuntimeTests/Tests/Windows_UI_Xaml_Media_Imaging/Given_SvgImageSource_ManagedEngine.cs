#if __SKIA__
#nullable enable

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Uno.UI.Composition.Drawing;
using Windows.Foundation;
using Windows.UI;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Media_Imaging;

/// <summary>
/// Pixel-level coverage for the SkiaSharp-free managed SVG engine (<c>ManagedSvg</c>). Each test crafts an inline
/// SVG exercising one previously-broken feature (rounded rects, fill-rule, currentColor, rgb()%/hsl(), stroke dash
/// and gradient stroke), renders it through the real engine over the managed geometry factory into a recording
/// <see cref="IDrawingSession"/>, then rasterizes the recorded verbs (via <c>ManagedGeometry.FillContains</c>, which
/// honors the fill rule, and by evaluating the recorded gradient shaders). No backend / SkiaSharp is involved.
/// </summary>
[TestClass]
public class Given_SvgImageSource_ManagedEngine
{
	private const int Size = 100;

	[TestMethod]
	public void When_Rect_Rx_Ry_Corners_Are_Rounded()
	{
		// rx=ry=30 rounded rect filled red (also exercises the named-color table).
		const string svg = "<svg xmlns='http://www.w3.org/2000/svg' width='100' height='100' viewBox='0 0 100 100'>" +
			"<rect width='100' height='100' rx='30' ry='30' fill='red'/></svg>";
		var img = Rasterize(svg);

		var corner = img[3, 3];
		var edge = img[50, 3];
		var center = img[50, 50];
		Log(nameof(When_Rect_Rx_Ry_Corners_Are_Rounded), ("corner(3,3)", corner), ("edge(50,3)", edge), ("center(50,50)", center));

		AssertBackground(corner, "corner must be rounded away");
		AssertRed(edge, "straight top edge must be filled");
		AssertRed(center, "center must be filled");
	}

	[TestMethod]
	public void When_FillRule_EvenOdd_Punches_Hole()
	{
		// Outer + inner sub-paths wound the same way: EvenOdd cancels the inner square to a hole; NonZero fills solid.
		const string svg = "<svg xmlns='http://www.w3.org/2000/svg' width='100' height='100' viewBox='0 0 100 100'>" +
			"<path fill-rule='evenodd' fill='red' d='M10,10 H90 V90 H10 Z M35,35 H65 V65 H35 Z'/></svg>";
		var img = Rasterize(svg);

		var hole = img[50, 50];
		var ring = img[20, 50];
		Log(nameof(When_FillRule_EvenOdd_Punches_Hole), ("hole(50,50)", hole), ("ring(20,50)", ring));

		AssertBackground(hole, "even-odd inner sub-path must be a hole");
		AssertRed(ring, "ring between the sub-paths must be filled");
	}

	[TestMethod]
	public void When_CurrentColor_And_Rgb_Percent()
	{
		// color=rgb(0%,50%,0%) => (0,128,0); fill=currentColor must resolve to it.
		const string svg = "<svg xmlns='http://www.w3.org/2000/svg' width='100' height='100' viewBox='0 0 100 100' color='rgb(0%,50%,0%)'>" +
			"<rect width='100' height='100' fill='currentColor'/></svg>";
		var img = Rasterize(svg);

		var center = img[50, 50];
		Log(nameof(When_CurrentColor_And_Rgb_Percent), ("center(50,50)", center));

		Assert.IsTrue(center.G is > 100 and < 160 && center.R < 40 && center.B < 40,
			$"currentColor/rgb(%) expected ~(0,128,0), got {Describe(center)}");
	}

	[TestMethod]
	public void When_Hsl_Color()
	{
		// hsl(240,100%,50%) => pure blue.
		const string svg = "<svg xmlns='http://www.w3.org/2000/svg' width='100' height='100' viewBox='0 0 100 100'>" +
			"<rect width='100' height='100' fill='hsl(240,100%,50%)'/></svg>";
		var img = Rasterize(svg);

		var center = img[50, 50];
		Log(nameof(When_Hsl_Color), ("center(50,50)", center));

		Assert.IsTrue(center.B > 200 && center.R < 40 && center.G < 40, $"hsl() expected blue, got {Describe(center)}");
	}

	[TestMethod]
	public void When_Stroke_Dasharray()
	{
		const string svg = "<svg xmlns='http://www.w3.org/2000/svg' width='100' height='100' viewBox='0 0 100 100'>" +
			"<line x1='0' y1='50' x2='100' y2='50' stroke='blue' stroke-width='10' stroke-dasharray='10 10'/></svg>";
		var img = Rasterize(svg);

		var onDash = img[5, 50];
		var gap = img[15, 50];
		Log(nameof(When_Stroke_Dasharray), ("onDash(5,50)", onDash), ("gap(15,50)", gap));

		Assert.IsTrue(onDash.B > 200 && onDash.R < 40, $"dash 'on' segment must be stroked, got {Describe(onDash)}");
		AssertBackground(gap, "dash 'off' segment must be background");
	}

	[TestMethod]
	public void When_Gradient_Stroke()
	{
		const string svg = "<svg xmlns='http://www.w3.org/2000/svg' width='100' height='100' viewBox='0 0 100 100'>" +
			"<defs><linearGradient id='g' x1='0' y1='0' x2='1' y2='0'>" +
			"<stop offset='0' stop-color='red'/><stop offset='1' stop-color='blue'/></linearGradient></defs>" +
			"<rect x='10' y='10' width='80' height='80' fill='none' stroke='url(#g)' stroke-width='16'/></svg>";
		var img = Rasterize(svg);

		var leftBand = img[10, 50];
		var rightBand = img[90, 50];
		Log(nameof(When_Gradient_Stroke), ("left(10,50)", leftBand), ("right(90,50)", rightBand));

		Assert.IsTrue(leftBand.R > leftBand.B + 40, $"left stroke band must be reddish, got {Describe(leftBand)}");
		Assert.IsTrue(rightBand.B > rightBand.R + 40, $"right stroke band must be bluish, got {Describe(rightBand)}");
	}

	private static PixelGrid Rasterize(string markup)
	{
		var renderer = new ManagedSvgRenderer();
		var doc = renderer.Parse(Encoding.UTF8.GetBytes(markup), new ManagedGeometryFactory(), new RecordingDrawingFactory());
		Assert.IsNotNull(doc, "managed SVG engine failed to parse the markup");

		var session = new RecordingSession();
		doc!.Render(session, new Size(Size, Size));
		return session.Rasterize(Size, Size);
	}

	private static void AssertRed(Color c, string because) =>
		Assert.IsTrue(c.R > 200 && c.G < 40 && c.B < 40, $"{because}: expected red, got {Describe(c)}");

	private static void AssertBackground(Color c, string because) =>
		Assert.IsTrue(c.A == 0, $"{because}: expected uncovered background, got {Describe(c)}");

	private static string Describe(Color c) => $"#{c.A:X2}{c.R:X2}{c.G:X2}{c.B:X2}";

	private static void Log(string test, params (string label, Color color)[] samples)
	{
		var sb = new StringBuilder($"[ManagedSvg] {test}:");
		foreach (var (label, color) in samples)
		{
			sb.Append(' ').Append(label).Append('=').Append(Describe(color));
		}

		Console.WriteLine(sb.ToString());
	}

	/// <summary>Row-major ARGB pixel grid. Uncovered pixels have A==0 (background).</summary>
	private sealed class PixelGrid
	{
		private readonly Color[] _pixels;
		private readonly int _width;

		public PixelGrid(Color[] pixels, int width)
		{
			_pixels = pixels;
			_width = width;
		}

		public Color this[int x, int y] => _pixels[y * _width + x];
	}

	// Records the neutral draw verbs the engine issues, then rasterizes them (painter's order) by point-sampling
	// the managed geometry's own fill test — so rounded corners, fill-rule holes and dash gaps show up as coverage.
	private sealed class RecordingSession : IDrawingSession
	{
		private readonly List<Op> _ops = new();
		private readonly Stack<(Matrix3x2 ctm, List<IGeometry> clips)> _stack = new();
		private Matrix3x2 _ctm = Matrix3x2.Identity;
		private List<IGeometry> _clips = new();

		private readonly record struct Op(IGeometry? Geometry, Rect Rect, Color Color, IShader? Shader, Matrix3x2 Ctm, IGeometry[] Clips);

		public PixelGrid Rasterize(int width, int height)
		{
			var pixels = new Color[width * height];
			for (var y = 0; y < height; y++)
			{
				for (var x = 0; x < width; x++)
				{
					var device = new Vector2(x + 0.5f, y + 0.5f);
					var color = default(Color); // A==0 background
					foreach (var op in _ops)
					{
						if (!Matrix3x2.Invert(op.Ctm, out var inv))
						{
							continue;
						}

						var p = Vector2.Transform(device, inv);
						if (!InClips(op.Clips, p))
						{
							continue;
						}

						if (op.Geometry is { } geom)
						{
							if (geom.FillContains(p))
							{
								color = op.Color;
							}
						}
						else if (Contains(op.Rect, p))
						{
							color = op.Shader is IEvaluableShader shader ? shader.Eval(p) : op.Color;
						}
					}

					pixels[y * width + x] = color;
				}
			}

			return new PixelGrid(pixels, width);
		}

		private static bool InClips(IGeometry[] clips, Vector2 p)
		{
			foreach (var clip in clips)
			{
				if (!clip.FillContains(p))
				{
					return false;
				}
			}

			return true;
		}

		private static bool Contains(Rect r, Vector2 p) =>
			p.X >= r.X && p.X <= r.X + r.Width && p.Y >= r.Y && p.Y <= r.Y + r.Height;

		public Matrix4x4 TotalMatrix => new(_ctm.M11, _ctm.M12, 0, 0, _ctm.M21, _ctm.M22, 0, 0, 0, 0, 1, 0, _ctm.M31, _ctm.M32, 0, 1);
		public object? NativeSurface => null;

		public void SetMatrix(in Matrix4x4 matrix) => _ctm = To2D(matrix);
		public void Concat(in Matrix4x4 matrix) => _ctm = To2D(matrix) * _ctm;
		public void Translate(float dx, float dy) => _ctm = Matrix3x2.CreateTranslation(dx, dy) * _ctm;
		public void Scale(float sx, float sy) => _ctm = Matrix3x2.CreateScale(sx, sy) * _ctm;

		public int SaveCount => _stack.Count;

		public int Save()
		{
			var depth = _stack.Count;
			_stack.Push((_ctm, new List<IGeometry>(_clips)));
			return depth;
		}

		public void Restore()
		{
			if (_stack.Count > 0)
			{
				(_ctm, _clips) = _stack.Pop();
			}
		}

		public void RestoreToCount(int count)
		{
			while (_stack.Count > count)
			{
				(_ctm, _clips) = _stack.Pop();
			}
		}

		public void ClipPath(IGeometry geometry, ClipOperation operation = ClipOperation.Intersect, bool antialias = false)
		{
			if (operation == ClipOperation.Intersect)
			{
				_clips.Add(geometry);
			}
		}

		public void DrawPath(IGeometry geometry, Color color, bool antialias = false) =>
			_ops.Add(new Op(geometry, default, color, null, _ctm, _clips.ToArray()));

		public void StrokePath(IGeometry geometry, Color color, float strokeWidth, bool antialias = false)
		{
			var stroke = geometry.GetStrokeFillGeometry(new StrokeStyle { Thickness = strokeWidth, MiterLimit = 4f });
			_ops.Add(new Op(stroke, default, color, null, _ctm, _clips.ToArray()));
		}

		public void DrawRect(in Rect rect, Color color, bool antialias = false) =>
			_ops.Add(new Op(null, rect, color, null, _ctm, _clips.ToArray()));

		public void DrawRect(in Rect rect, IShader shader, bool antialias = false) =>
			_ops.Add(new Op(null, rect, default, shader, _ctm, _clips.ToArray()));

		private static Matrix3x2 To2D(in Matrix4x4 m) => new(m.M11, m.M12, m.M21, m.M22, m.M41, m.M42);

		// Unused verbs for these SVGs.
		public void SaveLayer(bool antialias = false) => Save();
		public void SaveLayer(IColorFilter colorFilter, bool antialias = false) => Save();
		public void SaveLayer(BlendMode blendMode, bool antialias = false) => Save();
		public void SaveLayer(IEffectFilter filter) => Save();
		public void ClipRect(in Rect rect, ClipOperation operation = ClipOperation.Intersect, bool antialias = false) { }
		public void ClipRoundRect(in RoundRectangle roundRect, ClipOperation operation = ClipOperation.Intersect, bool antialias = false) { }
		public void Clear(Color color) { }
		public void DrawRoundedRect(in Rect rect, Vector4 radii, Color color, bool antialias = false) { }
		public void DrawRoundedRectBorder(in Rect outer, Vector4 outerRadii, in Rect inner, Vector4 innerRadii, Color color, bool antialias = false) { }
		public void DrawShadow(IGeometry silhouette, Color color, float sigmaX, float sigmaY, bool additive, bool antialias = false) { }
		public void DrawLine(Vector2 p0, Vector2 p1, Color color, float strokeWidth, bool antialias = false) { }
		public void DrawImage(ITexture texture, float x, float y, ImageSampling sampling, float opacity = 1f, bool antialias = false) { }
		public void DrawImage(ITexture texture, float x, float y, ImageSampling sampling, IColorFilter colorFilter, bool antialias = false) { }
		public void DrawImageNineSlice(ITexture texture, in Rect centerSlice, in Rect destination, bool centerHollow, bool antialias = false) { }
		public void DrawEffectBackdrop(IEffectFilter filter, float opacity) { }
	}

	private interface IEvaluableShader : IShader
	{
		Color Eval(Vector2 point);
	}

	// Minting stub for the gradient shaders the engine asks for — records the parameters and evaluates a color at a
	// point so gradient fills/strokes can be checked. All other factory members are unused by these SVGs.
	private sealed class RecordingDrawingFactory : IDrawingFactory
	{
		public IShader CreateLinearGradientShader(Vector2 start, Vector2 end, Color[] colors, float[] colorPositions, GradientTileMode tileMode, Matrix3x2 localMatrix)
			=> new LinearShader(start, end, colors, colorPositions);

		public IShader CreateRadialGradientShader(Vector2 center, Vector2 gradientOrigin, float radiusX, float radiusY, Color[] colors, float[] colorPositions, GradientTileMode tileMode, Matrix3x2 localMatrix)
			=> new RadialShader(center, MathF.Max(radiusX, 0.001f), colors, colorPositions);

		public ITexture RenderOffscreen(int pixelWidth, int pixelHeight, Action<IDrawingSession> render) => throw new NotSupportedException();
		public Task<IImage> SnapshotAsync(ITexture texture) => throw new NotSupportedException();
		public ITexture CreateTexture(IImage image) => throw new NotSupportedException();
		public IColorFilter CreateBlendModeColorFilter(Color color, BlendMode mode) => throw new NotSupportedException();
		public IColorFilter CreateColorMatrixColorFilter(float[] matrix) => throw new NotSupportedException();
		public IEffectFilter? CreateEffectFilter(EffectNode tree, Rect bounds) => throw new NotSupportedException();
		public IEffectFilter CreateDropShadowFilter(float dx, float dy, float sigmaX, float sigmaY, Color color) => throw new NotSupportedException();
		public ICommandRecorder CreateRecording() => throw new NotSupportedException();
	}

	private sealed class LinearShader : IEvaluableShader
	{
		private readonly Vector2 _start;
		private readonly Vector2 _axis;
		private readonly float _lenSq;
		private readonly Color[] _colors;
		private readonly float[] _positions;

		public LinearShader(Vector2 start, Vector2 end, Color[] colors, float[] positions)
		{
			_start = start;
			_axis = end - start;
			_lenSq = MathF.Max(_axis.LengthSquared(), 0.0001f);
			_colors = colors;
			_positions = positions;
		}

		public Color Eval(Vector2 p) => Sample(_colors, _positions, Math.Clamp(Vector2.Dot(p - _start, _axis) / _lenSq, 0f, 1f));
	}

	private sealed class RadialShader : IEvaluableShader
	{
		private readonly Vector2 _center;
		private readonly float _radius;
		private readonly Color[] _colors;
		private readonly float[] _positions;

		public RadialShader(Vector2 center, float radius, Color[] colors, float[] positions)
		{
			_center = center;
			_radius = radius;
			_colors = colors;
			_positions = positions;
		}

		public Color Eval(Vector2 p) => Sample(_colors, _positions, Math.Clamp((p - _center).Length() / _radius, 0f, 1f));
	}

	private static Color Sample(Color[] colors, float[] positions, float t)
	{
		if (colors.Length == 0)
		{
			return default;
		}

		if (t <= positions[0])
		{
			return colors[0];
		}

		for (var i = 1; i < colors.Length; i++)
		{
			if (t <= positions[i])
			{
				var span = MathF.Max(positions[i] - positions[i - 1], 0.0001f);
				var f = (t - positions[i - 1]) / span;
				return Color.FromArgb(
					Lerp(colors[i - 1].A, colors[i].A, f),
					Lerp(colors[i - 1].R, colors[i].R, f),
					Lerp(colors[i - 1].G, colors[i].G, f),
					Lerp(colors[i - 1].B, colors[i].B, f));
			}
		}

		return colors[^1];
	}

	private static byte Lerp(byte a, byte b, float f) => (byte)Math.Clamp(a + (b - a) * f, 0f, 255f);
}
#endif
