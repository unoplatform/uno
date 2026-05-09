using System;
using System.Collections.Generic;
using Uno.UI.Runtime.Skia.Vulkan.Interop;
using Uno.UI.Runtime.Skia.Vulkan.UnmanagedInterop;

namespace Uno.UI.Runtime.Skia.Vulkan;

/// <summary>
/// Platform-specific factory for creating Vulkan surfaces from native window handles.
/// Each platform (Android, X11, Win32) implements this interface.
/// </summary>
internal interface IVulkanPlatformSurfaceFactory
{
	/// <summary>
	/// Required Vulkan instance extensions for this platform (e.g., "VK_KHR_android_surface").
	/// </summary>
	IReadOnlyList<string> RequiredInstanceExtensions { get; }

	/// <summary>
	/// Load the platform's vkGetInstanceProcAddr function pointer.
	/// </summary>
	VkGetInstanceProcAddressDelegate GetVkGetInstanceProcAddr();

	/// <summary>
	/// Create a VkSurfaceKHR from the platform's native window handle.
	/// </summary>
	ulong CreateSurface(VulkanInstance instance, IntPtr nativeWindowHandle);
}
