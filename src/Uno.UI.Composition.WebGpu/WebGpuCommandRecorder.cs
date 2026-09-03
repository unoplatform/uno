// Records drawing calls into a WebGpuRenderRecord, and decides what can be replayed from the GPU geometry cache.
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

public sealed unsafe class WebGpuCommandRecorder : ICommandRecorder, IFlattenedPathSink
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
										   // session-native textures within the paint scope. Null only for the internal transform-scratch recorder
										   // (TransformFor), whose Factory is never read.
	private readonly IDrawingFactory _factory;

	public WebGpuCommandRecorder(IDrawingFactory factory = null) { _target = _data.Commands; _factory = factory; }

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
	public IDrawingFactory Factory => _factory
		?? throw new InvalidOperationException("This WebGPU recorder was created without a drawing factory (internal transform recorder).");
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
	public void SaveLayer(bool antialias = false) => PushLayer(0, null);
	public void SaveLayer(IColorFilter colorFilter, bool antialias = false)
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
	public void SaveLayerMask(bool antialias = false) => PushLayer(1, null);   // 1 = DstIn composite
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

	public void ClipRect(in Rect rect, ClipOperation operation = ClipOperation.Intersect, bool antialias = false)
	{
		// Tighten the scissor AABB; any active rounded shape is preserved (Intersect only).
		var a = DeviceAabb(rect);
		_clip.Aabb = new Vector4(MathF.Max(_clip.Aabb.X, a.X), MathF.Max(_clip.Aabb.Y, a.Y), MathF.Min(_clip.Aabb.Z, a.Z), MathF.Min(_clip.Aabb.W, a.W));
		_clip.ScissorInert = false;
	}

	public void ClipRoundRect(in RoundRectangle roundRect, ClipOperation operation = ClipOperation.Intersect, bool antialias = false)
	{
		var aabb = DeviceAabb(roundRect.Rect);
		// Device-space, axis-aligned rounded rect (exact under scale/translate). Per-corner radii carry BOTH axes
		// (elliptical corners), each axis scaled by the matrix's corresponding axis length; a full rotation would need
		// clip-local eval (falls back to the AABB below).
		var sx = new Vector2(_m.M11, _m.M12).Length();
		var sy = new Vector2(_m.M21, _m.M22).Length();
		var exclude = operation == ClipOperation.Difference;
		var rc = new RoundClip
		{
			Rect = aabb,
			Radii = new Vector4(roundRect.TopLeft.X * sx, roundRect.TopRight.X * sx, roundRect.BottomRight.X * sx, roundRect.BottomLeft.X * sx),
			RadiiY = new Vector4(roundRect.TopLeft.Y * sy, roundRect.TopRight.Y * sy, roundRect.BottomRight.Y * sy, roundRect.BottomLeft.Y * sy),
			Exclude = exclude,
		};
		// Nested rounded clips stack (all ANDed in clipCov) instead of the innermost overwriting the outer.
		_clip.Rounds = ClipData.Push(_clip.Rounds, rc);
		// Difference (PushClipExclude): keep the area OUTSIDE the rounded rect — so DON'T tighten the scissor to it
		// (the visible region extends past the rect); the per-fragment clipCov inverts the coverage.
		if (!exclude)
		{
			_clip.Aabb = new Vector4(MathF.Max(_clip.Aabb.X, aabb.X), MathF.Max(_clip.Aabb.Y, aabb.Y), MathF.Min(_clip.Aabb.Z, aabb.Z), MathF.Min(_clip.Aabb.W, aabb.W));
			_clip.ScissorInert = false;
		}
	}

	public void ClipPath(IGeometry geometry, ClipOperation operation = ClipOperation.Intersect, bool antialias = false)
	{
		// A geometry that advertises itself as a single (rounded) rect clips analytically (shader-evaluated
		// rounds / plain scissor) instead of costing a stencil-mask fan draw and defeating coalescing.
		// Only under an axis-aligned matrix: the rounds are device-space axis-aligned, while the fan is
		// exact under any transform.
		if (_m.M12 == 0 && _m.M21 == 0 && geometry.TryGetRoundRect() is { } rr)
		{
			if (operation == ClipOperation.Intersect
				&& rr.TopLeft == Vector2.Zero && rr.TopRight == Vector2.Zero && rr.BottomRight == Vector2.Zero && rr.BottomLeft == Vector2.Zero)
			{
				ClipRect(rr.Rect, operation, antialias);
			}
			else
			{
				ClipRoundRect(rr, operation, antialias);
			}
			return;
		}

		// Capture the flattened device-space fan for an exact per-fragment coverage mask (built at present time).
		// Tighten the scissor to the path bounds ONLY for Intersect (the path lies within its bounds). For
		// Difference the visible region is OUTSIDE the path and extends past its bounds, so tightening to the
		// bounds would wrongly clip everything beyond them — leave the scissor and let PathExclude do the exact cut.
		if (operation != ClipOperation.Difference)
		{
			ClipRect(geometry.Bounds, operation, antialias);
		}
		_fan = new List<float>();
		_bbMin = new Vector2(float.MaxValue); _bbMax = new Vector2(float.MinValue);
		geometry.StreamFlattened(this);
		if (_fan.Count > 0)
		{
			_clip.PathFan = _fan.ToArray();
			_clip.PathEvenOdd = geometry.FillRule == GeometryFillRule.EvenOdd;
			_clip.PathExclude = operation == ClipOperation.Difference;
		}
		_clip.ScissorInert = false;
		_fan = null;
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

	public void DrawRect(in Rect rect, WColor color, bool antialias = false)
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
		if (clip.ScissorInert || clip.PathFan is not null
			|| bbMin.X < clip.Aabb.X || bbMin.Y < clip.Aabb.Y || bbMax.X > clip.Aabb.Z || bbMax.Y > clip.Aabb.W)
		{
			return clip;
		}
		if (clip.Rounds is { Length: > 0 } rounds)
		{
			RoundClip[] kept = null;
			int keptCount = 0;
			for (int i = 0; i < rounds.Length; i++)
			{
				var rc = rounds[i];
				bool inert;
				if (rc.Exclude)
				{
					// An exclude-round can't cut an op that doesn't overlap its rect.
					inert = bbMax.X <= rc.Rect.X || bbMax.Y <= rc.Rect.Y || bbMin.X >= rc.Rect.Z || bbMin.Y >= rc.Rect.W;
				}
				else
				{
					// An intersect-round is coverage-1 inside its rect inset by the largest radii.
					float rx = MathF.Max(MathF.Max(rc.Radii.X, rc.Radii.Y), MathF.Max(rc.Radii.Z, rc.Radii.W));
					float ry = MathF.Max(MathF.Max(rc.RadiiY.X, rc.RadiiY.Y), MathF.Max(rc.RadiiY.Z, rc.RadiiY.W));
					inert = bbMin.X >= rc.Rect.X + rx && bbMin.Y >= rc.Rect.Y + ry && bbMax.X <= rc.Rect.Z - rx && bbMax.Y <= rc.Rect.W - ry;
				}
				if (!inert)
				{
					kept ??= new RoundClip[rounds.Length];
					kept[keptCount++] = rc;
				}
			}
			if (keptCount == 0) { clip.Rounds = null; }
			else if (keptCount < rounds.Length) { System.Array.Resize(ref kept, keptCount); clip.Rounds = kept; }
		}
		clip.ScissorInert = true;
		return clip;
	}

	private List<float> _fan;
	private Vector2 _pivot, _prev, _bbMin, _bbMax;
	private bool _firstInContour;
	// Does the triangle fan tile the shape without overlap? True iff every triangle winds the same way, which is
	// exactly sum(|area|) == |sum(area)|. Accumulated incrementally so the test is free.
	private int _contourCount;
	private double _fanAreaAbs, _fanAreaSigned;
	// Contour points, buffered so the fan can pivot on the CENTROID. Fanning from the first vertex self-overlaps
	// for any shape that is star-shaped about its middle rather than about that vertex (a blob, a star, a gauge
	// arc), which is most of them — pivoting on the centroid is what lets FanTiles actually fire.
	private readonly List<Vector2> _contourPts = new();
	private bool _fanFromCentroid;
	// Every contour of the current fill, kept so the path can be re-tessellated into a NON-overlapping
	// triangulation with an analytic AA ring (PathTessellator) instead of going through stencil-then-cover.
	private readonly List<List<Vector2>> _allContours = new();
	private readonly List<float> _aaVerts = new(), _aaCov = new();
	private readonly List<float> _hardVerts = new(), _hardCov = new();
	private float[] _fanHard;
	// Per-vertex AA coverage for the fill being recorded (null = no ring, edges rely on the attachment).
	private float[] _fanCoverage;
	private bool _contoursTruncated;
	// Stroke tessellation: contours collected in LOCAL space (offsetting must happen before the transform so a
	// non-uniform scale strokes correctly, same as DrawLine).
	private List<(List<Vector2> Pts, bool Closed)> _localContours;
	private bool _collectLocal;

	public void DrawRoundedRect(in Rect rect, Vector4 radii, WColor color, bool antialias = false)
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

	public void DrawRoundedRectBorder(in Rect outer, Vector4 outerRadii, in Rect inner, Vector4 innerRadii, WColor color, bool antialias = false)
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

	public void DrawPath(IGeometry geometry, WColor color, bool antialias = false)
		=> FillGeometry(geometry, color, geometry.FillRule == GeometryFillRule.EvenOdd);

	private void FillGeometry(IGeometry geometry, WColor color, bool evenOdd)
	{
		if (_pendingColorMatrix is { Length: >= 20 } pm) { color = ApplyColorMatrix(color, pm); }

		_fan = new List<float>();
		_bbMin = new Vector2(float.MaxValue); _bbMax = new Vector2(float.MinValue);
		_contourCount = 0; _fanAreaAbs = 0; _fanAreaSigned = 0;
		_allContours.Clear(); _fanCoverage = null; _contoursTruncated = false;
		// Even-odd fills stencil by parity, and parity depends on the fan decomposition, so only the non-zero
		// path may move its pivot.
		_fanFromCentroid = !evenOdd;
		_contourPts.Clear();
		geometry.StreamFlattened(this);
		_fanFromCentroid = false;
		if (_fan.Count > 0)
		{
			// A single contour whose fan tiles without overlap fills correctly in ONE pass, even when translucent:
			// adjacent fan triangles share an edge exactly, so no sample is covered twice.
			var tiles = !evenOdd && _contourCount == 1 && _fanAreaAbs > 0
				&& Math.Abs(_fanAreaAbs - Math.Abs(_fanAreaSigned)) <= 1e-4 * _fanAreaAbs;
			// Tessellate into non-overlapping triangles plus an analytic AA ring, so the fill runs in ONE pass
			// over the ink alone and antialiases itself instead of leaning on the multisampled attachment.
			var aa = TryTessellate(geometry);
			if (aa) { tiles = true; }
			_target.Add(new PathFill { FanDevice = _fan.ToArray(), FanCoverage = _fanCoverage, FanHard = _fanHard, Geometry = geometry, GeomMatrix = _m, BbMin = _bbMin, BbMax = _bbMax, Color = color, EvenOdd = evenOdd, FanTiles = tiles, Clip = RelaxedClip(_bbMin, _bbMax) });
		}
		_fan = null;
	}

	void IFlattenedPathSink.BeginContour(Vector2 start)
	{
		if (_collectLocal) { _localContours.Add((new List<Vector2> { start }, false)); return; }
		_pivot = Map(start.X, start.Y); _prev = _pivot; _firstInContour = true; _contourCount++; Include(_pivot);
		if (_fanFromCentroid) { _contourPts.Clear(); _contourPts.Add(_pivot); }
	}
	void IFlattenedPathSink.LineTo(Vector2 point)
	{
		if (_collectLocal)
		{
			if (_localContours.Count > 0) { _localContours[^1].Pts.Add(point); }
			return;
		}
		var p = Map(point.X, point.Y); Include(p);
		if (_fanFromCentroid) { _contourPts.Add(p); _prev = p; return; }
		if (_firstInContour) { _firstInContour = false; }
		else
		{
			_fan.Add(_pivot.X); _fan.Add(_pivot.Y); _fan.Add(_prev.X); _fan.Add(_prev.Y); _fan.Add(p.X); _fan.Add(p.Y);
			double a = ((double)_prev.X - _pivot.X) * ((double)p.Y - _pivot.Y) - ((double)p.X - _pivot.X) * ((double)_prev.Y - _pivot.Y);
			_fanAreaAbs += Math.Abs(a);
			_fanAreaSigned += a;
		}
		_prev = p;
	}
	void IFlattenedPathSink.EndContour(bool closed)
	{
		if (_collectLocal)
		{
			if (_localContours.Count > 0) { _localContours[^1] = (_localContours[^1].Pts, closed); }
			return;
		}
		if (!_fanFromCentroid) { return; }
		var n = _contourPts.Count;
		if (n < 3) { _contourPts.Clear(); return; }
		// One contour per glyph for a text run, so this bound has to clear a whole string, not a single shape.
		// Truncating silently would hand the tessellator a partial path.
		if (_allContours.Count < 512) { _allContours.Add(new List<Vector2>(_contourPts)); }
		else { _contoursTruncated = true; }
		var c = Vector2.Zero;
		for (int i = 0; i < n; i++) { c += _contourPts[i]; }
		c /= n;
		// Every edge gets a triangle, the closing one included — with a centroid pivot it is no longer degenerate.
		for (int i = 0; i < n; i++)
		{
			var a0 = _contourPts[i];
			var b0 = _contourPts[(i + 1) % n];
			_fan.Add(c.X); _fan.Add(c.Y); _fan.Add(a0.X); _fan.Add(a0.Y); _fan.Add(b0.X); _fan.Add(b0.Y);
			double ar = ((double)a0.X - c.X) * ((double)b0.Y - c.Y) - ((double)b0.X - c.X) * ((double)a0.Y - c.Y);
			_fanAreaAbs += Math.Abs(ar);
			_fanAreaSigned += ar;
		}
		_contourPts.Clear();
	}
	private void Include(Vector2 p) { _bbMin = Vector2.Min(_bbMin, p); _bbMax = Vector2.Max(_bbMax, p); }

	// Triangulation topology, cached per geometry. Ear clipping is O(n^2) and these recordings re-record every
	// frame, so tessellating per frame is a large LOSS. What makes it pay is that the
	// triangle INDICES are affine-invariant: the same geometry re-flattened under a different transform yields
	// the same indices, so the cache survives the per-frame transform changes that defeat device-space caches.
	// The entry records the point count it was built for and is rejected unless it matches exactly, because
	// flattening density is resolution-dependent and stale indices would tessellate the wrong shape.
	private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<object, int[]> _triCache = new();

	/// <summary>
	/// Emit the analytic AA ring? Set from the device's sample count: the ring REPLACES multisampling, so running
	/// both would antialias every edge twice and spread ink half a pixel too far.
	/// </summary>
	public static bool AnalyticAa;


	/// <summary>
	/// Replaces the fan with a non-overlapping triangulation plus a one-pixel analytic AA ring, so the fill can
	/// take the single-pass path and antialias itself. Leaves the fan untouched (returning false) whenever the
	/// result cannot be trusted.
	/// </summary>
	private bool TryTessellate(IGeometry geometry)
	{
		_fanHard = null;
		if (_contoursTruncated || _allContours.Count != _contourCount) { return false; }
		PathTessellator.Simplify(_allContours);
		var total = 0;
		for (var i = 0; i < _allContours.Count; i++) { total += _allContours[i].Count; }
		if (_allContours.Count == 0 || total < 3 || total > PathTessellator.MaxPoints) { return false; }

		if (!_triCache.TryGetValue(geometry, out var tris) || tris is null || tris.Length < 4 || tris[0] != total)
		{
			var built = PathTessellator.TryTriangulate(_allContours);
			if (built is null) { return false; }
			tris = new int[built.Length + 1];
			tris[0] = total;
			Array.Copy(built, 0, tris, 1, built.Length);
			_triCache.Remove(geometry);
			_triCache.Add(geometry, tris);
		}

		var idx = new int[tris.Length - 1];
		Array.Copy(tris, 1, idx, 0, idx.Length);

		// The triangulation must cover the same area the winding rule fills; if it does not, the two rules
		// disagree on this path (self-intersection, same-wound overlap) and the fan is the safe answer. Both
		// quantities are twice the true area, so they compare directly.
		double triArea = 0;
		for (var t = 0; t + 2 < idx.Length; t += 3)
		{
			var a = ContourPoint(idx[t]); var b = ContourPoint(idx[t + 1]); var c = ContourPoint(idx[t + 2]);
			triArea += Math.Abs((double)(b.X - a.X) * (c.Y - a.Y) - (double)(c.X - a.X) * (b.Y - a.Y));
		}
		double windArea = 0;
		for (var i = 0; i < _allContours.Count; i++) { windArea += PathTessellator.SignedArea2(_allContours[i]); }
		if (Math.Abs(triArea - Math.Abs(windArea)) > 1e-2 * Math.Max(triArea, 1)) { return false; }

		// Half the ramp, in device pixels (the points are already device-space). Zero when the attachment is
		// multisampled: the triangulation still pays for itself by filling only the ink, and MSAA keeps the edges.
		if (!PathTessellator.BuildGeometry(_allContours, idx, AnalyticAa ? 0.5f : 0f, _aaVerts, _aaCov)) { return false; }

		_fan.Clear();
		_fan.AddRange(_aaVerts);
		_fanCoverage = _aaCov.ToArray();

		// Ring-free twin for the atlas. Recordings are cached, so this runs once per record, not per frame.
		if (AnalyticAa && PathTessellator.BuildGeometry(_allContours, idx, 0f, _hardVerts, _hardCov))
		{
			_fanHard = _hardVerts.ToArray();
		}
		return true;
	}

	private Vector2 ContourPoint(int global)
	{
		for (var c = 0; c < _allContours.Count; c++)
		{
			if (global < _allContours[c].Count) { return _allContours[c][global]; }
			global -= _allContours[c].Count;
		}
		return default;
	}

	public void DrawRect(in Rect rect, IShader shader, bool antialias = false)
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
			u[4] = a.X; u[5] = a.Y; u[6] = b.X; u[7] = b.Y;
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
	public void DrawShadow(IGeometry silhouette, WColor color, float sigmaX, float sigmaY, bool additive, bool antialias = false)
	{
		_fan = new List<float>();
		_bbMin = new Vector2(float.MaxValue); _bbMax = new Vector2(float.MinValue);
		silhouette.StreamFlattened(this);
		if (_fan.Count > 0)
		{
			_target.Add(new ShadowCmd
			{
				FanDevice = _fan.ToArray(),
				BbMin = _bbMin,
				BbMax = _bbMax,
				EvenOdd = silhouette.FillRule == GeometryFillRule.EvenOdd,
				Color = color,
				SigmaX = sigmaX,
				SigmaY = sigmaY,
				Additive = additive,
				Clip = _clip,
			});
		}
		_fan = null;
	}
	public void StrokePath(IGeometry geometry, WColor color, float strokeWidth, bool antialias = false)
	{
		if (strokeWidth > 0 && TryStrokeAsStrip(geometry, color, strokeWidth)) { return; }
		using var sg = geometry.GetStrokeFillGeometry(new StrokeStyle { Thickness = strokeWidth, LineJoin = StrokeJoin.Miter, MiterLimit = 10f });
		FillGeometry(sg, color, evenOdd: false);
	}

	/// <summary>
	/// Strokes by tessellating the polyline into a miter-joined triangle strip, which TILES — so it fills in one
	/// pass (see PathFill.FanTiles) instead of stencilling the stroke OUTLINE and covering its whole bbox. That
	/// outline route costs bbox-scale rasterisation twice over: the stencil fan spans the shape and so does the
	/// cover quad, which is why a 2px outline around a 600x500 blob dominated coverMpx.
	/// Consecutive quads share their join edge exactly, so a translucent stroke does not double-blend — except
	/// where the polyline crosses ITSELF, which this does not detect.
	/// </summary>
	private bool TryStrokeAsStrip(IGeometry geometry, WColor color, float strokeWidth)
	{
		if (_pendingColorMatrix is { Length: >= 20 } pm) { color = ApplyColorMatrix(color, pm); }
		_localContours ??= new();
		_localContours.Clear();
		_collectLocal = true;
		try { geometry.StreamFlattened(this); }
		finally { _collectLocal = false; }
		if (_localContours.Count == 0) { return false; }

		var h = strokeWidth * 0.5f;
		_fan = new List<float>();
		_bbMin = new Vector2(float.MaxValue); _bbMax = new Vector2(float.MinValue);
		foreach (var (pts, closed) in _localContours)
		{
			EmitStrokeStrip(pts, closed, h);
		}
		var ok = _fan.Count > 0;
		if (ok) { WgStrokeStats.Strips++; }
		if (ok)
		{
			_target.Add(new PathFill { FanDevice = _fan.ToArray(), BbMin = _bbMin, BbMax = _bbMax, Color = color, EvenOdd = false, FanTiles = true, Clip = RelaxedClip(_bbMin, _bbMax) });
		}
		_fan = null;
		return ok;
	}

	private void EmitStrokeStrip(List<Vector2> pts, bool closed, float h)
	{
		// Drop repeated points: a zero-length segment has no direction to offset along.
		for (int i = pts.Count - 1; i > 0; i--)
		{
			if ((pts[i] - pts[i - 1]).LengthSquared() < 1e-12f) { pts.RemoveAt(i); }
		}
		if (closed && pts.Count > 1 && (pts[^1] - pts[0]).LengthSquared() < 1e-12f) { pts.RemoveAt(pts.Count - 1); }
		var n = pts.Count;
		if (n < 2) { return; }

		// Per-vertex offset: the miter, so the quads on either side share this edge exactly and the strip tiles.
		var off = new Vector2[n];
		for (int i = 0; i < n; i++)
		{
			var hasPrev = i > 0 || closed;
			var hasNext = i < n - 1 || closed;
			var prev = pts[(i - 1 + n) % n];
			var next = pts[(i + 1) % n];
			var n1 = hasPrev ? Norm(pts[i] - prev) : Vector2.Zero;
			var n2 = hasNext ? Norm(next - pts[i]) : Vector2.Zero;
			if (!hasPrev) { off[i] = Perp(n2) * h; continue; }
			if (!hasNext) { off[i] = Perp(n1) * h; continue; }
			var m = Perp(n1) + Perp(n2);
			var ml = m.Length();
			if (ml < 1e-5f) { off[i] = Perp(n1) * h; continue; }   // 180 degree reversal: no finite miter
			m /= ml;
			// miterLength = h / cos(theta/2); clamped so a near-degenerate corner cannot shoot off to infinity.
			var cos = Vector2.Dot(m, Perp(n1));
			var scale = MathF.Abs(cos) < 0.1f ? h * 10f : h / cos;
			off[i] = m * MathF.Min(MathF.Abs(scale), h * 10f) * MathF.Sign(scale == 0 ? 1 : scale);
		}

		var segs = closed ? n : n - 1;
		for (int i = 0; i < segs; i++)
		{
			var j = (i + 1) % n;
			var a0 = Map(pts[i].X + off[i].X, pts[i].Y + off[i].Y);
			var a1 = Map(pts[i].X - off[i].X, pts[i].Y - off[i].Y);
			var b0 = Map(pts[j].X + off[j].X, pts[j].Y + off[j].Y);
			var b1 = Map(pts[j].X - off[j].X, pts[j].Y - off[j].Y);
			Include(a0); Include(a1); Include(b0); Include(b1);
			_fan.Add(a0.X); _fan.Add(a0.Y); _fan.Add(b0.X); _fan.Add(b0.Y); _fan.Add(b1.X); _fan.Add(b1.Y);
			_fan.Add(a0.X); _fan.Add(a0.Y); _fan.Add(b1.X); _fan.Add(b1.Y); _fan.Add(a1.X); _fan.Add(a1.Y);
		}

		static Vector2 Norm(Vector2 v) { var l = v.Length(); return l < 1e-6f ? Vector2.Zero : v / l; }
		static Vector2 Perp(Vector2 v) => new(-v.Y, v.X);
	}
	public void DrawLine(Vector2 p0, Vector2 p1, WColor color, float strokeWidth, bool antialias = false)
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

	public void DrawImage(ITexture texture, float x, float y, float opacity = 1f, bool antialias = false)
	{
		if (texture is not WebGpuTexture t) { return; }
		int w = t.PixelWidth, h = t.PixelHeight; if (w <= 0 || h <= 0) { return; }
		TrackTexture(t);
		// No per-frame upload — the texture is already resident; record its view for the present pass.
		{ var ip0 = Map(x, y); var ip1 = Map(x + w, y); var ip2 = Map(x + w, y + h); var ip3 = Map(x, y + h); _target.Add(new ImageCmd { P0 = ip0, P1 = ip1, P2 = ip2, P3 = ip3, View = t.View, W = w, H = h, Opacity = opacity, ColorMatrix = _pendingColorMatrix, Clip = RelaxedClip(ip0, ip1, ip2, ip3) }); }
	}
	public void DrawImage(ITexture texture, float x, float y, IColorFilter colorFilter, bool antialias = false)
	{
		if (texture is not WebGpuTexture t) { return; }
		int w = t.PixelWidth, h = t.PixelHeight; if (w <= 0 || h <= 0) { return; }
		TrackTexture(t);
		// A 4x5 colour-matrix filter (e.g. MonochromeColor / effect brush): apply it in the image shader.
		// The SrcIn blend-mode tint stays the fast path.
		if (colorFilter is WebGpuColorFilter { Matrix: { } matrix })
		{
			{ var ip0 = Map(x, y); var ip1 = Map(x + w, y); var ip2 = Map(x + w, y + h); var ip3 = Map(x, y + h); _target.Add(new ImageCmd { P0 = ip0, P1 = ip1, P2 = ip2, P3 = ip3, View = t.View, W = w, H = h, Opacity = 1f, ColorMatrix = matrix, Clip = RelaxedClip(ip0, ip1, ip2, ip3) }); }
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

		{ var ip0 = Map(x, y); var ip1 = Map(x + w, y); var ip2 = Map(x + w, y + h); var ip3 = Map(x, y + h); _target.Add(new ImageCmd { P0 = ip0, P1 = ip1, P2 = ip2, P3 = ip3, View = t.View, W = w, H = h, Opacity = 1f, TintMode = mode, Tint = tint, ColorMatrix = _pendingColorMatrix, Clip = RelaxedClip(ip0, ip1, ip2, ip3) }); }
	}

	// A tint WebGpuColorFilter → a straight-alpha tint; a colour matrix or a foreign filter → untinted.
	private static (int mode, Vector4 tint) ResolveTint(IColorFilter colorFilter)
		=> colorFilter is WebGpuColorFilter { IsTint: true } f
			? (1, new Vector4(f.Color.R / 255f, f.Color.G / 255f, f.Color.B / 255f, f.Color.A / 255f))
			: (0, default);

	public void DrawImageNineSlice(ITexture texture, in Rect centerSlice, in Rect destination, bool centerHollow, bool antialias = false)
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

	internal static int StatBlockRef, StatBlockLayer, StatBlockShadow, StatBlockOther, StatBlockEmpty;

	/// <summary>
	/// Whether a recording can be GPU-geometry-cached: only simple primitives (rect/rrect/path/image/gradient).
	/// Path clips qualify too, because their fan is residentized (see ResidentizeFan) rather than re-tessellated
	/// every frame, leaving only the bbox-scissored depth-mask draw to repeat. Memoized on the record.
	/// </summary>
	internal static bool IsCacheable(WebGpuRenderRecord d)
	{
		if (d.Cacheable is { } memo) { return memo; }
		bool ok = d.Commands.Count > 0;
		if (!ok) { StatBlockEmpty++; }
		foreach (var c in d.Commands)
		{
			if (c is not (RectCommand or RoundedRectCmd or PathFill or ImageCmd or GradientCmd))
			{
				ok = false;
				switch (c)
				{
					case ReplayRefCmd: StatBlockRef++; break;
					case LayerCmd: StatBlockLayer++; break;
					case ShadowCmd: StatBlockShadow++; break;
					default: StatBlockOther++; break;
				}
				break;
			}
		}
		d.Cacheable = ok;
		return ok;
	}


	// Transforms a recording's (simple) commands to device space under a transform+clip, for building its GPU
	// cache. Uses the inline (always-transform) path so it never emits a nested ReplayRef.
	internal static List<WebGpuCommand> TransformFor(List<WebGpuCommand> commands, Matrix4x4 transform, ClipData clip)
	{
		var rec = new WebGpuCommandRecorder();
		rec._m = transform;
		rec._clip = clip;
		rec.ReplayInline(new WebGpuRenderRecord { Commands = commands });
		return rec._data.Commands;
	}

	// Retained sub-recordings (SKPicture equivalent) are recorded at identity; replaying one bakes in the target
	// session's current matrix + clip. A cacheable recording is deferred as a ReplayRef capturing its immutable
	// command list (the present caches its GPU geometry); otherwise its commands are transformed inline.
	public void Replay(IRenderRecord data)
	{
		if (data is WebGpuRenderRecord cacheable && IsCacheable(cacheable))
		{
			// The nested recording's command list (with the raw image view handles) is captured by reference and may
			// be compiled at present AFTER the nested recording is disposed — so this recording must also hold a ref to
			// its textures to keep the views alive for the whole time this recording can be replayed.
			TrackNestedTextures(cacheable);
			_target.Add(new ReplayRefCmd { Data = cacheable, Commands = cacheable.Commands, Transform = _m, Clip = _clip });
			StatCacheableReplays++;
			return;
		}
		if (data is WebGpuRenderRecord inl)
		{
			StatInlineReplays++; StatInlineCmds += inl.Commands.Count;
		}
		ReplayInline(data);
	}

	// Per-frame record-phase counters: a recording is only cacheable when EVERY command is a simple primitive, so
	// one nested replay/layer/shadow anywhere forces the whole list to be re-transformed inline — reallocating a
	// fan array per path fill (i.e. per glyph) every frame. Reset and reported by the backend's stats line.
	internal static int StatCacheableReplays, StatInlineReplays, StatInlineCmds;

	// Take a ref to every texture the nested recording references, so an outer frame keeps them alive as long as it can
	// be replayed. Balanced by this recording's Dispose (which Releases every entry in its Textures list).
	private void TrackNestedTextures(WebGpuRenderRecord source)
	{
		if (source.Textures is not { } src) { return; }
		var dst = _data.Textures ??= new();
		foreach (var t in src) { t.AddRef(); dst.Add(t); }
	}

	private void ReplayInline(IRenderRecord data)
	{
		if (data is not WebGpuRenderRecord d) { return; }
		// Inlined (non-cacheable) recordings copy their ImageCmds (with the same view handle) into this target, so this
		// recording must keep those textures alive too. TransformFor wraps a bare command list (Textures == null), so
		// this is a no-op on the present-time transform path.
		TrackNestedTextures(d);
		Vector2 T(Vector2 p) => new(p.X * _m.M11 + p.Y * _m.M21 + _m.M41, p.X * _m.M12 + p.Y * _m.M22 + _m.M42);
		foreach (var cmd in d.Commands)
		{
			switch (cmd)
			{
				case RectCommand r:
					_target.Add(new RectCommand { Color = r.Color, Clip = ClipCompose(r.Clip), P0 = T(r.P0), P1 = T(r.P1), P2 = T(r.P2), P3 = T(r.P3) });
					break;
				case RoundedRectCmd rrc:
					// Local Half/Radii/Inner are intrinsic (transform-independent); only the device corners move.
					_target.Add(new RoundedRectCmd { P0 = T(rrc.P0), P1 = T(rrc.P1), P2 = T(rrc.P2), P3 = T(rrc.P3), Half = rrc.Half, Radii = rrc.Radii, Color = rrc.Color, Opacity = rrc.Opacity, InnerHalf = rrc.InnerHalf, InnerCenter = rrc.InnerCenter, InnerRadii = rrc.InnerRadii, Clip = ClipCompose(rrc.Clip) });
					break;
				case PathFill p:
					// A non-cacheable recording is replayed EVERY frame, and a path's fan is transformed point by
					// point into a fresh array each time — thousands of points per glyph. The result depends only
					// on (this command, this transform), so remember it: a static or merely re-replayed visual then
					// costs nothing here, and the reused instance keeps the caches hanging off it (its
					// slot-interleaved fan) alive too.
					if (p.ReplayedAt(_m) is { } cachedFill)
					{
						_target.Add(cachedFill);
						break;
					}

					var src = p.FanDevice; var dst = new float[src.Length];
					var bbMin = new Vector2(float.MaxValue); var bbMax = new Vector2(float.MinValue);
					for (int i = 0; i < src.Length; i += 2)
					{
						var q = T(new Vector2(src[i], src[i + 1])); dst[i] = q.X; dst[i + 1] = q.Y;
						bbMin = Vector2.Min(bbMin, q); bbMax = Vector2.Max(bbMax, q);
					}
					// The ring-free twin has to be transformed too, or the atlas bake falls back to the ringed fan and
					// antialiases the mask twice.
					float[] dstHard = null;
					if (p.FanHard is { } srcHard)
					{
						dstHard = new float[srcHard.Length];
						for (int i = 0; i < srcHard.Length; i += 2)
						{
							var qh = T(new Vector2(srcHard[i], srcHard[i + 1])); dstHard[i] = qh.X; dstHard[i + 1] = qh.Y;
						}
					}
					// FanTiles carries over: an affine map scales every triangle area by the same determinant, so
					// sum(|area|) == |sum(area)| still holds. Dropping it silently disabled the single-pass fill for
					// every replayed (scrolled or transformed) recording. GeomMatrix carries the atlas key the same
					// way — composed with this replay's transform, so a scaled instance keys to its own entry rather
					// than reusing a mask baked at a different scale.
					var replayed = new PathFill { FanDevice = dst, FanCoverage = p.FanCoverage, FanHard = dstHard, BbMin = bbMin, BbMax = bbMax, Color = p.Color, EvenOdd = p.EvenOdd, FanTiles = p.FanTiles, Geometry = p.Geometry, GeomMatrix = p.GeomMatrix * _m, Clip = ClipCompose(p.Clip) };
					p.StoreReplayed(_m, replayed);
					_target.Add(replayed);
					break;
				case ShadowCmd sh:
					var ssrc = sh.FanDevice; var sdst = new float[ssrc.Length];
					var sbbMin = new Vector2(float.MaxValue); var sbbMax = new Vector2(float.MinValue);
					for (int i = 0; i < ssrc.Length; i += 2)
					{
						var q = T(new Vector2(ssrc[i], ssrc[i + 1])); sdst[i] = q.X; sdst[i + 1] = q.Y;
						sbbMin = Vector2.Min(sbbMin, q); sbbMax = Vector2.Max(sbbMax, q);
					}
					var ss = new Vector2(_m.M11, _m.M12).Length();
					_target.Add(new ShadowCmd { FanDevice = sdst, BbMin = sbbMin, BbMax = sbbMax, EvenOdd = sh.EvenOdd, Color = sh.Color, SigmaX = sh.SigmaX * ss, SigmaY = sh.SigmaY * ss, Additive = sh.Additive, Clip = ClipCompose(sh.Clip) });
					break;
				case ImageCmd im:
					_target.Add(new ImageCmd { P0 = T(im.P0), P1 = T(im.P1), P2 = T(im.P2), P3 = T(im.P3), View = im.View, W = im.W, H = im.H, Opacity = im.Opacity, U0 = im.U0, V0 = im.V0, U1 = im.U1, V1 = im.V1, TintMode = im.TintMode, Tint = im.Tint, ColorMatrix = im.ColorMatrix, Clip = ClipCompose(im.Clip) });
					break;
				case GradientCmd gc:
					// Transform the device-space geometry baked into the uniform by the replay matrix too, so the
					// gradient stays aligned with its (transformed) quad.
					var uu = (float[])gc.Uniform.Clone();
					var ga = T(new Vector2(uu[4], uu[5])); uu[4] = ga.X; uu[5] = ga.Y;
					if (uu[0] < 0.5f)
					{
						var gb = T(new Vector2(uu[6], uu[7])); uu[6] = gb.X; uu[7] = gb.Y;
					}
					else
					{
						// Center + focal are points → transform by T. The unit-ellipse map M is relative to device
						// deltas, so under the extra device transform T2 it becomes M' = M * T2^-1 (deltas map back
						// through T2 before M). Center/focal stay in the (new) device space.
						int ob = WebGpuDevice.GradOriginBase;
						var go = T(new Vector2(uu[ob], uu[ob + 1])); uu[ob] = go.X; uu[ob + 1] = go.Y;
						float t11 = _m.M11, t12 = _m.M12, t21 = _m.M21, t22 = _m.M22;
						float dt = t11 * t22 - t21 * t12;
						if (MathF.Abs(dt) < 1e-12f) { dt = dt < 0 ? -1e-12f : 1e-12f; }
						// T2^-1 (row-major [[i00,i01],[i10,i11]]), where T2 = [[t11,t21],[t12,t22]] (MapM convention).
						float i00 = t22 / dt, i01 = -t21 / dt, i10 = -t12 / dt, i11 = t11 / dt;
						// M row-major from packed cols: m00=uu[6], m10=uu[7], m01=uu[ob+2], m11=uu[ob+3]. M' = M * T2^-1.
						float m00 = uu[6], m10 = uu[7], m01 = uu[ob + 2], m11 = uu[ob + 3];
						float n00 = m00 * i00 + m01 * i10, n01 = m00 * i01 + m01 * i11;
						float n10 = m10 * i00 + m11 * i10, n11 = m10 * i01 + m11 * i11;
						uu[6] = n00; uu[7] = n10; uu[ob + 2] = n01; uu[ob + 3] = n11;
					}
					_target.Add(new GradientCmd { P0 = T(gc.P0), P1 = T(gc.P1), P2 = T(gc.P2), P3 = T(gc.P3), Uniform = uu, Clip = ClipCompose(gc.Clip) });
					break;
				case LayerCmd lyr:
					var saved = _target;
					var layerList = new List<WebGpuCommand>();
					_target = layerList;
					Replay(new WebGpuRenderRecord { Commands = lyr.Commands });   // recursively transform sub-commands
					_target = saved;
					_target.Add(new LayerCmd { Commands = layerList, CompositeMode = lyr.CompositeMode, ColorMatrix = lyr.ColorMatrix, ShadowEffect = lyr.ShadowEffect, Clip = ClipCompose(lyr.Clip) });
					break;
				case BackdropCmd bk:
					_target.Add(new BackdropCmd { Effect = bk.Effect, Opacity = bk.Opacity, Clip = ClipCompose(bk.Clip) });
					break;
				case ReplayRefCmd rr:
					// Compose this replay's transform/clip onto the ref so the present still caches it.
					_target.Add(new ReplayRefCmd { Data = rr.Data, Commands = rr.Commands, Transform = rr.Transform * _m, Clip = ClipCompose(rr.Clip) });
					break;
			}
		}
	}

	// AABB of a child rect (its 4 corners) under the replay transform t.
	// Takes the matrix, not a Func: this runs for every command and every replay, and an indirect call per corner
	// (plus the closure the delegate conversion forces on the caller) is real cost under wasm.
	private static Vector4 TransformedAabb(Vector4 rect, in Matrix4x4 m)
	{
		static Vector2 Mp(float x, float y, in Matrix4x4 m) => new(x * m.M11 + y * m.M21 + m.M41, x * m.M12 + y * m.M22 + m.M42);
		var a = Mp(rect.X, rect.Y, m); var b = Mp(rect.Z, rect.Y, m); var e = Mp(rect.Z, rect.W, m); var f = Mp(rect.X, rect.W, m);
		var l = MathF.Min(MathF.Min(a.X, b.X), MathF.Min(e.X, f.X)); var top = MathF.Min(MathF.Min(a.Y, b.Y), MathF.Min(e.Y, f.Y));
		var r = MathF.Max(MathF.Max(a.X, b.X), MathF.Max(e.X, f.X)); var bo = MathF.Max(MathF.Max(a.Y, b.Y), MathF.Max(e.Y, f.Y));
		return new Vector4(l, top, r, bo);
	}

	// Intersect a child (sub-recording) clip into the current session clip, transforming it by the replay matrix.
	private ClipData ClipCompose(ClipData c)
	{
		var result = _clip;
		// The op's containment proof only covers its own recorded clip; the replay-site clip can still cut it,
		// so the composed op is scissor-inert only when both sides are.
		result.ScissorInert = c.ScissorInert && _clip.ScissorInert;
		if (!(c.Aabb.X <= -1e8f && c.Aabb.Y <= -1e8f && c.Aabb.Z >= 1e8f && c.Aabb.W >= 1e8f))
		{
			var a = TransformedAabb(c.Aabb, _m);
			result.Aabb = new Vector4(MathF.Max(result.Aabb.X, a.X), MathF.Max(result.Aabb.Y, a.Y), MathF.Min(result.Aabb.Z, a.Z), MathF.Min(result.Aabb.W, a.W));
		}
		// Child rounded clips AND with the parent's; transform each rect and scale radii by the replay matrix.
		if (c.Rounds is { Length: > 0 } rounds)
		{
			var sx = new Vector2(_m.M11, _m.M12).Length();
			var sy = new Vector2(_m.M21, _m.M22).Length();
			foreach (var src in rounds)
			{
				result.Rounds = ClipData.Push(result.Rounds, new RoundClip
				{
					Rect = TransformedAabb(src.Rect, _m),
					Radii = src.Radii * sx,
					RadiiY = src.RadiiY * sy,
					Exclude = src.Exclude,
				});
			}
		}
		if (c.PathFan != null)
		{
			var pf = new float[c.PathFan.Length];
			for (int i = 0; i < c.PathFan.Length; i += 2)
			{
				float x = c.PathFan[i], y = c.PathFan[i + 1];
				pf[i] = x * _m.M11 + y * _m.M21 + _m.M41;
				pf[i + 1] = x * _m.M12 + y * _m.M22 + _m.M42;
			}
			result.PathFan = pf;
			result.PathEvenOdd = c.PathEvenOdd;
			result.PathExclude = c.PathExclude;
		}
		return result;
	}
}
