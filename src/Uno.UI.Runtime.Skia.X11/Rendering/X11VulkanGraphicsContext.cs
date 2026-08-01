#nullable enable

using System;
using Uno.UI.Composition.Drawing;
using Uno.UI.Runtime.Skia.Vulkan;
using Uno.WinUI.Runtime.Skia.X11.Vulkan;

namespace Uno.WinUI.Runtime.Skia.X11;

/// <summary>
/// On-window Vulkan <see cref="IGraphicsContext"/> for X11, routing the Vulkan path through the neutral
/// GraphicsRegistry pipeline. It owns the shared <see cref="VulkanContext"/> (Vulkan device/swapchain + Skia
/// GRContext) and, per frame, acquires the device, ensures the cached Vulkan-backed SKSurface and hands the
/// backend a <see cref="SkiaRenderTarget"/> over its canvas; <see cref="Present"/> flushes and blits/presents.
/// Vulkan-on-Skia is inherently a Skia path (WebGPU-on-Vulkan is the separate neutral competitor), so the Skia
/// GRContext is reused rather than rebuilt behind a neutral seam. Falls through to the next kind if Vulkan is
/// unavailable (the ctor throws → negotiation continues).
/// </summary>
internal sealed class X11VulkanGraphicsContext : IGraphicsContext
{
	private readonly VulkanContext _vk;
	private IDisposable? _deviceLock;
	private int _width, _height;

	public X11VulkanGraphicsContext(X11Window x11Window)
	{
		if (!X11VulkanSurfaceFactory.IsVulkanAvailable())
		{
			throw new InvalidOperationException("Vulkan rendering not available: libvulkan.so.1 not found");
		}

		var display = x11Window.Display;
		var window = x11Window.Window;
		using var lockDisposable = X11Helper.XLock(display);
		XWindowAttributes attributes = default;
		_ = XLib.XGetWindowAttributes(display, window, ref attributes);
		_width = Math.Max(attributes.width, 1);
		_height = Math.Max(attributes.height, 1);

		var factory = new X11VulkanSurfaceFactory(display);
		_vk = new VulkanContext();
		_vk.Initialize(factory, window, _width, _height);
	}

	public GraphicsContextKind Kind => GraphicsContextKind.Vulkan;

	public bool IsLost => false;

	public IRenderTarget AcquireRenderTarget(int width, int height)
	{
		width = Math.Max(1, width);
		height = Math.Max(1, height);
		if (width != _width || height != _height)
		{
			_width = width;
			_height = height;
			_vk.InvalidateCachedSurface();
			_vk.ResizeRenderImage(width, height);
			_vk.GrContext?.ResetContext();
		}

		_deviceLock = _vk.Device?.Lock();
		_vk.GrContext?.ResetContext();
		_vk.EnsureCachedSurface();
		var surface = _vk.CachedSkSurface ?? throw new InvalidOperationException("Failed to create the Vulkan SKSurface");
		return new SkiaRenderTarget(surface.Canvas);
	}

	public void Present()
	{
		if (_vk.CachedSkSurface is { } surface && _vk.GrContext is { } grContext)
		{
			surface.Canvas.Flush();
			grContext.Flush();
			_vk.BlitAndPresent();
		}
		_deviceLock?.Dispose();
		_deviceLock = null;
	}

	public void Dispose()
	{
		_deviceLock?.Dispose();
		_deviceLock = null;
		_vk.Dispose();
	}
}
