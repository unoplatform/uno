#nullable enable

using System;
using Uno.UI.Composition.Drawing;
using Uno.UI.Runtime.Skia.Vulkan;
using Uno.UI.Runtime.Skia.Win32.Vulkan;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace Uno.UI.Runtime.Skia.Win32;

/// <summary>
/// On-window Vulkan graphics context for Win32, on the neutral (kind)=>context seam — the Win32 mirror of
/// <c>X11VulkanGraphicsContext</c>. It owns the Vulkan device/swapchain (<see cref="VulkanContext"/>) and exposes
/// the device details as a neutral <see cref="IVulkanDeviceContext"/>; the Skia backend builds its own
/// <c>GRContext</c>-Vulkan over the render image handed as an <see cref="IVulkanRenderTarget"/>. The device lock is
/// held for the whole frame (acquire → backend render → present); <see cref="Present"/> blits the render image to
/// the swapchain and releases the lock. Names no Skia type. Chosen by negotiation for
/// <see cref="GraphicsContextKind.Vulkan"/> (Win32 gates it on <c>UseVulkanOnWin32</c>); the ctor throws when
/// Vulkan is unavailable → negotiation falls through.
/// </summary>
internal sealed class Win32VulkanGraphicsContext : ISwapChain, IVulkanDeviceContext
{
	private readonly VulkanContext _vk;
	private IDisposable? _frameLock;
	private int _width, _height;

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
	}

	public GraphicsContextKind Kind => GraphicsContextKind.Vulkan;

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
		_vk.BlitAndPresent();
		_frameLock?.Dispose();
		_frameLock = null;
	}

	public void Dispose()
	{
		_frameLock?.Dispose();
		_frameLock = null;
		_vk.Dispose();
	}
}
