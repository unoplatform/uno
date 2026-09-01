// A render target the backend owns: its MSAA colour, its depth/stencil, and the single-sample view it resolves
// into. Also the per-recording resources a session hands back when it is released.
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

// Renderer-internal render surface (main pass + offscreen layers): the MSAA colour + depth the backend owns,
// resolving into a single-sample colour (its own, for offscreens; the host's IWebGpuRenderTarget.ColorView, for
// the main pass). Not the neutral seam type — that is the host's WebGpuSwapchainTarget.
internal sealed unsafe class WebGpuRenderSurface
{
	public IntPtr Tex;
	public IntPtr View;              // single-sample resolve target (offscreen readback / swapchain image)
	public IntPtr MsaaColorTex;
	public IntPtr MsaaColorView;     // multisampled color the pass renders into, resolved into View
	public IntPtr DepthTex;
	public IntPtr DepthView;         // multisampled depth/stencil (clip mask + stencil-then-cover)
									 // Render-bundle cache for this surface's main pass (see RenderInto): the present session is per-frame,
									 // so the cache lives here; a resize allocates a new surface, which naturally resets it.

	public DrawOp[] BundleOps = Array.Empty<DrawOp>();
	public int BundleOpsN = -1;
	public IntPtr[] BundleChunks = Array.Empty<IntPtr>();
	public nint BundleSolidTableBuf, BundleRrectTableBuf, BundleSolidSlabBuf, BundleXformBg;
	public int Width { get; }
	public int Height { get; }
	// True when MSAA colour + depth were rented from the transient pool. Both are write-only within this surface's
	// own render pass (the MSAA colour resolves into View, depth is discarded) and never sampled afterwards, so
	// once the pass ends they can be returned to the pool and reused by the next same-size offscreen/main pass —
	// only the single-sample resolve View must stay live (it's sampled later as a layer/coverage/backdrop texture).
	public bool Pooled { get; private set; }
	public GraphicsColorFormat ColorFormat => GraphicsColorFormat.Rgba8888;

	// Pooled surfaces rent their views from the WebGpuTexturePool, which owns and reclaims them — Dispose must not
	// touch those. Directly-created surfaces (offscreen readback / swapchain resolve) own their textures and MUST
	// release them, otherwise every window resize leaks a full-window MSAA color + depth texture until VRAM is
	// exhausted (wgpuDeviceCreateTexture: "Not enough memory left").
	private readonly bool _ownsResources = true;
	// For a swapchain surface the colour View/Tex are the per-frame acquired swapchain image, borrowed from the
	// context (WebGpuSwapChainContext) which releases them in Present. Only the MSAA+depth are owned here, so
	// Dispose (on resize) must NOT release the borrowed colour — doing so double-frees the swapchain view.
	private readonly bool _ownsColor = true;

	public void Dispose()
	{
		if (!_ownsResources)
		{
			return;
		}
		if (_ownsColor)
		{
			if (View != IntPtr.Zero) { wgpuTextureViewRelease(View); View = IntPtr.Zero; }
			if (Tex != IntPtr.Zero) { wgpuTextureDestroy(Tex); Tex = IntPtr.Zero; }
		}
		// At 1x MsaaColorView aliases the (already-released) View and there is no MSAA texture — only release it when
		// it's a distinct multisampled texture (MsaaColorTex set).
		if (MsaaColorTex != IntPtr.Zero) { wgpuTextureViewRelease(MsaaColorView); MsaaColorView = IntPtr.Zero; wgpuTextureDestroy(MsaaColorTex); MsaaColorTex = IntPtr.Zero; }
		if (DepthView != IntPtr.Zero) { wgpuTextureViewRelease(DepthView); DepthView = IntPtr.Zero; }
		if (DepthTex != IntPtr.Zero) { wgpuTextureDestroy(DepthTex); DepthTex = IntPtr.Zero; }
	}

	public WebGpuRenderSurface(WebGpuDevice device, int width, int height)
	{
		Width = width; Height = height;
		var td = new WGPUTextureDescriptor
		{
			Size = new WGPUExtent3D { Width = (uint)width, Height = (uint)height, DepthOrArrayLayers = 1 },
			Format = device.ColorFormat,
			MipLevelCount = 1,
			SampleCount = 1,
			Dimension = WGPUTextureDimension._2D,
			// TextureBinding so a resolved surface can be sampled (e.g. shadow coverage feeding the blur pass).
			Usage = WGPUTextureUsage.RenderAttachment | WGPUTextureUsage.CopySrc | WGPUTextureUsage.TextureBinding,
		};
		Tex = wgpuDeviceCreateTexture(device.Dev, &td);
		View = wgpuTextureCreateView(Tex, null);
		CreateMultisampledTargets(device, width, height);
	}

	// External-color variant for a swapchain: the color View/Tex are provided per frame (the acquired
	// swapchain image, used as the resolve target); the multisampled color + depth are owned here.
	public WebGpuRenderSurface(WebGpuDevice device, int width, int height, bool externalColor)
	{
		Width = width; Height = height;
		_ownsColor = false;   // View/Tex are the borrowed swapchain image (set per frame); only MSAA+depth are owned
		CreateMultisampledTargets(device, width, height);
	}

	// Pooled transient offscreen: MSAA color + depth + a single-sample resolve target (sampled later), all rented
	// from the pool so a steady-state frame allocates nothing. Dispose is a no-op (the pool reclaims on BeginFrame).
	public WebGpuRenderSurface(WebGpuDevice device, int width, int height, WebGpuTexturePool pool)
	{
		Width = width; Height = height;
		_ownsResources = false;   // the pool owns and reclaims these; Dispose must not release them
		Pooled = true;
		DepthView = pool.Rent(width, height, (int)device.MsaaSamples, WGPUTextureUsage.RenderAttachment, WebGpuDevice.DepthStencilFormat);
		// CopySrc so the resolved result can be read back (ReadPixelsFromTex, via SnapshotAsync) for RenderTargetBitmap / offscreen.
		View = pool.Rent(width, height, 1, WGPUTextureUsage.RenderAttachment | WGPUTextureUsage.TextureBinding | WGPUTextureUsage.CopySrc, device.ColorFormat);
		Tex = pool.TexForView(View);
		// 1x: no separate MSAA colour — the pass renders straight into the single-sample View (no resolve). Otherwise
		// the pass renders into a multisampled colour that resolves into View. MsaaColorView aliases View at 1x, so the
		// pool-return/Dispose paths must NOT free it as if it were a distinct texture (guarded on MsaaSamples>1).
		MsaaColorView = device.MsaaSamples > 1
			? pool.Rent(width, height, (int)device.MsaaSamples, WGPUTextureUsage.RenderAttachment, device.ColorFormat)
			: View;
	}

	// Hands the resolved single-sample color texture/view to a longer-lived owner (RenderOffscreen → ITexture)
	// and nulls them here so Dispose releases only the (now-finished) MSAA + depth targets. Only valid on a
	// resource-owning surface (the dedicated ctor), after the render has been submitted+resolved.
	internal (IntPtr tex, IntPtr view) DetachColor()
	{
		var t = Tex; var v = View;
		Tex = IntPtr.Zero; View = IntPtr.Zero;
		return (t, v);
	}

	private void CreateMultisampledTargets(WebGpuDevice device, int width, int height)
	{
		// 1x: no multisampled colour — the pass renders straight into the single-sample View (no resolve). For the
		// swapchain external-colour surface View is set per frame, so MsaaColorView is aliased to it there.
		if (device.MsaaSamples > 1)
		{
			var cd = new WGPUTextureDescriptor
			{
				Size = new WGPUExtent3D { Width = (uint)width, Height = (uint)height, DepthOrArrayLayers = 1 },
				Format = device.ColorFormat,
				MipLevelCount = 1,
				SampleCount = device.MsaaSamples,
				Dimension = WGPUTextureDimension._2D,
				Usage = WGPUTextureUsage.RenderAttachment,
			};
			MsaaColorTex = wgpuDeviceCreateTexture(device.Dev, &cd);
			MsaaColorView = wgpuTextureCreateView(MsaaColorTex, null);
		}
		else
		{
			MsaaColorView = View;   // Zero for the swapchain ctor (View set per frame) — aliased in the context
		}

		var dd = new WGPUTextureDescriptor
		{
			Size = new WGPUExtent3D { Width = (uint)width, Height = (uint)height, DepthOrArrayLayers = 1 },
			Format = WebGpuDevice.DepthStencilFormat,
			MipLevelCount = 1,
			SampleCount = device.MsaaSamples,
			Dimension = WGPUTextureDimension._2D,
			Usage = WGPUTextureUsage.RenderAttachment,
		};
		DepthTex = wgpuDeviceCreateTexture(device.Dev, &dd);
		DepthView = wgpuTextureCreateView(DepthTex, null);
	}
}


// A clip is a device-space scissor AABB (fast reject + plain-rect clip) plus an optional device-space,
// axis-aligned rounded-rect whose corners are masked per-fragment in the shaders. A rotated rounded clip
// degrades to its AABB (the exact fix is clip-local-space eval, as with the radial gradient — follow-up).
// A single analytic rounded-rect clip (device space). Nested clips stack in ClipData.Rounds and are ANDed in-shader.

internal sealed class OwnedResources
{
	public System.Collections.Generic.List<nint> Buffers = new();
	public System.Collections.Generic.List<nint> BindGroups = new();
	// Clip-slab slot handles this bag's bind groups reference; freed with the bag (see WebGpuClipSlab).
	public System.Collections.Generic.List<nint> ClipSlots;
	// Coverage-atlas slots this bag's draw ops sample; freed with the bag, because those ops bake the slot's UVs
	// and would sample another shape's mask if the region were reclaimed while they still existed.
	public System.Collections.Generic.List<WebGpuPathAtlas.Slot> AtlasSlots;
	// Release-once claim: a rebuild (render thread) and the recording's Dispose (UI thread) can both hand the
	// same bag to DeferRelease — the rebuild reads the compiled entry before it stores the replacement, so a
	// Dispose in that window re-defers the old bag. Double-releasing recycles wgpu ids under in-flight uses
	// ("BindGroup[Id] does not exist" panic); the claim makes the second hand-off a no-op.
	public int Released;
}


// One draw op in a pass's ordered list. A struct so glyph coalescing can carry the extra
// fields (a shared glyph-fan-buffer start + the fill colour) without threading a wider tuple through ~30 sites. The
// lowercase field names + Deconstruct keep the existing `var (kind, b0, ...) = op` destructuring and `.kind`/`.b0`
// access working unchanged. For a coalesced-glyph path op (DrawKind.Path),
// GlyphFanStart>=0 marks the fan as living in the pass's shared glyph buffer at that start vertex (b0 unused),
// and Color is the run colour (coalescing merges same-Color+same-clip stencils).
