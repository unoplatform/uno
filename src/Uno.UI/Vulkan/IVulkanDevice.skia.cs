// Based on the Avalonia project (MIT License, Copyright (c) AvaloniaUI OÜ).
// Original source: https://github.com/AvaloniaUI/Avalonia/tree/master/src/Avalonia.Vulkan
using System;
using System.Collections.Generic;
using Uno.UI.Runtime.Skia.Vulkan.UnmanagedInterop;

namespace Uno.UI.Runtime.Skia.Vulkan;

internal interface IVulkanInstance
{
	IntPtr Handle { get; }
	IntPtr GetInstanceProcAddress(IntPtr instance, string name);
	IntPtr GetDeviceProcAddress(IntPtr device, string name);
	IReadOnlyList<string> EnabledExtensions { get; }
}

internal interface IVulkanDevice
{
	IntPtr Handle { get; }
	IntPtr PhysicalDeviceHandle { get; }
	IntPtr MainQueueHandle { get; }
	uint GraphicsQueueFamilyIndex { get; }
	IVulkanInstance Instance { get; }
	IDisposable Lock();
}
