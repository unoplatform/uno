#nullable enable

using System;
using Uno.UI.Composition.Drawing;
using Uno.UI.Runtime.Vulkan;
using Uno.UI.Runtime.X11.Vulkan;

namespace Uno.UI.Runtime.X11;

/// <summary>
/// On-window Vulkan graphics context for X11: owns the Vulkan device/swapchain (<see cref="VulkanContext"/>) and
/// exposes it as a neutral <see cref="IVulkanDeviceContext"/>. The device lock is held for the whole frame;
/// <see cref="Present"/> blits the render image to the swapchain and releases it.
/// </summary>
internal sealed class X11VulkanGraphicsContext : ISwapChain, IVulkanDeviceContext
{
	// Popups render on one instance and device: a window for each is a device for each otherwise, and a process only
	// gets so many. Its surfaces go through a connection of its own, which outlives any popup's. UI thread only.
	private static VulkanContext? _sharedPopupDevice;
	private static IntPtr _sharedPopupDeviceDisplay;
	private static int _sharedPopupDeviceUsers;

	private readonly VulkanContext _vk;
	private readonly bool _usesSharedDevice;
	private IDisposable? _frameLock;
	private bool _skipPresent;
	private int _width, _height;

	public X11VulkanGraphicsContext(X11Window x11Window, bool transparent = false)
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

		if (transparent)
		{
			var device = AcquireSharedPopupDevice();
			try
			{
				_vk = VulkanContext.ForSharedDevice(device);
				_vk.InitializeSurface(window, _width, _height, premultipliedAlpha: true);
				_usesSharedDevice = true;
			}
			catch
			{
				_vk?.Dispose();
				ReleaseSharedPopupDevice();
				throw;
			}
		}
		else
		{
			_vk = new VulkanContext();
			_vk.Initialize(new X11VulkanSurfaceFactory(display), window, _width, _height);
		}
	}

	private static VulkanContext AcquireSharedPopupDevice()
	{
		if (_sharedPopupDevice is null)
		{
			_sharedPopupDeviceDisplay = XLib.XOpenDisplay(IntPtr.Zero);
			VulkanContext device = new();
			try
			{
				device.InitializeDevice(new X11VulkanSurfaceFactory(_sharedPopupDeviceDisplay));
			}
			catch
			{
				device.Dispose();
				_ = XLib.XCloseDisplay(_sharedPopupDeviceDisplay);
				_sharedPopupDeviceDisplay = IntPtr.Zero;
				throw;
			}

			_sharedPopupDevice = device;
		}

		_sharedPopupDeviceUsers++;
		return _sharedPopupDevice;
	}

	private static void ReleaseSharedPopupDevice()
	{
		if (--_sharedPopupDeviceUsers == 0)
		{
			_sharedPopupDevice?.Dispose();
			_sharedPopupDevice = null;
			_ = XLib.XCloseDisplay(_sharedPopupDeviceDisplay);
			_sharedPopupDeviceDisplay = IntPtr.Zero;
		}
	}

	/// <summary>Holds the device for a teardown: other windows may be rendering on the same one.</summary>
	internal IDisposable LockDevice() => _vk.Lock();

	public GraphicsContextKind Kind => GraphicsContextKind.Vulkan;

	/// <summary>False when the last frame was dropped for a resized swapchain.</summary>
	internal bool PresentedLastFrame { get; private set; } = true;

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
			// The frame about to be composed was recorded at the old size; presenting it into the resized
			// swapchain shows a stretched/garbled image, so this one frame is dropped.
			_skipPresent = true;
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

		if (_skipPresent)
		{
			_skipPresent = false;
			PresentedLastFrame = false;
		}
		else
		{
			_vk.BlitAndPresent();
			PresentedLastFrame = true;
		}

		_frameLock.Dispose();
		_frameLock = null;
	}

	public void Dispose()
	{
		_frameLock?.Dispose();
		_frameLock = null;
		_vk.Dispose();

		if (_usesSharedDevice)
		{
			ReleaseSharedPopupDevice();
		}
	}
}
