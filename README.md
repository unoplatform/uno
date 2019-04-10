# Uno Platform - The UWP Bridge for iOS, Android and WebAssembly

[![Gitter](https://badges.gitter.im/uno-platform/Lobby.svg)](https://gitter.im/uno-platform/Lobby?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge)

# What is the Uno Platform

The Uno Platform is an Universal Windows Platform Bridge to allow UWP based code to run on iOS, Android, and WebAssembly. It provides the full definitions of the UWP Windows 10 October 2018 Update (17763), and the implementation of growing number parts of the UWP API, such as **Windows.UI.Xaml**, to enable applications to run on these platforms.

Use the UWP tooling from Windows in [Visual Studio](https://www.visualstudio.com/), such as [XAML Edit and Continue](https://blogs.msdn.microsoft.com/visualstudio/2016/04/06/ui-development-made-easier-with-xaml-edit-continue/) and [C# Edit and Continue](https://docs.microsoft.com/en-us/visualstudio/debugger/how-to-use-edit-and-continue-csharp), build your application as much as possible on Windows, then validate that your application runs on iOS, Android and WebAssembly.

Visit [our documentation](doc/index.md) for more details.

# Live WebAssembly Apps

Here's a list of live apps made with the Uno Platform for WebAssembly.

* The [Uno Platform Playground](https://playground.platform.uno) ([Source](https://github.com/nventive/Uno.Playground))
* The [Xaml Controls Gallery](https://xamlcontrolsgallery.platform.uno/) ([Source](https://github.com/nventive/Uno.Xaml-Controls-Gallery))
* The [Uno.WindowsCommunityToolkit](https://windowstoolkit-wasm.platform.uno/), ([Source](https://github.com/nventive/Uno.WindowsCommunityToolkit))
* The [Uno.Lottie](https://lottie.platform.uno/), a sample that uses the [AnimatedVisualPlayer](https://docs.microsoft.com/en-us/uwp/api/microsoft.ui.xaml.controls.animatedvisualplayer) ([Source](https://github.com/nventive/Uno.LottieSample))
* The [Uno.RoslynQuoter](http://roslynquoter-wasm.platform.uno/), a [Roslyn](https://github.com/dotnet/roslyn) based C# analysis tool ([Source](https://github.com/nventive/Uno.RoslynQuoter))
* The [Uno.BikeSharing360 App](http://bikerider-wasm.platform.uno/), a Xamarin.Forms app running on top of Uno for WebAssembly ([Source](https://github.com/nventive/Uno.BikeSharing360_MobileApps))
* The [Uno.WindowsStateTriggers App](http://winstatetriggers-wasm.platform.uno/), a demo of the [Morten's WindowsStateTriggers](https://github.com/dotMorten/WindowsStateTriggers) ([Source](https://github.com/nventive/Uno.WindowsStateTriggers))
* The [SQLite + Entity Framework Core App](http://sqliteefcore-wasm.platform.uno), a demo of the combination of [Roslyn](https://github.com/dotnet/roslyn), [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/), [SQLite](https://github.com/nventive/Uno.SQLitePCLRaw.Wasm) and the Uno Platform to manipulate an in-browser database.
* The [Uno.WebSockets App](https://websockets-wasm.platform.uno), a demo of System.Net.WebSocket running from WebAssembly ([Source](https://github.com/nventive/Uno.Wasm.WebSockets))
* A [mono-wasm AOT RayTracer](https://raytracer-mono-aot.platform.uno/)

Let us know if you've made your app publicly available, we'll list it here!

# Uno Features
* Supported platforms:
    * Windows (via the standard UWP Toolkit)
    * iOS and Android (via [Xamarin](https://www.visualstudio.com/xamarin/))
    * WebAssembly through the [Mono Wasm SDK](https://github.com/mono/mono/blob/master/sdks/wasm/README.md)
* Dev loop
    * Develop on Windows first using Visual Studio
    * [XAML Edit and Continue](https://blogs.msdn.microsoft.com/visualstudio/2016/04/06/ui-development-made-easier-with-xaml-edit-continue/) for live XAML edition on each key stroke
    * [C# Edit and Continue](https://docs.microsoft.com/en-us/visualstudio/debugger/how-to-use-edit-and-continue-csharp)
    * Validate on other platforms as late as possible
* Cross Platform Controls
    * [Control Templating](https://docs.microsoft.com/en-us/windows/uwp/design/controls-and-patterns/control-templates)
    * [Data Templating](https://code.msdn.microsoft.com/Data-Binding-in-UWP-b5c98114)
    * [Styling](https://docs.microsoft.com/en-us/windows/uwp/design/controls-and-patterns/xaml-styles)
    * [Rich Animations](https://docs.microsoft.com/en-us/windows/uwp/design/motion/xaml-animation)
* UWP Code Support
    * [Windows Community Toolkit](https://github.com/nventive/Uno.WindowsCommunityToolkit)
    * [MVVM Light Toolkit](https://github.com/nventive/uno.mvvmlight)
    * [Microsoft XAML Behaviors](https://github.com/nventive/Uno.XamlBehaviors)
    * [Prism](https://github.com/nventive/Uno.Prism)
    * [MVVMCross](https://www.mvvmcross.com/) (soon)
    * [ReactiveUI](https://github.com/nventive/Uno.ReactiveUI)
    * [WindowsStateTriggers](https://github.com/nventive/Uno.WindowsStateTriggers)
    * [Xamarin.Forms for UWP](https://github.com/nventive/Uno.Xamarin.Forms)
    * [Rx.NET](https://github.com/nventive/Uno.Rx.NET)
    * [ColorCode-Universal](https://github.com/nventive/Uno.ColorCode-Universal)
    * Any UWP project
* Responsive Design
    * [Visual State Manager](https://docs.microsoft.com/en-us/uwp/api/Windows.UI.Xaml.VisualStateManager)
    * [State Triggers](https://blogs.msdn.microsoft.com/mvpawardprogram/2017/02/07/state-triggers-uwp-apps/)
    * [Adaptive Triggers](https://docs.microsoft.com/en-us/uwp/api/Windows.UI.Xaml.AdaptiveTrigger)
* Platform Specific 
    * Native controls and properties via [conditional XAML](doc/articles/using-uno-ui.md#supporting-multiple-platforms-in-xaml-files)
    * Any of the existing Xamarin iOS/Android libraries available

# Getting Started
To get started with Uno and build your first Uno app check out the [QuickStart repository](https://github.com/nventive/Uno.QuickStart).

For a larger example and features demo:
* Visit the [Uno Gallery and Playground](https://github.com/nventive/Uno.Playground) repository
* Try the [WebAssembly Uno Playground](https://playground.platform.uno) live in your browser

# Have questions? Feature requests? Issues?

Make sure to visit our [FAQ](doc/articles/faq.md), [StackOverflow](https://stackoverflow.com/questions/tagged/uno-platform), [create an issue](https://github.com/nventive/Uno/issues) or [visit our gitter](https://gitter.im/uno-platform/Lobby).

# Contributing

We're getting started, but there are many ways that you can contribute to the Uno Platform, as the UWP api is
pretty large! Read our [contributing guide](CONTRIBUTING.md) to learn about our development process and how to propose bug fixes and improvements.
