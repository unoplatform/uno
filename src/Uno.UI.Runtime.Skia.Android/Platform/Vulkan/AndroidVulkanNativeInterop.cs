using System;
using System.Runtime.InteropServices;
using Uno.UI.Runtime.Skia.Vulkan;
using Uno.UI.Runtime.Skia.Vulkan.Interop;
using static Uno.UI.Runtime.Skia.Vulkan.UnmanagedInterop.VulkanProcHelper;

namespace Uno.WinUI.Runtime.Skia.Android.Platform.Vulkan;

[StructLayout(LayoutKind.Sequential)]
internal struct VkAndroidSurfaceCreateInfoKHR
{
	public const uint VK_STRUCTURE_TYPE_ANDROID_SURFACE_CREATE_INFO_KHR = 1000008000;
	public uint sType;
	public IntPtr pNext;
	public uint flags;
	public IntPtr window;
}

internal class AndroidVulkanNativeInterop
{
	private delegate int PFN_vkCreateAndroidSurfaceKHR(IntPtr instance, ref VkAndroidSurfaceCreateInfoKHR pCreateInfo, IntPtr pAllocator, out ulong pSurface);

	private readonly PFN_vkCreateAndroidSurfaceKHR _createAndroidSurfaceKHR;

	public AndroidVulkanNativeInterop(IVulkanInstance instance)
	{
		IntPtr GetAddr(string name) => instance.GetInstanceProcAddress(instance.Handle, name);
		_createAndroidSurfaceKHR = LoadFunc<PFN_vkCreateAndroidSurfaceKHR>(GetAddr, "vkCreateAndroidSurfaceKHR")!;
	}

	public int CreateAndroidSurfaceKHR(IntPtr instance, ref VkAndroidSurfaceCreateInfoKHR pCreateInfo, IntPtr pAllocator, out ulong pSurface)
		=> _createAndroidSurfaceKHR(instance, ref pCreateInfo, pAllocator, out pSurface);
}
