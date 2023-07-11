---
uid: Uno.Development.PlatformSpecificCSharp
---

# Platform-specific C# code in Uno

Uno allows you to reuse views and business logic across platforms. Sometimes though you may want to write different code per platform. You may need to access platform-specific native APIs and 3rd-party libraries, or want your app to look and behave differently depending on the platform. 

This guide covers multiple approaches to managing per-platform code in C#. See [this guide for managing per-platform XAML](platform-specific-xaml.md).

## Project structure

There are two ways to restrict code or XAML markup to be used only on a specific platform:

* Use [conditionals](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/preprocessor-directives/preprocessor-if) in a source file
* Place the code in a separate file which is only included in the desired platform.
 
The structure of an Uno app created with the default Visual Studio template is [explained in more detail here](uno-app-solution-structure.md).

 ## `#if` conditionals
 
 The most basic means of authoring platform-specific code is to use `#if` conditionals:
 
 ```csharp
 #if MY_SYMBOL
 Console.WriteLine("MY_SYMBOL is defined for this compilation");
 ```
 
 If the supplied condition is not met, e.g. if `MY_SYMBOL` is not defined, then the enclosed code will be ignored by the compiler.
 
 The following conditional symbols are predefined for each platform:
 
 | Platform    | Symbol                               | Comments |
 | ----------- | ------------------------------------ | ------- |
 | WinAppSDK   | `WINDOWS10_0_18362_0_OR_GREATER` 	  | Depending on your `TargetFramework` value, you may need to adjust the 18362 value |
 | UWP         | `NETFX_CORE`                         | |
 | Android     | `__ANDROID__`                        | |
 | iOS         | `__IOS__`                            | |
 | WebAssembly | `HAS_UNO_WASM`                       | Only available in the `MyApp.WebAssembly head, see below |
 | macOS       | `__MACOS__`                          | |
 | Catalyst    | `__MACCATALYST__`                    | |
 | Skia        | `HAS_UNO_SKIA`                       | |
 
Note that you can combine conditionals with boolean operators, e.g. `#if __ANDROID__ || __IOS__`. 

You can define your own conditional compilation symbols per project in the 'Build' tab in the project's properties.

### WebAssembly considerations

The Uno Platform templates use a separate project library to share your code between platforms. As of .NET 7, WebAssembly does not have its own `TargetFramework` and Uno Platform uses the same value (e.g. `net7.0`) for both WebAssembly and Skia-based platforms. This means that `__WASM__` and `HAS_UNO_WASM` are not available in this project, but are available C# code specified directly in the `MyApp.WebAssembly` head.

In order to create platform specific code for WebAssembly, a runtime check needs to included to execute WebAssembly specific code:

```csharp
if (RuntimeInformation.IsOSPlatform(OSPlatform.Create("BROWSER")))
{
   // Do something WebAssembly specific
}
```

> [!NOTE]
> [JSImport/JSExport](xref:Uno.Wasm.Bootstrap.JSInterop) are available on all platforms targeting .NET 7 and later, and this code does not need to be conditionally excluded.

WebAssembly is a currently a `net7.0` target, and cannot yet be discriminated at compile time, at least not until (dotnet/designs#289)[https://github.com/dotnet/designs/pull/289] will be available in .NET 8 with the inclusion of `net8.0-browser`.

## Type aliases

Defining a [type alias](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/using-directive) with the `using` directive, in combination with `#if` conditionals, can make for cleaner code. For example:

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

Heavy usage of `#if` conditionals in shared code makes it hard to read and comprehend. A better approach is to use [partial class definitions](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/partial-classes-and-methods) to split shared and platform-specific code. These partial classes need to exist in the shared project as it's compiled separately from each project head. Also, `#if` conditionals are still necessary.

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

You can use [partial methods](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/partial-classes-and-methods#partial-methods) when only one platform needs specialized logic.
