#nullable enable

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text.Json;
using Windows.UI;

namespace Uno.UI.Composition.Drawing;

// SkiaSharp-free Lottie (Bodymovin) model + keyframe evaluation. v1 covers the shape-layer subset (shape/null
// layers, parenting, transforms, bezier/rect/ellipse paths, solid fills + strokes) with full keyframe interpolation
// (linear + cubic-bézier easing + hold). Gradients, trim paths, repeaters, masks, mattes, precomps, image/text
// layers and effects are not modelled yet — an unsupported item is skipped, never fatal. Rendering: ManagedLottie.Render.cs.
internal sealed partial class ManagedLottie
{
	public float Width { get; private init; }
	public float Height { get; private init; }
	public float FrameRate { get; private init; }
	public float InPoint { get; private init; }
	public float OutPoint { get; private init; }
	public IReadOnlyList<Layer> Layers { get; private init; } = Array.Empty<Layer>();
	public IReadOnlyDictionary<int, Layer> LayersByIndex { get; private init; } = new Dictionary<int, Layer>();

	// ---- model ----

	public sealed class Layer
	{
		public int Type;              // ty: 3=null, 4=shape (others skipped in v1)
		public int Index;             // ind
		public int? ParentIndex;      // parent
		public float InPoint;         // ip
		public float OutPoint;        // op
		public Transform Transform = new();
		public IReadOnlyList<ShapeItem> Shapes = Array.Empty<ShapeItem>();
	}

	public sealed class Transform
	{
		public AnimatedVector Anchor = AnimatedVector.Constant(Vector2.Zero);
		public AnimatedVector Position = AnimatedVector.Constant(Vector2.Zero);
		public AnimatedVector Scale = AnimatedVector.Constant(new Vector2(100, 100));
		public AnimatedScalar Rotation = AnimatedScalar.Constant(0);
		public AnimatedScalar Opacity = AnimatedScalar.Constant(100);

		public Matrix3x2 Matrix(float frame)
		{
			var a = Anchor.Evaluate(frame);
			var p = Position.Evaluate(frame);
			var s = Scale.Evaluate(frame) / 100f;
			var r = Rotation.Evaluate(frame) * (float)(Math.PI / 180.0);
			// child-space → parent-space: T(-anchor) · Scale · Rotate · T(position)  (row-vector, apply-first-on-left).
			return Matrix3x2.CreateTranslation(-a)
				* Matrix3x2.CreateScale(s)
				* Matrix3x2.CreateRotation(r)
				* Matrix3x2.CreateTranslation(p);
		}
	}

	public abstract class ShapeItem { }

	public sealed class GroupShape : ShapeItem
	{
		public IReadOnlyList<ShapeItem> Items = Array.Empty<ShapeItem>();
	}

	public sealed class PathShape : ShapeItem { public AnimatedPath Path = AnimatedPath.Empty; }
	public sealed class RectShape : ShapeItem { public AnimatedVector Position = AnimatedVector.Constant(Vector2.Zero); public AnimatedVector Size = AnimatedVector.Constant(Vector2.Zero); public AnimatedScalar Roundness = AnimatedScalar.Constant(0); }
	public sealed class EllipseShape : ShapeItem { public AnimatedVector Position = AnimatedVector.Constant(Vector2.Zero); public AnimatedVector Size = AnimatedVector.Constant(Vector2.Zero); }

	public sealed class FillShape : ShapeItem { public AnimatedColor Color = AnimatedColor.Constant(Windows.UI.Color.FromArgb(255, 0, 0, 0)); public AnimatedScalar Opacity = AnimatedScalar.Constant(100); public bool EvenOdd; }
	public sealed class StrokeShape : ShapeItem { public AnimatedColor Color = AnimatedColor.Constant(Windows.UI.Color.FromArgb(255, 0, 0, 0)); public AnimatedScalar Opacity = AnimatedScalar.Constant(100); public AnimatedScalar Width = AnimatedScalar.Constant(1); public int Cap = 2; public int Join = 2; }

	public sealed class TransformShape : ShapeItem { public Transform Transform = new(); }

	// ---- animated properties ----

	// One keyframe segment: value goes from Start (at Frame) toward the next keyframe's value, eased by the cubic
	// bézier (EaseOut of this kf, EaseIn of the next). Hold snaps to Start for the whole segment. Values are float[]
	// so scalars/vectors/colors share one machine; a path is animated separately (PathKeyframe).
	internal readonly struct Keyframe
	{
		public readonly float Frame;
		public readonly float[] Start;
		public readonly float[]? End;   // legacy 'e'; null → use next keyframe's Start
		public readonly bool Hold;
		public readonly float EaseOutX, EaseOutY, EaseInX, EaseInY;
		public readonly bool HasEase;

		public Keyframe(float frame, float[] start, float[]? end, bool hold, float ox, float oy, float ix, float iy, bool hasEase)
		{
			Frame = frame; Start = start; End = end; Hold = hold;
			EaseOutX = ox; EaseOutY = oy; EaseInX = ix; EaseInY = iy; HasEase = hasEase;
		}
	}

	// Shared keyframe track over float[]; Evaluate lerps componentwise with temporal easing.
	internal sealed class Track
	{
		private readonly float[] _constant;         // set when not animated
		private readonly Keyframe[]? _keyframes;

		private Track(float[] constant) { _constant = constant; }
		private Track(Keyframe[] keyframes) { _keyframes = keyframes; _constant = keyframes.Length > 0 ? keyframes[0].Start : Array.Empty<float>(); }

		public static Track Const(float[] v) => new(v);
		public static Track Animated(Keyframe[] kf) => new(kf);

		public float[] Evaluate(float frame)
		{
			var kf = _keyframes;
			if (kf is null || kf.Length == 0)
			{
				return _constant;
			}
			if (kf.Length == 1 || frame <= kf[0].Frame)
			{
				return kf[0].Start;
			}
			if (frame >= kf[^1].Frame)
			{
				return kf[^1].Start;
			}

			var i = 0;
			while (i < kf.Length - 1 && frame >= kf[i + 1].Frame)
			{
				i++;
			}
			var cur = kf[i];
			var next = kf[i + 1];
			var start = cur.Start;
			var end = cur.End ?? next.Start;
			var span = next.Frame - cur.Frame;
			var local = span > 0 ? (frame - cur.Frame) / span : 0f;
			var t = cur.Hold ? 0f : cur.HasEase ? CubicBezierEase(local, cur.EaseOutX, cur.EaseOutY, cur.EaseInX, cur.EaseInY) : local;

			var n = Math.Min(start.Length, end.Length);
			var result = new float[n];
			for (var c = 0; c < n; c++)
			{
				result[c] = start[c] + (end[c] - start[c]) * t;
			}
			return result;
		}
	}

	internal readonly struct AnimatedScalar
	{
		private readonly Track _track;
		private AnimatedScalar(Track t) => _track = t;
		public static AnimatedScalar Constant(float v) => new(Track.Const(new[] { v }));
		internal static AnimatedScalar FromTrack(Track t) => new(t);
		public float Evaluate(float frame) { var v = _track.Evaluate(frame); return v.Length > 0 ? v[0] : 0f; }
	}

	internal readonly struct AnimatedVector
	{
		private readonly Track _track;
		private AnimatedVector(Track t) => _track = t;
		public static AnimatedVector Constant(Vector2 v) => new(Track.Const(new[] { v.X, v.Y }));
		internal static AnimatedVector FromTrack(Track t) => new(t);
		public Vector2 Evaluate(float frame) { var v = _track.Evaluate(frame); return new Vector2(v.Length > 0 ? v[0] : 0f, v.Length > 1 ? v[1] : 0f); }
	}

	internal readonly struct AnimatedColor
	{
		private readonly Track _track;
		private AnimatedColor(Track t) => _track = t;
		public static AnimatedColor Constant(Color c) => new(Track.Const(new[] { c.R / 255f, c.G / 255f, c.B / 255f, c.A / 255f }));
		internal static AnimatedColor FromTrack(Track t) => new(t);
		public Color Evaluate(float frame)
		{
			var v = _track.Evaluate(frame);
			byte Ch(int i, byte def) => i < v.Length ? (byte)Math.Clamp((float)Math.Round(v[i] * 255f), 0, 255) : def;
			return Color.FromArgb(Ch(3, 255), Ch(0, 0), Ch(1, 0), Ch(2, 0));
		}
	}

	// A bezier shape: vertices with in/out tangents (relative to their vertex) + closed flag.
	internal readonly struct ShapeData
	{
		public readonly Vector2[] Vertices;
		public readonly Vector2[] InTangents;
		public readonly Vector2[] OutTangents;
		public readonly bool Closed;
		public ShapeData(Vector2[] v, Vector2[] i, Vector2[] o, bool closed) { Vertices = v; InTangents = i; OutTangents = o; Closed = closed; }
		public bool IsEmpty => Vertices is null || Vertices.Length == 0;
	}

	// Animated bezier path: static shape, or keyframed shapes (same vertex count) lerped per point.
	internal readonly struct AnimatedPath
	{
		private readonly ShapeData _constant;
		private readonly (float frame, ShapeData shape, bool hold, float ox, float oy, float ix, float iy, bool hasEase)[]? _keyframes;

		public static readonly AnimatedPath Empty = new(new ShapeData(Array.Empty<Vector2>(), Array.Empty<Vector2>(), Array.Empty<Vector2>(), false));

		public AnimatedPath(ShapeData constant) { _constant = constant; _keyframes = null; }
		public AnimatedPath((float, ShapeData, bool, float, float, float, float, bool)[] kf) { _keyframes = kf; _constant = kf.Length > 0 ? kf[0].Item2 : Empty._constant; }

		public ShapeData Evaluate(float frame)
		{
			var kf = _keyframes;
			if (kf is null || kf.Length == 0 || frame <= kf[0].frame)
			{
				return kf is { Length: > 0 } ? kf[0].shape : _constant;
			}
			if (frame >= kf[^1].frame)
			{
				return kf[^1].shape;
			}
			var i = 0;
			while (i < kf.Length - 1 && frame >= kf[i + 1].frame)
			{
				i++;
			}
			var cur = kf[i];
			var next = kf[i + 1];
			var a = cur.shape;
			var b = next.shape;
			if (a.Vertices.Length != b.Vertices.Length)
			{
				return a; // mismatched topology — can't tween; hold the start
			}
			var span = next.frame - cur.frame;
			var local = span > 0 ? (frame - cur.frame) / span : 0f;
			var t = cur.hold ? 0f : cur.hasEase ? CubicBezierEase(local, cur.ox, cur.oy, cur.ix, cur.iy) : local;

			var n = a.Vertices.Length;
			var v = new Vector2[n];
			var inT = new Vector2[n];
			var outT = new Vector2[n];
			for (var k = 0; k < n; k++)
			{
				v[k] = Vector2.Lerp(a.Vertices[k], b.Vertices[k], t);
				inT[k] = Vector2.Lerp(a.InTangents[k], b.InTangents[k], t);
				outT[k] = Vector2.Lerp(a.OutTangents[k], b.OutTangents[k], t);
			}
			return new ShapeData(v, inT, outT, a.Closed);
		}
	}

	// Cubic-bézier timing solve: control points (ox,oy),(ix,iy) between (0,0)-(1,1). Given x, find parameter s with
	// Bx(s)=x (bisection), return By(s). Same math as CSS cubic-bezier / Lottie temporal easing.
	private static float CubicBezierEase(float x, float ox, float oy, float ix, float iy)
	{
		x = Math.Clamp(x, 0f, 1f);
		static float B(float s, float c1, float c2)
		{
			var u = 1f - s;
			return 3f * u * u * s * c1 + 3f * u * s * s * c2 + s * s * s;
		}
		float lo = 0f, hi = 1f, s = x;
		for (var iter = 0; iter < 24; iter++)
		{
			s = (lo + hi) * 0.5f;
			var bx = B(s, ox, ix);
			if (Math.Abs(bx - x) < 1e-4f)
			{
				break;
			}
			if (bx < x) { lo = s; } else { hi = s; }
		}
		return B(s, oy, iy);
	}
}
