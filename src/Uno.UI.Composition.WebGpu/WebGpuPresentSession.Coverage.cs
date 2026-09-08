// Signed-area coverage fills: rasterize a path's exact per-pixel coverage into a mask, then draw the mask.
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
	// UNO_WEBGPU_COVERAGE=1 gives the coverage rasterizer the fills that would otherwise render aliased -- the
	// ones the tessellator refused, which fall through to stencil-then-cover. =all takes every fill instead, so
	// the two rasterizers can be compared on the same scene.
	private static readonly string _coverageMode = Environment.GetEnvironmentVariable("UNO_WEBGPU_COVERAGE");
	private static readonly bool _coverageFills = _coverageMode is "1" or "true" or "all";
	private static readonly bool _coverageAll = _coverageMode is "all";

	/// <summary>Bound so a pathological shape cannot allocate a mask larger than a page-sized texture.</summary>
	private const int CoverageMaxDim = 4096;

	/// <summary>
	/// Rasterizes <paramref name="pf"/>'s coverage into a pooled mask and emits the quad that samples it, or
	/// returns false when the shape has no usable edge list or is too large to mask.
	/// <para>
	/// Runs during op BUILD, before the frame's render pass opens: the accumulation is its own pass, and a pass
	/// cannot be nested inside another.
	/// </para>
	/// </summary>
	private bool TryCoverageFill(PathFill pf, OwnedResources owned, out DrawOp op)
	{
		op = default;
		if (pf.Edges is not { Length: >= 12 } edges)
		{
			return false;
		}

		// The mask is grown by a pixel on each side: an edge exactly on the bbox boundary still needs the pixel
		// it partially covers, and the accumulation reads floor()/ceil() of edge positions.
		var x0 = MathF.Floor(pf.BbMin.X) - 1f;
		var y0 = MathF.Floor(pf.BbMin.Y) - 1f;
		var w = (int)(MathF.Ceiling(pf.BbMax.X) + 1f - x0);
		var h = (int)(MathF.Ceiling(pf.BbMax.Y) + 1f - y0);
		if (w <= 0 || h <= 0 || w > CoverageMaxDim || h > CoverageMaxDim)
		{
			return false;
		}

		// Edges into mask-local pixel space. The rasterizer works in pixels, not NDC: coverage is an area per
		// pixel, so the arithmetic has to be in the units the answer is expressed in.
		var local = new float[edges.Length];
		for (var i = 0; i < edges.Length; i += 2)
		{
			local[i] = edges[i] - x0;
			local[i + 1] = edges[i + 1] - y0;
		}

		var edgeBuf = _d.BufferPool.Rent(local.Length * sizeof(float), WGPUBufferUsage.Storage | WGPUBufferUsage.CopyDst);
		fixed (float* p = local) { wgpuQueueWriteBuffer(_d.Q, edgeBuf, 0, (IntPtr)p, (nuint)(local.Length * sizeof(float))); }

		var sizeBuf = _d.BufferPool.Rent(16, WGPUBufferUsage.Uniform | WGPUBufferUsage.CopyDst);
		var size = stackalloc float[4];
		size[0] = w; size[1] = h;
		wgpuQueueWriteBuffer(_d.Q, sizeBuf, 0, (IntPtr)size, 16);

		var maskView = _d.Pool.Rent(w, h, 1, WGPUTextureUsage.RenderAttachment | WGPUTextureUsage.TextureBinding, WebGpuDevice.CoverageFormat);

		var ae = stackalloc WGPUBindGroupEntry[2];
		ae[0] = new WGPUBindGroupEntry { Binding = 0, Buffer = edgeBuf, Offset = 0, Size = (nuint)(local.Length * sizeof(float)) };
		ae[1] = new WGPUBindGroupEntry { Binding = 1, Buffer = sizeBuf, Offset = 0, Size = 16 };
		var abgd = new WGPUBindGroupDescriptor { Layout = _d.CoverageBgl, EntryCount = 2, Entries = ae };
		var accumBg = Bg(ref abgd, owned);

		// Cleared to zero: the accumulator starts at "no area", and every edge adds to it.
		var color = new WGPURenderPassColorAttachment
		{
			DepthSlice = uint.MaxValue,
			View = maskView,
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

		// The draw: premultiplied colour times coverage, over the mask's rect, with mask texels addressed
		// directly so no sampler and no filtering are involved.
		var a = pf.Color.A / 255f;
		var du = _d.BufferPool.Rent(32, WGPUBufferUsage.Uniform | WGPUBufferUsage.CopyDst);
		var dp = stackalloc float[8];
		dp[0] = pf.Color.R / 255f * a; dp[1] = pf.Color.G / 255f * a; dp[2] = pf.Color.B / 255f * a; dp[3] = a;
		dp[4] = pf.EvenOdd ? 1f : 0f;
		wgpuQueueWriteBuffer(_d.Q, du, 0, (IntPtr)dp, 32);

		var de = stackalloc WGPUBindGroupEntry[2];
		de[0] = new WGPUBindGroupEntry { Binding = 0, TextureView = maskView };
		de[1] = new WGPUBindGroupEntry { Binding = 1, Buffer = du, Offset = 0, Size = 32 };
		var dbgd = new WGPUBindGroupDescriptor { Layout = _d.CoverageDrawBgl, EntryCount = 2, Entries = de };
		var drawBg = Bg(ref dbgd, owned);

		var q = new float[24];
		void V(int i, float px, float py, float tx, float ty)
		{
			var n = Ndc(new Vector2(px, py));
			q[i] = n.X; q[i + 1] = n.Y; q[i + 2] = tx; q[i + 3] = ty;
		}

		float x1 = x0 + w, y1 = y0 + h;
		V(0, x0, y0, 0, 0); V(4, x1, y0, w, 0); V(8, x1, y1, w, h);
		V(12, x0, y0, 0, 0); V(16, x1, y1, w, h); V(20, x0, y1, 0, h);

		op = new DrawOp(DrawKind.Coverage, (nint)drawBg, 6, (nint)Vbuf(q, owned), false, pf.Clip, (nint)MakeClipBg(_d.ImageClipBgl, pf.Clip, owned));
		return true;
	}
}
