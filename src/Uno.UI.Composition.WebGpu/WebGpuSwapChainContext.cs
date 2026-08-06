// Shared on-window WebGPU context for NATIVE hosts (X11, Win32, macOS, …). The WebGPU swapchain semantics are
// identical across them — only the platform surface source differs — so hosts create a device + a surface (via
// the CreateXxxSurface factories) and this type owns acquire/configure/present. The browser is separate: it
// presents implicitly and blits (no wgpuSurfacePresent), so it has its own context.
#nullable enable

using System;
using Uno.UI.Composition.Drawing;
using Uno.WebGpu.Native;
using static Uno.WebGpu.Native.WGPU;

namespace Uno.UI.Composition.WebGpu;

public sealed unsafe class WebGpuSwapChainContext : IGraphicsContext, IWebGpuDeviceContext
{
	private readonly WebGpuDevice _device;
	private IntPtr _surface;
	private WebGpuRenderSurface? _target;
	private IntPtr _currentTexture;
	private IntPtr _currentView;
	private int _w, _h;
	private bool _configured;

	/// <param name="createSurface">Builds the wgpu surface for the platform window, given the instance handle
	/// (use one of the CreateXxxSurface factories).</param>
	public WebGpuSwapChainContext(WGPUTextureFormat colorFormat, Func<IntPtr, IntPtr> createSurface)
	{
		_device = new WebGpuDevice(colorFormat);
		_surface = createSurface(_device.Inst);
		if (_surface == IntPtr.Zero)
		{
			throw new InvalidOperationException("Failed to create the wgpu surface for this window.");
		}
	}

	public WebGpuDevice Device => _device;
	public GraphicsContextKind Kind => GraphicsContextKind.WebGpu;
	public bool IsLost => false;

	public IRenderTarget AcquireRenderTarget(int width, int height)
	{
		width = Math.Max(1, width);
		height = Math.Max(1, height);
		Configure(width, height);

		if (_currentView != IntPtr.Zero)
		{
			return _target!;
		}

		WGPUSurfaceTexture st = default;
		wgpuSurfaceGetCurrentTexture(_surface, &st);
		if ((st.Status != WGPUSurfaceGetCurrentTextureStatus.SuccessOptimal
				&& st.Status != WGPUSurfaceGetCurrentTextureStatus.SuccessSuboptimal)
			|| st.Texture == IntPtr.Zero)
		{
			return _target!;
		}

		_currentTexture = st.Texture;
		_currentView = wgpuTextureCreateView(st.Texture, null);
		_target!.View = _currentView;
		return _target;
	}

	public void Present()
	{
		if (_currentView == IntPtr.Zero)
		{
			return;
		}
		wgpuSurfacePresent(_surface);
		wgpuTextureViewRelease(_currentView);
		wgpuTextureRelease(_currentTexture);
		_currentView = IntPtr.Zero;
		_currentTexture = IntPtr.Zero;
	}

	private void Configure(int width, int height)
	{
		if (_configured && width == _w && height == _h)
		{
			return;
		}
		_w = width;
		_h = height;
		_currentView = IntPtr.Zero;
		_currentTexture = IntPtr.Zero;
		_target?.Dispose();
		_target = new WebGpuRenderSurface(_device, width, height, externalColor: true);

		WGPUSurfaceCapabilities caps = default;
		wgpuSurfaceGetCapabilities(_surface, _device.Adapter, &caps);
		var format = _device.ColorFormat;
		bool supported = false;
		for (nuint i = 0; i < caps.FormatCount; i++) { if (caps.Formats[i] == format) { supported = true; break; } }
		if (!supported && caps.FormatCount > 0) { format = caps.Formats[0]; }
		var alphaMode = caps.AlphaModeCount > 0 ? caps.AlphaModes[0] : WGPUCompositeAlphaMode.Auto;

		var cfg = new WGPUSurfaceConfiguration
		{
			Device = _device.Dev,
			Format = format,
			Usage = WGPUTextureUsage.RenderAttachment,
			Width = (uint)width,
			Height = (uint)height,
			PresentMode = WGPUPresentMode.Fifo,
			AlphaMode = alphaMode,
		};
		wgpuSurfaceConfigure(_surface, &cfg);
		_configured = true;
	}

	public void Dispose()
	{
		_target?.Dispose();
		if (_surface != IntPtr.Zero) { wgpuSurfaceRelease(_surface); _surface = IntPtr.Zero; }
		_device.Dispose();
	}

	// ---- Platform surface factories (build the chained surface source + create the wgpu surface) ----

	public static IntPtr CreateXlibSurface(IntPtr instance, IntPtr display, ulong window)
	{
		var xlib = new WGPUSurfaceSourceXlibWindow
		{
			Chain = new WGPUChainedStruct { SType = WGPUSType.SurfaceSourceXlibWindow },
			Display = display,
			Window = window,
		};
		var desc = new WGPUSurfaceDescriptor { NextInChain = (WGPUChainedStruct*)&xlib };
		return wgpuInstanceCreateSurface(instance, &desc);
	}

	public static IntPtr CreateHwndSurface(IntPtr instance, IntPtr hinstance, IntPtr hwnd)
	{
		var hwndSrc = new WGPUSurfaceSourceWindowsHWND
		{
			Chain = new WGPUChainedStruct { SType = WGPUSType.SurfaceSourceWindowsHWND },
			Hinstance = hinstance,
			Hwnd = hwnd,
		};
		var desc = new WGPUSurfaceDescriptor { NextInChain = (WGPUChainedStruct*)&hwndSrc };
		return wgpuInstanceCreateSurface(instance, &desc);
	}

	public static IntPtr CreateAndroidSurface(IntPtr instance, IntPtr aNativeWindow)
	{
		var android = new WGPUSurfaceSourceAndroidNativeWindow
		{
			Chain = new WGPUChainedStruct { SType = WGPUSType.SurfaceSourceAndroidNativeWindow },
			Window = aNativeWindow,
		};
		var desc = new WGPUSurfaceDescriptor { NextInChain = (WGPUChainedStruct*)&android };
		return wgpuInstanceCreateSurface(instance, &desc);
	}

	public static IntPtr CreateMetalSurface(IntPtr instance, IntPtr metalLayer)
	{
		var metal = new WGPUSurfaceSourceMetalLayer
		{
			Chain = new WGPUChainedStruct { SType = WGPUSType.SurfaceSourceMetalLayer },
			Layer = metalLayer,
		};
		var desc = new WGPUSurfaceDescriptor { NextInChain = (WGPUChainedStruct*)&metal };
		return wgpuInstanceCreateSurface(instance, &desc);
	}
}
