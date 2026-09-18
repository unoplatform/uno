// Based on the Avalonia project (MIT License, Copyright (c) AvaloniaUI OÜ).
// Original source: https://github.com/AvaloniaUI/Avalonia/tree/master/src/Avalonia.Vulkan
using System;
using Uno.UI.Runtime.Skia.Vulkan.UnmanagedInterop;

namespace Uno.UI.Runtime.Skia.Vulkan;

public class VulkanException : Exception
{
    public VulkanException(string message) : base(message)
    {

    }

    public VulkanException(string funcName, int res) : this(funcName, (VkResult)res)
    {

    }

    internal VulkanException(string funcName, VkResult res) : base($"{funcName} returned {res}")
    {

    }

    public static void ThrowOnError(string funcName, int res) => ((VkResult)res).ThrowOnError(funcName);
}

internal static class VulkanExceptionExtensions
{
    public static void ThrowOnError(this VkResult res, string funcName)
    {
        if (res != VkResult.VK_SUCCESS)
            throw new VulkanException(funcName, res);
    }
}
