# Platform-specific C# code in Uno

Uno allows you to reuse views and business logic across platforms. Sometimes though you may want to write different code per platform, either because you need to access platform-specific native APIs and 3rd-party libraries, or because you want your app to look and behave differently depending on the platform. 

This guide covers multiple approaches to managing per-platform code in C#. See [this guide for managing per-platform XAML](platform-specific-xaml.md).

## Project structure

There are two ways to restrict code or XAML markup to be used only on a specific platform:

* Use [conditionals](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/preprocessor-directives/preprocessor-if) within a shared file
* Place the code in a file which is only included in the desired platform head.
 
The structure of an Uno app created with the default [Visual Studio template](https://marketplace.visualstudio.com/items?itemName=nventivecorp.uno-platform-addin) is [explained in more detail here](uno-app-solution-structure.md). The key point to understand is that files in a shared project referenced from a platform head **are treated in exactly the same way** as files included directly under the head, and are compiled together into a single assembly.
 
 
 ## `#if` conditionals
 
 The most basic means of authoring platform-specific code is to use `#if` conditionals:
 
 ```csharp
 #if MY_SYMBOL
 Console.WriteLine("MY_SYMBOL is defined for this compilation");
 ```
 
 If the supplied condition is not met, e.g. if `MY_SYMBOL` is not defined, then the enclosed code will be ignored by the compiler.
 
 The following conditional symbols are predefined for each platform:
 
 | Platform    | Symbol        |
 | ----------- | ------------- |
 | UWP         | `NETFX_CORE`  |
 | Android     | `__ANDROID__` |
 | iOS         | `__IOS__`     |
 | WebAssembly | `__WASM__`    |
 | MacOS       | `__MACOS__`   |
 
Note that you can combine conditionals with boolean operators, e.g. `#if __ANDROID__ || __IOS__`. 

You can define your own conditional compilation symbols per project in the 'Build' tab in the project's properties.

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

Heavy usage of `#if` conditionals makes code hard to read and comprehend. A better approach is to use [partial class definitions](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/partial-classes-and-methods) to split shared and platform-specific code.

### A simple example

Shared code in `PROJECTNAME.Shared/NativeWrapperControl.cs`:

```csharp
public partial class NativeWrapperControl : Control {

...

		protected override void OnApplyTemplate()
		{
			 base.OnApplyTemplate();
   
  			 _nativeView = CreateNativeView();
		}
```

Platform-specific code in `PROJECTNAME.Droid/NativeWrapperControl.Android.cs`:

```csharp
public partial class NativeWrapperControl : Control {

...

		private View CreateNativeView() {
			... //Android-specific code
		}
```

Platform-specific code in `PROJECTNAME.iOS/NativeWrapperControl.iOS.cs`:

```csharp
public partial class NativeWrapperControl : Control {

...

		private UIView CreateNativeView() {
			... //iOS-specific code
		}
```

You can use [partial methods](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/partial-classes-and-methods#partial-methods) when only one platform needs specialized logic.
