// The encode-side shapes: what one GPU draw is (DrawOp), the geometry a replayed recording keeps on the GPU, and
// the state a pass-encode carries as it walks its ops.
#nullable disable
using System;
using System.Collections.Generic;
using System.Numerics;
using Uno.WebGpu.Native;
using static Uno.WebGpu.Native.WGPU;

namespace Uno.UI.Composition.WebGpu;

/// <summary>
/// A growable float buffer for vertex data. Deliberately a plain array rather than a <c>List&lt;float&gt;</c>:
/// reaching a list's storage goes through <c>CollectionsMarshal.SetCount&lt;T&gt;</c> and <c>AsSpan&lt;T&gt;</c>,
/// generic methods over a value type that Mono's AOT does not specialise, so on wasm every vertex append ran in
/// the interpreter. Growth copies with the non-generic <see cref="Array.Copy(Array, Array, int)"/> for the same
/// reason.
/// </summary>
internal sealed class VertBuf
{
	public float[] A = new float[4096];
	public int Count;

	public void Clear() => Count = 0;

	/// <summary>Reserves <paramref name="n"/> floats at the end and returns them to write into.</summary>
	public Span<float> Grow(int n)
	{
		int need = Count + n;
		if (need > A.Length)
		{
			int cap = A.Length;
			while (cap < need) { cap <<= 1; }
			var bigger = new float[cap];
			Array.Copy(A, bigger, Count);
			A = bigger;
		}
		int at = Count;
		Count = need;
		return new Span<float>(A, at, n);
	}

	public void Add(float v) => Grow(1)[0] = v;

	public ReadOnlySpan<float> Span => new(A, 0, Count);
}

/// <summary>Floats per vertex of each vertex layout.</summary>
internal static class VertexStride
{
	public const int Solid = 8;         // position + colour + coverage uv: rects, atlas quads, path fans
	public const int RoundedRect = 22;  // corner + local SDF params + colour + inner ring
	public const int Quad = 4;          // position + uv: images, layer composites, gradients
}

/// <summary>What a <see cref="DrawOp"/> draws, and so which pipeline and vertex layout it is encoded with.</summary>
internal enum DrawKind
{
	/// <summary>Solid-coloured triangles: rects, atlas quads and path fans (their coverage rides the vertex alpha).</summary>
	Solid,
	/// <summary>Analytic rounded rect / border ring (one SDF quad, no tessellation).</summary>
	RoundedRect,
	/// <summary>Textured quad.</summary>
	Image,
	/// <summary>A textured quad blended DstIn: the destination keeps only where the texture has alpha (a mask layer).</summary>
	Mask,
	/// <summary>Gradient-filled geometry.</summary>
	Gradient,
	/// <summary>Ends the pass segment so a backdrop can sample what is already drawn, then reopens it.</summary>
	BackdropSegment,
}

/// <summary>
/// One GPU draw of a pass: its pipeline (by kind), the vertices it draws (a range of a vertex buffer), the bind group
/// of its texture or gradient, and the clip it draws under. Two consecutive ops merge into one draw when they differ
/// only in their vertex range and that range is contiguous (see the encoder).
/// </summary>
internal struct DrawOp
{
	public DrawKind Kind;
	public IntPtr Verts;       // the op's own vertex buffer, or Zero for the pass's shared buffer of its kind
	public uint FirstVertex;   // into Verts
	public uint Count;         // vertices; a BackdropSegment keeps its index into the pass's backdrops here
	public IntPtr Group1;      // the image / gradient bind group; Zero for a solid or rounded rect
	public ClipData Clip;
	public IntPtr ClipBg;
	/// <summary>Where the recording this op belongs to sits. Zero = it is already in device space.</summary>
	public IntPtr SiteBg;
	/// <summary>Its site's slot, whose scissor box the encode reads. Zero = the op carries its own.</summary>
	public nint SiteSlot;
	/// <summary>
	/// The op's shape in the space its vertices are in, before the antialiasing pad; empty when it is not a plain
	/// rect or is not worth tracking. The occlusion cull reads it, and nothing else may assume it is set.
	/// </summary>
	public Vector4 Bounds;
	/// <summary>It paints <see cref="Bounds"/> at full alpha, so whatever it covers need not be drawn.</summary>
	public bool Opaque;
	/// <summary>
	/// The only band of the op still worth drawing, when a later opaque rect covers the rest of it. Empty
	/// (Z &lt;= X) unless the cull set it; applied on top of whatever scissor the op would otherwise get.
	/// </summary>
	public Vector4 CullScissor;

	/// <summary>An op drawing a range of the pass's shared buffer for its kind.</summary>
	public static DrawOp Shared(DrawKind kind, uint firstVertex, uint count, IntPtr group1, in ClipData clip, IntPtr clipBg)
		=> new() { Kind = kind, FirstVertex = firstVertex, Count = count, Group1 = group1, Clip = clip, ClipBg = clipBg };

	/// <summary>An op drawing a vertex buffer of its own from its start.</summary>
	public static DrawOp Own(DrawKind kind, IntPtr verts, uint count, IntPtr group1, in ClipData clip, IntPtr clipBg)
		=> new() { Kind = kind, Verts = verts, Count = count, Group1 = group1, Clip = clip, ClipBg = clipBg };

	public static DrawOp Backdrop(int index, in ClipData clip)
		=> new() { Kind = DrawKind.BackdropSegment, Count = (uint)index, Clip = clip };

	/// <summary>The same draw under another clip: what a restamp of an arena op produces.</summary>
	public DrawOp WithClip(in ClipData clip, IntPtr clipBg) { var o = this; o.Clip = clip; o.ClipBg = clipBg; return o; }

	public DrawOp WithClipSite(in ClipData clip, IntPtr clipBg, IntPtr siteBg, nint siteSlot) { var o = this; o.Clip = clip; o.ClipBg = clipBg; o.SiteBg = siteBg; o.SiteSlot = siteSlot; return o; }

	public bool SharesBuffer => Verts == IntPtr.Zero;
}

/// <summary>
/// A replayed recording's GPU geometry: its ops built once in the recording's own space, positioned on the GPU by the
/// replay transform each op's clip data carries. A move restamps that clip data; only a change of scale that
/// invalidates baked masks rebuilds. Owned by the render-thread device, released when the recording is disposed.
/// </summary>
internal sealed class WebGpuGeometryCache
{
	public List<DrawOp> Ops;
	public OwnedResources Owned;
	// Back-reference to the owning device so the recording's Dispose (UI thread) can enqueue this for a render-thread free.
	public WebGpuDevice Device;
	// This entry emitted atlas quads, baked at AtlasScale; a replay at another scale rebuilds rather than sample masks of the wrong size.
	public bool HasAtlas;
	// This entry baked coverage masks (path clips or big fills) at MaskScale; same rule.
	public bool HasClipMask;
	// Some op carries a path clip, so a restamp must bake a mask into the new stamp's bag rather than rewrite ClipU.
	public bool HasPathClip;
	// Built while the replay transform rotated or skewed, so its paths could not take the atlas; rebuilt once it settles.
	public bool AtlasBlockedByScale;
	public Vector2 AtlasScale;
	// The subpixel placement phase the atlas masks were baked at; a replay at a different fraction rebakes.
	public Vector2 AtlasPhase;
	public Vector2 MaskScale;
	// The stamps: each is the per-op clip bind groups for one (transform, clip, pass basis), reused verbatim while
	// those hold and rewritten in place when only the transform moved. There are several because one recording is
	// commonly replayed at several sites in a frame (a card template across a wall of cards), and a single stamp
	// would make every replay after the first mint a slab slot and a bind group per op, on every frame.
	public readonly List<StampSlot> Stamps = new();

	// Shared-entry state. The geometry is built in the recording's own space and positioned by the transform the
	// stamp carries, so two recordings whose commands are identical can draw from ONE entry -- which is the common
	// case for a templated list (a wall of bars records the same rect, only the visual's transform differs). Refs
	// counts the live recordings pointing here; the pool's own reference is deliberately uncounted so an entry at
	// zero stays claimable until the idle sweep takes it.
	public int Refs;
	// Replay sites seen in a frame. A template drawn at more sites than the entry keeps stamps makes them evict
	// each other, and every evicted site re-mints a slab slot and a bind group per op on the next frame, so the
	// cap follows demand rather than a fixed guess.
	public int SitesThisFrame, SitesLastFrame;
	public long SitesFrame;
	public long ContentKey;
	public long IdleSince;
	// The command list this was built from, kept for the equality check that guards against a hash collision.
	public List<WebGpuCommand> Src;
}

/// <summary>One stamp of an arena entry: its ops carrying the clip groups for one replay site.</summary>
internal sealed class StampSlot
{
	public List<DrawOp> Ops;
	public OwnedResources Owned;
	public List<nint> Bufs;
	public long Frame;
	public Matrix3x2 Xform;
	public ClipData Clip;
	/// <summary>The target rect the ops were baked against. The SIZE belongs here as much as the origin: the ops
	/// carry geometry in the target's own normalised space, so replaying a stamp built for the window into a small
	/// offscreen of the same origin puts every draw at the wrong scale.</summary>
	public Vector4 Basis;
	public int SessionEntries;
	public nint SiteSlot;
	public IntPtr SiteBg;
	/// <summary>Its ops were built independent of where the site sits, so a move can reuse them untouched.</summary>
	public bool SiteOps;
}

/// <summary>
/// The state one pass-encode works over: handles fixed once the pass is set up, and the parts that evolve as ops
/// are encoded - the encoder and the counters the stats line reports.
/// </summary>
internal ref struct PassOps
{
	public IntPtr Pass;
	public WebGpuRenderSurface Target;
	public List<DrawOp> Ops;
	public List<BackdropCmd> Backdrops;
	public IntPtr SolidBuf, RrectBuf, GradBuf, QuadBuf;
	public IntPtr PassBg;   // group 0 of every colour draw: this pass's projection
	public nuint SolidBufBytes, RrectBufBytes, GradBufBytes, QuadBufBytes;

	public PassEncoder Enc;

	public int Iters, Scissors, SharedOps;
}

/// <summary>
/// Encodes a pass's draws, skipping redundant pipeline / bind-group / vertex-buffer sets: a run of like ops
/// otherwise costs five native calls each where two suffice.
/// <para>
/// The tracked state belongs to one pass, so <see cref="Reset"/> must run at every boundary - a pass opening or
/// reopening, and any site that sets state directly rather than through these methods.
/// </para>
/// </summary>
internal unsafe struct PassEncoder
{
	private IntPtr _pass;
	private IntPtr _pipe, _bg0, _bg1, _bg2, _bg3, _vb;
	private nuint _vbOffset, _vbSize;
	private int _sx, _sy, _sw, _sh;

	public PassEncoder(IntPtr pass)
	{
		_pass = pass;
		Reset();
	}

	/// <summary>
	/// Points the encoder at a freshly opened pass. A pass is ended and reopened mid-encode (a backdrop has to
	/// sample what is already drawn), and every handle plus all dedup state belongs to the pass that ended.
	/// </summary>
	public void Rebind(IntPtr pass)
	{
		_pass = pass;
		Reset();
	}

	public void Reset()
	{
		_pipe = -1; _bg0 = -1; _bg1 = -1; _bg2 = -1; _bg3 = -1; _vb = -1;
		_vbOffset = unchecked((nuint)ulong.MaxValue);
		_vbSize = 0;
		_sx = _sy = _sw = _sh = -1;
	}

	/// <summary>Applies a scissor unless it is already current.</summary>
	public void Scissor(int x, int y, int w, int h)
	{
		if (x == _sx && y == _sy && w == _sw && h == _sh) { return; }
		_sx = x; _sy = y; _sw = w; _sh = h;
		wgpuRenderPassEncoderSetScissorRect(_pass, (uint)x, (uint)y, (uint)w, (uint)h);
	}

	public void Pipe(IntPtr pipe)
	{
		if (pipe == _pipe) { return; }
		_pipe = pipe;
		wgpuRenderPassEncoderSetPipeline(_pass, pipe);
	}

	public void Bg(uint group, IntPtr bg)
	{
		if (group == 0) { if (bg == _bg0) { return; } _bg0 = bg; }
		else if (group == 1) { if (bg == _bg1) { return; } _bg1 = bg; }
		else if (group == 2) { if (bg == _bg2) { return; } _bg2 = bg; }
		else if (group == 3) { if (bg == _bg3) { return; } _bg3 = bg; }
		wgpuRenderPassEncoderSetBindGroup(_pass, group, bg, 0, (uint*)null);
	}

	public void Vb(IntPtr buf, nuint offset, nuint size)
	{
		if (buf == _vb && offset == _vbOffset && size == _vbSize) { return; }
		_vb = buf; _vbOffset = offset; _vbSize = size;
		wgpuRenderPassEncoderSetVertexBuffer(_pass, 0, buf, offset, size);
	}

	public void Draw(uint count, uint firstVertex = 0)
	{
		wgpuRenderPassEncoderDraw(_pass, count, 1, firstVertex, 0);
	}
}
