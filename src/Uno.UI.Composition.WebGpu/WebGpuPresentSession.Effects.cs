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
	// The blurred shadow as a texture with its device-space placement. Cached in the atlas under the silhouette's
	// geometry, transform and blur radius (a texture of its own, since a shadow is padded by its blur reach), so a
	// static shadow bakes once: on a miss the silhouette's coverage is baked, blurred, and resampled into the entry.
	private IntPtr RenderShadow(ShadowCmd sh, out Vector2 origin, out Vector2 size, out Vector4 uv)
	{
		float pad = MathF.Ceiling(3f * MathF.Max(sh.SigmaX, sh.SigmaY)) + 2f;
		var bbMin = sh.BbMin - new Vector2(pad); var bbMax = sh.BbMax + new Vector2(pad);
		int sigmaKey = ((int)(sh.SigmaX * 16f) << 16) ^ (int)(sh.SigmaY * 16f);
		var shape = _d.Shapes.Get(sh.Geometry, sh.M, 1f, sh.EvenOdd);
		WebGpuPathAtlas.Key key = default; int w = 0, h = 0; float ox = 0f, oy = 0f;
		var keyed = shape.Edges is not null && WebGpuPathAtlas.TryKey(shape.Hash, Matrix4x4.Identity, bbMin, bbMax, Vector2.One, out key, out w, out h, out ox, out oy, allowBig: true, extra: sigmaKey) && _pathAtlas;
		uv = new Vector4(0f, 0f, 1f, 1f);
		if (keyed && _d.PathAtlas.TryGet(key, out var hit))
		{
			_d.PathAtlas.NoteUse(hit, _d.FrameSeq);
			origin = new Vector2(hit.OriginX, hit.OriginY);
			size = new Vector2(hit.W, hit.H);
			uv = hit.Uv;
			return hit.Owner.View;
		}
		if (!keyed)
		{
			ox = MathF.Floor(bbMin.X); oy = MathF.Floor(bbMin.Y);
			w = Math.Clamp((int)MathF.Ceiling(bbMax.X - ox) + 2, 1, 4096); h = Math.Clamp((int)MathF.Ceiling(bbMax.Y - oy) + 2, 1, 4096);
		}
		origin = new Vector2(ox, oy);
		size = new Vector2(w, h);

		// Every shadow of the frame bakes on the shadow sheet for its blur radius, so N shadows cost one bake and one
		// blur pyramid, not N. One whose key has held for a run of frames (see Recurring) is static: its blurred slot
		// is copied out of the sheet into an entry of its own, at the pyramid's top-level size, and drawn from there.
		float sigma = MathF.Max(sh.SigmaX, sh.SigmaY);
		if (TryReserveShadowSlot(sigma, w, h, out var sheet, out var sx, out var sy))
		{
			AddBake(sheet.Bake, sx, sy, w, h, shape.Edges, new Vector2(ox + 1, oy + 1) - sh.Offset, Vector2.One, sh.EvenOdd);
			ShadowSlotsBaked++;
			if (!(keyed && _d.PathAtlas.Recurring(key, _d.FrameSeq)))
			{
				uv = new Vector4(sx, sy, sx + w, sy + h) / SheetSize;
				return sheet.Blurred;
			}
			// The slot's texels on the top level, plus the one a bilinear tap at its far edge reads (the slot's gutter).
			int step = 1 << BlurLevels(sigma);
			int tw = (w + step - 1) / step + 1, th = (h + step - 1) / step + 1;
			var slot = AddStandaloneSlot(key, w, h, ox, oy, WebGpuDevice.DefaultColorFormat, tw, th, WGPUTextureUsage.TextureBinding | WGPUTextureUsage.CopyDst);
			slot.Uv = new Vector4(0f, 0f, (float)w / step / tw, (float)h / step / th);
			_d.PathAtlas.HoldForCache(slot, _d.FrameSeq);
			_pendingCopies.Add((sheet.Blurred, sx / step, sy / step, slot.Owner.Texture, tw, th));
			uv = slot.Uv;
			return slot.Owner.View;
		}
		// Too big for the sheet: its own bake and pyramid.
		var (view, tex) = NewMaskTexture(w, h);
		AddBake(BatchFor(view, w, h, load: false), 0, 0, w, h, shape.Edges, new Vector2(ox + 1, oy + 1) - sh.Offset, Vector2.One, sh.EvenOdd);
		_d.DeferTextureRelease(view, tex);
		return DeferBlur(view, w, h, sh.SigmaX, sh.SigmaY);
	}

	// A frame's moving shadows of one pyramid depth: their silhouettes bake on one sheet (a coverage batch like any
	// other) and the whole sheet runs through one blur pyramid into Blurred, which the shadow draws sample by slot.
	// The kernel on the pyramid's top level is fixed and sigma only picks the depth, so a depth is a blur class.
	private sealed class ShadowSheet
	{
		public BakeBatch Bake;
		public IntPtr Blurred;     // the pyramid's top level for the whole sheet, rented up front so draws can bind it
	}

	private readonly Dictionary<int, ShadowSheet> _shadowSheetByDepth = new();
	private readonly List<(IntPtr Src, int W, int H, float SigmaX, float SigmaY, IntPtr Dst)> _pendingBlurs = new();
	private readonly List<(IntPtr SrcView, int X, int Y, IntPtr Dst, int W, int H)> _pendingCopies = new();   // after the blurs
	internal static int ShadowSlotsBaked;

	// Reserves a w x h slot on the frame's shadow sheet for this blur depth. A slot already holds its shadow's blur
	// reach (the caller padded it by 3 sigma), so slots only need to sit on the top level's texel grid, one texel
	// apart, for a slot's edge samples not to read a neighbour.
	private bool TryReserveShadowSlot(float sigma, int w, int h, out ShadowSheet sheet, out int x, out int y)
	{
		sheet = null; x = y = 0;
		var levels = BlurLevels(sigma);
		int step = 1 << levels;
		int gw = (w + step - 1) / step * step + step, gh = (h + step - 1) / step * step + step;
		if (gw > SheetSize || gh > SheetSize) { return false; }
		_shadowSheetByDepth.TryGetValue(levels, out var cur);
		if (cur is not null)
		{
			var b = cur.Bake;
			if (b.CursorX + gw > SheetSize) { b.ShelfY += b.ShelfH; b.ShelfH = 0; b.CursorX = 0; }
			if (b.ShelfY + gh > SheetSize) { cur = null; }
		}
		if (cur is null)
		{
			var view = _d.Pool.Rent(SheetSize, SheetSize, 1, WGPUTextureUsage.RenderAttachment | WGPUTextureUsage.TextureBinding, _d.ColorFormat);
			int top = SheetSize >> levels;
			var blurred = _d.Pool.Rent(top, top, 1, WGPUTextureUsage.RenderAttachment | WGPUTextureUsage.TextureBinding | WGPUTextureUsage.CopySrc, WebGpuDevice.DefaultColorFormat);
			cur = new ShadowSheet { Bake = BatchFor(view, SheetSize, SheetSize, load: false), Blurred = blurred };
			_shadowSheetByDepth[levels] = cur;
			// The sigma that maps back to exactly this depth (see BlurLevels), so the pyramid builds the same levels.
			float depthSigma = 2f * step;
			_pendingBlurs.Add((view, SheetSize, SheetSize, depthSigma, depthSigma, blurred));
		}
		sheet = cur;
		x = cur.Bake.CursorX; y = cur.Bake.ShelfY;
		cur.Bake.CursorX += gw;
		if (gh > cur.Bake.ShelfH) { cur.Bake.ShelfH = gh; }
		return true;
	}

	// A blur that must run after the frame's bakes: the result texture is rented now so the draw can bind it.
	private IntPtr DeferBlur(IntPtr src, int w, int h, float sigmaX, float sigmaY)
	{
		var levels = BlurLevels(MathF.Max(sigmaX, sigmaY));
		while (levels > 1 && ((w >> levels) < 4 || (h >> levels) < 4)) { levels--; }
		var dst = _d.Pool.Rent(Math.Max(1, w >> levels), Math.Max(1, h >> levels), 1, WGPUTextureUsage.RenderAttachment | WGPUTextureUsage.TextureBinding, WebGpuDevice.DefaultColorFormat);
		_pendingBlurs.Add((src, w, h, sigmaX, sigmaY, dst));
		return dst;
	}

	// Runs the blurs queued behind the frame's bakes, once those are encoded.
	private void FlushPendingBlurs()
	{
		foreach (var b in _pendingBlurs) { BlurPyramidRegion(b.Src, b.W, b.H, 0f, 0f, b.W, b.H, b.SigmaX, b.SigmaY, b.Dst); }
		_pendingBlurs.Clear();
		_shadowSheetByDepth.Clear();
		foreach (var c in _pendingCopies)
		{
			var src = new WGPUTexelCopyTextureInfo { Texture = _d.Pool.TexForView(c.SrcView), MipLevel = 0, Origin = new WGPUOrigin3D { X = (uint)c.X, Y = (uint)c.Y }, Aspect = WGPUTextureAspect.All };
			var dst = new WGPUTexelCopyTextureInfo { Texture = c.Dst, MipLevel = 0, Origin = default, Aspect = WGPUTextureAspect.All };
			var ext = new WGPUExtent3D { Width = (uint)c.W, Height = (uint)c.H, DepthOrArrayLayers = 1 };
			wgpuCommandEncoderCopyTextureToTexture(_frameEncoder, &src, &dst, &ext);
		}
		_pendingCopies.Clear();
	}

	// Pyramid depth for a blur radius: halve until the fixed 9-tap kernel on the top level spans the sigma.
	private static int BlurLevels(float sigma) => Math.Clamp((int)MathF.Round(MathF.Log2(MathF.Max(sigma, 1f) / 2f)), 1, 5);

	// ------------------------------------------------------------------------------------------------ layer sheet

	private const int LayerSheetSize = 2048;

	// The frame's size-to-content layers at one nesting depth, shelf-packed into one texture rendered in one pass;
	// the blurs its shadows share, one pyramid per blur depth, run right after that pass. Sheets are per nesting
	// depth because a layer's content composites the layers nested in it, so those must be rendered first, and a
	// texture cannot be drawn into and sampled in the same pass.
	private sealed class LayerSheet
	{
		public WebGpuRenderSurface Surface;
		public int Depth;
		public readonly List<PassBuild> Builds = new();
		public readonly Dictionary<int, IntPtr> Blurs = new();   // blur depth -> the pyramid's top level, rented up front
		public int CursorX, ShelfY, ShelfH;
	}

	private readonly List<LayerSheet> _layerSheets = new();
	internal static int LayerSheetSlots, LayerSheetPasses;

	// Reserves a w x h slot on the sheet for the current nesting depth, aligned to `step` (the shadow's top-level
	// texel) with a texel of that size around it.
	private bool TryReserveLayerSlot(int w, int h, int step, out LayerSheet sheet, out int x, out int y)
	{
		sheet = null; x = y = 0;
		int gw = (w + step - 1) / step * step + 2 * step, gh = (h + step - 1) / step * step + 2 * step;
		if (gw > LayerSheetSize || gh > LayerSheetSize) { return false; }
		LayerSheet cur = null;
		for (int i = _layerSheets.Count - 1; i >= 0; i--) { if (_layerSheets[i].Depth == _layerDepth) { cur = _layerSheets[i]; break; } }
		if (cur is not null)
		{
			int cx = (cur.CursorX + step - 1) / step * step;
			if (cx + gw > LayerSheetSize) { cur.ShelfY += cur.ShelfH; cur.ShelfH = 0; cur.CursorX = 0; cx = 0; }
			int sy = (cur.ShelfY + step - 1) / step * step;
			if (sy + gh > LayerSheetSize) { cur = null; }
			else { cur.CursorX = cx; cur.ShelfY = sy; }
		}
		if (cur is null)
		{
			cur = new LayerSheet { Surface = new WebGpuRenderSurface(_d, LayerSheetSize, LayerSheetSize, _d.Pool), Depth = _layerDepth };
			_layerSheets.Add(cur);
			_frameLayerSurfaces.Add(cur.Surface);
		}
		sheet = cur; x = cur.CursorX + step; y = cur.ShelfY + step;
		cur.CursorX += gw;
		if (gh > cur.ShelfH) { cur.ShelfH = gh; }
		LayerSheetSlots++;
		return true;
	}

	// The blurred sheet for this depth, rented now so the shadow draws can bind it; the pyramid runs after the sheet's pass.
	private IntPtr LayerSheetBlur(LayerSheet sheet, float sigma)
	{
		var levels = BlurLevels(sigma);
		if (!sheet.Blurs.TryGetValue(levels, out var blurred))
		{
			int top = LayerSheetSize >> levels;
			blurred = _d.Pool.Rent(top, top, 1, WGPUTextureUsage.RenderAttachment | WGPUTextureUsage.TextureBinding, WebGpuDevice.DefaultColorFormat);
			sheet.Blurs[levels] = blurred;
		}
		return blurred;
	}

	// Renders the sheets one level deeper than the pass about to open (the layers it composites), each followed by
	// its shadow pyramids. A sheet's own pass flushes the level below it first, so the deepest render first.
	private void FlushLayerSheets(int depth)
	{
		if (_layerSheets.Count == 0) { return; }
		var sheets = new List<LayerSheet>();
		for (int i = _layerSheets.Count - 1; i >= 0; i--)
		{
			if (_layerSheets[i].Depth == depth + 1) { sheets.Add(_layerSheets[i]); _layerSheets.RemoveAt(i); }
		}
		var saved = _layerDepth;
		_layerDepth = depth + 1;
		foreach (var sheet in sheets)
		{
			EncodePass(sheet.Surface, null, false, sheet.Builds);
			LayerSheetPasses++;
			foreach (var (levels, blurred) in sheet.Blurs)
			{
				float depthSigma = 2f * (1 << levels);
				BlurPyramidRegion(sheet.Surface.View, LayerSheetSize, LayerSheetSize, 0f, 0f, LayerSheetSize, LayerSheetSize, depthSigma, depthSigma, blurred);
			}
		}
		_layerDepth = saved;
	}

	// Blur pyramid over a REGION of `src`: extract the device-px rect (rx,ry,rw,rh) out of the fullW×fullH source
	// into a sigma-scaled downsample pyramid (depth set by the requested blur radius), then a fixed 9-tap separable
	// gaussian on the small top level. Returns the region-sized blurred view; the caller maps screen px -> region uv
	// in the composite (bilinear upscales it). Only the region behind the acrylic element is ever processed, and the
	// per-pass kernel is constant, so a large blur is a few tiny passes instead of a full-frame O(sigma) kernel.
	private IntPtr BlurPyramidRegion(IntPtr src, int fullW, int fullH, float rx, float ry, float rw, float rh, float sigmaX, float sigmaY, IntPtr dst = default)
	{
		int iw = Math.Max(1, (int)MathF.Round(rw)), ih = Math.Max(1, (int)MathF.Round(rh));
		int levels = BlurLevels(MathF.Max(sigmaX, sigmaY));
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
		// The final pass lands in the caller's texture when it brought one (a blur deferred behind the frame's bakes).
		var vv = dst != IntPtr.Zero ? dst : _d.Pool.Rent(cw, ch, 1, WGPUTextureUsage.RenderAttachment | WGPUTextureUsage.TextureBinding, WebGpuDevice.DefaultColorFormat);
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
