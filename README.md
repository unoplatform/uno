# Uno Platform - Build Mobile, Desktop and WebAssembly apps with C# and XAML. Today.
[![Gitter](https://badges.gitter.im/uno-platform/Lobby.svg)](https://gitter.im/uno-platform/Lobby?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge) [![All Contributors](https://img.shields.io/badge/all_contributors-4-orange.svg?style=flat-square)](#contributors)

# What is the Uno Platform

The Uno Platform is a Universal Windows Platform Bridge that allows UWP-based code (C# and XAML) to run on iOS, Android, and WebAssembly. It provides the full definitions of the UWP Windows 10 October 2018 Update (17763), and the implementation of a growing number of parts of the UWP API, such as **Windows.UI.Xaml**, to enable UWP applications to run on these platforms.

Use the UWP tooling from Windows in [Visual Studio](https://www.visualstudio.com/), such as [XAML Edit and Continue](https://blogs.msdn.microsoft.com/visualstudio/2016/04/06/ui-development-made-easier-with-xaml-edit-continue/) and [C# Edit and Continue](https://docs.microsoft.com/en-us/visualstudio/debugger/how-to-use-edit-and-continue-csharp), build your application as much as possible on Windows, then validate that your application runs on iOS, Android and WebAssembly.

Visit [our documentation](doc/articles/intro.md) for more details.

# Getting Started

## Prerequisites
* [**Visual Studio 2017 15.5 or later**](https://visualstudio.microsoft.com/), with:
    * **Universal Windows Platform component** installed

	* **Xamarin component** installed (for Android and iOS development)

    * **ASP.NET/web component** installed, along with .NET Core 2.2 (for WASM development)

To easily create a multi-platform application:
* Install the [Uno Solution Template Visual Studio Extension](https://marketplace.visualstudio.com/items?itemName=nventivecorp.uno-platform-addin)
* Create a new C# solution using the **Cross-Platform App (Uno Platform)** template, from Visual Studio's **Start Page**.

See the complete [Getting Started guide](https://platform.uno/docs/articles/get-started.html) for more information.

For a larger example and features demo:
* Visit the [Uno Gallery and Playground](https://github.com/nventive/Uno.Playground) repository
* Try the [WebAssembly Uno Playground](https://playground.platform.uno) live in your browser

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
    * [ReactiveUI Official](https://github.com/reactiveui/ReactiveUI/pull/2067)
    * [WindowsStateTriggers](https://github.com/nventive/Uno.WindowsStateTriggers)
    * [Xamarin.Forms for UWP](https://github.com/nventive/Uno.Xamarin.Forms), [NuGet](https://www.nuget.org/packages/ReactiveUI.Uno)
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

# Live WebAssembly Apps

Here's a list of live apps made with the Uno Platform for WebAssembly.

* The [Uno Platform Playground](https://playground.platform.uno) ([Source](https://github.com/nventive/Uno.Playground))
* The [Uno Calculator](https://calculator.platform.uno), a simple yet powerful iOS/Android/WebAssembly C# port of the calculator that ships with Windows ([Source](https://github.com/unoplatform/calculator)). Also try the [pink theme](https://calculator.platform.uno/?theme=pink), the [iOS version](https://apps.apple.com/app/id1464736591) or the [Android version](https://play.google.com/store/apps/details?id=uno.platform.calculator).
* The [Xaml Controls Gallery](https://xamlcontrolsgallery.platform.uno/) ([Source](https://github.com/nventive/Uno.Xaml-Controls-Gallery))
* [SkiaSharp fork for the Uno Platform](https://skiasharp-wasm.platform.uno/), Skia is a cross-platform 2D graphics API for .NET platforms based on Google's Skia Graphics Library ([Source](https://github.com/unoplatform/Uno.SkiaSharp))
* The [Uno.WindowsCommunityToolkit](https://windowstoolkit-wasm.platform.uno/), ([Source](https://github.com/nventive/Uno.WindowsCommunityToolkit))
* The [Uno.Lottie](https://lottie.platform.uno/), a sample that uses the [AnimatedVisualPlayer](https://docs.microsoft.com/en-us/uwp/api/microsoft.ui.xaml.controls.animatedvisualplayer) ([Source](https://github.com/nventive/Uno.LottieSample))
* The [Uno.RoslynQuoter](http://roslynquoter-wasm.platform.uno/), a [Roslyn](https://github.com/dotnet/roslyn) based C# analysis tool ([Source](https://github.com/nventive/Uno.RoslynQuoter))
* The [Uno.BikeSharing360 App](http://bikerider-wasm.platform.uno/), a Xamarin.Forms app running on top of Uno for WebAssembly ([Source](https://github.com/nventive/Uno.BikeSharing360_MobileApps))
* The [Uno.WindowsStateTriggers App](http://winstatetriggers-wasm.platform.uno/), a demo of the [Morten's WindowsStateTriggers](https://github.com/dotMorten/WindowsStateTriggers) ([Source](https://github.com/nventive/Uno.WindowsStateTriggers))
* The [SQLite + Entity Framework Core App](http://sqliteefcore-wasm.platform.uno), a demo of the combination of [Roslyn](https://github.com/dotnet/roslyn), [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/), [SQLite](https://github.com/nventive/Uno.SQLitePCLRaw.Wasm) and the Uno Platform to manipulate an in-browser database.
* The [Uno.WebSockets App](https://websockets-wasm.platform.uno), a demo of System.Net.WebSocket running from WebAssembly ([Source](https://github.com/nventive/Uno.Wasm.WebSockets))
* A [mono-wasm AOT RayTracer](https://raytracer-mono-aot.platform.uno/)

Let us know if you've made your app publicly available, we'll list it here!

# Have questions? Feature requests? Issues?

Make sure to visit our [FAQ](doc/articles/faq.md), [StackOverflow](https://stackoverflow.com/questions/tagged/uno-platform), [create an issue](https://github.com/nventive/Uno/issues) or [visit our gitter](https://gitter.im/uno-platform/Lobby).

# Contributing

There are many ways that you can contribute to the Uno Platform, as the UWP API is
pretty large! Read our [contributing guide](CONTRIBUTING.md) to learn about our development process and how to propose bug fixes and improvements.

# Contributors

Thanks goes to these wonderful people ([emoji key](https://allcontributors.org/docs/en/emoji-key)):

<!-- ALL-CONTRIBUTORS-LIST:START - Do not remove or modify this section -->
<!-- prettier-ignore -->
<table><tr><td align="center"><a href="https://github.com/jeromelaban"><img src="https://avatars0.githubusercontent.com/u/5839577?v=4" width="100px;" alt="Jérôme Laban"/><br /><sub><b>Jérôme Laban</b></sub></a><br /><a href="https://github.com/unoplatform/uno/commits?author=jeromelaban" title="Code">💻</a> <a href="#content-jeromelaban" title="Content">🖋</a> <a href="https://github.com/unoplatform/uno/commits?author=jeromelaban" title="Documentation">📖</a> <a href="#example-jeromelaban" title="Examples">💡</a> <a href="#maintenance-jeromelaban" title="Maintenance">🚧</a> <a href="#infra-jeromelaban" title="Infrastructure (Hosting, Build-Tools, etc)">🚇</a> <a href="#ideas-jeromelaban" title="Ideas, Planning, & Feedback">🤔</a> <a href="#review-jeromelaban" title="Reviewed Pull Requests">👀</a> <a href="https://github.com/unoplatform/uno/commits?author=jeromelaban" title="Tests">⚠️</a> <a href="#projectManagement-jeromelaban" title="Project Management">📆</a></td><td align="center"><a href="http://www.dissolutegames.com/"><img src="https://avatars0.githubusercontent.com/u/8270914?v=4" width="100px;" alt="David Oliver"/><br /><sub><b>David Oliver</b></sub></a><br /><a href="#blog-davidjohnoliver" title="Blogposts">📝</a></td><td align="center"><a href="http://mzikmund.com"><img src="https://avatars3.githubusercontent.com/u/1075116?v=4" width="100px;" alt="Martin Zikmund"/><br /><sub><b>Martin Zikmund</b></sub></a><br /><a href="https://github.com/unoplatform/uno/commits?author=MartinZikmund" title="Code">💻</a> <a href="https://github.com/unoplatform/uno/commits?author=MartinZikmund" title="Tests">⚠️</a> <a href="#review-MartinZikmund" title="Reviewed Pull Requests">👀</a> <a href="https://github.com/unoplatform/uno/issues?q=author%3AMartinZikmund" title="Bug reports">🐛</a></td><td align="center"><a href="https://www.ghuntley.com/now"><img src="https://avatars0.githubusercontent.com/u/127353?v=4" width="100px;" alt="Geoffrey Huntley"/><br /><sub><b>Geoffrey Huntley</b></sub></a><br /><a href="#question-ghuntley" title="Answering Questions">💬</a> <a href="https://github.com/unoplatform/uno/commits?author=ghuntley" title="Documentation">📖</a> <a href="#maintenance-ghuntley" title="Maintenance">🚧</a> <a href="https://github.com/unoplatform/uno/commits?author=ghuntley" title="Code">💻</a> <a href="https://github.com/unoplatform/uno/commits?author=ghuntley" title="Tests">⚠️</a> <a href="#tutorial-ghuntley" title="Tutorials">✅</a> <a href="#review-ghuntley" title="Reviewed Pull Requests">👀</a></td></tr></table>

<!-- ALL-CONTRIBUTORS-LIST:END -->
