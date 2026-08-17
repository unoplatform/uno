// Minimal-but-real WebGPU backend implementing the NEUTRAL drawing seam (public SPI from Uno.UI.Composition).
// Solid rects + even-odd path fill (stencil-then-cover) consuming IGeometry.StreamFlattened (Skia-less).
#nullable disable
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Uno.WebGpu.Native;
using static Uno.WebGpu.Native.WGPU;
using Uno.UI.Composition.Drawing;
using Windows.Graphics.Effects.Interop;
using Windows.Foundation;
using WColor = Windows.UI.Color;

namespace Uno.UI.Composition.WebGpu;

internal struct RoundClip
{
	public Vector4 Rect;    // device rounded-rect L,T,R,B
	public Vector4 Radii;   // per-corner X radius (TL,TR,BR,BL), device px
	public Vector4 RadiiY;  // per-corner Y radius (elliptical corners; equals Radii for circular)
	public bool Exclude;    // Difference op: keep the area OUTSIDE the rounded rect (PushClipExclude) rather than inside
}

internal struct ClipData
{
	public const int MaxRounds = 4;   // nesting depth beyond this drops the outermost (least likely to clip content)
	public Vector4 Aabb;    // device L,T,R,B scissor
	// Nested rounded-rect clips, all ANDed per-fragment (clipCov). null/empty = none. Copy-on-write: each push
	// allocates a fresh array so Save/Restore snapshots and sibling commands keep their own reference.
	public RoundClip[] Rounds;
	// Arbitrary path clip: the flattened device-space fan is applied via the shared depth mask in the main pass.
	// Single slot — innermost path wins (nested arbitrary paths keep only the AABB intersection for the outer ones).
	public float[] PathFan;
	public bool PathEvenOdd;
	public bool PathExclude;   // Difference op for the path clip
	// RESIDENT clip-fan buffer: a CACHED recording's fan is stable, so its NDC vertex buffer is uploaded ONCE
	// (into owned) and reused every frame instead of re-tessellated + re-uploaded per frame in ApplyDepthClip.
	// 0 = not resident. FanW/FanH = surface size it was baked for (invalidated on resize).
	public nint FanBuf;
	public int FanW, FanH;
	public static ClipData None => new() { Aabb = new Vector4(-1e9f, -1e9f, 1e9f, 1e9f) };

	// No clip at all: infinite scissor, no rounded shapes, no path mask. (Arena re-stamp is only correct when the
	// fragment shader doesn't depend on device position — i.e. no clip; see the ReplayRefCmd arena path.)
	public bool IsNone => (Rounds is null || Rounds.Length == 0) && PathFan is null
		&& Aabb.X <= -1e8f && Aabb.Y <= -1e8f && Aabb.Z >= 1e8f && Aabb.W >= 1e8f;

	// Append a rounded clip, copy-on-write, capped at MaxRounds (drops the oldest/outermost on overflow).
	public static RoundClip[] Push(RoundClip[] existing, in RoundClip rc)
	{
		int n = existing?.Length ?? 0;
		if (n < MaxRounds)
		{
			var arr = new RoundClip[n + 1];
			if (n > 0) { System.Array.Copy(existing, arr, n); }
			arr[n] = rc;
			return arr;
		}
		var capped = new RoundClip[MaxRounds];
		System.Array.Copy(existing, 1, capped, 0, MaxRounds - 1);
		capped[MaxRounds - 1] = rc;
		return capped;
	}
}

// Draw commands share one ordered stream so cross-type z-order (rect over path over image) is preserved.
internal abstract class WebGpuCommand
{
	public ClipData Clip;
}

internal sealed class RectCommand : WebGpuCommand
{
	public WColor Color;
	public Vector2 P0, P1, P2, P3;
}

// An analytic rounded rectangle / border ring (ported from ramez): one SDF quad instead of a tessellated path.
// The SDF is evaluated in LOCAL centred space (Half/Radii are local, transform-independent), so it's correct under
// ANY affine transform (rotation/scale/skew) — the four device corners P0..P3 only position the quad. A positive
// InnerHalf makes it a BORDER RING (outer minus an inner rounded rect at InnerCenter); InnerHalf<0 = solid fill.
// Radii = (TopLeft, TopRight, BottomRight, BottomLeft).
internal sealed class RoundedRectCmd : WebGpuCommand
{
	public Vector2 P0, P1, P2, P3;   // device-space corners: TL, TR, BR, BL (matches RectCommand order)
	public Vector2 Half;             // local half-size
	public Vector4 Radii;            // local per-corner
	public WColor Color; public float Opacity = 1f;
	public Vector2 InnerHalf = new(-1f, -1f);
	public Vector2 InnerCenter;
	public Vector4 InnerRadii;
}

internal sealed class PathFill : WebGpuCommand
{
	public float[] FanDevice;
	public Vector2 BbMin, BbMax;
	public WColor Color;
	public bool EvenOdd;
}

internal sealed unsafe class ImageCmd : WebGpuCommand
{
	public Vector2 P0, P1, P2, P3;
	public IntPtr View;   // the pre-uploaded WebGpuTexture view (no per-frame upload)
	public int W, H;
	public float Opacity;
	public float U0, V0, U1 = 1f, V1 = 1f;   // source UV sub-rect (whole texture by default)
	public int TintMode;        // 0 = none, 1 = SrcIn blend-mode tint
	public Vector4 Tint;        // straight-alpha tint color (0..1) for TintMode 1
	public float[] ColorMatrix; // null, or 20-float (4x5) effect colour matrix applied in the image shader
}

internal sealed class GradientCmd : WebGpuCommand
{
	public Vector2 P0, P1, P2, P3;   // device-space quad
	public float[] Uniform;          // packed Grad struct (WebGpuDevice.GradientUniformBytes / 4 floats)
}

// A drop shadow: the silhouette (flattened, device space) is filled into an offscreen coverage texture,
// separably gaussian-blurred (SigmaX/Y), then composited tinted by Color. Same fan/bbox form as PathFill.
internal sealed class ShadowCmd : WebGpuCommand
{
	public float[] FanDevice;
	public Vector2 BbMin, BbMax;
	public bool EvenOdd;
	public WColor Color;
	public float SigmaX, SigmaY;
	public bool Additive;
}

// A SaveLayer group: its Commands are rendered into a full-size offscreen surface, then composited onto the
// parent with CompositeMode (0 = SrcOver, 1 = DstIn mask) and an optional color matrix (SaveLayer(IColorFilter)).
internal sealed class LayerCmd : WebGpuCommand
{
	public List<WebGpuCommand> Commands;
	public int CompositeMode;   // 0 = SrcOver, 1 = DstIn
	public float[] ColorMatrix; // null, or 20-float (4x5) color matrix applied at composite
	public WebGpuEffectFilter ShadowEffect; // SaveLayer(IEffectFilter): a drop shadow derived from the content
}

// DrawEffectBackdrop (acrylic): the content drawn BEFORE this in the frame is captured, gaussian-blurred by
// Effect's sigma, drawn clipped to the effect region, then tinted by Effect.Color. Effect-graph realization is
// simplified to blur + tint (the dominant acrylic visual), not the full IGraphicsEffect DAG.
internal sealed class BackdropCmd : WebGpuCommand
{
	public WebGpuEffectFilter Effect;
	public float Opacity;
}

// A deferred replay of a cacheable child recording under a transform+clip. Captures BOTH the recording
// (WebGpuRenderRecord, which owns its compiled GPU draw-list — the persistent retained state) and its immutable
// command-list reference. The list is captured directly so a build survives the recording's Dispose (which only
// nulls Commands + defers the compiled state's GPU free to the render thread); the frame presents on the render
// thread while the main thread may Dispose the recording.
internal sealed class ReplayRefCmd : WebGpuCommand
{
	public WebGpuRenderRecord Data;
	public System.Collections.Generic.List<WebGpuCommand> Commands;
	public System.Numerics.Matrix4x4 Transform;
}

// Persistent (non-pooled) GPU resources for a cached recording, released on eviction. Separate from the per-frame
// pool so cached draws survive across frames.
internal struct DrawOp
{
	public int kind; public nint b0; public uint u0; public nint b1; public bool flag; public ClipData clip; public nint clipBg;
	public uint Color; public int GlyphFanStart;
	public DrawOp(int kind, nint b0, uint u0, nint b1, bool flag, ClipData clip, nint clipBg)
	{
		this.kind = kind; this.b0 = b0; this.u0 = u0; this.b1 = b1; this.flag = flag; this.clip = clip; this.clipBg = clipBg;
		Color = 0; GlyphFanStart = -1;
	}
	public readonly void Deconstruct(out int kind, out nint b0, out uint u0, out nint b1, out bool flag, out ClipData clip, out nint clipBg)
	{
		kind = this.kind; b0 = this.b0; u0 = this.u0; b1 = this.b1; flag = this.flag; clip = this.clip; clipBg = this.clipBg;
	}
}

// A recording's cached GPU geometry, owned by the render-thread device and keyed by the immutable command list.
internal sealed unsafe class WebGpuGeometryCache
{
	public List<DrawOp> Ops;
	public OwnedResources Owned;
	public Matrix4x4 Transform;
	public ClipData Clip;
	// Back-reference to the owning device so the recording's Dispose (UI thread) can enqueue this for a render-thread
	// free. Set at build time (render thread).
	public WebGpuDevice Device;
	// Surface size (px) the geometry's NDC verts were baked for. Verts are CPU-NDC'd (pos/size), so a size change
	// (window resize) makes the cached NDC stale — rebuild when the current surface differs. Without this, cached
	// recordings replay old-size NDC into the resized surface and look stretched.
	public int BuiltW, BuiltH;
	// Stable transform-table slot for this recording's path-fill (kind 1) geometry: its fan/cover verts are stored in
	// recorded-device space and bake this slot as a per-vertex index; the slot's local->NDC affine is rewritten each
	// frame (folding the replay transform + current device->NDC projection), so resize/move never re-bakes the verts.
	// -1 until the recording first builds a path fill. Returned to the device free-list when this cache is released.
	public int XformSlot = -1;
	// Arena entry: Ops geometry is baked in the recording's OWN (identity) NDC space; a moved replay re-stamps a
	// transform uniform (xform) on the per-op clip bind groups and reuses the vertex buffers instead of rebuilding.
	public bool Arena;
	// All ops are path fills (kind 1) — their verts are device-space + the transform table, so the recording is fully
	// surface-size-independent: a resize repositions them via the per-frame table entry and needs NO rebuild (unlike
	// a mixed entry whose solid/rrect verts are NDC-baked). Lets the arena resize-staleness skip pure-path entries.
	public bool PurePath;
	// Frame-solid entry (recording contains solids): only its NON-solid ops (paths/images/gradients, device space)
	// are cached here; its solids are re-appended into the shared per-pass buffer each frame so they coalesce across
	// visuals; the ordered emit list (FrameOrder) interleaves them with cached non-solid ops in draw order.
	public bool FrameSolid;
	// Resident extracted geometry for a frame-solid recording: device-space verts (solid = 6 floats/vert, rrect =
	// 22) + an ordered emit list, built ONCE (rebuilt only on transform/clip change). Each frame the verts are
	// bulk-appended to the shared buffers and the ops re-emitted with the base offset — NO per-frame TransformFor,
	// re-tessellation, or allocation (that was ~60ms + 26MB/frame at 500 visuals).
	public long SlabId;       // stable id for this recording's slices in the shared solid/rrect slabs
	public List<FrameOp> FrameOrder;
	// TRANSFORM-TABLE frame-solid entry (gap 4 solid-scroll): solids/rrects are baked in the recording's OWN (identity)
	// device space + a per-vertex slot, resident in the SHARED TABLE slabs; a move rewrites the slot (WriteXform), not
	// the verts. FrameOrder byte offsets are RELATIVE to the recording's own vert list — the ABSOLUTE slab offset is
	// re-derived every frame (never cached across a cull->reclaim) so a reappearing recording can't alias a freed slice.
	public bool TableFrame;
	public List<float> TableSolids;   // resident local solid verts (7 floats/v) — re-Put only when the slice was culled
	public List<float> TableRrects;   // resident local rrect verts (23 floats/v)
	// Per-op (device scissor, clip bind group) for the current stamp, parallel to FrameOrder. Rebuilt only when the
	// replay transform / session clip / surface size changes (memoized like the arena stamp); the slab base is applied
	// on top each frame. StampW/StampH invalidate it on resize (the clip uniform + scissor are device-space).
	public List<(ClipData Scissor, nint ClipBg)> StampClips;
	public ClipData StampClip;
	public int StampW, StampH;
	// Arena stamp memo: the per-op clip bind groups + device scissors for a given replay transform depend only on
	// that transform, so cache the fully-stamped ops (built with StampOwned) and reuse them verbatim while the
	// transform is unchanged — a STATIC arena visual then costs one AddRange/frame, no per-op MakeClipBg.
	public List<DrawOp> StampedOps;
	public OwnedResources StampOwned;
	public Matrix4x4 StampXform;
	public bool HasStamp;
}

// A persistent, shared, per-kind vertex slab: ONE GPU buffer holding every visual's geometry of a kind (solid /
// rrect / …) at stable per-visual slices (via WebGpuVertexSlab). A static visual's slice is resident — drawn each
// frame with NO re-upload; a changed visual rewrites its slice in place and only those bytes upload (DIRTY); a new
// visual appends. This is what makes resident + coalescing + partial-upload work ACROSS recordings (not per-
// recording buffers). `Put`/`Offset` return BYTE offsets. Grow reallocs the buffer once and re-uploads the shadow.
internal struct FrameOp
{
	public int Kind;          // 0 = solid, 5 = rrect, -1 = non-solid
	public int ByteOff;       // byte offset of this run within its shared slab (solid/rrect)
	public uint Count;        // vertex count (solid/rrect)
	public ClipData Clip;
	public nint ClipBg;
	public DrawOp NonSolid;
}

// Backend-created gradient shader handle. The WebGPU backend mints its own (rather than delegating to Skia) so
// the recorder can read the gradient parameters back and evaluate them in the WGSL gradient pipeline.
public sealed class WebGpuShader : IShader
{
	public bool Radial;
	public Vector2 P0;          // start (linear) / center (radial), &gradient-local space
	public Vector2 P1;          // end (linear) / gradient origin (radial)
	public float RadiusX, RadiusY;
	public WColor[] Colors;
	public float[] Stops;
	public GradientTileMode TileMode;
	public Matrix3x2 LocalMatrix;
}

// Backend-owned color filter so the WebGPU renderer can read the tint params (an IColorFilter is opaque —
// consumed only by the paired renderer). Currently the SrcIn blend-mode tint (image fade/tint, the only
// DrawImage color-filter case) is honored; other modes / the color matrix carry through but the image path
// applies only SrcIn for now.
public sealed class WebGpuColorFilter : IColorFilter
{
	public bool IsBlendMode;
	public WColor Color;
	public BlendMode Mode;
	public float[] Matrix;
}

// Backend-owned effect filter. Today only the drop shadow (SaveLayer(IEffectFilter) from Visual/ShadowState):
// the layer content is blurred, tinted by Color and offset by (Dx,Dy), drawn behind the content.
public sealed class WebGpuEffectFilter : IEffectFilter
{
	public float Dx, Dy, SigmaX, SigmaY;
	public WColor Color;      // acrylic tint (composited SrcOver on top) / drop-shadow color
	public WColor LumColor;   // acrylic luminosity color (SrcOver over the blurred backdrop == mix(blurred, lum.rgb, lum.a))
	public float Noise;       // acrylic procedural-grain opacity (0 = none); baked into the backdrop composite
	public void Dispose() { }
}

public sealed class WebGpuRenderRecord : IRenderRecord
{
	internal List<WebGpuCommand> Commands = new();
	internal WColor? ClearColor;
	internal bool? Cacheable;   // memoized: all commands are simple primitives with no path clip
	// The compiled GPU draw-list for this recording (the persistent retained state IRenderRecord is contracted to hold):
	// built once on the render thread at first replay, reused every frame, freed (deferred to the render thread) when
	// this recording is disposed. Written by the render thread, taken by the UI thread's Dispose — via Interlocked.
	internal WebGpuGeometryCache Compiled;
	// Transient image textures recorded into this frame that the caller disposed while recording (e.g. the one-shot
	// texture CompositionNineGridBrush uploads). We keep them alive for every present of this recording, then release
	// their GPU resources here at Dispose — resident textures (surface-owned) are left untouched (DisposeRequested=false).
	internal List<WebGpuTexture> Textures;

	// Backend-bound: dispatches to the WebGpu session that must consume it (guaranteed same-backend by the single
	// registered backend). A recorder nests it (deferred ReplayRef / inline transform); a present session encodes
	// and submits it as the frame.
	public void Replay(IDrawingSession into)
	{
		switch (into)
		{
			case WebGpuCommandRecorder recorder:
				recorder.Replay(this);
				break;
			case WebGpuPresentSession present:
				present.Replay(this);
				break;
		}
	}

	// Dispose only nulls the field; the command LIST object stays alive while any in-flight frame's ReplayRef
	// still references it (captured by reference), and the device's geometry cache is keyed on that list.
	public void Dispose()
	{
		// Drop this recording's references to the textures it recorded/nested. The GPU view is freed only once the
		// composition has disposed the texture AND every recording that captured its handle has released it — an outer
		// frame's ReplayRef may still hold this command list (with the raw view handle) and be compiled after us.
		if (Textures is { } textures) { foreach (var t in textures) { t.Release(); } }
		// Hand the compiled draw-list's GPU resources to the render thread for a deferred free (an in-flight frame may
		// still reference them). Interlocked so a concurrent render-thread rebuild can't leak or double-free it.
		var c = System.Threading.Interlocked.Exchange(ref Compiled, null);
		if (c is { Device: { } dev }) { dev.DeferCompiledRelease(c.Owned, c.StampOwned, c.XformSlot); }
		Commands = null;
	}
}

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

	public WebGpuCommandRecorder() => _target = _data.Commands;

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
		PushLayer(0, null);
	}
	public void SaveLayer(BlendMode blendMode, bool antialias = false) => PushLayer(blendMode == BlendMode.DstIn ? 1 : 0, null);
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
		}
	}

	public void ClipPath(IGeometry geometry, ClipOperation operation = ClipOperation.Intersect, bool antialias = false)
	{
		// Tighten the scissor to the path bounds, and capture the flattened device-space fan for an exact
		// per-fragment coverage mask (built at present time).
		ClipRect(geometry.Bounds, operation, antialias);
		_fan = new List<float>();
		_bbMin = new Vector2(float.MaxValue); _bbMax = new Vector2(float.MinValue);
		geometry.StreamFlattened(this);
		if (_fan.Count > 0)
		{
			_clip.PathFan = _fan.ToArray();
			_clip.PathEvenOdd = geometry.FillRule == GeometryFillRule.EvenOdd;
			_clip.PathExclude = operation == ClipOperation.Difference;
		}
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
		=> _target.Add(new RectCommand
		{
			Color = _pendingColorMatrix is { Length: >= 20 } pm ? ApplyColorMatrix(color, pm) : color, Clip = _clip,
			P0 = Map((float)rect.Left, (float)rect.Top), P1 = Map((float)rect.Right, (float)rect.Top),
			P2 = Map((float)rect.Right, (float)rect.Bottom), P3 = Map((float)rect.Left, (float)rect.Bottom),
		});

	private List<float> _fan;
	private Vector2 _pivot, _prev, _bbMin, _bbMax;
	private bool _firstInContour;

	public void DrawRoundedRect(in Rect rect, Vector4 radii, WColor color, bool antialias = false)
	{
		if (_pendingColorMatrix is { Length: >= 20 } pm) { color = ApplyColorMatrix(color, pm); }
		float w = (float)rect.Width, h = (float)rect.Height;
		float maxR = MathF.Min(w, h) * 0.5f;
		_target.Add(new RoundedRectCmd
		{
			P0 = Map((float)rect.Left, (float)rect.Top), P1 = Map((float)rect.Right, (float)rect.Top),
			P2 = Map((float)rect.Right, (float)rect.Bottom), P3 = Map((float)rect.Left, (float)rect.Bottom),
			Half = new Vector2(w * 0.5f, h * 0.5f),
			Radii = new Vector4(Math.Clamp(radii.X, 0, maxR), Math.Clamp(radii.Y, 0, maxR), Math.Clamp(radii.Z, 0, maxR), Math.Clamp(radii.W, 0, maxR)),
			Color = color, Clip = _clip,
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
		_target.Add(new RoundedRectCmd
		{
			P0 = Map((float)outer.Left, (float)outer.Top), P1 = Map((float)outer.Right, (float)outer.Top),
			P2 = Map((float)outer.Right, (float)outer.Bottom), P3 = Map((float)outer.Left, (float)outer.Bottom),
			Half = oHalf,
			Radii = new Vector4(Math.Clamp(outerRadii.X, 0, oMax), Math.Clamp(outerRadii.Y, 0, oMax), Math.Clamp(outerRadii.Z, 0, oMax), Math.Clamp(outerRadii.W, 0, oMax)),
			Color = color, Clip = _clip,
			InnerHalf = iHalf, InnerCenter = innerCenter,
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
		geometry.StreamFlattened(this);
		if (_fan.Count > 0)
		{
			_target.Add(new PathFill { FanDevice = _fan.ToArray(), BbMin = _bbMin, BbMax = _bbMax, Color = color, EvenOdd = evenOdd, Clip = _clip });
		}
		_fan = null;
	}

	void IFlattenedPathSink.BeginContour(Vector2 start) { _pivot = Map(start.X, start.Y); _prev = _pivot; _firstInContour = true; Include(_pivot); }
	void IFlattenedPathSink.LineTo(Vector2 point)
	{
		var p = Map(point.X, point.Y); Include(p);
		if (_firstInContour) { _firstInContour = false; }
		else { _fan.Add(_pivot.X); _fan.Add(_pivot.Y); _fan.Add(_prev.X); _fan.Add(_prev.Y); _fan.Add(p.X); _fan.Add(p.Y); }
		_prev = p;
	}
	void IFlattenedPathSink.EndContour(bool closed) { }
	private void Include(Vector2 p) { _bbMin = Vector2.Min(_bbMin, p); _bbMax = Vector2.Max(_bbMax, p); }

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

		_target.Add(new GradientCmd
		{
			Clip = _clip, Uniform = u,
			P0 = Map((float)rect.Left, (float)rect.Top), P1 = Map((float)rect.Right, (float)rect.Top),
			P2 = Map((float)rect.Right, (float)rect.Bottom), P3 = Map((float)rect.Left, (float)rect.Bottom),
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
				FanDevice = _fan.ToArray(), BbMin = _bbMin, BbMax = _bbMax,
				EvenOdd = silhouette.FillRule == GeometryFillRule.EvenOdd,
				Color = color, SigmaX = sigmaX, SigmaY = sigmaY, Additive = additive, Clip = _clip,
			});
		}
		_fan = null;
	}
	public void StrokePath(IGeometry geometry, WColor color, float strokeWidth, bool antialias = false)
	{
		using var sg = geometry.GetStrokeFillGeometry(new StrokeStyle { Thickness = strokeWidth, LineJoin = StrokeJoin.Miter, MiterLimit = 10f });
		FillGeometry(sg, color, evenOdd: false);
	}
	public void DrawLine(Vector2 p0, Vector2 p1, WColor color, float strokeWidth, bool antialias = false)
	{
		var dir = p1 - p0; var len = dir.Length(); if (len < 1e-4f) { return; } dir /= len;
		var n = new Vector2(-dir.Y, dir.X) * (strokeWidth / 2f);
		_target.Add(new RectCommand
		{
			Color = color, Clip = _clip,
			P0 = Map(p0.X + n.X, p0.Y + n.Y), P1 = Map(p1.X + n.X, p1.Y + n.Y),
			P2 = Map(p1.X - n.X, p1.Y - n.Y), P3 = Map(p0.X - n.X, p0.Y - n.Y),
		});
	}
	// Keep a texture recorded into this frame alive for the frame's lifetime (it may be a one-shot texture the
	// caller disposes right after recording — e.g. CompositionNineGridBrush; the draw is replayed later at present).
	// Refcounted: this recording holds a ref until it is disposed (see WebGpuRenderRecord.Dispose / WebGpuTexture).
	private void TrackTexture(WebGpuTexture t) { t.AddRef(); (_data.Textures ??= new()).Add(t); }

	public void DrawImage(ITexture texture, float x, float y, ImageSampling sampling, float opacity = 1f, bool antialias = false)
	{
		if (texture is not WebGpuTexture t) { return; }
		int w = t.PixelWidth, h = t.PixelHeight; if (w <= 0 || h <= 0) { return; }
		TrackTexture(t);
		// No per-frame upload — the texture is already resident; record its view for the present pass.
		_target.Add(new ImageCmd { P0 = Map(x, y), P1 = Map(x + w, y), P2 = Map(x + w, y + h), P3 = Map(x, y + h), View = t.View, W = w, H = h, Opacity = opacity, ColorMatrix = _pendingColorMatrix, Clip = _clip });
	}
	public void DrawImage(ITexture texture, float x, float y, ImageSampling sampling, IColorFilter colorFilter, bool antialias = false)
	{
		if (texture is not WebGpuTexture t) { return; }
		int w = t.PixelWidth, h = t.PixelHeight; if (w <= 0 || h <= 0) { return; }
		TrackTexture(t);
		// A 4x5 colour-matrix filter (e.g. MonochromeColor / effect brush): apply it in the image shader.
		// The SrcIn blend-mode tint stays the fast path.
		if (colorFilter is WebGpuColorFilter { Matrix: { } matrix })
		{
			_target.Add(new ImageCmd { P0 = Map(x, y), P1 = Map(x + w, y), P2 = Map(x + w, y + h), P3 = Map(x, y + h), View = t.View, W = w, H = h, Opacity = 1f, ColorMatrix = matrix, Clip = _clip });
			return;
		}
		var (mode, tint) = ResolveTint(colorFilter);
		_target.Add(new ImageCmd { P0 = Map(x, y), P1 = Map(x + w, y), P2 = Map(x + w, y + h), P3 = Map(x, y + h), View = t.View, W = w, H = h, Opacity = 1f, TintMode = mode, Tint = tint, ColorMatrix = _pendingColorMatrix, Clip = _clip });
	}

	// A SrcIn blend-mode WebGpuColorFilter → a straight-alpha tint (the only image color-filter case today);
	// anything else (other modes, color matrix, or a foreign filter) → untinted.
	private static (int mode, Vector4 tint) ResolveTint(IColorFilter colorFilter)
		=> colorFilter is WebGpuColorFilter { IsBlendMode: true, Mode: BlendMode.SrcIn } f
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
				_target.Add(new ImageCmd
				{
					View = t.View, W = w, H = h, Opacity = 1f, Clip = _clip,
					P0 = Map(dl, dt), P1 = Map(dr, dt), P2 = Map(dr, db), P3 = Map(dl, db),
					U0 = sxe[col] / w, V0 = sye[row] / h, U1 = sxe[col + 1] / w, V1 = sye[row + 1] / h,
				});
			}
		}
	}

	public void DrawEffectBackdrop(IEffectFilter filter, float opacity)
	{
		if (filter is not WebGpuEffectFilter fx) { return; }
		// Opaque acrylic OR a zero-blur acrylic: a fully-opaque tint completely covers the blurred backdrop, and a
		// zero sigma makes the blur a no-op — either way skip the backdrop capture, full-window surface and gaussian
		// blur entirely and just fill the effect region with the tint (the clip masks its rounded corners). Matches
		// WinUI's opaque acrylic fallback and the reference's `isOpaque || blurSigma <= 0` short-circuit.
		if (fx.Color.A == 255 || (fx.SigmaX <= 0f && fx.SigmaY <= 0f))
		{
			var a = _clip.Aabb;
			_target.Add(new RectCommand
			{
				Color = fx.Color, Clip = _clip,
				P0 = new Vector2(a.X, a.Y), P1 = new Vector2(a.Z, a.Y), P2 = new Vector2(a.Z, a.W), P3 = new Vector2(a.X, a.W),
			});
			return;
		}
		_target.Add(new BackdropCmd { Effect = fx, Opacity = opacity, Clip = _clip });
	}

	public IRenderRecord Finish() => _data;

	// Whether a recording can be GPU-geometry-cached: only simple primitives (rect/rrect/path/image/gradient). PATH
	// (PathFan) clips ARE cacheable — their fan is residentized (ResidentizeFan) so it isn't re-tessellated per frame,
	// and only the (cheap, bbox-scissored) in-pass depth-mask draw repeats. This was a regression under the old
	// reference-equality ClipDataEquals (cached path-clip recordings always looked stale → rebuilt every frame); the
	// value-compare fix + resident fan made it a win (see RUNNING-CONTEXT §17/§21). Memoized.
	internal static bool IsCacheable(WebGpuRenderRecord d)
	{
		if (d.Cacheable is { } memo) { return memo; }
		bool ok = d.Commands.Count > 0;
		foreach (var c in d.Commands)
		{
			if (c is not (RectCommand or RoundedRectCmd or PathFill or ImageCmd or GradientCmd)) { ok = false; break; }
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
			return;
		}
		ReplayInline(data);
	}

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
					_target.Add(new RectCommand { Color = r.Color, Clip = ClipCompose(r.Clip, T), P0 = T(r.P0), P1 = T(r.P1), P2 = T(r.P2), P3 = T(r.P3) });
					break;
				case RoundedRectCmd rrc:
					// Local Half/Radii/Inner are intrinsic (transform-independent); only the device corners move.
					_target.Add(new RoundedRectCmd { P0 = T(rrc.P0), P1 = T(rrc.P1), P2 = T(rrc.P2), P3 = T(rrc.P3), Half = rrc.Half, Radii = rrc.Radii, Color = rrc.Color, Opacity = rrc.Opacity, InnerHalf = rrc.InnerHalf, InnerCenter = rrc.InnerCenter, InnerRadii = rrc.InnerRadii, Clip = ClipCompose(rrc.Clip, T) });
					break;
				case PathFill p:
					var src = p.FanDevice; var dst = new float[src.Length];
					var bbMin = new Vector2(float.MaxValue); var bbMax = new Vector2(float.MinValue);
					for (int i = 0; i < src.Length; i += 2)
					{
						var q = T(new Vector2(src[i], src[i + 1])); dst[i] = q.X; dst[i + 1] = q.Y;
						bbMin = Vector2.Min(bbMin, q); bbMax = Vector2.Max(bbMax, q);
					}
					_target.Add(new PathFill { FanDevice = dst, BbMin = bbMin, BbMax = bbMax, Color = p.Color, EvenOdd = p.EvenOdd, Clip = ClipCompose(p.Clip, T) });
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
					_target.Add(new ShadowCmd { FanDevice = sdst, BbMin = sbbMin, BbMax = sbbMax, EvenOdd = sh.EvenOdd, Color = sh.Color, SigmaX = sh.SigmaX * ss, SigmaY = sh.SigmaY * ss, Additive = sh.Additive, Clip = ClipCompose(sh.Clip, T) });
					break;
				case ImageCmd im:
					_target.Add(new ImageCmd { P0 = T(im.P0), P1 = T(im.P1), P2 = T(im.P2), P3 = T(im.P3), View = im.View, W = im.W, H = im.H, Opacity = im.Opacity, U0 = im.U0, V0 = im.V0, U1 = im.U1, V1 = im.V1, TintMode = im.TintMode, Tint = im.Tint, ColorMatrix = im.ColorMatrix, Clip = ClipCompose(im.Clip, T) });
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
					_target.Add(new GradientCmd { P0 = T(gc.P0), P1 = T(gc.P1), P2 = T(gc.P2), P3 = T(gc.P3), Uniform = uu, Clip = ClipCompose(gc.Clip, T) });
					break;
				case LayerCmd lyr:
					var saved = _target;
					var layerList = new List<WebGpuCommand>();
					_target = layerList;
					Replay(new WebGpuRenderRecord { Commands = lyr.Commands });   // recursively transform sub-commands
					_target = saved;
					_target.Add(new LayerCmd { Commands = layerList, CompositeMode = lyr.CompositeMode, ColorMatrix = lyr.ColorMatrix, ShadowEffect = lyr.ShadowEffect, Clip = ClipCompose(lyr.Clip, T) });
					break;
				case BackdropCmd bk:
					_target.Add(new BackdropCmd { Effect = bk.Effect, Opacity = bk.Opacity, Clip = ClipCompose(bk.Clip, T) });
					break;
				case ReplayRefCmd rr:
					// Compose this replay's transform/clip onto the ref so the present still caches it.
					_target.Add(new ReplayRefCmd { Data = rr.Data, Commands = rr.Commands, Transform = rr.Transform * _m, Clip = ClipCompose(rr.Clip, T) });
					break;
			}
		}
	}

	// AABB of a child rect (its 4 corners) under the replay transform t.
	private static Vector4 TransformedAabb(Vector4 rect, Func<Vector2, Vector2> t)
	{
		var a = t(new Vector2(rect.X, rect.Y)); var b = t(new Vector2(rect.Z, rect.Y)); var e = t(new Vector2(rect.Z, rect.W)); var f = t(new Vector2(rect.X, rect.W));
		var l = MathF.Min(MathF.Min(a.X, b.X), MathF.Min(e.X, f.X)); var top = MathF.Min(MathF.Min(a.Y, b.Y), MathF.Min(e.Y, f.Y));
		var r = MathF.Max(MathF.Max(a.X, b.X), MathF.Max(e.X, f.X)); var bo = MathF.Max(MathF.Max(a.Y, b.Y), MathF.Max(e.Y, f.Y));
		return new Vector4(l, top, r, bo);
	}

	// Intersect a child (sub-recording) clip into the current session clip, transforming it by the replay matrix.
	private ClipData ClipCompose(ClipData c, Func<Vector2, Vector2> t)
	{
		var result = _clip;
		if (!(c.Aabb.X <= -1e8f && c.Aabb.Y <= -1e8f && c.Aabb.Z >= 1e8f && c.Aabb.W >= 1e8f))
		{
			var a = TransformedAabb(c.Aabb, t);
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
					Rect = TransformedAabb(src.Rect, t),
					Radii = src.Radii * sx,
					RadiiY = src.RadiiY * sy,
					Exclude = src.Exclude,
				});
			}
		}
		if (c.PathFan != null)
		{
			var pf = new float[c.PathFan.Length];
			for (int i = 0; i < c.PathFan.Length; i += 2) { var p = t(new Vector2(c.PathFan[i], c.PathFan[i + 1])); pf[i] = p.X; pf[i + 1] = p.Y; }
			result.PathFan = pf;
			result.PathEvenOdd = c.PathEvenOdd;
			result.PathExclude = c.PathExclude;
		}
		return result;
	}
}

public sealed unsafe class WebGpuPresentSession : IPresentSession
{
	private readonly WebGpuDevice _d;
	private readonly WebGpuRenderSurface _s;
	private WColor? _presentClear;
	// Root scale (DPI) applied to the whole replayed frame. The composition records in LOGICAL coords and applies the
	// RasterizationScale through the neutral session (Save→Scale→Replay→Restore); this session must honour it or
	// content renders at logical size on a physical-size surface (the 1.5x-DPI bug). Bracketed by Save/Restore.
	private Vector2 _presentScale = Vector2.One;
	private readonly System.Collections.Generic.Stack<Vector2> _presentScaleStack = new();
	// The single command encoder for the whole frame. Every pass (offscreen coverage/blur/layer + the main pass)
	// records into it and it's submitted once — so wgpu barriers offscreen resolve->sample automatically, without
	// the cross-submission resolve hazard (which previously needed a full-texture readback flush to work around).
	private IntPtr _frameEncoder;
	// Immediate-mode drawing on the present session (e.g. the FPS/diagnostics overlay drawn after Replay) records
	// here and is composited onto the replayed frame at Dispose — the present session IS a real drawing session,
	// like the Skia one, not a replay-only sink. State verbs (Save/Scale/clip/…) forward here too so the overlay
	// honours the transform; Scale/Save/Restore additionally drive the frame's root DPI scale (_presentScale).
	private readonly WebGpuCommandRecorder _overlay = new();
	// The replayed frame's (DPI-scaled) commands + clear, captured at Replay and rendered ONCE at Dispose with the
	// immediate-mode overlay appended as final top-most commands. Deferring lets the whole present be a single pass
	// (no follow-up LoadOp.Load overlay pass), so the fast path's MSAA target resolves on-tile (StoreOp.Discard).
	private List<WebGpuCommand> _pendingCmds;
	private WColor? _pendingClear;
	private long _tReplayStart;
	private static int _streamDumpFrame;
	internal WebGpuPresentSession(WebGpuDevice d, WebGpuRenderSurface s) { _d = d; _s = s; }

	// Runs a frame: opens the shared encoder (if not already inside one), renders, then finishes+submits once.
	// load=true preserves the target's existing colour (LoadOp.Load) so an overlay composites over the frame.
	private void RunFrame(List<WebGpuCommand> cmds, WColor? clear, bool load = false)
	{
		var owns = _frameEncoder == IntPtr.Zero;
		if (owns) { _frameEncoder = wgpuDeviceCreateCommandEncoder(_d.Dev, null); if (WebGpuDevice.PerfEnabled) { _d.PerfBgCreates = 0; _d.PerfBufCreates = 0; _d.PerfSw.Restart(); } }
			var pr = _d.Profiler;
			var tRender = WebGpuProfiler.T();
			try
			{
				RenderInto(cmds, _s, clear, load);
			}
			finally
			{
				if (owns)
				{
					pr?.Render(tRender);
					var tSubmit = WebGpuProfiler.T();
					var cb = wgpuCommandEncoderFinish(_frameEncoder, null);
					wgpuQueueSubmit(_d.Q, 1, (IntPtr)(&cb));
					pr?.Submit(tSubmit);
					// Pump the device. Blocking (wait=1) fully drains the GPU each frame (conservative, serializes CPU/GPU);
					// UNO_WEBGPU_PIPELINE polls non-blocking (wait=0) so the CPU can overlap the next frame with the GPU.
					var tPoll = WebGpuProfiler.T();
					wgpuDevicePoll(_d.Dev, WebGpuDevice.Pipeline ? 0u : 1u, null);
					pr?.Poll(tPoll);
					_frameEncoder = IntPtr.Zero;
				if (WebGpuDevice.PerfEnabled)
				{
					_d.PerfSw.Stop();
					_d.PerfAccumMs += _d.PerfSw.Elapsed.TotalMilliseconds;
					_d.PerfFrame++;
					if (_d.PerfFrame == 1 || _d.PerfFrame % 20 == 0)
					{
						System.Console.Error.WriteLine($"RENDERPERF frame={_d.PerfFrame} cmds={cmds.Count} bgCreates={_d.PerfBgCreates} bufCreates={_d.PerfBufCreates} frameMs={_d.PerfSw.Elapsed.TotalMilliseconds:F2} avgMs={_d.PerfAccumMs / _d.PerfFrame:F2}");
						System.Console.Error.Flush();
					}
				}
			}
		}
	}

	// Computes the device-space scissor for a clip AABB (clamped to the surface). Returns false when degenerate
	// (the op is fully clipped out and should be skipped).
	private bool TryScissor(Vector4 clip, out int x, out int y, out int w, out int h)
	{
		x = (int)MathF.Max(0, MathF.Floor(clip.X)); y = (int)MathF.Max(0, MathF.Floor(clip.Y));
		int r = (int)MathF.Min(_s.Width, MathF.Ceiling(clip.Z)); int b = (int)MathF.Min(_s.Height, MathF.Ceiling(clip.W));
		w = r - x; h = b - y; return w > 0 && h > 0;
	}
	private Vector2 Ndc(Vector2 dev) => new(2f * dev.X / _s.Width - 1f, 1f - 2f * dev.Y / _s.Height);

	// Reused scratch so the per-frame op rebuild doesn't allocate a List + array per primitive (the whole frame is
	// rebuilt every present). Safe: each primitive fills the scratch, uploads it (copied to GPU immediately), and is
	// done before the next — no builder holds the scratch across a nested RenderInto. _clipU backs MakeClipBg's
	// lookup; a bind-group cache MISS clones it before storing.
	private readonly List<float> _scratch = new();
	private readonly float[] _clipU = new float[72];   // ClipU: rects[4]+radii[4] + ex+ctrl+size+xform+xoff+finv + radiiY[4] = 288B

	// Pool of per-RenderInto op lists so a static frame's rebuild doesn't allocate the (large ClipData) op array
	// every present. A stack (not one field) keeps it correct under the recursive nested-layer RenderInto — each
	// level rents its own list and returns it when done.
	private readonly Stack<List<DrawOp>> _opsPool = new();
	private List<DrawOp> RentOps()
		=> _opsPool.Count > 0 ? _opsPool.Pop() : new(256);
	private void ReturnOps(List<DrawOp> ops)
	{
		ops.Clear();   // drops the captured ClipData/PathFan refs; keeps the backing array for reuse
		_opsPool.Push(ops);
	}

	// Per-pass transform table (path fills). 8 floats/slot = a local->NDC affine (a=ax,ay,az,aw  b=bx,by,_,_) folding
	// an extra transform R and the current device->NDC projection. Indexed by a per-recording stable slot baked into
	// the fan/cover verts; rewritten every frame the recording draws, so resize/move/DPI touches only this table, not
	// the (recorded-device or, for arena, local-space) verts. `_xforms` is per-RenderInto (saved/restored around the
	// recursive nested-layer render); transient (immediate-draw) slots are freed at the pass's end.
	private List<float> _xforms;
	private readonly Stack<List<float>> _xformsPool = new();
	private List<int> _xformTransient;
	private readonly Stack<List<int>> _xformTransientPool = new();
	private List<float> RentXforms() => _xformsPool.Count > 0 ? _xformsPool.Pop() : new(64);
	private List<int> RentTransient() => _xformTransientPool.Count > 0 ? _xformTransientPool.Pop() : new(16);

	// Writes `slot`'s local->NDC affine into `_xforms` (growing it), composing R (Identity for recorded-device verts;
	// the replay transform for arena local-space verts) with the current surface's device->NDC map.
	private void WriteXform(int slot, Matrix4x4 r)
	{
		int need = (slot + 1) * 8;
		while (_xforms.Count < need) { _xforms.Add(0f); }
		float w = _s.Width, h = _s.Height;
		int o = slot * 8;
		_xforms[o + 0] = 2f * r.M11 / w; _xforms[o + 1] = 2f * r.M21 / w; _xforms[o + 2] = 2f * r.M41 / w - 1f; _xforms[o + 3] = -2f * r.M12 / h;
		_xforms[o + 4] = -2f * r.M22 / h; _xforms[o + 5] = 1f - 2f * r.M42 / h; _xforms[o + 6] = 0f; _xforms[o + 7] = 0f;
	}

	// A per-frame transform slot for an immediate (non-cached) path fill: allocated from the shared allocator, its
	// projection entry written now (immediate build == draw), and returned to the free-list when the pass ends.
	private int AllocTransientPathSlot()
	{
		int slot = _d.AllocXformSlot();
		_xformTransient.Add(slot);
		WriteXform(slot, Matrix4x4.Identity);
		return slot;
	}

	// Per-pass shared SOLID vertex buffer (ramez arena baseline): every device-space solid run — immediate draws AND
	// solid-only cached recordings — appends its 6-float verts here in op order, so adjacent solid ops sharing a clip
	// occupy a CONTIGUOUS range and the emit loop coalesces them into ONE draw (cross-visual, not just within one
	// recording). Uploaded once per pass; recycled next pass. A solid op with b0==0 references (b1=startVert, u0=count)
	// into this buffer; b0!=0 is a legacy private-buffer solid (mixed/arena recording) that draws on its own.
	private readonly Stack<List<float>> _solidPool = new();
	private List<float> RentSolid() => _solidPool.Count > 0 ? _solidPool.Pop() : new(4096);
	private void ReturnSolid(List<float> s) { s.Clear(); _solidPool.Push(s); }
	// Appends one device-space quad (two tris) to the shared solid buffer; returns the start vertex index. 6 verts.
	private int AppendSolidRect(List<float> solid, Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float r, float g, float b, float a)
	{
		int start = solid.Count / 6;
		void V(Vector2 p) { var n = Ndc(p); solid.Add(n.X); solid.Add(n.Y); solid.Add(r); solid.Add(g); solid.Add(b); solid.Add(a); }
		V(p0); V(p1); V(p2); V(p0); V(p2); V(p3);
		return start;
	}

	// Per-pass shared ROUNDED-RECT buffer (22 floats/vert; ramez per-vertex SDF layout). Every rrect — immediate and
	// re-appended cached — lands here in op order so adjacent rrect ops sharing a clip coalesce into ONE draw across
	// visuals (ramez emits rrect v=6*N; neutral used to emit N separate v=6). Returns the start vertex index.
	private readonly Stack<List<float>> _rrectPool = new();
	private List<float> RentRrect() => _rrectPool.Count > 0 ? _rrectPool.Pop() : new(4096);
	private void ReturnRrect(List<float> s) { s.Clear(); _rrectPool.Push(s); }
	private int AppendRrect(List<float> rr, RoundedRectCmd rrc)
	{
		int start = rr.Count / 22;
		var hf = rrc.Half; var rad = rrc.Radii; var ih = rrc.InnerHalf; var ic = rrc.InnerCenter; var ir = rrc.InnerRadii;
		float cr = rrc.Color.R / 255f, cg = rrc.Color.G / 255f, cb = rrc.Color.B / 255f, ca = rrc.Color.A / 255f * rrc.Opacity;
		Span<Vector2> dev = stackalloc Vector2[4] { rrc.P0, rrc.P1, rrc.P3, rrc.P2 };
		Span<Vector2> ctr = stackalloc Vector2[4] { new(-hf.X, -hf.Y), new(hf.X, -hf.Y), new(-hf.X, hf.Y), new(hf.X, hf.Y) };
		ReadOnlySpan<int> tri = stackalloc int[6] { 0, 1, 2, 2, 1, 3 };
		foreach (var idx in tri)
		{
			var n = Ndc(dev[idx]);
			rr.Add(n.X); rr.Add(n.Y); rr.Add(ctr[idx].X); rr.Add(ctr[idx].Y); rr.Add(hf.X); rr.Add(hf.Y);
			rr.Add(rad.X); rr.Add(rad.Y); rr.Add(rad.Z); rr.Add(rad.W); rr.Add(cr); rr.Add(cg); rr.Add(cb); rr.Add(ca);
			rr.Add(ih.X); rr.Add(ih.Y); rr.Add(ic.X); rr.Add(ic.Y); rr.Add(ir.X); rr.Add(ir.Y); rr.Add(ir.Z); rr.Add(ir.W);
		}
		return start;
	}

	// Transform-table SOLID vert (7 floats): LOCAL device pos (NOT Ndc — the slot's affine applies the replay
	// transform + projection in-shader) + colour + the raw-bits slot index. Mirrors AppendSolidRect, minus the Ndc.
	private void AppendSolidRectLocalT(List<float> solid, Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float r, float g, float b, float a, float slotBits)
	{
		void V(Vector2 p) { solid.Add(p.X); solid.Add(p.Y); solid.Add(r); solid.Add(g); solid.Add(b); solid.Add(a); solid.Add(slotBits); }
		V(p0); V(p1); V(p2); V(p0); V(p2); V(p3);
	}

	// Transform-table ROUNDED-RECT vert (23 floats): LOCAL device corner (NOT Ndc) + the per-vertex SDF params
	// (p/hf/radii, all local + transform-invariant) + colour + inner-ring params + the raw-bits slot index.
	private void AppendRrectLocalT(List<float> rr, RoundedRectCmd rrc, float slotBits)
	{
		var hf = rrc.Half; var rad = rrc.Radii; var ih = rrc.InnerHalf; var ic = rrc.InnerCenter; var ir = rrc.InnerRadii;
		float cr = rrc.Color.R / 255f, cg = rrc.Color.G / 255f, cb = rrc.Color.B / 255f, ca = rrc.Color.A / 255f * rrc.Opacity;
		Span<Vector2> dev = stackalloc Vector2[4] { rrc.P0, rrc.P1, rrc.P3, rrc.P2 };
		Span<Vector2> ctr = stackalloc Vector2[4] { new(-hf.X, -hf.Y), new(hf.X, -hf.Y), new(-hf.X, hf.Y), new(hf.X, hf.Y) };
		ReadOnlySpan<int> tri = stackalloc int[6] { 0, 1, 2, 2, 1, 3 };
		foreach (var idx in tri)
		{
			var d = dev[idx];
			rr.Add(d.X); rr.Add(d.Y); rr.Add(ctr[idx].X); rr.Add(ctr[idx].Y); rr.Add(hf.X); rr.Add(hf.Y);
			rr.Add(rad.X); rr.Add(rad.Y); rr.Add(rad.Z); rr.Add(rad.W); rr.Add(cr); rr.Add(cg); rr.Add(cb); rr.Add(ca);
			rr.Add(ih.X); rr.Add(ih.Y); rr.Add(ic.X); rr.Add(ic.Y); rr.Add(ir.X); rr.Add(ir.Y); rr.Add(ir.Z); rr.Add(ir.W); rr.Add(slotBits);
		}
	}

	private IntPtr MakeBuffer(float[] data)
	{
		var size = data.Length * sizeof(float);
		var buf = _d.BufferPool.Rent(size, WGPUBufferUsage.Vertex | WGPUBufferUsage.CopyDst);
		fixed (float* p = data) { wgpuQueueWriteBuffer(_d.Q, buf, 0, (IntPtr)p, (nuint)size); }
		_d.Profiler?.Upload(size);
		return buf;
	}

	// List overload: uploads directly from the list's backing store (no ToArray copy).
	private IntPtr MakeBuffer(List<float> data)
	{
		var span = System.Runtime.InteropServices.CollectionsMarshal.AsSpan(data);
		var size = span.Length * sizeof(float);
		var buf = _d.BufferPool.Rent(size, WGPUBufferUsage.Vertex | WGPUBufferUsage.CopyDst);
		fixed (float* p = span) { wgpuQueueWriteBuffer(_d.Q, buf, 0, (IntPtr)p, (nuint)size); }
		_d.Profiler?.Upload(size);
		return buf;
	}

	private IntPtr Vbuf(List<float> data, OwnedResources owned)
		=> owned is null ? MakeBuffer(data) : Vbuf(System.Runtime.InteropServices.CollectionsMarshal.AsSpan(data).ToArray(), owned);

	// Append a coloured vertex (pos in device space -> NDC) to the scratch. A class method, not a per-primitive
	// local function, so building a run of rects/a path cover allocates no capturing closure.
	private void PushVert(Vector2 dev, float r, float g, float b, float a)
	{
		var n = Ndc(dev);
		_scratch.Add(n.X); _scratch.Add(n.Y); _scratch.Add(r); _scratch.Add(g); _scratch.Add(b); _scratch.Add(a);
	}

	// Table-path cover vertex: recorded-DEVICE pos + colour + the transform SLOT (raw u32 bits in a float slot). No
	// Ndc — the vertex shader applies xf[slot] (device->NDC, folding the replay transform + current projection).
	private void PushVertT(Vector2 dev, float r, float g, float b, float a, float slotBits)
	{
		_scratch.Add(dev.X); _scratch.Add(dev.Y); _scratch.Add(r); _scratch.Add(g); _scratch.Add(b); _scratch.Add(a); _scratch.Add(slotBits);
	}

	private IntPtr MakeUniform(int byteSize)
		=> _d.BufferPool.Rent(byteSize, WGPUBufferUsage.Uniform | WGPUBufferUsage.CopyDst);

	// Resource allocation that is pooled (owned == null) for per-frame commands, or persistent (added to `owned`
	// for later release) for a cached recording's geometry that must survive across frames.
	private IntPtr Vbuf(float[] data, OwnedResources owned)
	{
		if (owned is null) { return MakeBuffer(data); }
		int size = data.Length * sizeof(float);
		var bd = new WGPUBufferDescriptor { Size = (nuint)size, Usage = WGPUBufferUsage.Vertex | WGPUBufferUsage.CopyDst };
		var buf = wgpuDeviceCreateBuffer(_d.Dev, &bd);
		fixed (float* p = data) { wgpuQueueWriteBuffer(_d.Q, buf, 0, (IntPtr)p, (nuint)size); }
		owned.Buffers.Add((nint)buf);
		return buf;
	}

	private IntPtr Ubuf(int size, OwnedResources owned)
	{
		if (owned is null) { return MakeUniform(size); }
		var bd = new WGPUBufferDescriptor { Size = (nuint)size, Usage = WGPUBufferUsage.Uniform | WGPUBufferUsage.CopyDst };
		var buf = wgpuDeviceCreateBuffer(_d.Dev, &bd);
		owned.Buffers.Add((nint)buf);
		return buf;
	}

	private IntPtr Bg(ref WGPUBindGroupDescriptor bgd, OwnedResources owned)
	{
		var bg = wgpuDeviceCreateBindGroup(_d.Dev, (WGPUBindGroupDescriptor*)Unsafe.AsPointer(ref bgd));
		if (owned is null) { _d.TrackBg(bg); } else { owned.BindGroups.Add((nint)bg); }
		return bg;
	}

	// The clip bind group for a command: just the ClipU uniform (rounded-rect + surface size). Arbitrary path clips
	// are applied via the shared depth mask in the main pass, not sampled here, so there is no coverage texture.
	private IntPtr MakeClipBg(IntPtr bgl, ClipData cd, OwnedResources owned = null, Matrix3x2 xform = default, Matrix3x2 finv = default)
	{
		if (xform == default) { xform = Matrix3x2.Identity; }   // default(Matrix3x2) is all-zero; treat as identity
		if (finv == default) { finv = Matrix3x2.Identity; }
		const int ClipUBytes = 288;   // rects[4]+radii[4] (128) + ex+ctrl+size+xform+xoff+finv (96) + radiiY[4] (64); match the WGSL struct
		var cu = _clipU;
		System.Array.Clear(cu);
		var rounds = cd.Rounds;
		int n = rounds?.Length ?? 0;
		if (n > ClipData.MaxRounds) { n = ClipData.MaxRounds; }
		for (int i = 0; i < n; i++)
		{
			var rc = rounds[i];
			cu[i * 4 + 0] = rc.Rect.X; cu[i * 4 + 1] = rc.Rect.Y; cu[i * 4 + 2] = rc.Rect.Z; cu[i * 4 + 3] = rc.Rect.W;   // rects[i]
			cu[16 + i * 4 + 0] = rc.Radii.X; cu[16 + i * 4 + 1] = rc.Radii.Y; cu[16 + i * 4 + 2] = rc.Radii.Z; cu[16 + i * 4 + 3] = rc.Radii.W;   // radii[i] (X)
			cu[56 + i * 4 + 0] = rc.RadiiY.X; cu[56 + i * 4 + 1] = rc.RadiiY.Y; cu[56 + i * 4 + 2] = rc.RadiiY.Z; cu[56 + i * 4 + 3] = rc.RadiiY.W;   // radiiY[i]
			cu[32 + i] = rc.Exclude ? 1f : 0f;   // ex[i]
		}
		cu[36] = n;                              // ctrl.x = active count
		cu[40] = _s.Width; cu[41] = _s.Height;   // size
		// xform maps stored (identity-baked) NDC verts to the replay NDC: px = M11*x + M21*y + M31, py = M12*x + M22*y + M32.
		cu[44] = xform.M11; cu[45] = xform.M21; cu[46] = xform.M12; cu[47] = xform.M22;
		cu[48] = xform.M31; cu[49] = xform.M32;   // xoff.xy (NDC translation)
		// finv maps the device fragment position back to the recording's own space (inverse device affine) so a clip
		// baked at identity is correct after the move. Identity => clipCov sees fc unchanged. finv 2x2 in `finv`,
		// finv translation in xoff.zw (px = fM11*x + fM21*y + fM31, py = fM12*x + fM22*y + fM32).
		cu[50] = finv.M31; cu[51] = finv.M32;
		cu[52] = finv.M11; cu[53] = finv.M12; cu[54] = finv.M21; cu[55] = finv.M22;

		// The ClipU depends only on (layout, these floats) — identical across frames for static chrome — so reuse a
		// cached bind group. Now that path clips carry no per-frame coverage texture, every clip is cacheable.
		if (owned is null)
		{
			if (_d.TryGetCachedBg(bgl, cu, out var cachedBg)) { return cachedBg; }
			var cbd = new WGPUBufferDescriptor { Size = ClipUBytes, Usage = WGPUBufferUsage.Uniform | WGPUBufferUsage.CopyDst };
			var cbuf = wgpuDeviceCreateBuffer(_d.Dev, &cbd);
			fixed (float* p = cu) { wgpuQueueWriteBuffer(_d.Q, cbuf, 0, (IntPtr)p, ClipUBytes); }
			var ce = new WGPUBindGroupEntry { Binding = 0, Buffer = cbuf, Offset = 0, Size = ClipUBytes };
			var cbgd = new WGPUBindGroupDescriptor { Layout = bgl, EntryCount = 1, Entries = &ce };
			var cbg = wgpuDeviceCreateBindGroup(_d.Dev, (WGPUBindGroupDescriptor*)Unsafe.AsPointer(ref cbgd));
			_d.AddCachedBg(bgl, (float[])cu.Clone(), cbuf, cbg);   // cache stores the key — clone off the reused scratch
			return cbg;
		}

		var buf = Ubuf(ClipUBytes, owned);
		fixed (float* p = cu) { wgpuQueueWriteBuffer(_d.Q, buf, 0, (IntPtr)p, ClipUBytes); }
		var e = new WGPUBindGroupEntry { Binding = 0, Buffer = buf, Offset = 0, Size = ClipUBytes };
		var bgd = new WGPUBindGroupDescriptor { Layout = bgl, EntryCount = 1, Entries = &e };
		return Bg(ref bgd, owned);
	}

	// Fills the shadow silhouette into an offscreen coverage surface (stencil-then-cover, white), then blurs it
	// separably (H then V). Returns the blurred coverage texture + its device-space placement. NOTE: the per-
	// shadow textures are not pooled/freed yet — fine for offscreen/one-shot; the on-window path needs cleanup.
	private IntPtr RenderShadow(ShadowCmd sh, out Vector2 origin, out Vector2 size)
	{
		_d.Profiler?.OsShadow();
		float pad = MathF.Ceiling(3f * MathF.Max(sh.SigmaX, sh.SigmaY)) + 2f;
		origin = new Vector2(sh.BbMin.X - pad, sh.BbMin.Y - pad);
		int sw = Math.Clamp((int)MathF.Ceiling(sh.BbMax.X - sh.BbMin.X + 2 * pad), 1, 4096);
		int sh2 = Math.Clamp((int)MathF.Ceiling(sh.BbMax.Y - sh.BbMin.Y + 2 * pad), 1, 4096);
		size = new Vector2(sw, sh2);

		// 1) coverage: fill the fan (stencil-then-cover, white) into an MSAA surface resolved to single-sample.
		var cov = new WebGpuRenderSurface(_d, sw, sh2, _d.Pool);
		var fanNdc = new float[sh.FanDevice.Length];
		for (int i = 0; i < sh.FanDevice.Length; i += 2)
		{
			fanNdc[i] = (sh.FanDevice[i] - origin.X) / sw * 2f - 1f;
			fanNdc[i + 1] = 1f - (sh.FanDevice[i + 1] - origin.Y) / sh2 * 2f;
		}
		var fanBuf = MakeBuffer(fanNdc);
		var cq = new List<float>();
		void CQ(float x, float y) { cq.Add(x); cq.Add(y); cq.Add(1f); cq.Add(1f); cq.Add(1f); cq.Add(1f); }
		CQ(-1, -1); CQ(1, -1); CQ(1, 1); CQ(-1, -1); CQ(1, 1); CQ(-1, 1);
		var coverBuf = MakeBuffer(cq.ToArray());
		var noClip = MakeClipBg(_d.CoverClipBgl, default);

		var ca = new WGPURenderPassColorAttachment { DepthSlice = uint.MaxValue, View = cov.MsaaColorView, ResolveTarget = _d.MsaaSamples > 1 ? cov.View : IntPtr.Zero, LoadOp = WGPULoadOp.Clear, StoreOp = _d.MsaaSamples > 1 ? WGPUStoreOp.Discard : WGPUStoreOp.Store, ClearValue = default };
		var dsa = new WGPURenderPassDepthStencilAttachment { View = cov.DepthView, DepthLoadOp = WGPULoadOp.Clear, DepthStoreOp = WGPUStoreOp.Discard, DepthClearValue = 0f, StencilLoadOp = WGPULoadOp.Clear, StencilStoreOp = WGPUStoreOp.Discard, StencilClearValue = 0 };
		var rp = new WGPURenderPassDescriptor { ColorAttachmentCount = 1, ColorAttachments = &ca, DepthStencilAttachment = &dsa };
		var pass = wgpuCommandEncoderBeginRenderPass(_frameEncoder, &rp);
		WebGpuTrace.Pass("shadow-coverage", sw, sh2, _d.MsaaSamples, true);
		wgpuRenderPassEncoderSetPipeline(pass, sh.EvenOdd ? _d.StencilEvenOdd : _d.StencilNonZero);
		wgpuRenderPassEncoderSetBindGroup(pass, 0, MakeClipBg(_d.ClipBgl, default), 0, (uint*)null);   // identity xform (shadow fan already NDC)
		wgpuRenderPassEncoderSetVertexBuffer(pass, 0, fanBuf, 0, (nuint)(fanNdc.Length * sizeof(float)));
		wgpuRenderPassEncoderDraw(pass, (uint)(fanNdc.Length / 2), 1, 0, 0);
		WebGpuTrace.Draw(sh.EvenOdd ? "shadow-stencil-eo" : "shadow-stencil-nz", (uint)(fanNdc.Length / 2));
		wgpuRenderPassEncoderSetPipeline(pass, _d.CoverPipe);
		wgpuRenderPassEncoderSetBindGroup(pass, 0, noClip, 0, (uint*)null);
		wgpuRenderPassEncoderSetStencilReference(pass, 0);
		wgpuRenderPassEncoderSetVertexBuffer(pass, 0, coverBuf, 0, (nuint)(cq.Count * sizeof(float)));
		wgpuRenderPassEncoderDraw(pass, 6, 1, 0, 0);
		WebGpuTrace.Draw("shadow-cover", 6);
		wgpuRenderPassEncoderEnd(pass);
		WebGpuTrace.PassEnd();
		if (_d.MsaaSamples > 1) { _d.Pool.Return(cov.MsaaColorView); }   // at 1x MsaaColorView aliases cov.View (blurred next) — don't reclaim
		_d.Pool.Return(cov.DepthView);

		// 2) blur pyramid (2x downsample + separable gaussian), matching the original's 3-pass shadow blur.
		return BlurPyramid(cov.View, sw, sh2, sh.SigmaX, sh.SigmaY);
	}

	// Blur pyramid over a REGION of `src`: extract the device-px rect (rx,ry,rw,rh) out of the fullW×fullH source
	// into a sigma-scaled downsample pyramid (depth set by the requested blur radius), then a fixed 9-tap separable
	// gaussian on the small top level. Returns the region-sized blurred view; the caller maps screen px -> region uv
	// in the composite (bilinear upscales it). Only the region behind the acrylic element is ever processed, and the
	// per-pass kernel is constant, so a large blur is a few tiny passes instead of a full-frame O(sigma) kernel.
	private IntPtr BlurPyramidRegion(IntPtr src, int fullW, int fullH, float rx, float ry, float rw, float rh, float sigmaX, float sigmaY)
	{
		int iw = Math.Max(1, (int)MathF.Round(rw)), ih = Math.Max(1, (int)MathF.Round(rh));
		float sigma = MathF.Max(sigmaX, sigmaY);
		int levels = Math.Clamp((int)MathF.Round(MathF.Log2(MathF.Max(sigma, 1f) / 2f)), 1, 5);
		while (levels > 1 && ((iw >> levels) < 4 || (ih >> levels) < 4)) { levels--; }

		var origin = new Vector2(rx / fullW, ry / fullH);
		var scale = new Vector2(rw / fullW, rh / fullH);
		int cw = Math.Max(1, iw >> 1), ch = Math.Max(1, ih >> 1);
		var cur = _d.Pool.Rent(cw, ch, 1, WGPUTextureUsage.RenderAttachment | WGPUTextureUsage.TextureBinding, WebGpuDevice.DefaultColorFormat);
		BlurPass(src, cur, default, default, downsample: true, origin, scale);   // extract sub-rect + downsample ×2
		for (int l = 2; l <= levels; l++)
		{
			int nw = Math.Max(1, cw >> 1), nh = Math.Max(1, ch >> 1);
			var nx = _d.Pool.Rent(nw, nh, 1, WGPUTextureUsage.RenderAttachment | WGPUTextureUsage.TextureBinding, WebGpuDevice.DefaultColorFormat);
			BlurPass(cur, nx, default, default, downsample: true, Vector2.Zero, Vector2.One);
			cur = nx; cw = nw; ch = nh;
		}
		var hh = _d.Pool.Rent(cw, ch, 1, WGPUTextureUsage.RenderAttachment | WGPUTextureUsage.TextureBinding, WebGpuDevice.DefaultColorFormat);
		BlurPass(cur, hh, new Vector2(1f, 0f), new Vector2(1f / cw, 0f), downsample: false, Vector2.Zero, Vector2.One);
		var vv = _d.Pool.Rent(cw, ch, 1, WGPUTextureUsage.RenderAttachment | WGPUTextureUsage.TextureBinding, WebGpuDevice.DefaultColorFormat);
		BlurPass(hh, vv, new Vector2(0f, 1f), new Vector2(0f, 1f / ch), downsample: false, Vector2.Zero, Vector2.One);
		return vv;
	}

	// Full-source blur (shadow coverage, already bbox-sized): the region IS the whole texture.
	private IntPtr BlurPyramid(IntPtr src, int w, int h, float sigmaX, float sigmaY)
		=> BlurPyramidRegion(src, w, h, 0f, 0f, w, h, sigmaX, sigmaY);

	private void BlurPass(IntPtr src, IntPtr dst, Vector2 dir, Vector2 texel, bool downsample, Vector2 srcOrigin, Vector2 srcScale)
	{
		var bu = new float[12];
		bu[0] = dir.X; bu[1] = dir.Y; bu[2] = texel.X; bu[3] = texel.Y;
		bu[4] = downsample ? 1f : 0f; bu[5] = 0f;
		bu[6] = srcOrigin.X; bu[7] = srcOrigin.Y; bu[8] = srcScale.X; bu[9] = srcScale.Y;
		var ubuf = MakeUniform(48);
		fixed (float* p = bu) { wgpuQueueWriteBuffer(_d.Q, ubuf, 0, (IntPtr)p, 48); }
		var entries = stackalloc WGPUBindGroupEntry[3];
		entries[0] = new WGPUBindGroupEntry { Binding = 0, TextureView = src };
		entries[1] = new WGPUBindGroupEntry { Binding = 1, Sampler = _d.Smp };
		entries[2] = new WGPUBindGroupEntry { Binding = 2, Buffer = ubuf, Offset = 0, Size = 48 };
		var bgd = new WGPUBindGroupDescriptor { Layout = _d.BlurBgl, EntryCount = 3, Entries = entries };
		var bg = _d.TrackBg(wgpuDeviceCreateBindGroup(_d.Dev, &bgd));

		var ca = new WGPURenderPassColorAttachment { DepthSlice = uint.MaxValue, View = dst, LoadOp = WGPULoadOp.Clear, StoreOp = WGPUStoreOp.Store, ClearValue = default };
		var rp = new WGPURenderPassDescriptor { ColorAttachmentCount = 1, ColorAttachments = &ca };
		var pass = wgpuCommandEncoderBeginRenderPass(_frameEncoder, &rp);
		WebGpuTrace.Pass(downsample ? "blur-down" : (dir.X != 0 ? "blur-h" : "blur-v"), 0, 0, 1, true);
		wgpuRenderPassEncoderSetPipeline(pass, _d.BlurPipe);
		wgpuRenderPassEncoderSetBindGroup(pass, 0, bg, 0, (uint*)null);
		wgpuRenderPassEncoderDraw(pass, 3, 1, 0, 0);
		WebGpuTrace.Draw("blur", 3);
		wgpuRenderPassEncoderEnd(pass);
		WebGpuTrace.PassEnd();
	}

	public void Replay(IRenderRecord data)
	{
		// During an async backend switch (e.g. the browser's on-canvas WebGPU init) a frame recorded by the
		// previous renderer can reach us; skip it rather than mis-cast — the next frame is recorded by this backend.
		if (data is not WebGpuRenderRecord rd) { return; }
		lock (_d.RenderGate)
		{
			WebGpuTrace.Reset();
			var pr = _d.Profiler;
			_tReplayStart = WebGpuProfiler.T();
			pr?.Cmds(rd.Commands.Count);
			var tBf = WebGpuProfiler.T();
			_d.BeginFrameResources();   // reclaim last frame's pooled textures/buffers + release its bind groups
			_d.SolidSlab.BeginFrame(); _d.RrectSlab.BeginFrame();   // reset the shared slabs' live sets for this frame
			_d.SolidTableSlab.BeginFrame(); _d.RrectTableSlab.BeginFrame();
			pr?.BeginFrameT(tBf);
			// Apply the root DPI scale to the whole (logical-coord) frame. Nested retained recordings keep their
			// command-list reference (only their Transform gains the scale) so the geometry cache still hits.
			// The actual render is deferred to Dispose so the immediate-mode overlay can be inlined (single pass).
			_pendingCmds = (_presentScale.X == 1f && _presentScale.Y == 1f)
				? rd.Commands
				: WebGpuCommandRecorder.TransformFor(rd.Commands, Matrix4x4.CreateScale(_presentScale.X, _presentScale.Y, 1f), ClipData.None);
			_pendingClear = _presentClear ?? rd.ClearColor;
		}
	}

	// Renders WITHOUT the per-frame reset — for a nested offscreen render (RenderOffscreen) that may run inside an
	// enclosing frame; resetting the shared pools mid-frame would free the enclosing frame's in-flight resources.
	// The gate is reentrant, so a nested call inside an enclosing Replay is safe; an independent call is serialized.
	public void ReplayNested(IRenderRecord data)
	{
		if (data is not WebGpuRenderRecord rd) { return; }
		lock (_d.RenderGate)
		{
			RunFrame(rd.Commands, _presentClear ?? rd.ClearColor);
		}
	}

	// VALUE equality: the rounded/path clip arrays are re-allocated every frame (copy-on-write Push / ClipCompose),
	// so a reference compare reports a stable clip as "changed" every frame -> a needless per-frame geometry rebuild
	// for every clipped cached recording (was the button scene's dominant CPU cost, ~100 rebuilds/frame). Compare by
	// content instead - far cheaper than the rebuild it prevents (Rounds is <=4 elements; the fan only when both have one).
	private static bool ClipDataEquals(in ClipData a, in ClipData b)
	{
		if (a.Aabb != b.Aabb) { return false; }
		int an = a.Rounds?.Length ?? 0, bn = b.Rounds?.Length ?? 0;
		if (an != bn) { return false; }
		for (int i = 0; i < an; i++)
		{
			var x = a.Rounds[i]; var y = b.Rounds[i];
			if (x.Rect != y.Rect || x.Radii != y.Radii || x.RadiiY != y.RadiiY || x.Exclude != y.Exclude) { return false; }
		}
		if ((a.PathFan is null) != (b.PathFan is null)) { return false; }
		if (a.PathFan is { } fa && b.PathFan is { } fb)
		{
			if (a.PathEvenOdd != b.PathEvenOdd || a.PathExclude != b.PathExclude) { return false; }
			if (!((ReadOnlySpan<float>)fa).SequenceEqual(fb)) { return false; }
		}
		return true;
	}

	// A recording is arena-safe when every draw is a solid rect or image with no clip: then the fragment shader
	// doesn't depend on device position, so its geometry can be baked once in the recording's own space and moved by
	// re-stamping the vertex xform (clipCov is a constant 1). Paths (stencil pass has no xform), gradients (device-
	// space geometry in the fragment) and any clip need a device-space re-stamp and are NOT arena-safe yet.
	// A recording contains at least one rect — its solids are cheap to re-emit each frame into the shared solid
	// buffer (ramez arena baseline) so they coalesce across visuals; any non-solids stay cached (NonSolidOps).
	// Re-appendable = rect or rounded-rect: cheap to re-emit each frame into a shared per-pass buffer (solids /
	// rrects) so they coalesce across visuals. Glyphs/images/gradients stay cached and are spliced in draw order.
	private static bool HasReappendable(List<WebGpuCommand> cmds)
	{
		for (int i = 0; i < cmds.Count; i++) { if (cmds[i] is RectCommand or RoundedRectCmd) { return true; } }
		return false;
	}
	private static bool HasNonRect(List<WebGpuCommand> cmds)
	{
		for (int i = 0; i < cmds.Count; i++) { if (cmds[i] is not (RectCommand or RoundedRectCmd)) { return true; } }
		return false;
	}

	private static bool IsArenaSafe(List<WebGpuCommand> cmds)
	{
		for (int i = 0; i < cmds.Count; i++)
		{
			var c = cmds[i];
			// Solid/image/gradient/path all route device fc through finv; the path stencil fan carries the xform via
			// the shared ClipU layout (ClipBgl binds to both stencil + cover). A rect/rounded clip is fine (clipCov
			// maps fc back via finv); a PATH clip uses the depth mask (no finv) so it's still excluded.
			if (c is not (RectCommand or ImageCmd or GradientCmd or PathFill) || c.Clip.PathFan is not null) { return false; }
		}
		return cmds.Count > 0;
	}

	// The NDC->NDC affine that maps the recording's own (identity-baked) NDC verts to the replay transform `t`
	// (device->device). Derived so re-stamping this uniform reproduces baking `t` into the verts: with A = the
	// device->NDC map (surface size), the vertex xform is A·T·A⁻¹. Lets a moved cached visual reuse its geometry.
	private Matrix3x2 ArenaXform(Matrix4x4 t)
	{
		float w = _s.Width, h = _s.Height;
		float a = t.M11, b = t.M21, c = t.M12, d = t.M22, e = t.M41, f = t.M42;
		return new Matrix3x2(
			a, -c * w / h,
			-b * h / w, d,
			a + b * h / w + 2f * e / w - 1f,
			-(c * w / h + d) - 2f * f / h + 1f);
	}

	// Builds ops for a command list, COALESCING runs of consecutive same-clip solid rects into one vertex buffer +
	// one draw (a Border's background+edges collapse from 4 draws to 1). Used for cached recordings — the per-command
	// BuildSimpleOp path did not coalesce, so every cached visual emitted a draw per rect (a major draw-count source
	// on Intel, where per-draw overhead dominates — see the RenderDoc capture). Coalesced rects share a clip so they
	// share the arena xform (one clip bind group), staying correct under re-stamp.
	private void BuildCoalesced(List<WebGpuCommand> cmds, List<DrawOp> ops, OwnedResources owned, int pathSlot)
	{
		float slotBits = System.BitConverter.Int32BitsToSingle(pathSlot);
		for (int ci = 0; ci < cmds.Count; ci++)
		{
			if (cmds[ci] is RectCommand rc0)
			{
				_scratch.Clear();
				int j = ci;
				while (j < cmds.Count && cmds[j] is RectCommand rcj && ClipDataEquals(rcj.Clip, rc0.Clip))
				{
					float vr = rcj.Color.R / 255f, vg = rcj.Color.G / 255f, vb = rcj.Color.B / 255f, va = rcj.Color.A / 255f;
					PushVert(rcj.P0, vr, vg, vb, va); PushVert(rcj.P1, vr, vg, vb, va); PushVert(rcj.P2, vr, vg, vb, va);
					PushVert(rcj.P0, vr, vg, vb, va); PushVert(rcj.P2, vr, vg, vb, va); PushVert(rcj.P3, vr, vg, vb, va);
					j++;
				}
				var rvb = Vbuf(_scratch, owned);
				ops.Add(new DrawOp(0, (nint)rvb, (uint)((j - ci) * 6), 0, false, rc0.Clip, (nint)MakeClipBg(_d.SolidClipBgl, rc0.Clip, owned)));
				ci = j - 1;
			}
			else if (cmds[ci] is PathFill pf0 && !pf0.EvenOdd)
			{
				// Coalesce a run of consecutive NON-ZERO paths sharing colour + clip (a text run's glyphs) into one
				// stencil (all fans) + one cover over the union bbox — N glyphs collapse from 2N draws to 2. Safe for
				// non-zero winding: the union of same-colour shapes fills identically. Even-odd is excluded (an overlap
				// would XOR to a hole), and per-path clips (PathFan) never enter cached recordings (not arena-safe).
				_scratch.Clear();
				var bbMin = new Vector2(float.MaxValue); var bbMax = new Vector2(float.MinValue);
				int j = ci;
				while (j < cmds.Count && cmds[j] is PathFill pfj && !pfj.EvenOdd
					&& pfj.Color.R == pf0.Color.R && pfj.Color.G == pf0.Color.G && pfj.Color.B == pf0.Color.B && pfj.Color.A == pf0.Color.A
					&& ClipDataEquals(pfj.Clip, pf0.Clip))
				{
					for (int i = 0; i < pfj.FanDevice.Length; i += 2) { _scratch.Add(pfj.FanDevice[i]); _scratch.Add(pfj.FanDevice[i + 1]); _scratch.Add(slotBits); }
					bbMin = Vector2.Min(bbMin, pfj.BbMin); bbMax = Vector2.Max(bbMax, pfj.BbMax);
					j++;
				}
				var fanBuf = Vbuf(_scratch, owned);
				uint fanCount = (uint)(_scratch.Count / 3);
				float pr = pf0.Color.R / 255f, pg = pf0.Color.G / 255f, pb = pf0.Color.B / 255f, pa = pf0.Color.A / 255f;
				_scratch.Clear();
				var tl = bbMin; var br = bbMax; var tr = new Vector2(br.X, tl.Y); var bl = new Vector2(tl.X, br.Y);
				PushVertT(tl, pr, pg, pb, pa, slotBits); PushVertT(tr, pr, pg, pb, pa, slotBits); PushVertT(br, pr, pg, pb, pa, slotBits);
				PushVertT(tl, pr, pg, pb, pa, slotBits); PushVertT(br, pr, pg, pb, pa, slotBits); PushVertT(bl, pr, pg, pb, pa, slotBits);
				var covBuf = Vbuf(_scratch, owned);
				ops.Add(new DrawOp(1, (nint)fanBuf, fanCount, (nint)covBuf, false, pf0.Clip, (nint)MakeClipBg(_d.CoverClipBgl, pf0.Clip, owned)));
				ci = j - 1;
			}
			else { BuildSimpleOp(cmds[ci], ops, owned, pathSlot); }
		}
	}

	// Builds the draw-op(s) for a simple primitive (rect/path/image/gradient) into `ops`, allocating GPU resources
	// pooled (owned == null, per-frame) or persistent (owned != null, a cached recording's geometry).
	private DrawOp ResidentizeFan(DrawOp op, OwnedResources owned)
	{
		if (owned is not null && op.clip.PathFan is { } fan && op.clip.FanBuf == 0)
		{
			_scratch.Clear();
			for (int j = 0; j < fan.Length; j += 2) { var n = Ndc(new Vector2(fan[j], fan[j + 1])); _scratch.Add(n.X); _scratch.Add(n.Y); }
			var c = op.clip; c.FanBuf = (nint)Vbuf(_scratch, owned); c.FanW = (int)_s.Width; c.FanH = (int)_s.Height;
			op.clip = c;
		}
		return op;
	}

	private void BuildSimpleOp(WebGpuCommand cmd, List<DrawOp> ops, OwnedResources owned, int pathSlot)
	{
		switch (cmd)
		{
			case RectCommand rc:
			{
				var c = new Vector4(rc.Color.R / 255f, rc.Color.G / 255f, rc.Color.B / 255f, rc.Color.A / 255f);
				var v = new List<float>();
				void V(Vector2 p) { var n = Ndc(p); v.Add(n.X); v.Add(n.Y); v.Add(c.X); v.Add(c.Y); v.Add(c.Z); v.Add(c.W); }
				V(rc.P0); V(rc.P1); V(rc.P2); V(rc.P0); V(rc.P2); V(rc.P3);
				ops.Add(new DrawOp(0, (nint)Vbuf(v.ToArray(), owned), 6, 0, false, rc.Clip, (nint)MakeClipBg(_d.SolidClipBgl, rc.Clip, owned)));
				break;
			}
			case PathFill pf:
			{
				float slotBits = System.BitConverter.Int32BitsToSingle(pathSlot);
				_scratch.Clear();
				for (int i = 0; i < pf.FanDevice.Length; i += 2) { _scratch.Add(pf.FanDevice[i]); _scratch.Add(pf.FanDevice[i + 1]); _scratch.Add(slotBits); }
				var fanBuf = Vbuf(_scratch, owned);
				float pr = pf.Color.R / 255f, pg = pf.Color.G / 255f, pb = pf.Color.B / 255f, pa = pf.Color.A / 255f;
				_scratch.Clear();
				var tl = pf.BbMin; var br = pf.BbMax; var tr = new Vector2(br.X, tl.Y); var bl = new Vector2(tl.X, br.Y);
				PushVertT(tl, pr, pg, pb, pa, slotBits); PushVertT(tr, pr, pg, pb, pa, slotBits); PushVertT(br, pr, pg, pb, pa, slotBits);
				PushVertT(tl, pr, pg, pb, pa, slotBits); PushVertT(br, pr, pg, pb, pa, slotBits); PushVertT(bl, pr, pg, pb, pa, slotBits);
				var covBuf = Vbuf(_scratch, owned);
				var clipBg = MakeClipBg(_d.CoverClipBgl, pf.Clip, owned);
				ops.Add(new DrawOp(1, (nint)fanBuf, (uint)(pf.FanDevice.Length / 2), (nint)covBuf, pf.EvenOdd, pf.Clip, (nint)clipBg));
				break;
			}
			case ImageCmd im:
			{
				var view = im.View;
				var ubuf = Ubuf(112, owned);
				var op = stackalloc float[28];
				bool hasMatrix = im.ColorMatrix is { Length: >= 20 };
				op[0] = im.Opacity; op[1] = im.TintMode; op[2] = hasMatrix ? 1f : 0f; op[3] = 0;
				op[4] = im.Tint.X; op[5] = im.Tint.Y; op[6] = im.Tint.Z; op[7] = im.Tint.W;
				if (im.ColorMatrix is { Length: >= 20 } mm)
				{
					op[8] = mm[0]; op[9] = mm[1]; op[10] = mm[2]; op[11] = mm[3];        // m0
					op[12] = mm[5]; op[13] = mm[6]; op[14] = mm[7]; op[15] = mm[8];      // m1
					op[16] = mm[10]; op[17] = mm[11]; op[18] = mm[12]; op[19] = mm[13];  // m2
					op[20] = mm[15]; op[21] = mm[16]; op[22] = mm[17]; op[23] = mm[18];  // m3
					op[24] = mm[4]; op[25] = mm[9]; op[26] = mm[14]; op[27] = mm[19];    // off (5th column)
				}
				wgpuQueueWriteBuffer(_d.Q, ubuf, 0, (IntPtr)op, 112);
				var entries = stackalloc WGPUBindGroupEntry[3];
				entries[0] = new WGPUBindGroupEntry { Binding = 0, TextureView = view };
				entries[1] = new WGPUBindGroupEntry { Binding = 1, Sampler = _d.Smp };
				entries[2] = new WGPUBindGroupEntry { Binding = 2, Buffer = ubuf, Offset = 0, Size = 112 };
				var bgd = new WGPUBindGroupDescriptor { Layout = _d.ImgBgl, EntryCount = 3, Entries = entries };
				var bg = Bg(ref bgd, owned);
				var q = new float[24];
				void QV(int idx, Vector2 pos, float u, float vv) { var n = Ndc(pos); q[idx] = n.X; q[idx + 1] = n.Y; q[idx + 2] = u; q[idx + 3] = vv; }
				QV(0, im.P0, im.U0, im.V0); QV(4, im.P1, im.U1, im.V0); QV(8, im.P2, im.U1, im.V1); QV(12, im.P0, im.U0, im.V0); QV(16, im.P2, im.U1, im.V1); QV(20, im.P3, im.U0, im.V1);
				ops.Add(new DrawOp(2, (nint)bg, 0, (nint)Vbuf(q, owned), false, im.Clip, (nint)MakeClipBg(_d.ImageClipBgl, im.Clip, owned)));
				break;
			}
			case GradientCmd gc:
			{
				var bytes = (nuint)WebGpuDevice.GradientUniformBytes;
				// A per-frame gradient bind group depends only on its uniform (the packed stops/geometry) — cache it
				// across frames like clips, so a static gradient isn't a CreateBuffer + CreateBindGroup every frame.
				IntPtr gbg;
				if (!(owned is null && _d.TryGetCachedBg((nint)_d.GradBgl, gc.Uniform, out gbg)))
				{
					// Cached (owned == null) entries need a PERSISTENT uniform buffer — a pooled one would be reused
					// next frame and corrupt the cached bind group. Cached-recording (owned) buffers persist already.
					IntPtr ubuf;
					if (owned is null)
					{
						var gbd = new WGPUBufferDescriptor { Size = bytes, Usage = WGPUBufferUsage.Uniform | WGPUBufferUsage.CopyDst };
						ubuf = wgpuDeviceCreateBuffer(_d.Dev, &gbd);
					}
					else { ubuf = Ubuf((int)bytes, owned); }
					fixed (float* p = gc.Uniform) { wgpuQueueWriteBuffer(_d.Q, ubuf, 0, (IntPtr)p, bytes); }
					var gentry = new WGPUBindGroupEntry { Binding = 0, Buffer = ubuf, Offset = 0, Size = bytes };
					var gbgd = new WGPUBindGroupDescriptor { Layout = _d.GradBgl, EntryCount = 1, Entries = &gentry };
					if (owned is null)
					{
						gbg = wgpuDeviceCreateBindGroup(_d.Dev, (WGPUBindGroupDescriptor*)Unsafe.AsPointer(ref gbgd));
						_d.AddCachedBg((nint)_d.GradBgl, gc.Uniform, ubuf, gbg);
					}
					else { gbg = Bg(ref gbgd, owned); }
				}
				var gq = new float[12];
				void GV(int idx, Vector2 pos) { var n = Ndc(pos); gq[idx] = n.X; gq[idx + 1] = n.Y; }
				GV(0, gc.P0); GV(2, gc.P1); GV(4, gc.P2); GV(6, gc.P0); GV(8, gc.P2); GV(10, gc.P3);
				ops.Add(new DrawOp(3, (nint)gbg, 0, (nint)Vbuf(gq, owned), false, gc.Clip, (nint)MakeClipBg(_d.GradClipBgl, gc.Clip, owned)));
				break;
			}
			case RoundedRectCmd rrc:
			{
				// Legacy per-op fallback (b0=1). The common path routes rrects through the shared per-pass buffer
				// (b0==0) for cross-visual coalescing; this stays for any non-frame-solid cached recording.
				var tmp = RentRrect();
				AppendRrect(tmp, rrc);
				var buf = Vbuf(tmp, owned);
				ReturnRrect(tmp);
				ops.Add(new DrawOp(5, (nint)buf, 6, 0, false, rrc.Clip, (nint)MakeClipBg(_d.RrClipBgl, rrc.Clip, owned)));
				break;
			}
		}
	}

	// Applies the in-pass path-clip transition to the shared depth buffer (all draws recorded into the open `pass`):
	// restore the previous clip's region to depth=0, then write the new clip's mask (depth=0 kept / else clipped)
	// via stencil-then-cover over its bbox. Content depth-tests GreaterEqual against it. No offscreen, no resolve.
	private void ApplyDepthClip(IntPtr pass, float[] prevFan, Vector4 prevAabb, ClipData next)
	{
		// Restore the previous path clip's region to depth=0 (no clip) so its mask doesn't leak past its bbox.
		if (prevFan is not null && TryScissor(prevAabb, out var px, out var py, out var pw, out var ph))
		{
			wgpuRenderPassEncoderSetScissorRect(pass, (uint)px, (uint)py, (uint)pw, (uint)ph);
			wgpuRenderPassEncoderSetPipeline(pass, _d.ClipDepthSet0);
			wgpuRenderPassEncoderDraw(pass, 3, 1, 0, 0);
			WebGpuTrace.Draw("clipdepth-set0(restore-prev)", 3);
		}
		if (next.PathFan is not { } fan || !TryScissor(next.Aabb, out var nx, out var ny, out var nw, out var nh))
		{
			return;
		}
		wgpuRenderPassEncoderSetScissorRect(pass, (uint)nx, (uint)ny, (uint)nw, (uint)nh);
		var excl = next.PathExclude;
		// 1) fill the bbox with the "clipped" depth (intersect: 1 = clipped outside the shape; exclude: 0).
		wgpuRenderPassEncoderSetPipeline(pass, excl ? _d.ClipDepthSet0 : _d.ClipDepthSet1);
		wgpuRenderPassEncoderDraw(pass, 3, 1, 0, 0);
		WebGpuTrace.Draw(excl ? "clipdepth-set0(fill)" : "clipdepth-set1(fill)", 3);
		// 2) stencil the clip fan (winding) in full-window NDC.
		IntPtr fanBuf; int fanVerts;
		if (next.FanBuf != 0 && next.FanW == (int)_s.Width && next.FanH == (int)_s.Height) { fanBuf = (IntPtr)next.FanBuf; fanVerts = fan.Length / 2; }
		else { _scratch.Clear(); for (int i = 0; i < fan.Length; i += 2) { var n = Ndc(new Vector2(fan[i], fan[i + 1])); _scratch.Add(n.X); _scratch.Add(n.Y); } fanBuf = MakeBuffer(_scratch); fanVerts = _scratch.Count / 2; }
		wgpuRenderPassEncoderSetPipeline(pass, next.PathEvenOdd ? _d.StencilEvenOdd : _d.StencilNonZero);
		wgpuRenderPassEncoderSetBindGroup(pass, 0, MakeClipBg(_d.ClipBgl, default), 0, (uint*)null);   // identity xform (clip fan already NDC)
		wgpuRenderPassEncoderSetVertexBuffer(pass, 0, fanBuf, 0, (nuint)(fanVerts * 2 * sizeof(float)));
		wgpuRenderPassEncoderDraw(pass, (uint)fanVerts, 1, 0, 0);
		WebGpuTrace.Draw(next.PathEvenOdd ? "clip-stencil-eo" : "clip-stencil-nz", (uint)(_scratch.Count / 2));
		// 3) cover: write the "kept" depth (intersect: 0 inside the shape; exclude: 1) where the stencil is set,
		// and reset the stencil to 0 (PassOp=Zero) so the next fill/clip starts clean.
		wgpuRenderPassEncoderSetPipeline(pass, excl ? _d.ClipDepthCover1 : _d.ClipDepthCover0);
		wgpuRenderPassEncoderSetStencilReference(pass, 0);
		wgpuRenderPassEncoderDraw(pass, 3, 1, 0, 0);
		WebGpuTrace.Draw(excl ? "clipdepth-cover1" : "clipdepth-cover0", 3);
	}

	// Gap-4 solid-scroll eligibility: a frame-solid recording whose SESSION clip is plain-AABB/None and whose commands
	// are only solids/rrects/path-fills with no path CHILD-clip. Images/gradients (no transform-table pipe) and path
	// child-clips (a depth mask, not restampable) keep the device rebuild path; a rounded/path session clip too.
	private static bool TableFrameEligible(ReplayRefCmd rr)
	{
		if (rr.Clip.PathFan is not null || (rr.Clip.Rounds?.Length ?? 0) != 0) { return false; }
		var cmds = rr.Commands;
		for (int i = 0; i < cmds.Count; i++)
		{
			var c = cmds[i];
			if (c is not (RectCommand or RoundedRectCmd or PathFill) || c.Clip.PathFan is not null) { return false; }
		}
		return true;
	}

	// Maps a table-frame-solid op's LOCAL clip to the current replay transform: clipCov gets finv (device fragment ->
	// local space) so a rounded child-clip stays correct after the move; the device SCISSOR follows the move (the local
	// AABB transformed by t2) with the plain-AABB session clip folded in. Table verts carry their own slot for position,
	// so ClipU.xform is unused here — only finv matters. Mirrors the arena stamp.
	private (ClipData Scissor, nint ClipBg) StampTableClip(ClipData local, OwnedResources stampOwned, Matrix3x2 finv, Matrix3x2 t2, Vector4 sessionAabb)
	{
		var scissor = local;
		scissor.PathFan = null;
		var ab = local.Aabb;
		if (ab.X > -1e8f || ab.Y > -1e8f || ab.Z < 1e8f || ab.W < 1e8f)
		{
			var p0 = new Vector2(ab.X * t2.M11 + ab.Y * t2.M21 + t2.M31, ab.X * t2.M12 + ab.Y * t2.M22 + t2.M32);
			var p1 = new Vector2(ab.Z * t2.M11 + ab.Y * t2.M21 + t2.M31, ab.Z * t2.M12 + ab.Y * t2.M22 + t2.M32);
			var p2 = new Vector2(ab.Z * t2.M11 + ab.W * t2.M21 + t2.M31, ab.Z * t2.M12 + ab.W * t2.M22 + t2.M32);
			var p3 = new Vector2(ab.X * t2.M11 + ab.W * t2.M21 + t2.M31, ab.X * t2.M12 + ab.W * t2.M22 + t2.M32);
			ab = new Vector4(
				MathF.Min(MathF.Min(p0.X, p1.X), MathF.Min(p2.X, p3.X)), MathF.Min(MathF.Min(p0.Y, p1.Y), MathF.Min(p2.Y, p3.Y)),
				MathF.Max(MathF.Max(p0.X, p1.X), MathF.Max(p2.X, p3.X)), MathF.Max(MathF.Max(p0.Y, p1.Y), MathF.Max(p2.Y, p3.Y)));
		}
		scissor.Aabb = new Vector4(MathF.Max(ab.X, sessionAabb.X), MathF.Max(ab.Y, sessionAabb.Y), MathF.Min(ab.Z, sessionAabb.Z), MathF.Min(ab.W, sessionAabb.W));
		return (scissor, (nint)MakeClipBg(_d.ClipBgl, local, stampOwned, Matrix3x2.Identity, finv));
	}

	// Transform-table frame-solid emit (gap-4 solid-scroll redo). Builds the recording's solids/rrects/path-fills ONCE
	// in identity (local) device space + one shared per-vertex slot, resident in the shared TABLE slabs; each frame a
	// MOVE rewrites only that slot (WriteXform) and re-stamps the per-op clips — no re-tessellation, no vertex re-Put.
	// The absolute slab offset is re-derived every frame (TryByteOffset, else re-Put) so a recording that scrolled out
	// (its slice culled + reclaimed) then back in never draws from a stale offset — the reverted attempt's crash.
	private void EmitTableFrameSolid(ReplayRefCmd rr, WebGpuGeometryCache feCur, List<DrawOp> ops)
	{
		var fe = feCur;
		bool hit = fe is { TableFrame: true, FrameOrder: not null };
		if (!hit)
		{
			if (fe is not null) { _d.DeferRelease(fe.Owned); _d.DeferRelease(fe.StampOwned); }
			var fOwned = new OwnedResources();
			var sv = new List<float>(); var rv = new List<float>(); var order = new List<FrameOp>();
			var tmp = new List<DrawOp>();
			var tcmds = WebGpuCommandRecorder.TransformFor(rr.Commands, Matrix4x4.Identity, ClipData.None);
			// One stable slot shared by ALL of this recording's geometry (solids/rrects/path-fills): its local->NDC
			// affine folds the replay transform + projection, rewritten per frame, so a move repositions everything.
			int slot = (fe is not null && fe.XformSlot >= 0) ? fe.XformSlot : _d.AllocXformSlot();
			float slotBits = System.BitConverter.Int32BitsToSingle(slot);
			for (int ti = 0; ti < tcmds.Count; ti++)
			{
				var tc = tcmds[ti];
				if (tc is RectCommand rc0)
				{
					// Coalesce a run of consecutive same-clip rects into one contiguous LOCAL-vert range + one draw.
					int rel = sv.Count / 7; int tj = ti;
					while (tj < tcmds.Count && tcmds[tj] is RectCommand rcj && ClipDataEquals(rcj.Clip, rc0.Clip))
					{
						AppendSolidRectLocalT(sv, rcj.P0, rcj.P1, rcj.P2, rcj.P3, rcj.Color.R / 255f, rcj.Color.G / 255f, rcj.Color.B / 255f, rcj.Color.A / 255f, slotBits);
						tj++;
					}
					order.Add(new FrameOp { Kind = 0, ByteOff = rel * 7 * sizeof(float), Count = (uint)((tj - ti) * 6), Clip = rc0.Clip });
					ti = tj - 1;
				}
				else if (tc is RoundedRectCmd rr0)
				{
					int rel = rv.Count / 23; int tj = ti;
					while (tj < tcmds.Count && tcmds[tj] is RoundedRectCmd rrj && ClipDataEquals(rrj.Clip, rr0.Clip))
					{
						AppendRrectLocalT(rv, rrj, slotBits);
						tj++;
					}
					order.Add(new FrameOp { Kind = 5, ByteOff = rel * 23 * sizeof(float), Count = (uint)((tj - ti) * 6), Clip = rr0.Clip });
					ti = tj - 1;
				}
				else
				{
					// Path fills (glyphs/icons): local device fan/cover + the shared slot, residentized so the fan/cover
					// buffers upload once. The move repositions them via the slot; clipCov uses the per-frame finv stamp.
					tmp.Clear();
					BuildSimpleOp(tc, tmp, fOwned, slot);
					foreach (var o in tmp) { order.Add(new FrameOp { Kind = -1, NonSolid = ResidentizeFan(o, fOwned) }); }
				}
			}
			long id = (fe is not null && fe.SlabId != 0) ? fe.SlabId : _d.NextSlabId();
			fe = new WebGpuGeometryCache { TableFrame = true, FrameSolid = true, SlabId = id, FrameOrder = order, TableSolids = sv, TableRrects = rv, Owned = fOwned, Transform = rr.Transform, Clip = rr.Clip, Device = _d, BuiltW = (int)_s.Width, BuiltH = (int)_s.Height, XformSlot = slot };
			rr.Data.Compiled = fe;
			WebGpuTrace.Upload("geometry-build(table-frame-solid)", order.Count);
		}
		else { WebGpuTrace.Upload("geometry-reuse(table-frame-solid)", 0); }
		// Re-derive the CURRENT slab byte offset of this recording's slices every frame: reuse the resident slice when it
		// survived last frame (no upload), else re-Put its UNCHANGED local verts (the slice was culled + its offset
		// reclaimed). NEVER a cached absolute offset — that stale-offset-into-a-reclaimed-slice was the crash the redo fixes.
		int sBase = 0, rBase = 0;
		if (fe.TableSolids.Count > 0 && !_d.SolidTableSlab.TryByteOffset(fe.SlabId, out sBase)) { sBase = _d.SolidTableSlab.Put(fe.SlabId, fe.TableSolids); }
		if (fe.TableRrects.Count > 0 && !_d.RrectTableSlab.TryByteOffset(fe.SlabId, out rBase)) { rBase = _d.RrectTableSlab.Put(fe.SlabId, fe.TableRrects); }
		// The single slot's affine = replay transform folded with the current projection: a move/resize is this one write.
		WriteXform(fe.XformSlot, rr.Transform);
		// Re-stamp per-op device scissors + clip bind groups only when transform / session clip / surface size changed
		// (memoized like the arena stamp); a STATIC table recording reuses them verbatim. The slab base is applied below.
		if (!fe.HasStamp || fe.StampXform != rr.Transform || !ClipDataEquals(fe.StampClip, rr.Clip) || fe.StampW != (int)_s.Width || fe.StampH != (int)_s.Height)
		{
			if (fe.StampOwned is not null) { _d.DeferRelease(fe.StampOwned); }
			var stampOwned = new OwnedResources();
			var t2 = new Matrix3x2(rr.Transform.M11, rr.Transform.M12, rr.Transform.M21, rr.Transform.M22, rr.Transform.M41, rr.Transform.M42);
			Matrix3x2 finv = Matrix3x2.Invert(t2, out var inv) ? inv : Matrix3x2.Identity;
			var sessionAabb = rr.Clip.Aabb;
			var stamps = new List<(ClipData, nint)>(fe.FrameOrder.Count);
			foreach (var fo in fe.FrameOrder)
			{
				var local = fo.Kind == -1 ? fo.NonSolid.clip : fo.Clip;
				stamps.Add(StampTableClip(local, stampOwned, finv, t2, sessionAabb));
			}
			fe.StampOwned = stampOwned; fe.StampClips = stamps; fe.StampXform = rr.Transform; fe.StampClip = rr.Clip; fe.StampW = (int)_s.Width; fe.StampH = (int)_s.Height; fe.HasStamp = true;
		}
		// Emit from the resident slabs (b0=2 => table slab) with the per-frame base + the memoized stamped clip.
		for (int i = 0; i < fe.FrameOrder.Count; i++)
		{
			var fo = fe.FrameOrder[i]; var (sc, bg) = fe.StampClips[i];
			if (fo.Kind == 0) { ops.Add(new DrawOp(0, 2, fo.Count, (nint)(sBase + fo.ByteOff), false, sc, bg)); }
			else if (fo.Kind == 5) { ops.Add(new DrawOp(5, 2, fo.Count, (nint)(rBase + fo.ByteOff), false, sc, bg)); }
			else { var op = fo.NonSolid; ops.Add(new DrawOp(op.kind, op.b0, op.u0, op.b1, op.flag, sc, bg)); }
		}
	}

	// Renders a command list into a target surface's MSAA pass (resolving to its single-sample view). Layers
	// recurse into their own full-size surface then composite here; shadows/layers pre-render before the pass.
	private void RenderInto(List<WebGpuCommand> cmds, WebGpuRenderSurface target, WColor? clear, bool load = false)
	{

		// Build GPU resources for every command up front (buffers/textures must be created outside the
		// render pass), preserving draw order in a single op list so cross-type z-order is honoured.
		// kind: 0=rect (b0=verts OR b0=0 => shared solid buffer at b1=startVert/u0=count), 1=path (b0=fan, u0=fanCount,
		// b1=cover, flag=evenOdd), 2=image (b0=bindGroup, b1=quad), 3=gradient, 4=composite layer.
		var ops = RentOps();
		var solid = RentSolid();
		var rrect = RentRrect();
		// Per-pass transform table (path fills). Saved/restored around the recursive nested-layer RenderInto so each
		// pass builds and uploads its own. Transient (immediate-draw) slots are collected here and freed at pass end.
		var savedXforms = _xforms; var savedTransient = _xformTransient;
		_xforms = RentXforms(); _xforms.Clear();
		_xformTransient = RentTransient(); _xformTransient.Clear();
		// Recordings emitted so far in THIS pass. A recording replayed more than once in one frame (same command
		// list at different transforms) can't share its single resident slab slice — see the frame-solid branch.
		var frameEmitted = new HashSet<List<WebGpuCommand>>(System.Collections.Generic.ReferenceEqualityComparer.Instance);
		// Backdrops deferred to encode-time pass-segmenting (kind-6 op): each samples the framebuffer resolved SO FAR
		// (content behind it) instead of re-rendering the whole command prefix here — O(n) vs the old O(n^2).
		var backdrops = new List<BackdropCmd>();
		for (int ci = 0; ci < cmds.Count; ci++)
		{
			var cmd = cmds[ci];
			switch (cmd)
			{
				case RectCommand rc0:
				{
					// Coalesce a run of consecutive rects sharing the same clip into the shared solid buffer + one op.
					// b0==0 marks a shared-buffer solid (b1=start vertex, u0=vertex count) so adjacent solid ops that
					// share a clip bind group coalesce further ACROSS recordings in the emit loop.
					int j = ci; int start = solid.Count / 6;
					while (j < cmds.Count && cmds[j] is RectCommand rcj && ClipDataEquals(rcj.Clip, rc0.Clip))
					{
						AppendSolidRect(solid, rcj.P0, rcj.P1, rcj.P2, rcj.P3, rcj.Color.R / 255f, rcj.Color.G / 255f, rcj.Color.B / 255f, rcj.Color.A / 255f);
						j++;
					}
					ops.Add(new DrawOp(0, 0, (uint)((j - ci) * 6), (nint)start, false, rc0.Clip, (nint)MakeClipBg(_d.SolidClipBgl, rc0.Clip)));
					ci = j - 1;   // the for-loop's ci++ advances past the run
					break;
				}
				case PathFill:
					BuildSimpleOp(cmd, ops, null, AllocTransientPathSlot());   // pooled (per-frame); transient table slot
					break;
				case ImageCmd:
				case GradientCmd:
					BuildSimpleOp(cmd, ops, null, -1);   // pooled (per-frame)
					break;
				case RoundedRectCmd rri:
				{
					// Shared rrect buffer (b0==0, b1=start vert): adjacent same-clip rrects coalesce in the emit loop.
					int st = rrect.Count / 22;
					AppendRrect(rrect, rri);
					ops.Add(new DrawOp(5, 0, 6, (nint)st, false, rri.Clip, (nint)MakeClipBg(_d.RrClipBgl, rri.Clip)));
					break;
				}
				case ReplayRefCmd rr:
				{
					// FRAME-SOLID path (ramez arena baseline): any recording that contains rects — a Border background,
					// a Button (background + border + glyphs) — re-emits its SOLIDS into the SHARED per-pass buffer
					// every frame so sibling visuals sharing a clip collapse to ONE draw (the cross-visual draw-count
					// win the profiler showed). NON-solids (glyphs/images/gradients) stay cached (device space,
					// rebuilt only on a transform/clip change) and are consumed in draw order as the recording is
					// re-walked. Pure non-solid recordings fall through to the arena path below (moving-visual reuse).
						if (HasReappendable(rr.Commands))
						{
							// A recording replayed MORE THAN ONCE in a single frame (same command list, different
							// transforms) can't reuse one resident slab slice — the second build's Put would overwrite
							// the first. Repeat emissions get a fresh transient slice (freed next frame); the first
							// emission keeps the recording's stable, resident slice.
							bool repeat = !frameEmitted.Add(rr.Commands);
							// Gap-4 solid-scroll: an eligible recording (plain-AABB/None session clip, only solids/rrects/
							// path-fills, no path child-clip, no images/gradients) restamps via the transform table on a MOVE
							// — identity verts resident in the shared table slabs + a per-vertex slot, so a scroll rewrites
							// the slot, not the verts, while siblings still coalesce. A repeat emission (same list twice this
							// frame) can't share one resident slice, so it takes the device path below.
							if (!repeat && TableFrameEligible(rr))
							{
								EmitTableFrameSolid(rr, rr.Data.Compiled, ops);
								break;
							}
							WebGpuGeometryCache fe = null;
							bool fMiss, fStale;
							if (repeat) { fMiss = true; fStale = false; }
							else
							{
								fe = rr.Data.Compiled;
									fMiss = fe is null;
								fStale = !fMiss && (!fe.FrameSolid || fe.TableFrame || fe.FrameOrder is null || fe.Transform != rr.Transform || fe.BuiltW != (int)_s.Width || fe.BuiltH != (int)_s.Height || !ClipDataEquals(fe.Clip, rr.Clip));
							}
							if (fMiss || fStale)
							{
								// Build once: extract device-space solid/rrect verts + an ordered emit list; owned (persistent) clip
								// bind groups so nothing is re-created per frame.
								if (fe is not null) { _d.DeferRelease(fe.Owned); }
								var fOwned = new OwnedResources();
								var sv = new List<float>(); var rv = new List<float>(); var order = new List<FrameOp>();
								var tmp = new List<DrawOp>();
								var tcmds = new List<WebGpuCommand>();
								foreach (var tc in WebGpuCommandRecorder.TransformFor(rr.Commands, rr.Transform, rr.Clip)) { tcmds.Add(tc); }
								// One stable transform-table slot for this recording's device-space path fills (reused on
								// rebuild; transient for a repeat emission). Verts are final-device here, so the slot's entry
								// is the pure device->NDC projection, rewritten per frame at emit.
								bool fHasPath = false; foreach (var c in tcmds) { if (c is PathFill) { fHasPath = true; break; } }
								int fSlot = fHasPath ? ((!repeat && fe is not null && fe.XformSlot >= 0) ? fe.XformSlot : _d.AllocXformSlot()) : -1;
								if (fHasPath && repeat) { _xformTransient.Add(fSlot); }
								for (int ti = 0; ti < tcmds.Count; ti++)
								{
									var tc = tcmds[ti];
									if (tc is RectCommand rc0)
									{
										// Coalesce a run of consecutive same-clip rects into one contiguous range + one draw.
										int rel = sv.Count / 6; int tj = ti;
										while (tj < tcmds.Count && tcmds[tj] is RectCommand rcj && ClipDataEquals(rcj.Clip, rc0.Clip))
										{
											AppendSolidRect(sv, rcj.P0, rcj.P1, rcj.P2, rcj.P3, rcj.Color.R / 255f, rcj.Color.G / 255f, rcj.Color.B / 255f, rcj.Color.A / 255f);
											tj++;
										}
										order.Add(new FrameOp { Kind = 0, ByteOff = rel * 6 * sizeof(float), Count = (uint)((tj - ti) * 6), Clip = rc0.Clip, ClipBg = (nint)MakeClipBg(_d.SolidClipBgl, rc0.Clip, fOwned) });
										ti = tj - 1;
									}
									else if (tc is RoundedRectCmd rr0)
									{
										int rel = rv.Count / 22; int tj = ti;
										while (tj < tcmds.Count && tcmds[tj] is RoundedRectCmd rrj && ClipDataEquals(rrj.Clip, rr0.Clip))
										{
											AppendRrect(rv, rrj);
											tj++;
										}
										order.Add(new FrameOp { Kind = 5, ByteOff = rel * 22 * sizeof(float), Count = (uint)((tj - ti) * 6), Clip = rr0.Clip, ClipBg = (nint)MakeClipBg(_d.RrClipBgl, rr0.Clip, fOwned) });
										ti = tj - 1;
									}
									else
									{
										tmp.Clear();
										BuildSimpleOp(tc, tmp, fOwned, fSlot);
										foreach (var o in tmp) { order.Add(new FrameOp { Kind = -1, NonSolid = ResidentizeFan(o, fOwned) }); }
									}
								}
								// Write the extracted verts into this recording's STABLE slices in the SHARED slabs (uploads
								// only the changed bytes; grows the slab buffer 1.5x when needed) and rebase the ordered
								// ops to absolute slab byte offsets. `id` is stable across frames so a static recording's
								// slice is resident (no re-upload) and coalesces with neighbours across recordings.
								long id = repeat ? _d.NextSlabId()
									: ((fMiss || fe is null || fe.SlabId == 0) ? _d.NextSlabId() : fe.SlabId);
								int sBase = sv.Count > 0 ? _d.SolidSlab.Put(id, sv) : 0;
								int rBase = rv.Count > 0 ? _d.RrectSlab.Put(id, rv) : 0;
								for (int oi2 = 0; oi2 < order.Count; oi2++)
								{
									var fo = order[oi2];
									if (fo.Kind == 0) { fo.ByteOff += sBase; order[oi2] = fo; }
									else if (fo.Kind == 5) { fo.ByteOff += rBase; order[oi2] = fo; }
								}
								fe = new WebGpuGeometryCache { FrameSolid = true, SlabId = id, FrameOrder = order, Owned = fOwned, Transform = rr.Transform, Clip = rr.Clip, Device = _d, BuiltW = (int)_s.Width, BuiltH = (int)_s.Height, XformSlot = fSlot };
								// A repeat emission is not cached (its slice is transient); free its bind groups next frame.
								if (repeat) { _d.DeferRelease(fOwned); }
								else { fe.Device = _d; rr.Data.Compiled = fe; }
								WebGpuTrace.Upload("geometry-build(frame-solid)", order.Count);
							}
							else { WebGpuTrace.Upload("geometry-reuse(frame-solid)", 0); _d.SolidSlab.MarkLive(fe.SlabId); _d.RrectSlab.MarkLive(fe.SlabId); }
							// Rewrite this recording's path-fill transform entry every frame (device verts => pure current
							// projection), so a window resize repositions its glyphs via the table with no re-tessellation.
							if (fe.XformSlot >= 0) { WriteXform(fe.XformSlot, Matrix4x4.Identity); }
							// Per frame: re-emit ops drawing from the RESIDENT shared slabs (b0=1 => solid slab / rrect slab;
							// b1 = absolute slab byte offset). No append, no upload, no re-tessellation on a cache hit.
							foreach (var fo in fe.FrameOrder)
							{
								if (fo.Kind == 0) { ops.Add(new DrawOp(0, 1, fo.Count, (nint)fo.ByteOff, false, fo.Clip, fo.ClipBg)); }
								else if (fo.Kind == 5) { ops.Add(new DrawOp(5, 1, fo.Count, (nint)fo.ByteOff, false, fo.Clip, fo.ClipBg)); }
								else { ops.Add(fo.NonSolid); }
							}
							break;
						}
					// The per-visual GPU-geometry cache (slab/scroll), keyed by the recording's immutable command
					// list. Build once; reuse while it's replayed at the same transform/clip. A stale entry (moved
					// visual) is deferred-released and rebuilt. Entries not referenced any frame are evicted.
					var entry = rr.Data.Compiled;
						var miss = entry is null;
						// ARENA (#22): a transform-safe recording (solid/image, no clip) bakes its geometry ONCE in its
						// own identity NDC space; a moved replay re-stamps the vertex xform on the per-op clip bind groups
						// and REUSES the vertex buffers instead of rebuilding. Moving-visual trace: moved frame => reuse.
						if (rr.Clip.IsNone && IsArenaSafe(rr.Commands))
						{
							// Stable path-fill transform slot: arena verts are in the recording's OWN (identity) space, so
							// the slot's entry folds the replay transform + projection — written per frame below, so a
							// move OR resize repositions the fan/cover via the table with no re-stamp and no re-bake.
							int aSlot = (miss || entry is null) ? -1 : entry.XformSlot;
							// A pure-path arena entry is surface-size-independent (device verts + table), so a resize is
							// handled by the per-frame table write below with NO rebuild; a mixed entry's NDC-baked solids
							// still force a size rebuild.
							bool aSizeChanged = entry is not null && (entry.BuiltW != (int)_s.Width || entry.BuiltH != (int)_s.Height);
							if (miss || !entry.Arena || (aSizeChanged && !entry.PurePath))
							{
								if (entry is not null) { _d.DeferRelease(entry.Owned); _d.DeferRelease(entry.StampOwned); }
								var aOwned = new OwnedResources();
								var aOps = new List<DrawOp>();
								var aList = new List<WebGpuCommand>();
								foreach (var tc in WebGpuCommandRecorder.TransformFor(rr.Commands, Matrix4x4.Identity, ClipData.None)) { aList.Add(tc); }
								bool aHasPath = false, aPure = aList.Count > 0; foreach (var c in aList) { if (c is PathFill) { aHasPath = true; } else { aPure = false; } }
								if (aHasPath && aSlot < 0) { aSlot = _d.AllocXformSlot(); }
								BuildCoalesced(aList, aOps, aOwned, aSlot);
									for (int _ri = 0; _ri < aOps.Count; _ri++) { aOps[_ri] = ResidentizeFan(aOps[_ri], aOwned); }
								entry = new WebGpuGeometryCache { Ops = aOps, Owned = aOwned, Transform = rr.Transform, Clip = rr.Clip, Arena = true, PurePath = aPure, Device = _d, BuiltW = (int)_s.Width, BuiltH = (int)_s.Height, XformSlot = aSlot };
								rr.Data.Compiled = entry;
								WebGpuTrace.Upload("geometry-build(new,arena)", aOps.Count);
							}
							else { WebGpuTrace.Upload("geometry-reuse(cache-hit)", 0); }
							// Per frame (even on a cache/stamp hit): the identity-space verts map to the current replay
							// transform + surface projection via this one table entry — the whole arena move/resize path.
							if (entry.XformSlot >= 0) { WriteXform(entry.XformSlot, rr.Transform); }
							if (!entry.HasStamp || entry.StampXform != rr.Transform)
							{
							if (entry.StampOwned is not null) { _d.DeferRelease(entry.StampOwned); }
							var stampOwned = new OwnedResources();
							var stamped = new List<DrawOp>(entry.Ops.Count);
							var xf = ArenaXform(rr.Transform);
							// finv = inverse device affine, so clipCov maps the moved fragment back to the recording's
							// own space where the (identity-baked) clip lives.
							var t2 = new Matrix3x2(rr.Transform.M11, rr.Transform.M12, rr.Transform.M21, rr.Transform.M22, rr.Transform.M41, rr.Transform.M42);
							Matrix3x2 finv = Matrix3x2.Invert(t2, out var inv) ? inv : Matrix3x2.Identity;
							Vector2 MoveP(float x, float y) => new(x * t2.M11 + y * t2.M21 + t2.M31, x * t2.M12 + y * t2.M22 + t2.M32);
							foreach (var op in entry.Ops)
							{
								var abgl = op.kind switch { 3 => _d.GradClipBgl, 2 => _d.ImageClipBgl, _ => _d.SolidClipBgl };
								// clipCov reads the LOCAL rounded shape (finv maps fc back to it); the SCISSOR is device-space
								// so its Aabb must follow the move — transform the (finite) clip Aabb by the replay transform.
								var scissorClip = op.clip;
								var ab = op.clip.Aabb;
								if (ab.X > -1e8f || ab.Y > -1e8f || ab.Z < 1e8f || ab.W < 1e8f)
								{
									var p0 = MoveP(ab.X, ab.Y); var p1 = MoveP(ab.Z, ab.Y); var p2 = MoveP(ab.Z, ab.W); var p3 = MoveP(ab.X, ab.W);
									scissorClip.Aabb = new Vector4(
										MathF.Min(MathF.Min(p0.X, p1.X), MathF.Min(p2.X, p3.X)), MathF.Min(MathF.Min(p0.Y, p1.Y), MathF.Min(p2.Y, p3.Y)),
										MathF.Max(MathF.Max(p0.X, p1.X), MathF.Max(p2.X, p3.X)), MathF.Max(MathF.Max(p0.Y, p1.Y), MathF.Max(p2.Y, p3.Y)));
								}
								var aClipBg = MakeClipBg(abgl, op.clip, stampOwned, xf, finv);
								stamped.Add(new DrawOp(op.kind, op.b0, op.u0, op.b1, op.flag, scissorClip, (nint)aClipBg));
							}
							entry.StampOwned = stampOwned; entry.StampedOps = stamped; entry.StampXform = rr.Transform; entry.HasStamp = true;
							}
							ops.AddRange(entry.StampedOps);
							break;
						}
						var transformChanged = !miss && entry.Transform != rr.Transform;
						int cSlot = (miss || entry is null) ? -1 : entry.XformSlot;
						if (miss || transformChanged || entry.Arena || entry.BuiltW != (int)_s.Width || entry.BuiltH != (int)_s.Height || !ClipDataEquals(entry.Clip, rr.Clip))
						{
							if (entry is not null) { _d.DeferRelease(entry.Owned); }
							var owned = new OwnedResources();
							var cachedOps = new List<DrawOp>();
							var cList = new List<WebGpuCommand>();
							foreach (var tc in WebGpuCommandRecorder.TransformFor(rr.Commands, rr.Transform, rr.Clip)) { cList.Add(tc); }
							bool cHasPath = false; foreach (var c in cList) { if (c is PathFill) { cHasPath = true; break; } }
							if (cHasPath && cSlot < 0) { cSlot = _d.AllocXformSlot(); }
							BuildCoalesced(cList, cachedOps, owned, cSlot);
							for (int _ri = 0; _ri < cachedOps.Count; _ri++) { cachedOps[_ri] = ResidentizeFan(cachedOps[_ri], owned); }
							entry = new WebGpuGeometryCache { Ops = cachedOps, Owned = owned, Transform = rr.Transform, Clip = rr.Clip, Device = _d, BuiltW = (int)_s.Width, BuiltH = (int)_s.Height, XformSlot = cSlot };
							rr.Data.Compiled = entry;
							// Rebuild signal: transform-only changes SHOULD become a uniform re-stamp under arena (#22),
							// not a full geometry rebuild — this UPLOAD line is what a moved-visual multi-frame trace watches.
							WebGpuTrace.Upload(transformChanged ? "geometry-rebuild(transform-changed)" : "geometry-build(new)", cachedOps.Count);
						}
						else { WebGpuTrace.Upload("geometry-reuse(cache-hit)", 0); }
						// Device-space verts => the slot's entry is the pure current projection (rewritten per frame so a
						// resize repositions the path fills via the table without re-baking).
						if (entry.XformSlot >= 0) { WriteXform(entry.XformSlot, Matrix4x4.Identity); }
					// Splice the cached draw-ops straight into this frame's op list — replayed by direct encoding in
					// the main pass, NOT a render bundle (ExecuteBundles measured ~6x slower on wgpu-native, and forces
					// a scissor reset; direct replay keeps each op's scissor). Buffers/bind groups persist in `owned`.
					ops.AddRange(entry.Ops);
					break;
				}
				case ShadowCmd sh:
				{
					// Render the blurred coverage offscreen, then composite it as a SrcIn-tinted image (tint =
					// shadow color) at its device placement — reusing the image draw path (kind 2), incl. clip.
					var blurView = RenderShadow(sh, out var origin, out var size);
					var ubuf = MakeUniform((int)112);
					var op = stackalloc float[8];
					op[0] = 1f; op[1] = 1f; op[2] = 0; op[3] = 0;
					op[4] = sh.Color.R / 255f; op[5] = sh.Color.G / 255f; op[6] = sh.Color.B / 255f; op[7] = sh.Color.A / 255f;
					wgpuQueueWriteBuffer(_d.Q, ubuf, 0, (IntPtr)op, 32);
					var sentries = stackalloc WGPUBindGroupEntry[3];
					sentries[0] = new WGPUBindGroupEntry { Binding = 0, TextureView = blurView };
					sentries[1] = new WGPUBindGroupEntry { Binding = 1, Sampler = _d.Smp };
					sentries[2] = new WGPUBindGroupEntry { Binding = 2, Buffer = ubuf, Offset = 0, Size = 112 };
					var sbgd = new WGPUBindGroupDescriptor { Layout = _d.ImgBgl, EntryCount = 3, Entries = sentries };
					var sbg = _d.TrackBg(wgpuDeviceCreateBindGroup(_d.Dev, &sbgd));
					var sq = new float[24];
					void SQV(int idx, Vector2 pos, float u, float vv) { var n = Ndc(pos); sq[idx] = n.X; sq[idx + 1] = n.Y; sq[idx + 2] = u; sq[idx + 3] = vv; }
					var o0 = origin; var o1 = origin + new Vector2(size.X, 0); var o2 = origin + size; var o3 = origin + new Vector2(0, size.Y);
					SQV(0, o0, 0, 0); SQV(4, o1, 1, 0); SQV(8, o2, 1, 1); SQV(12, o0, 0, 0); SQV(16, o2, 1, 1); SQV(20, o3, 0, 1);
					ops.Add(new DrawOp(2, (nint)sbg, 0, (nint)MakeBuffer(sq), false, sh.Clip, (nint)MakeClipBg(_d.ImageClipBgl, sh.Clip)));
					break;
				}
				case LayerCmd lyr:
				{
					// Render the layer's commands into a full-size offscreen surface, then composite (kind 4). Both the
					// offscreen render and this composite record into the frame's single encoder, so wgpu barriers the
					// offscreen resolve before the composite samples it — no explicit flush needed.
					_d.Profiler?.OsLayer();
					var layerSurface = new WebGpuRenderSurface(_d, _s.Width, _s.Height, _d.Pool);
					RenderInto(lyr.Commands, layerSurface, null);

					// SaveLayer(IEffectFilter) drop shadow: blur the content, draw it tinted+offset behind, then
					// the content on top. Reuses the image path (SrcIn tint) for the shadow — same as DrawShadow.
					if (lyr.ShadowEffect is { } fx)
					{
						var blur = BlurPyramid(layerSurface.View, _s.Width, _s.Height, fx.SigmaX, fx.SigmaY);
						var subuf = MakeUniform((int)112);
						var sop = stackalloc float[8];
						sop[0] = 1f; sop[1] = 1f; sop[2] = 0; sop[3] = 0;
						sop[4] = fx.Color.R / 255f; sop[5] = fx.Color.G / 255f; sop[6] = fx.Color.B / 255f; sop[7] = fx.Color.A / 255f;
						wgpuQueueWriteBuffer(_d.Q, subuf, 0, (IntPtr)sop, 32);
						var sfe = stackalloc WGPUBindGroupEntry[3];
						sfe[0] = new WGPUBindGroupEntry { Binding = 0, TextureView = blur };
						sfe[1] = new WGPUBindGroupEntry { Binding = 1, Sampler = _d.Smp };
						sfe[2] = new WGPUBindGroupEntry { Binding = 2, Buffer = subuf, Offset = 0, Size = 112 };
						var sfbgd = new WGPUBindGroupDescriptor { Layout = _d.ImgBgl, EntryCount = 3, Entries = sfe };
						var sfbg = _d.TrackBg(wgpuDeviceCreateBindGroup(_d.Dev, &sfbgd));
						var fq = new float[24];
						void FQV(int idx, Vector2 pos, float u, float vv) { var n = Ndc(pos); fq[idx] = n.X; fq[idx + 1] = n.Y; fq[idx + 2] = u; fq[idx + 3] = vv; }
						var off = new Vector2(fx.Dx, fx.Dy);
						FQV(0, new Vector2(0, 0) + off, 0, 0); FQV(4, new Vector2(_s.Width, 0) + off, 1, 0); FQV(8, new Vector2(_s.Width, _s.Height) + off, 1, 1);
						FQV(12, new Vector2(0, 0) + off, 0, 0); FQV(16, new Vector2(_s.Width, _s.Height) + off, 1, 1); FQV(20, new Vector2(0, _s.Height) + off, 0, 1);
						ops.Add(new DrawOp(2, (nint)sfbg, 0, (nint)MakeBuffer(fq), false, lyr.Clip, (nint)MakeClipBg(_d.ImageClipBgl, lyr.Clip)));
					}

					var cu = new float[24];
					cu[0] = lyr.ColorMatrix is { Length: >= 20 } ? 1f : 0f; cu[1] = 1f;
					if (lyr.ColorMatrix is { Length: >= 20 } mm)
					{
						cu[4] = mm[0]; cu[5] = mm[1]; cu[6] = mm[2]; cu[7] = mm[3];        // m0
						cu[8] = mm[5]; cu[9] = mm[6]; cu[10] = mm[7]; cu[11] = mm[8];      // m1
						cu[12] = mm[10]; cu[13] = mm[11]; cu[14] = mm[12]; cu[15] = mm[13]; // m2
						cu[16] = mm[15]; cu[17] = mm[16]; cu[18] = mm[17]; cu[19] = mm[18]; // m3
						cu[20] = mm[4]; cu[21] = mm[9]; cu[22] = mm[14]; cu[23] = mm[19];   // off (5th column)
					}
					var lubuf = MakeUniform((int)96);
					fixed (float* p = cu) { wgpuQueueWriteBuffer(_d.Q, lubuf, 0, (IntPtr)p, 96); }
					var lentries = stackalloc WGPUBindGroupEntry[3];
					lentries[0] = new WGPUBindGroupEntry { Binding = 0, TextureView = layerSurface.View };
					lentries[1] = new WGPUBindGroupEntry { Binding = 1, Sampler = _d.Smp };
					lentries[2] = new WGPUBindGroupEntry { Binding = 2, Buffer = lubuf, Offset = 0, Size = 96 };
					var lbgd = new WGPUBindGroupDescriptor { Layout = lyr.CompositeMode == 1 ? _d.CompositeDstInBgl : _d.CompositeBgl, EntryCount = 3, Entries = lentries };
					var lbg = _d.TrackBg(wgpuDeviceCreateBindGroup(_d.Dev, &lbgd));
					ops.Add(new DrawOp(4, (nint)lbg, (uint)lyr.CompositeMode, 0, false, lyr.Clip, 0));
					break;
				}
				case BackdropCmd bk:
				{
					// Defer to encode-time pass-segmenting: a kind-6 marker splits THIS pass here so the backdrop samples the
					// framebuffer RESOLVED SO FAR (the content behind it) in place — no offscreen, no prefix re-render. Works for
					// the on-window target AND pooled layer targets: both store+reload their MSAA across the segment (see the
					// main-pass + kind-6 StoreOp), so an acrylic inside a layer/flyout skips the full-window offscreen the old
					// pooled fallback re-rendered per backdrop, and an empty prefix costs nothing (no separate blurred offscreen).
					int bi = backdrops.Count; backdrops.Add(bk);
					ops.Add(new DrawOp(6, 0, 0, (nint)bi, false, bk.Clip, 0));
					break;
				}
			}
		}

		// Upload the whole pass's coalesceable solid + rrect geometry in ONE buffer each; b0==0 ops index them.
		nint solidBuf = solid.Count > 0 ? (nint)MakeBuffer(solid) : IntPtr.Zero;
		nint rrectBuf = rrect.Count > 0 ? (nint)MakeBuffer(rrect) : IntPtr.Zero;

		// Upload this pass's transform table + one read-only storage bind group (group 0 of the path-fill pipelines).
		// Every drawn path recording wrote its slot's local->NDC affine above; a pass with no path fills skips this.
		nint xformBg = IntPtr.Zero;
		if (_xforms.Count > 0)
		{
			// Main on-window pass: persistent buffer + bind group cached across frames (rebuilt only on growth).
			// Nested/pooled passes rent a transient buffer instead so their distinct tables never alias the main one
			// within a single frame's submit (queue ordering only protects the persistent buffer across frames).
			if (target == _s)
			{
				xformBg = _d.EnsureXformBindGroup(_xforms);
			}
			else
			{
				int xbytes = _xforms.Count * sizeof(float);
				var xbuf = _d.BufferPool.Rent(xbytes, WGPUBufferUsage.Storage | WGPUBufferUsage.CopyDst);
				var xspan = System.Runtime.InteropServices.CollectionsMarshal.AsSpan(_xforms);
				fixed (float* xp = xspan) { wgpuQueueWriteBuffer(_d.Q, xbuf, 0, (IntPtr)xp, (nuint)xbytes); }
				var xe = new WGPUBindGroupEntry { Binding = 0, Buffer = xbuf, Offset = 0, Size = (nuint)xbytes };
				var xbgd = new WGPUBindGroupDescriptor { Layout = _d.XformBgl, EntryCount = 1, Entries = &xe };
				xformBg = (nint)_d.TrackBg(wgpuDeviceCreateBindGroup(_d.Dev, &xbgd));
			}
		}

		var ca = new WGPURenderPassColorAttachment
		{
			// Render into the multisampled color and resolve into the single-sample target texture.
			// A fresh MSAA buffer can't LoadOp.Load, so we always clear (transparent when no clear was given);
			// the neutral loop redraws the whole frame each present, so nothing prior needs preserving here.
			// The resolve into target.View happens regardless of StoreOp; StoreOp.Discard drops the MSAA samples
			// afterwards (never sampled) to save the store bandwidth — target.View (sampled later) is unaffected.
			DepthSlice = uint.MaxValue,
			// 1x: render straight into the single-sample View (no resolve target), and Store it (it IS the result).
			// MSAA store: the resolved target.View is all any later consumer (blit, backdrop sample) reads, so the
			// multisampled buffer is Discarded after resolve — EXCEPT when a case-6 backdrop will segment this pass
			// (it ends + reopens with LoadOp.Load, which requires the samples were Stored). The overlay is inlined
			// into this same pass (see Dispose), so there is no follow-up load pass to keep the samples alive for.
			View = target.MsaaColorView, ResolveTarget = _d.MsaaSamples > 1 ? target.View : IntPtr.Zero, LoadOp = load ? WGPULoadOp.Load : WGPULoadOp.Clear, StoreOp = (_d.MsaaSamples > 1 && backdrops.Count == 0) ? WGPUStoreOp.Discard : WGPUStoreOp.Store,
			ClearValue = clear.HasValue ? new WGPUColor { R = clear.Value.R / 255.0, G = clear.Value.G / 255.0, B = clear.Value.B / 255.0, A = clear.Value.A / 255.0 } : default,
		};
		var dsa = new WGPURenderPassDepthStencilAttachment
		{
			View = target.DepthView,
			DepthLoadOp = WGPULoadOp.Clear, DepthStoreOp = WGPUStoreOp.Discard, DepthClearValue = 0f,
			StencilLoadOp = WGPULoadOp.Clear, StencilStoreOp = WGPUStoreOp.Discard, StencilClearValue = 0,
		};
		var rp = new WGPURenderPassDescriptor { ColorAttachmentCount = 1, ColorAttachments = &ca, DepthStencilAttachment = &dsa };
		var pass = wgpuCommandEncoderBeginRenderPass(_frameEncoder, &rp);
		WebGpuTrace.Pass(target.Pooled ? "offscreen" : "main", target.Width, target.Height, _d.MsaaSamples, true);

		// Track the last-applied scissor and skip redundant SetScissorRect calls: static chrome draws many ops under
		// one clip, so this collapses a per-op call to one per distinct clip. Locals (not a field) keep it correct
		// under the recursive nested-layer RenderInto (each pass has its own scissor state).
		if (!target.Pooled) { _d.Profiler?.Ops(ops.Count); }
		int lastX = -1, lastY = -1, lastW = -1, lastH = -1;
		// Current in-pass path-clip mask (device depth buffer). Changes only when a run of ops moves to a different
		// path clip — the composition emits a clip then its subtree consecutively, so this fires ~once per clip.
		float[] curFan = null; Vector4 curAabb = default;
		for (int oi = 0; oi < ops.Count; oi++)
		{
			var (kind, b0, u0, b1, flag, clip, clipBg) = ops[oi];
			if (!ReferenceEquals(clip.PathFan, curFan))
			{
				ApplyDepthClip(pass, curFan, curAabb, clip);
				curFan = clip.PathFan; curAabb = clip.Aabb;
				lastX = lastY = lastW = lastH = -1;   // the clip setup changed the scissor
			}
			if (!TryScissor(clip.Aabb, out var sx, out var sy, out var sw, out var sh)) { continue; }
			_d.Profiler?.Draw();
			if (sx != lastX || sy != lastY || sw != lastW || sh != lastH)
			{
				wgpuRenderPassEncoderSetScissorRect(pass, (uint)sx, (uint)sy, (uint)sw, (uint)sh);
				lastX = sx; lastY = sy; lastW = sw; lastH = sh;
			}
			switch (kind)
			{
				case 0 when b0 == 0:
				{
					// Shared-buffer solid (b1=start vertex, u0=vertex count). COALESCE the maximal run of following
					// solid ops sharing this clip bind group + clip (same scissor + depth-clip): their verts are
					// contiguous in the shared buffer by construction, so the whole run draws in ONE call.
					int startVert = (int)b1; uint count = u0;
					while (oi + 1 < ops.Count)
					{
						var nx = ops[oi + 1];
						if (nx.kind != 0 || nx.b0 != 0 || nx.clipBg != clipBg
							|| !ReferenceEquals(nx.clip.PathFan, clip.PathFan) || nx.clip.Aabb != clip.Aabb) { break; }
						count += nx.u0; oi++; _d.Profiler?.Coalesced(1);
					}
					wgpuRenderPassEncoderSetPipeline(pass, _d.SolidPipe);
					wgpuRenderPassEncoderSetBindGroup(pass, 0, (IntPtr)clipBg, 0, (uint*)null);
					wgpuRenderPassEncoderSetVertexBuffer(pass, 0, solidBuf, (nuint)(startVert * 6 * sizeof(float)), (nuint)(count * 6 * sizeof(float)));
					wgpuRenderPassEncoderDraw(pass, count, 1, 0, 0);
					WebGpuTrace.Draw("solid", count);
					_d.Profiler?.DrawKind(0);
					break;
				}
				case 0 when b0 == 1:
				{
					// Resident SOLID SLAB (b1 = absolute byte offset). Coalesce a byte-contiguous run sharing clip+bindgroup.
					int byteOff = (int)b1; uint count = u0;
					while (oi + 1 < ops.Count)
					{
						var nx = ops[oi + 1];
						if (nx.kind != 0 || nx.b0 != 1 || nx.clipBg != clipBg || !ReferenceEquals(nx.clip.PathFan, clip.PathFan)
							|| nx.clip.Aabb != clip.Aabb || (int)nx.b1 != byteOff + (int)(count * 6 * sizeof(float))) { break; }
						count += nx.u0; oi++; _d.Profiler?.Coalesced(1);
					}
					wgpuRenderPassEncoderSetPipeline(pass, _d.SolidPipe);
					wgpuRenderPassEncoderSetBindGroup(pass, 0, (IntPtr)clipBg, 0, (uint*)null);
					wgpuRenderPassEncoderSetVertexBuffer(pass, 0, _d.SolidSlab.Buf, (nuint)byteOff, (nuint)(count * 6 * sizeof(float)));
					wgpuRenderPassEncoderDraw(pass, count, 1, 0, 0);
					WebGpuTrace.Draw("solid", count);
					_d.Profiler?.DrawKind(0);
					break;
				}
				case 0 when b0 == 2:
				{
					// Resident SOLID TABLE SLAB (b1 = absolute byte offset, stride 7 = pos+col+slot). Group 0 = the
					// transform table (each vertex's slot positions it), group 1 = ClipU. Coalesce byte-contiguous
					// same-clip runs ACROSS recordings — each vertex still carries its own slot, so one draw is correct.
					int byteOff = (int)b1; uint count = u0;
					while (oi + 1 < ops.Count)
					{
						var nx = ops[oi + 1];
						if (nx.kind != 0 || nx.b0 != 2 || nx.clipBg != clipBg || !ReferenceEquals(nx.clip.PathFan, clip.PathFan)
							|| nx.clip.Aabb != clip.Aabb || (int)nx.b1 != byteOff + (int)(count * 7 * sizeof(float))) { break; }
						count += nx.u0; oi++; _d.Profiler?.Coalesced(1);
					}
					wgpuRenderPassEncoderSetPipeline(pass, _d.SolidTablePipe);
					wgpuRenderPassEncoderSetBindGroup(pass, 0, (IntPtr)xformBg, 0, (uint*)null);
					wgpuRenderPassEncoderSetBindGroup(pass, 1, (IntPtr)clipBg, 0, (uint*)null);
					wgpuRenderPassEncoderSetVertexBuffer(pass, 0, _d.SolidTableSlab.Buf, (nuint)byteOff, (nuint)(count * 7 * sizeof(float)));
					wgpuRenderPassEncoderDraw(pass, count, 1, 0, 0);
					WebGpuTrace.Draw("solid-table", count);
					_d.Profiler?.DrawKind(0);
					break;
				}
				case 0:
					// b0 = vertex buffer (private/immediate or a resident frame-solid buffer); b1 = byte offset into it.
					wgpuRenderPassEncoderSetPipeline(pass, _d.SolidPipe);
					wgpuRenderPassEncoderSetBindGroup(pass, 0, (IntPtr)clipBg, 0, (uint*)null);
					wgpuRenderPassEncoderSetVertexBuffer(pass, 0, (IntPtr)b0, (nuint)b1, (nuint)(u0 * 6 * sizeof(float)));
					wgpuRenderPassEncoderDraw(pass, u0, 1, 0, 0);   // u0 = 6 * (coalesced) rect count
					WebGpuTrace.Draw("solid", u0);
					_d.Profiler?.DrawKind(0);
					break;
				case 1:
					// Path fill via the transform table: fan verts = device pos + slot index (stride 3); cover verts =
					// device pos + colour + slot index (stride 7). Group 0 = storage table (positions the verts);
					// group 1 (cover) = ClipU (analytic clip coverage). Table entries were written during op-build.
					wgpuRenderPassEncoderSetPipeline(pass, flag ? _d.StencilTableEO : _d.StencilTableNZ);
					wgpuRenderPassEncoderSetBindGroup(pass, 0, (IntPtr)xformBg, 0, (uint*)null);
					wgpuRenderPassEncoderSetVertexBuffer(pass, 0, (IntPtr)b0, 0, (nuint)(u0 * 3 * sizeof(float)));
					wgpuRenderPassEncoderDraw(pass, u0, 1, 0, 0);
					WebGpuTrace.Draw(flag ? "path-stencil-eo" : "path-stencil-nz", u0);
					wgpuRenderPassEncoderSetPipeline(pass, _d.CoverTablePipe);
					wgpuRenderPassEncoderSetBindGroup(pass, 0, (IntPtr)xformBg, 0, (uint*)null);
					wgpuRenderPassEncoderSetBindGroup(pass, 1, (IntPtr)clipBg, 0, (uint*)null);
					wgpuRenderPassEncoderSetStencilReference(pass, 0);
					wgpuRenderPassEncoderSetVertexBuffer(pass, 0, (IntPtr)b1, 0, (nuint)(42 * sizeof(float)));
					wgpuRenderPassEncoderDraw(pass, 6, 1, 0, 0);
					WebGpuTrace.Draw("path-cover", 6);
					_d.Profiler?.DrawKind(1);
					break;
				case 2:
					wgpuRenderPassEncoderSetPipeline(pass, _d.ImagePipe);
					wgpuRenderPassEncoderSetBindGroup(pass, 0, (IntPtr)b0, 0, (uint*)null);
					wgpuRenderPassEncoderSetBindGroup(pass, 1, (IntPtr)clipBg, 0, (uint*)null);
					wgpuRenderPassEncoderSetVertexBuffer(pass, 0, (IntPtr)b1, 0, (nuint)(24 * sizeof(float)));
					wgpuRenderPassEncoderDraw(pass, 6, 1, 0, 0);
					WebGpuTrace.Draw("image", 6);
					_d.Profiler?.DrawKind(2);
					break;
				case 3:
					wgpuRenderPassEncoderSetPipeline(pass, _d.GradientPipe);
					wgpuRenderPassEncoderSetBindGroup(pass, 0, (IntPtr)b0, 0, (uint*)null);
					wgpuRenderPassEncoderSetBindGroup(pass, 1, (IntPtr)clipBg, 0, (uint*)null);
					wgpuRenderPassEncoderSetVertexBuffer(pass, 0, (IntPtr)b1, 0, (nuint)(12 * sizeof(float)));
					wgpuRenderPassEncoderDraw(pass, 6, 1, 0, 0);
					WebGpuTrace.Draw("gradient", 6);
					_d.Profiler?.DrawKind(3);
					break;
				case 4:
					wgpuRenderPassEncoderSetPipeline(pass, u0 == 1 ? _d.CompositeDstIn : _d.CompositeSrcOver);
					wgpuRenderPassEncoderSetBindGroup(pass, 0, (IntPtr)b0, 0, (uint*)null);
					wgpuRenderPassEncoderDraw(pass, 3, 1, 0, 0);
					WebGpuTrace.Draw(u0 == 1 ? "composite-dstin" : "composite-srcover", 3);
					_d.Profiler?.DrawKind(4);
					break;
				case 6:
				{
					// Backdrop pass-segment (acrylic O(n) path): END this segment so its MSAA resolves into target.View
					// (the content BEHIND the backdrop), blur that, REOPEN the pass loading the content back, and
					// composite the blurred backdrop + tint over the effect region. Subsequent ops draw on top in the
					// reopened pass. No prefix re-render — each command is encoded once.
					var bk = backdrops[(int)b1];
					wgpuRenderPassEncoderEnd(pass);
					// Region-limit: blur only the element AABB padded by the blur reach, not the whole framebuffer.
					float sPad = MathF.Max(bk.Effect.SigmaX, bk.Effect.SigmaY) + 8f;
					var sAabb = bk.Clip.Aabb;
					float srx = MathF.Max(0f, sAabb.X - sPad), sry = MathF.Max(0f, sAabb.Y - sPad);
					float srw = MathF.Max(1f, MathF.Min(_s.Width, sAabb.Z + sPad) - srx), srh = MathF.Max(1f, MathF.Min(_s.Height, sAabb.W + sPad) - sry);
					var bblur = BlurPyramidRegion(target.View, _s.Width, _s.Height, srx, sry, srw, srh, bk.Effect.SigmaX, bk.Effect.SigmaY);
					var ca6 = new WGPURenderPassColorAttachment
					{
						DepthSlice = uint.MaxValue,
						View = target.MsaaColorView, ResolveTarget = _d.MsaaSamples > 1 ? target.View : IntPtr.Zero,
						LoadOp = WGPULoadOp.Load, StoreOp = WGPUStoreOp.Store,   // store: a following segment (next backdrop) reloads it; pooled layer targets segment too now
					};
					var dsa6 = new WGPURenderPassDepthStencilAttachment
					{
						View = target.DepthView, DepthLoadOp = WGPULoadOp.Clear, DepthStoreOp = WGPUStoreOp.Discard, DepthClearValue = 0f,
						StencilLoadOp = WGPULoadOp.Clear, StencilStoreOp = WGPUStoreOp.Discard, StencilClearValue = 0,
					};
					var rp6 = new WGPURenderPassDescriptor { ColorAttachmentCount = 1, ColorAttachments = &ca6, DepthStencilAttachment = &dsa6 };
					pass = wgpuCommandEncoderBeginRenderPass(_frameEncoder, &rp6);
					WebGpuTrace.Pass("backdrop-segment", target.Width, target.Height, _d.MsaaSamples, true);
					lastX = lastY = lastW = lastH = -1; curFan = null; curAabb = default;   // fresh pass: reset scissor + clip mask
					if (TryScissor(bk.Clip.Aabb, out var bsx, out var bsy, out var bsw, out var bsh))
					{
						wgpuRenderPassEncoderSetScissorRect(pass, (uint)bsx, (uint)bsy, (uint)bsw, (uint)bsh);
						lastX = bsx; lastY = bsy; lastW = bsw; lastH = bsh;
						// Acrylic composite: blurred backdrop image (lum/noise/opacity baked via the 112B uniform).
						var bubuf = MakeUniform(112);
						var bop = stackalloc float[28]; bop[0] = bk.Opacity; bop[3] = 1f; var lum = bk.Effect.LumColor; bop[4] = lum.R / 255f; bop[5] = lum.G / 255f; bop[6] = lum.B / 255f; bop[7] = lum.A / 255f; bop[24] = bk.Effect.Noise;
						wgpuQueueWriteBuffer(_d.Q, bubuf, 0, (IntPtr)bop, 112);
						var bde = stackalloc WGPUBindGroupEntry[3];
						bde[0] = new WGPUBindGroupEntry { Binding = 0, TextureView = bblur };
						bde[1] = new WGPUBindGroupEntry { Binding = 1, Sampler = _d.Smp };
						bde[2] = new WGPUBindGroupEntry { Binding = 2, Buffer = bubuf, Offset = 0, Size = 112 };
						var bdbgd = new WGPUBindGroupDescriptor { Layout = _d.ImgBgl, EntryCount = 3, Entries = bde };
						var bdbg = _d.TrackBg(wgpuDeviceCreateBindGroup(_d.Dev, &bdbgd));
						var bq = new float[24];
						void BQV(int idx, Vector2 pos, float u, float vv) { var n = Ndc(pos); bq[idx] = n.X; bq[idx + 1] = n.Y; bq[idx + 2] = u; bq[idx + 3] = vv; }
						BQV(0, new Vector2(srx, sry), 0, 0); BQV(4, new Vector2(srx + srw, sry), 1, 0); BQV(8, new Vector2(srx + srw, sry + srh), 1, 1);
						BQV(12, new Vector2(srx, sry), 0, 0); BQV(16, new Vector2(srx + srw, sry + srh), 1, 1); BQV(20, new Vector2(srx, sry + srh), 0, 1);
						var bqbuf = MakeBuffer(bq);
						var bclipBg = MakeClipBg(_d.ImageClipBgl, bk.Clip);
						wgpuRenderPassEncoderSetPipeline(pass, _d.ImagePipe);
						wgpuRenderPassEncoderSetBindGroup(pass, 0, (IntPtr)bdbg, 0, (uint*)null);
						wgpuRenderPassEncoderSetBindGroup(pass, 1, (IntPtr)bclipBg, 0, (uint*)null);
						wgpuRenderPassEncoderSetVertexBuffer(pass, 0, (IntPtr)bqbuf, 0, (nuint)(24 * sizeof(float)));
						wgpuRenderPassEncoderDraw(pass, 6, 1, 0, 0);
						WebGpuTrace.Draw("backdrop", 6);
						// Tint overlay (skip A==0).
						if (bk.Effect.Color.A != 0)
						{
							var col = bk.Effect.Color; var tcx = col.R / 255f; var tcy = col.G / 255f; var tcz = col.B / 255f; var tcw = col.A / 255f;
							var tv = new System.Collections.Generic.List<float>();
							void TV(float x, float y) { var n = Ndc(new Vector2(x, y)); tv.Add(n.X); tv.Add(n.Y); tv.Add(tcx); tv.Add(tcy); tv.Add(tcz); tv.Add(tcw); }
							var a = bk.Clip.Aabb;
							TV(a.X, a.Y); TV(a.Z, a.Y); TV(a.Z, a.W); TV(a.X, a.Y); TV(a.Z, a.W); TV(a.X, a.W);
							var tvbuf = MakeBuffer(tv);
							var tclipBg = MakeClipBg(_d.SolidClipBgl, bk.Clip);
							wgpuRenderPassEncoderSetPipeline(pass, _d.SolidPipe);
							wgpuRenderPassEncoderSetBindGroup(pass, 0, (IntPtr)tclipBg, 0, (uint*)null);
							wgpuRenderPassEncoderSetVertexBuffer(pass, 0, (IntPtr)tvbuf, 0, (nuint)(36 * sizeof(float)));
							wgpuRenderPassEncoderDraw(pass, 6, 1, 0, 0);
							WebGpuTrace.Draw("backdrop-tint", 6);
						}
					}
					_d.Profiler?.OsBackdrop(0);   // segmented: 0 prefix commands re-rendered
					break;
				}
				case 5 when b0 == 0:
				{
					// Shared rrect buffer (b1=start vert, u0=6). COALESCE the run of following rrect ops sharing this
					// clip bind group + clip: their 22-float verts are contiguous, so the run draws in ONE call.
					int startVert = (int)b1; uint count = u0;
					while (oi + 1 < ops.Count)
					{
						var nx = ops[oi + 1];
						if (nx.kind != 5 || nx.b0 != 0 || nx.clipBg != clipBg
							|| !ReferenceEquals(nx.clip.PathFan, clip.PathFan) || nx.clip.Aabb != clip.Aabb) { break; }
						count += nx.u0; oi++; _d.Profiler?.Coalesced(1);
					}
					wgpuRenderPassEncoderSetPipeline(pass, _d.RrPipe);
					wgpuRenderPassEncoderSetBindGroup(pass, 0, (IntPtr)clipBg, 0, (uint*)null);
					wgpuRenderPassEncoderSetVertexBuffer(pass, 0, rrectBuf, (nuint)(startVert * 22 * sizeof(float)), (nuint)(count * 22 * sizeof(float)));
					wgpuRenderPassEncoderDraw(pass, count, 1, 0, 0);
					WebGpuTrace.Draw("rrect", count);
					_d.Profiler?.DrawKind(6);
					break;
				}
				case 5 when b0 == 1:
				{
					// Resident RRECT SLAB (b1 = absolute byte offset). Coalesce byte-contiguous same-clip runs.
					int byteOff = (int)b1; uint count = u0;
					while (oi + 1 < ops.Count)
					{
						var nx = ops[oi + 1];
						if (nx.kind != 5 || nx.b0 != 1 || nx.clipBg != clipBg || !ReferenceEquals(nx.clip.PathFan, clip.PathFan)
							|| nx.clip.Aabb != clip.Aabb || (int)nx.b1 != byteOff + (int)(count * 22 * sizeof(float))) { break; }
						count += nx.u0; oi++; _d.Profiler?.Coalesced(1);
					}
					wgpuRenderPassEncoderSetPipeline(pass, _d.RrPipe);
					wgpuRenderPassEncoderSetBindGroup(pass, 0, (IntPtr)clipBg, 0, (uint*)null);
					wgpuRenderPassEncoderSetVertexBuffer(pass, 0, _d.RrectSlab.Buf, (nuint)byteOff, (nuint)(count * 22 * sizeof(float)));
					wgpuRenderPassEncoderDraw(pass, count, 1, 0, 0);
					WebGpuTrace.Draw("rrect", count);
					_d.Profiler?.DrawKind(6);
					break;
				}
				case 5 when b0 == 2:
				{
					// Resident RRECT TABLE SLAB (b1 = absolute byte offset, stride 23). Group 0 = the transform table
					// (per-vertex slot positions the local corners), group 1 = ClipU. Coalesce byte-contiguous same-clip runs.
					int byteOff = (int)b1; uint count = u0;
					while (oi + 1 < ops.Count)
					{
						var nx = ops[oi + 1];
						if (nx.kind != 5 || nx.b0 != 2 || nx.clipBg != clipBg || !ReferenceEquals(nx.clip.PathFan, clip.PathFan)
							|| nx.clip.Aabb != clip.Aabb || (int)nx.b1 != byteOff + (int)(count * 23 * sizeof(float))) { break; }
						count += nx.u0; oi++; _d.Profiler?.Coalesced(1);
					}
					wgpuRenderPassEncoderSetPipeline(pass, _d.RrTablePipe);
					wgpuRenderPassEncoderSetBindGroup(pass, 0, (IntPtr)xformBg, 0, (uint*)null);
					wgpuRenderPassEncoderSetBindGroup(pass, 1, (IntPtr)clipBg, 0, (uint*)null);
					wgpuRenderPassEncoderSetVertexBuffer(pass, 0, _d.RrectTableSlab.Buf, (nuint)byteOff, (nuint)(count * 23 * sizeof(float)));
					wgpuRenderPassEncoderDraw(pass, count, 1, 0, 0);
					WebGpuTrace.Draw("rrect-table", count);
					_d.Profiler?.DrawKind(6);
					break;
				}
				case 5:
					// b0 = vertex buffer (resident frame-solid or legacy per-op); b1 = byte offset; u0 = vertex count.
					wgpuRenderPassEncoderSetPipeline(pass, _d.RrPipe);
					wgpuRenderPassEncoderSetBindGroup(pass, 0, (IntPtr)clipBg, 0, (uint*)null);
					wgpuRenderPassEncoderSetVertexBuffer(pass, 0, (IntPtr)b0, (nuint)b1, (nuint)(u0 * 22 * sizeof(float)));
					wgpuRenderPassEncoderDraw(pass, u0, 1, 0, 0);
					WebGpuTrace.Draw("rrect", u0);
					_d.Profiler?.DrawKind(6);
					break;
			}
		}

		wgpuRenderPassEncoderEnd(pass);
		WebGpuTrace.PassEnd();
		// A pooled offscreen (layer/backdrop) target: its MSAA colour has resolved into View and the depth is spent,
		// so return both for the next same-size pass to reuse — only View (composited/sampled later) stays live. The
		// on-window/dedicated target owns its MSAA+depth (persistent across frames) and is left untouched.
		if (target.Pooled) { if (_d.MsaaSamples > 1) { _d.Pool.Return(target.MsaaColorView); } _d.Pool.Return(target.DepthView); }   // at 1x MsaaColorView aliases View (sampled later) — don't reclaim
		ReturnOps(ops);   // ops are fully encoded into the pass now — recycle the list
		ReturnSolid(solid);
		ReturnRrect(rrect);
		// Return this pass's transient (immediate-draw) transform slots to the free-list and recycle the table lists,
		// then restore the enclosing pass's table (nested-layer render).
		foreach (var s in _xformTransient) { _d.FreeXformSlot(s); }
		_xforms.Clear(); _xformsPool.Push(_xforms); _xformTransient.Clear(); _xformTransientPool.Push(_xformTransient);
		_xforms = savedXforms; _xformTransient = savedTransient;
	}

	// Immediate-mode drawing forwards to the overlay recorder; Scale/Save/Restore additionally drive the frame's
	// root DPI scale (_presentScale) applied to the replayed frame.
	public Matrix4x4 TotalMatrix => _overlay.TotalMatrix;
	public void SetMatrix(in Matrix4x4 matrix) => _overlay.SetMatrix(matrix);
	public void Concat(in Matrix4x4 matrix) => _overlay.Concat(matrix);
	public void Translate(float dx, float dy) => _overlay.Translate(dx, dy);
	public void Scale(float sx, float sy) { _presentScale = new Vector2(_presentScale.X * sx, _presentScale.Y * sy); _overlay.Scale(sx, sy); }
	public int Save() { _presentScaleStack.Push(_presentScale); _overlay.Save(); return _presentScaleStack.Count; }
	public int SaveCount => _presentScaleStack.Count;
	public object NativeSurface => null;
	public void Restore() { if (_presentScaleStack.Count > 0) { _presentScale = _presentScaleStack.Pop(); } _overlay.Restore(); }
	public void RestoreToCount(int count) { while (_presentScaleStack.Count > count) { Restore(); } }
	public void SaveLayer(bool antialias = false) => _overlay.SaveLayer(antialias);
	public void SaveLayer(IColorFilter colorFilter, bool antialias = false) => _overlay.SaveLayer(colorFilter, antialias);
	public void SaveLayer(BlendMode blendMode, bool antialias = false) => _overlay.SaveLayer(blendMode, antialias);
	public void SaveLayer(IEffectFilter filter) => _overlay.SaveLayer(filter);
	public void ClipRect(in Rect rect, ClipOperation operation = ClipOperation.Intersect, bool antialias = false) => _overlay.ClipRect(rect, operation, antialias);
	public void ClipRoundRect(in RoundRectangle roundRect, ClipOperation operation = ClipOperation.Intersect, bool antialias = false) => _overlay.ClipRoundRect(roundRect, operation, antialias);
	public void ClipPath(IGeometry geometry, ClipOperation operation = ClipOperation.Intersect, bool antialias = false) => _overlay.ClipPath(geometry, operation, antialias);
	public void Clear(WColor color) => _presentClear = color;
	public void DrawRect(in Rect rect, WColor color, bool antialias = false) => _overlay.DrawRect(rect, color, antialias);
	public void DrawRect(in Rect rect, IShader shader, bool antialias = false) => _overlay.DrawRect(rect, shader, antialias);
	public void DrawRoundedRect(in Rect rect, Vector4 radii, WColor color, bool antialias = false) => _overlay.DrawRoundedRect(rect, radii, color, antialias);
	public void DrawRoundedRectBorder(in Rect outer, Vector4 outerRadii, in Rect inner, Vector4 innerRadii, WColor color, bool antialias = false) => _overlay.DrawRoundedRectBorder(outer, outerRadii, inner, innerRadii, color, antialias);
	public void DrawPath(IGeometry geometry, WColor color, bool antialias = false) => _overlay.DrawPath(geometry, color, antialias);
	public void DrawShadow(IGeometry silhouette, WColor color, float sigmaX, float sigmaY, bool additive, bool antialias = false) => _overlay.DrawShadow(silhouette, color, sigmaX, sigmaY, additive, antialias);
	public void StrokePath(IGeometry geometry, WColor color, float strokeWidth, bool antialias = false) => _overlay.StrokePath(geometry, color, strokeWidth, antialias);
	public void DrawLine(Vector2 p0, Vector2 p1, WColor color, float strokeWidth, bool antialias = false) => _overlay.DrawLine(p0, p1, color, strokeWidth, antialias);
	public void DrawImage(ITexture texture, float x, float y, ImageSampling sampling, float opacity = 1f, bool antialias = false) => _overlay.DrawImage(texture, x, y, sampling, opacity, antialias);
	public void DrawImage(ITexture texture, float x, float y, ImageSampling sampling, IColorFilter colorFilter, bool antialias = false) => _overlay.DrawImage(texture, x, y, sampling, colorFilter, antialias);
	public void DrawImageNineSlice(ITexture texture, in Rect centerSlice, in Rect destination, bool centerHollow, bool antialias = false) => _overlay.DrawImageNineSlice(texture, centerSlice, destination, centerHollow, antialias);
	public void DrawEffectBackdrop(IEffectFilter filter, float opacity) => _overlay.DrawEffectBackdrop(filter, opacity);

	// Renders the deferred frame with the immediate-mode overlay (e.g. the diagnostics FPS counter drawn after Replay)
	// appended as final, top-most commands. Doing it in ONE pass — rather than a follow-up LoadOp.Load overlay pass —
	// is what lets the fast path's MSAA target resolve on-tile (StoreOp.Discard) instead of storing every sample every
	// frame. Mirrors the reference, which composites its FPS panel into the draw list as a final image.
	public void Dispose()
	{
		lock (_d.RenderGate)
		{
			if (_pendingCmds is not { } main)
			{
				// No frame was replayed this present (e.g. a transitional frame during an async backend switch).
				return;
			}
			var cmds = main;
			if (_overlay.Finish() is WebGpuRenderRecord od && od.Commands.Count > 0)
			{
				cmds = new List<WebGpuCommand>(main.Count + od.Commands.Count);
				cmds.AddRange(main);
				cmds.AddRange(od.Commands);
			}
			RunFrame(cmds, _pendingClear);
			// Diagnostic (UNO_WEBGPU_TRACE=1): dump the frame's ordered GPU command stream (PASS/DRAW) every 120th
			// frame, so a running scene's stream can be diffed against the reference branch. Off by default.
			if (WebGpuTrace.Enabled && (++_streamDumpFrame % 120) == 1)
			{
				System.Console.Error.WriteLine($"===== WEBGPU-STREAM frame {_streamDumpFrame} =====\n{WebGpuTrace.Dump()}===== end WEBGPU-STREAM =====");
				System.Console.Error.Flush();
			}
			_d.SolidSlab.EndFrame(); _d.RrectSlab.EndFrame();   // free slices of recordings not seen this frame
		_d.SolidTableSlab.EndFrame(); _d.RrectTableSlab.EndFrame();
			_d.Profiler?.Replayed(_tReplayStart);
			_pendingCmds = null;
		}
	}
}

// --- New-SPI pluggable-backend surface (see doc/uno-drawing-backend-abstraction.md) ---

// NOTE: presentation belongs on the HOST graphics context that owns the window swapchain (it implements
// IWebGpuDeviceContext below and drives Acquire/Present); there is deliberately no device-only IGraphicsContext
// here — a device without a window has no surface to present to.

/// <summary>A host graphics context that owns a <see cref="WebGpuDevice"/> (e.g. an on-window swapchain context).
/// Lets <see cref="WebGpuGraphicsProvider"/> obtain the device without naming the platform context type.</summary>
public sealed class WebGpuGraphicsProvider : IGraphicsProvider<IWebGpuDeviceContext>
{
	private static readonly GraphicsContextKind[] _preferred = { GraphicsContextKind.WebGpu };

	public IReadOnlyList<GraphicsContextKind> PreferredContexts => _preferred;

	// Builds the WebGPU render engine from the neutral device context the host created (raw wgpu handles + the
	// host's colour format + MSAA count) — the exact seam a third-party WebGPU backend consumes. No privileged
	// path into the host's internals. Geometry is a separate seam (GeometryFactory): WebGPU flattens everything, so
	// a SkiaSharp-free app registers a ManagedGeometryFactory there rather than injecting it here.
	public IDrawingFactory CreateGraphics(IWebGpuDeviceContext context)
		=> new WebGpuDrawingFactory(new WebGpuDevice(context));
}

/// <summary>
/// The WebGPU "GPU-API half" (renderer-agnostic): builds an on-window WebGPU swapchain context (surface + device)
/// from a host's <em>raw</em> native handles, so a host can create a WebGpu context for the <see cref="GraphicsContextKind.WebGpu"/>
/// kind by calling one of these entry points — without referencing the WebGPU <em>renderer</em>. The returned
/// context exposes its device via <see cref="IWebGpuDeviceContext"/>, consumed by <see cref="WebGpuGraphicsProvider"/>
/// (or a user's own WebGPU-rendering <see cref="IGraphicsProvider"/>).
/// </summary>
public sealed unsafe class WebGpuTexture : ITexture
{
	private readonly WebGpuDevice _d;
	private readonly IImage _source; // set when uploaded from a CPU IImage; null for an adopted offscreen texture
	public IntPtr Tex;
	public IntPtr View;

	public int PixelWidth { get; }
	public int PixelHeight { get; }

	// Cross-backend fallback only (a foreign backend drawing this texture). When uploaded from an IImage, defer to
	// it; for an adopted offscreen texture (no CPU source) do a blocking GPU readback — off-browser only, since the
	// matched WebGPU backend never calls this (it samples View directly), and browser readback is async elsewhere.
	public void CopyPixels(Span<byte> destination)
	{
		if (_source is { } s)
		{
			s.CopyPixels(destination);
			return;
		}

		var bytes = _d.ReadPixelsFromTex(Tex, PixelWidth, PixelHeight);
		int n = Math.Min(bytes.Length, destination.Length);
		if (_d.ColorFormat == WGPUTextureFormat.BGRA8Unorm)
		{
			bytes.AsSpan(0, n).CopyTo(destination);
		}
		else
		{
			for (int i = 0; i + 3 < n; i += 4) { destination[i] = bytes[i + 2]; destination[i + 1] = bytes[i + 1]; destination[i + 2] = bytes[i]; destination[i + 3] = bytes[i + 3]; }
		}
	}

	// Adopts an already-rendered offscreen texture (from RenderOffscreen) as a sampleable, disposable handle —
	// no upload, no readback. Deferred release is shared with the upload path (refcount + DisposeRequested).
	internal WebGpuTexture(WebGpuDevice device, IntPtr tex, IntPtr view, int width, int height)
	{
		_d = device;
		_source = null;
		Tex = tex;
		View = view;
		PixelWidth = width;
		PixelHeight = height;
	}

	internal WebGpuTexture(WebGpuDevice device, IImage image)
	{
		_d = device;
		_source = image;
		int w = image.PixelWidth, h = image.PixelHeight;
		PixelWidth = w; PixelHeight = h;
		// A zero-sized source (e.g. an image brush whose surface isn't ready yet) would create an empty wgpu
		// texture whose view is a null/"empty" handle, which fails bind-group validation. Fall back to a 1x1
		// transparent texture so the draw is a no-op instead of a hard wgpu panic.
		if (w <= 0 || h <= 0)
		{
			var td0 = new WGPUTextureDescriptor { Size = new WGPUExtent3D { Width = 1, Height = 1, DepthOrArrayLayers = 1 }, Format = WGPUTextureFormat.RGBA8Unorm, MipLevelCount = 1, SampleCount = 1, Dimension = WGPUTextureDimension._2D, Usage = WGPUTextureUsage.TextureBinding | WGPUTextureUsage.CopyDst };
			Tex = wgpuDeviceCreateTexture(device.Dev, &td0);
			View = wgpuTextureCreateView(Tex, null);
			var transparent = new byte[4];
			var dst0 = new WGPUTexelCopyTextureInfo { Texture = Tex, Aspect = WGPUTextureAspect.All, MipLevel = 0, Origin = default };
			var layout0 = new WGPUTexelCopyBufferLayout { BytesPerRow = 4, RowsPerImage = 1 };
			var ext0 = new WGPUExtent3D { Width = 1, Height = 1, DepthOrArrayLayers = 1 };
			fixed (byte* p0 = transparent) { wgpuQueueWriteTexture(device.Q, &dst0, (IntPtr)p0, 4, &layout0, &ext0); }
			return;
		}
		var bgra = new byte[w * h * 4];
		image.CopyPixels(bgra);
		var rgba = new byte[w * h * 4];
		for (int i = 0; i < bgra.Length; i += 4) { rgba[i] = bgra[i + 2]; rgba[i + 1] = bgra[i + 1]; rgba[i + 2] = bgra[i]; rgba[i + 3] = bgra[i + 3]; }
		var td = new WGPUTextureDescriptor { Size = new WGPUExtent3D { Width = (uint)w, Height = (uint)h, DepthOrArrayLayers = 1 }, Format = WGPUTextureFormat.RGBA8Unorm, MipLevelCount = 1, SampleCount = 1, Dimension = WGPUTextureDimension._2D, Usage = WGPUTextureUsage.TextureBinding | WGPUTextureUsage.CopyDst | WGPUTextureUsage.CopySrc };
		WebGpuDevice.TexLog("ImageTexture.upload", (uint)w, (uint)h, 1);
		Tex = wgpuDeviceCreateTexture(device.Dev, &td);
		View = wgpuTextureCreateView(Tex, null);
		var dst = new WGPUTexelCopyTextureInfo { Texture = Tex, Aspect = WGPUTextureAspect.All, MipLevel = 0, Origin = default };
		var layout = new WGPUTexelCopyBufferLayout { BytesPerRow = (uint)(w * 4), RowsPerImage = (uint)h };
		var ext = new WGPUExtent3D { Width = (uint)w, Height = (uint)h, DepthOrArrayLayers = 1 };
		fixed (byte* p = rgba) { wgpuQueueWriteTexture(device.Q, &dst, (IntPtr)p, (nuint)rgba.Length, &layout, &ext); }
	}

	// A transient image texture (e.g. CompositionNineGridBrush, or any per-frame-changing image) is disposed by the
	// composition right after recording its draw, but the recorded ImageCmd captures the raw view HANDLE and the WebGPU
	// draw is compiled/replayed later at present — possibly from an OUTER frame recording whose ReplayRef still holds the
	// (disposed) content recording's command list. So the view must outlive EVERY recording that references it, not just
	// the innermost one. We reference-count: each recording that records or nests this texture holds a ref (AddRef); the
	// GPU resources are freed only once the composition has disposed the texture (DisposeRequested) AND the last
	// referencing recording has released it (refcount 0). Mirrors SkiaSharp's SKPicture refcounting the SKImage it
	// captured across nested pictures. Resident surface-owned textures are never Dispose()d, so they are never freed here.
	private readonly object _lifetimeGate = new();
	private int _refCount;
	private bool _freed;
	internal bool DisposeRequested { get; private set; }

	// Taken by every WebGpuRenderRecord that records this texture directly or nests a recording that references it.
	internal void AddRef()
	{
		lock (_lifetimeGate) { _refCount++; }
	}

	// The composition (e.g. CompositionImageSurface swapping to a new frame) is done with the CPU image. Marks intent;
	// the GPU free is withheld until no recording references the captured view any more.
	public void Dispose()
	{
		lock (_lifetimeGate) { DisposeRequested = true; TryFree(); }
	}

	// Balances one AddRef: called by WebGpuRenderRecord.Dispose for each reference it took.
	internal void Release()
	{
		lock (_lifetimeGate) { _refCount--; TryFree(); }
	}

	// Enqueues the deferred GPU free exactly once, when both conditions hold: the composition has disposed the texture
	// and no recording references it. The actual view/texture release happens at the next BeginFrameResources (drained
	// under RenderGate, after the last present's submit), like the per-frame bind groups/buffers.
	private void TryFree()
	{
		if (!_freed && DisposeRequested && _refCount <= 0)
		{
			_freed = true;
			if (View != IntPtr.Zero || Tex != IntPtr.Zero) { _d.DeferTextureRelease(View, Tex); View = IntPtr.Zero; Tex = IntPtr.Zero; }
		}
	}
}

/// <summary>A managed <see cref="IImage"/> over a WebGPU offscreen readback. The readback bytes are in the
/// device's color format (RGBA for the offscreen device, BGRA for a swapchain device); <see cref="CopyPixels"/>
/// yields BGRA per the seam's image convention, swapping R/B only when the source is RGBA. No Skia.</summary>
internal sealed class WebGpuReadbackImage : IImage
{
	private readonly byte[] _bytes;
	private readonly bool _sourceIsBgra;
	public WebGpuReadbackImage(int width, int height, byte[] bytes, bool sourceIsBgra) { PixelWidth = width; PixelHeight = height; _bytes = bytes; _sourceIsBgra = sourceIsBgra; }
	public int PixelWidth { get; }
	public int PixelHeight { get; }
	public void CopyPixels(Span<byte> destination)
	{
		int n = Math.Min(_bytes.Length, destination.Length);
		if (_sourceIsBgra) { _bytes.AsSpan(0, n).CopyTo(destination); return; }
		for (int i = 0; i + 3 < n; i += 4) { destination[i] = _bytes[i + 2]; destination[i + 1] = _bytes[i + 1]; destination[i + 2] = _bytes[i]; destination[i + 3] = _bytes[i + 3]; }
	}
}

/// <summary>
/// The device-bound WebGPU resource factory: textures, gradient shaders, color filters, the drop-shadow /
/// backdrop-blur effect, and offscreen rasterization are all WebGPU-owned. Geometry, font resolution/shaping and
/// image decode are separate backend-independent seams (<see cref="GeometryFactory"/> / <see cref="FontProvider"/>
/// / <see cref="ImageEncoderDecoder"/>); WebGPU consumes the neutral <see cref="IGeometry"/> it's registered by flattening
/// it, so a SkiaSharp-free app registers a <see cref="ManagedGeometryFactory"/> and links zero SkiaSharp for drawing.
/// </summary>
public sealed class WebGpuDrawingFactory : IDrawingFactory<IWebGpuRenderTarget>
{
	private readonly WebGpuDevice _device;
	// The main-pass surface the backend OWNS: the host hands only a single-sample resolve colour (the neutral
	// IWebGpuRenderTarget); the backend allocates its own MSAA colour + depth (recreated on resize) and resolves
	// into the host's colour — the same "backend brings its own depth/stencil" contract every other target follows.
	private WebGpuRenderSurface _mainSurface;
	private int _mainW, _mainH;

	internal WebGpuDrawingFactory(WebGpuDevice device) { _device = device; }

	public ICommandRecorder CreateRecording() => new WebGpuCommandRecorder();


	public IPresentSession BeginPresent(IWebGpuRenderTarget target)
	{
		if (_mainSurface is null || _mainW != target.Width || _mainH != target.Height)
		{
			_mainSurface?.Dispose();
			_mainSurface = new WebGpuRenderSurface(_device, target.Width, target.Height, externalColor: true);
			_mainW = target.Width;
			_mainH = target.Height;
		}
		// Point the backend surface at THIS frame's host resolve colour view (host owns its lifetime; _ownsColor=false,
		// and the underlying texture stays host-side — the render pass only needs the view).
		_mainSurface.View = target.ColorView;
		if (_device.MsaaSamples == 1) { _mainSurface.MsaaColorView = target.ColorView; }   // 1x: render straight into it
		return new WebGpuPresentSession(_device, _mainSurface);
	}

	public ITexture CreateTexture(IImage image) => new WebGpuTexture(_device, image);

	// Offscreen rasterization on the WebGPU device (record → present into a dedicated offscreen surface) and hand
	// back the resolved color texture as a sampleable ITexture — no CPU read-back, so a nine-slice/glyph/SVG
	// consumer draws it straight. CPU pixels (RenderTargetBitmap) come from SnapshotAsync instead.
	public ITexture RenderOffscreen(int pixelWidth, int pixelHeight, Action<IDrawingSession> render)
	{
		var recorder = new WebGpuCommandRecorder();
		render(recorder);
		var surface = new WebGpuRenderSurface(_device, pixelWidth, pixelHeight);
		var present = new WebGpuPresentSession(_device, surface);
		present.ReplayNested(recorder.Finish());   // encodes + submits the nested render into the surface's color texture
		// Take ownership of the resolved color texture; dispose releases only the (finished) MSAA + depth targets.
		var (tex, view) = surface.DetachColor();
		surface.Dispose();
		return new WebGpuTexture(_device, tex, view, pixelWidth, pixelHeight);
	}

	// GPU→CPU read of a texture produced by this factory. Off-browser a native thread drives the map (blocking);
	// on the browser the map must run off the JS event loop, so the copy is encoded here and mapped in JS.
	public async System.Threading.Tasks.Task<IImage> SnapshotAsync(ITexture texture)
	{
		if (texture is not WebGpuTexture t)
		{
			throw new ArgumentException("Texture was not produced by WebGpuDrawingFactory.", nameof(texture));
		}

		int w = t.PixelWidth, h = t.PixelHeight;
		bool srcBgra = _device.ColorFormat == WGPUTextureFormat.BGRA8Unorm;
		if (!OperatingSystem.IsBrowser())
		{
			return new WebGpuReadbackImage(w, h, _device.ReadPixelsFromTex(t.Tex, w, h), srcBgra);
		}

		_device.EncodeCopyTexToReadbackBuffer(t.Tex, w, h, out var buf, out var total, out var padded);
		// Browser GPU→CPU map must run off the JS event loop; the JS bridge lives in the host init assembly.
		var paddedBytes = Convert.FromBase64String(await WebGpuJsInterop.MapReadBase64Async((int)buf, total));
		_device.DestroyBuffer(buf);
		return new WebGpuReadbackImage(w, h, WebGpuDevice.Unpad(paddedBytes, w, h, padded), srcBgra);
	}
	public IShader CreateLinearGradientShader(Vector2 start, Vector2 end, WColor[] colors, float[] colorPositions, GradientTileMode tileMode, System.Numerics.Matrix3x2 localMatrix)
		=> new WebGpuShader { Radial = false, P0 = start, P1 = end, Colors = colors, Stops = colorPositions, TileMode = tileMode, LocalMatrix = localMatrix };
	public IShader CreateRadialGradientShader(Vector2 center, Vector2 gradientOrigin, float radiusX, float radiusY, WColor[] colors, float[] colorPositions, GradientTileMode tileMode, System.Numerics.Matrix3x2 localMatrix)
		=> new WebGpuShader { Radial = true, P0 = center, P1 = gradientOrigin, RadiusX = radiusX, RadiusY = radiusY, Colors = colors, Stops = colorPositions, TileMode = tileMode, LocalMatrix = localMatrix };
	public IColorFilter CreateBlendModeColorFilter(WColor color, BlendMode mode) => new WebGpuColorFilter { IsBlendMode = true, Color = color, Mode = mode };
	public IColorFilter CreateColorMatrixColorFilter(float[] matrix) => new WebGpuColorFilter { Matrix = matrix };
	public IEffectFilter CreateDropShadowFilter(float dx, float dy, float sigmaX, float sigmaY, WColor color) => new WebGpuEffectFilter { Dx = dx, Dy = dy, SigmaX = sigmaX, SigmaY = sigmaY, Color = color };

	// Fuses the neutral EffectNode tree (Uno's parser output) into a backend filter. Only realizes the acrylic shape
	// (a gaussian-blurred backdrop + tint/luminosity colours); any other tree returns null so CompositionEffectBrush
	// falls back to the recipe path. Structure-matches the acrylic graph: the outer Blend's ColorInput foreground is
	// the tint, the inner Blend's is the luminosity colour.
	public IEffectFilter CreateEffectFilter(EffectNode tree, Rect bounds)
	{
		float sigma = 0f;
		WColor tint = default, lum = default;
		bool sawColorSource = false;
		bool sawBackdrop = false;

		void Walk(EffectNode node)
		{
			switch (node)
			{
				case SourceInput:
					sawBackdrop = true;
					break;
				case BlurEffectNode blur:
					sigma = MathF.Max(sigma, blur.Sigma);
					Walk(blur.Source);
					break;
				case BlendEffectNode blend:
					if (blend.Foreground is ColorInput colorInput)
					{
						sawColorSource = true;
						if (blend.Background is BlendEffectNode) { tint = colorInput.Color; } else { lum = colorInput.Color; }
					}
					Walk(blend.Background);
					Walk(blend.Foreground);
					break;
				default:
					foreach (var child in node.Children) { Walk(child); }
					break;
			}
		}

		Walk(tree);

		if ((sigma > 0f || sawColorSource) && sawBackdrop)
		{
			return new WebGpuEffectFilter { SigmaX = sigma, SigmaY = sigma, Color = tint, LumColor = lum, Noise = 0.02f };
		}

		return null;
	}
}
