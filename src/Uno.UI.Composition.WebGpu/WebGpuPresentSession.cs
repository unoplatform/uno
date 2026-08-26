// The WebGPU implementation of the neutral drawing seam: turns a recorded frame into render passes, with no
// SkiaSharp anywhere in the path. Recording lives in WebGpuCommandRecorder, the op and encode shapes in
// WebGpuDrawOps.cs, and the resources in WebGpuDrawingFactory.cs.
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

public sealed unsafe partial class WebGpuPresentSession : IPresentSession
{
	// UNO_WEBGPU_STATS=1: per-pass emit-shape diagnostics (see RenderInto).
	private int _statCrMiss, _statCrMove, _statCrPathFlip, _statCrSize, _statCrClip;
	private static readonly bool _emitStats = Environment.GetEnvironmentVariable("UNO_WEBGPU_STATS") is "1" or "true";
	private static int _emitStatsFrame;
	// Build-shape counters (per stats interval): geometry-cache rebuilds / clip re-stamps observed while replaying.
	private static int _statTableRebuilds, _statStamps, _statArenaRebuilds, _statCachedRebuilds;

	private readonly WebGpuDevice _d;
	private readonly WebGpuRenderSurface _s;
	private WColor? _presentClear;
	// The composition records in LOGICAL coordinates; without this the frame renders at logical size on a
	// physical-size surface.
	private Vector2 _presentScale = Vector2.One;
	private readonly System.Collections.Generic.Stack<Vector2> _presentScaleStack = new();
	private IntPtr _frameEncoder;
	// Immediate-mode drawing on the present session (e.g. the FPS/diagnostics overlay drawn after Replay) records
	// here and is composited onto the replayed frame at Dispose — the present session IS a real drawing session,
	// like the Skia one, not a replay-only sink. State verbs (Save/Scale/clip/…) forward here too so the overlay
	// honours the transform; Scale/Save/Restore additionally drive the frame's root DPI scale (_presentScale).
	private readonly WebGpuCommandRecorder _overlay;
	private readonly IDrawingFactory _factory;
	private List<WebGpuCommand> _pendingCmds;
	private WColor? _pendingClear;
	internal WebGpuPresentSession(WebGpuDevice d, WebGpuRenderSurface s, IDrawingFactory factory) { _d = d; _s = s; _factory = factory; _overlay = new WebGpuCommandRecorder(factory); }

	private static int _frameStatsCounter;
	// Op-build vs pass-encode, accumulated across the frame's passes (UNO_WEBGPU_STATS).
	internal static long OpsBuildTicks, EncodeTicks;

	private void RunFrame(List<WebGpuCommand> cmds, WColor? clear, bool load = false)
	{
		var owns = _frameEncoder == IntPtr.Zero;
		if (owns) { _frameEncoder = wgpuDeviceCreateCommandEncoder(_d.Dev, null); }
		long t0 = _emitStats ? System.Diagnostics.Stopwatch.GetTimestamp() : 0;
		try
		{
			RenderInto(cmds, _s, clear, load);
		}
		finally
		{
			if (owns)
			{
				long t1 = _emitStats ? System.Diagnostics.Stopwatch.GetTimestamp() : 0;
				_d.ClipSlab.Flush();   // one queue write per dirty chunk, before the submit that reads the clips
				_d.FlushFrameSlabs();
				var cb = wgpuCommandEncoderFinish(_frameEncoder, null);
				wgpuQueueSubmit(_d.Q, 1, (IntPtr)(&cb));
				// wgpu holds its own reference until the submission completes, so both handles are dropped
				// here — otherwise every frame leaks an encoder + a command buffer into the handle table.
				wgpuCommandBufferRelease(cb);
				wgpuCommandEncoderRelease(_frameEncoder);
				if (_emitStats && (_frameStatsCounter++ % 60) == 0)
				{
					long t2 = System.Diagnostics.Stopwatch.GetTimestamp();
					double toMs = 1000.0 / System.Diagnostics.Stopwatch.Frequency;
					System.Console.WriteLine($"[webgpu-frame] cmds={cmds.Count} renderInto={(t1 - t0) * toMs:F1}ms finishSubmit={(t2 - t1) * toMs:F1}ms opsBuild={OpsBuildTicks * toMs:F1}ms encode={EncodeTicks * toMs:F1}ms");
					OpsBuildTicks = 0; EncodeTicks = 0;
				}
				// Pump the device non-blocking (wait=0) so the CPU can overlap the next frame with the GPU: pooled-buffer
				// reuse is queue-ordered (wgpuQueueWriteBuffer runs after the prior frame's reads) and transient textures
				// are refcount-released, so it is safe; the swapchain's max-frames-in-flight provides backpressure.
				_ = wgpuDevicePoll(_d.Dev, 0u, null);

				// The frame is submitted, so every layer's colour texture is free to be reused by the next frame.
				foreach (var ls in _frameLayerSurfaces)
				{
					if (_d.MsaaSamples > 1) { _d.Pool.Return(ls.MsaaColorView); }   // at 1x MsaaColorView aliases View
					_d.Pool.Return(ls.View);
				}

				_frameLayerSurfaces.Clear();
				_frameEncoder = IntPtr.Zero;
			}
		}
	}

	// Stores a freshly built compiled entry on its recording, handling the Dispose race: Dispose exchanged the
	// field before this store, so it couldn't see the new entry — hand it over here (the exchange keeps the
	// release single-shot whichever side wins).
	private void StoreCompiled(WebGpuRenderRecord rec, WebGpuGeometryCache fe)
	{
		fe.Device = _d;
		rec.Compiled = fe;
		if (rec.Commands is null && System.Threading.Interlocked.Exchange(ref rec.Compiled, null) is { } orphan)
		{
			_d.DeferCompiledRelease(orphan.Owned, orphan.StampOwned, orphan.XformSlot);
		}
	}

	private readonly List<WebGpuRenderSurface> _frameLayerSurfaces = new();

	// NDC basis for the surface the open pass renders into: device-space (_basisOx,_basisOy) is its top-left and
	// (_basisW,_basisH) its size. The window and a full-size layer get (0,0,W,H) — the target itself; a
	// size-to-content layer gets its device sub-rect, so absolute device coords map into the smaller offscreen.
	// A scissor must also be contained in its attachment, which the same size gives us. Saved/restored per RenderInto.
	private float _basisOx, _basisOy, _basisW, _basisH;

	// Size-to-content layer offscreens (on by default). UNO_WEBGPU_NO_SUBLAYER=1 forces the full-window offscreen
	// for every layer — an escape hatch and the A/B baseline for validating the optimization is behaviour-neutral.
	private static readonly bool _subLayerSizing = Environment.GetEnvironmentVariable("UNO_WEBGPU_NO_SUBLAYER") is not ("1" or "true");

	private bool TryScissor(Vector4 clip, out int x, out int y, out int w, out int h)
	{
		// Scissor is in the CURRENT target's pixels; clip AABBs are absolute device coords, so rebase by the basis
		// origin and clamp to its size (identity for the window / a full-size layer, a real shift for a sub-rect).
		var limW = _basisW > 0f ? _basisW : _s.Width;
		var limH = _basisH > 0f ? _basisH : _s.Height;
		x = (int)MathF.Max(0, MathF.Floor(clip.X - _basisOx)); y = (int)MathF.Max(0, MathF.Floor(clip.Y - _basisOy));
		int r = (int)MathF.Min(limW, MathF.Ceiling(clip.Z - _basisOx)); int b = (int)MathF.Min(limH, MathF.Ceiling(clip.W - _basisOy));
		x = (int)MathF.Min(x, limW); y = (int)MathF.Min(y, limH);
		w = r - x; h = b - y; return w > 0 && h > 0;
	}
	private Vector2 Ndc(Vector2 dev) => new(2f * (dev.X - _basisOx) / BasisW - 1f, 1f - 2f * (dev.Y - _basisOy) / BasisH);

	private float BasisW => _basisW > 0f ? _basisW : _s.Width;
	private float BasisH => _basisH > 0f ? _basisH : _s.Height;

	private static readonly Vector4 _emptyBounds = new(float.MaxValue, float.MaxValue, float.MinValue, float.MinValue);

	// True if the list directly contains a nested layer or a backdrop — either makes size-to-content unsafe: a nested
	// layer composites in window space, and a backdrop must sample the real framebuffer, which a sub-surface lacks.
	private static bool HasLayerOrBackdrop(List<WebGpuCommand> cmds)
	{
		for (int i = 0; i < cmds.Count; i++)
		{
			if (cmds[i] is LayerCmd or BackdropCmd)
			{
				return true;
			}
		}
		return false;
	}

	private static Vector4 CmdListBounds(List<WebGpuCommand> cmds)
	{
		var b = _emptyBounds;
		foreach (var cmd in cmds)
		{
			var cb = cmd switch
			{
				RectCommand r => QuadBounds(r.P0, r.P1, r.P2, r.P3, r.Clip),
				RoundedRectCmd rr => QuadBounds(rr.P0, rr.P1, rr.P2, rr.P3, rr.Clip),
				ImageCmd im => QuadBounds(im.P0, im.P1, im.P2, im.P3, im.Clip),
				GradientCmd g => QuadBounds(g.P0, g.P1, g.P2, g.P3, g.Clip),
				PathFill p => ClampToClip(new Vector4(p.BbMin.X, p.BbMin.Y, p.BbMax.X, p.BbMax.Y), p.Clip),
				ShadowCmd sh => ClampToClip(Inflate(new Vector4(sh.BbMin.X, sh.BbMin.Y, sh.BbMax.X, sh.BbMax.Y), MathF.Ceiling(3f * MathF.Max(sh.SigmaX, sh.SigmaY)) + 2f), sh.Clip),
				LayerCmd l => ClampToClip(LayerBounds(l), l.Clip),
				ReplayRefCmd rr => ClampToClip(TransformBounds(rr.Data.IdentityBounds ??= CmdListBounds(rr.Commands), rr.Transform), rr.Clip),
				// A backdrop samples/draws within its clip; with no finite clip it can cover the whole surface.
				BackdropCmd bk => IsFiniteAabb(bk.Clip.Aabb) ? bk.Clip.Aabb : new Vector4(float.MinValue, float.MinValue, float.MaxValue, float.MaxValue),
				_ => new Vector4(float.MinValue, float.MinValue, float.MaxValue, float.MaxValue),
			};
			b = new Vector4(MathF.Min(b.X, cb.X), MathF.Min(b.Y, cb.Y), MathF.Max(b.Z, cb.Z), MathF.Max(b.W, cb.W));
		}
		return b;
	}

	private static Vector4 LayerBounds(LayerCmd l)
	{
		var b = CmdListBounds(l.Commands);
		if (l.ShadowEffect is { } fx && b.X <= b.Z)
		{
			var pad = MathF.Ceiling(3f * MathF.Max(fx.SigmaX, fx.SigmaY)) + 2f;
			var sb = Inflate(new Vector4(b.X + fx.Dx, b.Y + fx.Dy, b.Z + fx.Dx, b.W + fx.Dy), pad);
			b = new Vector4(MathF.Min(b.X, sb.X), MathF.Min(b.Y, sb.Y), MathF.Max(b.Z, sb.Z), MathF.Max(b.W, sb.W));
		}
		return b;
	}

	private static Vector4 QuadBounds(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, in ClipData clip)
	{
		var min = Vector2.Min(Vector2.Min(p0, p1), Vector2.Min(p2, p3));
		var max = Vector2.Max(Vector2.Max(p0, p1), Vector2.Max(p2, p3));
		return ClampToClip(new Vector4(min.X, min.Y, max.X, max.Y), clip);
	}

	private static Vector4 ClampToClip(Vector4 b, in ClipData clip)
		=> IsFiniteAabb(clip.Aabb)
			? new Vector4(MathF.Max(b.X, clip.Aabb.X), MathF.Max(b.Y, clip.Aabb.Y), MathF.Min(b.Z, clip.Aabb.Z), MathF.Min(b.W, clip.Aabb.W))
			: b;

	private static Vector4 Inflate(Vector4 b, float pad) => new(b.X - pad, b.Y - pad, b.Z + pad, b.W + pad);

	private static Vector4 TransformBounds(Vector4 b, Matrix4x4 m)
	{
		if (b.X > b.Z) { return b; }   // empty stays empty
		Vector2 T(float x, float y) => new(x * m.M11 + y * m.M21 + m.M41, x * m.M12 + y * m.M22 + m.M42);
		var q0 = T(b.X, b.Y); var q1 = T(b.Z, b.Y); var q2 = T(b.Z, b.W); var q3 = T(b.X, b.W);
		var min = Vector2.Min(Vector2.Min(q0, q1), Vector2.Min(q2, q3));
		var max = Vector2.Max(Vector2.Max(q0, q1), Vector2.Max(q2, q3));
		return new Vector4(min.X, min.Y, max.X, max.Y);
	}

	// Reused so the per-frame op rebuild does not allocate a list and an array per primitive.
	private readonly List<float> _scratch = new();
	private readonly float[] _clipU = new float[72];   // ClipU: rects[4]+radii[4] + ex+ctrl+size+xform+xoff+finv + radiiY[4] = 288B

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

	private void WriteXform(int slot, Matrix4x4 r)
	{
		int need = (slot + 1) * 8;
		while (_xforms.Count < need) { _xforms.Add(0f); }
		// device->NDC for the CURRENT target via the basis, matching Ndc().
		float w = BasisW, h = BasisH;
		int o = slot * 8;
		_xforms[o + 0] = 2f * r.M11 / w; _xforms[o + 1] = 2f * r.M21 / w; _xforms[o + 2] = 2f * (r.M41 - _basisOx) / w - 1f; _xforms[o + 3] = -2f * r.M12 / h;
		_xforms[o + 4] = -2f * r.M22 / h; _xforms[o + 5] = 1f - 2f * (r.M42 - _basisOy) / h; _xforms[o + 6] = 0f; _xforms[o + 7] = 0f;
	}

	private int AllocTransientPathSlot()
	{
		int slot = _d.AllocXformSlot();
		_xformTransient.Add(slot);
		WriteXform(slot, Matrix4x4.Identity);
		return slot;
	}

	// Per-pass shared SOLID vertex buffer: every device-space solid run — immediate draws AND
	// solid-only cached recordings — appends its 6-float verts here in op order, so adjacent solid ops sharing a clip
	// occupy a CONTIGUOUS range and the emit loop coalesces them into ONE draw (cross-visual, not just within one
	// recording). Uploaded once per pass; recycled next pass. A PassBuffer solid op references (b1=startVert, u0=count)
	// into this buffer; b0!=0 is a legacy private-buffer solid (mixed/arena recording) that draws on its own.
	private readonly Stack<List<float>> _solidPool = new();
	private List<float> _gradVerts;
	private List<float> _quadVerts;
	private List<float> _pathVerts;

	private int AppendPathBlock(float[] src)
	{
		while ((_pathVerts.Count * sizeof(float)) % 84 != 0) { _pathVerts.Add(0f); }
		var off = _pathVerts.Count * sizeof(float);
		_pathVerts.AddRange(src);
		return off;
	}

	private int AppendPathBlock(List<float> src)
	{
		while ((_pathVerts.Count * sizeof(float)) % 84 != 0) { _pathVerts.Add(0f); }
		var off = _pathVerts.Count * sizeof(float);
		_pathVerts.AddRange(src);
		return off;
	}
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

	// Per-pass shared ROUNDED-RECT buffer (22 floats/vert, per-vertex SDF params). Every rrect — immediate and
	// re-appended cached — lands here in op order so adjacent rrect ops sharing a clip coalesce into ONE draw across
	// visuals as one draw of 6*N verts rather than N draws of 6. Returns the start vertex index.
	private readonly Stack<List<float>> _rrectPool = new();
	private List<float> RentRrect() => _rrectPool.Count > 0 ? _rrectPool.Pop() : new(4096);
	private void ReturnRrect(List<float> s) { s.Clear(); _rrectPool.Push(s); }
	private int AppendRrect(List<float> rr, RoundedRectCmd rrc)
	{
		int start = rr.Count / 22;
		var hf = rrc.Half; var rad = rrc.Radii; var ih = rrc.InnerHalf; var ic = rrc.InnerCenter; var ir = rrc.InnerRadii;
		float cr = rrc.Color.R / 255f, cg = rrc.Color.G / 255f, cb = rrc.Color.B / 255f, color = rrc.Color.A / 255f * rrc.Opacity;
		Span<Vector2> dev = stackalloc Vector2[4] { rrc.P0, rrc.P1, rrc.P3, rrc.P2 };
		Span<Vector2> ctr = stackalloc Vector2[4] { new(-hf.X, -hf.Y), new(hf.X, -hf.Y), new(-hf.X, hf.Y), new(hf.X, hf.Y) };
		ReadOnlySpan<int> tri = stackalloc int[6] { 0, 1, 2, 2, 1, 3 };
		foreach (var idx in tri)
		{
			var n = Ndc(dev[idx]);
			rr.Add(n.X); rr.Add(n.Y); rr.Add(ctr[idx].X); rr.Add(ctr[idx].Y); rr.Add(hf.X); rr.Add(hf.Y);
			rr.Add(rad.X); rr.Add(rad.Y); rr.Add(rad.Z); rr.Add(rad.W); rr.Add(cr); rr.Add(cg); rr.Add(cb); rr.Add(color);
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
		float cr = rrc.Color.R / 255f, cg = rrc.Color.G / 255f, cb = rrc.Color.B / 255f, color = rrc.Color.A / 255f * rrc.Opacity;
		Span<Vector2> dev = stackalloc Vector2[4] { rrc.P0, rrc.P1, rrc.P3, rrc.P2 };
		Span<Vector2> ctr = stackalloc Vector2[4] { new(-hf.X, -hf.Y), new(hf.X, -hf.Y), new(-hf.X, hf.Y), new(hf.X, hf.Y) };
		ReadOnlySpan<int> tri = stackalloc int[6] { 0, 1, 2, 2, 1, 3 };
		foreach (var idx in tri)
		{
			var d = dev[idx];
			rr.Add(d.X); rr.Add(d.Y); rr.Add(ctr[idx].X); rr.Add(ctr[idx].Y); rr.Add(hf.X); rr.Add(hf.Y);
			rr.Add(rad.X); rr.Add(rad.Y); rr.Add(rad.Z); rr.Add(rad.W); rr.Add(cr); rr.Add(cg); rr.Add(cb); rr.Add(color);
			rr.Add(ih.X); rr.Add(ih.Y); rr.Add(ic.X); rr.Add(ic.Y); rr.Add(ir.X); rr.Add(ir.Y); rr.Add(ir.Z); rr.Add(ir.W); rr.Add(slotBits);
		}
	}

	private IntPtr MakeBuffer(float[] data)
	{
		var size = data.Length * sizeof(float);
		var buf = _d.BufferPool.Rent(size, WGPUBufferUsage.Vertex | WGPUBufferUsage.CopyDst);
		fixed (float* p = data) { wgpuQueueWriteBuffer(_d.Q, buf, 0, (IntPtr)p, (nuint)size); }
		return buf;
	}

	// List overload: uploads directly from the list's backing store (no ToArray copy).
	private IntPtr MakeBuffer(List<float> data)
	{
		var span = System.Runtime.InteropServices.CollectionsMarshal.AsSpan(data);
		var size = span.Length * sizeof(float);
		var buf = _d.BufferPool.Rent(size, WGPUBufferUsage.Vertex | WGPUBufferUsage.CopyDst);
		fixed (float* p = span) { wgpuQueueWriteBuffer(_d.Q, buf, 0, (IntPtr)p, (nuint)size); }
		return buf;
	}

	private IntPtr Vbuf(List<float> data, OwnedResources owned)
		=> owned is null ? MakeBuffer(data) : Vbuf(System.Runtime.InteropServices.CollectionsMarshal.AsSpan(data).ToArray(), owned);

	private void PushVert(Vector2 dev, float r, float g, float b, float a)
	{
		var n = Ndc(dev);
		_scratch.Add(n.X); _scratch.Add(n.Y); _scratch.Add(r); _scratch.Add(g); _scratch.Add(b); _scratch.Add(a);
	}

	private void PushVertT(Vector2 dev, float r, float g, float b, float a, float slotBits)
	{
		_scratch.Add(dev.X); _scratch.Add(dev.Y); _scratch.Add(r); _scratch.Add(g); _scratch.Add(b); _scratch.Add(a); _scratch.Add(slotBits);
	}

	private IntPtr MakeUniform(int byteSize)
		=> _d.BufferPool.Rent(byteSize, WGPUBufferUsage.Uniform | WGPUBufferUsage.CopyDst);

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

	private const int ClipUBytes = 288;   // rects[4]+radii[4] (128) + ex+ctrl+size+xform+xoff+finv (96) + radiiY[4] (64); match the WGSL struct

	private bool FillClipU(ClipData cd, Matrix3x2 xform, Matrix3x2 finv)
	{
		if (xform == default) { xform = Matrix3x2.Identity; }   // default(Matrix3x2) is all-zero; treat as identity
		if (finv == default) { finv = Matrix3x2.Identity; }
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
		// Fold the clip's finite AABB into the dedicated rect slot (ctrl.y flag; min in ctrl.zw, max in
		// size.zw): the shader then owns the rect edge and the emit widens the scissor to cull-only
		// (see AabbInClipU). Path clips keep the scissor (the depth mask relies on it).
		var foldedAabb = false;
		var ab = cd.Aabb;
		if (cd.PathFan is null && (ab.X > -1e8f || ab.Y > -1e8f || ab.Z < 1e8f || ab.W < 1e8f))
		{
			cu[37] = 1f;                       // ctrl.y = rect clip enabled
			cu[38] = ab.X; cu[39] = ab.Y;      // ctrl.zw = rect min
			cu[42] = ab.Z; cu[43] = ab.W;      // size.zw = rect max
			foldedAabb = true;
		}
		cu[36] = n;                              // ctrl.x = active count
		cu[40] = _s.Width; cu[41] = _s.Height;   // size
												 // xform maps stored (identity-baked) NDC verts to the replay NDC: px = M11*x + M21*y + M31, py = M12*x + M22*y + M32.
		cu[44] = xform.M11; cu[45] = xform.M21; cu[46] = xform.M12; cu[47] = xform.M22;
		cu[48] = xform.M31; cu[49] = xform.M32;   // xoff.xy (NDC translation)
												  // finv maps the device fragment position back to the recording's own space (inverse device affine) so a clip
												  // baked at identity is correct after the move. Identity => clipCov sees fc unchanged. finv 2x2 in `finv`,
												  // finv translation in xoff.zw (px = fM11*x + fM21*y + fM31, py = fM12*x + fM22*y + fM32).
		// Under a size-to-content layer the fragment position is sub-LOCAL while the baked clip/gradient geometry is
		// ABSOLUTE device, so finvMap must shift sub-local->absolute first; composing with finv folds that into its
		// translation. A zero basis (window / full-size layer) leaves this unchanged.
		cu[50] = finv.M31 + finv.M11 * _basisOx + finv.M21 * _basisOy;
		cu[51] = finv.M32 + finv.M12 * _basisOx + finv.M22 * _basisOy;
		cu[52] = finv.M11; cu[53] = finv.M12; cu[54] = finv.M21; cu[55] = finv.M22;
		return foldedAabb;
	}

	// In-place restamp of an existing owned ClipU slab slot: the shadow write flushes as part of ONE per-chunk
	// queue write before submit (queue-ordered, so frames already submitted read the old floats); the bind group
	// survives, making a per-frame restamp free of native calls.
	private bool RewriteClipU(nint slot, ClipData cd, Matrix3x2 xform, Matrix3x2 finv)
	{
		var folded = FillClipU(cd, xform, finv);
		_d.ClipSlab.Write(slot, _clipU);
		return folded;
	}

	// Owned variant exposing the ClipU slab slot so a later restamp can RewriteClipU it in place.
	private IntPtr MakeClipBgOwned(IntPtr bgl, ClipData cd, OwnedResources owned, Matrix3x2 xform, Matrix3x2 finv, out nint buf, out bool aabbInClipU)
	{
		aabbInClipU = FillClipU(cd, xform, finv);
		var slot = _d.ClipSlab.Alloc();
		_d.ClipSlab.Write(slot, _clipU);
		(owned.ClipSlots ??= new()).Add(slot);
		var e = new WGPUBindGroupEntry { Binding = 0, Buffer = _d.ClipSlab.BufferOf(slot), Offset = _d.ClipSlab.OffsetOf(slot), Size = ClipUBytes };
		var bgd = new WGPUBindGroupDescriptor { Layout = bgl, EntryCount = 1, Entries = &e };
		buf = slot;
		return Bg(ref bgd, owned);
	}

	private IntPtr MakeClipBg(IntPtr bgl, ClipData cd, OwnedResources owned = null, Matrix3x2 xform = default, Matrix3x2 finv = default)
	{
		if (owned is not null) { return MakeClipBgOwned(bgl, cd, owned, xform, finv, out _, out _); }
		FillClipU(cd, xform, finv);
		var cu = _clipU;

		// Immediate ops take a recycled per-frame slab slot: its bind group is created once and reused, and the
		// whole frame's clips upload in one queue write per chunk. Do NOT content-key this: a clip carries
		// DEVICE-space geometry, so under any moving transform every lookup misses and mints a buffer + bind
		// group per draw.
		return _d.ClipBgSlabFor(bgl, ClipUBytes).Rent(bgl, cu);
	}

	// Coverage atlas: ON by default - it is what makes arbitrary path edges and glyphs crisp without MSAA.
	// UNO_WEBGPU_PATH_ATLAS=0 opts out (falls back to tessellated AA, which aliases on curved outlines).
	private static readonly bool _pathAtlas = Environment.GetEnvironmentVariable("UNO_WEBGPU_PATH_ATLAS") is not "0";

	/// <summary>
	/// Renders a command list into a target surface's MSAA pass, resolving into its single-sample view. Layers
	/// recurse into a surface of their own and composite here; shadows and layers pre-render before the pass opens.
	/// </summary>
	// basisW/basisH default (0) to the target's own size at origin (basisOx,basisOy) — the whole-target mapping the
	// window and full-size layers use. A size-to-content layer passes its device sub-rect.
	private void RenderInto(List<WebGpuCommand> cmds, WebGpuRenderSurface target, WColor? clear, bool load = false,
		float basisOx = 0f, float basisOy = 0f, float basisW = 0f, float basisH = 0f)
	{
		_renderIntoStart = System.Diagnostics.Stopwatch.GetTimestamp();

		var savedBasis = (_basisOx, _basisOy, _basisW, _basisH);
		_basisOx = basisOx;
		_basisOy = basisOy;
		_basisW = basisW > 0f ? basisW : target.Width;
		_basisH = basisH > 0f ? basisH : target.Height;

		var ops = RentOps();
		var solid = RentSolid();
		var rrect = RentRrect();
		var savedXforms = _xforms; var savedTransient = _xformTransient;
		// Immediate gradient quads share ONE per-pass buffer, like solids. Giving each quad its own pooled buffer
		// reads cleaner but costs a queue write apiece: 500 native calls per frame on RenderStress_Gradients to
		// carry 48 bytes each, and a native call costs far more than the bytes it carries. Fields rather than
		// locals because BuildSimpleOp appends to them; saved/restored so each nested pass uploads its own.
		var savedGradVerts = _gradVerts;
		var savedQuadVerts = _quadVerts;
		var savedPathVerts = _pathVerts;
		_gradVerts = RentSolid(); _gradVerts.Clear();
		_quadVerts = RentSolid(); _quadVerts.Clear();
		_pathVerts = RentSolid(); _pathVerts.Clear();
		var mainPass = ReferenceEquals(target, _s);
		_xforms = RentXforms(); _xforms.Clear();
		_xformTransient = RentTransient(); _xformTransient.Clear();
		// Recordings emitted so far in THIS pass. A recording replayed more than once in one frame (same command
		// list at different transforms) can't share its single resident slab slice — see the frame-solid branch.
		var frameEmitted = new HashSet<List<WebGpuCommand>>(System.Collections.Generic.ReferenceEqualityComparer.Instance);
		var backdrops = new List<BackdropCmd>();
		for (int ci = 0; ci < cmds.Count; ci++)
		{
			var cmd = cmds[ci];
			switch (cmd)
			{
				case RectCommand rc0:
					{
						int j = ci; int start = solid.Count / 6;
						while (j < cmds.Count && cmds[j] is RectCommand rcj && ClipDataEquals(rcj.Clip, rc0.Clip))
						{
							AppendSolidRect(solid, rcj.P0, rcj.P1, rcj.P2, rcj.P3, rcj.Color.R / 255f, rcj.Color.G / 255f, rcj.Color.B / 255f, rcj.Color.A / 255f);
							j++;
						}
						ops.Add(new DrawOp(DrawKind.Solid, VertexSource.PassBuffer, (uint)((j - ci) * 6), (nint)start, false, rc0.Clip, (nint)MakeClipBg(_d.SolidClipBgl, rc0.Clip)));
						ci = j - 1;   // the for-loop's ci++ advances past the run
						break;
					}
				case PathFill:
					BuildSimpleOp(cmd, ops, null, AllocTransientPathSlot(), atlasScale: Vector2.One);   // pooled (per-frame); transient table slot
					break;
				case ImageCmd:
				case GradientCmd:
					BuildSimpleOp(cmd, ops, null, -1, atlasScale: Vector2.One);   // pooled (per-frame)
					break;
				case RoundedRectCmd rri:
					{
						// Shared rrect buffer (VertexSource.PassBuffer, b1=start vert): adjacent same-clip rrects coalesce on emit.
						int st = rrect.Count / 22;
						AppendRrect(rrect, rri);
						ops.Add(new DrawOp(DrawKind.RoundedRect, VertexSource.PassBuffer, 6, (nint)st, false, rri.Clip, (nint)MakeClipBg(_d.RrClipBgl, rri.Clip)));
						break;
					}
				case ReplayRefCmd rr:
					EmitReplayRef(rr, ops, frameEmitted);
					break;
				case ShadowCmd sh:
					EmitShadow(sh, ops);
					break;
				case LayerCmd lyr:
					EmitLayer(lyr, ops);
					break;
				case BackdropCmd bk:
					{
						// Defer to encode-time pass-segmenting: a BackdropSegment op splits THIS pass here so the backdrop samples the
						// framebuffer RESOLVED SO FAR (the content behind it) in place — no offscreen, no prefix re-render. Works for
						// the on-window target AND pooled layer targets: both store+reload their MSAA across the segment (see the
						// main-pass + segment StoreOp), so an acrylic inside a layer/flyout skips the full-window offscreen the old
						// pooled fallback re-rendered per backdrop, and an empty prefix costs nothing (no separate blurred offscreen).
						int bi = backdrops.Count; backdrops.Add(bk);
						ops.Add(new DrawOp(DrawKind.BackdropSegment, 0, 0, (nint)bi, false, bk.Clip, 0));
						break;
					}
			}
		}

		// Upload the whole pass's coalesceable solid + rrect geometry in ONE buffer each; PassBuffer ops index them.
		nint solidBuf = solid.Count > 0 ? (nint)MakeBuffer(solid) : IntPtr.Zero;
		nint rrectBuf = rrect.Count > 0 ? (nint)MakeBuffer(rrect) : IntPtr.Zero;
		nint gradBuf = _gradVerts.Count > 0 ? (nint)MakeBuffer(_gradVerts) : IntPtr.Zero;
		var gradBufBytes = (nuint)(_gradVerts.Count * sizeof(float));
		nint quadBuf = _quadVerts.Count > 0 ? (nint)MakeBuffer(_quadVerts) : IntPtr.Zero;
		var quadBufBytes = (nuint)(_quadVerts.Count * sizeof(float));
		nint pathBuf = _pathVerts.Count > 0 ? (nint)MakeBuffer(_pathVerts) : IntPtr.Zero;
		var pathBufBytes = (nuint)(_pathVerts.Count * sizeof(float));
		var solidBufBytes = (nuint)(solid.Count * sizeof(float));

		nint xformBg = IntPtr.Zero;
		if (_xforms.Count > 0)
		{
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

		if (_emitStats) { OpsBuildTicks += System.Diagnostics.Stopwatch.GetTimestamp() - _renderIntoStart; }

		var color = new WGPURenderPassColorAttachment
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
			View = target.MsaaColorView,
			ResolveTarget = _d.MsaaSamples > 1 ? target.View : IntPtr.Zero,
			LoadOp = load ? WGPULoadOp.Load : WGPULoadOp.Clear,
			StoreOp = (_d.MsaaSamples > 1 && backdrops.Count == 0) ? WGPUStoreOp.Discard : WGPUStoreOp.Store,
			ClearValue = clear.HasValue ? new WGPUColor { R = clear.Value.R / 255.0, G = clear.Value.G / 255.0, B = clear.Value.B / 255.0, A = clear.Value.A / 255.0 } : default,
		};
		var depthStencil = new WGPURenderPassDepthStencilAttachment
		{
			View = target.DepthView,
			DepthLoadOp = WGPULoadOp.Clear,
			DepthStoreOp = WGPUStoreOp.Discard,
			DepthClearValue = 0f,
			StencilLoadOp = WGPULoadOp.Clear,
			StencilStoreOp = WGPUStoreOp.Discard,
			StencilClearValue = 0,
		};
		var desc = new WGPURenderPassDescriptor { ColorAttachmentCount = 1, ColorAttachments = &color, DepthStencilAttachment = &depthStencil };
		var pass = wgpuCommandEncoderBeginRenderPass(_frameEncoder, &desc);
		var encodeStart = System.Diagnostics.Stopwatch.GetTimestamp();

		var pst = new PassOps
		{
			Pass = pass, Target = target, Ops = ops, Backdrops = backdrops,
			SolidBuf = solidBuf, SolidBufBytes = solidBufBytes,
			RrectBuf = rrectBuf,
			GradBuf = gradBuf, GradBufBytes = gradBufBytes,
			QuadBuf = quadBuf, QuadBufBytes = quadBufBytes,
			PathBuf = pathBuf, PathBufBytes = pathBufBytes,
			XformBg = xformBg,
			Enc = new PassEncoder(pass),
		};
		EncodeOps(0, ops.Count, ref pst);
		if (_emitStats) { EncodeTicks += System.Diagnostics.Stopwatch.GetTimestamp() - encodeStart; }
		if (_emitStats && ops.Count > 0 && (_emitStatsFrame++ % 60) == 0)
		{
			WriteFrameStats(ops.Count, ref pst);
		}

		wgpuRenderPassEncoderEnd(pst.Pass);
		// A pooled offscreen (layer/backdrop) target: its MSAA colour has resolved into View and the depth is spent,
		// so return both for the next same-size pass to reuse — only View (composited/sampled later) stays live. The
		// on-window/dedicated target owns its MSAA+depth (persistent across frames) and is left untouched.
		if (target.Pooled) { if (_d.MsaaSamples > 1) { _d.Pool.Return(target.MsaaColorView); } _d.Pool.Return(target.DepthView); }   // at 1x MsaaColorView aliases View (sampled later) — don't reclaim
		ReturnOps(ops);   // ops are fully encoded into the pass now — recycle the list
		ReturnSolid(solid);
		ReturnRrect(rrect);
		foreach (var s in _xformTransient) { _d.FreeXformSlot(s); }
		_xforms.Clear(); _xformsPool.Push(_xforms);
		_xformTransient.Clear(); _xformTransientPool.Push(_xformTransient);
		ReturnSolid(_gradVerts);
		ReturnSolid(_quadVerts);
		ReturnSolid(_pathVerts);
		_gradVerts = savedGradVerts;
		_quadVerts = savedQuadVerts;
		_pathVerts = savedPathVerts;
		_xforms = savedXforms; _xformTransient = savedTransient;
		(_basisOx, _basisOy, _basisW, _basisH) = savedBasis;
	}

	public Matrix4x4 TotalMatrix => _overlay.TotalMatrix;
	public void SetMatrix(in Matrix4x4 matrix) => _overlay.SetMatrix(matrix);
	public void Concat(in Matrix4x4 matrix) => _overlay.Concat(matrix);
	public void Translate(float dx, float dy) => _overlay.Translate(dx, dy);
	public void Scale(float sx, float sy) { _presentScale = new Vector2(_presentScale.X * sx, _presentScale.Y * sy); _overlay.Scale(sx, sy); }
	public int Save() { _presentScaleStack.Push(_presentScale); _overlay.Save(); return _presentScaleStack.Count; }
	public int SaveCount => _presentScaleStack.Count;
	public object NativeSurface => null;
	public IDrawingFactory Factory => _factory;
	public void Restore() { if (_presentScaleStack.Count > 0) { _presentScale = _presentScaleStack.Pop(); } _overlay.Restore(); }
	public void RestoreToCount(int count) { while (_presentScaleStack.Count > count) { Restore(); } }
	public void SaveLayer(bool antialias = false) => _overlay.SaveLayer(antialias);
	public void SaveLayer(IColorFilter colorFilter, bool antialias = false) => _overlay.SaveLayer(colorFilter, antialias);
	public void SaveLayerMask(bool antialias = false) => _overlay.SaveLayerMask(antialias);
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
			_d.SolidSlab.EndFrame(); _d.RrectSlab.EndFrame();   // free slices of recordings not seen this frame
			_d.SolidTableSlab.EndFrame(); _d.RrectTableSlab.EndFrame();
			_pendingCmds = null;
		}
	}
}

// --- New-SPI pluggable-backend surface (see doc/uno-drawing-backend-abstraction.md) ---

// NOTE: presentation belongs on the HOST graphics context that owns the window swapchain (it implements
// IWebGpuDeviceContext below and drives Acquire/Present); there is deliberately no device-only IGraphicsContext
// here — a device without a window has no surface to present to.
