#nullable enable

using System;
using Uno.UI.Composition.Drawing;
using Uno.UI.Runtime.Skia.Vulkan;
using Uno.WinUI.Runtime.Skia.Android.Platform.Vulkan;

namespace Uno.UI.Runtime.Skia.Android;

/// <summary>
/// On-window Vulkan graphics context for Android (the Android mirror of the Win32/X11 Vulkan contexts). It owns the
/// Vulkan device/swapchain over the <c>ANativeWindow</c> and exposes the device as a neutral
/// <see cref="IVulkanDeviceContext"/>. The device lock is held for the whole frame (acquire → render → present);
/// <see cref="Present"/> blits the render image to the swapchain and releases the lock. The ctor throws when Vulkan
/// is unavailable, letting the view fall back to the canvas render view.
/// </summary>
internal sealed class AndroidVulkanGraphicsContext : ISwapChain, IVulkanDeviceContext
{
	private readonly VulkanContext _vk;
	private IDisposable? _frameLock;
	private int _width, _height;

	public AndroidVulkanGraphicsContext(IntPtr nativeWindow, int width, int height)
	{
		if (!AndroidVulkanSurfaceFactory.IsVulkanAvailable())
		{
			throw new InvalidOperationException("Vulkan rendering not available: libvulkan.so not found");
		}

		_width = Math.Max(width, 1);
		_height = Math.Max(height, 1);

		var factory = new AndroidVulkanSurfaceFactory();
		_vk = new VulkanContext();
		_vk.Initialize(factory, nativeWindow, _width, _height);
	}

	public GraphicsContextKind Kind => GraphicsContextKind.Vulkan;

	// The Vulkan render image is stable across frames (resized only on change), so it keeps the previous frame's
	// pixels — the compositor can repaint only the damaged region.
	public bool PreservesContents => true;

	// Neutral device face — the GRVkBackendContext inputs the Skia backend reads to build its GRContext-Vulkan.
	public nint Instance => _vk.InstancePtr;
	public nint PhysicalDevice => _vk.PhysicalDevicePtr;
	public nint Device => _vk.DevicePtr;
	public nint Queue => _vk.QueuePtr;
	public uint GraphicsQueueFamilyIndex => _vk.GraphicsQueueFamilyIndex;
	public uint MaxApiVersion => _vk.MaxApiVersion;
	public string[] InstanceExtensions => _vk.EnabledInstanceExtensions;
	public string[] DeviceExtensions => _vk.EnabledDeviceExtensions;
	public Func<string, nint, nint, nint> GetProcAddress => _vk.GetProcAddress;

	public IRenderTarget AcquireRenderTarget(int width, int height)
	{
		width = Math.Max(1, width);
		height = Math.Max(1, height);

		// Hold the device lock across the backend's render (GRContext-Vulkan ops) and this frame's present.
		_frameLock = _vk.Lock();

		if (width != _width || height != _height)
		{
			_width = width;
			_height = height;
			_vk.ResizeRenderImage(width, height);
		}

		return _vk.CurrentRenderTarget;
	}

	public void Present()
	{
		// No frame was acquired this tick (the compositor skipped drawing — e.g. no recorded frame yet or empty
		// bounds), so the device lock was never taken; there is nothing to blit/present.
		if (_frameLock is null)
		{
			return;
		}

		_vk.BlitAndPresent();
		_frameLock.Dispose();
		_frameLock = null;
	}

	public void Dispose()
	{
		_frameLock?.Dispose();
		_frameLock = null;
		_vk.Dispose();
	}
}
