#nullable enable

using System;
using Uno.UI.Composition.Drawing;
using Uno.UI.Runtime.Skia.Vulkan;

namespace Uno.UI.Runtime.Skia.Android;

/// <summary>
/// On-window Vulkan graphics context for Android (the Android mirror of the Win32/X11 Vulkan contexts). It
/// completes an already device-initialized <see cref="VulkanContext"/> for a specific <c>ANativeWindow</c> and
/// exposes the device as a neutral <see cref="IVulkanDeviceContext"/>. Unlike Win32/X11, the device is owned by
/// <see cref="UnoVulkanView"/> and outlives this wrapper across surface re-creations (Android tears down and
/// recreates the Surface far more often than a desktop window is recreated) — only the window-scoped swapchain
/// is created and torn down here. The device lock is held for the whole frame (acquire → render → present);
/// <see cref="Present"/> blits the render image to the swapchain and releases the lock.
/// </summary>
internal sealed class AndroidVulkanGraphicsContext : ISwapChain, IVulkanDeviceContext
{
	private readonly VulkanContext _vk;
	private IDisposable? _frameLock;
	private int _width, _height;

	public AndroidVulkanGraphicsContext(VulkanContext vk, IntPtr nativeWindow, int width, int height)
	{
		_vk = vk;
		_width = Math.Max(width, 1);
		_height = Math.Max(height, 1);

		_vk.InitializeSurface(nativeWindow, _width, _height);
	}

	public GraphicsContextKind Kind => GraphicsContextKind.Vulkan;

	private bool _firstFramePresented;

	// The Vulkan render image is stable across frames once the first frame has been presented on this swapchain,
	// so subsequent frames can repaint only the damaged region. On a newly initialized swapchain, the previous
	// frame's contents are not preserved and the entire frame must be repainted.
	public bool PreservesContents => _firstFramePresented;

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
		_firstFramePresented = true;
		_frameLock.Dispose();
		_frameLock = null;
	}

	public void Dispose()
	{
		// SurfaceDestroyed reaches here on the UI thread while the render thread may still own this frame's device
		// lock; releasing it from here is a cross-thread Monitor.Exit, which throws and takes the app down during an
		// activity relaunch. The owning thread releases it in Present.
		if (_vk.IsLockedByCurrentThread)
		{
			_frameLock?.Dispose();
		}

		_frameLock = null;
		// The device is owned by UnoVulkanView and reused for the next surface; only the window-scoped
		// swapchain/render image are released here.
		_vk.DisposeSurfaceResources();
		_firstFramePresented = false;
	}
}
