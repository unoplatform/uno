---
uid: Uno.Overview.WhyUno
---

# Why use Uno Platform for your project?

Thank you for considering Uno Platform for your next project. While we always recommend trying out any stack before deciding on your next project, here are some tips you can use for your in-depth evaluation.

## It's a platform, not just a UI framework

Uno Platform is a developer productivity **platform** and much **more than a UI framework**, for building single codebase, native, mobile, web, desktop, and embedded apps with .NET.

At the core is a cross-platform .NET UI framework, that allows apps to run everywhere with a single codebase.

However, built on top of this foundation is also a rich platform which includes libraries, extensions, and tools that accelerate the design, development, and testing of cross-platform applications.

## It's Free and Open-Source

Uno Platform is free and open source under Apache 2.0.

It is well funded and has a [sustainability model](https://platform.uno/blog/sustaining-the-open-source-uno-platform/) built in so that the project is sustainable in the long term. In addition, it is fueled by support from a thriving community of users who regularly send feedback and contribute.

## Same code based that runs on iOS, Android, Web, MacOS, Linux, and Windows

Uno Platform apps run as a single codebase for native mobile, web, desktop, and embedded apps, utilizing the full breadth and reach of .NET. Uno Platform ships its releases in lock step with the latest .NET releases. So, you can always benefit from the latest and greatest advancements in the .NET world.

## You can use your favorite IDE (Visual Studio, VS Code, or Rider)

Need we say more? Use the IDE that works for you – Visual Studio, VS Code, Rider as well as CodeSpaces & GitPod. Wherever you are, Uno Platform is with you.

## You can work from your favorite OS (Windows, Mac, Linux)

Remain on the operating system that you are on. A detailed list of target platforms for which you can develop from Windows, Mac, or Linux is available. Our docs are also OS agnostic, so you can follow all tutorials regardless of the system you develop on.

## Use either XAML or C# Markup for your app UI

With Uno Platform, you have a choice of building polished cross-platform UI with concise declarative UI markup using a modern XAML-centric syntax or declarative-style C# Markup. Both approaches are supported right out of the box. Our C# Markup approach works with both 1st party and 3rd party controls and application-level libraries, mapping directly to the underlying object model. All thanks to real-time C# source generators doing the heavy lifting for you.

## Hot Reload that just works

Our Hot Reload offers the most comprehensive solution for fast development loop – make a change and see it live everywhere is running in real-time without recompiling. Uno Platform Hot Reload works on:

- Visual Studio and Visual Studio Code
- All target platforms
- XAML, C# Markup, and C#
- Bindings & x:Bind, Resources, Data Templates and Styles
- 1st party or 3rd party controls
- Devices and Emulators

See official [docs](xref:Uno.Features.HotReload).

## Stop typing markup and use Figma code generation

Design handoff is one of the biggest time traps, as design envisioned by the designer needs to be manually translated to markup. With our plugin* this process is automatic, and the result is well structured, performant XAML or C#.

[Design and Build Uno Platform Applications with Figma](xref:Uno.Figma.Overview.GetStarted)

*_Note this plugin is optional_

## Ease of Use: Quickly create and customize an app with the Templates Wizard

Our [Uno Platform Template Wizard](https://platform.uno/blog/the-new-uno-platform-solution-template-wizard/) and its [live version](https://new.platform.uno/) allow you to get to a working project quickly. You can select your preferred version of .NET, choose the target platforms you want to develop for, pick a design system that suits your app the best, pick from the MVVM or MVUX design pattern, and throw in any mix of the rich set of Uno Extensions as starting building blocks. Additionally, you can include support for PWA, first-party API server, UI tests and even generate initial build scripts for GitHub Actions or Azure DevOps, alleviating the hassle of figuring out the complex, cross-platform YAML specifics.

## Your apps will be Pixel-Perfect across all platforms and look exactly the way you want them to look

Uno Platform allows you to control each pixel using the concept of lookless-controls, similar to the "[headless controls](https://martinfowler.com/articles/headless-component.html)" term used in React world. Each built-in and even third-party control defines its logic (e.g. how the control reacts to pointer or keyboard input, how it handles data, or how different properties affect its behavior) independently of its visual style and template. This means you can completely change the look of each control as you see fit for every specific use case or to match your brand. You can even change the style of the control at runtime. Thanks to this, we can also offer two different built-in design systems – Fluent Design System based on Microsoft's design language and Material Design based on Google's design language – and both in light and dark theme!

Under the hood, your app is still native – we are still using the built-in native UI primitives on each target which build up a native view hierarchy and draws the visuals using native OS capabilities. This way you still get all the benefits of the native world such as localization and accessibility, while
you still have full control of every pixel of your app and only need to design your application once.

## Out of the box support for Material/Fluent/Cupertino design systems with Dark/Light themes

Uno Platform controls come pre-built with the Fluent design system, so your app looks the same across all platforms. The [Uno Themes](xref:Uno.Themes.Overview) library is available to provide alternative styles for your controls and help with adapting them to the other design systems Material Design or Cupertino. All three design implementations take advantage of [theme resources](https://learn.microsoft.com/windows/apps/design/style/xaml-theme-resources), enabling out-of-the-box support for light and dark color modes.

## Continuous modernization with Uno Islands

We recognize that many developers have existing WPF applications they want to modernize, and offer such a path via [Uno Islands](xref:Uno.Tutorials.UnoIslands), which allows you to host modern Uno Platform content within WPF UI. This means you can start modernizing your application and bringing it cross-platform step by step, not just all at once!

## Native: Performance + Gestures + Never stuck

Uno Platform-built apps are **native apps**. As a developer, you can take advantage of each platform's capabilities and features while enjoying the flexibility to adapt and optimize as needed.

Uno Platform provides access to the **original APIs** provided by the target platform, both for UI and non-UI functionalities. You will be able to use the UI controls that come directly from the respective target platform's native SDK. This ensures that your app seamlessly incorporates the _familiar_ look and feel of the platform it's running on.

You can tap into the richness of the ecosystems on the Web, iOS, and Android, Linux and Skia. This versatility allows for a broader reach and compatibility across different devices and platforms.

Uno Platform provides "escape hatches" when needed. This means that you have the flexibility to utilize platform-specific features or optimizations when required, should that functionality not be provided by Uno Platform itself.

Uno Platform simply reuses default behaviors that come from the respective platform itself. This includes features like Input Method Editor (IME), spell checking, password manager integration, autofill, magnifier features (long press on textbox), accessibility features, voice-over, support for TTY (Teletypewriter) and native back swipe gestures.

The resulting app provides a consistent, native user experience your end-users expect.

## Rich/Breadth of UI Controls – over 500 controls available

Uno Platform gives you automatic access to all controls coming from Microsoft and its 3rd party ecosystem. All controls from WinUI, Windows Community Toolkit or open-source projects supporting WinUI or UWP will work with Uno Platform.

## Controls from Windows Community Toolkit (WCT)

The Windows Community Toolkit (WCT) is a collection of helper functions, controls, and services designed to simplify and enhance Windows app development for developers. You can reuse the richness of controls offered in WCT such as Data Grid or Expanders and use them in cross-platform fashion on Web, mobile, desktop and Linux targets.

## More UI controls with MAUI Embedding

Uno Platform allows for embedding .NET MAUI-specific controls from all leading 3rd party vendors like Syncfusion, Grial Kit, Telerik, DevExpress, Esri, Grape City, and the .NET MAUI Community Toolkit. Keep in mind that this cross-platform approach works only for target platforms .NET MAUI reaches – iOS, Android, MacOS, and Windows. [We have 6 sample apps](xref:Uno.Extensions.Maui.Overview) showing how to work with MAUI embedding by all leading 3rd party vendors.

## Uno Toolkit

In addition to hundreds of UI controls available from 3rd party UI vendors as described above, we offer our own set of [higher-level UI Controls](https://platform.uno/uno-toolkit/) designed specifically for multi-platform, responsive applications.

## Consistent shadow effect everywhere

Displaying consistent shadows in cross-platform apps was always a problem. We are confident that we have solved it! Our [ShadowContainer control](xref:Toolkit.Controls.ShadowContainer) will allow you to easily display highly customizable inner and outer shadows in your apps. And if you fancy [neumorphism](xref:Toolkit.Controls.ShadowContainer#neumorphism) depth effects, we have control styles for that too!

## Maps

Uno Platform offers charting components via integration with [Maps UI](https://github.com/Mapsui/Mapsui) (Open Source) on all targets.

Also, for targeting just Mobile and MacCatalyst, you can use [Esri ArcGIS Maps SDK for .NET](xref:Uno.Extensions.Maui.ThirdParty.EsriMaps) via our .NET MAUI embedding feature.

In addition, we are looking into integrating the new WinUI [MapControl](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.mapcontrol?view=windows-app-sdk-1.5) into our core offering.

## Charts

Uno Platform offers charting components via integration with [Live Charts](https://livecharts.dev/) (Open Source) and OxyPlot on all targets.

## DataGrid

Uno Platform offers DataGrid control via our support for Windows Community Toolkit (WCT). You can see the DataGrid implemented by going to [nuget.info](https://nuget.info/), which is the web implementation of the popular NuGet Package Explorer. Furthermore, you can see an implementation of it at [Uno Gallery](https://gallery.platform.uno/), then navigating to "Windows Community Toolkit" tab. Do note that you have options to [performance-tune your Uno Platform web apps](https://platform.uno/blog/optimizing-uno-platform-webassembly-applications-for-peak-performance/).

We will support Windows Community Toolkit [DataTable](https://github.com/CommunityToolkit/Labs-Windows/discussions/415) once it reaches RTM.

## Non-UI APIs

It is a common misconception that Uno Platform is just a UI framework. Nothing could be further from the truth, however! Uno Platform comes with [many non-UI APIs built in](xref:Uno.Features.Accelerometer) – ranging from various ways to retrieve device-related information, through access to user and application preferences, all the way to a range of sensors including GPS, compass, or even Accelerometer! All these APIs are fully cross-platform, so you just write once and run everywhere. For those who have a musical instrument, we even have MIDI!

How's _that_ for music to your ears?

## Hosting-based extensions for everything an app needs (HTTP, Serialization, Configuration, etc)

Uno.Extensions is built on top of Microsoft.Extensions to setup and create a host for your application that can be used to access services for Logging, Configuration, Serialization, Http and your own custom services.

## Navigation

As part of Uno.Extensions, Navigation provides a consistent abstraction for navigating within an application. Whether it is navigating to a new page, switching tabs or opening a dialog, navigation can be initiated from code-behind, XAML or from within a ViewModel.

## Authentication

Uno.Extensions Authentication is a provider-based authentication service that can be used to authenticate users of an application. It has built-in support for Microsoft Entra ID (formerly Azure AD), OpenIDConnect, Web and Custom.

## Skia drawing

The Uno Platform provides access to SkiaSharp as a render canvas for your app, enabling rich support to fine grained drawing primitives. Uno Platform also uses SkiaSharp to render the UI for Gtk/WPF/Framebuffer apps.

## Animations: Beyond storyboards, access to Lottie and Rive

Based on SkiaSharp support, Uno Platform provides AnimatedVisualPlayer to give the ability to render rich Lottie files directly in your app, for all target platforms.

## Performance and App Size with AOT/Jiterpreter

Uno Platform allows to use .NET 8 features such as Profiled AOT (Ahead of Time compilation) and the Jiterpreter, to get better performance for your apps and balanced size. Profiled AOT is a powerful feature that allows to continue using the interpreter for code that is not often used, thus keeping your app's size down.

## Accessibility included

Uno Platform comes with built-in support for accessible apps such as font scaling and support for screen readers. Support is provided for Windows Narrator, Android TalkBack, iOS and macOS VoiceOver and browser-specific narrators for Web / WebAssembly based apps. In addition, Uno Platform provides programmatic access to most UI elements, enabling enables assistive technology products.

## Localization included

Uno Platform supports localization from the `x:Uid` feature in XAML, as well as using resource files from `ResourceManager` in plain C# code. Localization is done once for all your target platforms.

## XAML Trimming

XAML Trimming is a feature that allows for trimming the size of WebAssembly apps down by removing XAML styles that are not used in your app, helping to bring down the size of your application.

## Lightweight styling

Lightweight styling is a way to customize the appearance of XAML controls by overriding their default brushes, fonts, and numeric properties. Lightweight styles are changed by providing alternate resources with the same key. All Uno Material styles support the capability to be customized through resource overrides without the need to redefine the style.

Overriding resources from Uno Material can be done at the app, page, or even control level.

## Resizetizer for easy app images management

[Resizetizer](xref:Uno.Resizetizer.GettingStarted) is a set of MSBuild tasks designed to manage an application's assets. With this package, there is no need to worry about creating and maintaining various image sizes or setting up a splash screen. Simply provide an SVG file, and the tool will handle everything else.

## Support for SVG

Today's apps run on such a wide range of displays, it is sometimes hard to make sure your icons always look crisp and sharp. To counter this, Uno Platform has a built-in support for the vector-based SVG format!

## Progressive Web Apps (PWAs)

Your Uno Platform WebAssembly app can run as a Progressive Web App. This means users can install it and run as a normal application on their device. In addition, this gives you even more customization options and additional PWA-specific APIs like badge notifications. You can install the [Nuget Package Explorer](https://nuget.info/) PWA like a local app!

## WebView across platforms

When you need to display web content within your cross-platform apps, you can use our `WebView2` control. Not only will it easily handle rendering of both remote and in-app HTML content, but it will also allow you to implement bi-directional JavaScript communication without any hassle. Note that `WebView2` is currently available on mobile and WinAppSDK targets. We are working on an implementation for Skia targets.

## Media Player across platforms

Uno Platform provides the `MediaPlayerElement` control which allows your app to play media files back in your app. It is supported on Linux through libVLC.

## Monaco Editor on Wasm

For all your in-app text and code editing needs you can use our port of the Monaco Editor – the same that is used by Visual Studio Code. With full support for syntax highlighting, you could even build your
own WebAssembly IDE!

## Migration path with WPF Uno Islands

Uno Platform's support for Skia powers integration with WPF. It allows an existing WPF application to include "islands" or portions of the app hosted using Uno Platform Islands and displaying WinUI controls. For instance, an app using a master-details pattern can show a WPF `ListView` as the master view and the details view as a WinUI control. In addition, DataBinding between both contexts is supported, allowing for a seamless transition between the two.

## Mix and Match XAML controls and Native components at will

Because Uno Platform apps are native mobile apps, you can embed any native control on a per platform basis into an Uno Platform app.

Uno Platform views inherit from the base native view defined in .NET binding or the native framework itself, you can incorporate native views into your app's visual tree. You can watch this example [video](https://www.youtube.com/watch?v=4Cwzk8dDHs0).

Your app's powerful visual tree can also render native components through embedding in Skia targets.

## Choice of presentation framework

With Uno Platform you get a choice of your favorite presentation framework – MVVM, Prism or Uno Platform in-box MVUX/Reactive.

## Support for SQLite

Uno Platform supports SQLite for all platforms, including WebAssembly, to allow for your app to use offline or local caching techniques.

## Proven migration path for Xamarin.Forms

In addition to providing 3rd party control MAUI Embedding, Uno Platform is a proven modernization path for Xamarin.Forms apps. We've [collated numerous docs](https://platform.uno/xamarin.forms) to aid you in the migration. Big brands such as **TOYOTA** have already migrated some of their apps to Uno Platform.

Watch [TOYOTA's video testimonial](https://youtu.be/TeA6zEq5MGk?t=1449).

## Standing on the shoulders of giants

Uno Platform was designed to be source compatible with the APIs of WinUI. This gives us the great advantage of being able to port existing code for even the most complex controls directly from Microsoft, preserving all their feature richness. And, because WinUI sources are now publicly available, everyone from the community can help!

## Developed by App Builders, for App Builders

We are first and foremost app builders who have built hundreds of applications for consumer brands via our sister company – nventive. We use our platform and tooling daily (dog fooding) and understand the pains of app development first-hand.

## Used to build countless apps for over a decade

Uno Platform started as an internal project at Nventive over 10 years ago, which we have used to develop [countless apps for top worldwide-recognized brands](https://nventive.com/en/our-work). In addition, many fortune-500 companies use Uno Platform for their internal, line of business applications, some of which you can find on the [Uno Platform case studies](https://platform.uno/case-studies/) page.

## Support: Community and Paid

We are well known for a friendly and welcoming community which is always willing to help. If you are needing support time directly from the core team, there are also paid plans available which is how we sustain Uno Platform, a process explained with this [blog post](https://platform.uno/blog/sustaining-the-open-source-uno-platform/).

To see your free & paid support options see our official [support page](https://platform.uno/support/).

## Workshops

Getting started is easy with our workshops that will give you all the necessary intro into cross-platform app development. And the fact that we have multiple workshops means you can pick and choose based on the time you have available and the level of difficulty! Give our [Simple Calculator](https://platform.uno/simple-calc/) or [Tube Player](https://platform.uno/tube-player/) workshop a go.

## Books/Content

XAML for UI is a mature & well documented technology – you'll always find an example suiting your needs. In addition, there are [dozens of training courses and books](https://platform.uno/blog/uno-platform-learning-resources-2023-update/) written to help you learn Uno Platform.

## Fast ship cadence

Uno Platform ships official releases 6-8 times per year, and dozens of dev builds in between. This flexibility has traditionally allowed us to ship on day-0 as .NET and Windows UI / Windows App SDK.

## Amazing team behind it

As Uno Platform grows, the team behind it grows as well. It is a group of focused and highly motivated experts in many areas, which ensures the product is in good hands and thrives with new features and improvements being added (literally!) every day.

## Integrates with any .NET Library

Be it UI components like mentioned above, Uno Platform also integrates perfectly with any .NET Standard 2.0, .NET 6 through 8 libraries such as SQLite, LiteDB, Json.NET and Skia.

## The Community

Welcoming, tight, and helpful community on GitHub discussions and Discord, with over 300 contributors to the project. Popular with over 60,000,000 NuGet downloads of its various packages and is used among the [biggest companies](https://platform.uno/case-studies/). Our core team is always ready to help!

## Wealth of Samples

To show Uno Platform's versatility, we tinker around with technology and share with you our full-blown apps and code samples that you can learn from.

Check out: [Cross-Platform Sample Apps with XAML and C# - Uno Platform](https://platform.uno/code-samples/)
