## What is the Uno Platform?

The Uno Platform is a Universal Windows Platform Bridge that allows UWP-based code (C# and XAML) to run on iOS, Android, and WebAssembly. It provides the full API definitions of the UWP Windows 10 October 2018 Update (17763), and the implementation of parts of the UWP API, such as **Windows.UI.Xaml**, to enable UWP applications to run on these platforms.

This allows the use of UWP tooling from Windows in [Visual Studio](https://www.visualstudio.com/), such as [Xaml Edit and Continue](https://blogs.msdn.microsoft.com/visualstudio/2016/04/06/ui-development-made-easier-with-xaml-edit-continue/) and [C# Edit and Continue](https://docs.microsoft.com/en-us/visualstudio/debugger/how-to-use-edit-and-continue-csharp), to build an application as much as possible on Windows, then validate that the application runs on iOS, Android and WebAssembly.

The XAML User Interface (UI) provides the ability to display the same XAML files on Windows, iOS, Android and WebAssembly platforms. Uno also provides support for the [Model-View-ViewModel](https://docs.microsoft.com/en-us/windows/uwp/data-binding/data-binding-and-mvvm) (MVVM) pattern on all platforms, with binding, styling, control and data-templating features.

As the Uno Platform provides all of the APIs of the complete UWP platform, any UWP library can be compiled on top of Uno (e.g. [XamlBehaviors](https://github.com/Microsoft/XamlBehaviors)), with the ability to determine which APIs are implemented or not via the IDE using C# Analyzers.

## Why Uno?

Developing for Windows (phone, desktop, tablet, XBox), iOS (tablet and phone),  Android (tablet and phone) and WebAssembly at once can be a complex process, especially when it comes to the user interface. Each platform has its own ways of defining dynamic layouts, with some being more efficient, some more verbose, some more elegant, and some more performant than others.

Yet, being able to master all these frameworks at once is a particularly difficult task because of the amount of platform-specific knowledge required to master each platform. Most of the time it boils down to different teams developing the same application multiple times, with each requiring a full development cycle.

With Xamarin, C# comes to all these platforms; however, it only provides transparent translations of the UI frameworks available for iOS and Android. Most non-UI code can be shared, but when it comes to the UI, almost nothing can be shared.

To avoid having to learn the UI-layout techniques and approaches for each platform, Uno.UI mimics the Windows XAML approach of defining UI and layouts. This translates into the ability to share styles, layouts, and data-bindings while retaining the ability to mix XAML-style and native layouts. For instance, a StackPanel can easily contain a RelativeLayout on Android or an MKMapView on iOS.

Uno.UI provides the ability for developers to reuse known layout and coding techniques on all platforms, resulting in a gain of overall productivity when creating UI-rich applications.

## What does Uno **not** do?

Uno is not meant to be a complete replacement of all the native UI frameworks. This would be the lowest-denominator approach and would result in end-users noticing the non-native appearance or behavior of an application on their device. Having an iOS application that behaves like an Android application may bother users.

Uno provides a common set of layout and controls, designed to provide the ability to share an important part of an application's code and markup; however, it leaves developers with the ability to retain the native look and feel. At the same time, it provides a way to have a *pixel-perfect* UI and UX being identical on all platforms. Commonly, this look and feel will be found in the navigation, transitions and animations, main pages, and edges of the screen.

While the Uno Platform provides all the UWP APIs, a lot of those APIs are not implemented. It currently provides a small set of basic non-UI parts of the UWP, such as the `Windows.UI.Xaml.Application` class, which provides the ability to have a common application bootstrapping code. 

## How does Uno work?

Uno provides a set of APIs that use class and property names compatible with Windows UWP, while allowing those classes to inherit from the primitive layout container of the platform, in the case of the XAML APIs.

For instance, `Windows.UI.Xaml.Controls.StackPanel` directly inherits from a `ViewGroup` on Android and from `UIView` on iOS. 

The native layout system for inner elements is then overridden with a XAML-compatible layout system, using the standard XAML Measure and Arrange passes. This means that a StackPanel will use the exact same layout strategies on all platforms, and will, therefore, look the same on-screen.

On Windows platforms, Uno.UI is not present and the XAML-layout files are left untouched. On Xamarin-compatible platforms, the XAML files are processed at compile time to generate non-conditional code that will be executed as-is at runtime on the device. This means that there is no runtime parsing of XAML, which makes the UI-tree creation particularly efficient.

Uno.UI also provides ways to have platform-specific markup in XAML files, which allows for a simple file tree while adjusting the UI for each platform.

## Want more information about Uno?

Check out the Uno website for more information and support documents here:
https://platform.uno/support/

