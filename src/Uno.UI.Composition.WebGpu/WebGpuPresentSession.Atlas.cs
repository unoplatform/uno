// The coverage atlas: rasterizes a shape's coverage mask once, then places it as a textured quad 1:1 with device
// pixels for as long as nothing about its device-space footprint changes.
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

public sealed unsafe partial class WebGpuPresentSession
{
	/// <summary>
	/// Rasterizes one fill's coverage into its atlas slot: stencil-then-cover in white into a scratch surface
	/// (multisampled, so the baked coverage is antialiased however the frame is later sampled), then a texture
	/// copy into the page. Runs during op BUILD, before the frame's render pass opens — a copy cannot be
	/// recorded inside a render pass.
	/// </summary>
	private void RasterizeAtlasEntry(PathFill pf, WebGpuPathAtlas.Slot slot, Vector2 scale)
	{
		const int SS = WebGpuDevice.MaskSuperSample;
		int sw = slot.W * SS, sh = slot.H * SS;

		// 1) Rasterize the coverage SUPERSAMPLED and single-sampled. Supersampling rather than MSAA keeps the bake
		// independent of the frame's sample count and gives 17 coverage levels instead of 5.
		// Use the SAME surface + pipeline pairing RenderShadow uses. A hand-rolled single-sample attachment set
		// rasterized geometry fine but never wrote STENCIL (verified: cover with Always and with Equal-0 both
		// filled the whole slot), so stencil-then-cover only works here against the device's own configuration.
		var surf = new WebGpuRenderSurface(_d, sw, sh, _d.Pool);

		var src = pf.FanHard ?? pf.FanDevice;
		var fan = new float[src.Length];
		for (int i = 0; i < src.Length; i += 2)
		{
			fan[i] = ((src[i] - slot.OriginX) * scale.X + 1f) / slot.W * 2f - 1f;
			fan[i + 1] = 1f - ((src[i + 1] - slot.OriginY) * scale.Y + 1f) / slot.H * 2f;
		}
		var fanBuf = MakeBuffer(fan);
		var cq = new List<float>();
		void CQ(float x, float y) { cq.Add(x); cq.Add(y); cq.Add(1f); cq.Add(1f); cq.Add(1f); cq.Add(1f); }
		CQ(-1, -1); CQ(1, -1); CQ(1, 1); CQ(-1, -1); CQ(1, 1); CQ(-1, 1);
		var coverBuf = MakeBuffer(cq.ToArray());

		var ca = new WGPURenderPassColorAttachment { DepthSlice = uint.MaxValue, View = surf.MsaaColorView, ResolveTarget = _d.MsaaSamples > 1 ? surf.View : IntPtr.Zero, LoadOp = WGPULoadOp.Clear, StoreOp = _d.MsaaSamples > 1 ? WGPUStoreOp.Discard : WGPUStoreOp.Store, ClearValue = default };
		var dsa = new WGPURenderPassDepthStencilAttachment { View = surf.DepthView, DepthLoadOp = WGPULoadOp.Clear, DepthStoreOp = WGPUStoreOp.Discard, DepthClearValue = 0f, StencilLoadOp = WGPULoadOp.Clear, StencilStoreOp = WGPUStoreOp.Discard, StencilClearValue = 0 };
		var rp = new WGPURenderPassDescriptor { ColorAttachmentCount = 1, ColorAttachments = &ca, DepthStencilAttachment = &dsa };
		var pass = wgpuCommandEncoderBeginRenderPass(_frameEncoder, &rp);
		if (pf.FanTiles)
		{
			// Already a non-overlapping triangulation: fill it directly. The ring-free twin carries no coverage.
			var cov = pf.FanHard is not null ? null : pf.FanCoverage;
			var tf = new List<float>(pf.FanDevice.Length * 3);
			for (int i = 0, v = 0; i < fan.Length; i += 2, v++)
			{
				var a = cov is null ? 1f : cov[v];
				tf.Add(fan[i]); tf.Add(fan[i + 1]); tf.Add(1f); tf.Add(1f); tf.Add(1f); tf.Add(a);
			}
			var tfBuf = MakeBuffer(tf.ToArray());
			wgpuRenderPassEncoderSetPipeline(pass, _d.MaskCoverPipe);
			wgpuRenderPassEncoderSetBindGroup(pass, 0, MakeClipBg(_d.CoverClipBgl, ClipData.None), 0, (uint*)null);
			wgpuRenderPassEncoderSetStencilReference(pass, 0);
			wgpuRenderPassEncoderSetVertexBuffer(pass, 0, tfBuf, 0, (nuint)(tf.Count * sizeof(float)));
			wgpuRenderPassEncoderDraw(pass, (uint)(fan.Length / 2), 1, 0, 0);
		}
		else
		{
			// A centroid fan self-overlaps and its union is a FATTER shape with the counters filled in, so the
			// mask needs real winding: stencil the fan, then cover through it.
			wgpuRenderPassEncoderSetPipeline(pass, pf.EvenOdd ? _d.StencilEvenOdd : _d.StencilNonZero);
			wgpuRenderPassEncoderSetBindGroup(pass, 0, MakeClipBg(_d.ClipBgl, ClipData.None), 0, (uint*)null);
			wgpuRenderPassEncoderSetVertexBuffer(pass, 0, fanBuf, 0, (nuint)(fan.Length * sizeof(float)));
			wgpuRenderPassEncoderDraw(pass, (uint)(fan.Length / 2), 1, 0, 0);
			wgpuRenderPassEncoderSetPipeline(pass, _d.CoverPipe);
			wgpuRenderPassEncoderSetBindGroup(pass, 0, MakeClipBg(_d.CoverClipBgl, ClipData.None), 0, (uint*)null);
			wgpuRenderPassEncoderSetStencilReference(pass, 0);
			wgpuRenderPassEncoderSetVertexBuffer(pass, 0, coverBuf, 0, (nuint)(cq.Count * sizeof(float)));
			wgpuRenderPassEncoderDraw(pass, 6, 1, 0, 0);
		}
		wgpuRenderPassEncoderEnd(pass);

		// 2) Box-filter it straight into the slot. Rendering INTO the atlas (viewport = slot) avoids both an MSAA
		// resolve and a texture copy, which is two fewer things to get wrong.
		var dq = new float[]
		{
			-1f, -1f, 0f, 1f,
			 1f, -1f, 1f, 1f,
			 1f,  1f, 1f, 0f,
			-1f, -1f, 0f, 1f,
			 1f,  1f, 1f, 0f,
			-1f,  1f, 0f, 0f,
		};
		var dqBuf = MakeBuffer(dq);
		var de = new WGPUBindGroupEntry { Binding = 0, TextureView = surf.View };
		var dbgd = new WGPUBindGroupDescriptor { Layout = _d.MaskDownsampleBgl, EntryCount = 1, Entries = &de };
		var dbg = _d.TrackBg(wgpuDeviceCreateBindGroup(_d.Dev, &dbgd));

		var aca = new WGPURenderPassColorAttachment { DepthSlice = uint.MaxValue, View = slot.Owner.View, ResolveTarget = IntPtr.Zero, LoadOp = WGPULoadOp.Load, StoreOp = WGPUStoreOp.Store, ClearValue = default };
		var arp = new WGPURenderPassDescriptor { ColorAttachmentCount = 1, ColorAttachments = &aca };
		var apass = wgpuCommandEncoderBeginRenderPass(_frameEncoder, &arp);
		wgpuRenderPassEncoderSetViewport(apass, slot.X, slot.Y, slot.W, slot.H, 0f, 1f);
		wgpuRenderPassEncoderSetPipeline(apass, _d.MaskDownsamplePipe);
		wgpuRenderPassEncoderSetBindGroup(apass, 0, (IntPtr)dbg, 0, (uint*)null);
		wgpuRenderPassEncoderSetVertexBuffer(apass, 0, dqBuf, 0, (nuint)(dq.Length * sizeof(float)));
		wgpuRenderPassEncoderDraw(apass, 6, 1, 0, 0);
		wgpuRenderPassEncoderEnd(apass);

		if (_d.MsaaSamples > 1) { _d.Pool.Return(surf.MsaaColorView); }
		_d.Pool.Return(surf.DepthView);
	}

	/// <summary>
	/// Emits an atlased fill as a tinted quad, or returns false to leave it on the geometry path.
	/// </summary>
	private bool TryAtlasFill(PathFill pf, List<DrawOp> ops, OwnedResources owned, Vector2 scale)
	{
		if (!TryAtlasOp(pf, owned, scale, out var op)) { return false; }
		ops.Add(op);
		return true;
	}

	/// <summary>True when any vertex carries partial coverage, i.e. the fan has an analytic AA ring baked in.</summary>
	private static bool HasAaRing(float[] coverage)
	{
		if (coverage is null) { return false; }
		for (int i = 0; i < coverage.Length; i++)
		{
			if (coverage[i] < 0.999f) { return true; }
		}
		return false;
	}


	internal static int AtlasTried, AtlasNoKey, AtlasHit, AtlasBaked, AtlasNoRoom, AtlasNoRing, ScaleBlocked;

	/// <summary>
	/// Keys and places on the op's own coordinate space, with <paramref name="scale"/> giving the extra scale the
	/// GPU applies afterwards — Vector2.One for geometry already in device space, the replay scale for an arena
	/// recording (identity-baked geometry mapped by the xform table). Getting that scale wrong bakes the mask at
	/// the wrong size, which is what broke When_ShapeVisual_ViewBox_Shape_Combinations.
	/// </summary>
	private bool TryAtlasOp(PathFill pf, OwnedResources owned, Vector2 scale, out DrawOp result)
	{
		result = default;
		if (!TryAtlasSlot(pf, owned, scale, out var slot, out var ox, out var oy)) { return false; }
		_atlasQuads.Clear();
		AppendAtlasQuad(_atlasQuads, slot, ox, oy, scale);
		result = MakeAtlasOp(pf, slot.Owner, _atlasQuads, owned);
		return true;
	}

	private readonly List<float> _atlasQuads = new();

	/// <summary>
	/// Emits a RUN of consecutive fills sharing an atlas page, a colour and a clip as ONE draw, advancing
	/// <paramref name="i"/> past them. Per-glyph geometry turns a string into N fills, and a draw apiece is far
	/// worse than the single merged run it replaces - the quads all sample one page, so they batch trivially.
	/// </summary>
	private bool TryAtlasBatch(List<WebGpuCommand> cmds, ref int i, OwnedResources owned, Vector2 scale, out DrawOp result)
	{
		result = default;
		if (cmds[i] is not PathFill first) { return false; }
		if (!TryAtlasSlot(first, owned, scale, out var slot0, out var ox0, out var oy0)) { return false; }

		_atlasQuads.Clear();
		AppendAtlasQuad(_atlasQuads, slot0, ox0, oy0, scale);
		var j = i + 1;
		while (j < cmds.Count && cmds[j] is PathFill nx
			&& nx.Color.R == first.Color.R && nx.Color.G == first.Color.G
			&& nx.Color.B == first.Color.B && nx.Color.A == first.Color.A
			&& ClipDataEquals(nx.Clip, first.Clip))
		{
			// A fill landing on ANOTHER page cannot share this draw's bind group. It stays baked, so the caller
			// picks it up next and starts a fresh batch on what is by then a cache hit.
			if (!TryAtlasSlot(nx, owned, scale, out var slotN, out var oxN, out var oyN)) { break; }
			if (!ReferenceEquals(slotN.Owner, slot0.Owner)) { break; }
			AppendAtlasQuad(_atlasQuads, slotN, oxN, oyN, scale);
			j++;
		}

		result = MakeAtlasOp(first, slot0.Owner, _atlasQuads, owned);

		i = j - 1;
		return true;
	}

	/// <summary>Resolves (baking on a miss) the atlas entry for one fill.</summary>
	private bool TryAtlasSlot(PathFill pf, OwnedResources owned, Vector2 scale, out WebGpuPathAtlas.Slot slot, out float ox, out float oy)
	{
		slot = null; ox = oy = 0;
		if (!_pathAtlas) { return false; }
		AtlasTried++;
		// A cached recording OWNS the entries it bakes and frees them when released. A per-frame op has no such
		// owner, so the ATLAS holds the reference and drops it after the entry goes idle (HoldForCache/SweepCache).
		// Per-frame ops must be able to BAKE, not only hit: restricting them to hits looks safer (nothing owns
		// the entry) but renders identical content crisp through the retained path and tessellated through the
		// command-list fallback.
		bool hitOnly = owned is null;
		// The bake derives coverage from a 4x supersample, so its input must be a HARD silhouette. Geometry that
		// already carries an analytic AA ring would be antialiased twice - the edge spreads half a pixel and a
		// boundary pixel that should be empty comes out at 50%.
		if (pf.FanHard is null && HasAaRing(pf.FanCoverage)) { AtlasNoRing++; return false; }
		if (!WebGpuPathAtlas.TryKey(pf.Geometry, pf.GeomMatrix, pf.BbMin, pf.BbMax, scale, out var key, out var w, out var h, out ox, out oy)) { AtlasNoKey++; return false; }

		if (_d.PathAtlas.Pages.Count == 0) { _d.AddPathAtlasPage(); }
		if (_d.PathAtlas.TryGet(key, out slot))
		{
			AtlasHit++;
			_d.PathAtlas.NoteUse(slot, _d.FrameSeq);
			// A REUSED entry needs its own reference for this recording. One slot backs a glyph everywhere it
			// appears, so without this the recording that baked it releases the region out from under every other
			// holder and they start sampling whatever took its place.
			if (!hitOnly)
			{
				_d.PathAtlas.Retain(slot);
				(owned.AtlasSlots ??= new()).Add(slot);
			}
		}
		else
		{
			slot = _d.PathAtlas.Allocate(key, w, h, ox, oy);
			if (slot is null)
			{
				// Every page is exhausted: open another rather than falling back, which would leave this shape
				// aliased at one sample while its neighbours stayed crisp.
				_d.AddPathAtlasPage();
				slot = _d.PathAtlas.Allocate(key, w, h, ox, oy);
			}
			if (slot is null) { AtlasNoRoom++; return false; }
			if (owned is not null) { (owned.AtlasSlots ??= new()).Add(slot); }
			else { _d.PathAtlas.HoldForCache(slot, _d.FrameSeq); }
			RasterizeAtlasEntry(pf, slot, scale);
			AtlasBaked++;
		}

		return true;
	}

	/// <summary>Appends one entry as 6 vertices (pos.xy, uv.xy), placed at the fill's OWN origin.</summary>
	private void AppendAtlasQuad(List<float> dst, WebGpuPathAtlas.Slot slot, float ox, float oy, Vector2 scale)
	{
		// The quad lives in the op's own space; one device pixel is 1/scale there, so a slot.W-wide mask needs a
		// slot.W/scale-wide quad to land 1:1 after the replay scale is applied on the GPU. Placed at this fill's
		// origin rather than the slot's: on a cache hit the same shape elsewhere draws at its own position, and
		// the subpixel phase is part of the key, so the mask already suits it.
		float x0 = ox - 1f / scale.X, y0 = oy - 1f / scale.Y;
		float x1 = x0 + slot.W / scale.X, y1 = y0 + slot.H / scale.Y;
		float u0 = (float)slot.X / WebGpuPathAtlas.Size, v0 = (float)slot.Y / WebGpuPathAtlas.Size;
		float u1 = (float)(slot.X + slot.W) / WebGpuPathAtlas.Size, v1 = (float)(slot.Y + slot.H) / WebGpuPathAtlas.Size;
		void QV(float x, float y, float uu, float vv) { var n = Ndc(new Vector2(x, y)); dst.Add(n.X); dst.Add(n.Y); dst.Add(uu); dst.Add(vv); }
		QV(x0, y0, u0, v0); QV(x1, y0, u1, v0); QV(x1, y1, u1, v1);
		QV(x0, y0, u0, v0); QV(x1, y1, u1, v1); QV(x0, y1, u0, v1);
	}

	/// <summary>
	/// Six vertices (pos.xy in NDC, uv.xy) for an axis-aligned textured quad covering the device rect at
	/// <paramref name="origin"/>, sampling the whole texture.
	/// </summary>
	private float[] TexturedQuad(Vector2 origin, Vector2 size)
	{
		var q = new float[24];
		void V(int i, Vector2 pos, float u, float v)
		{
			var n = Ndc(pos);
			q[i] = n.X; q[i + 1] = n.Y; q[i + 2] = u; q[i + 3] = v;
		}

		var tr = origin + new Vector2(size.X, 0);
		var br = origin + size;
		var bl = origin + new Vector2(0, size.Y);
		V(0, origin, 0, 0); V(4, tr, 1, 0); V(8, br, 1, 1);
		V(12, origin, 0, 0); V(16, br, 1, 1); V(20, bl, 0, 1);
		return q;
	}

	/// <summary>
	/// Bind group for a SrcIn-tinted image draw: the texture carries coverage in its alpha and the colour comes
	/// from <paramref name="tint"/> (see ImageWgsl op.y). Pass <paramref name="owned"/> for a cached recording so
	/// the uniform is persistent - a per-frame one would be recycled and replay in another element's colour.
	/// </summary>
	private IntPtr TintedImageBg(IntPtr view, WColor tint, OwnedResources owned = null)
	{
		var ubuf = Ubuf(112, owned);
		var u = stackalloc float[8];
		u[0] = 1f; u[1] = 1f;
		u[4] = tint.R / 255f; u[5] = tint.G / 255f; u[6] = tint.B / 255f; u[7] = tint.A / 255f;
		wgpuQueueWriteBuffer(_d.Q, ubuf, 0, (IntPtr)u, 32);
		var e = stackalloc WGPUBindGroupEntry[3];
		e[0] = new WGPUBindGroupEntry { Binding = 0, TextureView = view };
		e[1] = new WGPUBindGroupEntry { Binding = 1, Sampler = _d.Smp };
		e[2] = new WGPUBindGroupEntry { Binding = 2, Buffer = ubuf, Offset = 0, Size = 112 };
		var bgd = new WGPUBindGroupDescriptor { Layout = _d.ImgBgl, EntryCount = 3, Entries = e };
		return Bg(ref bgd, owned);
	}

	/// <summary>One draw for a batch of quads sharing a page, a colour and a clip.</summary>
	private DrawOp MakeAtlasOp(PathFill pf, WebGpuPathAtlas.Page page, List<float> quads, OwnedResources owned)
	{
		var bg = TintedImageBg(page.View, pf.Color, owned);
		return new DrawOp(DrawKind.Image, (nint)bg, (uint)(quads.Count / 4), (nint)Vbuf(quads, owned), false, pf.Clip, (nint)MakeClipBg(_d.ImageClipBgl, pf.Clip, owned));
	}
}
