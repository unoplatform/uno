---
uid: Uno.Skia.Vulkan
---

# Vulkan Rendering Backend

Uno Platform supports Vulkan as an optional hardware-accelerated rendering backend for the Skia renderer on **Android**, **Linux (X11)**, and **Windows (Win32)**.

Vulkan provides lower driver overhead and more efficient GPU utilization compared to OpenGL on supported hardware. When enabled, the Skia drawing operations are backed by a Vulkan graphics pipeline instead of OpenGL.

> [!NOTE]
> Vulkan rendering is **opt-in**. The default rendering backend remains OpenGL (or software) on all platforms. Enabling Vulkan when it is not available on the target device will automatically fall back to the default backend.

## Platform Support

| Platform | Vulkan Support | Minimum Requirement |
|----------|---------------|---------------------|
| Android  | Yes | Android 7.0+ (API 24) with Vulkan-capable GPU |
| Linux (X11) | Yes | Vulkan ICD (Mesa or proprietary drivers) |
| Windows (Win32) | Yes | Vulkan runtime + compatible GPU driver |
| macOS | No | Uses Metal instead |
| WebAssembly | No | Uses WebGL instead |

## Enabling Vulkan

### Using the Host Builder (Recommended — Desktop)

On desktop platforms, the preferred way to enable Vulkan is through the platform host builder:

```csharp
var host = UnoPlatformHostBuilder.Create()
    .App(() => new App())
    .UseX11(b => b.RenderingBackend(X11RenderingBackend.Vulkan))
    .UseWin32(b => b.RenderingBackend(Win32RenderingBackend.Vulkan))
    .UseLinuxFrameBuffer()
    .UseMacOS()
    .Build();

host.Run();
```

Each platform has its own rendering backend enum reflecting the backends it supports:

**`X11RenderingBackend`** (Linux):

| Value | Description |
|-------|-------------|
| `Default` | Auto-detect: try OpenGL, fall back to software |
| `Vulkan` | Vulkan with fallback to OpenGL/software |
| `OpenGL` | OpenGL via GLX |
| `OpenGLES` | OpenGL ES via EGL |
| `Software` | CPU-based software rendering |

**`Win32RenderingBackend`** (Windows):

| Value | Description |
|-------|-------------|
| `Default` | Auto-detect: try OpenGL, fall back to software |
| `Vulkan` | Vulkan with fallback to OpenGL/software |
| `OpenGL` | OpenGL via WGL |
| `Software` | CPU-based software rendering |

### Using FeatureConfiguration Flags

For backwards compatibility, rendering can also be configured via `FeatureConfiguration.Rendering`:

```csharp
// Android — set before host.Build()
FeatureConfiguration.Rendering.UseVulkanOnSkiaAndroid = true;

// Linux/X11 — set before host.Build()
FeatureConfiguration.Rendering.UseVulkanOnX11 = true;

// Windows/Win32 — set before host.Build()
FeatureConfiguration.Rendering.UseVulkanOnWin32 = true;
```

> [!NOTE]
> When both the builder API and the feature flags are used, the builder takes precedence if it runs after the flag is set (which is the typical case). If you set a feature flag *after* `Build()`, the flag value wins.

### Android

Android uses the same host builder as the other targets, from `CreateHost()` in your `Application` class (see [Customizing the Android `Application` class](xref:Uno.Features.CustomizingAndroidApplication)):

```csharp
protected override UnoPlatformHost CreateHost() =>
    UnoPlatformHostBuilder.Create()
        .App(() => new App())
        .UseAndroid(b => b.UseVulkan())
        .Build();
```

Android exposes two independent options rather than a single backend enum, because the Vulkan path can fail at runtime and fall back to the canvas render view:

| Option | Description |
|--------|-------------|
| `UseVulkan(bool)` | Use the Vulkan render view when the device reports Vulkan support. Default `true`. |
| `UseOpenGL(bool)` | Accelerate the canvas render view — used whenever Vulkan is disabled or unavailable — with OpenGL ES. Default `true`. |

To force software rendering on both paths, disable each one:

```csharp
.UseAndroid(b => b.UseVulkan(false).UseOpenGL(false))
```

## Fallback Behavior

When Vulkan is requested but unavailable, the application automatically falls back to the next available backend:

1. **Vulkan** (if requested)
2. **OpenGL / OpenGL ES** (platform default)
3. **Software rendering** (CPU-based)

No user intervention is required. A diagnostic log message is emitted indicating which backend was selected and why.

## Diagnostic Logging

Enable debug logging to see which rendering backend was selected:

```csharp
builder.AddFilter("Uno.UI.Runtime.Skia", LogLevel.Information);
```

When Vulkan is successfully initialized:

```text
Vulkan rendering initialized: <device name>, <driver version>
```

When Vulkan falls back:

```text
Vulkan rendering not available: <reason>. Falling back to OpenGL ES.
```

## Troubleshooting

### Vulkan not available on Linux

Ensure Vulkan drivers are installed:

```bash
# Debian/Ubuntu (Mesa)
sudo apt install mesa-vulkan-drivers

# Verify
vulkaninfo
```

### Vulkan not available on Android

- Vulkan requires Android 7.0 (API 24) or higher
- Some low-end or older devices may have Vulkan listed but with incomplete driver support — in these cases the fallback to OpenGL ES is automatic

### Vulkan not available on Windows

Ensure your GPU driver includes Vulkan support. Most modern NVIDIA, AMD, and Intel drivers include Vulkan. You can verify with the [Vulkan SDK](https://vulkan.lunarg.com/) `vulkaninfo` tool.

### Application crashes with Vulkan enabled

If you experience crashes with Vulkan enabled, disable it and file an issue:

```csharp
// Temporarily disable Vulkan
FeatureConfiguration.Rendering.UseVulkanOnSkiaAndroid = false;
FeatureConfiguration.Rendering.UseVulkanOnX11 = false;
FeatureConfiguration.Rendering.UseVulkanOnWin32 = false;
```
