// Based on the Avalonia project (MIT License, Copyright (c) AvaloniaUI OÜ).
// Original source: https://github.com/AvaloniaUI/Avalonia/tree/master/src/Avalonia.Vulkan
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Uno.UI.Runtime.Skia.Vulkan;


static class VulkanHelpers
{
    public static uint MakeVersion(Version v) => MakeVersion(v.Major, v.Minor, v.Build);

    public static uint MakeVersion(int major, int minor, int patch)
    {
        return (uint)((major << 22) | (minor << 12) | patch);
    }

    public const uint QueueFamilyIgnored = 4294967295;
}
