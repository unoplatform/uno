---
uid: Uno.Development.PlatformSpecificCSharp
---

# Platform-specific C# code

Uno Platform allows you to reuse views and business logic across platforms. Sometimes though, you may want to write different code per platform. You may need to access platform-specific native APIs and 3rd-party libraries, or want your app to look and behave differently depending on the platform.

> [!Video https://www.youtube-nocookie.com/embed/WgKNG8Yjbc4]

This guide covers multiple approaches to managing per-platform code in C#. See [this guide for managing per-platform XAML](xref:Uno.Development.PlatformSpecificXaml).

## Project structure

There are multiple ways to restrict code or XAML markup to be used only on a specific platform:

* Use conditional code based on the `OperatingSystem.IsXXX` method
* Use [conditionals](https://learn.microsoft.com/dotnet/csharp/language-reference/preprocessor-directives/preprocessor-if) in a source file
* Place the code in a separate file which is only included in the desired platform.

The structure of an Uno Platform app created with the default Visual Studio template is [explained in more detail here](xref:Uno.Development.AppStructure).

### OperatingSystem.IsXXX

The Uno Platform templates differentiate platforms using the current target framework, yet it's possible to determine the running platform at runtime using:

```csharp
if (OperatingSystem.IsBrowser())
{
   // Do something WebAssembly specific
}
```

When building an application that uses the Skia renderer, it is generally best to use such conditionals in order to build for only one target framework in class libraries (e.g., when only targeting `net10.0`, without a platform specifier like `net10.0-ios`).

> [!NOTE]
> [JSImport/JSExport](xref:Uno.Wasm.Bootstrap.JSInterop) and [BrowserHtmlElement](xref:Uno.Interop.WasmJavaScript1) are available on all platforms targeting .NET 7 and later, and code using those APIs to be conditionally excluded at compile time, and should only use `OperatingSystem` conditions.

## `#if` conditionals

The most basic means of authoring platform-specific code is to use `#if` conditionals:

```csharp
#if __UNO__
Console.WriteLine("Uno Platform - Pixel-perfect WinUI apps that run everywhere");
#else
Console.WriteLine("Windows - Built with Microsoft's own tooling");
#endif
```

If the supplied condition is not met, e.g. if `__UNO__` is not defined, then the enclosed code will be ignored by the compiler.

The following conditional symbols are predefined for each Uno platform:

| Platform        | Symbol             | Remarks |
| --------------- | ------------------ | ------- |
| Android         | `__ANDROID__`      | |
| iOS             | `__IOS__`          | |
| tvOS            | `__TVOS__`         | |
| Catalyst        | `__MACCATALYST__`  | |
| iOS or tvOS or Catalyst | `__APPLE_UIKIT__` | |
| WebAssembly     | `__WASM__`         | Only available in the `net10.0-browserwasm` target framework, see [below](xref:Uno.Development.PlatformSpecificCSharp#webassembly-considerations) |
| Desktop         | `__DESKTOP__`      | Only available in the `net10.0-desktop` target framework. |
| Skia            | `__UNO_SKIA__`     | Only available with `SkiaRenderer` feature. |
| _Non-Windows_   | `__UNO__`          | To learn about symbols available when `__UNO__` is not present, see [below](xref:Uno.Development.PlatformSpecificCSharp#windows-specific-code) |

> [!TIP]
> Conditionals can be combined with boolean operators, e.g. `#if __ANDROID__ || __IOS__`. It is also possible to define custom conditional compilation symbols per project in the 'Build' tab in the project's properties.

### Windows-specific code

On `net10.0-windows10.0.xxxxx` target framework, an Uno Platform application isn't using Uno.UI at all. It's compiled using Microsoft's own tooling. For that reason, the `__UNO__` symbol is not defined on Windows. This aspect can optionally be leveraged to write code specifically intended for Uno.

Apps generated with the default `unoapp` solution template use **Windows App SDK** when targeting Windows. While this is the recommended path for new Windows apps, some solutions instead use **UWP** to target Windows. Both app models define a different conditional symbol:

| App model   | Symbol        | Remarks       |
| ----------- | ------------- | ------------- |
| Windows App SDK | `WINDOWS10_0_18362_0_OR_GREATER`  | Depending on the `TargetFramework` value, the _18362_ part may need adjustment |
| Universal Windows Platform         | `NETFX_CORE`  | No longer defined in new apps by default |

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

* `*.wasm.cs` is built only for `net10.0-browserwasm`
* `*.desktop.cs` is built only for `net10.0-desktop`
* `*.iOS.cs` is built only for `net10.0-ios` and `net10.0-maccatalyst`
* `*.tvOS.cs` is built only for `net10.0-tvos`
* `*.UIKit.cs` is built only for `net10.0-ios` and `net10.0-maccatalyst` and `net10.0-tvos`
* `*.Apple.cs` is built only for `net10.0-ios` and `net10.0-maccatalyst` and `net10.0-tvos`
* `*.Android.cs` is built only for `net10.0-android`
* `*.WinAppSDK.cs` is built only for `net10.0-windows10` (eg. `net10.0-windows10.0.22621`)

In addition, for class libraries:

* `*.reference.cs` is built only for reference implementation
* `*.crossruntime.cs` is built for WebAssembly, Desktop, and reference implementation

> [!NOTE]
> For backwards compatibility, using `.skia.cs` is currently equivalent to `.desktop.cs`. This might change in the future, so we recommend using the suffixes above instead.

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
