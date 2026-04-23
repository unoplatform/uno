// Based on the Avalonia project (MIT License, Copyright (c) AvaloniaUI OÜ).
// Original source: https://github.com/AvaloniaUI/Avalonia/tree/master/src/Avalonia.Vulkan
using System;
using SkiaSharp;
using Uno.UI.Runtime.Skia.Vulkan.UnmanagedInterop;

namespace Uno.UI.Runtime.Skia.Vulkan;

/// <summary>
/// Provides access to the Vulkan device, instance, and queue for rendering operations.
/// </summary>
internal interface IVulkanPlatformGraphicsContext
{
	IVulkanDevice Device { get; }
	IVulkanInstance Instance { get; }
	VulkanInstanceApi InstanceApi { get; }
	VulkanDeviceApi DeviceApi { get; }
	VkDevice DeviceHandle { get; }
	VkPhysicalDevice PhysicalDeviceHandle { get; }
	VkInstance InstanceHandle { get; }
	VkQueue MainQueueHandle { get; }
	uint GraphicsQueueFamilyIndex { get; }
}

/// <summary>
/// Platform-specific surface for Vulkan KHR surface presentation.
/// </summary>
internal interface IVulkanKhrSurfacePlatformSurface : IDisposable
{
	SKSizeI Size { get; }
	ulong CreateSurface(IVulkanPlatformGraphicsContext context);
}
