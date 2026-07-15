#nullable enable
// Based on the Avalonia project (MIT License, Copyright (c) AvaloniaUI OÜ).
// Original source: https://github.com/AvaloniaUI/Avalonia/tree/master/src/Avalonia.Vulkan
using System;
using System.Runtime.InteropServices;
using static Uno.UI.Runtime.Skia.Vulkan.UnmanagedInterop.VulkanProcHelper;
using uint32_t = System.UInt32;
using VkBool32 = System.UInt32;

namespace Uno.UI.Runtime.Skia.Vulkan.UnmanagedInterop;

internal unsafe class VulkanInstanceApi
{
	// Delegate types
	private delegate VkResult PFN_vkCreateDebugUtilsMessengerEXT(VkInstance instance, ref VkDebugUtilsMessengerCreateInfoEXT pCreateInfo, IntPtr pAllocator, out VkDebugUtilsMessengerEXT pMessenger);
	private delegate VkResult PFN_vkEnumeratePhysicalDevices(VkInstance instance, ref uint32_t pPhysicalDeviceCount, VkPhysicalDevice* pPhysicalDevices);
	private delegate void PFN_vkGetPhysicalDeviceProperties(VkPhysicalDevice physicalDevice, out VkPhysicalDeviceProperties pProperties);
	private delegate VkResult PFN_vkEnumerateDeviceExtensionProperties(VkPhysicalDevice physicalDevice, byte* pLayerName, ref uint32_t pPropertyCount, VkExtensionProperties* pProperties);
	private delegate VkResult PFN_vkGetPhysicalDeviceSurfaceSupportKHR(VkPhysicalDevice physicalDevice, uint32_t queueFamilyIndex, VkSurfaceKHR surface, out VkBool32 pSupported);
	private delegate void PFN_vkGetPhysicalDeviceQueueFamilyProperties(VkPhysicalDevice physicalDevice, ref uint32_t pQueueFamilyPropertyCount, VkQueueFamilyProperties* pQueueFamilyProperties);
	private delegate VkResult PFN_vkCreateDevice(VkPhysicalDevice physicalDevice, ref VkDeviceCreateInfo pCreateInfo, IntPtr pAllocator, out VkDevice pDevice);
	private delegate VkResult PFN_vkDestroyDevice(VkDevice device, IntPtr pAllocator);
	private delegate void PFN_vkGetDeviceQueue(VkDevice device, uint32_t queueFamilyIndex, uint32_t queueIndex, out VkQueue pQueue);
	private delegate IntPtr PFN_vkGetDeviceProcAddr(VkDevice device, IntPtr pName);
	private delegate void PFN_vkDestroySurfaceKHR(VkInstance instance, VkSurfaceKHR surface, IntPtr pAllocator);
	private delegate VkResult PFN_vkGetPhysicalDeviceSurfaceFormatsKHR(VkPhysicalDevice physicalDevice, VkSurfaceKHR surface, ref uint32_t pSurfaceFormatCount, VkSurfaceFormatKHR* pSurfaceFormats);
	private delegate void PFN_vkGetPhysicalDeviceMemoryProperties(VkPhysicalDevice physicalDevice, out VkPhysicalDeviceMemoryProperties pMemoryProperties);
	private delegate VkResult PFN_vkGetPhysicalDeviceSurfaceCapabilitiesKHR(VkPhysicalDevice physicalDevice, VkSurfaceKHR surface, out VkSurfaceCapabilitiesKHR pSurfaceCapabilities);
	private delegate VkResult PFN_vkGetPhysicalDeviceSurfacePresentModesKHR(VkPhysicalDevice physicalDevice, VkSurfaceKHR surface, ref uint32_t pPresentModeCount, VkPresentModeKHR* pPresentModes);
	private delegate void PFN_vkGetPhysicalDeviceProperties2(VkPhysicalDevice physicalDevice, VkPhysicalDeviceProperties2* pProperties);

	// Loaded delegates
	private readonly PFN_vkCreateDebugUtilsMessengerEXT? _createDebugUtilsMessengerEXT;
	private readonly PFN_vkEnumeratePhysicalDevices _enumeratePhysicalDevices;
	private readonly PFN_vkGetPhysicalDeviceProperties _getPhysicalDeviceProperties;
	private readonly PFN_vkEnumerateDeviceExtensionProperties _enumerateDeviceExtensionProperties;
	private readonly PFN_vkGetPhysicalDeviceSurfaceSupportKHR _getPhysicalDeviceSurfaceSupportKHR;
	private readonly PFN_vkGetPhysicalDeviceQueueFamilyProperties _getPhysicalDeviceQueueFamilyProperties;
	private readonly PFN_vkCreateDevice _createDevice;
	private readonly PFN_vkDestroyDevice _destroyDevice;
	private readonly PFN_vkGetDeviceQueue _getDeviceQueue;
	private readonly PFN_vkGetDeviceProcAddr _getDeviceProcAddr;
	private readonly PFN_vkDestroySurfaceKHR _destroySurfaceKHR;
	private readonly PFN_vkGetPhysicalDeviceSurfaceFormatsKHR _getPhysicalDeviceSurfaceFormatsKHR;
	private readonly PFN_vkGetPhysicalDeviceMemoryProperties _getPhysicalDeviceMemoryProperties;
	private readonly PFN_vkGetPhysicalDeviceSurfaceCapabilitiesKHR _getPhysicalDeviceSurfaceCapabilitiesKHR;
	private readonly PFN_vkGetPhysicalDeviceSurfacePresentModesKHR _getPhysicalDeviceSurfacePresentModesKHR;
	private readonly PFN_vkGetPhysicalDeviceProperties2? _getPhysicalDeviceProperties2;

	public IVulkanInstance Instance { get; }

	public VulkanInstanceApi(IVulkanInstance instance)
	{
		Instance = instance;
		IntPtr GetAddr(string name) => instance.GetInstanceProcAddress(instance.Handle, name);

		_createDebugUtilsMessengerEXT = LoadFunc<PFN_vkCreateDebugUtilsMessengerEXT>(GetAddr, "vkCreateDebugUtilsMessengerEXT", optional: true);
		_enumeratePhysicalDevices = LoadFunc<PFN_vkEnumeratePhysicalDevices>(GetAddr, "vkEnumeratePhysicalDevices")!;
		_getPhysicalDeviceProperties = LoadFunc<PFN_vkGetPhysicalDeviceProperties>(GetAddr, "vkGetPhysicalDeviceProperties")!;
		_enumerateDeviceExtensionProperties = LoadFunc<PFN_vkEnumerateDeviceExtensionProperties>(GetAddr, "vkEnumerateDeviceExtensionProperties")!;
		_getPhysicalDeviceSurfaceSupportKHR = LoadFunc<PFN_vkGetPhysicalDeviceSurfaceSupportKHR>(GetAddr, "vkGetPhysicalDeviceSurfaceSupportKHR")!;
		_getPhysicalDeviceQueueFamilyProperties = LoadFunc<PFN_vkGetPhysicalDeviceQueueFamilyProperties>(GetAddr, "vkGetPhysicalDeviceQueueFamilyProperties")!;
		_createDevice = LoadFunc<PFN_vkCreateDevice>(GetAddr, "vkCreateDevice")!;
		_destroyDevice = LoadFunc<PFN_vkDestroyDevice>(GetAddr, "vkDestroyDevice")!;
		_getDeviceQueue = LoadFunc<PFN_vkGetDeviceQueue>(GetAddr, "vkGetDeviceQueue")!;
		_getDeviceProcAddr = LoadFunc<PFN_vkGetDeviceProcAddr>(GetAddr, "vkGetDeviceProcAddr")!;
		_destroySurfaceKHR = LoadFunc<PFN_vkDestroySurfaceKHR>(GetAddr, "vkDestroySurfaceKHR")!;
		_getPhysicalDeviceSurfaceFormatsKHR = LoadFunc<PFN_vkGetPhysicalDeviceSurfaceFormatsKHR>(GetAddr, "vkGetPhysicalDeviceSurfaceFormatsKHR")!;
		_getPhysicalDeviceMemoryProperties = LoadFunc<PFN_vkGetPhysicalDeviceMemoryProperties>(GetAddr, "vkGetPhysicalDeviceMemoryProperties")!;
		_getPhysicalDeviceSurfaceCapabilitiesKHR = LoadFunc<PFN_vkGetPhysicalDeviceSurfaceCapabilitiesKHR>(GetAddr, "vkGetPhysicalDeviceSurfaceCapabilitiesKHR")!;
		_getPhysicalDeviceSurfacePresentModesKHR = LoadFunc<PFN_vkGetPhysicalDeviceSurfacePresentModesKHR>(GetAddr, "vkGetPhysicalDeviceSurfacePresentModesKHR")!;
		_getPhysicalDeviceProperties2 = LoadFunc<PFN_vkGetPhysicalDeviceProperties2>(GetAddr, "vkGetPhysicalDeviceProperties2", optional: true);
	}

	public VkResult CreateDebugUtilsMessengerEXT(VkInstance instance, ref VkDebugUtilsMessengerCreateInfoEXT pCreateInfo, IntPtr pAllocator, out VkDebugUtilsMessengerEXT pMessenger)
	{
		if (_createDebugUtilsMessengerEXT == null) { pMessenger = default; return VkResult.VK_ERROR_EXTENSION_NOT_PRESENT; }
		return _createDebugUtilsMessengerEXT(instance, ref pCreateInfo, pAllocator, out pMessenger);
	}

	public VkResult EnumeratePhysicalDevices(VkInstance instance, ref uint32_t pPhysicalDeviceCount, VkPhysicalDevice* pPhysicalDevices)
		=> _enumeratePhysicalDevices(instance, ref pPhysicalDeviceCount, pPhysicalDevices);

	public void GetPhysicalDeviceProperties(VkPhysicalDevice physicalDevice, out VkPhysicalDeviceProperties pProperties)
		=> _getPhysicalDeviceProperties(physicalDevice, out pProperties);

	public VkResult EnumerateDeviceExtensionProperties(VkPhysicalDevice physicalDevice, byte* pLayerName, ref uint32_t pPropertyCount, VkExtensionProperties* pProperties)
		=> _enumerateDeviceExtensionProperties(physicalDevice, pLayerName, ref pPropertyCount, pProperties);

	public VkResult GetPhysicalDeviceSurfaceSupportKHR(VkPhysicalDevice physicalDevice, uint32_t queueFamilyIndex, VkSurfaceKHR surface, out VkBool32 pSupported)
		=> _getPhysicalDeviceSurfaceSupportKHR(physicalDevice, queueFamilyIndex, surface, out pSupported);

	public void GetPhysicalDeviceQueueFamilyProperties(VkPhysicalDevice physicalDevice, ref uint32_t pQueueFamilyPropertyCount, VkQueueFamilyProperties* pQueueFamilyProperties)
		=> _getPhysicalDeviceQueueFamilyProperties(physicalDevice, ref pQueueFamilyPropertyCount, pQueueFamilyProperties);

	public VkResult CreateDevice(VkPhysicalDevice physicalDevice, ref VkDeviceCreateInfo pCreateInfo, IntPtr pAllocator, out VkDevice pDevice)
		=> _createDevice(physicalDevice, ref pCreateInfo, pAllocator, out pDevice);

	public VkResult DestroyDevice(VkDevice device, IntPtr pAllocator)
		=> _destroyDevice(device, pAllocator);

	public void GetDeviceQueue(VkDevice device, uint32_t queueFamilyIndex, uint32_t queueIndex, out VkQueue pQueue)
		=> _getDeviceQueue(device, queueFamilyIndex, queueIndex, out pQueue);

	public IntPtr GetDeviceProcAddr(VkDevice device, IntPtr pName)
		=> _getDeviceProcAddr(device, pName);

	public void DestroySurfaceKHR(VkInstance instance, VkSurfaceKHR surface, IntPtr pAllocator)
		=> _destroySurfaceKHR(instance, surface, pAllocator);

	public VkResult GetPhysicalDeviceSurfaceFormatsKHR(VkPhysicalDevice physicalDevice, VkSurfaceKHR surface, ref uint32_t pSurfaceFormatCount, VkSurfaceFormatKHR* pSurfaceFormats)
		=> _getPhysicalDeviceSurfaceFormatsKHR(physicalDevice, surface, ref pSurfaceFormatCount, pSurfaceFormats);

	public void GetPhysicalDeviceMemoryProperties(VkPhysicalDevice physicalDevice, out VkPhysicalDeviceMemoryProperties pMemoryProperties)
		=> _getPhysicalDeviceMemoryProperties(physicalDevice, out pMemoryProperties);

	public VkResult GetPhysicalDeviceSurfaceCapabilitiesKHR(VkPhysicalDevice physicalDevice, VkSurfaceKHR surface, out VkSurfaceCapabilitiesKHR pSurfaceCapabilities)
		=> _getPhysicalDeviceSurfaceCapabilitiesKHR(physicalDevice, surface, out pSurfaceCapabilities);

	public VkResult GetPhysicalDeviceSurfacePresentModesKHR(VkPhysicalDevice physicalDevice, VkSurfaceKHR surface, ref uint32_t pPresentModeCount, VkPresentModeKHR* pPresentModes)
		=> _getPhysicalDeviceSurfacePresentModesKHR(physicalDevice, surface, ref pPresentModeCount, pPresentModes);

	public void GetPhysicalDeviceProperties2(VkPhysicalDevice physicalDevice, VkPhysicalDeviceProperties2* pProperties)
		=> _getPhysicalDeviceProperties2?.Invoke(physicalDevice, pProperties);
}
