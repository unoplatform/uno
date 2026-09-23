#nullable enable

using System;
using Uno.UI.Composition.Drawing;
using Uno.UI.Runtime.Skia.Vulkan;
using Uno.UI.Runtime.Skia.Win32.Vulkan;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace Uno.UI.Runtime.Skia.Win32;

/// <summary>
/// On-window Vulkan graphics context for Win32 (the mirror of <c>X11VulkanGraphicsContext</c>): owns the Vulkan
/// device/swapchain and exposes it as a neutral <see cref="IVulkanDeviceContext"/>. The device lock is held for
/// the whole frame; <see cref="Present"/> blits the render image to the swapchain and releases it. The ctor throws
/// when Vulkan is unavailable so negotiation falls through to the next kind.
/// </summary>
internal sealed class Win32VulkanGraphicsContext : ISwapChain, IVulkanDeviceContext, IWin32PacedContext, IWin32PresentReporting
{
	private readonly VulkanContext _vk;
	// MAILBOX present returns without blocking, so the render thread is paced here; otherwise it spins at
	// thousands of fps rendering frames the presentation engine discards, and FrameRate is ignored.
	private readonly Win32RenderPacer _pacer;
	private IDisposable? _frameLock;
	private bool _skipPresent;
	private bool _presented;
	private int _width, _height;

	public bool PresentedLastFrame => _presented;

	public Win32VulkanGraphicsContext(HWND hwnd)
	{
		if (!Win32VulkanSurfaceFactory.IsVulkanAvailable())
		{
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

		var factory = new Win32VulkanSurfaceFactory();
		_vk = new VulkanContext();
		_vk.Initialize(factory, hwnd.Value, _width, _height);

		// Created last so a declined negotiation (the throw above) doesn't leave a timer behind.
		_pacer = new Win32RenderPacer(
			FeatureConfiguration.CompositionTarget.FrameRate,
			FeatureConfiguration.CompositionTarget.SetFrameRateAsScreenRefreshRate);
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
			_pacer.WaitForNextFrame();
		}

		_frameLock.Dispose();
		_frameLock = null;
	}

	// Retargets the pacer's timer (the degraded DwmFlush fallback, or the active pacer under a fixed FrameRate)
	// when the window moves to a display with a different refresh rate.
	public void UpdateRefreshRate(double fps) => _pacer.UpdateTargetFps(fps);

	public void Dispose()
	{
		_frameLock?.Dispose();
		_frameLock = null;
		_pacer.Dispose();
		_vk.Dispose();
	}
}
