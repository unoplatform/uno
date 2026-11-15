#nullable enable
// Based on the Avalonia project (MIT License, Copyright (c) AvaloniaUI OÜ).
// Original source: https://github.com/AvaloniaUI/Avalonia/tree/master/src/Avalonia.Vulkan
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Uno.UI.Runtime.Vulkan;
using Uno.UI.Runtime.Vulkan.UnmanagedInterop;

namespace Uno.UI.Runtime.Vulkan.Interop;

internal unsafe partial class VulkanDevice
{
	/// <summary>
	/// Creates a VulkanDevice. Simplified from Avalonia's version:
	/// - Prefers discrete GPUs, unless <paramref name="preferDiscrete"/> keeps the loader's order, with software devices last
	/// - Always requires VK_KHR_swapchain
	/// - Does not require compute bit by default
	/// </summary>
	public static VulkanDevice Create(VulkanInstance instance, VulkanInstanceApi instanceApi,
		VkSurfaceKHR? checkSurface = null, Func<VkPhysicalDevice, uint, bool>? supportsPresentation = null, bool preferDiscrete = true)
	{
		uint deviceCount = 0;
		var vkInstance = instance.Handle;
		instanceApi.EnumeratePhysicalDevices(vkInstance, ref deviceCount, null)
			.ThrowOnError("vkEnumeratePhysicalDevices");

		if (deviceCount == 0)
			throw new VulkanException("No devices found");

		var devices = stackalloc VkPhysicalDevice[(int)deviceCount];
		instanceApi.EnumeratePhysicalDevices(vkInstance, ref deviceCount, devices)
			.ThrowOnError("vkEnumeratePhysicalDevices");

		var deviceTypes = new VkPhysicalDeviceType[deviceCount];
		for (var c = 0; c < deviceCount; c++)
		{
			instanceApi.GetPhysicalDeviceProperties(devices[c], out var properties);
			deviceTypes[c] = properties.deviceType;
		}

		DeviceInfo? compatibleDevice = null;
		foreach (var index in GetProbeOrder(deviceTypes, preferDiscrete))
		{
			compatibleDevice = CheckDevice(instanceApi, devices[index], checkSurface, supportsPresentation);
			if (compatibleDevice != null)
				break;
		}

		if (compatibleDevice == null)
			throw new VulkanException("No compatible devices found");

		var dev = compatibleDevice.Value;

		var queuePriorities = stackalloc float[(int)dev.QueueCount];
		for (var c = 0; c < dev.QueueCount; c++)
			queuePriorities[c] = 1f;

		var queueCreateInfo = new VkDeviceQueueCreateInfo
		{
			sType = VkStructureType.VK_STRUCTURE_TYPE_DEVICE_QUEUE_CREATE_INFO,
			queueFamilyIndex = dev.QueueFamilyIndex,
			queueCount = dev.QueueCount,
			pQueuePriorities = queuePriorities,
		};

		var enabledExtensions = new[] { VK_KHR_swapchain }
			.Concat(dev.Extensions.Where(e => e != VK_KHR_swapchain))
			.Distinct()
			.ToArray();

		// Only keep the swapchain extension plus any that are in the supported set
		enabledExtensions = new[] { VK_KHR_swapchain };

		using var pEnabledExtensions = new Utf8BufferArray(enabledExtensions);

		var createInfo = new VkDeviceCreateInfo
		{
			sType = VkStructureType.VK_STRUCTURE_TYPE_DEVICE_CREATE_INFO,
			queueCreateInfoCount = 1,
			pQueueCreateInfos = &queueCreateInfo,
			ppEnabledExtensionNames = pEnabledExtensions,
			enabledExtensionCount = pEnabledExtensions.UCount,
		};

		instanceApi.CreateDevice(dev.PhysicalDevice, ref createInfo, IntPtr.Zero, out var createdDevice)
			.ThrowOnError("vkCreateDevice");

		instanceApi.GetDeviceQueue(createdDevice, dev.QueueFamilyIndex, 0, out var createdQueue);

		return new VulkanDevice(instanceApi, createdDevice, dev.PhysicalDevice, createdQueue,
			dev.QueueFamilyIndex, enabledExtensions);
	}

	// OrderBy is stable, so devices of the same rank keep the loader's order.
	internal static int[] GetProbeOrder(VkPhysicalDeviceType[] types, bool preferDiscrete)
		=> Enumerable.Range(0, types.Length)
			.OrderBy(i => preferDiscrete
				? types[i] != VkPhysicalDeviceType.VK_PHYSICAL_DEVICE_TYPE_DISCRETE_GPU
				: types[i] == VkPhysicalDeviceType.VK_PHYSICAL_DEVICE_TYPE_CPU)
			.ToArray();

	struct DeviceInfo
	{
		public VkPhysicalDevice PhysicalDevice;
		public uint QueueFamilyIndex;
		public List<string> Extensions;
		public uint QueueCount;
	}

	static List<string> GetDeviceExtensions(VulkanInstanceApi instance, VkPhysicalDevice physicalDevice)
	{
		uint propertyCount = 0;
		instance.EnumerateDeviceExtensionProperties(physicalDevice, null, ref propertyCount, null);
		var extensionProps = new VkExtensionProperties[propertyCount];
		var extensions = new List<string>((int)propertyCount);
		if (propertyCount != 0)
			fixed (VkExtensionProperties* ptr = extensionProps)
			{
				instance.EnumerateDeviceExtensionProperties(physicalDevice, null, ref propertyCount, ptr);

				for (var c = 0; c < propertyCount; c++)
					extensions.Add(Marshal.PtrToStringAnsi(new IntPtr(ptr[c].extensionName))!);
			}

		return extensions;
	}

	private const string VK_KHR_swapchain = "VK_KHR_swapchain";

	internal static long? GetDeviceLuid(VulkanInstanceApi instance, VkPhysicalDevice physicalDevice)
	{
		var idProperties = new VkPhysicalDeviceIDProperties
		{
			sType = VkStructureType.VK_STRUCTURE_TYPE_PHYSICAL_DEVICE_ID_PROPERTIES,
		};
		var properties = new VkPhysicalDeviceProperties2
		{
			sType = VkStructureType.VK_STRUCTURE_TYPE_PHYSICAL_DEVICE_PROPERTIES_2,
			pNext = &idProperties,
		};
		instance.GetPhysicalDeviceProperties2(physicalDevice, &properties);

		// Same memory layout as a Win32 LUID: LowPart, then HighPart.
		return idProperties.deviceLUIDValid != 0 ? *(long*)idProperties.deviceLUID : null;
	}

	static DeviceInfo? CheckDevice(VulkanInstanceApi instance, VkPhysicalDevice physicalDevice,
		VkSurfaceKHR? surface, Func<VkPhysicalDevice, uint, bool>? supportsPresentation)
	{
		var supportedExtensions = GetDeviceExtensions(instance, physicalDevice);
		if (!supportedExtensions.Contains(VK_KHR_swapchain))
			return null;

		uint familyCount = 0;
		instance.GetPhysicalDeviceQueueFamilyProperties(physicalDevice, ref familyCount, null);
		var familyProperties = stackalloc VkQueueFamilyProperties[(int)familyCount];
		instance.GetPhysicalDeviceQueueFamilyProperties(physicalDevice, ref familyCount, familyProperties);
		var requiredFlags = VkQueueFlags.VK_QUEUE_GRAPHICS_BIT;

		for (var c = 0; c < familyCount; c++)
		{
			if ((familyProperties[c].queueFlags & requiredFlags) != requiredFlags)
				continue;
			if (surface.HasValue)
			{
				instance.GetPhysicalDeviceSurfaceSupportKHR(physicalDevice, (uint)c, surface.Value, out var supported)
					.ThrowOnError("vkGetPhysicalDeviceSurfaceSupportKHR");
				if (supported == 0)
					continue;
			}
			if (supportsPresentation is not null && !supportsPresentation(physicalDevice, (uint)c))
			{
				continue;
			}

			return new DeviceInfo
			{
				PhysicalDevice = physicalDevice,
				Extensions = supportedExtensions,
				QueueFamilyIndex = (uint)c,
				QueueCount = familyProperties[c].queueCount
			};
		}

		return null;
	}
}
