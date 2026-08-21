// Minimal-but-real WebGPU backend implementing the NEUTRAL drawing seam (public SPI from Uno.UI.Composition).
// Solid rects + even-odd path fill (stencil-then-cover) consuming IGeometry.StreamFlattened (Skia-less).
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


// One entry in a frame-solid recording's ordered emit list: a solid/rrect run (relative vert start into the
// recording's cached SolidVerts/RrectVerts) or a spliced non-solid op (glyph/image/gradient).



/// <summary>The host-facing WebGPU context factories. Each builds the device bring-up (WebGpuInitDevice) + a
/// swapchain/browser context and returns it as a neutral ISwapChain — naming no renderer type. The
/// rasterizationScale argument is retained for the host-facing signature but no longer influences init (MSAA is
/// chosen by capability, and the per-frame DPI scale is applied by the drawing session's matrix).</summary>
internal static class WebGpuContext
{
	/// <summary>Win32 HWND swapchain context.</summary>
	public static ISwapChain CreateWin32(nint hwnd, nint hinstance, float rasterizationScale)
		=> new WebGpuSwapChainContext(WGPUTextureFormat.BGRA8Unorm, inst => WebGpuSwapChainContext.CreateHwndSurface(inst, hinstance, hwnd));

	/// <summary>X11 window swapchain context.</summary>
	public static ISwapChain CreateX11(nint display, nint window, float rasterizationScale)
		=> new WebGpuSwapChainContext(WGPUTextureFormat.BGRA8Unorm, inst => WebGpuSwapChainContext.CreateXlibSurface(inst, display, (ulong)window));

	/// <summary>Metal (<c>CAMetalLayer</c>) swapchain context — macOS and iOS/tvOS.</summary>
	public static ISwapChain CreateMetal(nint caMetalLayer, float rasterizationScale)
		=> new WebGpuSwapChainContext(WGPUTextureFormat.BGRA8Unorm, inst => WebGpuSwapChainContext.CreateMetalSurface(inst, caMetalLayer));

	/// <summary>Android <c>ANativeWindow</c> swapchain context.</summary>
	public static ISwapChain CreateAndroid(nint aNativeWindow, float rasterizationScale)
		=> new WebGpuSwapChainContext(WGPUTextureFormat.BGRA8Unorm, inst => WebGpuSwapChainContext.CreateAndroidSurface(inst, aNativeWindow));

	/// <summary>
	/// Browser (canvas) context. Asynchronous because the device is created in JavaScript (navigator.gpu) and
	/// imported into emdawnwebgpu's C handle table — the in-WASM wgpuInstanceProcessEvents pump hangs when driven
	/// from a managed call stack — and the JS thread must not be blocked. Returns null if the device import fails.
	/// </summary>
	public static async Task<ISwapChain> CreateWasmAsync(string canvasId)
	{
		var inst = WebGpuInitDevice.CreateInstancePtr();
		var devPtr = await WebGpuJsInterop.CreateImportedDeviceAsync((int)inst);
		if (devPtr == 0)
		{
			return null;
		}

		var device = WebGpuInitDevice.FromImported(WGPUTextureFormat.RGBA8Unorm, inst, (IntPtr)devPtr);
		return new WebGpuBrowserGraphicsContext(device, canvasId ?? "");
	}
}

/// <summary>
/// The Uno-provided WebGPU device BRING-UP (the host "GPU-API half"): creates instance/adapter/device/queue, picks
/// the MSAA sample count by capability (2× if the colour format supports it, else 4×, else 1× — no DPI/scale input),
/// and owns the present-blit sampler. It exposes the neutral <see cref="IWebGpuDeviceContext"/> the render backend
/// adopts to build its engine; it names no renderer type. Native path creates the device synchronously; the browser
/// path adopts a device imported from JS.
/// </summary>
internal sealed unsafe class WebGpuInitDevice : IWebGpuDeviceContext
{
	public IntPtr Inst, Adapter, Dev, Q;
	public readonly WGPUTextureFormat ColorFormat;
	// 2× is the preferred default (quality/cost sweet spot for UI); the browser stays 4× in
	// PickSampleCount because the WebGPU spec only guarantees sample counts 1 and 4.
	public uint MsaaSamples { get; private set; } = 2;
	public IntPtr Smp;                       // present-blit sampler (used by the swapchain/browser contexts)
	private bool _hasFormatFeatures;
	private readonly bool _browser;

	public static IntPtr CreateInstancePtr() => CreateInstance();

	/// <summary>
	/// Creates the wgpu instance. <c>UNO_WEBGPU_BACKENDS</c> (comma-separated: vulkan, gl, metal, dx12)
	/// restricts wgpu's adapter enumeration to the named backends — needed e.g. under RenderDoc, whose
	/// hooks crash multi-backend instance enumeration. Unset keeps wgpu's default (all backends).
	/// </summary>
	private static IntPtr CreateInstance()
	{
		if (Environment.GetEnvironmentVariable("UNO_WEBGPU_BACKENDS") is not { Length: > 0 } spec)
		{
			return wgpuCreateInstance(null);
		}

		var backends = WGPUInstanceBackend.All;
		foreach (var name in spec.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
		{
			backends |= name.ToLowerInvariant() switch
			{
				"vulkan" => WGPUInstanceBackend.Vulkan,
				"gl" => WGPUInstanceBackend.GL,
				"metal" => WGPUInstanceBackend.Metal,
				"dx12" => WGPUInstanceBackend.DX12,
				_ => WGPUInstanceBackend.All,
			};
		}

		var extras = new WGPUInstanceExtras
		{
			Chain = new WGPUChainedStruct { SType = (WGPUSType)WGPUNativeSType.WGPUSType_InstanceExtras },
			Backends = backends,
		};
		var descriptor = new WGPUInstanceDescriptor { NextInChain = &extras.Chain };
		return wgpuCreateInstance(&descriptor);
	}

	public WebGpuInitDevice(WGPUTextureFormat colorFormat)
	{
		ColorFormat = colorFormat;
		Inst = CreateInstance();

		var abox = new IntPtr[1];
		var ah = GCHandle.Alloc(abox);
		var aopts = new WGPURequestAdapterOptions { PowerPreference = WGPUPowerPreference.HighPerformance };
		wgpuInstanceRequestAdapter(Inst, &aopts, new WGPURequestAdapterCallbackInfo
		{
			Mode = WGPUCallbackMode.AllowProcessEvents,
			Callback = (IntPtr)(delegate* unmanaged[Cdecl]<WGPURequestAdapterStatus, IntPtr, WGPUStringView, IntPtr, IntPtr, void>)&OnAdapter,
			Userdata1 = GCHandle.ToIntPtr(ah),
		});
		for (int i = 0; i < 1000 && abox[0] == IntPtr.Zero; i++) { wgpuInstanceProcessEvents(Inst); }
		Adapter = abox[0];
		ah.Free();
		if (Adapter == IntPtr.Zero) { throw new InvalidOperationException("WebGPU: wgpuInstanceRequestAdapter returned no adapter (no compatible GPU/driver, or the callback never fired)."); }

		var dbox = new IntPtr[1];
		var dh = GCHandle.Alloc(dbox);
		var ddesc = new WGPUDeviceDescriptor();
		// wgpu-native's TextureAdapterSpecificFormatFeatures (when present) is what makes 2× MSAA valid for BGRA8/
		// RGBA8; without it 2× fails validation. Request it so PickSampleCount's 2× tier is usable.
		WGPUFeatureName* feats = stackalloc WGPUFeatureName[1];
		var fmtFeat = (WGPUFeatureName)WGPUNativeFeature.TextureAdapterSpecificFormatFeatures;
		_hasFormatFeatures = wgpuAdapterHasFeature(Adapter, fmtFeat) != 0;
		if (_hasFormatFeatures) { feats[0] = fmtFeat; ddesc.RequiredFeatures = feats; ddesc.RequiredFeatureCount = 1; }
		// Non-fatal uncaptured-error handler (wgpu's default panics the process on any validation/OOM error).
		ddesc.UncapturedErrorCallbackInfo = new WGPUUncapturedErrorCallbackInfo
		{
			Callback = (IntPtr)(delegate* unmanaged[Cdecl]<IntPtr, WGPUErrorType, WGPUStringView, IntPtr, IntPtr, void>)&OnUncapturedError,
		};
		wgpuAdapterRequestDevice(Adapter, &ddesc, new WGPURequestDeviceCallbackInfo
		{
			Mode = WGPUCallbackMode.AllowProcessEvents,
			Callback = (IntPtr)(delegate* unmanaged[Cdecl]<WGPURequestDeviceStatus, IntPtr, WGPUStringView, IntPtr, IntPtr, void>)&OnDevice,
			Userdata1 = GCHandle.ToIntPtr(dh),
		});
		for (int i = 0; i < 1000 && dbox[0] == IntPtr.Zero; i++) { wgpuInstanceProcessEvents(Inst); }
		Dev = dbox[0];
		dh.Free();
		if (Dev == IntPtr.Zero) { throw new InvalidOperationException("WebGPU: wgpuAdapterRequestDevice returned no device."); }

		Q = wgpuDeviceGetQueue(Dev);
		MsaaSamples = PickSampleCount();
		CreatePresentSampler();
		System.Console.WriteLine($"[webgpu] init device — msaa={MsaaSamples}x fmtFeatures={_hasFormatFeatures} colorFormat={ColorFormat}");
	}

	private WebGpuInitDevice(WGPUTextureFormat colorFormat, IntPtr inst, IntPtr dev)
	{
		_browser = true;
		ColorFormat = colorFormat;
		Inst = inst;
		Adapter = IntPtr.Zero;   // JS adapter isn't imported
		Dev = dev;
		Q = wgpuDeviceGetQueue(Dev);
		MsaaSamples = RequestedSampleCount() is 1u ? 1u : 4u;   // browser (Dawn) init is async — can't probe; spec guarantees only 1×/4×
		CreatePresentSampler();
	}

	/// <summary>Adopts an instance + a device imported from JS (browser bring-up). No adapter handle.</summary>
	public static WebGpuInitDevice FromImported(WGPUTextureFormat colorFormat, IntPtr inst, IntPtr dev)
		=> new WebGpuInitDevice(colorFormat, inst, dev);

	private void CreatePresentSampler()
	{
		var sd = new WGPUSamplerDescriptor { AddressModeU = WGPUAddressMode.ClampToEdge, AddressModeV = WGPUAddressMode.ClampToEdge, MagFilter = WGPUFilterMode.Linear, MinFilter = WGPUFilterMode.Linear, MipmapFilter = WGPUMipmapFilterMode.Nearest, MaxAnisotropy = 1 };
		Smp = wgpuDeviceCreateSampler(Dev, &sd);
	}

	// MSAA sample count, no DPI/scale input: prefer 2× (needs the format feature), else 4× (spec-guaranteed), else 1×.
	// The browser can't synchronously probe → 4×.
	// UNO_WEBGPU_MSAA=1|2|4|8 overrides the sample count (validated against the device; the browser
	// honours only 1, since the spec guarantees just 1 and 4 and Dawn init can't probe synchronously).
	private static uint? RequestedSampleCount()
		=> Environment.GetEnvironmentVariable("UNO_WEBGPU_MSAA") switch
		{
			"1" => 1u,
			"2" => 2u,
			"4" => 4u,
			"8" => 8u,
			_ => null,
		};

	private uint PickSampleCount()
	{
		// The WebGPU spec only guarantees sample counts 1 and 4, so the browser uses 4.
		if (_browser || OperatingSystem.IsBrowser()) { return RequestedSampleCount() is 1u ? 1u : 4u; }
		if (RequestedSampleCount() is { } requested && (requested == 1u || SupportsSampleCount(requested)))
		{
			return requested;
		}
		if (_hasFormatFeatures && SupportsSampleCount(2)) { return 2u; }
		if (SupportsSampleCount(4)) { return 4u; }
		return 1u;
	}

	private bool SupportsSampleCount(uint samples)
	{
		wgpuDevicePushErrorScope(Dev, WGPUErrorFilter.Validation);
		var td = new WGPUTextureDescriptor
		{
			Size = new WGPUExtent3D { Width = 1, Height = 1, DepthOrArrayLayers = 1 },
			Format = ColorFormat,
			MipLevelCount = 1,
			SampleCount = samples,
			Dimension = WGPUTextureDimension._2D,
			Usage = WGPUTextureUsage.RenderAttachment,
		};
		var tex = wgpuDeviceCreateTexture(Dev, &td);
		var box = new uint[2];
		var h = GCHandle.Alloc(box);
		wgpuDevicePopErrorScope(Dev, new WGPUPopErrorScopeCallbackInfo
		{
			Mode = WGPUCallbackMode.AllowProcessEvents,
			Callback = (IntPtr)(delegate* unmanaged[Cdecl]<WGPUPopErrorScopeStatus, WGPUErrorType, WGPUStringView, IntPtr, IntPtr, void>)&OnPopErrorScope,
			Userdata1 = GCHandle.ToIntPtr(h),
		});
		for (int i = 0; i < 1000 && box[0] == 0; i++) { wgpuInstanceProcessEvents(Inst); }
		h.Free();
		if (tex != IntPtr.Zero) { wgpuTextureDestroy(tex); }
		return box[1] == (uint)WGPUErrorType.NoError;
	}

	// --- neutral IWebGpuDeviceContext ---
	nint IWebGpuDeviceContext.Instance => Inst;
	nint IWebGpuDeviceContext.Adapter => Adapter;
	nint IWebGpuDeviceContext.Device => Dev;
	nint IWebGpuDeviceContext.Queue => Q;
	uint IWebGpuDeviceContext.ColorFormat => (uint)ColorFormat;
	uint IWebGpuDeviceContext.SampleCount => MsaaSamples;
	public GraphicsContextKind Kind => GraphicsContextKind.WebGpu;

	public void Dispose()
	{
		if (Smp != IntPtr.Zero) { wgpuSamplerRelease(Smp); Smp = IntPtr.Zero; }
		if (Q != IntPtr.Zero) { wgpuQueueRelease(Q); Q = IntPtr.Zero; }
		if (Dev != IntPtr.Zero) { wgpuDeviceRelease(Dev); Dev = IntPtr.Zero; }
		if (Adapter != IntPtr.Zero) { wgpuAdapterRelease(Adapter); Adapter = IntPtr.Zero; }
		if (Inst != IntPtr.Zero) { wgpuInstanceRelease(Inst); Inst = IntPtr.Zero; }
	}

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void OnAdapter(WGPURequestAdapterStatus status, IntPtr adapter, WGPUStringView message, IntPtr u1, IntPtr u2)
		=> ((IntPtr[])GCHandle.FromIntPtr(u1).Target!)[0] = adapter;

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void OnDevice(WGPURequestDeviceStatus status, IntPtr device, WGPUStringView message, IntPtr u1, IntPtr u2)
		=> ((IntPtr[])GCHandle.FromIntPtr(u1).Target!)[0] = device;

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void OnUncapturedError(IntPtr device, WGPUErrorType type, WGPUStringView message, IntPtr u1, IntPtr u2)
	{
		var msg = message.Data != IntPtr.Zero && message.Length > 0
			? System.Runtime.InteropServices.Marshal.PtrToStringUTF8(message.Data, (int)message.Length)
			: "";
		System.Console.Error.WriteLine($"[webgpu] uncaptured error ({type}): {msg}");
	}

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void OnPopErrorScope(WGPUPopErrorScopeStatus status, WGPUErrorType type, WGPUStringView message, IntPtr u1, IntPtr u2)
	{
		var box = (uint[])GCHandle.FromIntPtr(u1).Target!;
		box[0] = 1; box[1] = (uint)type;
	}
}
