# Using the Skia+GTK head

Uno supports running applications using Gtk+3 shell, using a Skia backend rendering. Gtk3+ is used to create a shell for the application to be used on various operatings systems, such as Linux, Windows and macOS.

Depending on the target platform, the UI rendering may be using OpenGL or software rendering.

Note that for Linux, the [framebuffer rendering](using-linux-framebuffer.md) head is also available.

## Get started with the Skia+GTK head
Follow the getting started guide [for Linux](../get-started-with-linux.md) or [Windows](../get-started-vs-2022.md)

Once done, you can create a new app using:
```
dotnet new unoapp -o MyApp
```

or by using the Visual Studio "project new" templates.

## Changing the rendering target

It may be required, depending on the environment, to use software rendering.

To do so, immediately before the line `host.Run()` in you `main.cs` file, add the following:
```
host.RenderSurfaceType = RenderSurfaceType.Software;
```

## Upgrading to a later version of SkiaSharp

By default Uno comes with a set of **SkiaSharp** dependencies set by the **[Uno.UI.Runtime.Skia.Gtk](https://nuget.info/packages/Uno.UI.Runtime.Skia.Gtk)** package. 

If you want to upgrade **SkiaSharp** to a later version, you'll need to specify all packages individually in your project as follows:

```xml
<ItemGroup>
   <PackagReference Include="SkiaSharp" Version="2.88.0" /> 
   <PackagReference Include="SkiaSharp.Harfbuzz" Version="2.88.0" /> 
   <PackagReference Include="SkiaSharp.NativeAssets.Linux" Version="2.88.0" /> 
   <PackageReference Update="SkiaSharp.NativeAssets.macOS" Version="2.88.0" />
</ItemGroup>
```
