// A render target the backend renders into: a colour texture and its view. Also the
// per-recording resources a session hands back when it is released.
#nullable disable
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Silk.NET.WebGPU;
using Uno.UI.Composition.Drawing;
using Windows.Graphics.Effects.Interop;
using Windows.Foundation;
using WColor = Windows.UI.Color;

namespace Uno.UI.Composition.WebGpu;

// Renderer-internal render surface (main pass + offscreen layers): its own colour for an offscreen, a pooled one for
// a layer, the host's IWebGpuRenderTarget.ColorView for the main pass. Not the neutral seam type — that is the host's
// WebGpuSwapchainTarget.
internal sealed unsafe class WebGpuRenderSurface
{
	public Texture* Tex;
	public TextureView* View;        // the colour the pass renders into, sampled afterwards as a layer / coverage / readback source
	public int Width { get; }
	public int Height { get; }
	public GraphicsColorFormat ColorFormat => GraphicsColorFormat.Rgba8888;

	// Only the dedicated offscreen owns its colour. A pooled surface's is reclaimed by the pool; the main pass's is the
	// host's target view, which the host owns.
	private readonly bool _ownsColor;
	private readonly WebGPU _api;

	public void Dispose()
	{
		if (!_ownsColor) { return; }
		if (View != null) { _api.TextureViewRelease(View); View = null; }
		if (Tex != null) { _api.TextureDestroy(Tex); _api.TextureRelease(Tex); Tex = null; }
	}

	// A dedicated offscreen (RenderOffscreen, effect evaluation): TextureBinding so the result can be sampled, CopySrc
	// so it can be read back.
	public WebGpuRenderSurface(WebGpuDevice device, int width, int height, string what = "offscreen")
	{
		_api = device.Api;
		Width = width; Height = height;
		_ownsColor = true;
		var td = new TextureDescriptor
		{
			Size = new Extent3D { Width = (uint)width, Height = (uint)height, DepthOrArrayLayers = 1 },
			Format = device.ColorFormat,
			MipLevelCount = 1,
			SampleCount = 1,
			Dimension = TextureDimension.Dimension2D,
			Usage = TextureUsage.RenderAttachment | TextureUsage.CopySrc | TextureUsage.TextureBinding,
		};
		if (device.OffscreenPool.TryRent(width, height, out var pooled, out var pooledView))
		{
			Tex = pooled;
			View = pooledView;
			return;
		}

		Tex = _api.DeviceCreateTexture(device.Dev, &td);
		View = _api.TextureCreateView(Tex, null);
	}

	// The main pass: the colour is the host's target view, set by the factory; nothing is allocated here.
	public WebGpuRenderSurface(WebGpuDevice device, int width, int height, bool externalColor)
	{
		_api = device.Api;
		Width = width; Height = height;
	}

	// A transient offscreen (a layer, a sheet) rented from the pool, so a steady-state frame allocates nothing.
	public WebGpuRenderSurface(WebGpuDevice device, int width, int height, WebGpuTexturePool pool)
	{
		_api = device.Api;
		Width = width; Height = height;
		View = pool.Rent(width, height, 1, TextureUsage.RenderAttachment | TextureUsage.TextureBinding | TextureUsage.CopySrc, device.ColorFormat);
		Tex = pool.TexForView(View);
	}

	// Hands the colour to a longer-lived owner (RenderOffscreen -> ITexture); Dispose then has nothing left to release.
	internal WebGpuTexture DetachTexture(WebGpuDevice device)
	{
		var texture = new WebGpuTexture(device, Tex, View, Width, Height);
		Tex = null; View = null;
		return texture;
	}
}

internal sealed class OwnedResources
{
	public System.Collections.Generic.List<nint> Buffers = new();
	// This bag's vertex data, packed into as few GPU buffers as possible when the build ends. A buffer per op
	// costs a whole placed-resource slot on the GPU (64 KB on D3D12), so a few thousand small ops commit hundreds
	// of MB; packing also lets the encoder's merge rule fire, which a buffer per op makes impossible. Split into
	// chunks because a recording's geometry can exceed the device's maximum buffer size on its own.
	internal System.Collections.Generic.List<VertBuf> VertexArenas;
	// Where each packed op's vertices landed (chunk, first vertex); an op's provisional Verts tag indexes this.
	internal System.Collections.Generic.List<(int Chunk, uint First)> PackedVertexRanges;
	public System.Collections.Generic.List<nint> BindGroups = new();
	// Clip-slab slot handles this bag's bind groups reference; freed with the bag (see WebGpuClipSlab).
	public System.Collections.Generic.List<nint> ClipSlots;
	// Coverage-atlas slots this bag's draw ops sample; freed with the bag, because those ops bake the slot's UVs
	// and would sample another shape's mask if the region were reclaimed while they still existed.
	public System.Collections.Generic.List<WebGpuPathAtlas.Slot> AtlasSlots;
	// Path-clip coverage masks this bag's clip bind groups sample. They live exactly as long as the bag: a mask
	// from the per-frame pool under a bind group that outlives the frame is a use-after-free.
	public System.Collections.Generic.List<(nint view, nint tex)> Textures;
	// Release-once claim: a rebuild (render thread) and the recording's Dispose (UI thread) can both hand the
	// same bag to DeferRelease — the rebuild reads the compiled entry before it stores the replacement, so a
	// Dispose in that window re-defers the old bag. Double-releasing recycles wgpu ids under in-flight uses
	// ("BindGroup[Id] does not exist" panic); the claim makes the second hand-off a no-op.
	public int Released;

	internal readonly record struct ClipMemoEntry(ClipData Clip, System.Numerics.Matrix3x2 Xform, System.Numerics.Matrix3x2 Finv, nint Bg, nint Slot, bool AabbInClipU);
	// The clip bind groups this bag has already made, for sharing between its ops while it is being built; dropped
	// when the build ends, since nothing adds to a finished bag.
	internal System.Collections.Generic.Dictionary<int, System.Collections.Generic.List<ClipMemoEntry>> ClipMemo;

}


// One draw op in a pass's ordered list. A struct so glyph coalescing can carry the extra
// fields (a shared glyph-fan-buffer start + the fill colour) without threading a wider tuple through ~30 sites. The
// lowercase field names + Deconstruct keep the existing `var (kind, b0, ...) = op` destructuring and `.kind`/`.b0`
// access working unchanged. For a path op,
// GlyphFanStart>=0 marks the fan as living in the pass's shared glyph buffer at that start vertex (b0 unused),
// and Color is the run colour (coalescing merges same-Color+same-clip runs).
