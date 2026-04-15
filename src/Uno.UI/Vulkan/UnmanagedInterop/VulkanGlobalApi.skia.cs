// Based on the Avalonia project (MIT License, Copyright (c) AvaloniaUI OÜ).
// Original source: https://github.com/AvaloniaUI/Avalonia/tree/master/src/Avalonia.Vulkan
using System;
using System.Runtime.InteropServices;
using static Uno.UI.Runtime.Skia.Vulkan.UnmanagedInterop.VulkanProcHelper;

namespace Uno.UI.Runtime.Skia.Vulkan.UnmanagedInterop;

/// <summary>
/// Delegate type for vkGetInstanceProcAddr.
/// </summary>
internal delegate IntPtr VkGetInstanceProcAddressDelegate(IntPtr instance, string name);

internal unsafe class VulkanGlobalApi
{
	private readonly VkGetInstanceProcAddressDelegate _vkGetProcAddress;

	// Delegate types
	private delegate VkResult PFN_vkEnumerateInstanceLayerProperties(ref uint pPropertyCount, VkLayerProperties* pProperties);
	private delegate VkResult PFN_vkCreateInstance(ref VkInstanceCreateInfo pCreateInfo, IntPtr pAllocator, out VkInstance pInstance);
	private delegate VkResult PFN_vkEnumerateInstanceExtensionProperties(IntPtr pLayerName, uint* pPropertyCount, VkExtensionProperties* pProperties);

	// Loaded delegates
	private readonly PFN_vkEnumerateInstanceLayerProperties _enumerateInstanceLayerProperties;
	private readonly PFN_vkCreateInstance _createInstance;
	private readonly PFN_vkEnumerateInstanceExtensionProperties _enumerateInstanceExtensionProperties;

	public VulkanGlobalApi(VkGetInstanceProcAddressDelegate vkGetProcAddress)
	{
		_vkGetProcAddress = vkGetProcAddress;
		IntPtr GetAddr(string name) => vkGetProcAddress(IntPtr.Zero, name);

		_enumerateInstanceLayerProperties = LoadFunc<PFN_vkEnumerateInstanceLayerProperties>(GetAddr, "vkEnumerateInstanceLayerProperties")!;
		_createInstance = LoadFunc<PFN_vkCreateInstance>(GetAddr, "vkCreateInstance")!;
		_enumerateInstanceExtensionProperties = LoadFunc<PFN_vkEnumerateInstanceExtensionProperties>(GetAddr, "vkEnumerateInstanceExtensionProperties")!;
	}

	public IntPtr GetProcAddress(VkInstance instance, string name) => _vkGetProcAddress(instance.Handle, name);

	public VkResult EnumerateInstanceLayerProperties(ref uint pPropertyCount, VkLayerProperties* pProperties)
		=> _enumerateInstanceLayerProperties(ref pPropertyCount, pProperties);

	public VkResult CreateInstance(ref VkInstanceCreateInfo pCreateInfo, IntPtr pAllocator, out VkInstance pInstance)
		=> _createInstance(ref pCreateInfo, pAllocator, out pInstance);

	public VkResult EnumerateInstanceExtensionProperties(IntPtr pLayerName, uint* pPropertyCount, VkExtensionProperties* pProperties)
		=> _enumerateInstanceExtensionProperties(pLayerName, pPropertyCount, pProperties);
}
