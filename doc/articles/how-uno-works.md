---
uid: Uno.Development.HowItWorks
---

# How Uno Platform works

How does Uno Platform make the same application code run on all platforms?

## On Windows

On Windows (the Windows head project), an Uno Platform application isn't using Uno.UI at all. It's compiled just like a single-platform WinUI application, using Microsoft's own tooling.

The rest of this article discusses how the Uno.UI tooling allows WinUI-compatible XAML and/or C# applications to run on non-Windows platforms.

## Uno.UI at runtime

The [`Uno.WinUI` NuGet package](https://www.nuget.org/packages/Uno.WinUI/) completely reproduces the WinUI API surface: all namespaces (`Microsoft.UI.Xaml`, `Windows.Foundation`, `Windows.Storage`, etc), all classes, all class members. Insofar as possible, the same look and behavior as on Windows are replicated on all other platforms.

Note that, as the API surface is very large, some parts are included but not implemented. These features are marked with the `Uno.NotImplementedAttribute` attribute, and a code analyzer is included with the Uno.WinUI package will generate a warning for any such features that are referenced. You can see a complete list of supported APIs [here](implemented-views.md).

## How the UI is rendered

As an application developer, you normally don't need to worry about exactly how Uno renders your visual tree to the screen on each platform. For every platform, the public API - panels, control templates, measurement and arranging, etc - is the same. However, it's sometimes useful to know what's going on at render-time, for example if you're [intermixing native views with XAML views](native-views.md) on a platform-specific basis.

### Web (WebAssembly)

On the web, each XAML element is converted into an appropriate [HTML element](https://developer.mozilla.org/en-US/docs/Glossary/Element). Panels, controls, and other 'intermediate' elements in the visual tree are converted to `<div/>` elements, whereas 'leaf' elements like `TextBlock`, `Image` etc get converted into more specific tags (`<p/>`, `<img/>` etc).

### iOS and Mac Catalyst

On iOS and Mac Catalyst, all types that inherit from `Microsoft.UI.Xaml.FrameworkElement`, also inherit from the native [`UIView` type](https://learn.microsoft.com/dotnet/api/uikit.uiview). That is to say, on iOS, all XAML visual elements are also native views.

When rendered at runtime, certain `FrameworkElement` types implicitly create inner views that inherit from higher-level native view types. For example, `Image` implicitly creates an inner `NativeImage` view, where `NativeImage` is an Uno-defined internal type that inherits directly from the native `UIKit.UIImageView` type.

### Android

On Android, all types that inherit from `Microsoft.UI.Xaml.FrameworkElement`, also inherit from the native [`ViewGroup` type](https://learn.microsoft.com/dotnet/api/android.views.viewgroup). That is to say, on Android, all XAML visual elements are also native views.

When rendered at runtime, certain `FrameworkElement` types implicitly create inner views that inherit from higher-level native view types. For example, `Image` implicitly creates an inner `NativeImageView`, where `NativeImageView` is an Uno-defined internal type that inherits directly from the native `Android.Widget.ImageView` type.

### macOS AppKit

On macOS, all types that inherit from `Microsoft.UI.Xaml.FrameworkElement`, also inherit from the native [`NSView` type](https://learn.microsoft.com/dotnet/api/appkit.nsview). That is to say, on macOS, all XAML visual elements are also native views.

When rendered at runtime, certain `FrameworkElement` types implicitly create inner views that inherit from higher-level native view types. For example, `Image` implicitly creates an inner `NativeImageView`, where `NativeImageView` is an Uno-defined internal type that inherits directly from the native `AppKit.NSImageView` type.

### Skia (Linux with X11/Wayland, Linux Framebuffer, macOS, Windows)

On Linux, XAML visual elements are rendered directly to a [Skia](https://skia.org/) canvas. Unlike the other target platforms, there's no 'native view type' to speak of. The Skia canvas is hosted inside of a [Gtk](https://www.gtk.org/) shell, but it's just that, a shell - there aren't any Gtk widgets used. However, the Gtk and WPF APIs are used by Uno to handle [pointer input](features/pointers-keyboard-and-other-user-inputs.md).

## Uno.UI at build time

The codebase of an Uno Platform application is a mix of XAML markup, C# code, images, string resources, and miscellaneous other assets. At build time, Uno.UI takes the shared codebase and creates a native application for the target being built, including binaries, properly named and packaged assets, etc. How does it happen?

### Binaries

The C# code is the easy part - [.NET runs pretty much everywhere](https://docs.microsoft.com/en-us/dotnet/core/introduction). On iOS, Android, macOS, and Mac Catalyst, Uno.WinUI is using .NET for Mobile (previously known as [Xamarin Native](https://dotnet.microsoft.com/apps/xamarin), and is not Xamarin.Forms or MAUI). On the web, it uses .NET running in [WebAssembly](https://webassembly.org/), and on Linux, it's running under .NET 7 or later.

The compiled binaries also include the output of the XAML parser, as described in the next section.

### XAML files

Uno.UI parses XAML files at build time and converts that into equivalent C# code via a Roslyn source generator, which is then compiled in the usual manner. This process is normally transparent to the application developer.

### Images, string resources, etc

Images are copied by Uno.UI into the target project according to the directory structure and naming conventions of the target platform. Similarly, string resources (`.resw` files) are converted into the native target format. Read more [here](features/working-with-assets.md). Again, this conversion process is normally transparent to the application developer.
