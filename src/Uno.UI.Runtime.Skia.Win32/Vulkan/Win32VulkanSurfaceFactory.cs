using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Uno.UI.Runtime.Skia.Vulkan;
using Uno.UI.Runtime.Skia.Vulkan.Interop;
using Uno.UI.Runtime.Skia.Vulkan.UnmanagedInterop;

namespace Uno.UI.Runtime.Skia.Win32.Vulkan;

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

	[DllImport("kernel32.dll")]
	private static extern IntPtr GetModuleHandle(string? lpModuleName);

	private static bool _vulkanAvailable;
	private static bool _vulkanChecked;

	public IReadOnlyList<string> RequiredInstanceExtensions { get; } = new[] { "VK_KHR_win32_surface" };

	public VkGetInstanceProcAddressDelegate GetVkGetInstanceProcAddr()
	{
		EnsureVulkanAvailable();
		return NativeGetInstanceProcAddr;
	}

	public ulong CreateSurface(VulkanInstance instance, IntPtr nativeWindowHandle)
	{
		if (nativeWindowHandle == IntPtr.Zero)
			throw new ArgumentException("HWND cannot be zero", nameof(nativeWindowHandle));

		// Load vkCreateWin32SurfaceKHR
		var createSurfacePtr = instance.GetInstanceProcAddress(instance.Handle.Handle, "vkCreateWin32SurfaceKHR");
		if (createSurfacePtr == IntPtr.Zero)
			throw new VulkanException("Failed to load vkCreateWin32SurfaceKHR");

		var vkCreateWin32SurfaceKHR = Marshal.GetDelegateForFunctionPointer<PFN_vkCreateWin32SurfaceKHR>(createSurfacePtr);

		var createInfo = new VkWin32SurfaceCreateInfoKHR
		{
			sType = VkWin32SurfaceCreateInfoKHR.VK_STRUCTURE_TYPE_WIN32_SURFACE_CREATE_INFO_KHR,
			hinstance = GetModuleHandle(null),
			hwnd = nativeWindowHandle
		};

		var result = vkCreateWin32SurfaceKHR(instance.Handle.Handle, ref createInfo, IntPtr.Zero, out var surface);
		if (result != 0) // VK_SUCCESS = 0
			throw new VulkanException($"vkCreateWin32SurfaceKHR failed with result {result}");

		return surface;
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

	private delegate int PFN_vkCreateWin32SurfaceKHR(IntPtr instance, ref VkWin32SurfaceCreateInfoKHR pCreateInfo, IntPtr pAllocator, out ulong pSurface);
}
