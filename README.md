# Uno Platform - The UWP Bridge for iOS, Android and WebAssembly

[![Gitter](https://badges.gitter.im/uno-platform/Lobby.svg)](https://gitter.im/uno-platform/Lobby?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge)

# What is the Uno Platform

The Uno Platform is an Universal Windows Platform Bridge to allow UWP based code to run on iOS, Android, and WebAssembly. It provides the full definitions of the UWP Spring Creators Update (17134), and the implementation of growing number parts of the UWP API, such as **Windows.UI.Xaml**, to enable applications to run on these platforms.

Use the UWP tooling from Windows in [Visual Studio](https://www.visualstudio.com/), such as [XAML Edit and Continue](https://blogs.msdn.microsoft.com/visualstudio/2016/04/06/ui-development-made-easier-with-xaml-edit-continue/) and [C# Edit and Continue](https://docs.microsoft.com/en-us/visualstudio/debugger/how-to-use-edit-and-continue-csharp), build your application as much as possible on Windows, then validate that your application runs on iOS, Android and WebAssembly.

Visit [our documentation](doc/index.md) for more details.

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
    * [MVVM Light Toolkit](http://www.mvvmlight.net/)
    * Microsoft XAML Behaviors
    * [Prism](https://prismlibrary.github.io/) (soon)[<sup>^</sup>](https://github.com/nventive/Uno/issues/60#issuecomment-400278037)
    * [MVVMCross](https://www.mvvmcross.com/) (soon)
    * [ReactiveUI](https://reactiveui.net/) (soon)
    * Any UWP project
* Responsive Design
    * [Visual State Manager](https://docs.microsoft.com/en-us/uwp/api/Windows.UI.Xaml.VisualStateManager)
    * [State Triggers](https://blogs.msdn.microsoft.com/mvpawardprogram/2017/02/07/state-triggers-uwp-apps/)
    * [Adaptive Triggers](https://docs.microsoft.com/en-us/uwp/api/Windows.UI.Xaml.AdaptiveTrigger)
* Platform Specific 
    * Native controls and properties via [conditional XAML](doc/articles/using-uno-ui.md)
    * Any of the existing Xamarin iOS/Android libraries available

# Getting Started
To get started with Uno and build your first Uno app check out the [QuickStart repository](https://github.com/nventive/Uno.QuickStart).

For a larger example and features demo:
* Visit the [Uno Gallery and Playground](https://github.com/nventive/Uno.Playground) repository
* Try the [WebAssembly Uno Playground](https://playground.platform.uno) live in your browser

# Contributing

We're getting started, but there are many ways that you can contribute to the Uno Platform, as the UWP api is pretty large! Read our [contributing guide](CONTRIBUTING.md) to learn about our development process and how to propose bug fixes and improvements.
