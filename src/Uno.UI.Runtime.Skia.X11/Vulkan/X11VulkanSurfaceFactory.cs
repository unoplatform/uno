using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Uno.UI.Runtime.Skia.Vulkan;
using Uno.UI.Runtime.Skia.Vulkan.Interop;
using Uno.UI.Runtime.Skia.Vulkan.UnmanagedInterop;

namespace Uno.WinUI.Runtime.Skia.X11.Vulkan;

[StructLayout(LayoutKind.Sequential)]
internal struct VkXlibSurfaceCreateInfoKHR
{
	public const uint VK_STRUCTURE_TYPE_XLIB_SURFACE_CREATE_INFO_KHR = 1000004000;
	public uint sType;
	public IntPtr pNext;
	public uint flags;
	public IntPtr dpy;    // X11 Display*
	public IntPtr window; // X11 Window (XID)
}

internal class X11VulkanSurfaceFactory : IVulkanPlatformSurfaceFactory
{
	[DllImport("libvulkan.so.1", EntryPoint = "vkGetInstanceProcAddr")]
	private static extern IntPtr NativeGetInstanceProcAddr(IntPtr instance, [MarshalAs(UnmanagedType.LPStr)] string name);

	private static bool _vulkanAvailable;
	private static bool _vulkanChecked;

	private readonly IntPtr _display;

	public X11VulkanSurfaceFactory(IntPtr display)
	{
		_display = display;
	}

	public IReadOnlyList<string> RequiredInstanceExtensions { get; } = new[] { "VK_KHR_xlib_surface" };

	public VkGetInstanceProcAddressDelegate GetVkGetInstanceProcAddr()
	{
		EnsureVulkanAvailable();
		return NativeGetInstanceProcAddr;
	}

	public ulong CreateSurface(VulkanInstance instance, IntPtr nativeWindowHandle)
	{
		if (nativeWindowHandle == IntPtr.Zero)
			throw new ArgumentException("X11 Window handle cannot be zero", nameof(nativeWindowHandle));

		var createSurfacePtr = instance.GetInstanceProcAddress(instance.Handle.Handle, "vkCreateXlibSurfaceKHR");
		if (createSurfacePtr == IntPtr.Zero)
			throw new VulkanException("Failed to load vkCreateXlibSurfaceKHR");

		var vkCreateXlibSurfaceKHR = Marshal.GetDelegateForFunctionPointer<PFN_vkCreateXlibSurfaceKHR>(createSurfacePtr);

		var createInfo = new VkXlibSurfaceCreateInfoKHR
		{
			sType = VkXlibSurfaceCreateInfoKHR.VK_STRUCTURE_TYPE_XLIB_SURFACE_CREATE_INFO_KHR,
			dpy = _display,
			window = nativeWindowHandle
		};

		var result = vkCreateXlibSurfaceKHR(instance.Handle.Handle, ref createInfo, IntPtr.Zero, out var surface);
		if (result != 0)
			throw new VulkanException($"vkCreateXlibSurfaceKHR failed with result {result}");

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
			throw new VulkanException("libvulkan.so.1 not found");
	}

	private delegate int PFN_vkCreateXlibSurfaceKHR(IntPtr instance, ref VkXlibSurfaceCreateInfoKHR pCreateInfo, IntPtr pAllocator, out ulong pSurface);
}
