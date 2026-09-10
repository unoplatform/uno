// The encode-side shapes: what one GPU draw is (DrawOp), the geometry a replayed recording keeps on the GPU, and
// the state a pass-encode carries as it walks its ops.
#nullable disable
using System;
using System.Collections.Generic;
using System.Numerics;
using Uno.WebGpu.Native;
using static Uno.WebGpu.Native.WGPU;

namespace Uno.UI.Composition.WebGpu;

/// <summary>Floats per vertex of the solid layout: position + colour + coverage uv.</summary>
internal static class VertexStride
{
	public const int Solid = 8;
}

/// <summary>
/// Sentinel <see cref="DrawOp.b0"/> for solid and rounded-rect ops whose verts live in the pass's shared buffer
/// (rebuilt each frame; <c>b1</c> = first vertex). Any other <c>b0</c> is the op's own vertex buffer handle.
/// </summary>
internal static class VertexSource
{
	public const int PassBuffer = 0;
}

/// <summary>What a <see cref="DrawOp"/> draws, and so which pipeline and vertex layout it is encoded with.</summary>
internal enum DrawKind
{
	/// <summary>Solid-coloured triangles: rects, atlas quads and path fans (their coverage rides the vertex alpha).</summary>
	Solid = 0,

	/// <summary>Textured quad. b0 = bind group, b1 = quad verts (or a byte offset into the pass's quad buffer when flag).</summary>
	Image = 2,

	/// <summary>Gradient-filled geometry.</summary>
	Gradient = 3,

	/// <summary>Analytic rounded rect / border ring (one SDF quad, no tessellation).</summary>
	RoundedRect = 5,

	/// <summary>Ends the pass segment so a backdrop can sample what is already drawn, then reopens it.</summary>
	BackdropSegment = 6,

	/// <summary>A textured quad blended DstIn: the destination keeps only where the texture has alpha (a mask layer).</summary>
	Mask = 7,
}

internal struct DrawOp
{
	public DrawKind kind; public nint b0; public uint u0; public nint b1; public bool flag; public ClipData clip; public nint clipBg;
	public DrawOp(DrawKind kind, nint b0, uint u0, nint b1, bool flag, ClipData clip, nint clipBg)
	{
		this.kind = kind; this.b0 = b0; this.u0 = u0; this.b1 = b1; this.flag = flag; this.clip = clip; this.clipBg = clipBg;
	}
	public readonly void Deconstruct(out DrawKind kind, out nint b0, out uint u0, out nint b1, out bool flag, out ClipData clip, out nint clipBg)
	{
		kind = this.kind; b0 = this.b0; u0 = this.u0; b1 = this.b1; flag = this.flag; clip = this.clip; clipBg = this.clipBg;
	}
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
	public Vector2 MaskScale;
	// The stamp: per-op clip bind groups for one (transform, clip, pass basis). Reused verbatim while those hold; a
	// change rewrites the ClipU slots in place when the entry count is unchanged, else stamps afresh.
	public List<DrawOp> StampedOps;
	public OwnedResources StampOwned;
	public List<nint> StampBufs;
	public long StampFrame;
	public Matrix3x2 StampXform;
	public ClipData StampClip;
	public Vector2 StampBasis;
	public int StampSessionEntries;
	public bool HasStamp;
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
	public nuint SolidBufBytes, GradBufBytes, QuadBufBytes;

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
	private IntPtr _pipe, _bg0, _bg1, _bg2, _vb;
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
		_pipe = -1; _bg0 = -1; _bg1 = -1; _bg2 = -1; _vb = -1;
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
