<h1 align=center>
 <img align=center width="25%" src="https://raw.githubusercontent.com/unoplatform/styleguide/master/logo/uno-platform-logo-with-text.png" />
</h1>


## Pixel-Perfect. Multi-Platform. C# & Windows XAML. Today.

[![Open Uno in Gitpod](https://gitpod.io/button/open-in-gitpod.svg)](https://gitpod.io/#https://github.com/unoplatform/uno) 
[![Azure DevOps](https://img.shields.io/azure-devops/build/uno-platform/1dd81cbd-cb35-41de-a570-b0df3571a196/5/master?label=master)](https://uno-platform.visualstudio.com/Uno%20Platform/_build?definitionId=5)
[![Twitter Followers](https://img.shields.io/twitter/follow/unoplatform?label=follow%20%40unoplatform&style=flat)](https://twitter.com/unoplatform)
[![GitHub Stars](https://img.shields.io/github/stars/unoplatform/uno?label=github%20stars)](https://github.com/unoplatform/uno/stargazers/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/uno.ui.svg)](https://www.nuget.org/packages/uno.ui)
[![All Contributors](https://img.shields.io/github/contributors/unoplatform/uno.svg)](https://GitHub.com/unoplatform/uno/graphs/contributors)
[![PRs Welcome](https://img.shields.io/badge/PRs-welcome-brightgreen.svg?style=flat-square)](https://github.com/unoplatform/uno/blob/master/CONTRIBUTING.md)

# What is the Uno Platform?
The Uno Platform is an Open-source platform for building single codebase native mobile, web, desktop, and embedded apps quickly.

It allows C# and WinUI XAML and/or C# code to run on all target platforms while allowing you control of every pixel. It comes with support for Fluent, Material, and Cupertino design systems out of the box. Uno Platform implements a growing number of the WinRT and WinUI APIs, such as **Windows.UI.Xaml**, to enable WinUI applications to run on all platforms with native performance. 

Use the WinUI tooling from Windows in [Visual Studio](https://www.visualstudio.com/), such as [XAML Hot Reload](https://learn.microsoft.com/visualstudio/xaml-tools/xaml-hot-reload?view=vs-2019) and [C# Hot Reload](https://learn.microsoft.com/visualstudio/debugger/hot-reload), build your application as much as possible on Windows, then validate that your application runs on iOS, Android, macOS, and WebAssembly.

Visit [our documentation](doc/articles/intro.md) for more details.

# Getting Started

See the complete [Getting Started](https://platform.uno/docs/articles/get-started.html) guides for starting with Visual Studio, Visual Studio Code, or JetBrains Rider.

For a larger example and features demo:
* Visit the [Uno Gallery](https://github.com/unoplatform/uno.gallery) repository.
* Try the [WebAssembly Uno Playground](https://playground.platform.uno) live in your browser.

# Uno Platform Features
* Supported platforms:
    * Windows 10 and Windows 11
    * Windows 7 (via Skia Desktop)
    * iOS, MacOS (Catalyst) and Android (via [.NET](https://dotnet.microsoft.com/))
    * WebAssembly through the [.NET Runtime WebAssembly SDK](https://github.com/dotnet/runtime/tree/main/src/mono/wasm)
    * Linux (via Skia Desktop with X11 and FrameBuffer)
    * macOS (via Skia Desktop)
* Dev loop:
    * Develop on Windows first using Visual Studio
    * [XAML Hot Reload](https://blogs.msdn.microsoft.com/visualstudio/2016/04/06/ui-development-made-easier-with-xaml-edit-continue/) for live XAML edition on each keystroke
    * [C# Hot Reload](https://learn.microsoft.com/visualstudio/debugger/hot-reload) on Windows (VS2022), Linux and macOS (VS Code)
    * Validate on other platforms as late as possible
    * Develop in VS Code, Codespaces, or GitPod
    * XAML and/or C# Hot Reload for WebAssembly, Linux, iOS and Android
    * [Uno.UITest](https://github.com/unoplatform/Uno.UITest), a library to create Cross-Platform UI Tests for WebAssembly, iOS, and Android.
* Cross Platform Controls:
    * [Control Templating](https://learn.microsoft.com/windows/uwp/design/controls-and-patterns/control-templates)
    * [Data Templating](https://code.msdn.microsoft.com/Data-Binding-in-UWP-b5c98114)
    * [Styling](https://learn.microsoft.com/windows/uwp/design/controls-and-patterns/xaml-styles)
    * [Rich Animations](https://learn.microsoft.com/windows/uwp/design/motion/xaml-animation)
* UWP/WinUI Code Support:
    * [Windows Community Toolkit](https://github.com/CommunityToolkit/Windows)
    * [Windows Community Toolkit (Uno Fork)](https://github.com/unoplatform/uno.WindowsCommunityToolkit)
    * [MVVM Light Toolkit](https://github.com/unoplatform/uno.mvvmlight)
    * [Microsoft XAML Behaviors](https://github.com/unoplatform/uno.XamlBehaviors)
    * [Prism](https://github.com/prismlibrary/prism)
    * [SkiaSharp](https://github.com/mono/SkiaSharp)
    * [SkiaSharp.Extended](https://github.com/mono/SkiaSharp.Extended)
    * [MVVMCross](https://www.mvvmcross.com/) (soon)
    * [ReactiveUI Official](https://github.com/reactiveui/ReactiveUI/pull/2067)
    * [WindowsStateTriggers](https://github.com/unoplatform/uno.WindowsStateTriggers)
    * [Xamarin.Forms for UWP](https://github.com/unoplatform/uno.Xamarin.Forms), [NuGet](https://www.nuget.org/packages/ReactiveUI.Uno)
    * [Rx.NET](https://github.com/reactiveui/Reactive.Wasm)
    * [ColorCode-Universal](https://github.com/unoplatform/uno.ColorCode-Universal)
    * [LibVLCSharp](https://github.com/videolan/libvlcsharp)
    * [MapsUI](https://github.com/Mapsui/Mapsui)
    * [LiveCharts](https://github.com/beto-rodriguez/LiveCharts2)
    * Any UWP project
* Responsive Design:
    * [Visual State Manager](https://learn.microsoft.com/uwp/api/Windows.UI.Xaml.VisualStateManager)
    * [State Triggers](https://blogs.msdn.microsoft.com/mvpawardprogram/2017/02/07/state-triggers-uwp-apps/)
    * [Adaptive Triggers](https://learn.microsoft.com/uwp/api/Windows.UI.Xaml.AdaptiveTrigger)
* Platform Specific:
    * Native controls and properties via [conditional XAML](doc/articles/platform-specific-xaml.md)
    * Any of the existing Xamarin iOS/Android libraries available
* Xamarin.Forms Renderers:
    * [Uno Platform WebAssembly Renderers for Xamarin.Forms](https://github.com/unoplatform/Uno.Xamarin.Forms.Platform)

# Live WebAssembly Apps

Here's a list of live apps made with the Uno Platform for WebAssembly.

* The [Uno Platform Playground](https://playground.platform.uno) ([Source](https://github.com/unoplatform/uno.Playground)).
* The [Uno Gallery](https://gallery.platform.uno) demonstrates the use of Fluent and Material guidelines.
* The [NuGet Package Explorer](https://nuget.info) ([Source](https://github.com/NuGetPackageExplorer/NuGetPackageExplorer)).
* The [Uno Calculator](https://calculator.platform.uno), a simple yet powerful iOS/Android/WebAssembly C# port of the calculator that ships with Windows ([Source](https://github.com/unoplatform/calculator)). Also try the [iOS version](https://apps.apple.com/app/id1464736591),  the [Android version](https://play.google.com/store/apps/details?id=uno.platform.calculator) and [Linux version](https://snapcraft.io/uno-calculator).
* The [Community Toolkit Labs App](https://toolkitlabs.dev/)
* [SkiaSharp fork for the Uno Platform](https://skiasharp-wasm.platform.uno/), Skia is a cross-platform 2D graphics API for .NET platforms based on Google's Skia Graphics Library ([Source](https://github.com/unoplatform/Uno.SkiaSharp)).
* The [Uno.WindowsCommunityToolkit](https://windowstoolkit-wasm.platform.uno/) ([Source](https://github.com/unoplatform/uno.WindowsCommunityToolkit)).
* The [Uno.Lottie](https://lottie.platform.uno/), a sample that uses the [AnimatedVisualPlayer](https://learn.microsoft.com/uwp/api/microsoft.ui.xaml.controls.animatedvisualplayer) ([Source](https://github.com/unoplatform/uno.LottieSample)).
* The [Uno.RoslynQuoter](https://roslynquoter-wasm.platform.uno/), a [Roslyn](https://github.com/dotnet/roslyn) based C# analysis tool ([Source](https://github.com/unoplatform/uno.RoslynQuoter)).
* The [Uno.BikeSharing360 App](http://bikerider-wasm.platform.uno/), a Xamarin.Forms app running on top of Uno for WebAssembly ([Source](https://github.com/unoplatform/uno.BikeSharing360_MobileApps)).
* The [Uno.WindowsStateTriggers App](http://winstatetriggers-wasm.platform.uno/), a demo of the [Morten's WindowsStateTriggers](https://github.com/dotMorten/WindowsStateTriggers) ([Source](https://github.com/unoplatform/uno.WindowsStateTriggers)).
* The [SQLite + Entity Framework Core App](https://sqliteefcore-wasm.platform.uno), a demo of the combination of [Roslyn](https://github.com/dotnet/roslyn), [Entity Framework Core](https://learn.microsoft.com/ef/core/), [SQLite](https://github.com/unoplatform/uno.SQLitePCLRaw.Wasm) and the Uno Platform to manipulate an in-browser database.
* The [Uno.WebSockets App](https://websockets-wasm.platform.uno), a demo of System.Net.WebSocket running from WebAssembly ([Source](https://github.com/unoplatform/uno.Wasm.WebSockets)).
* A [WebAssembly AOT RayTracer](https://raytracer-mono-aot.platform.uno/).
* The [Xaml Controls Gallery](https://xamlcontrolsgallery.platform.uno/) ([Source](https://github.com/unoplatform/uno.Xaml-Controls-Gallery)).

Let us know if you've made your app publicly available, we'll list it here!

# Have questions? Feature requests? Issues?

Make sure to visit our [FAQ](doc/articles/faq.md), [create an issue](https://github.com/unoplatform/uno/issues), [open a GitHub Discussion](https://github.com/unoplatform/uno/discussions) or visit our [Discord Server](https://platform.uno/uno-discord) - where our engineering team and community will be able to help you.

# Contributing

There are many ways that you can contribute to the Uno Platform, as the WinRT and WinUI APIs are pretty large! Read our [contributing guide](CONTRIBUTING.md) to learn about our development process and how to propose bug fixes and improvements. Come visit us on [Discord](https://platform.uno/uno-discord) for help on how to contribute!

Contribute to Uno in your browser using [GitPod.io](https://gitpod.io), follow [our guide here](doc/articles/features/working-with-gitpod.md).

 [![Open in Gitpod](https://gitpod.io/button/open-in-gitpod.svg)](https://gitpod.io/#https://github.com/unoplatform/uno)

# Contributors
Thanks go to these wonderful people (List made with [contrib.rocks](https://contrib.rocks)):

[![Uno Platform Contributors](https://contrib.rocks/image?repo=unoplatform/uno&max=500)](https://github.com/unoplatform/uno/graphs/contributors)

💖 Thank you.
