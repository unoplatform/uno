---
uid: Uno.Skia.Desktop
---

# Using the Skia Desktop

Uno Platform supports running applications using a common Skia Desktop shell, which is automatically used based on the running platform, using a single build output using the `net9.0-desktop` target framework from the [Uno.Sdk](xref:Uno.Features.Uno.Sdk).

The currently supported targets and platforms are:

- Linux X11
- [Linux Framebuffer](xref:Uno.Skia.Linux.Framebuffer)
- Windows (Using Win32 shell)
- [macOS (Using an AppKit shell)](xref:Uno.Skia.macOS)

The set of supported platforms can be defined by using the `UnoPlatformHostBuilder`, introduced in Uno Platform 5.2 and the [Single Project support](xref:Uno.Development.MigratingToSingleProject).

## Get started with the Skia Desktop head

Follow the getting started guide for [VS Code](xref:Uno.GetStarted.vscode) or [Visual Studio 2022](xref:Uno.GetStarted.vs2022), make sure to use Uno Platform 5.1 templates to get it.

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

This builder allows us to configure the SkiaHost and setup which platforms will be supported at runtime. The builder evaluates the platform's availability one by one, in the order of definition.

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
builder.AddFilter("Uno.UI.Runtime.Skia", LogLevel.Information);
```

You may also need to initialize the logging system earlier than what is found in Uno.UI's default templates by calling this in `Main`:

```csharp
YourAppNamespace.App.ConfigureFilters(); // Enable tracing of the Skia host
```

## Upgrading to a later version of SkiaSharp

By default, Uno Platform comes with a set of **SkiaSharp** dependencies.

If you want to upgrade **SkiaSharp** to a later version, you'll need to specify all packages individually in your project as follows:

```xml
<ItemGroup>
   <PackagReference Include="SkiaSharp" Version="3.119.0" />
   <PackagReference Include="SkiaSharp.NativeAssets.Linux" Version="3.119.0" />
   <PackageReference Update="SkiaSharp.NativeAssets.macOS" Version="3.119.0" />
   <PackagReference Include="HarfBuzzSharp" Version="8.3.1.1" />
</ItemGroup>
```

## Windows Specifics

### RDP Hardware Acceleration

The Uno Platform Skia Desktop runtime on Windows uses a WPF shell internally. By default, Uno Platform enables RDP hardware acceleration in order to get good performance, yet this feature is disabled by default in standard WPF apps with .NET 8.

If you're having issues with the Windows support for Skia Desktop over RDP, add the following to your project:

```xml
<ItemGroup>
    <RuntimeHostConfigurationOption Include="Switch.System.Windows.Media.EnableHardwareAccelerationInRdp" Value="false" />
</ItemGroup>
```

## X11 Specifics

When running using X11 Wayland compatibility (e.g. recent Ubuntu releases), DPI scaling cannot be determined in a reliable way. In order to specify the scaling to be used by Uno Platform, set the `UNO_DISPLAY_SCALE_OVERRIDE` environment variable. The default value is `1.0`.

The X11 support uses DBus for various interactions with the system, such as file selection. Make sure that dbus is installed.

## .NET Native AOT support

Building an Uno Platform Skia Desktop app with .NET (7+) Native AOT requires Uno Platform 4.7 (or later).

To build an app with this feature enabled:

1. Add the following property in your `.csproj`:

   ```xml
   <PropertyGroup>
      <PublishAot>true</PublishAot>
   </PropertyGroup>
   ```

1. Add the following items in your `.csproj`:

   ```xml
   <ItemGroup>
      <TrimmerRootAssembly Include="MyApp" />
   </ItemGroup>
   ```

1. Build your app with:

   ```dotnetcli
   dotnet publish -c Release -f net9.0-desktop
   ```

   > [!NOTE]
   > Cross-compilation support is not supported as of .NET 7. To build a Native AOT app for Linux or Mac, you'll need to build on the corresponding host.
   > [!NOTE]
   > .NET Native AOT on Windows is not yet supported as WPF does not support it at this time.

For more information, see [the runtime documentation](https://github.com/dotnet/runtime/blob/main/src/coreclr/nativeaot/docs/reflection-in-aot-mode.md) and the [.NET Native AOT documentation](https://learn.microsoft.com/dotnet/core/deploying/native-aot/).
