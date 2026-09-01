// The encode-side shapes: what one GPU draw is (DrawOp), where its verts live (VertexSource), the geometry a
// recording has cached on the GPU, and the state a pass-encode carries as it walks its ops.
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

// Persistent (non-pooled) GPU resources for a cached recording, released on eviction. Separate from the per-frame
// pool so cached draws survive across frames.
/// <summary>
/// Sentinel <see cref="DrawOp.b0"/> values for solid and rounded-rect ops whose verts live in a buffer shared with
/// other ops instead of one of their own. Any other <c>b0</c> is the op's own vertex buffer handle.
/// </summary>
internal static class VertexSource
{
	/// <summary>Per-pass buffer, rebuilt each frame. <c>b1</c> = first vertex; stride 6 floats.</summary>
	public const int PassBuffer = 0;

	/// <summary>Resident slab that survives across frames. <c>b1</c> = byte offset; stride 6 floats.</summary>
	public const int Slab = 1;

	/// <summary>
	/// Resident slab whose verts each carry an xform-table slot, so a move rewrites the slot rather than the
	/// geometry. <c>b1</c> = byte offset; stride 7 floats (pos + colour + slot).
	/// </summary>
	public const int TableSlab = 2;
}

/// <summary>What a <see cref="DrawOp"/> draws, and so which pipeline and vertex layout it is encoded with.</summary>
internal enum DrawKind
{
	/// <summary>Axis-aligned rect. b0 = verts, or 0 to take the shared per-pass solid buffer at b1/u0.</summary>
	Solid = 0,

	/// <summary>Arbitrary path, stencil-then-cover. b0 = fan, u0 = fan vertex count, b1 = cover, flag = even-odd.</summary>
	Path = 1,

	/// <summary>Textured quad. b0 = bind group, b1 = quad verts.</summary>
	Image = 2,

	/// <summary>Gradient-filled geometry.</summary>
	Gradient = 3,

	/// <summary>Composites a layer's offscreen surface onto its parent.</summary>
	CompositeLayer = 4,

	/// <summary>Analytic rounded rect / border ring (one SDF quad, no tessellation).</summary>
	RoundedRect = 5,

	/// <summary>Ends the pass segment so a backdrop can sample what is already drawn, then reopens it.</summary>
	BackdropSegment = 6,

	/// <summary>Path whose verts carry an xform-table slot, so a move rewrites the slot and not the geometry.</summary>
	TablePath = 7,

	/// <summary>Fan that tiles without overlap, so it fills in ONE pass with no stencil (see PathFill.FanTiles).</summary>
	TilingFan = 8,
}

internal struct DrawOp
{
	public DrawKind kind; public nint b0; public uint u0; public nint b1; public bool flag; public ClipData clip; public nint clipBg;
	public uint Color; public int GlyphFanStart;
	public DrawOp(DrawKind kind, nint b0, uint u0, nint b1, bool flag, ClipData clip, nint clipBg)
	{
		this.kind = kind; this.b0 = b0; this.u0 = u0; this.b1 = b1; this.flag = flag; this.clip = clip; this.clipBg = clipBg;
		Color = 0; GlyphFanStart = -1;
	}
	public readonly void Deconstruct(out DrawKind kind, out nint b0, out uint u0, out nint b1, out bool flag, out ClipData clip, out nint clipBg)
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
	// This entry emitted atlas quads. Their coverage masks were baked at unit scale in the recording's own space,
	// so a replay at any other scale would sample a wrong-sized mask — the replay guard rebuilds instead.
	public bool HasAtlas;
	// This entry holds path fills that WOULD have been atlased but were built while the replay transform was
	// scaling or rotating. Ops are cached, so without a rebuild once the transform settles it keeps the aliased
	// geometry path forever — which is what left text built during a navigation transition permanently aliased.
	public bool AtlasBlockedByScale;
	// The replay scale this entry's masks were baked at. A different scale needs different masks, so it forces a
	// rebuild rather than sampling one sized for the old scale.
	public Vector2 AtlasScale;
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
	// Resident device-space verts for a (non-table) frame-solid recording, kept so a pure reuse can re-derive its
	// CURRENT slab byte offset every frame (TryByteOffset, else re-Put) instead of trusting a cached absolute — a
	// stale offset into a culled-then-reclaimed slice draws one visual's geometry under another's. FrameOrder
	// offsets are RELATIVE to these lists; the absolute base is applied at emit. Solid = 6 floats/v, rrect = 22.
	public List<float> FrameSolidVerts;
	public List<float> FrameRrectVerts;
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
	// ClipU buffer handles parallel to StampClips/StampedOps: a restamp rewrites these in place (bind groups kept)
	// instead of allocating a fresh bag - see StampFrame for the same-submit guard.
	public List<nint> StampBufs;
	public long StampFrame;
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
	/// <summary>Solid or RoundedRect for a run in the shared slabs; null when this op is <see cref="NonSolid"/>.</summary>
	public DrawKind? Kind;
	public int ByteOff;       // byte offset of this run within its shared slab (solid/rrect)
	public uint Count;        // vertex count (solid/rrect)
	public ClipData Clip;
	public nint ClipBg;
	public DrawOp NonSolid;
}

/// <summary>
/// Where a pass encodes its draws - the render pass itself, or a render bundle while a cache-eligible chunk is
/// being recorded - skipping redundant pipeline / bind-group / vertex-buffer sets. A run of like ops otherwise
/// costs five native calls each where two suffice.
/// <para>
/// Encoder state is per-pass AND per-bundle, so <see cref="Reset"/> must run at EVERY boundary: pass open or
/// reopen, bundle begin and end, and after any site that sets state directly instead of through these methods.
/// </para>
/// </summary>
/// <summary>
/// The state one pass-encode works over: handles fixed once the pass is set up, and the parts that evolve as ops
/// are encoded - the encoder, the path clip whose depth mask is currently applied, and the counters the stats line
/// reports.
/// </summary>
internal ref struct PassOps
{
	public IntPtr Pass;
	public WebGpuRenderSurface Target;
	public System.Collections.Generic.List<DrawOp> Ops;
	public System.Collections.Generic.List<BackdropCmd> Backdrops;
	public IntPtr SolidBuf, RrectBuf, GradBuf, QuadBuf, PathBuf, XformBg;
	public nuint SolidBufBytes, GradBufBytes, QuadBufBytes, PathBufBytes;

	public PassEncoder Enc;
	public float[] ClipFan;
	public Vector4 ClipAabb;

	public int Iters, Scissors, ClipChanges, FanOps, SharedOps, Tiled;
	public double CoverMpx;
}

internal unsafe struct PassEncoder
{
	private IntPtr _pass;
	private IntPtr _bundle;
	private IntPtr _pipe, _bg0, _bg1, _vb;
	private nuint _vbOffset, _vbSize;
	private int _sx, _sy, _sw, _sh;

	public PassEncoder(IntPtr pass)
	{
		_pass = pass;
		_bundle = IntPtr.Zero;
		Reset();
	}

	/// <summary>True while draws go to a render bundle rather than straight to the pass.</summary>
	public bool Recording => _bundle != IntPtr.Zero;

	/// <summary>Directs subsequent draws into <paramref name="bundle"/>.</summary>
	public void BeginBundle(IntPtr bundle)
	{
		_bundle = bundle;
		Reset();
	}

	/// <summary>Directs subsequent draws back to the pass.</summary>
	public void EndBundle()
	{
		_bundle = IntPtr.Zero;
		Reset();
	}

	/// <summary>
	/// Points the encoder at a freshly opened pass. A pass is ended and reopened mid-encode (a backdrop has to
	/// sample what is already drawn), and every handle plus all dedup state belongs to the pass that ended.
	/// </summary>
	public void Rebind(IntPtr pass)
	{
		_pass = pass;
		_bundle = IntPtr.Zero;
		Reset();
	}

	public void Reset()
	{
		_pipe = -1; _bg0 = -1; _bg1 = -1; _vb = -1;
		_vbOffset = unchecked((nuint)ulong.MaxValue);
		_vbSize = 0;
		_sx = _sy = _sw = _sh = -1;
	}

	/// <summary>
	/// Applies a scissor unless it is already current. A bundle inherits the pass's scissor and cannot set one,
	/// so this does nothing while recording.
	/// </summary>
	public void Scissor(int x, int y, int w, int h)
	{
		if (Recording || (x == _sx && y == _sy && w == _sw && h == _sh)) { return; }
		_sx = x; _sy = y; _sw = w; _sh = h;
		wgpuRenderPassEncoderSetScissorRect(_pass, (uint)x, (uint)y, (uint)w, (uint)h);
	}

	public void Pipe(IntPtr pipe)
	{
		if (pipe == _pipe) { return; }
		_pipe = pipe;
		if (Recording) { wgpuRenderBundleEncoderSetPipeline(_bundle, pipe); }
		else { wgpuRenderPassEncoderSetPipeline(_pass, pipe); }
	}

	public void Bg(uint group, IntPtr bg)
	{
		if (group == 0) { if (bg == _bg0) { return; } _bg0 = bg; }
		else if (group == 1) { if (bg == _bg1) { return; } _bg1 = bg; }
		if (Recording) { wgpuRenderBundleEncoderSetBindGroup(_bundle, group, bg, 0, (uint*)null); }
		else { wgpuRenderPassEncoderSetBindGroup(_pass, group, bg, 0, (uint*)null); }
	}

	public void Vb(IntPtr buf, nuint offset, nuint size)
	{
		if (buf == _vb && offset == _vbOffset && size == _vbSize) { return; }
		_vb = buf; _vbOffset = offset; _vbSize = size;
		if (Recording) { wgpuRenderBundleEncoderSetVertexBuffer(_bundle, 0, buf, offset, size); }
		else { wgpuRenderPassEncoderSetVertexBuffer(_pass, 0, buf, offset, size); }
	}

	public void Draw(uint count, uint firstVertex = 0)
	{
		if (Recording) { wgpuRenderBundleEncoderDraw(_bundle, count, 1, firstVertex, 0); }
		else { wgpuRenderPassEncoderDraw(_pass, count, 1, firstVertex, 0); }
	}
}
