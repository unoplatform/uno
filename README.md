<h1 align=center>
 <img align=center width="25%" src="https://raw.githubusercontent.com/unoplatform/styleguide/master/logo/uno-platform-logo-with-text.png" />
</h1>


## Build Mobile, Desktop and WebAssembly apps with C# and XAML. Today.

[![Open Uno in Gitpod](https://gitpod.io/button/open-in-gitpod.svg)](https://gitpod.io/#https://github.com/unoplatform/uno) 

[![Azure DevOps](https://img.shields.io/azure-devops/build/uno-platform/1dd81cbd-cb35-41de-a570-b0df3571a196/5/master?label=master)](https://uno-platform.visualstudio.com/Uno%20Platform/_build?definitionId=5)
[![Azure DevOps](https://img.shields.io/azure-devops/build/uno-platform/1dd81cbd-cb35-41de-a570-b0df3571a196/5/release/beta/Batman?label=release/beta/Batman)](https://uno-platform.visualstudio.com/Uno%20Platform/_build?definitionId=5)
[![Azure DevOps](https://img.shields.io/azure-devops/build/uno-platform/1dd81cbd-cb35-41de-a570-b0df3571a196/5/release/stable/Batman?label=release/stable/Batman)](https://uno-platform.visualstudio.com/Uno%20Platform/_build?definitionId=5)
[![Dependabot Status](https://api.dependabot.com/badges/status?host=github&repo=unoplatform/uno)](https://dependabot.com)
[![Gitter](https://badges.gitter.im/uno-platform/Lobby.svg)](https://gitter.im/uno-platform/Lobby?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge)
[![Twitter Followers](https://img.shields.io/twitter/follow/unoplatform?label=follow%20%40unoplatform&style=flat)](https://twitter.com/unoplatform)
[![GitHub Stars](https://img.shields.io/github/stars/unoplatform/uno?label=github%20stars)](https://github.com/unoplatform/uno/stargazers/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/uno.ui.svg)](https://www.nuget.org/packages/uno.ui)
[![All Contributors](https://img.shields.io/badge/all_contributors-61-orange.svg?style=flat-square)](#contributors)
[![PRs Welcome](https://img.shields.io/badge/PRs-welcome-brightgreen.svg?style=flat-square)](https://github.com/unoplatform/uno/blob/master/CONTRIBUTING.md)

# What is the Uno Platform

The Uno Platform (Pronounced 'Oono' or 'Ouno') is a Universal Windows Platform Bridge that allows UWP-based code (C# and XAML) to run on iOS, Android, and WebAssembly. It provides the full definitions of the UWP Windows 10 October 2018 Update (17763), and the implementation of a growing number of parts of the UWP API, such as **Windows.UI.Xaml**, to enable UWP applications to run on these platforms.

Use the UWP tooling from Windows in [Visual Studio](https://www.visualstudio.com/), such as [XAML Edit and Continue](https://blogs.msdn.microsoft.com/visualstudio/2016/04/06/ui-development-made-easier-with-xaml-edit-continue/) and [C# Edit and Continue](https://docs.microsoft.com/en-us/visualstudio/debugger/how-to-use-edit-and-continue-csharp), build your application as much as possible on Windows, then validate that your application runs on iOS, Android and WebAssembly.

Visit [our documentation](doc/articles/intro.md) for more details.

# Getting Started

## Prerequisites
* [**Visual Studio 2017 15.5 or later**](https://visualstudio.microsoft.com/) with:
    * **Universal Windows Platform component** installed.

	* **Xamarin component** installed (for Android and iOS development).

    * **ASP.NET/web component** installed, along with .NET Core 2.2 (for WASM development).

To easily create a multi-platform application:
* Install the [Uno Solution Template Visual Studio Extension](https://marketplace.visualstudio.com/items?itemName=nventivecorp.uno-platform-addin).
* Create a new C# solution using the **Cross-Platform App (Uno Platform)** template, from Visual Studio's **Start Page**.

See the complete [Getting Started](https://platform.uno/docs/articles/get-started.html) guide for more information.

For a larger example and features demo:
* Visit the [Uno Gallery and Playground](https://github.com/unoplatform/uno.Playground) repository.
* Try the [WebAssembly Uno Playground](https://playground.platform.uno) live in your browser.

# Uno Features
* Supported platforms:
    * Windows (via the standard UWP Toolkit)
    * iOS and Android (via [Xamarin](https://www.visualstudio.com/xamarin/))
    * WebAssembly through the [Mono Wasm SDK](https://github.com/mono/mono/blob/master/sdks/wasm/README.md)
* Dev loop:
    * Develop on Windows first using Visual Studio
    * [XAML Edit and Continue](https://blogs.msdn.microsoft.com/visualstudio/2016/04/06/ui-development-made-easier-with-xaml-edit-continue/) for live XAML edition on each keystroke
    * [C# Edit and Continue](https://docs.microsoft.com/en-us/visualstudio/debugger/how-to-use-edit-and-continue-csharp)
    * Validate on other platforms as late as possible
    * Experimental XAML Hot Reload for WebAssembly, iOS and Android
    * [Uno.UITest](https://github.com/unoplatform/Uno.UITest), a library to create Cross-Platform UI Tests for WebAssembly, iOS and Android.
* Cross Platform Controls:
    * [Control Templating](https://docs.microsoft.com/en-us/windows/uwp/design/controls-and-patterns/control-templates)
    * [Data Templating](https://code.msdn.microsoft.com/Data-Binding-in-UWP-b5c98114)
    * [Styling](https://docs.microsoft.com/en-us/windows/uwp/design/controls-and-patterns/xaml-styles)
    * [Rich Animations](https://docs.microsoft.com/en-us/windows/uwp/design/motion/xaml-animation)
* UWP Code Support:
    * [Windows Community Toolkit](https://github.com/unoplatform/uno.WindowsCommunityToolkit)
    * [MVVM Light Toolkit](https://github.com/unoplatform/uno.mvvmlight)
    * [Microsoft XAML Behaviors](https://github.com/unoplatform/uno.XamlBehaviors)
    * [Prism](https://github.com/unoplatform/uno.Prism)
    * [SkiaSharp](https://github.com/unoplatform/Uno.SkiaSharp)
    * [SkiaSharp.Extended](https://github.com/unoplatform/Uno.SkiaSharp.Extended)
    * [MVVMCross](https://www.mvvmcross.com/) (soon)
    * [ReactiveUI Official](https://github.com/reactiveui/ReactiveUI/pull/2067)
    * [WindowsStateTriggers](https://github.com/unoplatform/uno.WindowsStateTriggers)
    * [Xamarin.Forms for UWP](https://github.com/unoplatform/uno.Xamarin.Forms), [NuGet](https://www.nuget.org/packages/ReactiveUI.Uno)
    * [Rx.NET](https://github.com/reactiveui/Reactive.Wasm)
    * [ColorCode-Universal](https://github.com/unoplatform/uno.ColorCode-Universal)
    * [LibVLCSharp](https://github.com/videolan/libvlcsharp)
    * Any UWP project
* Responsive Design:
    * [Visual State Manager](https://docs.microsoft.com/en-us/uwp/api/Windows.UI.Xaml.VisualStateManager)
    * [State Triggers](https://blogs.msdn.microsoft.com/mvpawardprogram/2017/02/07/state-triggers-uwp-apps/)
    * [Adaptive Triggers](https://docs.microsoft.com/en-us/uwp/api/Windows.UI.Xaml.AdaptiveTrigger)
* Platform Specific:
    * Native controls and properties via [conditional XAML](doc/articles/using-uno-ui.md#supporting-multiple-platforms-in-xaml-files)
    * Any of the existing Xamarin iOS/Android libraries available
* Xamarin.Forms Renderers:
    * [Uno Platform WebAssembly Renderers for Xamarin.Forms](https://github.com/unoplatform/Uno.Xamarin.Forms.Platform)

# Live WebAssembly Apps

Here's a list of live apps made with the Uno Platform for WebAssembly.

* The [Uno Platform Playground](https://playground.platform.uno) ([Source](https://github.com/unoplatform/uno.Playground)).
* The [Uno Calculator](https://calculator.platform.uno), a simple yet powerful iOS/Android/WebAssembly C# port of the calculator that ships with Windows ([Source](https://github.com/unoplatform/calculator)). Also try the [pink theme](https://calculator.platform.uno/?theme=pink), the [iOS version](https://apps.apple.com/app/id1464736591) or the [Android version](https://play.google.com/store/apps/details?id=uno.platform.calculator).
* The [Xaml Controls Gallery](https://xamlcontrolsgallery.platform.uno/) ([Source](https://github.com/unoplatform/uno.Xaml-Controls-Gallery)).
* [SkiaSharp fork for the Uno Platform](https://skiasharp-wasm.platform.uno/), Skia is a cross-platform 2D graphics API for .NET platforms based on Google's Skia Graphics Library ([Source](https://github.com/unoplatform/Uno.SkiaSharp)).
* The [Uno.WindowsCommunityToolkit](https://windowstoolkit-wasm.platform.uno/) ([Source](https://github.com/unoplatform/uno.WindowsCommunityToolkit)).
* The [Uno.Lottie](https://lottie.platform.uno/), a sample that uses the [AnimatedVisualPlayer](https://docs.microsoft.com/en-us/uwp/api/microsoft.ui.xaml.controls.animatedvisualplayer) ([Source](https://github.com/unoplatform/uno.LottieSample)).
* The [Uno.RoslynQuoter](https://roslynquoter-wasm.platform.uno/), a [Roslyn](https://github.com/dotnet/roslyn) based C# analysis tool ([Source](https://github.com/unoplatform/uno.RoslynQuoter)).
* The [Uno.BikeSharing360 App](http://bikerider-wasm.platform.uno/), a Xamarin.Forms app running on top of Uno for WebAssembly ([Source](https://github.com/unoplatform/uno.BikeSharing360_MobileApps)).
* The [Uno.WindowsStateTriggers App](http://winstatetriggers-wasm.platform.uno/), a demo of the [Morten's WindowsStateTriggers](https://github.com/dotMorten/WindowsStateTriggers) ([Source](https://github.com/unoplatform/uno.WindowsStateTriggers)).
* The [SQLite + Entity Framework Core App](https://sqliteefcore-wasm.platform.uno), a demo of the combination of [Roslyn](https://github.com/dotnet/roslyn), [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/), [SQLite](https://github.com/unoplatform/uno.SQLitePCLRaw.Wasm) and the Uno Platform to manipulate an in-browser database.
* The [Uno.WebSockets App](https://websockets-wasm.platform.uno), a demo of System.Net.WebSocket running from WebAssembly ([Source](https://github.com/unoplatform/uno.Wasm.WebSockets)).
* A [mono-wasm AOT RayTracer](https://raytracer-mono-aot.platform.uno/).

Let us know if you've made your app publicly available, we'll list it here!

# Have questions? Feature requests? Issues?

Make sure to visit our [FAQ](doc/articles/faq.md), [StackOverflow](https://stackoverflow.com/questions/tagged/uno-platform), [create an issue](https://github.com/unoplatform/uno/issues) or [visit our Gitter](https://gitter.im/uno-platform/Lobby).

# Contributing

There are many ways that you can contribute to the Uno Platform, as the UWP API is pretty large! Read our [contributing guide](CONTRIBUTING.md) to learn about our development process and how to propose bug fixes and improvements.

Contribute to Uno in your browser using [GitPod.io](https://gitpod.io), follow [our guide here](doc/articles/features/working-with-gitpod.md).

 [![Open in Gitpod](https://gitpod.io/button/open-in-gitpod.svg)](https://gitpod.io/#https://github.com/unoplatform/uno)

# Contributors

Thanks goes to these wonderful people ([emoji key](https://allcontributors.org/docs/en/emoji-key)):

<!-- ALL-CONTRIBUTORS-LIST:START - Do not remove or modify this section -->
<!-- prettier-ignore -->
<table>
  <tr>
    <td align="center"><a href="https://github.com/jeromelaban"><img src="https://avatars0.githubusercontent.com/u/5839577?v=4" width="100px;" alt="Jérôme Laban"/><br /><sub><b>Jérôme Laban</b></sub></a><br /><a href="https://github.com/unoplatform/uno/commits?author=jeromelaban" title="Code">💻</a> <a href="#content-jeromelaban" title="Content">🖋</a> <a href="https://github.com/unoplatform/uno/commits?author=jeromelaban" title="Documentation">📖</a> <a href="#example-jeromelaban" title="Examples">💡</a> <a href="#maintenance-jeromelaban" title="Maintenance">🚧</a> <a href="#infra-jeromelaban" title="Infrastructure (Hosting, Build-Tools, etc)">🚇</a> <a href="#ideas-jeromelaban" title="Ideas, Planning, & Feedback">🤔</a> <a href="#review-jeromelaban" title="Reviewed Pull Requests">👀</a> <a href="https://github.com/unoplatform/uno/commits?author=jeromelaban" title="Tests">⚠️</a> <a href="#projectManagement-jeromelaban" title="Project Management">📆</a></td>
    <td align="center"><a href="http://www.dissolutegames.com/"><img src="https://avatars0.githubusercontent.com/u/8270914?v=4" width="100px;" alt="David Oliver"/><br /><sub><b>David Oliver</b></sub></a><br /><a href="#blog-davidjohnoliver" title="Blogposts">📝</a> <a href="https://github.com/unoplatform/uno/commits?author=davidjohnoliver" title="Code">💻</a> <a href="#content-davidjohnoliver" title="Content">🖋</a> <a href="https://github.com/unoplatform/uno/commits?author=davidjohnoliver" title="Documentation">📖</a> <a href="#maintenance-davidjohnoliver" title="Maintenance">🚧</a> <a href="#tutorial-davidjohnoliver" title="Tutorials">✅</a> <a href="#review-davidjohnoliver" title="Reviewed Pull Requests">👀</a> <a href="https://github.com/unoplatform/uno/commits?author=davidjohnoliver" title="Tests">⚠️</a> <a href="#example-davidjohnoliver" title="Examples">💡</a></td>
    <td align="center"><a href="http://mzikmund.com"><img src="https://avatars3.githubusercontent.com/u/1075116?v=4" width="100px;" alt="Martin Zikmund"/><br /><sub><b>Martin Zikmund</b></sub></a><br /><a href="https://github.com/unoplatform/uno/commits?author=MartinZikmund" title="Code">💻</a> <a href="https://github.com/unoplatform/uno/commits?author=MartinZikmund" title="Tests">⚠️</a> <a href="#review-MartinZikmund" title="Reviewed Pull Requests">👀</a></td>
    <td align="center"><a href="https://www.ghuntley.com/now"><img src="https://avatars0.githubusercontent.com/u/127353?v=4" width="100px;" alt="Geoffrey Huntley"/><br /><sub><b>Geoffrey Huntley</b></sub></a><br /><a href="#question-ghuntley" title="Answering Questions">💬</a> <a href="https://github.com/unoplatform/uno/commits?author=ghuntley" title="Documentation">📖</a> <a href="#maintenance-ghuntley" title="Maintenance">🚧</a> <a href="https://github.com/unoplatform/uno/commits?author=ghuntley" title="Code">💻</a> <a href="https://github.com/unoplatform/uno/commits?author=ghuntley" title="Tests">⚠️</a> <a href="#tutorial-ghuntley" title="Tutorials">✅</a> <a href="#review-ghuntley" title="Reviewed Pull Requests">👀</a></td>
    <td align="center"><a href="https://www.vogels.com"><img src="https://avatars0.githubusercontent.com/u/47024956?v=4" width="100px;" alt="Patrick Decoster"/><br /><sub><b>Patrick Decoster</b></sub></a><br /><a href="https://github.com/unoplatform/uno/commits?author=pdecostervgls" title="Tests">⚠️</a> <a href="https://github.com/unoplatform/uno/commits?author=pdecostervgls" title="Code">💻</a> <a href="#example-pdecostervgls" title="Examples">💡</a></td>
  </tr>
  <tr>
    <td align="center"><a href="https://github.com/dr1rrb"><img src="https://avatars3.githubusercontent.com/u/8635919?v=4" width="100px;" alt="David"/><br /><sub><b>David</b></sub></a><br /><a href="https://github.com/unoplatform/uno/commits?author=dr1rrb" title="Code">💻</a> <a href="#content-dr1rrb" title="Content">🖋</a> <a href="https://github.com/unoplatform/uno/commits?author=dr1rrb" title="Documentation">📖</a> <a href="#maintenance-dr1rrb" title="Maintenance">🚧</a> <a href="#review-dr1rrb" title="Reviewed Pull Requests">👀</a> <a href="https://github.com/unoplatform/uno/commits?author=dr1rrb" title="Tests">⚠️</a> <a href="#example-dr1rrb" title="Examples">💡</a></td>
    <td align="center"><a href="http://carl.debilly.net/"><img src="https://avatars1.githubusercontent.com/u/4174207?v=4" width="100px;" alt="Carl de Billy"/><br /><sub><b>Carl de Billy</b></sub></a><br /><a href="https://github.com/unoplatform/uno/commits?author=carldebilly" title="Code">💻</a> <a href="#content-carldebilly" title="Content">🖋</a> <a href="https://github.com/unoplatform/uno/commits?author=carldebilly" title="Documentation">📖</a> <a href="#maintenance-carldebilly" title="Maintenance">🚧</a> <a href="#tutorial-carldebilly" title="Tutorials">✅</a> <a href="#review-carldebilly" title="Reviewed Pull Requests">👀</a> <a href="https://github.com/unoplatform/uno/commits?author=carldebilly" title="Tests">⚠️</a> <a href="#example-carldebilly" title="Examples">💡</a></td>
    <td align="center"><a href="https://github.com/vincentcastagna"><img src="https://avatars3.githubusercontent.com/u/15191066?v=4" width="100px;" alt="vincentcastagna"/><br /><sub><b>vincentcastagna</b></sub></a><br /><a href="https://github.com/unoplatform/uno/commits?author=vincentcastagna" title="Code">💻</a> <a href="#example-vincentcastagna" title="Examples">💡</a></td>
    <td align="center"><a href="https://github.com/TopperDEL"><img src="https://avatars2.githubusercontent.com/u/1833242?v=4" width="100px;" alt="TopperDEL"/><br /><sub><b>TopperDEL</b></sub></a><br /><a href="https://github.com/unoplatform/uno/commits?author=TopperDEL" title="Code">💻</a></td>
    <td align="center"><a href="https://github.com/MaxineMheir"><img src="https://avatars1.githubusercontent.com/u/18086278?v=4" width="100px;" alt="Maxine Mheir-El-Saadi"/><br /><sub><b>Maxine Mheir-El-Saadi</b></sub></a><br /><a href="https://github.com/unoplatform/uno/commits?author=MaxineMheir" title="Code">💻</a> <a href="#example-MaxineMheir" title="Examples">💡</a> <a href="https://github.com/unoplatform/uno/commits?author=MaxineMheir" title="Tests">⚠️</a></td>
  </tr>
  <tr>
    <td align="center"><a href="https://github.com/artemious7"><img src="https://avatars2.githubusercontent.com/u/16724889?v=4" width="100px;" alt="Artem"/><br /><sub><b>Artem</b></sub></a><br /><a href="https://github.com/unoplatform/uno/commits?author=artemious7" title="Code">💻</a> <a href="https://github.com/unoplatform/uno/commits?author=artemious7" title="Documentation">📖</a></td>
    <td align="center"><a href="https://github.com/DysnomianC"><img src="https://avatars1.githubusercontent.com/u/20263707?v=4" width="100px;" alt="Dysnomian Charles"/><br /><sub><b>Dysnomian Charles</b></sub></a><br /><a href="https://github.com/unoplatform/uno/commits?author=DysnomianC" title="Code">💻</a></td>
    <td align="center"><a href="https://github.com/jeanplevesque"><img src="https://avatars3.githubusercontent.com/u/39710855?v=4" width="100px;" alt="Jean-Philippe Lévesque"/><br /><sub><b>Jean-Philippe Lévesque</b></sub></a><br /><a href="https://github.com/unoplatform/uno/commits?author=jeanplevesque" title="Code">💻</a></td>
    <td align="center"><a href="https://github.com/Xiaoy312"><img src="https://avatars1.githubusercontent.com/u/2359550?v=4" width="100px;" alt="Xiaotian Gu"/><br /><sub><b>Xiaotian Gu</b></sub></a><br /><a href="https://github.com/unoplatform/uno/commits?author=Xiaoy312" title="Code">💻</a></td>
    <td align="center"><a href="http://www.lhotka.net"><img src="https://avatars1.githubusercontent.com/u/2333134?v=4" width="100px;" alt="Rockford Lhotka"/><br /><sub><b>Rockford Lhotka</b></sub></a><br /><a href="#blog-rockfordlhotka" title="Blogposts">📝</a></td>
  </tr>
  <tr>
    <td align="center"><a href="http://nicksnettravels.builttoroam.com"><img src="https://avatars2.githubusercontent.com/u/1614057?v=4" width="100px;" alt="Nick Randolph"/><br /><sub><b>Nick Randolph</b></sub></a><br /><a href="#blog-nickrandolph" title="Blogposts">📝</a></td>
    <td align="center"><a href="https://opensource.microsoft.com"><img src="https://avatars2.githubusercontent.com/u/6154722?v=4" width="100px;" alt="Microsoft"/><br /><sub><b>Microsoft</b></sub></a><br /><a href="https://github.com/unoplatform/uno/commits?author=microsoft" title="Code">💻</a> <a href="https://github.com/unoplatform/uno/commits?author=microsoft" title="Documentation">📖</a> <a href="#example-microsoft" title="Examples">💡</a> <a href="https://github.com/unoplatform/uno/commits?author=microsoft" title="Tests">⚠️</a> <a href="#infra-microsoft" title="Infrastructure (Hosting, Build-Tools, etc)">🚇</a></td>
    <td align="center"><a href="http://xamarin.com"><img src="https://avatars0.githubusercontent.com/u/790012?v=4" width="100px;" alt="Xamarin"/><br /><sub><b>Xamarin</b></sub></a><br /><a href="https://github.com/unoplatform/uno/commits?author=xamarin" title="Code">💻</a> <a href="https://github.com/unoplatform/uno/commits?author=xamarin" title="Documentation">📖</a> <a href="#example-xamarin" title="Examples">💡</a> <a href="https://github.com/unoplatform/uno/commits?author=xamarin" title="Tests">⚠️</a></td>
    <td align="center"><a href="https://github.com/NicolasChampagne"><img src="https://avatars0.githubusercontent.com/u/49762217?v=4" width="100px;" alt="NicolasChampagne"/><br /><sub><b>NicolasChampagne</b></sub></a><br /><a href="https://github.com/unoplatform/uno/commits?author=NicolasChampagne" title="Code">💻</a></td>
    <td align="center"><a href="https://github.com/agneszitte-nventive"><img src="https://avatars0.githubusercontent.com/u/16295702?v=4" width="100px;" alt="Agnes ZITTE"/><br /><sub><b>Agnes ZITTE</b></sub></a><br /><a href="https://github.com/unoplatform/uno/commits?author=agneszitte-nventive" title="Code">💻</a></td>
  </tr>
  <tr>
    <td align="center"><a href="http://miguelrochefort.com"><img src="https://avatars0.githubusercontent.com/u/1556332?v=4" width="100px;" alt="Miguel Rochefort"/><br /><sub><b>Miguel Rochefort</b></sub></a><br /><a href="https://github.com/unoplatform/uno/commits?author=miguelrochefort" title="Code">💻</a></td>
    <td align="center"><a href="https://github.com/YGuerin"><img src="https://avatars2.githubusercontent.com/u/11750340?v=4" width="100px;" alt="Yohan Guérin"/><br /><sub><b>Yohan Guérin</b></sub></a><br /><a href="https://github.com/unoplatform/uno/commits?author=YGuerin" title="Code">💻</a></td>
    <td align="center"><a href="https://github.com/jcantin-nventive"><img src="https://avatars1.githubusercontent.com/u/43351943?v=4" width="100px;" alt="jcantin-nventive"/><br /><sub><b>jcantin-nventive</b></sub></a><br /><a href="https://github.com/unoplatform/uno/commits?author=jcantin-nventive" title="Code">💻</a></td>
    <td align="center"><a href="https://github.com/MatFillion"><img src="https://avatars0.githubusercontent.com/u/7029537?v=4" width="100px;" alt="Mathieu Fillion"/><br /><sub><b>Mathieu Fillion</b></sub></a><br /><a href="https://github.com/unoplatform/uno/commits?author=MatFillion" title="Code">💻</a> <a href="#maintenance-MatFillion" title="Maintenance">🚧</a></td>
    <td align="center"><a href="http://www.florent-cima.com"><img src="https://avatars0.githubusercontent.com/u/669433?v=4" width="100px;" alt="Florent Cima"/><br /><sub><b>Florent Cima</b></sub></a><br /><a href="https://github.com/unoplatform/uno/commits?author=florentcm" title="Code">💻</a></td>
  </tr>
  <tr>
    <td align="center"><a href="https://github.com/AlexTrepanier"><img src="https://avatars1.githubusercontent.com/u/46451463?v=4" width="100px;" alt="alextrepanier"/><br /><sub><b>alextrepanier</b></sub></a><br /><a href="https://github.com/unoplatform/uno/commits?author=AlexTrepanier" title="Code">💻</a></td>
    <td align="center"><a href="https://github.com/warrenbenyahia"><img src="https://avatars2.githubusercontent.com/u/46033284?v=4" width="100px;" alt="warrenbenyahia"/><br /><sub><b>warrenbenyahia</b></sub></a><br /><a href="https://github.com/unoplatform/uno/commits?author=warrenbenyahia" title="Code">💻</a></td>
    <td align="center"><a href="https://github.com/Batesias"><img src="https://avatars3.githubusercontent.com/u/2448861?v=4" width="100px;" alt="JP"/><br /><sub><b>JP</b></sub></a><br /><a href="https://github.com/unoplatform/uno/commits?author=Batesias" title="Code">💻</a></td>
    <td align="center"><a href="https://github.com/GuillaumeSE"><img src="https://avatars0.githubusercontent.com/u/50678763?v=4" width="100px;" alt="GuillaumeSE"/><br /><sub><b>GuillaumeSE</b></sub></a><br /><a href="https://github.com/unoplatform/uno/commits?author=GuillaumeSE" title="Code">💻</a></td>
    <td align="center"><a href="http://blogs.microsoft.co.il/shimmy/"><img src="https://avatars3.githubusercontent.com/u/2716316?v=4" width="100px;" alt="Shimmy"/><br /><sub><b>Shimmy</b></sub></a><br /><a href="https://github.com/unoplatform/uno/commits?author=weitzhandler" title="Documentation">📖</a></td>
  </tr>
  <tr>
    <td align="center"><a href="https://github.com/gfbriggs"><img src="https://avatars1.githubusercontent.com/u/18409414?v=4" width="100px;" alt="Geoffrey Fielden-Briggs"/><br /><sub><b>Geoffrey Fielden-Briggs</b></sub></a><br /><a href="https://github.com/unoplatform/uno/commits?author=gfbriggs" title="Code">💻</a></td>
    <td align="center"><a href="https://github.com/Massimo37"><img src="https://avatars1.githubusercontent.com/u/36633246?v=4" width="100px;" alt="Massimo Cacchiotti"/><br /><sub><b>Massimo Cacchiotti</b></sub></a><br /><a href="https://github.com/unoplatform/uno/commits?author=Massimo37" title="Code">💻</a></td>
    <td align="center"><a href="https://github.com/rfrappier"><img src="https://avatars2.githubusercontent.com/u/30271212?v=4" width="100px;" alt="rfrappier"/><br /><sub><b>rfrappier</b></sub></a><br /><a href="https://github.com/unoplatform/uno/commits?author=rfrappier" title="Documentation">📖</a></td>
    <td align="center"><a href="http://furkankambay.com/"><img src="https://avatars1.githubusercontent.com/u/8467416?v=4" width="100px;" alt="Furkan Kambay"/><br /><sub><b>Furkan Kambay</b></sub></a><br /><a href="https://github.com/unoplatform/uno/commits?author=FurkanKambay" title="Documentation">📖</a></td>
    <td align="center"><a href="https://github.com/pkar70"><img src="https://avatars2.githubusercontent.com/u/23451507?v=4" width="100px;" alt="pkar70"/><br /><sub><b>pkar70</b></sub></a><br /><a href="https://github.com/unoplatform/uno/commits?author=pkar70" title="Code">💻</a> <a href="https://github.com/unoplatform/uno/commits?author=pkar70" title="Tests">⚠️</a> <a href="https://github.com/unoplatform/uno/commits?author=pkar70" title="Documentation">📖</a></td>
  </tr>
  <tr>
    <td align="center"><a href="https://github.com/hugovk"><img src="https://avatars2.githubusercontent.com/u/1324225?v=4" width="100px;" alt="Hugo van Kemenade"/><br /><sub><b>Hugo van Kemenade</b></sub></a><br /><a href="https://github.com/unoplatform/uno/commits?author=hugovk" title="Documentation">📖</a></td>
    <td align="center"><a href="https://tomercohen.com"><img src="https://avatars0.githubusercontent.com/u/50206?v=4" width="100px;" alt="Tomer Cohen"/><br /><sub><b>Tomer Cohen</b></sub></a><br /><a href="https://github.com/unoplatform/uno/commits?author=tomer" title="Documentation">📖</a></td>
    <td align="center"><a href="https://github.com/SantosAntero"><img src="https://avatars1.githubusercontent.com/u/36671793?v=4" width="100px;" alt="Antero Santos"/><br /><sub><b>Antero Santos</b></sub></a><br /><a href="https://github.com/unoplatform/uno/commits?author=SantosAntero" title="Documentation">📖</a></td>
    <td align="center"><a href="https://zzyzy.github.io/"><img src="https://avatars2.githubusercontent.com/u/6206954?v=4" width="100px;" alt="Zhen Zhi Lee"/><br /><sub><b>Zhen Zhi Lee</b></sub></a><br /><a href="https://github.com/unoplatform/uno/commits?author=zzyzy" title="Code">💻</a></td>
    <td align="center"><a href="https://backendtea.com"><img src="https://avatars1.githubusercontent.com/u/14289961?v=4" width="100px;" alt="Gert de Pagter"/><br /><sub><b>Gert de Pagter</b></sub></a><br /><a href="https://github.com/unoplatform/uno/commits?author=BackEndTea" title="Documentation">📖</a></td>
  </tr>
  <tr>
    <td align="center"><a href="http://www.deveshsingh.ml"><img src="https://avatars3.githubusercontent.com/u/31030254?v=4" width="100px;" alt="Devesh Singh"/><br /><sub><b>Devesh Singh</b></sub></a><br /><a href="https://github.com/unoplatform/uno/commits?author=D3v3sh5ingh" title="Documentation">📖</a></td>
    <td align="center"><a href="https://github.com/RanadeepPolavarapu"><img src="https://avatars1.githubusercontent.com/u/7084995?v=4" width="100px;" alt="RanadeepPolavarapu"/><br /><sub><b>RanadeepPolavarapu</b></sub></a><br /><a href="https://github.com/unoplatform/uno/commits?author=RanadeepPolavarapu" title="Documentation">📖</a></td>
    <td align="center"><a href="http://ujjwalll.github.io"><img src="https://avatars1.githubusercontent.com/u/39565250?v=4" width="100px;" alt="Ujjwal "/><br /><sub><b>Ujjwal </b></sub></a><br /><a href="https://github.com/unoplatform/uno/commits?author=ujjwalll" title="Documentation">📖</a></td>
    <td align="center"><a href="https://github.com/cbaumler"><img src="https://avatars1.githubusercontent.com/u/9060078?v=4" width="100px;" alt="Chris Baumler"/><br /><sub><b>Chris Baumler</b></sub></a><br /><a href="https://github.com/unoplatform/uno/commits?author=cbaumler" title="Documentation">📖</a></td>
    <td align="center"><a href="https://github.com/SnicklePickles"><img src="https://avatars2.githubusercontent.com/u/56023363?v=4" width="100px;" alt="SnicklePickles"/><br /><sub><b>SnicklePickles</b></sub></a><br /><a href="https://github.com/unoplatform/uno/commits?author=SnicklePickles" title="Documentation">📖</a></td>
  </tr>
  <tr>
    <td align="center"><a href="https://github.com/Pedrocssg"><img src="https://avatars3.githubusercontent.com/u/24390966?v=4" width="100px;" alt="Pedro Gonçalves"/><br /><sub><b>Pedro Gonçalves</b></sub></a><br /><a href="https://github.com/unoplatform/uno/commits?author=Pedrocssg" title="Documentation">📖</a></td>
    <td align="center"><a href="https://github.com/Bharat123rox"><img src="https://avatars3.githubusercontent.com/u/13381361?v=4" width="100px;" alt="Bharat Raghunathan"/><br /><sub><b>Bharat Raghunathan</b></sub></a><br /><a href="https://github.com/unoplatform/uno/commits?author=Bharat123rox" title="Documentation">📖</a></td>
    <td align="center"><a href="https://github.com/aayushbisen"><img src="https://avatars2.githubusercontent.com/u/41341387?v=4" width="100px;" alt="Aayush Bisen"/><br /><sub><b>Aayush Bisen</b></sub></a><br /><a href="https://github.com/unoplatform/uno/commits?author=aayushbisen" title="Documentation">📖</a></td>
    <td align="center"><a href="https://lex111.ru/"><img src="https://avatars2.githubusercontent.com/u/4408379?v=4" width="100px;" alt="Alexey Pyltsyn"/><br /><sub><b>Alexey Pyltsyn</b></sub></a><br /><a href="https://github.com/unoplatform/uno/commits?author=lex111" title="Documentation">📖</a></td>
    <td align="center"><a href="http://imagentleman.github.io"><img src="https://avatars2.githubusercontent.com/u/2272928?v=4" width="100px;" alt="José Antonio Chio"/><br /><sub><b>José Antonio Chio</b></sub></a><br /><a href="https://github.com/unoplatform/uno/commits?author=imagentleman" title="Documentation">📖</a></td>
  </tr>
  <tr>
    <td align="center"><a href="https://mrassili.com"><img src="https://avatars0.githubusercontent.com/u/25288435?v=4" width="100px;" alt="Marouane R"/><br /><sub><b>Marouane R</b></sub></a><br /><a href="https://github.com/unoplatform/uno/commits?author=mrassili" title="Documentation">📖</a></td>
    <td align="center"><a href="https://github.com/avinasjha"><img src="https://avatars1.githubusercontent.com/u/56090532?v=4" width="100px;" alt="Avinash Jha"/><br /><sub><b>Avinash Jha</b></sub></a><br /><a href="https://github.com/unoplatform/uno/commits?author=avinasjha" title="Documentation">📖</a></td>
    <td align="center"><a href="https://github.com/khyativalera"><img src="https://avatars3.githubusercontent.com/u/47522632?v=4" width="100px;" alt="Khyati Valera"/><br /><sub><b>Khyati Valera</b></sub></a><br /><a href="https://github.com/unoplatform/uno/commits?author=khyativalera" title="Documentation">📖</a></td>
    <td align="center"><a href="https://github.com/pushkyn"><img src="https://avatars0.githubusercontent.com/u/3326427?v=4" width="100px;" alt="Alex"/><br /><sub><b>Alex</b></sub></a><br /><a href="https://github.com/unoplatform/uno/commits?author=pushkyn" title="Documentation">📖</a></td>
    <td align="center"><a href="https://miguelpiedrafita.com"><img src="https://avatars0.githubusercontent.com/u/23558090?v=4" width="100px;" alt="Miguel Piedrafita"/><br /><sub><b>Miguel Piedrafita</b></sub></a><br /><a href="https://github.com/unoplatform/uno/commits?author=m1guelpf" title="Documentation">📖</a></td>
  </tr>
  <tr>
    <td align="center"><a href="http://www.linkedin.com/in/gagandeepp"><img src="https://avatars1.githubusercontent.com/u/34858937?v=4" width="100px;" alt="Gagan Deep"/><br /><sub><b>Gagan Deep</b></sub></a><br /><a href="https://github.com/unoplatform/uno/commits?author=gagandeepp" title="Documentation">📖</a></td>
    <td align="center"><a href="https://github.com/RobJenks"><img src="https://avatars0.githubusercontent.com/u/3159730?v=4" width="100px;" alt="RobJenks"/><br /><sub><b>RobJenks</b></sub></a><br /><a href="https://github.com/unoplatform/uno/commits?author=RobJenks" title="Documentation">📖</a></td>
    <td align="center"><a href="https://github.com/cTxplorer"><img src="https://avatars0.githubusercontent.com/u/28287478?v=4" width="100px;" alt="Pratik Gadhiya"/><br /><sub><b>Pratik Gadhiya</b></sub></a><br /><a href="https://github.com/unoplatform/uno/commits?author=cTxplorer" title="Documentation">📖</a></td>
    <td align="center"><a href="https://github.com/mkwhalen"><img src="https://avatars0.githubusercontent.com/u/17869199?v=4" width="100px;" alt="MacKenzie Whalen"/><br /><sub><b>MacKenzie Whalen</b></sub></a><br /><a href="https://github.com/unoplatform/uno/commits?author=mkwhalen" title="Documentation">📖</a></td>
    <td align="center"><a href="https://github.com/jotaro-sama"><img src="https://avatars1.githubusercontent.com/u/36264038?v=4" width="100px;" alt="Giovanni De Luca"/><br /><sub><b>Giovanni De Luca</b></sub></a><br /><a href="#infra-jotaro-sama" title="Infrastructure (Hosting, Build-Tools, etc)">🚇</a></td>
  </tr>
  <tr>
    <td align="center"><a href="https://www.facebook.com/InternetHeroBINIT"><img src="https://avatars1.githubusercontent.com/u/20013689?v=4" width="100px;" alt="Binit Ghimire"/><br /><sub><b>Binit Ghimire</b></sub></a><br /><a href="https://github.com/unoplatform/uno/commits?author=TheBinitGhimire" title="Documentation">📖</a></td>
  </tr>
</table>

<!-- ALL-CONTRIBUTORS-LIST:END -->

💖 Thank-you.
