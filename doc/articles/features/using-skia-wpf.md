# Using the Skia+WPF head

Uno Platform supports running applications using a WPF shell, using a Skia backend rendering. WPF is used to create a shell for the application to be used on various versions of Windows, going back to Windows 7.

> [!Tip]
> For a step-by-step guide to installing the prerequisites for your preferred IDE and environment, consult the [Get Started guide](../get-started.md).

## Anatomy of the Skia+WPF project
When creating a Skia+WPF solution, you will get at least the project head, and a shared project.

### The head project
The project head contains the WPF assets, XAML and C# files for the WPF part of the app (the shell).

The XAML files in this project are using the WPF syntax and APIs, and contain a `ContentControl` in which the WinUI/Uno content will be drawn.

### The shared project
The shared project contain the WinUI part of the app, and is shared with all the others platform heads. This is where most of your app code will be located.

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

## Upgrading from earlier versions of the Uno Platform template

In previous versions of the Uno Platform template, the Skia+WPF support was split in two projects.

This is not required anymore, but to be able to use a single project, you'll can either:
- Recreate your project from the templates and migrate your code to the new project
- Make some adjustments to your projects:
    1. From the `MyProject.Skia.Wpf.csproj` :
        - Copy the last `<Import Project="..." Label="Shared" />` line to `MyProject.Skia.Wpf.csproj`
        - Copy the `Microsoft.Extensions.Logging` and `Microsoft.Extensions.Logging.Console` lines to `MyProject.Skia.Wpf.csproj`
    1. Delete the `MyProject.Skia.Wpf.csproj`
    1. Rename `MyProject.Skia.Wpf.Host.csproj` to `MyProject.Skia.Wpf.csproj`
    1. In the `MyProject.Shared.projitems`, add the following code just before the last `</ItemGroup>` line:
        ```xml
        <!-- Mark the files from this folder as being part of WinUI -->
		<Page Update="$(MSBuildThisFileDirectory)**/*.xaml" XamlRuntime="WinUI" />
		<ApplicationDefinition Update="$(MSBuildThisFileDirectory)**/*.xaml" XamlRuntime="WinUI" />

		<!-- Make sure XAML files force reevaluation of up-to-date checks -->
		<UpToDateCheckInput Include="$(MSBuildThisFileDirectory)**/*.xaml" />
        ```
        You'll need edit this file outside of Visual Studio. You need an example, create a new temporary project, and take a look at the way these lines are defined in the shared project file.
