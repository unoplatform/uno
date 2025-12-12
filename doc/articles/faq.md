---
uid: Uno.Development.FAQ
---

# FAQ: General

## About Uno Platform

### What is the Uno Platform?

Uno Platform lets you write an application once in XAML and/or C#, and deploy it to [any target platform](getting-started/requirements.md). Read more [here](xref:Uno.Documentation.Intro).

### Who makes Uno Platform?

Uno Platform is an [open-source project](https://github.com/unoplatform/Uno) with many contributors. It was developed internally by [nventive](https://nventive.com) from 2013-2018, and has been open source since 2018. It's maintained by [Uno Platform](https://platform.uno).

### What platforms can I target with Uno Platform?

Uno Platform applications run on the Web (via WebAssembly), Windows, Linux, macOS, iOS, and Android. [Check supported platform versions.](xref:Uno.GettingStarted.Requirements)

### Are Uno Platform applications native?

Yes - Uno Platform taps into the native UI frameworks on most supported platforms, so your final product is a native app. [Read more about how Uno Platform works.](xref:Uno.Documentation.Intro)

### Can applications look the same on all platforms?

Yes. Unless configured otherwise, your application's UI renders exactly the same on all targeted platforms, to the pixel. Uno Platform achieves this by either taking low-level control of the native visual primitives on the targeted platform or by using a Skia-based rendering approach. [Read more about how Uno works.](xref:Uno.Documentation.Intro)

### How is Uno Platform different from .NET MAUI?

First, Uno Platform is available in production today to build single-codebase, pixel-perfect applications for Web, Desktop, and Mobile. .NET MAUI is a successor to Xamarin.Forms.

Second, Uno Platform can target additional platforms like Linux and the Web.

Third, Uno Platform aligns with WinUI, which uses a flavor of XAML most Windows developers are familiar with. It also allows you to tap into WinUI's rich styling engine to create pixel-perfect applications.

Fourth, Uno Platform provides [Hot Design](https://platform.uno/hot-design/) for visually designing your Uno Platform apps.

Fifth, Uno Platform provides an optional [Figma plugin](https://platform.uno/unofigma/) for pixel-perfect XAML export for Uno Platform apps.

Finally, by extending the reach of WinUI across all supported platforms, it also allows you to leverage the rich 1st and 3rd party ecosystem and bring rich controls everywhere like `DataGrid`, `TreeView`, `TabView`, `NavigationView`, and many others.

At the practical level, we suggest you try both and see which works the best for your skill set and scenario.

### How is Uno Platform different from Blazor?

Uno Platform applications are cross-platform, running on the web as well as mobile and desktop, equally, from a single codebase. Blazor is a feature of ASP.NET for primarily building web applications.

Uno Platform applications are written in C# and XAML markup, whereas Blazor applications are written in 'Razor' syntax, a hybrid of HTML/CSS and C#.

Uno Platform and Blazor both make use of .NET's WebAssembly support to run natively in the browser.

### How is Uno Platform different from Flutter?

Uno Platform and Flutter solve a similar problem - pixel-perfect applications on all target platforms. However, Uno Platform leverages decades of Microsoft's investment made into developer tooling, .NET, and C# programming language for developing applications.

### Do I need to have an existing WinUI app or skills to use Uno Platform?

No, there's no need to have an existing WinUI application or have that specific skill set. The [Uno Platform templates](xref:Uno.GetStarted) make it easy to create a new project in Visual Studio or from the command line for anyone familiar with C# and XAML.

### What 3rd parties support Uno Platform?

Uno Platform is supported by several 3rd-party packages and libraries, including advanced controls from Microsoft Windows Community Toolkit, Syncfusion, and LightningChart; graphics processing with SkiaSharp; presentation and navigation with Prism, ReactiveUI, and MVVMCross; HTTP/API clients with Refit and Kiota; local database management with SQLite; and more. [See the full list of supported 3rd-party libraries.](xref:Uno.Development.SupportedLibraries)

### Where can I get support?

Support is available through [GitHub Discussions](https://github.com/unoplatform/uno/discussions) or [Discord Server](https://www.platform.uno/discord) - where our engineering team and community will be able to help you.  
Paid support is also available and allows you to collaborate with our engineering team to ensure the success of your projects and address your GitHub issues faster. [Contact us for more details.](https://platform.uno/contact/)

### How can I get involved?

There are lots of ways to contribute to the Uno Platform and we appreciate all the help we get from the community. You can provide feedback, report bugs, give suggestions, contribute code, and participate in the platform discussions. If you're interested, the [contributors' guide](uno-development/contributing-intro.md) is a great place to start.

### How can I report a bug?

If you think you've found a bug, please [log a new issue](https://github.com/unoplatform/Uno/issues) in the Uno Platform GitHub issue tracker. When filing issues, please use our bug filing template. The best way to get your bug fixed is to be as detailed as you can be about the problem. Providing a minimal project with steps to reproduce the problem is ideal. Here are questions you can answer before you file a bug to make sure you're not missing any important information.

## Features

### Is [Control X] supported by Uno Platform?

Consult [the list of supported WinUI controls](implemented-views.md).

### What do I need to develop Uno Platform applications?

You can develop Uno Platform applications on Windows, macOS, or Linux. Supported IDEs include Visual Studio, Visual Studio Code, and Rider. Consult the [setup guide](get-started.md) for more details. If you need help with issues specific to your developer environment or hardware, check out [Common Issues](xref:Uno.UI.CommonIssues).

### Can I use VB.NET for Uno Platform applications?

Much like the new UI technologies from Microsoft, Uno Platform doesn’t support creation of new applications using VB.NET.

If you have an existing VB.NET application that you would like to port/modernize for cross-platform scenarios with Uno Platform, you should be able to reuse all of your VB.NET business logic. It needs to be built as .NET standard libraries, then used in a new Uno Platform app where only the new UI code would be defined in XAML with some glue in C#.
To be exact, add "Class Library" VB project (not "Class Library (.Net Framework)", and not "Class Library (Universal Windows)"). Use ".NET Standard 2.0" as Target Framework.
To use this library in Uno heads for all platforms, add a reference to this library (the simplest way is to right-click on "References" nodes inside these heads).
You can also use the same Class Library in your original VB project. The same Class Library can also be used in any other .NET Standard compatible projects.
Additionally, if you’d like to move any of your VB.NET code to C#, you may be able to use automated tools such as https://converter.telerik.com

The best course of action is to do a POC and our team is happy to assist you in validating Uno Platform’s fit. Please [contact us](https://platform.uno/contact) with any queries.

## Technologies

### What is WinUI 3?

WinUI 3 is the [next generation of Microsoft's Windows UI library](https://learn.microsoft.com/windows/apps/winui/).

From [Microsoft](https://learn.microsoft.com/windows/apps/winui/):

> WinUI is the path forward for all Windows apps—you can use it as the UI layer on your native UWP or Win32 app, or you can gradually modernize your desktop app, piece by piece, with XAML Islands.
> All new XAML features will eventually ship as part of WinUI. The existing UWP XAML APIs that ship as part of the OS will no longer receive new feature updates. However, they will continue to receive security updates and critical fixes according to the Windows 10 support lifecycle.

Read more about [Uno Platform and WinUI 3](uwp-vs-winui3.md).

### How is Uno Platform different from Xamarin.Forms, MAUI or Avalonia?

Multiple techniques can be used to render UI, ranging from rendering pixels in a Frame Buffer (Avalonia) to rendering only using platform-provided controls (Xamarin.Forms, MAUI).

While the former provides high flexibility in terms of rendering fidelity and the ability to add new platforms, it has the drawback of not following the platform's native behaviors. For instance, interactions with text boxes have to be re-implemented completely to match the native behavior and have to be updated regularly to follow platform updates. This approach also makes it more difficult to integrate native UI components "in-canvas", such as Map or Browser controls.

The latter, however, provides full fidelity with the underlying platform, making it blend easily with native applications. While this can be interesting for some kinds of applications, designers usually want to have a branded pixel-perfect look and feel that stays consistent across platforms, where drawing primitives are not available.

The Uno Platform sits in the middle, using the power of XAML to provide the ability to custom draw and animate UI, while reusing key parts of the underlying UI Toolkit (such as chrome-less native text boxes) to provide native system interactions support and native accessibility features.

### What is XAML?

XAML stands for **eXtensible Application Markup Language**, and is used to provide a declarative way to define user interfaces. Its ability to provide a clear separation of concerns between the UI definition and application logic using data binding provides a good experience for small to very large applications where integrators can easily create UI without having to deal with C# code.
