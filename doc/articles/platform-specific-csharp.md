---
uid: Uno.Development.PlatformSpecificCSharp
---

# Platform-specific C# code in Uno

Uno allows you to reuse views and business logic across platforms. Sometimes though, you may want to write different code per platform. You may need to access platform-specific native APIs and 3rd-party libraries, or want your app to look and behave differently depending on the platform.

<iframe width="560" height="315" src="https://www.youtube-nocookie.com/embed/WgKNG8Yjbc4" title="YouTube video player" frameborder="0" allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture; web-share" allowfullscreen></iframe>

This guide covers multiple approaches to managing per-platform code in C#. See [this guide for managing per-platform XAML](platform-specific-xaml.md).

## Project structure

There are two ways to restrict code or XAML markup to be used only on a specific platform:

* Use [conditionals](https://learn.microsoft.com/dotnet/csharp/language-reference/preprocessor-directives/preprocessor-if) in a source file
* Place the code in a separate file which is only included in the desired platform.

The structure of an Uno app created with the default Visual Studio template is [explained in more detail here](uno-app-solution-structure.md).

## `#if` conditionals

The most basic means of authoring platform-specific code is to use `#if` conditionals:

```csharp
#if HAS_UNO
Console.WriteLine("Uno Platform - Pixel-perfect WinUI apps that run everywhere");
#else
Console.WriteLine("Windows - Built with Microsoft's own tooling");
#endif
```

If the supplied condition is not met, e.g. if `HAS_UNO` is not defined, then the enclosed code will be ignored by the compiler.

The following conditional symbols are predefined for each Uno platform:

| Platform        | Symbol             | Remarks |
| --------------- | ------------------ | ------- |
| Android         | `__ANDROID__`      | |
| iOS             | `__IOS__`          | |
| Catalyst        | `__MACCATALYST__`  | |
| macOS           | `__MACOS__`        | |
| WebAssembly     | `__WASM__`         | Only available in the `MyApp.WebAssembly` head, see [below](xref:Uno.Development.PlatformSpecificCSharp#webassembly-and-considerations) |
| Skia            | `HAS_UNO_SKIA`     | Only available in the Skia head, see [below](xref:Uno.Development.PlatformSpecificCSharp#webassembly-and-considerations) |
| _Non-Windows_   | `HAS_UNO`          | To learn about symbols available when `HAS_UNO` is not present, see [below](xref:Uno.Development.PlatformSpecificCSharp#windows-specific-code) |

> [!TIP]
> Conditionals can be combined with boolean operators, e.g. `#if __ANDROID__ || __IOS__`. It is also possible to define custom conditional compilation symbols per project in the 'Build' tab in the project's properties.

### Windows-specific code

On Windows (the Windows head project), an Uno Platform application isn't using Uno.UI at all. It's compiled just like a single-platform desktop application, using Microsoft's own tooling. For that reason, the `HAS_UNO` symbol is not defined on Windows. This aspect can optionally be leveraged to write code specifically intended for Uno.

Apps generated with the default `unoapp` solution template use **Windows App SDK** when targeting Windows. While this is the recommended path for new Windows apps, some solutions instead use **UWP** to target Windows. Both app models define a different conditional symbol:

| App model   | Symbol        | Remarks       |
| ----------- | ------------- | ------------- |
| Windows App SDK | `WINDOWS10_0_18362_0_OR_GREATER`  | Depending on the `TargetFramework` value, the _18362_ part may need adjustment |
| Universal Windows Platform         | `NETFX_CORE`  | No longer defined in new apps by default |

### WebAssembly and Skia considerations

The Uno Platform templates use a separate project library to share code between platforms. As of .NET 7, WebAssembly does not have its own `TargetFramework`, and Uno Platform uses the same value (e.g. `net7.0`) for both WebAssembly and Skia-based platforms. This means that `__WASM__` and `HAS_UNO_SKIA` are not available in this project, but are available in C# code specified directly in the individual heads.

In order to execute platform-specific code for WebAssembly, a runtime check needs to be included:

```csharp
if (RuntimeInformation.IsOSPlatform(OSPlatform.Create("BROWSER")))
{
   // Do something WebAssembly specific
}
```

> [!NOTE]
> [JSImport/JSExport](xref:Uno.Wasm.Bootstrap.JSInterop) are available on all platforms targeting .NET 7 and later, and this code does not need to be conditionally excluded.

WebAssembly is currently a `net7.0` target, and cannot yet be discriminated at compile time until a new TargetFramework for WebAssembly is included. This was [proposed](https://github.com/dotnet/designs/pull/289) as `net8.0-browser` in .NET 8, but it didn't yet happen.

## Type aliases

Defining a [type alias](https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/using-directive) with the `using` directive, in combination with `#if` conditionals, can make for cleaner code. For example:

```csharp
#if __ANDROID__
using _View = Android.Views.View;
#elif __IOS__
using _View = UIKit.UIView;
#else
using _View = Windows.UI.Xaml.UIElement;
#endif

...

public IEnumerable<_View> FindDescendants(FrameworkElement parent) => ...
```

## Partial class definitions

Heavy usage of `#if` conditionals in shared code makes it hard to read and comprehend. A better approach is to use [partial class definitions](https://learn.microsoft.com/dotnet/csharp/programming-guide/classes-and-structs/partial-classes-and-methods) to split shared and platform-specific code. These partial classes need to exist in the shared project as it's compiled separately from each project head. This method still requires `#if` conditionals.

### A simple example

Shared code in `PROJECTNAME/NativeWrapperControl.cs`:

```csharp
public partial class NativeWrapperControl : Control {

...

  protected override void OnApplyTemplate()
  {
    base.OnApplyTemplate();
   
      _nativeView = CreateNativeView();
  }
```

Platform-specific code in `PROJECTNAME/NativeWrapperControl.Android.cs`:

```csharp
#if __ANDROID__
public partial class NativeWrapperControl : Control {

...

  private View CreateNativeView() {
   ... //Android-specific code
  }
```

Platform-specific code in `PROJECTNAME/NativeWrapperControl.iOS.cs`:

```csharp
#if __IOS__
public partial class NativeWrapperControl : Control {

...

  private UIView CreateNativeView() {
   ... //iOS-specific code
  }
```

You can use [partial methods](https://learn.microsoft.com/dotnet/csharp/programming-guide/classes-and-structs/partial-classes-and-methods#partial-methods) when only one platform needs specialized logic.
