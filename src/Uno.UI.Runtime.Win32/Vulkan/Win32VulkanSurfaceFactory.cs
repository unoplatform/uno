using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Uno.UI.Runtime.Vulkan;
using Uno.UI.Runtime.Vulkan.Interop;
using Uno.UI.Runtime.Vulkan.UnmanagedInterop;
using Windows.Win32;
using Windows.Win32.Devices.Display;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;

namespace Uno.UI.Runtime.Win32.Vulkan;

[StructLayout(LayoutKind.Sequential)]
internal struct VkWin32SurfaceCreateInfoKHR
{
	public const uint VK_STRUCTURE_TYPE_WIN32_SURFACE_CREATE_INFO_KHR = 1000009000;
	public uint sType;
	public IntPtr pNext;
	public uint flags;
	public IntPtr hinstance;
	public IntPtr hwnd;
}

internal class Win32VulkanSurfaceFactory : IVulkanPlatformSurfaceFactory
{
	[DllImport("vulkan-1.dll", EntryPoint = "vkGetInstanceProcAddr")]
	private static extern IntPtr NativeGetInstanceProcAddr(IntPtr instance, [MarshalAs(UnmanagedType.LPStr)] string name);

	private static bool _vulkanAvailable;
	private static bool _vulkanChecked;

	// Lets tests put a window on another adapter's display without a second adapter.
	internal static long? PresentingAdapterLuidOverride { get; set; }

	public IReadOnlyList<string> RequiredInstanceExtensions { get; } = new[] { "VK_KHR_win32_surface" };

	// The loader orders devices as DXGI orders adapters, and WinUI renders on the first of those: the primary display's
	// adapter, or the app's choice in the Windows graphics settings.
	public bool PrefersDiscreteDevice => false;

	public VkGetInstanceProcAddressDelegate GetVkGetInstanceProcAddr()
	{
		EnsureVulkanAvailable();
		return NativeGetInstanceProcAddr;
	}

	public ulong CreateSurface(VulkanInstance instance, IntPtr nativeWindowHandle)
	{
		if (nativeWindowHandle == IntPtr.Zero)
			throw new ArgumentException("HWND cannot be zero", nameof(nativeWindowHandle));

		var createSurfacePtr = instance.GetInstanceProcAddress(instance.Handle.Handle, "vkCreateWin32SurfaceKHR");
		if (createSurfacePtr == IntPtr.Zero)
			throw new VulkanException("Failed to load vkCreateWin32SurfaceKHR");

		var vkCreateWin32SurfaceKHR = Marshal.GetDelegateForFunctionPointer<PFN_vkCreateWin32SurfaceKHR>(createSurfacePtr);

		var createInfo = new VkWin32SurfaceCreateInfoKHR
		{
			sType = VkWin32SurfaceCreateInfoKHR.VK_STRUCTURE_TYPE_WIN32_SURFACE_CREATE_INFO_KHR,
			hinstance = Win32Helper.GetModuleHInstance(),
			hwnd = nativeWindowHandle
		};

		var result = vkCreateWin32SurfaceKHR(instance.Handle.Handle, ref createInfo, IntPtr.Zero, out var surface);
		if (result != 0) // VK_SUCCESS = 0
			throw new VulkanException($"vkCreateWin32SurfaceKHR failed with result {result}");

		return surface;
	}

	public bool SupportsPresentation(VulkanInstance instance, VkPhysicalDevice physicalDevice, uint queueFamilyIndex)
	{
		var queryPtr = instance.GetInstanceProcAddress(instance.Handle.Handle, "vkGetPhysicalDeviceWin32PresentationSupportKHR");
		if (queryPtr == IntPtr.Zero)
		{
			return true;
		}

		var query = Marshal.GetDelegateForFunctionPointer<PFN_vkGetPhysicalDeviceWin32PresentationSupportKHR>(queryPtr);
		return query(physicalDevice.Handle, queueFamilyIndex) != 0;
	}

	/// <summary>
	/// Finds the adapter driving the window's monitor, or null if the display configuration doesn't report one.
	/// </summary>
	internal static unsafe long? GetPresentingAdapterLuid(HWND hwnd)
	{
		if (PresentingAdapterLuidOverride is { } luidOverride)
		{
			return luidOverride;
		}

		var monitor = PInvoke.MonitorFromWindow(hwnd, MONITOR_FROM_FLAGS.MONITOR_DEFAULTTONEAREST);
		var monitorInfo = new MONITORINFOEXW { monitorInfo = new MONITORINFO { cbSize = (uint)sizeof(MONITORINFOEXW) } };
		if (!PInvoke.GetMonitorInfo(monitor, (MONITORINFO*)&monitorInfo))
		{
			return null;
		}

		uint pathCount;
		DISPLAYCONFIG_PATH_INFO[] paths;
		WIN32_ERROR result;

		// The display configuration can change between the two calls, as it does while a screen is being unplugged.
		do
		{
			if (PInvoke.GetDisplayConfigBufferSizes(QUERY_DISPLAY_CONFIG_FLAGS.QDC_ONLY_ACTIVE_PATHS, out pathCount, out var modeCount) != WIN32_ERROR.ERROR_SUCCESS)
			{
				return null;
			}

			paths = new DISPLAYCONFIG_PATH_INFO[pathCount];
			var modes = new DISPLAYCONFIG_MODE_INFO[modeCount];
			fixed (DISPLAYCONFIG_PATH_INFO* pPaths = paths)
			fixed (DISPLAYCONFIG_MODE_INFO* pModes = modes)
			{
				result = PInvoke.QueryDisplayConfig(QUERY_DISPLAY_CONFIG_FLAGS.QDC_ONLY_ACTIVE_PATHS, ref pathCount, pPaths, ref modeCount, pModes, null);
			}
		}
		while (result == WIN32_ERROR.ERROR_INSUFFICIENT_BUFFER);

		if (result != WIN32_ERROR.ERROR_SUCCESS)
		{
			return null;
		}

		var monitorName = monitorInfo.szDevice.ToString();
		for (var i = 0; i < pathCount; i++)
		{
			var source = new DISPLAYCONFIG_SOURCE_DEVICE_NAME
			{
				header = new DISPLAYCONFIG_DEVICE_INFO_HEADER
				{
					type = DISPLAYCONFIG_DEVICE_INFO_TYPE.DISPLAYCONFIG_DEVICE_INFO_GET_SOURCE_NAME,
					size = (uint)sizeof(DISPLAYCONFIG_SOURCE_DEVICE_NAME),
					adapterId = paths[i].sourceInfo.adapterId,
					id = paths[i].sourceInfo.id,
				},
			};

			if (PInvoke.DisplayConfigGetDeviceInfo(&source.header) == 0
				&& source.viewGdiDeviceName.ToString() == monitorName)
			{
				var luid = paths[i].sourceInfo.adapterId;
				return ((long)luid.HighPart << 32) | luid.LowPart;
			}
		}

		return null;
	}

	public static bool IsVulkanAvailable()
	{
		if (!_vulkanChecked)
		{
			_vulkanChecked = true;
			try
			{
				NativeGetInstanceProcAddr(IntPtr.Zero, "vkEnumerateInstanceVersion");
				_vulkanAvailable = true;
			}
			catch (DllNotFoundException)
			{
				_vulkanAvailable = false;
			}
			catch (EntryPointNotFoundException)
			{
				_vulkanAvailable = true;
			}
		}
		return _vulkanAvailable;
	}

	private static void EnsureVulkanAvailable()
	{
		if (!IsVulkanAvailable())
			throw new VulkanException("vulkan-1.dll not found");
	}

	private delegate uint PFN_vkGetPhysicalDeviceWin32PresentationSupportKHR(IntPtr physicalDevice, uint queueFamilyIndex);

	private delegate int PFN_vkCreateWin32SurfaceKHR(IntPtr instance, ref VkWin32SurfaceCreateInfoKHR pCreateInfo, IntPtr pAllocator, out ulong pSurface);
}
