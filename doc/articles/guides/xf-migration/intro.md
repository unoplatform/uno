---
uid: Uno.XamarinFormsMigration.Intro
---

# Uno Platform for Xamarin.Forms developers

## Our approach

Uno Platform is designed for compatibility with Microsoft's native UI framework [WinUI](https://learn.microsoft.com/windows/apps/get-started/uno-simple-photo-viewer). It achieves this by implementing the same controls, panels, and other constructs. While Xamarin.Forms also supports reuse across platforms, it differs from Uno Platform architecturally as a type of abstraction. 

The former _does_ preserve platform-specific behavior, but often requires more boilerplate code with less ability to fine-tune UI elements to your brand's identity. Migrating to Uno Platform is a step forward, ensuring your applications stay relevant, performant, and maintainable in the evolving .NET ecosystem. Uno Platform's appreciation for consistency in appearance and behavior across platforms allows your app to embody a precise brand identity.

### Architectural details

Xamarin.Forms works under the hood by abstracting a set of analogous features and components from each supported platform, yet the customization options for shared UI elements are limited for the sake of consistency. Your Uno Platform views also map to native controls, but does so at a lower level. Despite these apparent differences, the markup language flavor used by Xamarin.Forms is similar enough to Uno Platform's that migrating your team's existing investment is a largely straightforward process with familiar aspects.

Still, it is not a one-to-one mapping. Adjustments to your app will be needed to benefit from over a decade of investment in responsive design, customization, accessibility and more. 

## Scoping the migration

Mobile platforms, such as iOS and Android, invested in a robust set of first-party controls that developers could use to build apps. Not only did this strategy acclimate users with mobile interaction paradigms, but it also became an early catalyst for today's app ecosystem. This success prompted traditional platforms to evolve their own visual layers. Microsoft introduced WinRT XAML in 2012, with the goal of enabling developers to build a new wave of rich, responsive app experiences.

