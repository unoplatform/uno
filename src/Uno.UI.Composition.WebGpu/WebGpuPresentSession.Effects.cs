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
	// Bakes the shadow silhouette's coverage into a padded offscreen mask, then blurs it. Returns the blurred
	// coverage texture + its device-space placement; the mask itself is released at the end of the frame.
	private IntPtr RenderShadow(ShadowCmd sh, out Vector2 origin, out Vector2 size)
	{
		float pad = MathF.Ceiling(3f * MathF.Max(sh.SigmaX, sh.SigmaY)) + 2f;
		int ox = (int)MathF.Floor(sh.BbMin.X - pad), oy = (int)MathF.Floor(sh.BbMin.Y - pad);
		int sw = Math.Clamp((int)MathF.Ceiling(sh.BbMax.X + pad) - ox, 1, 4096);
		int sh2 = Math.Clamp((int)MathF.Ceiling(sh.BbMax.Y + pad) - oy, 1, 4096);
		origin = new Vector2(ox, oy);
		size = new Vector2(sw, sh2);

		var path = new PathClip { Edges = sh.Edges, EvenOdd = sh.EvenOdd, Bbox = new Vector4(sh.BbMin.X, sh.BbMin.Y, sh.BbMax.X, sh.BbMax.Y) };
		var (covView, covTex) = BakeCoverageMask(new[] { path }, ox, oy, sw, sh2, Vector2.One);
		var blurred = BlurPyramid(covView, sw, sh2, sh.SigmaX, sh.SigmaY);
		_d.DeferTextureRelease(covView, covTex);
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

	/// <summary>
	/// Opens the frame's command encoder unless an outer render already owns one. The effect entry points below run
	/// during effect setup, which can happen inside a frame or on its own, so each must submit only what it opened.
	/// </summary>
	/// <returns>True when this caller opened the encoder and so must end it.</returns>
	private bool BeginOwnedFrameEncoder()
	{
		var owns = _frameEncoder == IntPtr.Zero;
		if (owns) { _frameEncoder = wgpuDeviceCreateCommandEncoder(_d.Dev, null); _clipMasks.Clear(); }
		return owns;
	}

	/// <summary>Submits and releases the frame encoder, if this caller was the one that opened it.</summary>
	private void EndOwnedFrameEncoder(bool owns)
	{
		if (!owns)
		{
			return;
		}

		_d.ClipSlab.Flush();   // one queue write per dirty chunk, before the submit that reads the clips
		_d.FlushFrameSlabs();
		var cb = wgpuCommandEncoderFinish(_frameEncoder, null);
		wgpuQueueSubmit(_d.Q, 1, (IntPtr)(&cb));
		// wgpu holds its own reference until the submission completes, so both handles are dropped here —
		// otherwise every frame leaks an encoder + a command buffer into the handle table.
		wgpuCommandBufferRelease(cb);
		wgpuCommandEncoderRelease(_frameEncoder);
		_ = wgpuDevicePoll(_d.Dev, 0u, null);
		_frameEncoder = IntPtr.Zero;
	}

	/// <summary>
	/// Draws one fullscreen effect pass into this session's target surface: opens a pass, draws the three-vertex
	/// covering triangle with the given pipeline and bind group, and ends the pass. The effect shaders emit the
	/// finished pixel, so the pass clears rather than loads.
	/// </summary>
	private void DrawEffectPass(IntPtr pipeline, IntPtr bindGroup)
	{
		var color = new WGPURenderPassColorAttachment { DepthSlice = uint.MaxValue, View = _s.MsaaColorView, ResolveTarget = _d.MsaaSamples > 1 ? _s.View : IntPtr.Zero, LoadOp = WGPULoadOp.Clear, StoreOp = WGPUStoreOp.Store, ClearValue = default };
		var desc = new WGPURenderPassDescriptor { ColorAttachmentCount = 1, ColorAttachments = &color };
		var pass = wgpuCommandEncoderBeginRenderPass(_frameEncoder, &desc);
		wgpuRenderPassEncoderSetPipeline(pass, pipeline);
		wgpuRenderPassEncoderSetBindGroup(pass, 0, bindGroup, 0, (uint*)null);
		wgpuRenderPassEncoderDraw(pass, 3, 1, 0, 0);
		wgpuRenderPassEncoderEnd(pass);
	}

	// Standalone blur for the effect-graph evaluator's BlurEffectNode: blur `src` and draw it (upscaled from the
	// pyramid) into this session's target surface _s. Mirrors RenderOffscreen's flow — own encoder + submit — so it
	// runs during effect setup (RenderGate is reentrant); the pooled offscreen surface is detached by the factory.
	internal void BlurInto(WebGpuTexture src, float sigmaX, float sigmaY)
	{
		lock (_d.RenderGate)
		{
			var owns = BeginOwnedFrameEncoder();
			try
			{
				var blurView = BlurPyramid(src.View, src.PixelWidth, src.PixelHeight, sigmaX, sigmaY);
				// The pyramid hands back its REDUCED top level and leaves the upscale to the caller, but the
				// composite below fetches exact texels -- so it read only the top-left (w >> levels) corner and
				// returned nothing for the rest, shrinking the blurred output as sigma (and so the level count)
				// grew. One linear tap over the full target resamples it back up first.
				var upscaled = _d.Pool.Rent(src.PixelWidth, src.PixelHeight, 1,
					WGPUTextureUsage.RenderAttachment | WGPUTextureUsage.TextureBinding, WebGpuDevice.DefaultColorFormat);
				BlurPass(blurView, upscaled, default, default, downsample: true, Vector2.Zero, Vector2.One);
				var idu = MakeUniform(WebGpuDevice.CompositeUniformBytes);
				var idc = stackalloc float[24]; idc[1] = 1f;   // params.x=0 (no colour matrix), params.y=1 (opacity)
				wgpuQueueWriteBuffer(_d.Q, idu, 0, (IntPtr)idc, 96);
				// Two entries, not three: the composite shader uses textureLoad, so its layout has no sampler.
				var e = stackalloc WGPUBindGroupEntry[2];
				e[0] = new WGPUBindGroupEntry { Binding = 0, TextureView = upscaled };
				e[1] = new WGPUBindGroupEntry { Binding = 2, Buffer = idu, Offset = 0, Size = WebGpuDevice.CompositeUniformBytes };
				var bgd = new WGPUBindGroupDescriptor { Layout = _d.CompositeBgl, EntryCount = 2, Entries = e };
				var bg = _d.TrackBg(wgpuDeviceCreateBindGroup(_d.Dev, &bgd));
				DrawEffectPass(_d.CompositeSrcOver, (IntPtr)bg);
			}
			finally
			{
				EndOwnedFrameEncoder(owns);
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
			var owns = BeginOwnedFrameEncoder();
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
				DrawEffectPass(_d.CompositeBlend, (IntPtr)bgh);
			}
			finally
			{
				EndOwnedFrameEncoder(owns);
			}
		}
	}

	// Standalone two-texture combine: out = k0*A + k1*B + k2*(A*B) + k3 (premultiplied, clamped), or A masked by B's
	// alpha when alphaMask. Covers CrossFade / ArithmeticComposite / AlphaMask. Fullscreen pass into the target surface.
	internal void CombineInto(WebGpuTexture a, WebGpuTexture b, float k0, float k1, float k2, float k3, bool alphaMask)
	{
		lock (_d.RenderGate)
		{
			var owns = BeginOwnedFrameEncoder();
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
				DrawEffectPass(_d.EffectCombine, (IntPtr)bgh);
			}
			finally
			{
				EndOwnedFrameEncoder(owns);
			}
		}
	}

	// Standalone single-input per-channel colour function (Contrast / GammaTransfer). u20 = 20 floats = the FU uniform
	// (params, amp, exps, offs, dis — 5 vec4). Fullscreen pass into the target surface.
	internal void ColorFuncInto(WebGpuTexture src, float[] u20)
	{
		lock (_d.RenderGate)
		{
			var owns = BeginOwnedFrameEncoder();
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
				DrawEffectPass(_d.ColorFunc, (IntPtr)bgh);
			}
			finally
			{
				EndOwnedFrameEncoder(owns);
			}
		}
	}

	// Procedural WhiteNoise generator into the target surface (no input). freq/offset are the effect params; the
	// surface size feeds pixel coords so the noise field is stable regardless of the fullscreen triangle.
	internal void NoiseInto(float fx, float fy, float ox, float oy, int w, int h)
	{
		lock (_d.RenderGate)
		{
			var owns = BeginOwnedFrameEncoder();
			try
			{
				var ubuf = MakeUniform(32);
				var uc = stackalloc float[8]; uc[0] = fx; uc[1] = fy; uc[2] = ox; uc[3] = oy; uc[4] = w; uc[5] = h;
				wgpuQueueWriteBuffer(_d.Q, ubuf, 0, (IntPtr)uc, 32);
				var e = stackalloc WGPUBindGroupEntry[1];
				e[0] = new WGPUBindGroupEntry { Binding = 0, Buffer = ubuf, Offset = 0, Size = 32 };
				var bgd = new WGPUBindGroupDescriptor { Layout = _d.EffectNoiseBgl, EntryCount = 1, Entries = e };
				var bgh = _d.TrackBg(wgpuDeviceCreateBindGroup(_d.Dev, &bgd));
				DrawEffectPass(_d.EffectNoise, (IntPtr)bgh);
			}
			finally
			{
				EndOwnedFrameEncoder(owns);
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

		var color = new WGPURenderPassColorAttachment { DepthSlice = uint.MaxValue, View = dst, LoadOp = WGPULoadOp.Clear, StoreOp = WGPUStoreOp.Store, ClearValue = default };
		var desc = new WGPURenderPassDescriptor { ColorAttachmentCount = 1, ColorAttachments = &color };
		var pass = wgpuCommandEncoderBeginRenderPass(_frameEncoder, &desc);
		wgpuRenderPassEncoderSetPipeline(pass, _d.BlurPipe);
		wgpuRenderPassEncoderSetBindGroup(pass, 0, bg, 0, (uint*)null);
		wgpuRenderPassEncoderDraw(pass, 3, 1, 0, 0);
		wgpuRenderPassEncoderEnd(pass);
	}
}
