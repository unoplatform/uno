<!--markdownlint-disable MD033 -->
<h1 align=center>
 <img alt="Uno Platform Banner" align=center width="95%" src="https://github.com/user-attachments/assets/ac1b6c72-a6ed-4c3f-9e0d-a8eca96bdc4e" />
</h1>

<div align=center>

[![NuGet](https://img.shields.io/nuget/v/uno.sdk.svg?style=flat&color=159bff&label=NuGet)](https://www.nuget.org/packages/uno.sdk/)
[![Azure DevOps](https://img.shields.io/azure-devops/build/uno-platform/1dd81cbd-cb35-41de-a570-b0df3571a196/5/master?label=master)](https://uno-platform.visualstudio.com/Uno%20Platform/_build?definitionId=5)
[![NuGet Downloads](https://img.shields.io/nuget/dt/uno.winui.svg?style=flat&color=7a67f8&label=NuGet%20Downloads)](https://www.nuget.org/packages/uno.winui)
[![GitHub Stars](https://img.shields.io/github/stars/unoplatform/uno?style=flat&color=7a67f8&label=GitHub%20Stars)](https://github.com/unoplatform/uno/stargazers/)
[![All Contributors](https://img.shields.io/github/contributors/unoplatform/uno.svg?style=flat&color=7a67f8&label=Contributors)](https://GitHub.com/unoplatform/uno/graphs/contributors)
[![Open Uno in Gitpod](https://img.shields.io/badge/gitpod-setup%20automated-159bff?logo=gitpod&style=flat&label=Gitpod)](https://gitpod.io/#https://github.com/unoplatform/uno)
[![PRs Welcome](https://img.shields.io/badge/PRs-Welcome-brightgreen.svg?style=flat)](https://github.com/unoplatform/uno/blob/master/CONTRIBUTING.md)

Uno Platform is an open-source developer platform for building single-codebase .NET applications that run natively on Web, Desktop, Mobile, and Embedded systems. It uses the WinUI 3 API surface, allowing you to reuse your existing C# and XAML skills to reach all platforms.

> Uno Platform is trusted by over 300 contributors and used by enterprise clients such as Toyota, Microsoft, and Kahua for mission-critical applications. With ~10,000 GitHub stars and 130+ million NuGet downloads, it is a proven foundation for professional-grade development.

</div>

---

## Uno Platform and Uno Platform Studio for The Most Productive C# / XAML Dev Loop

### Uno Platform (Core Framework)

The free and open-source (Apache 2.0) foundation for building cross-platform .NET applications. It includes the UI framework, platform heads, and a rich set of developer experience enhancements.

#### 🌐 Cross-Platform Support

Develop fully native applications for a wide range of platforms from a single codebase.

* **Mobile (iOS & Android)**: Build native, pixel-perfect UIs with C# and XAML.
* **Web (WebAssembly)**: Reuse existing C# and XAML skills to build fast web applications.
* **Desktop (Windows, macOS & Linux)**: Uno Platform uses **Skia** for rendering across all desktop platforms, ensuring high-performance, hardware-accelerated graphics and animations. On Windows, Skia runs within a Win32 shell, on macOS within an AppKit shell (Metal when available), and on Linux within an X11 shell or directly to the Framebuffer for embedded scenarios.

#### 🛠️ Toolkit & Extensions

* **UI Controls**: Access [hundreds of UI components from WinUI](https://aka.platform.uno/using-uno-ui), [Windows Community Toolkit](https://aka.platform.uno/install-uno-community-toolkit), [Uno Toolkit](https://aka.platform.uno/uno-toolkit), [third-party vendors](https://aka.platform.uno/supported-libraries), and even .NET [MAUI controls](https://aka.platform.uno/maui-embedding).
* **Theming**: Easily theme your app with [Material, Fluent, or Cupertino styles](https://aka.platform.uno/uno-themes) using minimal code.
* **State Management**: Choose between the familiar MVVM pattern or the modern [MVUX](https://aka.platform.uno/mvux) approach for declarative and scalable state management.
* **Extensions**: Utilize [ready-to-use libraries](https://aka.platform.uno/uno-extensions) for common functionalities like navigation, logging, and dependency injection.
* **Non-UI Cross-Platform APIs**: Access a [comprehensive set of APIs](https://aka.platform.uno/using-uno-ui) to interact with native device features such as sensors and secure storage.

### Uno Platform Studio

An optional premium toolkit that integrates with Visual Studio, VS Code, and JetBrains Rider to offer an unparalleled development loop.

* **[Hot Design](https://aka.platform.uno/hot-design)**: A next-generation visual designer that transforms your live app into a design surface with a single click.
* **[Hot Reload](https://aka.platform.uno/hot-reload)**: Instantly modify XAML and C# on a running app, allowing for rapid iteration without losing the app's state.
* **[Design-to-Code](https://aka.platform.uno/Design-to-Code)**: Export Figma designs to clean, responsive XAML or C# markup in seconds.
<img alt="Uno Platform Studio - Hot Design" align=center width="95%" src="https://github.com/user-attachments/assets/0962dbac-2130-4d2d-b187-71974160ddbe" />

---

## 🚀 Quick Start

Get your development environment ready and create your first app in minutes.

1. **Check Your Environment**: Use our Uno.Check command-line tool to automatically check, install, and configure all required workloads and dependencies.

2. **Create Your App**: Use the Template Wizard in your IDE or the command line to quickly create and configure new Uno Platform projects with the appropriate settings for your target platforms.

    ![Uno Platform New Project Wizard](https://github.com/user-attachments/assets/fc67df45-30e2-43d7-8323-890686f1e9bf)

3. **Build and Run**

➡️ For detailed guides, visit the **[Official Uno Platform Documentation](https://aka.platform.uno/get-started)** on how to get started with Uno Platform.

---

## 🛠️ How It Works

Uno Platform unifies cross-platform development by abstracting platform-specific implementations behind the WinUI 3 API surface.

1. **Develop**: You write your application in a single project using C# and XAML (or C# Markup) within your preferred environment (Visual Studio, JetBrains Rider, Visual Studio Code) on Windows, macOS, or Linux.
2. **Render**: Uno Platform renders your UI using one of two methods:
    * **Unified Skia Rendering**: A Skia-based engine draws your UI on a canvas, ensuring consistent performance, smooth animations, and pixel-perfect visuals across all targets.
    * **Native Rendering**: The XAML UI is translated into native platform controls (e.g., UIKit on iOS), providing a platform-native look and feel when desired.
3. **Deploy**: The build process generates a native application package for each target platform from the single codebase.

# Uno Platform Features

* Supported platforms:
  * Windows 10 and Windows 11
  * Windows 7 (via Skia Desktop)
  * macOS (via Skia Desktop)
  * iOS and Android (via [.NET](https://dotnet.microsoft.com/))
  * WebAssembly through the [.NET Runtime WebAssembly SDK](https://github.com/dotnet/runtime/tree/main/src/mono/wasm)
  * Linux (via Skia Desktop with X11 and FrameBuffer)
* Dev loop:
  * Develop on your favorite IDE (Visual Studio, Rider, or VS Code) on your favorite OS (Windows, macOS, or Linux)
  * XAML and/or C# Hot Reload for WebAssembly, macOS, Linux, Windows, iOS, and Android
  * [Uno.UITest](https://github.com/unoplatform/Uno.UITest), a library to create Cross-Platform UI Tests for WebAssembly, iOS, and Android.
* Cross-Platform Controls:
  * [Control Templating](https://learn.microsoft.com/windows/uwp/design/controls-and-patterns/control-templates)
  * [Data Templating](https://code.msdn.microsoft.com/Data-Binding-in-UWP-b5c98114)
  * [Styling](https://learn.microsoft.com/windows/uwp/design/controls-and-patterns/xaml-styles)
  * [Rich Animations](https://learn.microsoft.com/windows/uwp/design/motion/xaml-animation)
  * [Composition API](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/composition)
* WinUI Code Support:
  * [Windows Community Toolkit](https://github.com/CommunityToolkit/Windows)
  * [Windows Community Toolkit (Uno Fork)](https://github.com/unoplatform/uno.WindowsCommunityToolkit)
  * [Community Toolkit MVVM](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/)
  * [Microsoft XAML Behaviors](https://github.com/unoplatform/uno.XamlBehaviors)
  * [Prism](https://github.com/prismlibrary/prism)
  * [SkiaSharp](https://github.com/mono/SkiaSharp)
  * [SkiaSharp.Extended](https://github.com/mono/SkiaSharp.Extended)
  * [ReactiveUI Official](https://github.com/reactiveui/ReactiveUI/pull/2067)
  * [WindowsStateTriggers](https://github.com/unoplatform/uno.WindowsStateTriggers)
  * [Rx.NET](https://github.com/reactiveui/Reactive.Wasm)
  * [ColorCode-Universal](https://github.com/unoplatform/uno.ColorCode-Universal)
  * [LibVLCSharp](https://github.com/videolan/libvlcsharp)
  * [MapsUI](https://github.com/Mapsui/Mapsui)
  * [LiveCharts2](https://github.com/beto-rodriguez/LiveCharts2)
  * Any WinUI project
* Responsive Design:
  * [Visual State Manager](https://learn.microsoft.com/uwp/api/Microsoft.UI.Xaml.VisualStateManager)
  * [State Triggers](https://blogs.msdn.microsoft.com/mvpawardprogram/2017/02/07/state-triggers-uwp-apps/)
* [Adaptive Triggers](https://learn.microsoft.com/uwp/api/Microsoft.UI.Xaml.AdaptiveTrigger)
* Platform Specific:
  * Native controls and properties via [conditional XAML](doc/articles/platform-specific-xaml.md)
  * Any of the existing Xamarin iOS/Android libraries available

---

## 📚 Learning & Community Resources

[![X/Twitter Followers](https://img.shields.io/twitter/follow/unoplatform?label=follow%20%40unoplatform&style=flat&color=f85977&logo=x)](https://x.com/unoplatform)
[![Uno Platform Discord](https://img.shields.io/discord/1182775715242967050?label=Discord&color=f85977)](https://platform.uno/discord)
![YouTube Followers](https://img.shields.io/youtube/channel/subscribers/UC8GkqD6hsSkwYof6n2Wk1hg?label=Follow%20%40UnoPlatform&style=flat&color=f85977&logo=Youtube)

* **[Official Documentation](https://aka.platform.uno/uno-docs-intro)**: The complete guide to Uno Platform.
* **[Uno Platform Studio](https://aka.platform.uno/studio)**: Details for the set of productivity tools Uno Platform Studio, that includes:
  * **[Hot Design](https://aka.platform.uno/hot-design)**
  * **[Hot Reload](https://aka.platform.uno/hot-reload)**
  * **[Design-to-Code](https://aka.platform.uno/Design-to-Code)**
* **[Uno Playground](https://playground.platform.uno/)**: Experiment with code snippets and see live previews.
* **[Uno Gallery](https://gallery.platform.uno/)**: Explore various UI themes and components in action.
* **[Workshops & Code Samples](https://aka.platform.uno/samples-tutorials)**: Access practical tutorials and sample projects to accelerate learning.
* **[Case Studies](https://platform.uno/case-studies/)**: Learn from real-world applications built using the Uno Platform.
* **[Uno Platform Blog](https://platform.uno/blog/)**: The latest news, technical deep-dives, and release announcements.
* **[Discord Server](https://aka.platform.uno/discord)**: Join our active community for real-time discussions and support.
* **[Uno Tech Bites](https://www.youtube.com/playlist?list=PLl_OlDcUya9rP_fDcFrHWV3DuP7KhQKRA)**: Bite-sized learning videos with the team.
* **[Why Use Uno Platform for Your Project?](https://aka.platform.uno/why-uno-platform)**: Discover the key benefits and reasons to choose Uno Platform.
* **[General FAQ](https://aka.platform.uno/uno-faq)**: Find answers to common questions about Uno Platform.

## Contributing

This is an active open-source project, and we welcome contributions from the community.
To learn how you can get involved, please refer to our **[Contribution Guide (`CONTRIBUTING.md`)](https://github.com/unoplatform/uno/blob/master/CONTRIBUTING.md)** for details on how to get started.

## Contributors

Thanks go to these wonderful people (List made with [contrib.rocks](https://contrib.rocks)):

[![Uno Platform Contributors](https://contrib.rocks/image?repo=unoplatform/uno&max=500&columns=15)](https://github.com/unoplatform/uno/graphs/contributors)

## License

This repository is licensed under the **[Apache 2.0 License](https://github.com/unoplatform/uno/blob/master/LICENSE)**.
