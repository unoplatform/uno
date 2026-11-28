---
uid: Uno.Skia.Desktop
---

# Using the Skia Desktop

Uno Platform supports running applications using a common Skia Desktop shell, which is automatically used based on the running platform, using a single build output using the `net10.0-desktop` target framework from the [Uno.Sdk](xref:Uno.Features.Uno.Sdk).

The currently supported targets and platforms are:

- Linux X11
- [Linux Framebuffer](xref:Uno.Skia.Linux.Framebuffer)
- Windows (Using Win32 shell)
- [macOS (Using an AppKit shell)](xref:Uno.Skia.macOS)

The set of supported platforms can be defined by using the `UnoPlatformHostBuilder`, introduced in Uno Platform 5.2 and the [Single Project support](xref:Uno.Development.MigratingToSingleProject).

## Get started with the Skia Desktop head

Follow the getting started guide for [VS Code](xref:Uno.GetStarted.vscode) or [Visual Studio](xref:Uno.GetStarted.vs2022), making sure to use the latest Uno Platform templates.

Once created, in the `Platforms/Desktop/Program.cs` file, you'll find the following builder:

```csharp
var host = UnoPlatformHostBuilder.Create()
   .App(() => new App())
   .UseX11()
   .UseLinuxFrameBuffer()
   .UseMacOS()
   .UseWin32()
   .Build();

host.Run();
```

This builder allows us to configure the host and set up which platforms will be supported at runtime. The builder evaluates the platform's availability one by one, in the order of definition.

### Additional setup

#### [**Linux**](#tab/linux)

[!include[linux-setup](../includes/additional-linux-setup-inline.md)]

---

### Troubleshooting OpenGL integration

Enabling debug logging messages for the Skia Host can help diagnose the render surface type selection.

In your `App.xaml.cs` file, change the minimum log level to:

```csharp
builder.SetMinimumLevel(LogLevel.Debug);
```

Then change the logging level of the Skia Host to `Information` or `Debug`:

```csharp
builder.AddFilter("Uno.UI.Runtime.", LogLevel.Information);
```

You may also need to initialize the logging system earlier than what is found in Uno.UI's default templates by calling this in `Main`:

```csharp
YourAppNamespace.App.ConfigureFilters(); // Enable tracing of the Skia host
```

## Upgrading to a later version of SkiaSharp

By default, the [Uno.Sdk](xref:Uno.Features.Uno.Sdk) brings a tested set of **SkiaSharp** and **HarfBuzzSharp** packages. To move to a different version, change the native and the managed packages together, to the same version:

- The `SkiaSharpVersion` and `HarfBuzzSharpVersion` properties retarget the native-asset packages the Uno.Sdk references for each platform.
- The managed `SkiaSharp`, `SkiaSharp.Skottie` and `HarfBuzzSharp` packages come through the Uno Platform packages, so reference them directly at the same version.

In your project file:

```xml
<PropertyGroup>
   <SkiaSharpVersion>4.156.1</SkiaSharpVersion>
   <HarfBuzzSharpVersion>14.4.0.101</HarfBuzzSharpVersion>
</PropertyGroup>
<ItemGroup>
   <PackageReference Include="SkiaSharp" />
   <PackageReference Include="SkiaSharp.Skottie" />
   <PackageReference Include="HarfBuzzSharp" />
</ItemGroup>
```

And in `Directory.Packages.props`:

```xml
<ItemGroup>
   <PackageVersion Include="SkiaSharp" Version="4.156.1" />
   <PackageVersion Include="SkiaSharp.Skottie" Version="4.156.1" />
   <PackageVersion Include="HarfBuzzSharp" Version="14.4.0.101" />
</ItemGroup>
```

If your project doesn't use Central Package Management, set the `Version` on the `PackageReference` items instead.

> [!IMPORTANT]
> SkiaSharp checks its native library when the app starts. If the managed `SkiaSharp` package and the native `libSkiaSharp` library have different versions, the app fails at startup with "The version of the native libSkiaSharp library (…) is incompatible with this version of SkiaSharp". Setting only `SkiaSharpVersion` moves the native packages but not the managed one, so always reference the managed packages too. For the versions the Uno.Sdk manages, see [Using the Uno.Sdk](xref:Uno.Features.Uno.Sdk).

### SkiaSharp 4

SkiaSharp 4.0 reached general availability as `4.148.0`, and Uno Platform is a co-maintainer of SkiaSharp. **Since Uno Platform 7.0, SkiaSharp 4 is the default** — the Uno.Sdk resolves the SkiaSharp package group at a `4.x` version, and the steps above move it to another version. SkiaSharp 4 turns several long-deprecated APIs into compile errors (for example, text APIs that moved from `SKPaint` to `SKFont`), so a project carrying its own SkiaSharp code from `3.x` may need small changes before it builds.

## Rendering Backend Selection

By default, Uno Platform tries the GPU backends in a fixed order (Vulkan, then OpenGL, then software) and uses the first available one. You can force a single backend, or disable specific ones, per platform using the host builder:

```csharp
var host = UnoPlatformHostBuilder.Create()
    .App(() => new App())
    .UseX11(b => b.ForceRenderingBackend(X11RenderingBackend.Vulkan))   // only Vulkan
    .UseWin32(b => b.DisableRenderingBackends(Win32RenderingBackend.Vulkan)) // everything except Vulkan
    .UseMacOS()
    .Build();
```

Each platform exposes its own `RenderingBackend` enum with only the backends it supports. `ForceRenderingBackend` restricts negotiation to that one backend; `DisableRenderingBackends` removes the listed backends and leaves the rest. For details, see [Vulkan Rendering Backend](xref:Uno.Skia.Vulkan).

Alternatively, you can use `FeatureConfiguration.Rendering` flags for backwards compatibility. The builder API takes precedence when both are used.

## Display Scale Override

On X11 and Windows, setting the `UNO_DISPLAY_SCALE_OVERRIDE` environment variable before the app starts fixes the display scale Uno Platform lays out and draws at, instead of the scale of the monitor, for example `1` for 100% or `1.5` for 150%. The value is read with a `.` as the decimal separator. The [Linux framebuffer host](xref:Uno.Skia.Linux.Framebuffer) reads the same variable.

- On X11, the value is used as given.
- On Windows, a value that isn't a positive number is ignored, and a value outside 0.25 to 8 is brought into that range, each with a warning in the log.

On Windows, the app stays per-monitor DPI aware: only Uno Platform's own scale changes. A window on a 125% monitor with the override set to `1` draws its content smaller than other apps do. When the window moves to a monitor with another scale, it keeps its size in pixels. The window frame, title bar and icons still follow the monitor's scale, so the client area changes by a few pixels. The content of a native element such as a `WebView2` also keeps the monitor's scale. This is mostly useful to run tests written for a given scale on any monitor.

## X11 Specifics

When running using X11 Wayland compatibility (e.g. recent Ubuntu releases), DPI scaling cannot be determined in a reliable way. In order to specify the scaling to be used by Uno Platform, set the `UNO_DISPLAY_SCALE_OVERRIDE` environment variable (see [Display Scale Override](#display-scale-override)). When it isn't set, the scale comes from the `Xft.dpi` X resource when there is one.

The X11 support uses DBus for various interactions with the system, such as file selection. Make sure that dbus is installed.

System appearance settings are read from the [XDG desktop portal](https://flatpak.github.io/xdg-desktop-portal/docs/doc-org.freedesktop.portal.Settings.html) (`org.freedesktop.portal.Settings`) over the session bus: the light or dark theme, the high contrast mode, the text scaling factor and the [system accent color](windows-ui-viewmanagement.md#system-accent-color). The portal is normally installed and started by the desktop environment (`xdg-desktop-portal` plus a backend such as `xdg-desktop-portal-gnome` or `xdg-desktop-portal-kde`). Each setting needs a backend that exposes it; the accent color requires version 2 of the Settings portal with the `accent-color` key, available on GNOME 47+ and KDE Plasma 6. When the session bus, the portal or the backend is not available, Uno Platform logs the failure and falls back to the defaults (light theme, default blue accent).

## .NET Native AOT support

Building an Uno Platform Skia Desktop app with .NET (7+) Native AOT requires Uno Platform 4.7 (or later).
[Uno Platform 6.6 improves support for .NET Native AOT](xref:Uno.Features.NativeAOT).

To build an app with this feature enabled:

1. Add the following property in your `.csproj`:

   ```xml
   <PropertyGroup>
      <PublishAot>true</PublishAot>
   </PropertyGroup>
   ```

1. When using Uno Platform before Uno Platform 6.6, add the following items in your `.csproj`:

   ```xml
   <ItemGroup>
      <TrimmerRootAssembly Include="MyApp" />
   </ItemGroup>
   ```

1. Build your app with:

   ```dotnetcli
   dotnet publish -c Release -f net10.0-desktop
   ```

   > [!NOTE]
   > Cross-compilation support is not supported as of .NET 7. To build a Native AOT app for Linux or Mac, you'll need to build on the corresponding host.
   > [!NOTE]
   > .NET Native AOT on Windows is not yet supported as WPF does not support it at this time.

### Automatic Binding Preservation

When building an Uno Platform application head (for example, a Skia Desktop head) with Native AOT, Uno Platform automatically preserves public properties of types referenced by `[Bindable]` types to ensure data binding works correctly at runtime. This happens automatically when both `IsUnoHead=true` and `PublishAot=true` are set on the project (Uno head templates set `IsUnoHead` for you).

> [!NOTE]
> Automatic binding preservation is only available for Uno Platform application heads. Class libraries or other projects that set `PublishAot=true` but are not marked with `IsUnoHead=true` will not get this behavior.

The build system:

1. Finds all types marked with `Microsoft.UI.Xaml.Data.BindableAttribute` or `Uno.Extensions.Reactive.Bindings.BindableAttribute`
2. Discovers types referenced by public properties of those bindable types
3. Generates an ILLink descriptor file to preserve the public properties (getters/setters) of discovered types

For example, if you have:

```csharp
[Bindable]
public class MainViewModel
{
    public Entity MyEntity { get; set; }
}

public record Entity(string Name);
```

The build system will automatically preserve the `Name` property of `Entity`, allowing `{Binding MyEntity.Name}` expressions to work correctly in Native AOT builds.

For more information, see [the runtime documentation](https://github.com/dotnet/runtime/blob/main/src/coreclr/nativeaot/docs/reflection-in-aot-mode.md) and the [.NET Native AOT documentation](https://learn.microsoft.com/dotnet/core/deploying/native-aot/).
