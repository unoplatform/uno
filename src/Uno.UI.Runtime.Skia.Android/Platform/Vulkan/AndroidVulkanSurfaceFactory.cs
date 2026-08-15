using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Uno.UI.Runtime.Skia.Vulkan;
using Uno.UI.Runtime.Skia.Vulkan.Interop;
using Uno.UI.Runtime.Skia.Vulkan.UnmanagedInterop;

namespace Uno.WinUI.Runtime.Skia.Android.Platform.Vulkan;

/// <summary>
/// Android-specific Vulkan surface factory. Loads libvulkan.so and creates
/// Android surfaces via vkCreateAndroidSurfaceKHR.
/// </summary>
internal class AndroidVulkanSurfaceFactory : IVulkanPlatformSurfaceFactory
{
	[DllImport("libvulkan.so", EntryPoint = "vkGetInstanceProcAddr")]
	private static extern IntPtr NativeGetInstanceProcAddr(IntPtr instance, [MarshalAs(UnmanagedType.LPStr)] string name);

	private static bool _vulkanAvailable;
	private static bool _vulkanChecked;

	public IReadOnlyList<string> RequiredInstanceExtensions { get; } = new[] { "VK_KHR_android_surface" };

	public VkGetInstanceProcAddressDelegate GetVkGetInstanceProcAddr()
	{
		EnsureVulkanAvailable();
		return NativeGetInstanceProcAddr;
	}

	public ulong CreateSurface(VulkanInstance instance, IntPtr nativeWindowHandle)
	{
		if (nativeWindowHandle == IntPtr.Zero)
			throw new ArgumentException("Native window handle cannot be zero", nameof(nativeWindowHandle));

		var vulkanAndroid = new AndroidVulkanNativeInterop(instance);
		var createInfo = new VkAndroidSurfaceCreateInfoKHR
		{
			sType = VkAndroidSurfaceCreateInfoKHR.VK_STRUCTURE_TYPE_ANDROID_SURFACE_CREATE_INFO_KHR,
			window = nativeWindowHandle
		};

		var result = vulkanAndroid.CreateAndroidSurfaceKHR(instance.Handle.Handle, ref createInfo, IntPtr.Zero, out var surface);
		if (result != 0) // VK_SUCCESS = 0
			throw new VulkanException($"vkCreateAndroidSurfaceKHR failed with result {result}");

		return surface;
	}

	public static bool IsVulkanAvailable()
	{
		if (!_vulkanChecked)
		{
			_vulkanChecked = true;
			try
			{
				// Try to load libvulkan.so and resolve vkGetInstanceProcAddr
				var addr = NativeGetInstanceProcAddr(IntPtr.Zero, "vkEnumerateInstanceVersion");
				_vulkanAvailable = true;
			}
			catch (DllNotFoundException)
			{
				_vulkanAvailable = false;
			}
			catch (EntryPointNotFoundException)
			{
				// libvulkan.so exists but is incomplete — still try
				_vulkanAvailable = true;
			}
		}
		return _vulkanAvailable;
	}

	private static void EnsureVulkanAvailable()
	{
		if (!IsVulkanAvailable())
			throw new VulkanException("libvulkan.so not found");
	}
}
