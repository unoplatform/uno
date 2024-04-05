---
uid: Uno.Skia.Gtk
---

# Using the Skia+GTK head

> [!IMPORTANT]
> The Skia+Gtk support is being retired as of Uno Platform 5.2. The package will continue to be available until further notice and the latest apps should be moving to the [Skia Desktop support](xref:Uno.Skia.Desktop).

Uno supports running applications using Gtk+3 shell, using a Skia backend rendering. Gtk3+ is used to create a shell for the application to be used on various operating systems, such as Linux, Windows, and macOS.

Depending on the target platform, the UI rendering may be using OpenGL or software rendering.

Note that for Linux, the [FrameBuffer rendering](using-linux-framebuffer.md) head is also available.

## Get started with the Skia+GTK head

Follow the getting started guide for [VS Code](xref:Uno.GetStarted.vscode) or [Visual Studio 2022](xref:Uno.GetStarted.vs2022), make sure to use Uno Platform 5.1 templates to get it.

### Additional setup

#### [**Windows**](#tab/windows)

[!include[windows-setup](../includes/additional-windows-setup-inline.md)]

#### [**Linux**](#tab/linux)

[!include[linux-setup](../includes/additional-linux-setup-inline.md)]

#### [**macOS**](#tab/macos)

[!include[macos-setup](../includes/additional-macos-setup-inline.md)]

***

Once done, you can create a new app with [`dotnet new`](xref:Uno.GetStarted.dotnet-new) using:

```dotnetcli
dotnet new unoapp --preset=blank -o MyApp
```

or by using the Visual Studio ["project new" templates](xref:Uno.GetStarted.vs2022).

## Changing the rendering target

It may be required, depending on the environment, to use software rendering.

To do so, immediately before the line `host.Run()` in you `Program.cs` file, add the following:

```csharp
host.RenderSurfaceType = RenderSurfaceType.Software;
```

### Hosting Native GTK Controls

Hosting native GTK controls is supported through `ContentPresenter` and `ContentControl`. For more information, see [embedding native controls](xref:Uno.Skia.Embedding.Native).

### Linux considerations

When running under Linux, GTK can use OpenGL for the UI rendering but some restrictions can apply depending on the environment and available hardware.

- When running under Wayland, to enable OpenGL acceleration, you may need to set the `GDK_BACKEND` environment variable to `x11` before running your application.
- When running under Wayland and running with OpenGL ES 3.3 or later (using glxinfo to confirm), you may need to set the `GDK_GL` environment variable to `gles` before running your application.

### Troubleshooting OpenGL integration

Enabling debug logging messages for the GTK Host can help diagnose the render surface type selection.

In your `App.xaml.cs` file, change the minimum log level to:

```csharp
builder.SetMinimumLevel(LogLevel.Debug);
```

Then change the logging level of the GTK Host to `Information` or `Debug`:

```csharp
builder.AddFilter("Uno.UI.Runtime.Skia", LogLevel.Information);
```

You may also need to initialize the logging system earlier than what is found in Uno.UI's default templates by calling this in `Main`:

```csharp
YourAppNamespace.App.ConfigureFilters(); // Enable tracing of the GTK host
```

## Upgrading to a later version of SkiaSharp

By default, Uno comes with a set of **SkiaSharp** dependencies set by the **[Uno.UI.Runtime.Skia.Gtk](https://nuget.info/packages/Uno.UI.Runtime.Skia.Gtk)** package.

If you want to upgrade **SkiaSharp** to a later version, you'll need to specify all packages individually in your project as follows:

```xml
<ItemGroup>
   <PackagReference Include="SkiaSharp" Version="2.88.7" />
   <PackagReference Include="SkiaSharp.Harfbuzz" Version="2.88.7" />
   <PackagReference Include="SkiaSharp.NativeAssets.Linux" Version="2.88.7" />
   <PackageReference Update="SkiaSharp.NativeAssets.macOS" Version="2.88.7" />
</ItemGroup>
```

### Remote debugging

VS Code can [connect to a remote computer](https://code.visualstudio.com/docs/remote/ssh) (using SSH) to develop and debug projects. Note that all the sources, build and debugging are done on the remote computer, where the Uno Platform extension is being executed.

To debug the application remotely, it's necessary to modify the launch.json file and add the DISPLAY variable in the env section:

```json
{
  "configurations": [
    {
      "env": {
        "DOTNET_MODIFIABLE_ASSEMBLIES": "debug",
        "DISPLAY": ":0"
      }
    }
  ]
}
```

### .NET Native AOT support

Building an Uno Platform Skia+GTK app with .NET (7+) Native AOT requires, GtkSharp 3.24.24.38 (or later), or Uno Platform 4.7 (or later).

To build an app with this feature enabled:

1. Add the following property in your `.csproj`:

   ```xml
   <PropertyGroup>
      <PublishAot>true</PublishAot>
   </PropertyGroup>
   ```

1. Upgrade your project to net7.0:

   ```xml
   <TargetFramework>net7.0</TargetFramework>
   ```

1. Add the following items in your `.csproj`:

   ```xml
   <ItemGroup>
      <TrimmerRootAssembly Include="MyApp.Skia.Gtk" />
      <TrimmerRootAssembly Include="GtkSharp" />
      <TrimmerRootAssembly Include="GdkSharp" />
   </ItemGroup>
   ```

1. Build your app with:

   ```dotnetcli
   dotnet publish -c Release
   ```

   > [!NOTE]
   > Cross-compilation support is not supported as of .NET 7. To build a Native AOT app for Linux or Mac, you'll need to build on the corresponding host.

For more information, see [the runtime documentation](https://github.com/dotnet/runtime/blob/main/src/coreclr/nativeaot/docs/reflection-in-aot-mode.md) and the [.NET Native AOT documentation](https://learn.microsoft.com/dotnet/core/deploying/native-aot/).
