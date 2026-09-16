// A render target the backend owns: its MSAA colour and the single-sample view it resolves into. Also the
// per-recording resources a session hands back when it is released.
#nullable disable
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Uno.WebGpu.Native;
using static Uno.WebGpu.Native.WGPU;
using Uno.UI.Composition.Drawing;
using Windows.Graphics.Effects.Interop;
using Windows.Foundation;
using WColor = Windows.UI.Color;

namespace Uno.UI.Composition.WebGpu;

// Renderer-internal render surface (main pass + offscreen layers): the MSAA colour the backend owns,
// its own colour for an offscreen, a pooled one for a layer, the host's IWebGpuRenderTarget.ColorView for the main
// pass. Not the neutral seam type — that is the host's WebGpuSwapchainTarget.
internal sealed unsafe class WebGpuRenderSurface
{
	public IntPtr Tex;
	public IntPtr View;              // the colour the pass renders into, sampled afterwards as a layer / coverage / readback source
	public int Width { get; }
	public int Height { get; }
	public GraphicsColorFormat ColorFormat => GraphicsColorFormat.Rgba8888;

	// Only the dedicated offscreen owns its colour. A pooled surface's is reclaimed by the pool; a swapchain surface's
	// is the per-frame acquired image, borrowed from the context, which releases it in Present.
	private readonly bool _ownsColor;

	public void Dispose()
	{
		if (!_ownsColor) { return; }
		if (View != IntPtr.Zero) { wgpuTextureViewRelease(View); View = IntPtr.Zero; }
		if (Tex != IntPtr.Zero) { wgpuTextureDestroy(Tex); Tex = IntPtr.Zero; }
	}

	// A dedicated offscreen (RenderOffscreen, effect evaluation): TextureBinding so the result can be sampled, CopySrc
	// so it can be read back.
	public WebGpuRenderSurface(WebGpuDevice device, int width, int height)
	{
		Width = width; Height = height;
		_ownsColor = true;
		var td = new WGPUTextureDescriptor
		{
			Size = new WGPUExtent3D { Width = (uint)width, Height = (uint)height, DepthOrArrayLayers = 1 },
			Format = device.ColorFormat,
			MipLevelCount = 1,
			SampleCount = 1,
			Dimension = WGPUTextureDimension._2D,
			Usage = WGPUTextureUsage.RenderAttachment | WGPUTextureUsage.CopySrc | WGPUTextureUsage.TextureBinding,
		};
		Tex = wgpuDeviceCreateTexture(device.Dev, &td);
		View = wgpuTextureCreateView(Tex, null);
	}

	// The main pass: the colour is the acquired swapchain image, set per frame by the factory.
	public WebGpuRenderSurface(WebGpuDevice device, int width, int height, bool externalColor)
	{
		Width = width; Height = height;
	}

	// A transient offscreen (a layer, a sheet) rented from the pool, so a steady-state frame allocates nothing.
	public WebGpuRenderSurface(WebGpuDevice device, int width, int height, WebGpuTexturePool pool)
	{
		Width = width; Height = height;
		View = pool.Rent(width, height, 1, WGPUTextureUsage.RenderAttachment | WGPUTextureUsage.TextureBinding | WGPUTextureUsage.CopySrc, device.ColorFormat);
		Tex = pool.TexForView(View);
	}

	// Hands the colour to a longer-lived owner (RenderOffscreen -> ITexture); Dispose then has nothing left to release.
	internal (IntPtr tex, IntPtr view) DetachColor()
	{
		var t = Tex; var v = View;
		Tex = IntPtr.Zero; View = IntPtr.Zero;
		return (t, v);
	}
}

internal sealed class OwnedResources
{
	public System.Collections.Generic.List<nint> Buffers = new();
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
}


// One draw op in a pass's ordered list. A struct so glyph coalescing can carry the extra
// fields (a shared glyph-fan-buffer start + the fill colour) without threading a wider tuple through ~30 sites. The
// lowercase field names + Deconstruct keep the existing `var (kind, b0, ...) = op` destructuring and `.kind`/`.b0`
// access working unchanged. For a path op,
// GlyphFanStart>=0 marks the fan as living in the pass's shared glyph buffer at that start vertex (b0 unused),
// and Color is the run colour (coalescing merges same-Color+same-clip runs).
