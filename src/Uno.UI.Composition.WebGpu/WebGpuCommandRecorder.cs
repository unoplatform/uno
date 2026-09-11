// Records drawing calls into a WebGpuRenderRecord: every command in the recording's own space, with the matrix and
// clip current when it was drawn. Nothing is resolved to the screen here; the present walk does that.
#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Uno.WebGpu.Native;
using static Uno.WebGpu.Native.WGPU;
using Uno.UI.Composition.Drawing;
using Uno.Foundation.Logging;
using Windows.Graphics.Effects.Interop;
using Windows.Foundation;
using WColor = Windows.UI.Color;

namespace Uno.UI.Composition.WebGpu;

public sealed unsafe class WebGpuCommandRecorder : ICommandRecorder
{
	// A save frame carries the matrix/clip to restore. Layer frames additionally redirect emitted commands into
	// a sub-list until Restore, which composites that sub-list (as a LayerCmd) back onto the parent.
	private struct SaveEntry { public Matrix4x4 M; public ClipData Clip; public bool IsLayer; public List<WebGpuCommand> ParentTarget; public int CompositeMode; public float[] ColorMatrix; public WebGpuEffectFilter Effect; public float[] PendingColorMatrix; }
	private readonly Stack<SaveEntry> _stack = new();
	private Matrix4x4 _m = Matrix4x4.Identity;
	private ClipData _clip = ClipData.None;
	private float[] _pendingColorMatrix;   // active effect colour matrix, applied per DrawImage in the image shader
	private readonly WebGpuRenderRecord _data = new();
	private List<WebGpuCommand> _target;   // current emit target (root command list, or a layer's list)
	// The owning drawing factory, surfaced as IDrawingSession.Factory so an add-in painting into this recording mints
	// session-native textures within the paint scope.
	private readonly IDrawingFactory _factory;

	public WebGpuCommandRecorder(IDrawingFactory factory) { _target = _data.Commands; _factory = factory; }

	public Matrix4x4 TotalMatrix => _m;
	public void SetMatrix(in Matrix4x4 matrix) => _m = matrix;
	public void Concat(in Matrix4x4 matrix) => _m = matrix * _m;
	public void Translate(float dx, float dy) => _m = Matrix4x4.CreateTranslation(dx, dy, 0) * _m;
	public void Scale(float sx, float sy) => _m = Matrix4x4.CreateScale(sx, sy, 1) * _m;
	// Returns the PRE-push depth, matching SKCanvas.Save(): RestoreToCount(count) pops entries while Count > count,
	// so it must be handed the depth to restore *to* (before this save). Returning the post-push count made
	// RestoreToCount a no-op, leaking _m/_clip across sibling visuals (identity-local visuals — e.g. opaque
	// container backgrounds — inherited a sibling's transform and painted over content).
	public int Save() { var pre = _stack.Count; _stack.Push(new SaveEntry { M = _m, Clip = _clip, PendingColorMatrix = _pendingColorMatrix }); return pre; }
	public int SaveCount => _stack.Count;
	public object NativeSurface => null;
	public IDrawingFactory Factory => _factory;
	public void Restore()
	{
		if (_stack.Count == 0) { return; }
		var t = _stack.Pop(); _m = t.M; _clip = t.Clip; _pendingColorMatrix = t.PendingColorMatrix;
		if (t.IsLayer)
		{
			var layerCmds = _target;
			_target = t.ParentTarget;
			_target.Add(new LayerCmd { Commands = layerCmds, CompositeMode = t.CompositeMode, ColorMatrix = t.ColorMatrix, ShadowEffect = t.Effect, Clip = _clip });
		}
	}
	public void RestoreToCount(int count) { while (_stack.Count > count) { Restore(); } }

	private void PushLayer(int compositeMode, float[] colorMatrix, WebGpuEffectFilter effect = null)
	{
		_stack.Push(new SaveEntry { M = _m, Clip = _clip, IsLayer = true, ParentTarget = _target, CompositeMode = compositeMode, ColorMatrix = colorMatrix, Effect = effect, PendingColorMatrix = _pendingColorMatrix });
		_target = new List<WebGpuCommand>();
	}
	public void SaveLayer() => PushLayer(0, null);
	public void SaveLayer(IColorFilter colorFilter)
	{
		// A 4x5 colour-matrix filter (effect brush): apply it directly in the image shader — matching the original
		// webgpu branch's AddImage(colorMatrix) — instead of an offscreen layer. Scope it to the matching Restore.
		if ((colorFilter as WebGpuColorFilter)?.Matrix is { } matrix)
		{
			_stack.Push(new SaveEntry { M = _m, Clip = _clip, PendingColorMatrix = _pendingColorMatrix });
			_pendingColorMatrix = matrix;
			return;
		}

		// Unreachable by design: every IColorFilter produced by the factory that reaches SaveLayer is a colour
		// matrix (CreateColorMatrixColorFilter — alpha mask / effect recipe). A blend-mode filter is only ever
		// routed to DrawImage. If this fires, a new caller has broken that invariant and the filter is being
		// silently dropped (a plain layer, below) — render output will be wrong. Fix the caller or implement the case.
		if (this.Log().IsEnabled(LogLevel.Error))
		{
			this.Log().Error($"WebGPU SaveLayer(IColorFilter) reached with a non-colour-matrix filter ('{colorFilter?.GetType().Name ?? "null"}'); only colour-matrix layer filters are supported. The filter is being ignored — this path is not expected to be taken.");
		}

		PushLayer(0, null);
	}
	public void SaveLayerMask() => PushLayer(1, null);   // 1 = DstIn composite
	public void SaveLayer(IEffectFilter filter) => PushLayer(0, null, filter as WebGpuEffectFilter);
	// Device-space AABB of a mapped rect (its 4 corners), for the scissor / fast reject.
	private Vector4 DeviceAabb(in Rect rect)
	{
		var a = Map((float)rect.Left, (float)rect.Top); var b = Map((float)rect.Right, (float)rect.Top);
		var c = Map((float)rect.Right, (float)rect.Bottom); var d = Map((float)rect.Left, (float)rect.Bottom);
		var l = MathF.Min(MathF.Min(a.X, b.X), MathF.Min(c.X, d.X)); var t = MathF.Min(MathF.Min(a.Y, b.Y), MathF.Min(c.Y, d.Y));
		var r = MathF.Max(MathF.Max(a.X, b.X), MathF.Max(c.X, d.X)); var bo = MathF.Max(MathF.Max(a.Y, b.Y), MathF.Max(c.Y, d.Y));
		return new Vector4(l, t, r, bo);
	}

	public void ClipRect(in Rect rect, ClipOperation operation = ClipOperation.Intersect)
	{
		// Difference has no AABB representation: keeping the area OUTSIDE the rect leaves a visible region that
		// extends past it, so tightening the scissor would be wrong. Route it through the rounded-clip path, which
		// already handles exclusion (and whose coverage degenerates to a sharp box at zero radii) — Intersect keeps
		// the cheap AABB-only tightening below and consumes no clip slot.
		if (operation == ClipOperation.Difference)
		{
			ClipRoundRect(new RoundRectangle { Rect = rect }, operation);
			return;
		}

		// Tighten the scissor AABB; any active rounded shape is preserved (Intersect only).
		var a = DeviceAabb(rect);
		_clip.Aabb = new Vector4(MathF.Max(_clip.Aabb.X, a.X), MathF.Max(_clip.Aabb.Y, a.Y), MathF.Min(_clip.Aabb.Z, a.Z), MathF.Min(_clip.Aabb.W, a.W));
		_clip.ScissorInert = false;
		// Under a rotation or skew the box only bounds the rect; the exact edge is an entry with square corners.
		if (_m.M12 != 0 || _m.M21 != 0)
		{
			PushEntry(rect, Vector4.Zero, Vector4.Zero, exclude: false);
		}
	}

	// The rounded rect stays in the recorder's current space; the entry carries the way back into it from the
	// clip's space, so a rotated or skewed clip is exact rather than its bounding box.
	private void PushEntry(in Rect rect, Vector4 radii, Vector4 radiiY, bool exclude)
	{
		var m = new Matrix3x2(_m.M11, _m.M12, _m.M21, _m.M22, _m.M41, _m.M42);
		if (!Matrix3x2.Invert(m, out var inv))
		{
			// A collapsed transform draws nothing anyway; keep the clip well-formed.
			inv = Matrix3x2.Identity;
		}
		ClipData.PushEntry(ref _clip, new ClipEntry
		{
			M = inv,
			Rect = new Vector4((float)rect.Left, (float)rect.Top, (float)rect.Right, (float)rect.Bottom),
			Radii = radii,
			RadiiY = radiiY,
			Exclude = exclude,
		});
	}

	public void ClipRoundRect(in RoundRectangle roundRect, ClipOperation operation = ClipOperation.Intersect)
	{
		var aabb = DeviceAabb(roundRect.Rect);
		var exclude = operation == ClipOperation.Difference;
		// Nested rounded clips stack (all ANDed in clipCov) instead of the innermost overwriting the outer.
		PushEntry(roundRect.Rect,
			new Vector4(roundRect.TopLeft.X, roundRect.TopRight.X, roundRect.BottomRight.X, roundRect.BottomLeft.X),
			new Vector4(roundRect.TopLeft.Y, roundRect.TopRight.Y, roundRect.BottomRight.Y, roundRect.BottomLeft.Y),
			exclude);
		// Difference (PushClipExclude): keep the area OUTSIDE the rounded rect — so DON'T tighten the scissor to it
		// (the visible region extends past the rect); the per-fragment clipCov inverts the coverage.
		if (!exclude)
		{
			_clip.Aabb = new Vector4(MathF.Max(_clip.Aabb.X, aabb.X), MathF.Max(_clip.Aabb.Y, aabb.Y), MathF.Min(_clip.Aabb.Z, aabb.Z), MathF.Min(_clip.Aabb.W, aabb.W));
			_clip.ScissorInert = false;
		}
	}

	public void ClipPath(IGeometry geometry, ClipOperation operation = ClipOperation.Intersect)
	{
		// A geometry that advertises itself as a single (rounded) rect clips analytically (an entry / plain
		// scissor) instead of costing a coverage-mask bake and defeating coalescing.
		if (geometry.TryGetRoundRect() is { } rr)
		{
			if (operation == ClipOperation.Intersect
				&& rr.TopLeft == Vector2.Zero && rr.TopRight == Vector2.Zero && rr.BottomRight == Vector2.Zero && rr.BottomLeft == Vector2.Zero)
			{
				ClipRect(rr.Rect, operation);
			}
			else
			{
				ClipRoundRect(rr, operation);
			}
			return;
		}

		// Tighten the scissor to the path bounds ONLY for Intersect (the path lies within its bounds). For
		// Difference the visible region is OUTSIDE the path and extends past its bounds, so tightening to the
		// bounds would wrongly clip everything beyond them — leave the scissor and let the mask do the exact cut.
		if (operation != ClipOperation.Difference)
		{
			ClipRect(geometry.Bounds, operation);
		}
		Geo.Bounds(geometry, M3, out var min, out var max);
		_clip.Paths = ClipData.PushPath(_clip.Paths, new PathClip
		{
			Geometry = Track(geometry),
			M = M3,
			EvenOdd = geometry.FillRule == GeometryFillRule.EvenOdd,
			Exclude = operation == ClipOperation.Difference,
			Bbox = new Vector4(min.X, min.Y, max.X, max.Y),
		});
		_clip.ScissorInert = false;
	}
	public void Clear(WColor color) => _data.ClearColor = color;

	private Vector2 Map(float x, float y) => new(x * _m.M11 + y * _m.M21 + _m.M41, x * _m.M12 + y * _m.M22 + _m.M42);

	// Applies an active effect colour matrix (SaveLayer(IColorFilter)) to a straight-alpha solid colour, matching
	// the image shader's 4x5 row-major matrix+offset. DrawImage folds the matrix in the shader; solid rect/path
	// fills fold it here so a colour-filter layer transforms ALL its content, not only images.
	private static WColor ApplyColorMatrix(WColor c, float[] m)
	{
		static float Cl(float v) => v < 0f ? 0f : v > 1f ? 1f : v;
		float r = c.R / 255f, g = c.G / 255f, b = c.B / 255f, a = c.A / 255f;
		float nr = Cl(m[0] * r + m[1] * g + m[2] * b + m[3] * a + m[4]);
		float ng = Cl(m[5] * r + m[6] * g + m[7] * b + m[8] * a + m[9]);
		float nb = Cl(m[10] * r + m[11] * g + m[12] * b + m[13] * a + m[14]);
		float na = Cl(m[15] * r + m[16] * g + m[17] * b + m[18] * a + m[19]);
		return WColor.FromArgb((byte)(na * 255f + 0.5f), (byte)(nr * 255f + 0.5f), (byte)(ng * 255f + 0.5f), (byte)(nb * 255f + 0.5f));
	}

	public void DrawRect(in Rect rect, WColor color)
	{
		var p0 = Map((float)rect.Left, (float)rect.Top);
		var p1 = Map((float)rect.Right, (float)rect.Top);
		var p2 = Map((float)rect.Right, (float)rect.Bottom);
		var p3 = Map((float)rect.Left, (float)rect.Bottom);
		_target.Add(new RectCommand
		{
			Color = _pendingColorMatrix is { Length: >= 20 } pm ? ApplyColorMatrix(color, pm) : color,
			Clip = RelaxedClip(p0, p1, p2, p3),
			P0 = p0,
			P1 = p1,
			P2 = p2,
			P3 = p3,
		});
	}

	// Containment relaxation: when the op's device bounds are provably unaffected by the current clip's
	// rect/rounded components, shed them from the op's clip (see ClipData.ScissorInert) so the emit-time
	// scissor dedups and coalescing can merge across visuals. Fan clips are exact-coverage — never relaxed.
	private ClipData RelaxedClip(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3)
		=> RelaxedClip(
			Vector2.Min(Vector2.Min(p0, p1), Vector2.Min(p2, p3)),
			Vector2.Max(Vector2.Max(p0, p1), Vector2.Max(p2, p3)));

	private ClipData RelaxedClip(Vector2 bbMin, Vector2 bbMax)
	{
		var clip = _clip;
		if (clip.ScissorInert || clip.Paths is not null
			|| bbMin.X < clip.Aabb.X || bbMin.Y < clip.Aabb.Y || bbMax.X > clip.Aabb.Z || bbMax.Y > clip.Aabb.W)
		{
			return clip;
		}
		if (clip.Entries is { Length: > 0 } entries)
		{
			ClipEntry[] kept = null;
			int keptCount = 0;
			for (int i = 0; i < entries.Length; i++)
			{
				var e = entries[i];
				// The op's box in the entry's space: its corners mapped, then boxed again -- a superset of the op.
				var c0 = Vector2.Transform(bbMin, e.M); var c1 = Vector2.Transform(new Vector2(bbMax.X, bbMin.Y), e.M);
				var c2 = Vector2.Transform(bbMax, e.M); var c3 = Vector2.Transform(new Vector2(bbMin.X, bbMax.Y), e.M);
				var lo = Vector2.Min(Vector2.Min(c0, c1), Vector2.Min(c2, c3));
				var hi = Vector2.Max(Vector2.Max(c0, c1), Vector2.Max(c2, c3));
				bool inert;
				if (e.Exclude)
				{
					// An exclude entry can't cut an op that doesn't overlap its rect.
					inert = hi.X <= e.Rect.X || hi.Y <= e.Rect.Y || lo.X >= e.Rect.Z || lo.Y >= e.Rect.W;
				}
				else
				{
					// An intersect entry is coverage-1 inside its rect inset by the largest radii.
					float rx = MathF.Max(MathF.Max(e.Radii.X, e.Radii.Y), MathF.Max(e.Radii.Z, e.Radii.W));
					float ry = MathF.Max(MathF.Max(e.RadiiY.X, e.RadiiY.Y), MathF.Max(e.RadiiY.Z, e.RadiiY.W));
					inert = lo.X >= e.Rect.X + rx && lo.Y >= e.Rect.Y + ry && hi.X <= e.Rect.Z - rx && hi.Y <= e.Rect.W - ry;
				}
				if (!inert)
				{
					kept ??= new ClipEntry[entries.Length];
					kept[keptCount++] = e;
				}
			}
			if (keptCount == 0) { clip.Entries = null; }
			else if (keptCount < entries.Length) { System.Array.Resize(ref kept, keptCount); clip.Entries = kept; }
		}
		clip.ScissorInert = true;
		return clip;
	}


	public void DrawRoundedRect(in Rect rect, Vector4 radii, WColor color)
	{
		if (_pendingColorMatrix is { Length: >= 20 } pm) { color = ApplyColorMatrix(color, pm); }
		float w = (float)rect.Width, h = (float)rect.Height;
		float maxR = MathF.Min(w, h) * 0.5f;
		var p0 = Map((float)rect.Left, (float)rect.Top);
		var p1 = Map((float)rect.Right, (float)rect.Top);
		var p2 = Map((float)rect.Right, (float)rect.Bottom);
		var p3 = Map((float)rect.Left, (float)rect.Bottom);
		_target.Add(new RoundedRectCmd
		{
			P0 = p0,
			P1 = p1,
			P2 = p2,
			P3 = p3,
			Half = new Vector2(w * 0.5f, h * 0.5f),
			Radii = new Vector4(Math.Clamp(radii.X, 0, maxR), Math.Clamp(radii.Y, 0, maxR), Math.Clamp(radii.Z, 0, maxR), Math.Clamp(radii.W, 0, maxR)),
			Color = color,
			Clip = RelaxedClip(p0, p1, p2, p3),
		});
	}

	public void DrawRoundedRectBorder(in Rect outer, Vector4 outerRadii, in Rect inner, Vector4 innerRadii, WColor color)
	{
		if (_pendingColorMatrix is { Length: >= 20 } pm) { color = ApplyColorMatrix(color, pm); }
		float ow = (float)outer.Width, oh = (float)outer.Height, iw = (float)inner.Width, ih = (float)inner.Height;
		var oHalf = new Vector2(ow * 0.5f, oh * 0.5f); var iHalf = new Vector2(iw * 0.5f, ih * 0.5f);
		float oMax = MathF.Min(ow, oh) * 0.5f, iMax = MathF.Min(iw, ih) * 0.5f;
		// Inner centre relative to the outer centre, in LOCAL space (the SDF's `p` is centred on the outer rect).
		var innerCenter = new Vector2((float)(inner.Left + iw * 0.5f - (outer.Left + ow * 0.5f)), (float)(inner.Top + ih * 0.5f - (outer.Top + oh * 0.5f)));
		var bp0 = Map((float)outer.Left, (float)outer.Top);
		var bp1 = Map((float)outer.Right, (float)outer.Top);
		var bp2 = Map((float)outer.Right, (float)outer.Bottom);
		var bp3 = Map((float)outer.Left, (float)outer.Bottom);
		_target.Add(new RoundedRectCmd
		{
			P0 = bp0,
			P1 = bp1,
			P2 = bp2,
			P3 = bp3,
			Half = oHalf,
			Radii = new Vector4(Math.Clamp(outerRadii.X, 0, oMax), Math.Clamp(outerRadii.Y, 0, oMax), Math.Clamp(outerRadii.Z, 0, oMax), Math.Clamp(outerRadii.W, 0, oMax)),
			Color = color,
			Clip = RelaxedClip(bp0, bp1, bp2, bp3),
			InnerHalf = iHalf,
			InnerCenter = innerCenter,
			InnerRadii = new Vector4(Math.Clamp(innerRadii.X, 0, iMax), Math.Clamp(innerRadii.Y, 0, iMax), Math.Clamp(innerRadii.Z, 0, iMax), Math.Clamp(innerRadii.W, 0, iMax)),
		});
	}


	public void DrawPath(IGeometry geometry, WColor color) => AddPath(geometry, color, 0f, StrokeJoin.Miter);

	// The path as recorded: its geometry and the current matrix. Flattening and tessellation wait for draw time,
	// when the density it is drawn at (DPI, replay scale) is known.
	private void AddPath(IGeometry geometry, WColor color, float stroke, StrokeJoin join)
	{
		if (_pendingColorMatrix is { Length: >= 20 } pm) { color = ApplyColorMatrix(color, pm); }
		Geo.Bounds(geometry, M3, out var min, out var max);
		if (stroke > 0f) { var half = new Vector2(stroke * 0.5f * MathF.Max(MathF.Abs(_m.M11), MathF.Abs(_m.M12)), stroke * 0.5f * MathF.Max(MathF.Abs(_m.M21), MathF.Abs(_m.M22))); min -= half; max += half; }
		if (max.X <= min.X || max.Y <= min.Y) { return; }
		_target.Add(new PathCmd
		{
			Geometry = Track(geometry),
			M = M3,
			Stroke = stroke,
			Join = join,
			Color = color,
			EvenOdd = stroke == 0f && geometry.FillRule == GeometryFillRule.EvenOdd,
			BbMin = min,
			BbMax = max,
			Clip = RelaxedClip(min, max),
		});
	}

	// The current matrix as the pixel affine the commands store.
	private Matrix3x2 M3 => new(_m.M11, _m.M12, _m.M21, _m.M22, _m.M41, _m.M42);

	// Keeps a recorded geometry alive for the recording's lifetime, like a texture: the caller may dispose it right
	// after drawing (a stroke's outline is), and the shape is only rasterised at draw time.
	private IGeometry Track(IGeometry g) { g.AddRef(); (_data.Geometries ??= new()).Add(g); return g; }

	public void DrawRect(in Rect rect, IShader shader)
	{
		if (shader is not WebGpuShader g)
		{
			return;
		}

		// Compose the gradient's local matrix with the current matrix (F = local->device). The center and focal
		// origin are baked to device space (so a replay transform can re-map them as points); for the radial case
		// we ALSO pack M = diag(1/rx,1/ry) * F^-1 — the linear map from a device delta to unit-ellipse space — so
		// the eval is exact under rotation/skew (not just per-axis scale). Linear stays exact in device space.
		var lm = new Matrix4x4(
			g.LocalMatrix.M11, g.LocalMatrix.M12, 0, 0,
			g.LocalMatrix.M21, g.LocalMatrix.M22, 0, 0,
			0, 0, 1, 0,
			g.LocalMatrix.M31, g.LocalMatrix.M32, 0, 1);
		var m = lm * _m;
		Vector2 MapM(Vector2 p) => new(p.X * m.M11 + p.Y * m.M21 + m.M41, p.X * m.M12 + p.Y * m.M22 + m.M42);
		var a = MapM(g.P0);
		var b = MapM(g.P1);

		var count = Math.Min(g.Colors?.Length ?? 0, WebGpuDevice.MaxGradientStops);
		if (count == 0)
		{
			return;
		}

		var u = new float[WebGpuDevice.GradientUniformBytes / 4];
		u[3] = -1f;   // header.w: no ramp row yet; the draw assigns one (see WebGpuDevice.RampRow)
		u[0] = g.Radial ? 1f : 0f;
		u[1] = count;
		u[2] = g.TileMode switch { GradientTileMode.Repeat => 1f, GradientTileMode.Mirror => 2f, _ => 0f };
		if (g.Radial)
		{
			// F = [[M11,M21],[M12,M22]] (local->device linear part). M = diag(1/rx,1/ry) * F^-1, row-major
			// [[m00,m01],[m10,m11]]; packed column-major into geo.zw (col0) + origin.zw (col1) for the WGSL mat2x2.
			float det = m.M11 * m.M22 - m.M21 * m.M12;
			if (MathF.Abs(det) < 1e-12f) { det = det < 0 ? -1e-12f : 1e-12f; }
			float rx = g.RadiusX <= 0 ? 1e-6f : g.RadiusX, ry = g.RadiusY <= 0 ? 1e-6f : g.RadiusY;
			float m00 = (m.M22 / det) / rx, m01 = (-m.M21 / det) / rx;
			float m10 = (-m.M12 / det) / ry, m11 = (m.M11 / det) / ry;
			u[4] = a.X; u[5] = a.Y; u[6] = m00; u[7] = m10;   // geo: center + M col0
			u[WebGpuDevice.GradOriginBase] = b.X; u[WebGpuDevice.GradOriginBase + 1] = b.Y;
			u[WebGpuDevice.GradOriginBase + 2] = m01; u[WebGpuDevice.GradOriginBase + 3] = m11;   // origin: focal + M col1
		}
		else
		{
			// geo.zw = the direction over its squared length, so t = dot(p - a, geo.zw).
			var ab = b - a; var len2 = ab.LengthSquared();
			u[4] = a.X; u[5] = a.Y;
			if (len2 > 0f) { u[6] = ab.X / len2; u[7] = ab.Y / len2; }
		}

		for (var i = 0; i < count; i++)
		{
			var c = g.Colors[i];
			u[WebGpuDevice.GradColorsBase + i * 4] = c.R / 255f;
			u[WebGpuDevice.GradColorsBase + i * 4 + 1] = c.G / 255f;
			u[WebGpuDevice.GradColorsBase + i * 4 + 2] = c.B / 255f;
			u[WebGpuDevice.GradColorsBase + i * 4 + 3] = c.A / 255f;
			u[WebGpuDevice.GradStopsBase + i] = g.Stops is { Length: > 0 } && i < g.Stops.Length ? g.Stops[i] : (count > 1 ? i / (float)(count - 1) : 0f);
		}
		if (count <= 4)
		{
			for (var i = 0; i < count - 1; i++)
			{
				float s0 = u[WebGpuDevice.GradStopsBase + i], s1 = u[WebGpuDevice.GradStopsBase + i + 1];
				int c0 = WebGpuDevice.GradColorsBase + i * 4, r = WebGpuDevice.GradRampBase + i * 8;
				for (var ch = 0; ch < 4; ch++)
				{
					float scale = s1 > s0 ? (u[c0 + 4 + ch] - u[c0 + ch]) / (s1 - s0) : 0f;
					u[r + ch] = scale;
					u[r + 4 + ch] = u[c0 + ch] - s0 * scale;
				}
			}
		}

		var gp0 = Map((float)rect.Left, (float)rect.Top);
		var gp1 = Map((float)rect.Right, (float)rect.Top);
		var gp2 = Map((float)rect.Right, (float)rect.Bottom);
		var gp3 = Map((float)rect.Left, (float)rect.Bottom);
		_target.Add(new GradientCmd
		{
			Clip = RelaxedClip(gp0, gp1, gp2, gp3),
			Uniform = u,
			P0 = gp0,
			P1 = gp1,
			P2 = gp2,
			P3 = gp3,
		});
	}
	public void DrawShadow(IGeometry silhouette, WColor color, float sigmaX, float sigmaY, bool additive)
	{
		Geo.Bounds(silhouette, M3, out var min, out var max);
		if (max.X <= min.X || max.Y <= min.Y) { return; }
		_target.Add(new ShadowCmd
		{
			Geometry = Track(silhouette),
			M = M3,
			BbMin = min,
			BbMax = max,
			EvenOdd = silhouette.FillRule == GeometryFillRule.EvenOdd,
			Color = color,
			SigmaX = sigmaX,
			SigmaY = sigmaY,
			Additive = additive,
			Clip = _clip,
		});
	}
	public void StrokePath(IGeometry geometry, WColor color, float strokeWidth, StrokeJoin join = StrokeJoin.Miter)
	{
		if (strokeWidth > 0f) { AddPath(geometry, color, strokeWidth, join); }
	}
	public void DrawLine(Vector2 p0, Vector2 p1, WColor color, float strokeWidth)
	{
		var dir = p1 - p0; var len = dir.Length(); if (len < 1e-4f) { return; }
		dir /= len;
		var n = new Vector2(-dir.Y, dir.X) * (strokeWidth / 2f);
		var lp0 = Map(p0.X + n.X, p0.Y + n.Y);
		var lp1 = Map(p1.X + n.X, p1.Y + n.Y);
		var lp2 = Map(p1.X - n.X, p1.Y - n.Y);
		var lp3 = Map(p0.X - n.X, p0.Y - n.Y);
		_target.Add(new RectCommand
		{
			Color = color,
			Clip = RelaxedClip(lp0, lp1, lp2, lp3),
			P0 = lp0,
			P1 = lp1,
			P2 = lp2,
			P3 = lp3,
		});
	}
	// Keep a texture recorded into this frame alive for the frame's lifetime (it may be a one-shot texture the
	// caller disposes right after recording — e.g. CompositionNineGridBrush; the draw is replayed later at present).
	// Refcounted: this recording holds a ref until it is disposed (see WebGpuRenderRecord.Dispose / WebGpuTexture).
	private void TrackTexture(WebGpuTexture t) { t.AddRef(); (_data.Textures ??= new()).Add(t); }

	public void DrawImage(ITexture texture, float x, float y, float opacity = 1f)
	{
		if (texture is not WebGpuTexture t) { return; }
		int w = t.PixelWidth, h = t.PixelHeight; if (w <= 0 || h <= 0) { return; }
		TrackTexture(t);
		var cmd = ImageQuad(t, x, y, w, h, opacity);
		cmd.ColorMatrix = _pendingColorMatrix;
		_target.Add(cmd);
	}

	// The texture's quad over x, y, qw, qh in the recording's space, and the clip relaxed to it. The texture is
	// already resident; the command only carries its view.
	private ImageCmd ImageQuad(WebGpuTexture t, float x, float y, float qw, float qh, float opacity)
	{
		var p0 = Map(x, y); var p1 = Map(x + qw, y); var p2 = Map(x + qw, y + qh); var p3 = Map(x, y + qh);
		return new ImageCmd { P0 = p0, P1 = p1, P2 = p2, P3 = p3, View = t.View, W = t.PixelWidth, H = t.PixelHeight, Opacity = opacity, Clip = RelaxedClip(p0, p1, p2, p3) };
	}
	public void DrawImageTiled(ITexture texture, in Rect destination, EdgeExtend extendX, EdgeExtend extendY, float opacity = 1f)
	{
		if (texture is not WebGpuTexture t) { return; }
		int w = t.PixelWidth, h = t.PixelHeight; if (w <= 0 || h <= 0) { return; }
		TrackTexture(t);
		// The sampler's address mode does the extending, so one quad over the whole destination with UVs running
		// past 1 is the entire fill. None doesn't extend, so that axis is cut back to the texture's own size
		// rather than sampled past it.
		float x = (float)destination.Left, y = (float)destination.Top;
		var dw = extendX == EdgeExtend.None ? MathF.Min((float)destination.Width, w) : (float)destination.Width;
		var dh = extendY == EdgeExtend.None ? MathF.Min((float)destination.Height, h) : (float)destination.Height;
		if (dw <= 0 || dh <= 0) { return; }
		var cmd = ImageQuad(t, x, y, dw, dh, opacity);
		cmd.U1 = dw / w; cmd.V1 = dh / h; cmd.ExtendX = extendX; cmd.ExtendY = extendY;
		_target.Add(cmd);
	}

	public void DrawImage(ITexture texture, float x, float y, IColorFilter colorFilter)
	{
		if (texture is not WebGpuTexture t) { return; }
		int w = t.PixelWidth, h = t.PixelHeight; if (w <= 0 || h <= 0) { return; }
		TrackTexture(t);
		// A 4x5 colour-matrix filter (e.g. MonochromeColor / effect brush): apply it in the image shader.
		// The SrcIn blend-mode tint stays the fast path.
		if (colorFilter is WebGpuColorFilter { Matrix: { } matrix })
		{
			var mc = ImageQuad(t, x, y, w, h, 1f);
			mc.ColorMatrix = matrix;
			_target.Add(mc);
			return;
		}
		var (mode, tint) = ResolveTint(colorFilter);

		// Unreachable by design: the only IColorFilter routed to DrawImage is the SrcIn blend-mode tint
		// (CompositionSurfaceBrush.MonochromeColor); colour matrices are handled above. mode == 0 with a filter
		// present means an unsupported filter reached here and is being silently dropped — render output will be
		// wrong. If this fires, a new caller has broken that invariant; fix the caller or implement the case.
		if (mode == 0 && colorFilter is not null && this.Log().IsEnabled(LogLevel.Error))
		{
			this.Log().Error($"WebGPU DrawImage reached with an unsupported IColorFilter ('{colorFilter.GetType().Name}'); only a SrcIn blend-mode tint or a colour matrix is honored. The filter is being ignored — this path is not expected to be taken.");
		}

		var tc = ImageQuad(t, x, y, w, h, 1f);
		tc.TintMode = mode; tc.Tint = tint; tc.ColorMatrix = _pendingColorMatrix;
		_target.Add(tc);
	}

	// A tint WebGpuColorFilter → a straight-alpha tint; a colour matrix or a foreign filter → untinted.
	private static (int mode, Vector4 tint) ResolveTint(IColorFilter colorFilter)
		=> colorFilter is WebGpuColorFilter { IsTint: true } f
			? (1, new Vector4(f.Color.R / 255f, f.Color.G / 255f, f.Color.B / 255f, f.Color.A / 255f))
			: (0, default);

	public void DrawImageNineSlice(ITexture texture, in Rect centerSlice, in Rect destination, bool centerHollow)
	{
		if (texture is not WebGpuTexture t) { return; }
		int w = t.PixelWidth, h = t.PixelHeight; if (w <= 0 || h <= 0) { return; }
		TrackTexture(t);

		// Source (pixel) column/row edges from the center slice, and the matching destination edges: the corner
		// insets keep their source pixel size, the middle band stretches to fill the rest of the destination.
		float sx0 = 0, sx1 = (float)centerSlice.Left, sx2 = (float)centerSlice.Right, sx3 = w;
		float sy0 = 0, sy1 = (float)centerSlice.Top, sy2 = (float)centerSlice.Bottom, sy3 = h;
		float dx0 = (float)destination.Left, dx1 = dx0 + sx1, dx3 = (float)destination.Right, dx2 = dx3 - (sx3 - sx2);
		float dy0 = (float)destination.Top, dy1 = dy0 + sy1, dy3 = (float)destination.Bottom, dy2 = dy3 - (sy3 - sy2);
		float[] sxe = { sx0, sx1, sx2, sx3 }, sye = { sy0, sy1, sy2, sy3 };
		float[] dxe = { dx0, dx1, dx2, dx3 }, dye = { dy0, dy1, dy2, dy3 };

		for (var row = 0; row < 3; row++)
		{
			for (var col = 0; col < 3; col++)
			{
				if (centerHollow && row == 1 && col == 1) { continue; }
				float dl = dxe[col], dr = dxe[col + 1], dt = dye[row], db = dye[row + 1];
				if (dr - dl <= 0 || db - dt <= 0) { continue; }
				var np0 = Map(dl, dt);
				var np1 = Map(dr, dt);
				var np2 = Map(dr, db);
				var np3 = Map(dl, db);
				_target.Add(new ImageCmd
				{
					View = t.View,
					W = w,
					H = h,
					Opacity = 1f,
					Clip = RelaxedClip(np0, np1, np2, np3),
					P0 = np0,
					P1 = np1,
					P2 = np2,
					P3 = np3,
					U0 = sxe[col] / w,
					V0 = sye[row] / h,
					U1 = sxe[col + 1] / w,
					V1 = sye[row + 1] / h,
				});
			}
		}
	}

	public void DrawEffectBackdrop(IEffectFilter filter, float opacity)
	{
		if (filter is not WebGpuEffectFilter fx) { return; }
		// General non-backdrop evaluator result: the whole tree was rendered to a texture — just draw it at the
		// effect bounds (no backdrop capture).
		if (fx.EvaluatedTexture is { } evaluated)
		{
			DrawImage(evaluated, (float)fx.EvaluatedBounds.Left, (float)fx.EvaluatedBounds.Top, opacity);
			return;
		}
		// Opaque acrylic OR a zero-blur acrylic: a fully-opaque tint completely covers the blurred backdrop, and a
		// zero sigma makes the blur a no-op — either way skip the backdrop capture, full-window surface and gaussian
		// blur entirely and just fill the effect region with the tint (the clip masks its rounded corners). Matches
		// WinUI's opaque acrylic fallback and the reference's `isOpaque || blurSigma <= 0` short-circuit.
		if (fx.Color.A == 255 || (fx.SigmaX <= 0f && fx.SigmaY <= 0f))
		{
			var a = _clip.Aabb;
			_target.Add(new RectCommand
			{
				Color = fx.Color,
				Clip = RelaxedClip(new Vector2(a.X, a.Y), new Vector2(a.Z, a.W)),
				P0 = new Vector2(a.X, a.Y),
				P1 = new Vector2(a.Z, a.Y),
				P2 = new Vector2(a.Z, a.W),
				P3 = new Vector2(a.X, a.W),
			});
			return;
		}
		_target.Add(new BackdropCmd { Effect = fx, Opacity = opacity, Clip = _clip });
	}

	public IRenderRecord Finish() => _data;

	// A retained sub-recording (the SKPicture equivalent) is recorded at identity; replaying it here adds a node
	// carrying the current matrix and clip. The recording's commands are never copied or transformed: the present
	// walk composes the matrices, and caches the recording's GPU geometry keyed on its immutable command list.
	public void Replay(IRenderRecord data)
	{
		if (data is not WebGpuRenderRecord rec) { return; }
		// The nested list (with its raw image view handles) is captured by reference and may be drawn after the
		// nested recording is disposed, so this recording holds its textures and geometries alive too.
		TrackNestedTextures(rec);
		_target.Add(new ReplayRefCmd { Data = rec, Commands = rec.Commands, Transform = _m, Clip = _clip });
	}

	// Take a ref to every texture and geometry the nested recording references, so an outer frame keeps them alive as
	// long as it can be replayed. Balanced by this recording's Dispose (which Releases every entry in its lists).
	private void TrackNestedTextures(WebGpuRenderRecord source)
	{
		if (source.Textures is { } src)
		{
			var dst = _data.Textures ??= new();
			foreach (var t in src) { t.AddRef(); dst.Add(t); }
		}
		if (source.Geometries is { } geos)
		{
			var dst = _data.Geometries ??= new();
			foreach (var g in geos) { g.AddRef(); dst.Add(g); }
		}
	}
}
