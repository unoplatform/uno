---
uid: Uno.Skia.Vulkan
---

# Vulkan Rendering Backend

Uno Platform uses Vulkan as the hardware-accelerated rendering backend for the Skia renderer on **Android**, **Linux (X11)**, and **Windows (Win32)**.

Vulkan provides lower driver overhead and more efficient GPU utilization compared to OpenGL on supported hardware. The Skia drawing operations are backed by a Vulkan graphics pipeline instead of OpenGL.

> [!NOTE]
> Since Uno Platform 7.0, Vulkan is the **default** backend on those three platforms — it was opt-in before. Devices without a usable Vulkan driver fall back to OpenGL (or software rendering) automatically, so no configuration is required either way. To force the previous behavior, see [Disabling Vulkan](#disabling-vulkan).

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
    .UseX11(b => b.ForceRenderingBackend(X11RenderingBackend.Vulkan))
    .UseWin32(b => b.ForceRenderingBackend(Win32RenderingBackend.Vulkan))
    .UseLinuxFrameBuffer()
    .UseMacOS()
    .Build();

host.Run();
```

Two builder methods control which backends negotiation may use:

- `ForceRenderingBackend(backend)` — restrict negotiation to that single backend (every other is excluded; if it can't be created, none is tried).
- `DisableRenderingBackends(params backends)` — remove the listed backends, leaving every other available in the default preference order (Vulkan → OpenGL → OpenGL ES → software).

If neither is called, all backends are available and the first one that initializes wins.

Each platform has its own rendering backend enum reflecting the backends it supports:

**`X11RenderingBackend`** (Linux): `Vulkan` (GLX-independent), `OpenGL` (via GLX), `OpenGLES` (via EGL), `Software`.

**`Win32RenderingBackend`** (Windows): `Vulkan`, `OpenGL` (via WGL), `Software`.

For example, to prefer OpenGL ES over desktop OpenGL on X11, disable the desktop-GL backend and let negotiation fall through to it:

```csharp
.UseX11(b => b.DisableRenderingBackends(X11RenderingBackend.OpenGL))
```

### Using FeatureConfiguration Flags

For backwards compatibility, Android rendering can also be configured via `FeatureConfiguration.Rendering`:

```csharp
// Set before host.Build()
FeatureConfiguration.Rendering.UseVulkanOnSkiaAndroid = true;
```

> [!NOTE]
> When both the builder API and the feature flag are used, the builder takes precedence if it runs after the flag is set (which is the typical case). If you set the feature flag *after* `Build()`, the flag value wins.

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

## Disabling Vulkan

Vulkan is the default on Android, Linux (X11) and Windows (Win32) since Uno Platform 7.0. To go
back to the pre-7.0 behavior of rendering with OpenGL, select the OpenGL backend explicitly:

```csharp
var host = UnoPlatformHostBuilder.Create()
    .App(() => new App())
    .UseX11(b => b.RenderingBackend(X11RenderingBackend.OpenGL))
    .UseWin32(b => b.RenderingBackend(Win32RenderingBackend.OpenGL))
    .Build();
```

Or clear the feature flag before the host is built — the only option on Android, which has no
host-builder backend enum:

```csharp
FeatureConfiguration.Rendering.UseVulkanOnSkiaAndroid = false;
FeatureConfiguration.Rendering.UseVulkanOnX11 = false;
FeatureConfiguration.Rendering.UseVulkanOnWin32 = false;
```

## Fallback Behavior

When Vulkan is unavailable or has been disabled, the application automatically falls back to the next available backend:

1. **Vulkan** (the default on Android, Linux (X11) and Windows (Win32))
2. **OpenGL / OpenGL ES**
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
// Desktop: exclude Vulkan, keep the other backends
var host = UnoPlatformHostBuilder.Create()
    .App(() => new App())
    .UseX11(b => b.DisableRenderingBackends(X11RenderingBackend.Vulkan))
    .UseWin32(b => b.DisableRenderingBackends(Win32RenderingBackend.Vulkan))
    .Build();

// Android
FeatureConfiguration.Rendering.UseVulkanOnSkiaAndroid = false;
```
