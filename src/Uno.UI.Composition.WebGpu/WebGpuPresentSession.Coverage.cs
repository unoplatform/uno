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
}
