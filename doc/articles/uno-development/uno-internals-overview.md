---
uid: Uno.Contributing.Overview
---

# How Uno Platform works

This article explores how Uno works in detail, with a focus on information that's useful for contributors to Uno.

## What Uno Platform does

Uno Platform is a cross-platform projection of Microsoft's WinUI framework (and its preview iteration, UWP). Uno mirrors WinUI types and supports the WinUI XAML dialect, as well as handling several additional aspects of the app contract, like assets and string resources. Thus, it allows app code written for WinUI to be built and run on Android, iOS, Linux, macOS, and in the browser via WebAssembly.

> [!NOTE]
> While WinUI supports authoring app code in C++ as well as C#, Uno Platform only supports C#.

Broadly then, Uno Platform has two jobs to do:

* Duplicate the types provided by WinUI, including views in the `Microsoft.UI.Xaml` namespace, and non-UI APIs such as `Windows.Foundation`, `Windows.Storage`, etc...
* Perform compile-time tasks related to non-C# aspects of the WinUI app contract (parse XAML files, process assets to platform-specific formats, etc)

Like WinUI, Uno Platform provides access to the existing .NET libraries, via [.NET](https://dotnet.microsoft.com/en-us/).

Uno Platform aims to be a 1:1 match for WinUI, in API surface (types, properties, methods, events, etc), in appearance, and in behavior. At the same time, Uno Platform places an emphasis on native interoperability and making it easy to intermix purely native views with Uno/WinUI controls in the visual tree.

## Uno.WinUI as a class library

Certain aspects of the framework are not tied in any way to the platform that Uno happens to be running on. These include support for the [`DependencyProperty` system and data-binding](https://learn.microsoft.com/windows/uwp/xaml-platform/dependency-properties-overview), and style and resource resolution. The code that implements these features is fully shared across all platforms.

View types (ie types inheriting from [`UIElement`](https://learn.microsoft.com/uwp/api/windows.ui.xaml.uielement)) are also shared across all platforms. High-level controls such as [`NavigationView`](https://github.com/unoplatform/uno/tree/master/src/Uno.UI/UI/Xaml/Controls/NavigationView) are built by composition of simpler visual primitives, and the primitives themselves, such as [`TextBlock`](https://github.com/unoplatform/uno/tree/master/src/Uno.UI/UI/Xaml/Controls/TextBlock), [`Image`](https://github.com/unoplatform/uno/tree/master/src/Uno.UI/UI/Xaml/Controls/Image), or [`Shape`](https://github.com/unoplatform/uno/tree/master/src/Uno.UI/UI/Xaml/Shapes), draw through the shared Skia-based composition layer. The platform-specific parts of the UI (the window, rendering surface, input, text input and native element hosting) live in the `Uno.UI.Runtime.*` host projects.

The layouting system is implemented in shared code. Uno runs measure and arrange itself, and the host only supplies the window size and schedules frames.

APIs for non-UI features, for example [`Windows.System.Power`](../features/windows-system-power.md) or [`Windows.Devices.Sensors`](../features/windows-devices-sensors.md), incorporate a large fraction of platform-specific code to interact with the associated native APIs.

### Generated `NotImplemented` stubs

WinUI has a very large API surface area, and not all features in it have been implemented by Uno Platform. We want pre-existing WinUI apps and libraries that reference these features to still be able to at least compile on Uno Platform. To support this, an [internal automated tool](https://github.com/unoplatform/uno/tree/master/src/Uno.WinAppSDKSyncGenerator) inspects the WinUI framework, compares it to authored code in Uno Platform, and generates stubs for all types and type members that exist in WinUI but are not implemented on Uno. For example:

```csharp
#if __SKIA__
[global::Uno.NotImplemented("__SKIA__")]
public bool ExitDisplayModeOnAccessKeyInvoked
{
    get
    {
        return (bool)this.GetValue(ExitDisplayModeOnAccessKeyInvokedProperty);
    }
    set
    {
        this.SetValue(ExitDisplayModeOnAccessKeyInvokedProperty, value);
    }
}
#endif
```

Notice the platform conditional. The UI layer (`Uno.UI`) only builds for Skia, so its stubs are guarded by `__SKIA__` alone, while the non-UI WinRT APIs (`Uno.WinRT`, `Uno.Foundation`) are still built per platform and their stubs list each platform a member is not implemented on. The `[NotImplemented]` attribute flags this property as not implemented and a code analyzer surfaces a warning if it is referenced in app code.

### Platform-specific details

For more details on how Uno Platform runs on each platform, see platform-specific information for:

* [Android](uno-internals-android.md)
* [iOS](uno-internals-ios.md)
* [WebAssembly](uno-internals-wasm.md)
* [macOS](uno-internals-macos.md)

## Uno.WinUI build-time tooling

### Parsing XAML to C# code

This is the most substantial compile-time task that Uno Platform carries out. Whenever an app or class library is built, all contained XAML files are parsed and converted to C# files, which are then compiled in the usual way. (Note that this differs from WinUI, which parses XAML to XAML Binary Format (.xbf) files which are processed by the WinUI runtime.)

Uno Platform uses existing libraries to parse a given XAML file into a XAML object tree, then Uno-specific code is responsible for interpreting the XAML object tree as a tree of visual elements and their properties. Most of this takes place within the [`XamlFileGenerator`](https://github.com/unoplatform/uno/blob/master/src/SourceGenerators/Uno.UI.SourceGenerators/XamlGenerator/XamlFileGenerator.cs) class.

### `DependencyObject` is a class

`DependencyObject` is an ordinary class that `UIElement` inherits from, exactly as in WinUI, and inheriting from it directly needs no special handling.

This was not always so. Before Uno Platform 7.0, the Android and iOS renderers made `UIElement` inherit the platform's native view class, which left no base-class slot for `DependencyObject` — so Uno declared it as an *interface*, and a `DependencyObjectGenerator` source generator emitted the implementation into any class that inherited from it directly. Rendering is now Skia on every target, so the native base class is gone, `DependencyObject` went back to being a class, and that generator was removed along with it.

### Formatting image assets

Different platforms have different requirements for where bundled image files are located and how multiple versions of the same asset are handled (eg, to target different resolutions). The [asset retargeting task](https://github.com/unoplatform/uno/blob/master/src/SourceGenerators/Uno.UI.Tasks/Assets/RetargetAssets.cs) copies the image assets located in the shared project to the appropriate location and format for the target platform.

### Formatting string resources

As with images, different platforms have different requirements for the location and formatting of localized string resources. The [ResourcesGenerationTask](https://github.com/unoplatform/uno/blob/master/src/SourceGenerators/Uno.UI.Tasks/ResourcesGenerator/ResourcesGenerationTask.cs) reads the strings defined in WinUI's `*.resw` files in the shared project, and generates the appropriate platform-specific file.
