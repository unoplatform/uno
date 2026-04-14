using System;
using SkiaSharp;
using Uno.Foundation.Logging;
using Uno.UI.Hosting;
using Uno.UI.Runtime.Skia.Vulkan;
using Uno.WinUI.Runtime.Skia.X11.Vulkan;

namespace Uno.WinUI.Runtime.Skia.X11;

/// <summary>
/// Vulkan-backed X11 renderer. Extends X11Renderer to provide Vulkan hardware-accelerated
/// rendering via the shared VulkanContext, following the same pattern as the OpenGL and EGL renderers.
/// </summary>
internal class X11VulkanRenderer : X11Renderer
{
	private readonly VulkanContext _vulkanContext;
	private IDisposable? _deviceLock;
	private bool _needsFullResize;

	private X11VulkanRenderer(IXamlRootHost host, X11Window x11Window, VulkanContext vulkanContext)
		: base(host, x11Window)
	{
		_vulkanContext = vulkanContext;
	}

	public static X11VulkanRenderer Create(IXamlRootHost host, X11Window x11Window)
	{
		if (!X11VulkanSurfaceFactory.IsVulkanAvailable())
		{
			throw new InvalidOperationException("Vulkan rendering not available: libvulkan.so.1 not found");
		}

		var display = x11Window.Display;
		var window = x11Window.Window;

		using var lockDisposable = X11Helper.XLock(display);

		// Get initial window size
		XWindowAttributes attributes = default;
		_ = XLib.XGetWindowAttributes(display, window, ref attributes);
		var width = Math.Max(attributes.width, 1);
		var height = Math.Max(attributes.height, 1);

		var factory = new X11VulkanSurfaceFactory(display);
		var context = new VulkanContext();
		context.Initialize(factory, window, width, height);

		var (deviceName, driverVersion) = context.GetDeviceInfo();
		typeof(X11VulkanRenderer).Log().Info($"Vulkan rendering initialized: {deviceName}, {driverVersion}");

		return new X11VulkanRenderer(host, x11Window, context);
	}

	protected override SKSurface UpdateSize(int width, int height)
	{
		// The base class (X11Renderer.Render) disposes _surface before calling
		// UpdateSize. Since _surface IS our cached SKSurface, we must invalidate
		// the reference (already disposed by caller) and resize the render image.
		_needsFullResize = true;
		_vulkanContext.InvalidateCachedSurface();
		_vulkanContext.ResizeRenderImage(Math.Max(width, 1), Math.Max(height, 1));
		_vulkanContext.GrContext?.ResetContext();
		_vulkanContext.EnsureCachedSurface();

		return _vulkanContext.CachedSkSurface
			?? throw new InvalidOperationException("Failed to create Vulkan SKSurface");
	}

	protected override void MakeCurrent()
	{
		// Acquire device lock and prepare for rendering
		_deviceLock = _vulkanContext.Device?.Lock();
		_vulkanContext.GrContext?.ResetContext();
		_vulkanContext.EnsureCachedSurface();
	}

	protected override void Flush()
	{
		if (_vulkanContext.CachedSkSurface != null && _vulkanContext.GrContext != null)
		{
			_vulkanContext.CachedSkSurface.Canvas.Flush();
			_vulkanContext.GrContext.Flush();

			if (!_needsFullResize)
			{
				_vulkanContext.BlitAndPresent();
			}
			// else: skip present this frame — resize changed the window
			// but we rendered at the old size. Next frame will be correct.
			_needsFullResize = false;
		}

		// Release device lock at end of frame
		_deviceLock?.Dispose();
		_deviceLock = null;
	}

	public override void Dispose()
	{
		_deviceLock?.Dispose();
		_deviceLock = null;
		_vulkanContext.Dispose();
	}
}
