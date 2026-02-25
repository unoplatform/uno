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

Other parts of the framework are implemented using a mix of shared and platform-specific code, such as view types (ie types inheriting from [`UIElement`](https://learn.microsoft.com/uwp/api/windows.ui.xaml.uielement)). There is a tendency for high-level controls, which are typically built by composition of simpler visual primitives, to be implemented mainly by shared code - [`NavigationView`](https://github.com/unoplatform/uno/tree/master/src/Uno.UI/UI/Xaml/Controls/NavigationView) is a good example. The primitives themselves, such as [`TextBlock`](https://github.com/unoplatform/uno/tree/master/src/Uno.UI/UI/Xaml/Controls/TextBlock), [`Image`](https://github.com/unoplatform/uno/tree/master/src/Uno.UI/UI/Xaml/Controls/Image), or [`Shape`](https://github.com/unoplatform/uno/tree/master/src/Uno.UI/UI/Xaml/Shapes), contain a much higher proportion of platform-specific code as they need to call into per-platform rendering APIs.

The layouting system is implemented in shared code as much as possible, for cross-platform consistency; however it's nonetheless tied into the underlying native layout cycle on each platform.

APIs for non-UI features, for example [`Windows.System.Power`](../features/windows-system-power.md) or [`Windows.Devices.Sensors`](../features/windows-devices-sensors.md), incorporate a large fraction of platform-specific code to interact with the associated native APIs.

### Generated `NotImplemented` stubs

WinUI has a very large API surface area, and not all features in it have been implemented by Uno Platform. We want pre-existing WinUI apps and libraries that reference these features to still be able to at least compile on Uno Platform. To support this, an [internal automated tool](https://github.com/unoplatform/uno/tree/master/src/Uno.UWPSyncGenerator) inspects the WinUI framework, compares it to authored code in Uno Platform, and generates stubs for all types and type members that exist in WinUI but are not implemented on Uno. For example:

```csharp
#if __ANDROID__ || __IOS__ || __TVOS__ || IS_UNIT_TESTS || __WASM__
[global::Uno.NotImplemented]
public  bool ExitDisplayModeOnAccessKeyInvoked
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

Notice the platform conditionals, since a member may be implemented for some platforms but not others. The `[NotImplemented]` attribute flags this property as not implemented and a code analyzer surfaces a warning if it is referenced in app code.

### Platform-specific details

For more details on how Uno Platform runs on each platform, see platform-specific information for:

* [Android](uno-internals-android.md)
* [iOS](uno-internals-ios.md)
* [WebAssembly](uno-internals-wasm.md)

## Uno.WinUI build-time tooling

### Parsing XAML to C# code

This is the most substantial compile-time task that Uno Platform carries out. Whenever an app or class library is built, all contained XAML files are parsed and converted to C# files, which are then compiled in the usual way. (Note that this differs from WinUI, which parses XAML to XAML Binary Format (.xbf) files which are processed by the WinUI runtime.)

Uno Platform uses existing libraries to parse a given XAML file into a XAML object tree, then Uno-specific code is responsible for interpreting the XAML object tree as a tree of visual elements and their properties. Most of this takes place within the [`XamlFileGenerator`](https://github.com/unoplatform/uno/blob/master/src/SourceGenerators/Uno.UI.SourceGenerators/XamlGenerator/XamlFileGenerator.cs) class.

### DependencyObject implementation generator

On [Android](uno-internals-android.md) and [iOS](uno-internals-ios.md), `UIElement` (the base view type in WinUI) inherits from the native view class on the respective platform. This poses a challenge because `UIElement` inherits from the `DependencyObject` class in UWP/WinUI, which is a key part of the dependency property system. Uno makes this work by breaking from WinUI and having `DependencyObject` be an interface rather than a type.

Class library authors and app authors sometimes inherit directly from `DependencyObject` rather than a more derived type. To support this scenario seamlessly, the [`DependencyObjectGenerator` task](https://github.com/unoplatform/uno/blob/master/src/SourceGenerators/Uno.UI.SourceGenerators/DependencyObject/DependencyObjectGenerator.cs) looks for such classes and [generates](https://github.com/unoplatform/Uno.SourceGeneration) a partial implementation for the `DependencyObject` interface, ie, the methods that on UWP would be inherited from the base class.

### Formatting image assets

Different platforms have different requirements for where bundled image files are located and how multiple versions of the same asset are handled (eg, to target different resolutions). The [asset retargeting task](https://github.com/unoplatform/uno/blob/master/src/SourceGenerators/Uno.UI.Tasks/Assets/RetargetAssets.cs) copies the image assets located in the shared project to the appropriate location and format for the target platform.

### Formatting string resources

As with images, different platforms have different requirements for the location and formatting of localized string resources. The [ResourcesGenerationTask](https://github.com/unoplatform/uno/blob/master/src/SourceGenerators/Uno.UI.Tasks/ResourcesGenerator/ResourcesGenerationTask.cs) reads the strings defined in WinUI's `*.resw` files in the shared project, and generates the appropriate platform-specific file.
