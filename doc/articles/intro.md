---
uid: Uno.Documentation.Intro
---

# Uno Platform Documentation

<!-- markdownlint-disable MD001 -->

Uno Platform is an open-source .NET platform for building single codebase native mobile, web, desktop, and embedded apps quickly.

<br/>

<div class="row">

<!-- Get Started -->
<div class="col-md-6 col-xs-12 ">
<a href="get-started.md">
<div class="alert alert-info alert-hover">

#### Get Started

Set up with your OS and IDE of choice.

</div>
</a>
</div>

<!-- How-tos and Tutorials -->
<div class="col-md-6 col-xs-12 ">
<a href="samples-tutorials-overview.md">
<div class="alert alert-info alert-hover">

#### How-tos and Tutorials

Four complete tutorials and hundreds of real-world samples.

</div>
</a>
</div>

<!-- Developing with Uno Platform -->
<div class="col-md-6 col-xs-12 ">
<a href="using-uno-ui.md">
<div class="alert alert-info alert-hover">

#### Developing with Uno Platform

Learn the principles of cross-platform development with Uno.

</div>
</a>
</div>

<!-- C# Markup -->
<div class="col-md-6 col-xs-12 ">
<a href="xref:Uno.Extensions.Markup.Overview">
<div class="alert alert-info alert-hover">

#### C# Markup

Write UI using C# instead of XAML

</div>
</a>
</div>

<!-- MVUX -->
<div class="col-md-6 col-xs-12 ">
<a href="xref:Uno.Extensions.Mvux.Overview">
<div class="alert alert-info alert-hover">

#### MVUX

Reactive programming with Uno Platform

</div>
</a>
</div>

<!-- Uno Toolkit -->
<div class="col-md-6 col-xs-12 ">
<a href="xref:Toolkit.GettingStarted">
<div class="alert alert-info alert-hover">

#### Uno Toolkit

Include new advanced UI controls

</div>
</a>
</div>

<!-- Uno Platform Studio -->
<div class="col-md-6 col-xs-12 ">
<a href="xref:Uno.Platform.Studio.Overview">
<div class="alert alert-info alert-hover">

#### Uno Platform Studio

Boost productivity with Hot Design®, Hot Reload, and Design-to-Code

</div>
</a>
</div>

<!-- Figma -->
<div class="col-md-6 col-xs-12 ">
<a href="xref:Uno.Figma.GetStarted">
<div class="alert alert-info alert-hover">

#### Figma

Design your app in Figma and easily import to XAML or C#

</div>
</a>
</div>

<!-- Uno Themes -->
<div class="col-md-6 col-xs-12 ">
<a href="external/uno.themes/doc/themes-overview.md">
<div class="alert alert-info alert-hover">

#### Uno Themes

Use Material theme in your app

</div>
</a>
</div>

<!-- Uno Extensions -->
<div class="col-md-6 col-xs-12 ">
<a href="external/uno.extensions/doc/ExtensionsOverview.md">
<div class="alert alert-info alert-hover">

#### Uno Extensions

Include large building blocks to complete your app faster

</div>
</a>
</div>

<!-- API Reference -->
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

## High level architecture

Uno Platform's application API is compatible with Microsoft's [WinUI 3 API](https://learn.microsoft.com/windows/apps/winui/winui3/). In fact, when your application runs on Windows, it's just an ordinary WinUI 3/WinAppSDK application.

This means that existing WinUI code is compatible with Uno Platform. Existing WinUI libraries can be recompiled for use in Uno Platform applications. A number of [3rd-party libraries](xref:Uno.Development.SupportedLibraries) have been ported to Uno Platform.

![High-level architecture diagram - WinUI on Windows, Uno.UI on other platforms](Assets/high-level-architecture-diagram.png)

Uno Platform is pixel-perfect by design, delivering consistent visuals on every platform. At the same time, it can either use the [native UI framework](xref:uno.features.renderer.native) on some target platforms or use a [Skia-based rendering](xref:uno.features.renderer.skia) approach, while making it easy to [integrate native views](xref:Uno.Development.NativeViews) and tap into native platform features.

Learn more about [how Uno Platform works](xref:Uno.Development.HowItWorks).

## Next Steps

Once you've gone through our [Get Started](get-started.md) guides, please visit our [GitHub Discussions](https://github.com/unoplatform/uno/discussions) where our team and community will be able to help you.
<br/>
<br/>

---
