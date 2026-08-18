#nullable enable
using System;
using System.Linq;
using System.Runtime.InteropServices;
using Uno.UI.Runtime.Skia.Vulkan.Interop;
using Uno.UI.Runtime.Skia.Vulkan.UnmanagedInterop;
using Uno.UI.Composition.Drawing;
using SkiaSharp;

namespace Uno.UI.Runtime.Skia.Vulkan;

/// <summary>
/// Unified Vulkan rendering context for Uno Platform.
/// Implements IVulkanPlatformGraphicsContext so it can be passed to all Interop/ classes.
/// Manages the full lifecycle: instance, device, surface, swapchain, and SkiaSharp integration.
/// </summary>
internal sealed class VulkanContext : IVulkanPlatformGraphicsContext, IDisposable
{
	private IVulkanInstance? _instance;
	private VulkanDevice? _device;
	private VulkanDisplay? _display;
	private VulkanImage? _renderImage;
	private VulkanInstanceApi? _instanceApi;
	private VulkanDeviceApi? _deviceApi;
	private bool _disposed;
	private IVulkanPlatformSurfaceFactory? _factory;
	private IntPtr _nativeWindowHandle;

	// IVulkanPlatformGraphicsContext implementation
	public IVulkanDevice Device => _device!;
	public IVulkanInstance Instance => _instance!;
	public VulkanInstanceApi InstanceApi => _instanceApi!;
	public VulkanDeviceApi DeviceApi => _deviceApi!;
	public VkDevice DeviceHandle => new() { Handle = _device!.Handle };
	public VkPhysicalDevice PhysicalDeviceHandle => new() { Handle = _device!.PhysicalDeviceHandle };
	public VkInstance InstanceHandle => new() { Handle = _instance!.Handle };
	public VkQueue MainQueueHandle => new() { Handle = _device!.MainQueueHandle };
	public uint GraphicsQueueFamilyIndex => _device!.GraphicsQueueFamilyIndex;

	// --- Neutral device face (consumed by the host's IVulkanDeviceContext) ---
	// The Skia GRContext lives in the Skia backend now; this subsystem is Vulkan-native only and just exposes the
	// device details + the per-frame render image, so the backend builds its own GRContext-Vulkan over them.
	public nint InstancePtr => _instance!.Handle;
	public nint PhysicalDevicePtr => _device!.PhysicalDeviceHandle;
	public nint DevicePtr => _device!.Handle;
	public nint QueuePtr => _device!.MainQueueHandle;
	public uint MaxApiVersion => VulkanHelpers.MakeVersion(1, 1, 0);
	public string[] EnabledInstanceExtensions => _instance!.EnabledExtensions.ToArray();
	public string[] EnabledDeviceExtensions => _device!.EnabledExtensions.ToArray();

	// Vulkan get-proc: resolves a device proc when device != 0, else an instance proc (the GRVkBackendContext contract).
	public nint GetProcAddress(string name, nint instance, nint device)
	{
		if (device != IntPtr.Zero)
		{
			var addr = _instance!.GetDeviceProcAddress(device, name);
			if (addr != IntPtr.Zero) { return addr; }
		}
		if (instance != IntPtr.Zero)
		{
			var addr = _instance!.GetInstanceProcAddress(instance, name);
			if (addr != IntPtr.Zero) { return addr; }
		}
		return _instance!.GetInstanceProcAddress(IntPtr.Zero, name);
	}

	/// <summary>Acquires the device lock for a frame (held across the backend's render + this context's present).</summary>
	public IDisposable Lock() => _device!.Lock();

	/// <summary>The intermediate render image described neutrally, for the backend to wrap as its surface.</summary>
	public IVulkanRenderTarget CurrentRenderTarget => new VulkanRenderTarget(_renderImage!, _device!.GraphicsQueueFamilyIndex);

	/// <summary>
	/// Initialize the Vulkan context with a platform-specific surface factory and native window handle.
	/// </summary>
	public void Initialize(IVulkanPlatformSurfaceFactory factory, IntPtr nativeWindowHandle, int width, int height)
	{
		_factory = factory;
		_nativeWindowHandle = nativeWindowHandle;

		// Create Vulkan instance
		var getProcAddr = factory.GetVkGetInstanceProcAddr();
		_instance = VulkanInstance.Create(getProcAddr, factory.RequiredInstanceExtensions);

		// Create instance API
		_instanceApi = new VulkanInstanceApi(_instance);

		// Create surface so we can check device presentation support
		var surfaceHandle = factory.CreateSurface((VulkanInstance)_instance, nativeWindowHandle);
		var vkSurface = new VkSurfaceKHR { Handle = surfaceHandle };

		// Create device (checks surface presentation support)
		_device = VulkanDevice.Create((VulkanInstance)_instance, _instanceApi, vkSurface);

		// Destroy the temporary surface used for device selection
		_instanceApi.DestroySurfaceKHR(new VkInstance { Handle = _instance.Handle }, vkSurface, IntPtr.Zero);

		// Create device API
		_deviceApi = new VulkanDeviceApi(_device);

		// All subsequent operations access device handles and require the device lock
		using (_device.Lock())
		{
			// Create display (swapchain) via platform surface wrapper
			var platformSurface = new DirectVulkanSurface(nativeWindowHandle, new SKSizeI(width, height), factory);
			_display = VulkanDisplay.CreateDisplay(this, platformSurface);

			// Create intermediate render image (TransitionLayout submits commands, needs lock). The Skia backend
			// builds its own GRContext-Vulkan over this image via the neutral IVulkanDeviceContext/RenderTarget.
			_renderImage = new VulkanImage(this, _display.CommandBufferPool,
				_display.SurfaceFormat.format, new SKSizeI(width, height));
		}
	}

	/// <summary>
	/// Full resize: destroys and recreates the display, swapchain, and render image.
	/// Use for major lifecycle events (window recreation, reinitialization).
	/// </summary>
	public void Resize(int width, int height)
	{
		if (_display == null || _device == null || _factory == null)
			return;

		using (_device.Lock())
		{
			_deviceApi!.DeviceWaitIdle(DeviceHandle);

			_renderImage?.Dispose();
			_display.Dispose();

			var platformSurface = new DirectVulkanSurface(_nativeWindowHandle, new SKSizeI(width, height), _factory);
			_display = VulkanDisplay.CreateDisplay(this, platformSurface);
			_renderImage = new VulkanImage(this, _display.CommandBufferPool,
				_display.SurfaceFormat.format, new SKSizeI(width, height));
			// _cachedSkSurface will be lazily recreated on next EnsureCachedSurface
		}
	}

	/// <summary>
	/// Lightweight resize: only recreates the intermediate render image and cached SKSurface.
	/// The swapchain handles its own resize via VK_ERROR_OUT_OF_DATE_KHR during presentation.
	/// Use for window resize events where only the render target dimensions change.
	/// Must be called while holding the device lock.
	/// </summary>
	public void ResizeRenderImage(int width, int height)
	{
		if (_display == null || _device == null)
			return;

		_deviceApi!.DeviceWaitIdle(DeviceHandle);

		_renderImage?.Dispose();
		_renderImage = new VulkanImage(this, _display.CommandBufferPool,
			_display.SurfaceFormat.format, new SKSizeI(width, height));
	}

	/// <summary>
	/// Blit the intermediate render image to the swapchain and present.
	/// Call after Skia canvas/context flush, while holding the device lock.
	/// Used by platforms (Win32) that use a split StartPaint/EndPaint pattern.
	/// </summary>
	public void BlitAndPresent()
	{
		if (_display == null || _renderImage == null || _deviceApi == null)
			return;

		// Try to acquire next swapchain image without retrying.
		var acquireResult = _display.TryAcquireNextImage();
		if (acquireResult != 0) // VK_SUCCESS = 0
		{
			// Swapchain out of date — recreate it now.
			// This is safe because we're inside the device lock and not
			// mid-presentation. The previous segfaults were caused by
			// StartPresentation's retry loop calling RecreateSwapchain
			// while already holding partial presentation state.
			try
			{
				_deviceApi.DeviceWaitIdle(DeviceHandle);
				_display.RecreateSwapchainSafe();

				// Retry acquire after recreation
				acquireResult = _display.TryAcquireNextImage();
				if (acquireResult != 0)
					return; // Still failing — skip this frame
			}
			catch
			{
				return; // Recreation failed — skip this frame
			}
		}

		try
		{
			var commandBuffer = _display.CommandBufferPool.CreateCommandBuffer();
			commandBuffer.BeginRecording();
			_display.PrepareCurrentImageForBlit(commandBuffer);
			_display.BlitImageToCurrentImage(commandBuffer, _renderImage);
			_display.EndPresentation(commandBuffer);
		}
		catch (VulkanException)
		{
			// Presentation failed — skip this frame
		}
	}

	/// <summary>
	/// Get information about the initialized Vulkan device for diagnostic logging.
	/// </summary>
	public unsafe (string DeviceName, string DriverVersion) GetDeviceInfo()
	{
		if (_device == null || _instanceApi == null)
			return ("Unknown", "Unknown");

		_instanceApi.GetPhysicalDeviceProperties(PhysicalDeviceHandle, out var properties);

		var deviceName = Marshal.PtrToStringAnsi(new IntPtr(properties.deviceName)) ?? "Unknown";
		var major = (properties.driverVersion >> 22) & 0x3FF;
		var minor = (properties.driverVersion >> 12) & 0x3FF;
		var patch = properties.driverVersion & 0xFFF;
		var driverVersion = $"{major}.{minor}.{patch}";

		return (deviceName, driverVersion);
	}

	public void Dispose()
	{
		if (_disposed) return;
		_disposed = true;

		if (_device != null)
		{
			using (_device.Lock())
			{
				_deviceApi?.DeviceWaitIdle(DeviceHandle);
			}
		}

		_renderImage?.Dispose();
		_renderImage = null;

		_display?.Dispose();
		_display = null;

		_device?.Dispose();
		_device = null;

		(_instance as IDisposable)?.Dispose();
		_instance = null;
	}

	// Neutral view of the intermediate render image (the GRVkImageInfo inputs). Read at AcquireRenderTarget; the
	// Skia backend wraps it as an SKSurface via a GRContext-Vulkan built from this context's device details.
	private sealed class VulkanRenderTarget : IVulkanRenderTarget
	{
		private readonly VulkanImageInfo _info;
		private readonly uint _queueFamily;

		public VulkanRenderTarget(VulkanImage image, uint queueFamily)
		{
			_info = image.ImageInfo;
			_queueFamily = queueFamily;
		}

		public ulong Image => (ulong)_info.Handle;
		public ulong Memory => (ulong)_info.MemoryHandle;
		public ulong MemorySize => _info.MemorySize;
		public uint Format => _info.Format;
		public uint ImageTiling => _info.Tiling;
		public uint ImageLayout => _info.Layout;
		public uint ImageUsageFlags => _info.UsageFlags;
		public uint SampleCount => _info.SampleCount;
		public uint LevelCount => _info.LevelCount;
		public uint CurrentQueueFamily => _queueFamily;
		public bool Protected => _info.IsProtected;
		public int Width => _info.PixelSize.Width;
		public int Height => _info.PixelSize.Height;
		public GraphicsColorFormat ColorFormat => GraphicsColorFormat.Bgra8888;
		// The host renders into one stable intermediate image (recreated only on resize) and blits it to the
		// swapchain each present (BlitAndPresent), so the previous frame's pixels survive — the compositor can
		// repaint only the damaged region.
		public bool PreservesContents => true;
		public void Dispose() { }
	}

	/// <summary>
	/// Simple IVulkanKhrSurfacePlatformSurface implementation that wraps a native window handle directly.
	/// The surface was already created during Initialize — this provides the CreateSurface callback for
	/// VulkanDisplay/VulkanKhrSurface to use.
	/// </summary>
	private sealed class DirectVulkanSurface : IVulkanKhrSurfacePlatformSurface
	{
		private readonly IntPtr _nativeWindowHandle;
		private readonly IVulkanPlatformSurfaceFactory _factory;

		public DirectVulkanSurface(IntPtr nativeWindowHandle, SKSizeI size, IVulkanPlatformSurfaceFactory factory)
		{
			_nativeWindowHandle = nativeWindowHandle;
			_factory = factory;
			Size = size;
		}

		public SKSizeI Size { get; }

		public ulong CreateSurface(IVulkanPlatformGraphicsContext context)
		{
			// Create a new VkSurfaceKHR from the native window handle
			return _factory.CreateSurface((VulkanInstance)context.Instance, _nativeWindowHandle);
		}

		public void Dispose() { }
	}
}

// Extension to convert IntPtr to Vulkan handle structs for API calls
internal static class VulkanHandleExtensions
{
	public static VkDevice ToVkDevice(this IntPtr handle) => new VkDevice { Handle = handle };
	public static VkInstance ToVkInstance(this IntPtr handle) => new VkInstance { Handle = handle };
	public static VkPhysicalDevice ToVkPhysicalDevice(this IntPtr handle) => new VkPhysicalDevice { Handle = handle };
	public static VkQueue ToVkQueue(this IntPtr handle) => new VkQueue { Handle = handle };
}
