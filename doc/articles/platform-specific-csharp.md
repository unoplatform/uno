---
uid: Uno.Development.PlatformSpecificCSharp
---

# Platform-specific C# code

Uno Platform allows you to reuse views and business logic across platforms. Sometimes though, you may want to write different code per platform. You may need to access platform-specific native APIs and 3rd-party libraries, or want your app to look and behave differently depending on the platform.

<div style="position: relative; width: 100%; padding-bottom: 56.25%;">
    <iframe
        src="https://www.youtube-nocookie.com/embed/WgKNG8Yjbc4"
        title="YouTube video player"
        frameborder="0"
        allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture; web-share"
        allowfullscreen
        style="position: absolute; top: 0; left: 0; width: 100%; height: 100%;">
    </iframe>
</div>

This guide covers multiple approaches to managing per-platform code in C#. See [this guide for managing per-platform XAML](xref:Uno.Development.PlatformSpecificXaml).

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
| WebAssembly     | `__WASM__`         | Only available in the `net8.0-browserwasm` target framework, see [below](xref:Uno.Development.PlatformSpecificCSharp#webassembly-considerations) |
| Skia            | `HAS_UNO_SKIA`     | Only available in the `net8.0-desktop` target framework, see [below](xref:Uno.Development.PlatformSpecificCSharp#webassembly-considerations) |
| _Non-Windows_   | `HAS_UNO`          | To learn about symbols available when `HAS_UNO` is not present, see [below](xref:Uno.Development.PlatformSpecificCSharp#windows-specific-code) |

> [!TIP]
> Conditionals can be combined with boolean operators, e.g. `#if __ANDROID__ || __IOS__`. It is also possible to define custom conditional compilation symbols per project in the 'Build' tab in the project's properties.

### Windows-specific code

On `net8.0-windows10` target framework, an Uno Platform application isn't using Uno.UI at all. It's compiled using Microsoft's own tooling. For that reason, the `HAS_UNO` symbol is not defined on Windows. This aspect can optionally be leveraged to write code specifically intended for Uno.

Apps generated with the default `unoapp` solution template use **Windows App SDK** when targeting Windows. While this is the recommended path for new Windows apps, some solutions instead use **UWP** to target Windows. Both app models define a different conditional symbol:

| App model   | Symbol        | Remarks       |
| ----------- | ------------- | ------------- |
| Windows App SDK | `WINDOWS10_0_18362_0_OR_GREATER`  | Depending on the `TargetFramework` value, the _18362_ part may need adjustment |
| Universal Windows Platform         | `NETFX_CORE`  | No longer defined in new apps by default |

### WebAssembly considerations

The Uno Platform templates differentiate platforms using the target framework, yet it's possible to determine the WebAssembly platform at runtime using:

```csharp
if (OperatingSystem.IsBrowser())
{
   // Do something WebAssembly specific
}
```

> [!NOTE]
> [JSImport/JSExport](xref:Uno.Wasm.Bootstrap.JSInterop) are available on all platforms targeting .NET 7 and later, and this code does not need to be conditionally excluded.

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

Heavy usage of `#if` conditionals in shared code makes it hard to read and comprehend. A better approach is to use [partial class definitions](https://learn.microsoft.com/dotnet/csharp/programming-guide/classes-and-structs/partial-classes-and-methods) to split shared and platform-specific code.

Starting from Uno Platform 5.2, in project or class libraries using the `Uno.Sdk`, a set of implicit file name conventions can be used to target specific platforms:

* `*.wasm.cs` is built only for `net8.0-browserwasm`
* `*.skia.cs` is built only for `net8.0-desktop`
* `*.reference.cs` is built only for `net8.0-desktop`
* `*.iOS.cs` is built only for `net8.0-ios` and `net8.0-maccatalyst`
* `*.macOS.cs` is built only for `net8.0-macos`
* `*.iOSmacOS.cs` is built only for `net8.0-ios` and `net8.0-macos`
* `*.Android.cs` is built only for `net8.0-android`

Using file name conventions allows for reducing the use of `#if` compiler directives.

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
