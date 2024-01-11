---
uid: Uno.Development.MigratingFromXamarinToNet6
---
# How to upgrade from Xamarin to .NET 7

There are two ways to migrate an application to .NET 7.

## The fastest path to keep using the UWP APIs

   1. In a separate folder, create a new application from the templates using [`dotnet new unoapp-uwp-net6 -o MyApp`](get-started-dotnet-new.md) (MyApp being of the same name as your existing app),
   2. Move the `MyApp.Mobile` project folder and include it in your existing solution
   3. Make the adjustments for your new application head to build
   4. Optionally remove the legacy Xamarin heads, once you're done with the migration

## The forward looking path using the WinUI/WinApp SDK APIs

   1. In a separate folder, create a new application from the templates using [`dotnet new unoapp -o MyApp`](get-started-dotnet-new.md) (MyApp being of the same name as your existing app), or by using the Visual Studio `Uno Platform App` template
   2. Move the `MyApp.Mobile` project folder and include it in your existing solution
   3. Make the adjustments to move from the [UWP APIs to the WinUI APIs](updating-to-winui3.ms)
   4. Optionally [convert your legacy Xamarin heads](updating-to-winui3.md) to use Uno.WinUI instead of Uno.UI

## Additional considerations

Since .NET 7 breaks binary compatibility with Xamarin, most of the existing nuget packages that target `monoandroidXX`, `xamarinios10` and `xamarinmac20` (bindings to native SDKs are a good example) will not work properly and will need an updated version that are compatible with `net7.0-ios`, `net7.0-android`, `net7.0-maccatalyst` and `net7.0-macos`.

## Xamarin Support Policy

As defined by [Microsoft support policy](https://dotnet.microsoft.com/platform/support/policy/xamarin) for Xamarin (as of 2022-08-25):

> _Xamarin support will end on May 1, 2024 for all Xamarin SDKs. Android 13 and Xcode 14 SDKs (iOS and iPadOS 16, macOS 13) will be the final versions Xamarin will target._

Uno Platform is going provides support for Xamarin (`monoandroid12`, `monoandroid13`, `xamarinios10` and `xamarinmac20`) by that date as well in the Uno 4.x releases. Uno 5.0 and later does not support Xamarin anymore. Note that later on, apps built with these SDK will not be accepted by both [Apple](https://developer.apple.com/support/xcode/) and [Google](https://developer.android.com/google/play/requirements/target-sdk) stores.
