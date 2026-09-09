// Signed-area coverage: rasterizes a path's exact per-pixel coverage analytically, as an alternative producer
// for the atlas masks that RasterizeAtlasEntry otherwise bakes by supersampling a triangulated silhouette.
#nullable disable
using System;
using System.Collections.Generic;
using System.Numerics;
using Uno.WebGpu.Native;
using static Uno.WebGpu.Native.WGPU;
using Uno.UI.Composition.Drawing;
using Windows.Foundation;
using WColor = Windows.UI.Color;

namespace Uno.UI.Composition.WebGpu;

public sealed unsafe partial class WebGpuPresentSession
{
	// UNO_WEBGPU_COVERAGE=1 bakes atlas masks by accumulating signed edge area instead of supersampling a
	// triangulated silhouette, which also lets shapes the tessellator refused into the atlas at all.
	private static readonly string _coverageMode = Environment.GetEnvironmentVariable("UNO_WEBGPU_COVERAGE");
	internal static readonly bool _coverageFills = _coverageMode is "1" or "true";

	/// <summary>True when <paramref name="pf"/> can be baked by accumulating its edges rather than its fan.</summary>
	internal static bool CanCoverageBake(PathFill pf) => _coverageFills && pf.Edges is { Length: >= 12 };

	/// <summary>
	/// Bakes <paramref name="slot"/>'s mask from the fill's edge list: accumulate signed area per pixel into a
	/// scratch target, then resolve that into the slot. Needs no triangulation and no hard silhouette -- the edges
	/// ARE the outline -- so it is exact rather than quantised to the supersample's levels.
	/// <para>
	/// Runs during op BUILD, before the frame's render pass opens: each step is its own pass, and a pass cannot be
	/// nested inside another. Only the slot outlives the frame, and the atlas owns it.
	/// </para>
	/// </summary>
	private void RasterizeAtlasEntryCoverage(PathFill pf, WebGpuPathAtlas.Slot slot, Vector2 scale)
	{
		var edges = pf.Edges;

		// Mask-local pixel space on the same placement AppendAtlasQuad draws with: the fill's own origin, shifted a
		// pixel so an edge sitting exactly on the bbox boundary still has the pixel it partly covers.
		var local = new float[edges.Length];
		for (var i = 0; i < edges.Length; i += 2)
		{
			local[i] = (edges[i] - slot.OriginX) * scale.X + 1f;
			local[i + 1] = (edges[i + 1] - slot.OriginY) * scale.Y + 1f;
		}

		var edgeBuf = _d.BufferPool.Rent(local.Length * sizeof(float), WGPUBufferUsage.Storage | WGPUBufferUsage.CopyDst);
		fixed (float* p = local) { wgpuQueueWriteBuffer(_d.Q, edgeBuf, 0, (IntPtr)p, (nuint)(local.Length * sizeof(float))); }

		var sizeBuf = _d.BufferPool.Rent(16, WGPUBufferUsage.Uniform | WGPUBufferUsage.CopyDst);
		var size = stackalloc float[4];
		size[0] = slot.W; size[1] = slot.H;
		wgpuQueueWriteBuffer(_d.Q, sizeBuf, 0, (IntPtr)size, 16);

		var accView = _d.Pool.Rent(slot.W, slot.H, 1, WGPUTextureUsage.RenderAttachment | WGPUTextureUsage.TextureBinding, WebGpuDevice.CoverageFormat);

		var ae = stackalloc WGPUBindGroupEntry[2];
		ae[0] = new WGPUBindGroupEntry { Binding = 0, Buffer = edgeBuf, Offset = 0, Size = (nuint)(local.Length * sizeof(float)) };
		ae[1] = new WGPUBindGroupEntry { Binding = 1, Buffer = sizeBuf, Offset = 0, Size = 16 };
		var abgd = new WGPUBindGroupDescriptor { Layout = _d.CoverageBgl, EntryCount = 2, Entries = ae };
		var accumBg = _d.TrackBg(wgpuDeviceCreateBindGroup(_d.Dev, &abgd));

		// Cleared to zero: the accumulator starts at "no area", and every edge adds its contribution.
		var color = new WGPURenderPassColorAttachment
		{
			DepthSlice = uint.MaxValue,
			View = accView,
			LoadOp = WGPULoadOp.Clear,
			StoreOp = WGPUStoreOp.Store,
			ClearValue = new WGPUColor { R = 0, G = 0, B = 0, A = 0 },
		};
		var desc = new WGPURenderPassDescriptor { ColorAttachmentCount = 1, ColorAttachments = &color };
		var pass = wgpuCommandEncoderBeginRenderPass(_frameEncoder, &desc);
		wgpuRenderPassEncoderSetPipeline(pass, _d.CoveragePipe);
		wgpuRenderPassEncoderSetBindGroup(pass, 0, (IntPtr)accumBg, 0, (uint*)null);
		wgpuRenderPassEncoderDraw(pass, (uint)(local.Length / 4 * 6), 1, 0, 0);
		wgpuRenderPassEncoderEnd(pass);

		// Resolve into the slot the way the supersampled bake's downsample does (viewport = slot, Load so the rest
		// of the page survives), so the sampling side cannot tell the two producers apart.
		var ru = _d.BufferPool.Rent(16, WGPUBufferUsage.Uniform | WGPUBufferUsage.CopyDst);
		var rp = stackalloc float[4];
		rp[0] = pf.EvenOdd ? 1f : 0f;
		wgpuQueueWriteBuffer(_d.Q, ru, 0, (IntPtr)rp, 16);

		var re = stackalloc WGPUBindGroupEntry[2];
		re[0] = new WGPUBindGroupEntry { Binding = 0, TextureView = accView };
		re[1] = new WGPUBindGroupEntry { Binding = 1, Buffer = ru, Offset = 0, Size = 16 };
		var rbgd = new WGPUBindGroupDescriptor { Layout = _d.CoverageResolveBgl, EntryCount = 2, Entries = re };
		var resolveBg = _d.TrackBg(wgpuDeviceCreateBindGroup(_d.Dev, &rbgd));

		// Row 0 of the accumulator is the TOP row (its vertex shader already maps pixel y down from NDC +1), and
		// NDC +1 is the top of the slot viewport -- so v runs opposite to y, exactly as the supersampled bake's
		// downsample quad does. Matching orientations by eye is not possible here: a flipped mask still fills
		// roughly the right pixels, so it reads as bad antialiasing rather than as an upside-down glyph.
		var q = new float[]
		{
			-1f, -1f, 0f, slot.H,
			 1f, -1f, slot.W, slot.H,
			 1f,  1f, slot.W, 0f,
			-1f, -1f, 0f, slot.H,
			 1f,  1f, slot.W, 0f,
			-1f,  1f, 0f, 0f,
		};
		var qBuf = MakeBuffer(q);

		var rca = new WGPURenderPassColorAttachment { DepthSlice = uint.MaxValue, View = slot.Owner.View, ResolveTarget = IntPtr.Zero, LoadOp = WGPULoadOp.Load, StoreOp = WGPUStoreOp.Store, ClearValue = default };
		var rrp = new WGPURenderPassDescriptor { ColorAttachmentCount = 1, ColorAttachments = &rca };
		var rpass = wgpuCommandEncoderBeginRenderPass(_frameEncoder, &rrp);
		wgpuRenderPassEncoderSetViewport(rpass, slot.X, slot.Y, slot.W, slot.H, 0f, 1f);
		wgpuRenderPassEncoderSetPipeline(rpass, _d.CoverageResolvePipe);
		wgpuRenderPassEncoderSetBindGroup(rpass, 0, (IntPtr)resolveBg, 0, (uint*)null);
		wgpuRenderPassEncoderSetVertexBuffer(rpass, 0, qBuf, 0, (nuint)(q.Length * sizeof(float)));
		wgpuRenderPassEncoderDraw(rpass, 6, 1, 0, 0);
		wgpuRenderPassEncoderEnd(rpass);

		_d.Pool.Return(accView);
	}

	// UNO_WEBGPU_COVERAGE_CLIPS=0 puts path clips back on the binary depth mask.
	private static readonly bool _coverageClips = Environment.GetEnvironmentVariable("UNO_WEBGPU_COVERAGE_CLIPS") is not ("0" or "false");
	internal static int ClipMasksBaked;

	/// <summary>A baked path-clip mask: the texture and the device pixel its texel (0,0) sits on.</summary>
	internal struct ClipMask { public IntPtr View; public int OriginX, OriginY; }

	// Per frame. Keyed on the composed path list AND the owning bag: the list is shared by every command under
	// one clip (ClipCompose memoizes it), so a clip is baked once per frame; the bag is part of the key because a
	// texture that lives in bag A must not be sampled by a group cached in bag B, which A's release would strand.
	// Immediate (unowned) requests share one entry per list and are released at the next frame start.
	private readonly Dictionary<(PathClip[], OwnedResources), ClipMask> _clipMasks = new();

	/// <summary>A path clip still applied through the depth mask: coverage clips off, no edge list, or a stamped session clip.</summary>
	private static bool UsesDepthFan(in ClipData c) => c.PathFan is not null && (!_coverageClips || c.DepthFanOnly || c.Paths is null);

	private static bool UsesMask(in ClipData c) => _coverageClips && c.Paths is { Length: > 0 } && !c.DepthFanOnly;

	// Intersect paths bound the visible region, so the mask covers their boxes' intersection; an Exclude keeps the
	// outside and bounds nothing, so with only Excludes the mask spans the surface. Outside the texture coverage
	// reads 0. Grown a pixel each side like the fill bake, and never empty: an empty intersection bakes a 1x1 mask
	// whose texel accumulates ~0, which still clips.
	private void ClipMaskRect(PathClip[] paths, out int ox, out int oy, out int w, out int h)
	{
		float l = 0f, t = 0f, r = _s.Width, b = _s.Height;
		foreach (var p in paths)
		{
			if (p.Exclude) { continue; }
			l = MathF.Max(l, p.Bbox.X); t = MathF.Max(t, p.Bbox.Y); r = MathF.Min(r, p.Bbox.Z); b = MathF.Min(b, p.Bbox.W);
		}
		ox = (int)MathF.Floor(l) - 1; oy = (int)MathF.Floor(t) - 1;
		w = Math.Clamp((int)MathF.Ceiling(r) + 1 - ox, 1, 4096);
		h = Math.Clamp((int)MathF.Ceiling(b) + 1 - oy, 1, 4096);
	}

	/// <summary>
	/// The coverage mask for a clip's path list, baking it on first use this frame: each path's signed area is
	/// accumulated into a scratch target and resolved (fill rule, Difference) into the mask with a multiply, so the
	/// mask is the product of every path's coverage and nesting depth is unbounded. Runs during op BUILD.
	/// </summary>
	private ClipMask ResolveClipMask(in ClipData cd, OwnedResources owned)
	{
		if (!UsesMask(cd)) { return default; }
		var key = (cd.Paths, owned);
		if (_clipMasks.TryGetValue(key, out var cached)) { return cached; }

		ClipMaskRect(cd.Paths, out var ox, out var oy, out var w, out var h);
		var td = new WGPUTextureDescriptor
		{
			Size = new WGPUExtent3D { Width = (uint)w, Height = (uint)h, DepthOrArrayLayers = 1 },
			// The device's swapchain format, which is what the resolve pipelines target -- not DefaultColorFormat.
			Format = _d.ColorFormat, MipLevelCount = 1, SampleCount = 1, Dimension = WGPUTextureDimension._2D,
			Usage = WGPUTextureUsage.RenderAttachment | WGPUTextureUsage.TextureBinding,
		};
		var tex = wgpuDeviceCreateTexture(_d.Dev, &td);
		var view = wgpuTextureCreateView(tex, null);

		// Full-target quad; row 0 of the accumulator is the top, so v runs opposite to y (see the fill bake).
		var q = new float[]
		{
			-1f, -1f, 0f, h,
			 1f, -1f, w, h,
			 1f,  1f, w, 0f,
			-1f, -1f, 0f, h,
			 1f,  1f, w, 0f,
			-1f,  1f, 0f, 0f,
		};
		var qBuf = MakeBuffer(q);
		var sizeBuf = _d.BufferPool.Rent(16, WGPUBufferUsage.Uniform | WGPUBufferUsage.CopyDst);
		var size = stackalloc float[4];
		size[0] = w; size[1] = h;
		wgpuQueueWriteBuffer(_d.Q, sizeBuf, 0, (IntPtr)size, 16);

		for (var pi = 0; pi < cd.Paths.Length; pi++)
		{
			var path = cd.Paths[pi];
			var edges = path.Edges;
			var local = new float[edges.Length];
			for (var i = 0; i < edges.Length; i += 2) { local[i] = edges[i] - ox; local[i + 1] = edges[i + 1] - oy; }
			var edgeBuf = _d.BufferPool.Rent(local.Length * sizeof(float), WGPUBufferUsage.Storage | WGPUBufferUsage.CopyDst);
			fixed (float* p = local) { wgpuQueueWriteBuffer(_d.Q, edgeBuf, 0, (IntPtr)p, (nuint)(local.Length * sizeof(float))); }

			var accView = _d.Pool.Rent(w, h, 1, WGPUTextureUsage.RenderAttachment | WGPUTextureUsage.TextureBinding, WebGpuDevice.CoverageFormat);
			var ae = stackalloc WGPUBindGroupEntry[2];
			ae[0] = new WGPUBindGroupEntry { Binding = 0, Buffer = edgeBuf, Offset = 0, Size = (nuint)(local.Length * sizeof(float)) };
			ae[1] = new WGPUBindGroupEntry { Binding = 1, Buffer = sizeBuf, Offset = 0, Size = 16 };
			var abgd = new WGPUBindGroupDescriptor { Layout = _d.CoverageBgl, EntryCount = 2, Entries = ae };
			var accumBg = _d.TrackBg(wgpuDeviceCreateBindGroup(_d.Dev, &abgd));
			var acc = new WGPURenderPassColorAttachment { DepthSlice = uint.MaxValue, View = accView, LoadOp = WGPULoadOp.Clear, StoreOp = WGPUStoreOp.Store, ClearValue = new WGPUColor { R = 0, G = 0, B = 0, A = 0 } };
			var adesc = new WGPURenderPassDescriptor { ColorAttachmentCount = 1, ColorAttachments = &acc };
			var apass = wgpuCommandEncoderBeginRenderPass(_frameEncoder, &adesc);
			wgpuRenderPassEncoderSetPipeline(apass, _d.CoveragePipe);
			wgpuRenderPassEncoderSetBindGroup(apass, 0, (IntPtr)accumBg, 0, (uint*)null);
			wgpuRenderPassEncoderDraw(apass, (uint)(local.Length / 4 * 6), 1, 0, 0);
			wgpuRenderPassEncoderEnd(apass);

			var ru = _d.BufferPool.Rent(16, WGPUBufferUsage.Uniform | WGPUBufferUsage.CopyDst);
			var rp = stackalloc float[4];
			rp[0] = path.EvenOdd ? 1f : 0f; rp[1] = path.Exclude ? 1f : 0f;
			wgpuQueueWriteBuffer(_d.Q, ru, 0, (IntPtr)rp, 16);
			var re = stackalloc WGPUBindGroupEntry[2];
			re[0] = new WGPUBindGroupEntry { Binding = 0, TextureView = accView };
			re[1] = new WGPUBindGroupEntry { Binding = 1, Buffer = ru, Offset = 0, Size = 16 };
			var rbgd = new WGPUBindGroupDescriptor { Layout = _d.CoverageResolveBgl, EntryCount = 2, Entries = re };
			var resolveBg = _d.TrackBg(wgpuDeviceCreateBindGroup(_d.Dev, &rbgd));
			// The first path clears the mask to 1 and multiplies into it; each later one multiplies into the result.
			var rca = new WGPURenderPassColorAttachment { DepthSlice = uint.MaxValue, View = view, LoadOp = pi == 0 ? WGPULoadOp.Clear : WGPULoadOp.Load, StoreOp = WGPUStoreOp.Store, ClearValue = new WGPUColor { R = 1, G = 1, B = 1, A = 1 } };
			var rdesc = new WGPURenderPassDescriptor { ColorAttachmentCount = 1, ColorAttachments = &rca };
			var rpass = wgpuCommandEncoderBeginRenderPass(_frameEncoder, &rdesc);
			wgpuRenderPassEncoderSetPipeline(rpass, _d.CoverageResolveMulPipe);
			wgpuRenderPassEncoderSetBindGroup(rpass, 0, (IntPtr)resolveBg, 0, (uint*)null);
			wgpuRenderPassEncoderSetVertexBuffer(rpass, 0, qBuf, 0, (nuint)(q.Length * sizeof(float)));
			wgpuRenderPassEncoderDraw(rpass, 6, 1, 0, 0);
			wgpuRenderPassEncoderEnd(rpass);
			_d.Pool.Return(accView);
		}

		if (owned is not null) { (owned.Textures ??= new()).Add(((nint)view, (nint)tex)); }
		else { _d.DeferTextureRelease(view, tex); }
		var mask = new ClipMask { View = view, OriginX = ox, OriginY = oy };
		_clipMasks[key] = mask;
		ClipMasksBaked++;
		return mask;
	}
}
