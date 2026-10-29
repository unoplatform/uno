#nullable enable

using System;
using System.Threading;
using Uno.UI.Composition.Drawing;
using Uno.UI.Runtime.Vulkan;
using Uno.UI.Runtime.Win32.Vulkan;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace Uno.UI.Runtime.Win32;

/// <summary>
/// On-window Vulkan graphics context for Win32 (the mirror of <c>X11VulkanGraphicsContext</c>): owns the Vulkan
/// device/swapchain and exposes it as a neutral <see cref="IVulkanDeviceContext"/>. The device lock is held for
/// the whole frame; <see cref="Present"/> blits the render image to the swapchain and releases it. The ctor throws
/// when Vulkan is unavailable so negotiation falls through to the next kind.
/// </summary>
internal sealed class Win32VulkanGraphicsContext : ISwapChain, IVulkanDeviceContext, IWin32PacedContext, IWin32PresentReporting
{
	// Instance + device creation is ~90% of the Vulkan init cost and needs no window, so the first window's device
	// is created on a background thread while the app starts up.
	private static BackgroundPrewarm<VulkanContext>? _devicePrewarm;

	// Every window renders on one instance and device: drivers cap how many devices a process may create (an NVIDIA
	// one refuses the 14th), and each costs memory and creation time. Only the swapchain and render image are per
	// window. UI thread only.
	private static VulkanContext? _sharedDevice;
	private static int _sharedDeviceUsers;

	private readonly VulkanContext _vk;
	// MAILBOX present returns without blocking, so the render thread is paced here; otherwise it spins at
	// thousands of fps rendering frames the presentation engine discards, and FrameRate is ignored.
	private readonly Win32RenderPacer _pacer;
	private IDisposable? _frameLock;
	private bool _skipPresent;
	private bool _presented;
	private bool _disposed;
	private int _width, _height;

	public bool PresentedLastFrame => _presented;

	public Win32VulkanGraphicsContext(HWND hwnd)
	{
		// Taken before anything can throw: this is the first kind the first window negotiates, so no later step
		// would release the device.
		var prewarm = ClaimDevicePrewarm();

		if (!Win32VulkanSurfaceFactory.IsVulkanAvailable())
		{
			prewarm?.Discard();
			throw new InvalidOperationException("Vulkan rendering not available: vulkan-1.dll not found");
		}

		if (PInvoke.GetClientRect(hwnd, out RECT clientRect))
		{
			_width = Math.Max(clientRect.Width, 1);
			_height = Math.Max(clientRect.Height, 1);
		}
		else
		{
			_width = _height = 1;
		}

		var device = AcquireSharedDevice(prewarm);
		var vk = VulkanContext.ForSharedDevice(device);
		try
		{
			vk.InitializeSurface(hwnd.Value, _width, _height);
		}
		catch
		{
			vk.Dispose();
			ReleaseSharedDevice();
			throw;
		}

		_vk = vk;

		// Created last so a declined negotiation (the throw above) doesn't leave a timer behind.
		_pacer = new Win32RenderPacer(
			FeatureConfiguration.CompositionTarget.FrameRate,
			FeatureConfiguration.CompositionTarget.SetFrameRateAsScreenRefreshRate);
	}

	/// <summary>
	/// Starts creating the Vulkan instance and device for the first window. Call only when Vulkan is the kind the
	/// first window will negotiate first: that window's Vulkan attempt claims or releases it, and an unclaimed device
	/// costs tens of MB.
	/// </summary>
	internal static void StartDevicePrewarm()
	{
		if (_devicePrewarm is not null)
		{
			return;
		}

		_devicePrewarm = new BackgroundPrewarm<VulkanContext>(static () =>
		{
			var vk = new VulkanContext();
			try
			{
				vk.InitializeDevice(new Win32VulkanSurfaceFactory());
				return vk;
			}
			catch
			{
				vk.Dispose();
				throw;
			}
		});
	}

	private static BackgroundPrewarm<VulkanContext>? ClaimDevicePrewarm() => Interlocked.Exchange(ref _devicePrewarm, null);

	private static VulkanContext AcquireSharedDevice(BackgroundPrewarm<VulkanContext>? prewarm)
	{
		if (_sharedDevice is null)
		{
			_sharedDevice = prewarm is not null ? prewarm.Claim(static _ => { }, CreateDevice) : CreateDevice();
		}
		else
		{
			prewarm?.Discard();
		}

		_sharedDeviceUsers++;
		return _sharedDevice;
	}

	private static void ReleaseSharedDevice()
	{
		if (--_sharedDeviceUsers == 0)
		{
			_sharedDevice?.Dispose();
			_sharedDevice = null;
		}
	}

	private static VulkanContext CreateDevice()
	{
		var vk = new VulkanContext();
		try
		{
			vk.InitializeDevice(new Win32VulkanSurfaceFactory());
			return vk;
		}
		catch
		{
			vk.Dispose();
			throw;
		}
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
			// The frame about to be composed was recorded at the old size; presenting it into the resized
			// swapchain shows a stretched/garbled image, so this one frame is dropped.
			_skipPresent = true;
		}

		return _vk.CurrentRenderTarget;
	}

	public void Present()
	{
		_presented = false;
		// No frame was acquired this tick (the compositor skipped drawing — e.g. no recorded frame yet or empty
		// bounds), so the device lock was never taken; there is nothing to blit/present.
		if (_frameLock is null)
		{
			return;
		}

		_pacer.OnFrameStart();

		if (_skipPresent)
		{
			_skipPresent = false;
		}
		else
		{
			_vk.BlitAndPresent();
			_presented = true;
		}

		_frameLock.Dispose();
		_frameLock = null;

		// Outside the device lock, which every window shares: waiting for this window's next frame must not hold up
		// the others.
		if (_presented)
		{
			_pacer.WaitForNextFrame();
		}
	}

	/// <summary>Takes the lock every window's use of the shared device and queue is serialized on.</summary>
	public IDisposable LockDevice() => _vk.Lock();

	// Retargets the pacer's timer (the degraded DwmFlush fallback, or the active pacer under a fixed FrameRate)
	// when the window moves to a display with a different refresh rate.
	public void UpdateRefreshRate(double fps) => _pacer.UpdateTargetFps(fps);

	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}

		_disposed = true;
		_frameLock?.Dispose();
		_frameLock = null;
		_pacer.Dispose();
		_vk.Dispose();
		ReleaseSharedDevice();
	}
}
