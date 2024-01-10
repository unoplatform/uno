---
uid: Uno.Documentation.Intro
---

# Uno Platform documentation

<div class="row">

<div class="col-md-6 col-xs-12 ">
<a href="get-started.md">
<div class="alert alert-info alert-hover">

#### Get Started

Set up with your OS and IDE of choice.

</div>
</a>
</div>

<div class="col-md-6 col-xs-12 ">
<a href="tutorials-intro.md">
<div class="alert alert-info alert-hover">

#### How-tos and Tutorials

See real-world examples with working code.

</div>
</a>
</div>

<div class="col-md-6 col-xs-12 ">
<a href="using-uno-ui.md">
<div class="alert alert-info alert-hover">

#### Developing with Uno Platform

Learn the principles of cross-platform development with Uno.

</div>
</a>
</div>

<div class="col-md-6 col-xs-12 ">
<a href="implemented-views.md">
<div class="alert alert-info alert-hover">

#### API Reference

Browse the set of available controls and their properties.

</div>
</a>
</div>

</div>

<br/>

Uno Platform lets you write an application once in XAML and/or C#.

<br/>
<br/>

***


## Top questions about Uno Platform

#### What platforms can I target with Uno Platform?

Uno Platform applications run on Web (via WebAssembly), Windows, Linux, mac Catalyst, iOS, Android. [Check supported platform versions.](xref:Uno.GettingStarted.Requirements)

#### Are Uno Platform applications native?

Yes - Uno Platform taps into the native UI frameworks on most supported platforms, so your final product is a native app. [Read more about how Uno Platform works.](what-is-uno.md)

#### Can applications look the same on all platforms?

Yes. Unless configured otherwise, your application's UI renders exactly the same on all targeted platforms, to the pixel. Uno Platform achieves this by taking low-level control of the native visual primitives on the targeted platform. [Read more about how Uno works.](what-is-uno.md)

#### How is Uno Platform different from .NET MAUI?

First, Uno Platform is available in production today to build single-codebase, pixel-perfect applications for Web, Desktop and Mobile. .NET MAUI is successor to Xamarin.Forms.

Second, Uno Platform can target additional platforms like Linux and the Web.

Third, Uno Platform aligns with WinUI, which uses a flavor of XAML most Windows developers are familiar with. It also allows you to tap in WinUI's rich styling engine to create pixel-perfect applications.

Fourth, Uno Platform provides an optional [Figma plugin](https://platform.uno/unofigma/) for pixel-perfect XAML export for Uno Platform apps.

Finally, by extending the reach of WinUI across all supported platforms, it also allows you to leverage the rich 1st and 3rd party ecosystem and bring rich controls everywhere like `DataGrid`, `TreeView`, `TabView`, `NavigationView` and many others.

At the practical level, we suggest you try both and see which works the best for your skill set and scenario.

#### How is Uno Platform different from Blazor?

Uno Platform applications are cross-platform, running on the web as well as mobile and desktop, equally, from a single codebase. Blazor is a feature of ASP.NET for primarily building web applications.

Uno Platform applications are written in C# and XAML markup, whereas Blazor applications are written in 'Razor' syntax, a hybrid of HTML/CSS and C#.

Uno Platform and Blazor both make use of .NET's WebAssembly support to run natively in the browser.

#### How is Uno Platform different from Flutter?

Uno Platform and Flutter solve a similar problem - pixel-perfect applications on all target platforms. However, Uno Platform leverages decades of Microsoft's investment made into developer tooling .NET and C# programming language for developing applications.

#### Do I need to have an existing UWP/WinUI app or skills to use Uno Platform?

No, there's no need to have an existing UWP or WinUI application, or have that specific skill set. The [Uno Platform templates](xref:Uno.GetStarted) make it easy to create a new project in Visual Studio or from the command line for anyone familiar with C# and XAML.

#### What 3rd parties support Uno Platform?

Uno Platform is supported by a number of 3rd-party packages and libraries, including advanced controls from Microsoft Windows Community Toolkit, Syncfusion, LightningChart and Infragistics; graphics processing with SkiaSharp; presentation and navigation with Prism, ReactiveUI and MVVMCross; local database management with SQLite; and more. [See the full list of supported 3rd-party libraries.](xref:Uno.Development.SupportedLibraries)

#### Where do I get help if I have any questions?

Free support is available via our [GitHub Discussions](https://github.com/unoplatform/uno/discussions) or [Discord](https://www.platform.uno/discord) - #uno-platform channel where our engineering team and community will be able to help you.

#### How do you sustain Uno Platform?

The Uno Platform is free and open source under the Apache 2.0 license. Alongside valued contributions from the Uno community, development by the core team is sustained by paid professional support contracts offered to enterprises who use Uno Platform. [Learn more about our paid professional support.](https://platform.uno/contact/)

More details about sustainability are covered here: https://platform.uno/blog/sustaining-the-open-source-uno-platform/

<br>

[_See more frequently asked questions about the Uno Platform._](faq.md)


## Next Steps

Learn more about:

 - [Get Started](get-started.md)
 - [How-tos and Tutorials](tutorials-intro.md)
 - [Developing with Uno Platform](using-uno-ui.md)
 - [API Reference](implemented-views.md)