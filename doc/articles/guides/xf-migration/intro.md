---
uid: Uno.XamarinFormsMigration.Intro
---

# Uno Platform for Xamarin.Forms developers

With an announced [end of support](https://dotnet.microsoft.com/platform/support/policy/xamarin#microsoft-support) for Xamarin.Forms, you should start the process of migrating to the Uno Platform as soon as possible. This page expands on the previous [section](xref:Uno.XamarinFormsMigration.Overview) section to offer more context about the Uno Platform.

## Understanding our approach

Uno Platform is designed for compatibility with Microsoft's native UI framework [WinUI](https://learn.microsoft.com/windows/apps/winui/winui3/). It achieves this by implementing the same controls, panels, and other non-UI APIs across the other supported platforms: iOS, Android, MacCatalyst, Web and Linux. While Xamarin.Forms also supports reuse across platforms, it differs from Uno Platform architecturally as a type of abstraction.

In the Xamarin.Forms space, it would be difficult to miss the differing behaviors and properties inherited by your app UI depending on the platform. This occasionally requires more boilerplate code, with less-natural methods to customize views for your brand identity. Migrating to Uno Platform is a step forward, as its appreciation for consistency in appearance and behavior across platforms allows your app to embody a precise brand identity.

### Battle-tested for over a decade

When platforms, albeit in their infancy, invested in a robust set of first-party controls for app developers, it proved to be vital for acclimating users to their devices' unfamiliar interaction paradigms. This strategy to catalyze the adoption of mobile devices influenced traditional platforms to prioritize a similar endeavor.

Microsoft significantly evolved the visual layer for Windows devices by launching WinRT XAML in 2012 and has expanded the compatibility of its modern UI framework with the launch of WinUI 3 in 2021.

The creation of Uno Platform was driven by the aspiration to make pixel-perfect apps possible across all platforms. This is achieved by implementing the same controls, panels, and other constructs as WinUI.

## Architectural differences

Xamarin.Forms works under the hood by abstracting a set of analogous features and components from each supported platform, yet the customization options for shared UI elements are limited for the sake of consistency. Your Uno Platform views also map to native controls but do so at a lower level. Despite these apparent differences, the XAML flavor used by Xamarin.Forms is similar enough to Uno Platform's that migrating your team's existing investment has an ample amount of familiarity.

Still, it is not a one-to-one mapping. Adjustments to your app will be needed to benefit from over a decade of investment in responsive design, customization, accessibility, and more. This series was designed as a straightforward guide to help your team migrate their existing app.

> [!TIP]
> Before moving on, we recommend setting up your development environment. See [Get Started](xref:Uno.GetStarted) for the steps to install the prerequisites to build and run Uno Platform projects throughout your migration process.

## Next step

By now, you should have a better understanding of how Uno Platform is a perfect fit to modernize existing Xamarin.Forms apps. The next sections focus on how to bring forward your **custom controls**, as well as the features they use to enable a great experience.

Read on to learn how to migrate **animations**, **navigation**, **data binding**, and more!

**[Previous](xref:Uno.XamarinFormsMigration.Overview) | [Next](xref:Uno.XamarinFormsMigration.Overview#whats-covered)**
