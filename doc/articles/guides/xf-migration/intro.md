---
uid: Uno.XamarinFormsMigration.Intro
---

# Uno Platform for Xamarin.Forms developers

With an imminent end of [support](https://dotnet.microsoft.com/platform/support/policy/xamarin#microsoft-support) date in May 2024, it is important for your team to consider a migration from Xamarin.Forms to another framework. This page expands on the previous [section](xref:Uno.XamarinFormsMigration.Overview) section to offer more context about why your team should consider Uno Platform.

## Understanding our approach

Uno Platform is designed for compatibility with Microsoft's native UI framework [WinUI](https://learn.microsoft.com/windows/apps/winui/winui3/). It achieves this by implementing the same controls, panels, and other constructs. While Xamarin.Forms also supports reuse across platforms, it differs from Uno Platform architecturally as a type of abstraction. 

In the Xamarin.Forms space, it would be difficult to miss the differing behaviors and properties inherited by your app UI depending on the platform. This occasionally requires more boilerplate code, with less-natural methods to customize views for your brand identity. Migrating to Uno Platform is a step forward, as its appreciation for consistency in appearance and behavior across platforms allows your app to embody a precise brand identity.

### Battle-tested for over a decade

When platforms, albeit in their infancy, invested in a robust set of first-party controls for app developers, it proved to be vital for acclimating users to their devices' unfamiliar interaction paradigms. This strategy to catalyze the adoption of mobile devices influenced traditional platforms to prioritize a similar endeavor. 

Microsoft significantly evolved the visual layer for Windows devices by launching WinRT XAML in 2012, and has expanded the compatibility of its modern UI framework with the launch of WinUI 3 in 2021. 

The creation of Uno Platform was driven by the aspiration to make pixel-perfect apps possible across platforms. This is achieved by implementing the same controls, panels, and other constructs as WinUI.

## Architectural differences

Xamarin.Forms works under the hood by abstracting a set of analogous features and components from each supported platform, yet the customization options for shared UI elements are limited for the sake of consistency. Your Uno Platform views also map to native controls but do so at a lower level. Despite these apparent differences, the markup language flavor used by Xamarin.Forms is similar enough to Uno Platform's that migrating your team's existing investment is a largely straightforward process with familiar aspects.

Still, it is not a one-to-one mapping. Adjustments to your app will be needed to benefit from over a decade of investment in responsive design, customization, accessibility, and more. The following sections will help your team migrate key aspects of your existing Xamarin.Forms app including controls, animations, and navigation.

> [!TIP]
> Before moving on, we recommend setting up your development environment. See [Get Started](xref:Uno.GetStarted) for the steps to install the prerequisites to build and run Uno Platform projects throughout your migration process.

## Next step: See what's covered
[![button](assets/NextButton.png)](xref:Uno.XamarinFormsMigration.Overview#whats-covered)
