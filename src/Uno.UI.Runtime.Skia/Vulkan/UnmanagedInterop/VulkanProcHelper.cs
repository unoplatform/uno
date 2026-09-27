#nullable enable
using System;
using System.Runtime.InteropServices;

namespace Uno.UI.Runtime.Skia.Vulkan.UnmanagedInterop;

/// <summary>
/// Helper for loading Vulkan function pointers as managed delegates.
/// Replaces Avalonia's [GetProcAddress] source generator.
/// </summary>
internal static class VulkanProcHelper
{
	public static T? LoadFunc<T>(Func<string, IntPtr> getProcAddress, string name, bool optional = false)
		where T : Delegate
	{
		var ptr = getProcAddress(name);
		if (ptr == IntPtr.Zero)
		{
			if (!optional)
				throw new VulkanException($"Failed to load Vulkan function: {name}");
			return null;
		}
		return Marshal.GetDelegateForFunctionPointer<T>(ptr);
	}
}
