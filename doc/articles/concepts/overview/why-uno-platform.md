---
uid: Uno.Overview.WhyUno
---

# Why use Uno Platform for your project?

Thank you for considering Uno Platform for your next project. While we always recommend trying out any stack before deciding on your next project, here are some tips you can use for your in-depth evaluation.

## It's a platform, not just a UI framework

Uno Platform is a developer productivity **platform** and much **more than a UI framework**, for building single codebase, native, mobile, web, desktop, and embedded apps with .NET.

At the core is a cross-platform .NET UI framework, that allows apps to run everywhere with a single codebase.

However, built on top of this foundation is also a rich platform which includes libraries, extensions, and tools that accelerate the design, development, and testing of cross-platform applications.

## Uno Platform Studio - Productivity Tools That Set Us Apart

**[Uno Platform Studio](xref:Uno.Platform.Studio.Overview)** revolutionizes how developers design, build, and iterate on cross-platform applications. This comprehensive suite of productivity tools is a key differentiator that sets Uno Platform apart from other alternatives.

### Hot Design® - The Industry's First Visual Designer for Cross-Platform .NET

**[Hot Design](xref:Uno.HotDesign.Overview)** is the industry's first runtime visual designer for cross-platform .NET applications. It transforms your running app into a designer from any IDE on any OS, offering unprecedented productivity gains. Hot Design provides true WYSIWYG design capabilities at runtime across all platforms.

### Hot Design® Agent - AI-Powered UX/UI Creation

**[Hot Design Agent](xref:Uno.HotDesign.Agent)** is an AI tooling assistant that builds your UI while the app is running. It works with data contexts and live previews to help developers design and interact with user interfaces in real time.

### MCP - AI-Powered Development Intelligence

**[Uno MCP](xref:Uno.Features.Uno.MCPs)** leverages semantic understanding to grasp your intent and deliver precise, context-aware answers. It provides instant, accurate guidance grounded in official Uno Platform documentation directly from your IDE.

### App MCP - Agent Control for Running Applications

**App MCP** allows AI agents to control the running app using pointer and keyboard interactions along with visual tree queries. It enables AI agents from Visual Studio, VS Code, Claude Code, GitHub Copilot CLI, Codex, and more to interact with your application.

## True Single Project across Mobile, Web, Desktop, and Embedded

Our Single Project approach is .NET ecosystem’s first and only true Single Project solution, empowering developers with a unified approach, spanning mobile, web, desktop, and embedded apps. This genuine Single Project approach simplifies development, accelerates build times, and facilitates platform-specific development, enhancing your productivity and efficiency.

## It's Free and Open-Source

Uno Platform is free and open source under Apache 2.0.

It is well funded and has a [sustainability model](https://platform.uno/blog/sustaining-the-open-source-uno-platform/) built in so that the project is sustainable in the long term. In addition, it is fueled by support from a thriving community of users who regularly send feedback and contribute.

## Why Choose Uno Platform Over Alternatives?

### Broader Platform Support

Uno Platform supports WebAssembly, enabling your .NET apps to run natively in browsers alongside mobile and desktop platforms. Uno Platform delivers true cross-platform reach including web deployment without additional frameworks.

### Rich Tooling

Uno Platform leverages the mature .NET ecosystem with Visual Studio, VS Code, and Rider support. With **Uno Platform Studio**, you get industry-leading tools like Hot Design®, Hot Design® Agent, and AI-powered development assistance through Uno MCP—capabilities.

### Pixel-Perfect Control with Multiple Rendering Options

Uno Platform offers both native and Skia-based rendering approaches, giving you flexibility other frameworks lack. You can choose native platform controls for maximum OS integration or Skia rendering for guaranteed pixel-perfect consistency across all platforms—or mix both approaches based on your needs.

## A single codebase that runs everywhere natively

Uno Platform apps run as a single codebase for native mobile, web, desktop, and embedded apps, utilizing the full breadth and reach of .NET. Uno Platform ships its releases in lock step with the latest .NET releases, including support for .NET 9 and .NET 10. You can always benefit from the latest and greatest advancements in the .NET world.

## Keep using your favorite IDE

Need we say more? Use the IDE that works for you – Visual Studio, VS Code, Rider as well as GitHub Codespaces. Wherever you are, Uno Platform is with you.

## Work from your favorite OS

Remain on the operating system that you are on. A detailed list of target platforms for which you can develop from Windows, macOS, or Linux is available. Our docs are designed to be OS agnostic, so every tutorial step works regardless of the system you develop on.

## Use either XAML or C# Markup for your app UI

With Uno Platform, you have a choice of building polished cross-platform UI with concise declarative UI markup using a modern XAML-centric syntax or declarative-style C# Markup. Both approaches are supported right out of the box. Our C# Markup approach works with both 1st party and 3rd party controls and application-level libraries, mapping directly to the underlying object model. All thanks to real-time C# source generators doing the heavy lifting for you.

## Hot Reload that just works

Our Hot Reload offers the most comprehensive solution for fast development loop – make a change and see it live everywhere is running in real-time without recompiling. Uno Platform Hot Reload works on:

- Visual Studio and Visual Studio Code
- All target platforms
- XAML, C# Markup, and C#
- Bindings & x:Bind, Resources, Data Templates, and Styles
- 1st party or 3rd party controls
- Devices and Emulators

For more information, see [Hot Reload](xref:Uno.Features.HotReload).

## Stop typing markup and use Figma code generation

Design handoff is one of the biggest time traps, as design envisioned by the designer needs to be manually translated to markup. With our [Figma plugin](xref:Uno.Figma.GetStarted)\*, this process is automatic, and the result is well-structured, performant XAML or C#. Uno Platform's Design-to-Code feature is part of **Uno Platform Studio**, providing enterprise-grade design handoff capabilities.

For more information, see [Design and Build Uno Platform Applications with Figma](xref:Uno.Figma.GetStarted).

\*_Note this plugin is optional_

## Quickly create a project using the Templates Wizard

Our [Uno Platform Template Wizard](https://platform.uno/blog/the-new-uno-platform-solution-template-wizard/) and its [live version](https://new.platform.uno/) allow you to get to a working project quickly. You can select your preferred version of .NET, choose the target platforms you want to develop for, pick a design system that suits your app the best, pick from the MVVM or MVUX architectural pattern, and throw in any mix of the rich set of Uno Extensions as starting building blocks. Additionally, you can include support for PWA, first-party API server, UI tests, and even generate initial build scripts for GitHub Actions or Azure DevOps, alleviating the hassle of figuring out the complex, cross-platform YAML specifics.

## Your apps will be pixel-perfect across all platforms

Uno Platform allows you to control each pixel of your UI elements to match the experience you envision. This concept of lookless-controls is very similar to what is named "[headless controls](https://martinfowler.com/articles/headless-component.html)" in the React world. Each built-in and third-party control defines its fundamental logic (e.g. how it responds to interactions, handles data, or behaves once a property value is set) independently of a visual style and template. This approach means you can tailor the appearance of any control to fit a special use case, or to match your brand identity. Changes to control styling can even be performed at runtime. Uno Platform leverages this to offer multiple built-in design systems influenced by guidance from popular platforms.

Under the hood, your app can use either a Native or Skia-based approach for rendering.

The Native rendering approach uses the built-in native UI primitives on each target, for iOS, Android, and WebAssembly. These build up a native view hierarchy and draw the visuals using native OS capabilities. That way, you still get all the benefits of the native world, such as localization and accessibility, but without giving up the rich control of pixel-level details in your app experience.

The [Skia-based](xref:uno.features.renderer.skia) rendering approach uses a Skia-based canvas for fast and rich rendering across platforms and gets the exact same behavior across all platforms. You can also use the Composition API to get advanced rendering and animations across platforms.

All of the above remains possible without needing to replicate the same design for each target platform.

## Multiple design systems included with support for dark mode

Uno Platform controls come pre-built with the Fluent design system, so your app looks the same across all platforms. The [Uno Themes](xref:Uno.Themes.Overview) library is available to provide alternative styles for your controls and help with adapting them to the other design systems Material Design or Cupertino. All three design implementations take advantage of [theme resources](https://learn.microsoft.com/windows/apps/design/style/xaml-theme-resources), enabling out-of-the-box support for light and dark color modes.

## Move forward gradually with Uno Islands

We recognize that many developers have existing WPF applications they want to modernize, and offer such a path via [Uno Islands](xref:Uno.Tutorials.UnoIslands), which allows you to host modern Uno Platform content within WPF UI. This means you can start modernizing your application and bringing it cross-platform step by step, not just all at once!

The _islands_ of hosted content enable any app to take advantage of modern WinUI controls, but preserve compatibility with its existing WPF experience. For example, an app using the list-details pattern can keep its robust, WPF-built details view while upgrading the list control to a WinUI `ListView`. Support for DataBinding between both contexts allows a seamless transition between the two.

## Native performance, never stuck

Uno Platform-built apps are **native apps**. As a developer, you can take advantage of each platform's capabilities and features while enjoying the flexibility to adapt and optimize as needed.

Uno Platform provides access to the **original APIs** provided by the target platform, both for UI and non-UI functionalities. You will be able to use the UI controls that come directly from the respective target platform's native SDK. This ensures that your app seamlessly incorporates the _familiar_ look and feel of the platform it's running on.

This versatility allows for a broader reach with compatibility across all supported platforms and devices.

Uno Platform provides "escape hatches" when needed. This means that you have the flexibility to utilize platform-specific features or optimizations when required, should that functionality not be provided by Uno Platform itself.

Uno Platform simply reuses default behaviors that come from the respective platform itself. This includes features like Input Method Editor (IME), spell checking, password manager integration, autofill, magnifier features (long press on textbox), accessibility features, voice-over, support for TTY (Teletypewriter) and native back swipe gestures.

The resulting app provides a consistent, native user experience your end-users expect.

## Breadth of UI controls – over 500 controls available

Uno Platform gives you automatic access to all controls coming from Microsoft and its 3rd party ecosystem. All controls from WinUI, Windows Community Toolkit, or open-source projects developed for WinUI 2/3 or UWP will work with Uno Platform.

## Controls from Windows Community Toolkit (WCT)

The Windows Community Toolkit (WCT) is a collection of helper functions, controls, and services designed to simplify and enhance Windows app development for developers. You can take those WCT features such as the `DataGrid` or `Expander` controls cross-platform when your project uses Uno Platform.

## More UI controls with MAUI Embedding

Uno Platform allows for embedding .NET MAUI-specific controls from all leading 3rd party vendors like Syncfusion, Grial Kit, Telerik, DevExpress, Esri, Grape City, and the .NET MAUI Community Toolkit. Keep in mind that this cross-platform approach works only for target platforms .NET MAUI reaches – iOS, Android, macOS, and Windows. [We have 6 sample apps](xref:Uno.Extensions.Maui.Overview) showing how to work with MAUI embedding by all leading 3rd party vendors.

> [!NOTE]
> Uno Platform is not built on top of .NET MAUI.

## Uno Toolkit

In addition to hundreds of UI controls available from 3rd party UI vendors as described above, we offer our own set of [higher-level UI Controls](https://platform.uno/uno-toolkit/) designed specifically for multi-platform, responsive applications.

## Consistent shadow effect everywhere

Displaying consistent shadows in cross-platform apps was always a problem. We are confident that we have solved it! Our [ShadowContainer control](xref:Toolkit.Controls.ShadowContainer) will allow you to easily display highly customizable inner and outer shadows in your apps. And if you fancy [neumorphism](xref:Toolkit.Controls.ShadowContainer#neumorphism) depth effects, we have control styles for that too!

## Maps

Uno Platform offers charting components via integration with [Maps UI](https://github.com/Mapsui/Mapsui) (Open Source) on all targets.

Also, for targeting just mobile, you can use [Esri ArcGIS Maps SDK for .NET](xref:Uno.Extensions.Maui.ThirdParty.EsriMaps) via our .NET MAUI embedding feature.

In addition, we are looking into integrating the new WinUI [MapControl](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.mapcontrol?view=windows-app-sdk-1.5) into our core offering.

## Charts

Uno Platform offers charting components via integration with [Live Charts](https://livecharts.dev/) (Open Source) and OxyPlot on all targets.

## DataGrid

Uno Platform offers DataGrid control via our support for Windows Community Toolkit (WCT). You can see the DataGrid implemented by going to [nuget.info](https://nuget.info/), which is the web implementation of the popular NuGet Package Explorer. Furthermore, you can see an implementation of it at [Uno Gallery](https://gallery.platform.uno/), then navigating to "Windows Community Toolkit" tab. Do note that you have options to [performance-tune your Uno Platform web apps](https://platform.uno/blog/optimizing-uno-platform-webassembly-applications-for-peak-performance/).

We will support Windows Community Toolkit [DataTable](https://github.com/CommunityToolkit/Labs-Windows/discussions/415) once it reaches RTM.

## Non-UI APIs

It is a common misconception that Uno Platform is just a UI framework. Nothing could be further from the truth, however! Uno Platform comes with [many non-UI APIs built in](xref:Uno.Features.Accelerometer) – ranging from various ways to retrieve device-related information, through access to user and application preferences, all the way to a range of sensors including GPS, compass, or even Accelerometer! All these APIs are fully cross-platform, so you just write once and run everywhere. For those who have a musical instrument, we even have MIDI!

How's _that_ for music to your ears?

## Our set of extensions for common app needs

[Uno.Extensions](https://platform.uno/uno-extensions/) is built on top of **Microsoft.Extensions** to setup and create a host for your application. The host allows access to a multitude of services for many app features including Configuration, Logging, Serialization, and HTTP. You can even register your own custom services using the same pattern.

## Navigation

As part of Uno.Extensions, Navigation provides a consistent abstraction for navigating within an application. Whether it is navigating to a new page, switching tabs, or opening a dialog, navigation can be initiated from code-behind, XAML, or from within a ViewModel.

## Authentication

Uno.Extensions Authentication is a provider-based authentication service that can be used to authenticate users of an application. It has built-in support for Microsoft Entra ID (formerly Azure AD), OpenIDConnect, Web, and Custom.

## Skia drawing

The Uno Platform provides access to SkiaSharp as a render canvas for your app, enabling rich support for fine-grained drawing primitives. Uno Platform also uses SkiaSharp to render the UI for X11, FrameBuffer, macOS, and Windows 7+ apps. Starting from Uno Platform 6.0, Skia rendering is also available for iOS, Android, and WebAssembly.

## Animations: Beyond storyboards, access to Lottie and Rive

Based on SkiaSharp support, Uno Platform provides AnimatedVisualPlayer to give the ability to render rich [Lottie files](https://airbnb.io/lottie/#/) directly in your app, for all target platforms.

## Performance and app size with AOT/Jiterpreter

Uno Platform allows you to use .NET 9+ features such as Profiled AOT (Ahead Of Time compilation) and the Jiterpreter, to get better performance for your apps with balanced size. Profiled AOT is a powerful feature that allows you to continue using the interpreter for code that is not often used, thus keeping your app's size down while maintaining excellent performance.

## Comprehensive App Packaging for All Platforms

Uno Platform offers comprehensive [app packaging solutions](xref:uno.publishing.overview), allowing you to build, package, and deploy your applications with ease across all supported platforms. Whether you're targeting mobile app stores, desktop environments, or web hosting, our built-in packaging support streamlines your release process. The platform handles all the platform-specific packaging requirements, enabling you to focus on your application logic instead of deployment complexities.

## Accessibility included

Uno Platform comes with built-in support for accessible apps, such as font scaling and support for screen readers. Support is provided for Windows Narrator, Android TalkBack, Apple VoiceOver, and browser-specific narrators when targeting the web. In addition, Uno Platform provides programmatic access to most UI elements, enabling assistive technology products.

## Localization included

Uno Platform supports localization from the `x:Uid` feature in XAML, as well as using resource files from `ResourceManager` in plain C# code. Localization is done once for all your target platforms.

## XAML and Resources Trimming

[XAML and Resources Trimming](xref:Uno.Features.ResourcesTrimming) is a feature that allows for trimming the size of WebAssembly, Desktop and iOS apps down by removing XAML styles and  that are not used in your app, helping to bring down the size of your application.

## Lightweight styling

Lightweight styling is a way to customize the appearance of XAML controls by overriding their default brushes, fonts, and numeric properties. Lightweight styles are changed by providing alternate resources with the same key. All Uno Material styles support the capability to be customized through resource overrides without the need to redefine the style.

Overriding resources from Uno Material can be done at the app, page, or even control level.

## Easily manage and scale image assets

[Resizetizer](xref:Uno.Resizetizer.GettingStarted) is a set of MSBuild tasks designed to manage an application's assets. With this package, there is no need to worry about creating and maintaining various image sizes or setting up a splash screen. Simply provide an SVG file, and the tool will handle everything else.

## Support for SVG

Today's apps run on such a wide range of displays, it is sometimes hard to make sure your icons always look crisp and sharp. To counter this, Uno Platform has a built-in support for the vector-based SVG format!

## Progressive Web Apps (PWAs)

Your Uno Platform WebAssembly app can run as a Progressive Web App. This means users can install it and run as a normal application on their device. In addition, this gives you even more customization options and additional PWA-specific APIs like badge notifications. You can install the [NuGet Package Explorer](https://nuget.info/) PWA like a local app!

## WebView across platforms

When you need to display web content within your cross-platform apps, you can use our `WebView2` control across all Uno Platform targets, at no additional cost. Not only will it easily handle rendering of both remote and in-app HTML content, but it will also allow you to implement bi-directional JavaScript communication without any hassle. We are working on an implementation for the remaining Skia targets.

## Media Player across platforms

Uno Platform provides - at no additional cost - the `MediaPlayerElement` control, which allows your app to play media files back in your app. It is supported on Linux through libVLC.

## Monaco Editor on WebAssembly

For all your in-app text and code editing needs, you can use our port of the Monaco Editor – the same one that is used by Visual Studio Code. With full support for syntax highlighting, you could even build your own WebAssembly IDE!

## Mix and match XAML controls with native components

Because Uno Platform apps are native mobile apps, you can embed any native control on a per platform basis into an Uno Platform app.

Uno Platform views inherit from the base native view defined in .NET binding or the native framework itself, you can incorporate native views into your app's visual tree. For more information, see [UnoConf 2021 - Using Native Controls in Uno Platform Apps](https://www.youtube.com/watch?v=4Cwzk8dDHs0).

Your app's powerful visual tree can also render native components through embedding in Skia targets.

## Choice of presentation framework

With Uno Platform, you get a choice of your favorite presentation framework – MVVM, Prism, or Uno Platform in-box MVUX/Reactive.

## Support for SQLite

Uno Platform supports SQLite for all platforms, including WebAssembly, to allow for your app to use offline or local caching techniques.

## Proven migration path for Xamarin.Forms

In addition to providing 3rd party control MAUI Embedding, Uno Platform is a proven modernization path for Xamarin.Forms apps. We've collated numerous [docs](https://platform.uno/xamarin.forms) to aid you in the migration. Big brands such as **TOYOTA** have already migrated some of their apps to Uno Platform.

> [!NOTE]
> Uno Platform is not built on top of .NET MAUI.

Watch TOYOTA's [video testimonial](https://youtu.be/TeA6zEq5MGk?t=1449).

## Standing on the shoulders of giants

Uno Platform was designed to be source compatible with the APIs of WinUI. This gives us the great advantage of being able to port existing code for even the most complex controls directly from Microsoft, preserving all their feature richness. And, because WinUI sources are now publicly available, everyone from the community can help!

## Developed by App Builders, for App Builders

We are first and foremost app builders who have built hundreds of applications for consumer brands via our sister company – nventive. We use our platform and tooling daily (dog fooding) and understand the pains of app development first-hand.

## Used to build countless apps for over a decade

Uno Platform started as an internal project at Nventive over 10 years ago, which we have used to develop [countless apps for top worldwide-recognized brands](https://nventive.com/en/our-work). In addition, many Fortune 500 companies use Uno Platform for their internal, line of business applications. Some of those can be found on the [Uno Platform case studies](https://platform.uno/case-studies/) page. This production-proven track record demonstrates that Uno Platform is not just experimental—it's enterprise-ready and battle-tested at scale.

## Support: Community and Paid

We are well known for a friendly and welcoming community which is always willing to help. If you need support time directly from the core team, there are also paid plans available which is how we sustain Uno Platform, a process explained in the [Sustaining the Open-Source Uno Platform blog post](https://platform.uno/blog/sustaining-the-open-source-uno-platform/).

To see your free & paid support options, see our official [support page](https://platform.uno/support/).

## Workshops

Getting started is easy with our workshops that will give you all the necessary intro into cross-platform app development. And the fact that we have multiple workshops means you can pick and choose based on the time you have available and the level of difficulty! Give our [Simple Calculator](https://platform.uno/simple-calc/) or [Tube Player](https://platform.uno/tube-player/) workshop a go.

## Books/Content

XAML for UI is a mature & well documented technology – you'll always find an example suiting your needs. In addition, there are [dozens of training courses and books](https://platform.uno/blog/uno-platform-learning-resources-2023-update/) written to help you learn Uno Platform.

## Fast ship cadence

Uno Platform ships official releases 6-8 times per year, and dozens of dev builds in between. This flexibility has traditionally allowed us to ship on day-0 alongside .NET, WinUI, and Windows App SDK.

## Amazing team behind it

As Uno Platform grows, the team behind it grows as well. The product is in good hands, with a group of focused and highly motivated experts in many areas. The project thrives because of this, as new features and improvements are added daily.

## Integrates with any .NET Library

In addition to the UI components mentioned above, Uno Platform apps also integrate perfectly with your existing .NET libraries. This includes both .NET Standard 2.0 and libraries targeting .NET 6 through .NET 10 and beyond. Because of this, your Uno Platform app has compatibility with packages like Json.NET, LiteDB, SQLite, SkiaSharp, and the vast NuGet ecosystem.

## The Community

Welcoming, tight, and helpful community on GitHub Discussions and Discord, with over 300 contributors to the project. Popular with over 70,000,000 NuGet downloads of its various packages and is used among the [biggest companies](https://platform.uno/case-studies/), including Fortune 500 enterprises. Our core team is always ready to help, and the community provides excellent support for developers at all skill levels.

## Wealth of Samples

To show Uno Platform's versatility, we tinker around with technology and share with you our full-blown apps and code samples that you can learn from.

Check out: [Cross-Platform Sample Apps with XAML and C# - Uno Platform](https://platform.uno/code-samples/)
