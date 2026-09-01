// Effects rendered offscreen: shadows, the blur pyramid every blur-based effect shares, and the passes that
// implement the neutral effect tree (blend, combine, colour function, noise).
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
	// Fills the shadow silhouette into an offscreen coverage surface (stencil-then-cover, white), then blurs it
	// separably (H then V). Returns the blurred coverage texture + its device-space placement. NOTE: the per-
	// shadow textures are not pooled/freed yet — fine for offscreen/one-shot; the on-window path needs cleanup.
	private IntPtr RenderShadow(ShadowCmd sh, out Vector2 origin, out Vector2 size)
	{
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
		wgpuRenderPassEncoderSetPipeline(pass, sh.EvenOdd ? _d.StencilEvenOdd : _d.StencilNonZero);
		wgpuRenderPassEncoderSetBindGroup(pass, 0, MakeClipBg(_d.ClipBgl, default), 0, (uint*)null);   // identity xform (shadow fan already NDC)
		wgpuRenderPassEncoderSetVertexBuffer(pass, 0, fanBuf, 0, (nuint)(fanNdc.Length * sizeof(float)));
		wgpuRenderPassEncoderDraw(pass, (uint)(fanNdc.Length / 2), 1, 0, 0);
		wgpuRenderPassEncoderSetPipeline(pass, _d.CoverPipe);
		wgpuRenderPassEncoderSetBindGroup(pass, 0, noClip, 0, (uint*)null);
		wgpuRenderPassEncoderSetStencilReference(pass, 0);
		wgpuRenderPassEncoderSetVertexBuffer(pass, 0, coverBuf, 0, (nuint)(cq.Count * sizeof(float)));
		wgpuRenderPassEncoderDraw(pass, 6, 1, 0, 0);
		wgpuRenderPassEncoderEnd(pass);
		if (_d.MsaaSamples > 1) { _d.Pool.Return(cov.MsaaColorView); }   // at 1x MsaaColorView aliases cov.View (blurred next) — don't reclaim
		_d.Pool.Return(cov.DepthView);

		// 2) blur pyramid (2x downsample + separable gaussian), matching the original's 3-pass shadow blur.
		var blurred = BlurPyramid(cov.View, sw, sh2, sh.SigmaX, sh.SigmaY);
		// The coverage resolve was consumed by the pyramid's first downsample pass — re-rentable this frame.
		_d.Pool.Return(cov.View);
		return blurred;
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
			// The consumed level is safe to re-rent within the frame: passes encode sequentially into the one
			// frame encoder, so a later renter's write is ordered after this read.
			_d.Pool.Return(cur);
			cur = nx; cw = nw; ch = nh;
		}
		var hh = _d.Pool.Rent(cw, ch, 1, WGPUTextureUsage.RenderAttachment | WGPUTextureUsage.TextureBinding, WebGpuDevice.DefaultColorFormat);
		BlurPass(cur, hh, new Vector2(1f, 0f), new Vector2(1f / cw, 0f), downsample: false, Vector2.Zero, Vector2.One);
		_d.Pool.Return(cur);
		var vv = _d.Pool.Rent(cw, ch, 1, WGPUTextureUsage.RenderAttachment | WGPUTextureUsage.TextureBinding, WebGpuDevice.DefaultColorFormat);
		BlurPass(hh, vv, new Vector2(0f, 1f), new Vector2(0f, 1f / ch), downsample: false, Vector2.Zero, Vector2.One);
		_d.Pool.Return(hh);
		return vv;
	}

	// Full-source blur (shadow coverage, already bbox-sized): the region IS the whole texture.
	private IntPtr BlurPyramid(IntPtr src, int w, int h, float sigmaX, float sigmaY)
		=> BlurPyramidRegion(src, w, h, 0f, 0f, w, h, sigmaX, sigmaY);

	// Standalone blur for the effect-graph evaluator's BlurEffectNode: blur `src` and draw it (upscaled from the
	// pyramid) into this session's target surface _s. Mirrors RenderOffscreen's flow — own encoder + submit — so it
	// runs during effect setup (RenderGate is reentrant); the pooled offscreen surface is detached by the factory.
	internal void BlurInto(WebGpuTexture src, float sigmaX, float sigmaY)
	{
		lock (_d.RenderGate)
		{
			var owns = _frameEncoder == IntPtr.Zero;
			if (owns) { _frameEncoder = wgpuDeviceCreateCommandEncoder(_d.Dev, null); }
			try
			{
				var blurView = BlurPyramid(src.View, src.PixelWidth, src.PixelHeight, sigmaX, sigmaY);
				var idu = MakeUniform(WebGpuDevice.CompositeUniformBytes);
				var idc = stackalloc float[24]; idc[1] = 1f;   // params.x=0 (no colour matrix), params.y=1 (opacity)
				wgpuQueueWriteBuffer(_d.Q, idu, 0, (IntPtr)idc, 96);
				// Two entries, not three: the composite shader uses textureLoad, so its layout has no sampler.
				var e = stackalloc WGPUBindGroupEntry[2];
				e[0] = new WGPUBindGroupEntry { Binding = 0, TextureView = blurView };
				e[1] = new WGPUBindGroupEntry { Binding = 2, Buffer = idu, Offset = 0, Size = WebGpuDevice.CompositeUniformBytes };
				var bgd = new WGPUBindGroupDescriptor { Layout = _d.CompositeBgl, EntryCount = 2, Entries = e };
				var bg = _d.TrackBg(wgpuDeviceCreateBindGroup(_d.Dev, &bgd));
				var ca = new WGPURenderPassColorAttachment { DepthSlice = uint.MaxValue, View = _s.MsaaColorView, ResolveTarget = _d.MsaaSamples > 1 ? _s.View : IntPtr.Zero, LoadOp = WGPULoadOp.Clear, StoreOp = WGPUStoreOp.Store, ClearValue = default };
				var dsa = new WGPURenderPassDepthStencilAttachment { View = _s.DepthView, DepthLoadOp = WGPULoadOp.Clear, DepthStoreOp = WGPUStoreOp.Discard, DepthClearValue = 0f, StencilLoadOp = WGPULoadOp.Clear, StencilStoreOp = WGPUStoreOp.Discard, StencilClearValue = 0 };
				var rp = new WGPURenderPassDescriptor { ColorAttachmentCount = 1, ColorAttachments = &ca, DepthStencilAttachment = &dsa };
				var pass = wgpuCommandEncoderBeginRenderPass(_frameEncoder, &rp);
				wgpuRenderPassEncoderSetPipeline(pass, _d.CompositeSrcOver);
				wgpuRenderPassEncoderSetBindGroup(pass, 0, (IntPtr)bg, 0, (uint*)null);
				wgpuRenderPassEncoderDraw(pass, 3, 1, 0, 0);
				wgpuRenderPassEncoderEnd(pass);
			}
			finally
			{
				if (owns)
				{
					_d.ClipSlab.Flush();   // one queue write per dirty chunk, before the submit that reads the clips
					_d.FlushFrameSlabs();
					var cb = wgpuCommandEncoderFinish(_frameEncoder, null);
					wgpuQueueSubmit(_d.Q, 1, (IntPtr)(&cb));
					// wgpu holds its own reference until the submission completes, so both handles are dropped
					// here — otherwise every frame leaks an encoder + a command buffer into the handle table.
					wgpuCommandBufferRelease(cb);
					wgpuCommandEncoderRelease(_frameEncoder);
					_ = wgpuDevicePoll(_d.Dev, 0u, null);
					_frameEncoder = IntPtr.Zero;
				}
			}
		}
	}

	// Standalone two-texture blend for the effect-graph evaluator (BlendEffect/CompositeEffect): composites the
	// foreground over the background with `shaderMode` (CompositeBlendWgsl id) into this session's target surface.
	// Both inputs are already offscreen textures, so this is a plain fullscreen pass — no dst-copy.
	internal void BlendInto(WebGpuTexture bg, WebGpuTexture fg, int shaderMode)
	{
		lock (_d.RenderGate)
		{
			var owns = _frameEncoder == IntPtr.Zero;
			if (owns) { _frameEncoder = wgpuDeviceCreateCommandEncoder(_d.Dev, null); }
			try
			{
				var ubuf = MakeUniform(WebGpuDevice.CompositeUniformBytes);
				var uc = stackalloc float[24]; uc[1] = 1f; uc[2] = shaderMode;   // params.x=0 (no matrix), y=1 (opacity), z=mode
				wgpuQueueWriteBuffer(_d.Q, ubuf, 0, (IntPtr)uc, 96);
				var e = stackalloc WGPUBindGroupEntry[4];
				e[0] = new WGPUBindGroupEntry { Binding = 0, TextureView = fg.View };   // src = foreground
				e[1] = new WGPUBindGroupEntry { Binding = 1, Sampler = _d.Smp };
				e[2] = new WGPUBindGroupEntry { Binding = 2, Buffer = ubuf, Offset = 0, Size = WebGpuDevice.CompositeUniformBytes };
				e[3] = new WGPUBindGroupEntry { Binding = 3, TextureView = bg.View };   // dst = background
				var bgd = new WGPUBindGroupDescriptor { Layout = _d.CompositeBlendBgl, EntryCount = 4, Entries = e };
				var bgh = _d.TrackBg(wgpuDeviceCreateBindGroup(_d.Dev, &bgd));
				var ca = new WGPURenderPassColorAttachment { DepthSlice = uint.MaxValue, View = _s.MsaaColorView, ResolveTarget = _d.MsaaSamples > 1 ? _s.View : IntPtr.Zero, LoadOp = WGPULoadOp.Clear, StoreOp = WGPUStoreOp.Store, ClearValue = default };
				var dsa = new WGPURenderPassDepthStencilAttachment { View = _s.DepthView, DepthLoadOp = WGPULoadOp.Clear, DepthStoreOp = WGPUStoreOp.Discard, DepthClearValue = 0f, StencilLoadOp = WGPULoadOp.Clear, StencilStoreOp = WGPUStoreOp.Discard, StencilClearValue = 0 };
				var rp = new WGPURenderPassDescriptor { ColorAttachmentCount = 1, ColorAttachments = &ca, DepthStencilAttachment = &dsa };
				var pass = wgpuCommandEncoderBeginRenderPass(_frameEncoder, &rp);
				wgpuRenderPassEncoderSetPipeline(pass, _d.CompositeBlend);
				wgpuRenderPassEncoderSetBindGroup(pass, 0, (IntPtr)bgh, 0, (uint*)null);
				wgpuRenderPassEncoderDraw(pass, 3, 1, 0, 0);
				wgpuRenderPassEncoderEnd(pass);
			}
			finally
			{
				if (owns)
				{
					_d.ClipSlab.Flush();   // one queue write per dirty chunk, before the submit that reads the clips
					_d.FlushFrameSlabs();
					var cb = wgpuCommandEncoderFinish(_frameEncoder, null);
					wgpuQueueSubmit(_d.Q, 1, (IntPtr)(&cb));
					// wgpu holds its own reference until the submission completes, so both handles are dropped
					// here — otherwise every frame leaks an encoder + a command buffer into the handle table.
					wgpuCommandBufferRelease(cb);
					wgpuCommandEncoderRelease(_frameEncoder);
					_ = wgpuDevicePoll(_d.Dev, 0u, null);
					_frameEncoder = IntPtr.Zero;
				}
			}
		}
	}

	// Standalone two-texture combine: out = k0*A + k1*B + k2*(A*B) + k3 (premultiplied, clamped), or A masked by B's
	// alpha when alphaMask. Covers CrossFade / ArithmeticComposite / AlphaMask. Fullscreen pass into the target surface.
	internal void CombineInto(WebGpuTexture a, WebGpuTexture b, float k0, float k1, float k2, float k3, bool alphaMask)
	{
		lock (_d.RenderGate)
		{
			var owns = _frameEncoder == IntPtr.Zero;
			if (owns) { _frameEncoder = wgpuDeviceCreateCommandEncoder(_d.Dev, null); }
			try
			{
				var ubuf = MakeUniform(32);
				var uc = stackalloc float[8]; uc[0] = k0; uc[1] = k1; uc[2] = k2; uc[3] = k3; uc[4] = alphaMask ? 1f : 0f;
				wgpuQueueWriteBuffer(_d.Q, ubuf, 0, (IntPtr)uc, 32);
				var e = stackalloc WGPUBindGroupEntry[4];
				e[0] = new WGPUBindGroupEntry { Binding = 0, TextureView = a.View };
				e[1] = new WGPUBindGroupEntry { Binding = 1, Sampler = _d.Smp };
				e[2] = new WGPUBindGroupEntry { Binding = 2, Buffer = ubuf, Offset = 0, Size = 32 };
				e[3] = new WGPUBindGroupEntry { Binding = 3, TextureView = b.View };
				var bgd = new WGPUBindGroupDescriptor { Layout = _d.EffectCombineBgl, EntryCount = 4, Entries = e };
				var bgh = _d.TrackBg(wgpuDeviceCreateBindGroup(_d.Dev, &bgd));
				var ca = new WGPURenderPassColorAttachment { DepthSlice = uint.MaxValue, View = _s.MsaaColorView, ResolveTarget = _d.MsaaSamples > 1 ? _s.View : IntPtr.Zero, LoadOp = WGPULoadOp.Clear, StoreOp = WGPUStoreOp.Store, ClearValue = default };
				var dsa = new WGPURenderPassDepthStencilAttachment { View = _s.DepthView, DepthLoadOp = WGPULoadOp.Clear, DepthStoreOp = WGPUStoreOp.Discard, DepthClearValue = 0f, StencilLoadOp = WGPULoadOp.Clear, StencilStoreOp = WGPUStoreOp.Discard, StencilClearValue = 0 };
				var rp = new WGPURenderPassDescriptor { ColorAttachmentCount = 1, ColorAttachments = &ca, DepthStencilAttachment = &dsa };
				var pass = wgpuCommandEncoderBeginRenderPass(_frameEncoder, &rp);
				wgpuRenderPassEncoderSetPipeline(pass, _d.EffectCombine);
				wgpuRenderPassEncoderSetBindGroup(pass, 0, (IntPtr)bgh, 0, (uint*)null);
				wgpuRenderPassEncoderDraw(pass, 3, 1, 0, 0);
				wgpuRenderPassEncoderEnd(pass);
			}
			finally
			{
				if (owns)
				{
					_d.ClipSlab.Flush();   // one queue write per dirty chunk, before the submit that reads the clips
					_d.FlushFrameSlabs();
					var cb = wgpuCommandEncoderFinish(_frameEncoder, null);
					wgpuQueueSubmit(_d.Q, 1, (IntPtr)(&cb));
					// wgpu holds its own reference until the submission completes, so both handles are dropped
					// here — otherwise every frame leaks an encoder + a command buffer into the handle table.
					wgpuCommandBufferRelease(cb);
					wgpuCommandEncoderRelease(_frameEncoder);
					_ = wgpuDevicePoll(_d.Dev, 0u, null);
					_frameEncoder = IntPtr.Zero;
				}
			}
		}
	}

	// Standalone single-input per-channel colour function (Contrast / GammaTransfer). u20 = 20 floats = the FU uniform
	// (params, amp, exps, offs, dis — 5 vec4). Fullscreen pass into the target surface.
	internal void ColorFuncInto(WebGpuTexture src, float[] u20)
	{
		lock (_d.RenderGate)
		{
			var owns = _frameEncoder == IntPtr.Zero;
			if (owns) { _frameEncoder = wgpuDeviceCreateCommandEncoder(_d.Dev, null); }
			try
			{
				var ubuf = MakeUniform(80);
				fixed (float* p = u20) { wgpuQueueWriteBuffer(_d.Q, ubuf, 0, (IntPtr)p, 80); }
				var e = stackalloc WGPUBindGroupEntry[3];
				e[0] = new WGPUBindGroupEntry { Binding = 0, TextureView = src.View };
				e[1] = new WGPUBindGroupEntry { Binding = 1, Sampler = _d.Smp };
				e[2] = new WGPUBindGroupEntry { Binding = 2, Buffer = ubuf, Offset = 0, Size = 80 };
				var bgd = new WGPUBindGroupDescriptor { Layout = _d.ColorFuncBgl, EntryCount = 3, Entries = e };
				var bgh = _d.TrackBg(wgpuDeviceCreateBindGroup(_d.Dev, &bgd));
				var ca = new WGPURenderPassColorAttachment { DepthSlice = uint.MaxValue, View = _s.MsaaColorView, ResolveTarget = _d.MsaaSamples > 1 ? _s.View : IntPtr.Zero, LoadOp = WGPULoadOp.Clear, StoreOp = WGPUStoreOp.Store, ClearValue = default };
				var dsa = new WGPURenderPassDepthStencilAttachment { View = _s.DepthView, DepthLoadOp = WGPULoadOp.Clear, DepthStoreOp = WGPUStoreOp.Discard, DepthClearValue = 0f, StencilLoadOp = WGPULoadOp.Clear, StencilStoreOp = WGPUStoreOp.Discard, StencilClearValue = 0 };
				var rp = new WGPURenderPassDescriptor { ColorAttachmentCount = 1, ColorAttachments = &ca, DepthStencilAttachment = &dsa };
				var pass = wgpuCommandEncoderBeginRenderPass(_frameEncoder, &rp);
				wgpuRenderPassEncoderSetPipeline(pass, _d.ColorFunc);
				wgpuRenderPassEncoderSetBindGroup(pass, 0, (IntPtr)bgh, 0, (uint*)null);
				wgpuRenderPassEncoderDraw(pass, 3, 1, 0, 0);
				wgpuRenderPassEncoderEnd(pass);
			}
			finally
			{
				if (owns)
				{
					_d.ClipSlab.Flush();   // one queue write per dirty chunk, before the submit that reads the clips
					_d.FlushFrameSlabs();
					var cb = wgpuCommandEncoderFinish(_frameEncoder, null);
					wgpuQueueSubmit(_d.Q, 1, (IntPtr)(&cb));
					// wgpu holds its own reference until the submission completes, so both handles are dropped
					// here — otherwise every frame leaks an encoder + a command buffer into the handle table.
					wgpuCommandBufferRelease(cb);
					wgpuCommandEncoderRelease(_frameEncoder);
					_ = wgpuDevicePoll(_d.Dev, 0u, null);
					_frameEncoder = IntPtr.Zero;
				}
			}
		}
	}

	// Procedural WhiteNoise generator into the target surface (no input). freq/offset are the effect params; the
	// surface size feeds pixel coords so the noise field is stable regardless of the fullscreen triangle.
	internal void NoiseInto(float fx, float fy, float ox, float oy, int w, int h)
	{
		lock (_d.RenderGate)
		{
			var owns = _frameEncoder == IntPtr.Zero;
			if (owns) { _frameEncoder = wgpuDeviceCreateCommandEncoder(_d.Dev, null); }
			try
			{
				var ubuf = MakeUniform(32);
				var uc = stackalloc float[8]; uc[0] = fx; uc[1] = fy; uc[2] = ox; uc[3] = oy; uc[4] = w; uc[5] = h;
				wgpuQueueWriteBuffer(_d.Q, ubuf, 0, (IntPtr)uc, 32);
				var e = stackalloc WGPUBindGroupEntry[1];
				e[0] = new WGPUBindGroupEntry { Binding = 0, Buffer = ubuf, Offset = 0, Size = 32 };
				var bgd = new WGPUBindGroupDescriptor { Layout = _d.EffectNoiseBgl, EntryCount = 1, Entries = e };
				var bgh = _d.TrackBg(wgpuDeviceCreateBindGroup(_d.Dev, &bgd));
				var ca = new WGPURenderPassColorAttachment { DepthSlice = uint.MaxValue, View = _s.MsaaColorView, ResolveTarget = _d.MsaaSamples > 1 ? _s.View : IntPtr.Zero, LoadOp = WGPULoadOp.Clear, StoreOp = WGPUStoreOp.Store, ClearValue = default };
				var dsa = new WGPURenderPassDepthStencilAttachment { View = _s.DepthView, DepthLoadOp = WGPULoadOp.Clear, DepthStoreOp = WGPUStoreOp.Discard, DepthClearValue = 0f, StencilLoadOp = WGPULoadOp.Clear, StencilStoreOp = WGPUStoreOp.Discard, StencilClearValue = 0 };
				var rp = new WGPURenderPassDescriptor { ColorAttachmentCount = 1, ColorAttachments = &ca, DepthStencilAttachment = &dsa };
				var pass = wgpuCommandEncoderBeginRenderPass(_frameEncoder, &rp);
				wgpuRenderPassEncoderSetPipeline(pass, _d.EffectNoise);
				wgpuRenderPassEncoderSetBindGroup(pass, 0, (IntPtr)bgh, 0, (uint*)null);
				wgpuRenderPassEncoderDraw(pass, 3, 1, 0, 0);
				wgpuRenderPassEncoderEnd(pass);
			}
			finally
			{
				if (owns)
				{
					_d.ClipSlab.Flush();   // one queue write per dirty chunk, before the submit that reads the clips
					_d.FlushFrameSlabs();
					var cb = wgpuCommandEncoderFinish(_frameEncoder, null);
					wgpuQueueSubmit(_d.Q, 1, (IntPtr)(&cb));
					// wgpu holds its own reference until the submission completes, so both handles are dropped
					// here — otherwise every frame leaks an encoder + a command buffer into the handle table.
					wgpuCommandBufferRelease(cb);
					wgpuCommandEncoderRelease(_frameEncoder);
					_ = wgpuDevicePoll(_d.Dev, 0u, null);
					_frameEncoder = IntPtr.Zero;
				}
			}
		}
	}

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
		wgpuRenderPassEncoderSetPipeline(pass, _d.BlurPipe);
		wgpuRenderPassEncoderSetBindGroup(pass, 0, bg, 0, (uint*)null);
		wgpuRenderPassEncoderDraw(pass, 3, 1, 0, 0);
		wgpuRenderPassEncoderEnd(pass);
	}
}
