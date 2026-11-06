---
uid: Uno.Development.MigratingToUno7
---

# Migrating to Uno Platform 7.0

Uno Platform 7.0 removes the **native UI rendering backends** (native Android Views,
native iOS/tvOS UIKit, and the native WebAssembly DOM renderer) and makes
**Skia the single, implicit rendering engine on every target**. It also removes the
**Mac Catalyst** target framework entirely.

Skia already runs on every platform — desktop (Win32, X11, macOS and the Linux framebuffer),
Skia-on-Android, Skia-on-iOS, and Skia-on-WebAssembly. In 7.0 it becomes the *only* UI
rendering path: a `UIElement` is a plain managed object backed by a `Composition.Visual`
on all platforms, drawn into a single Skia surface.

This does **not** drop platform support: Android, iOS, macOS, Windows, Linux, and
WebAssembly all remain supported — they now all render with Skia. The one exception is
**Mac Catalyst**, which is removed as a target framework; macOS is served by the
`net10.0-desktop` head.

> [!IMPORTANT]
> This is a hard removal in a single major version — there is no `[Obsolete]` interim.
> Plan to recompile every Uno Platform library against 7.0 and update your application heads.

## TL;DR

If your app is already a single `Sdk="Uno.Sdk"` project on .NET 10 or later, [the steps below](#upgrade-step-by-step) are likely all you need:

1. Bump `Uno.Sdk` in `global.json`.
2. Remove the `net9.0-*` and `net*-maccatalyst` targets.
3. Update the two Android head classes.
4. On iOS and tvOS, migrate saved settings at startup.
5. Update and rebuild every library the app depends on.
6. Build, fix what the compiler reports, and re-test on your devices.

Everything else lives on [linked pages](#the-rest-of-the-guide) you only need to open when something applies to you.

## Before you start

| Your project | What to do |
|---|---|
| A single `Sdk="Uno.Sdk"` app project | Continue with the steps below. |
| A project created before Uno Platform 6.0 that still selects a renderer | Follow the [Uno Platform 6.0 migration guide](xref:Uno.Development.MigratingToUno6) first. |
| Separate head projects (`*.Mobile`, `*.Wasm`, `*.Skia.*`, …) or an app on `Microsoft.NET.Sdk` | [Convert to the `Uno.Sdk`](xref:Uno.Development.MigratingToUno7.ProjectSetup#the-unosdk-is-required-for-applications) first, since these no longer build. |
| A class library or NuGet package | Read [Libraries and conditional code](xref:Uno.Development.MigratingToUno7.Libraries) as well. |

## Upgrade step by step

### 1. Update your tools

Update your [Visual Studio](https://aka.platform.uno/vs-extension-marketplace), [Visual Studio Code](https://aka.platform.uno/vscode-extension-marketplace) or [Rider](https://aka.platform.uno/rider-extension-marketplace) extension, then run [`uno-check`](xref:UnoCheck.UsingUnoCheck) to get the matching .NET SDK and workloads.

### 2. Bump the `Uno.Sdk`

In the `global.json` at the root of your solution, set `Uno.Sdk` to the latest 7.x version from [NuGet](https://www.nuget.org/packages/uno.sdk/):

```json
{
  "msbuild-sdks": {
    "Uno.Sdk": "7.x.y"
  }
}
```

This drives the versions of all Uno Platform packages. In Visual Studio, restart the IDE when the banner asks you to. See [How to upgrade Uno Platform NuGet Packages](xref:Uno.Development.UpgradeUnoNuget).

### 3. Update your target frameworks

In `<TargetFrameworks>`, in every project:

- Change `net9.0-*` to `net10.0-*` or later. 7.0 has no `net9.0` assets. See [Upgrading from .NET 9 to .NET 10](xref:Uno.Development.MigratingFromNet9ToNet10).
- Remove `net*-maccatalyst`. macOS is served by `net10.0-desktop`, so add that if you don't have it. See [Mac Catalyst is removed](xref:Uno.Development.MigratingToUno7.ProjectSetup#mac-catalyst-is-removed) for what else to move.

Only if your project sets them explicitly, also raise these:

- `SupportedOSPlatformVersion` to 15.0 on iOS and tvOS, and 24 on Android.
- `TargetPlatformMinVersion` to 10.0.19041.0 on WinAppSDK.
- `WinAppSdkVersion` to 2.4.0 or later. On Windows, this also [changes the runtime your users need](xref:Uno.Development.MigratingToUno7.Packages#windows-app-sdk-minimum-raised-from-1x-to-240).

### 4. Clean up `UnoFeatures`

- `skiarenderer` is now implied in an app. You can remove it.
- Remove `Prism` and add [the packages yourself](xref:Uno.Development.MigratingToUno7.Packages#prism-uno-feature-removed). The build reports `UNOB0035` until you do.
- Remove `Lottie` and any reference to `Uno.WinUI.Lottie`. [Lottie needs no package on Uno Platform targets now](xref:Uno.Development.MigratingToUno7.Packages#unowinuilottie-package-and-lottie-uno-feature-removed). A project that also targets Windows still references `CommunityToolkit.WinUI.Lottie` for that target.
- If your Windows head or your library uses `SKCanvasElement` or Toolkit Skia controls such as `ShadowContainer`, add `Skia`. See [Windows heads](xref:Uno.Development.MigratingToUno7.Packages#skia-uno-feature-no-longer-implied-on-windows-winappsdk) and [libraries](xref:Uno.Development.MigratingToUno7.Packages#skia-uno-feature-no-longer-implied-on-libraries).

Your code may use a package that used to arrive implicitly, such as AndroidX or `LibVLCSharp`. [Add those explicitly](xref:Uno.Development.MigratingToUno7.Packages#packages-no-longer-referenced-implicitly).

### 5. Update the Android head

Skip this if you don't target Android. Otherwise, every app needs this edit, usually in `Platforms/Android/Main.Android.cs` and `Platforms/Android/MainActivity.Android.cs`:

```diff
- public class MainActivity : Microsoft.UI.Xaml.ApplicationActivity
+ public class MainActivity : Uno.UI.Runtime.Android.ApplicationActivity
```

```diff
+ using Uno.UI.Hosting;
+
- public class Application : Microsoft.UI.Xaml.NativeApplication
+ public class Application : Uno.UI.Runtime.Android.NativeApplication
  {
      public Application(IntPtr javaReference, JniHandleOwnership transfer)
-         : base(() => new App(), javaReference, transfer)
+         : base(javaReference, transfer)
      {
      }
+
+     protected override UnoPlatformHost CreateHost() =>
+         UnoPlatformHostBuilder.Create()
+             .App(() => new App())
+             .UseAndroid()
+             .Build();
  }
```

See [Android head uses the host builder](xref:Uno.Development.MigratingToUno7.AppStartup#android-head-uses-the-host-builder) for details.

### 6. Keep existing settings on iOS and tvOS

Skip this if you don't target iOS or tvOS, or don't use `LocalSettings` or `RoamingSettings`. Otherwise, **updating users lose access to their saved settings unless you do this**, and the build won't warn you. Call this once during startup, before the settings are first read:

```csharp
#if __IOS__ || __TVOS__
var migrated = Uno.Storage.ApplicationDataMigrator.MigrateSettings();
#endif
```

The move is one-way. See [Application settings on iOS and tvOS](xref:Uno.Development.MigratingToUno7.AppStartup#application-settings-on-ios-and-tvos) before you ship it.

### 7. Update your libraries and rebuild

Libraries built against 6.x are binary-incompatible with 7.0, and many of the failures only appear at run time. So before you build:

- Update every third-party Uno Platform library to a version built for 7.0.
- Recompile every library of your own against 7.0.

Then delete the `bin` and `obj` folders and build each target framework. Use [Errors, symptoms and code search](xref:Uno.Development.MigratingToUno7.Lookup) to go from each error to its fix.

### 8. Re-test

Rendering now goes through Skia on every target, and Vulkan is the default on Android, Linux and Windows. Re-baseline visual or snapshot tests, and re-test text, lists and scrolling, IME, pickers, and safe-area or notch handling on devices. Then check for [the changes the compiler won't catch](xref:Uno.Development.MigratingToUno7.Lookup#changes-the-compiler-wont-catch), starting with these:

- [`ItemClickCommand="{Binding}"`](xref:Uno.Development.MigratingToUno7.RemovedApis#listviewbaseitemclickcommand-and-listviewbasecommand) on a `ListView` or `GridView` still compiles but no longer does anything.
- [Removed XAML prefixes](xref:Uno.Development.MigratingToUno7.Libraries#removed-xaml-prefixes) such as `skia:` silently change what your markup does.
- [`PrimaryLanguageOverride`](xref:Uno.Development.MigratingToUno7.FeatureFlags#applicationlanguagesuselegacyprimarylanguageoverride) no longer swaps the culture immediately.
- [`Window.SystemBackdrop`](xref:Uno.Development.MigratingToUno7.Behavior#systembackdrop-no-longer-rewrites-your-contents-backgrounds) no longer makes your backgrounds transparent.

## Who is affected the most

- Apps that **opted out** of the Skia renderer on mobile by omitting
  `<UnoFeatures>skiarenderer</UnoFeatures>` and relying on native rendering.
- Apps that used the **native WebAssembly DOM** renderer (`Uno.WinUI.WebAssembly`). See the [details](xref:Uno.Development.MigratingToUno7.Packages#unowinuiwebassembly-package-removed-and-the-older-unowinuiruntimewebassembly).
- Code that referenced native rendering types, native element hosting, or native-only
  `FeatureConfiguration` flags (see [Removed public APIs](xref:Uno.Development.MigratingToUno7.RemovedApis) and [Removed feature flags](xref:Uno.Development.MigratingToUno7.FeatureFlags)).
- Apps that still build a **Mac Catalyst** (`net*-maccatalyst`) head. See the [details](xref:Uno.Development.MigratingToUno7.ProjectSetup#mac-catalyst-is-removed).
- Apps and libraries that still target **.NET 9** (`net9.0-*`). See the [details](xref:Uno.Development.MigratingToUno7.ProjectSetup#net-9-is-no-longer-supported).

Apps already running on Skia on every target need only recompile against 7.0 and remove
native bootstrap/heads.

## The rest of the guide

Open these only when you need them. Each page starts by saying who should read it.

| Page | Open it when |
|---|---|
| [Errors, symptoms and code search](xref:Uno.Development.MigratingToUno7.Lookup) | You have an error or a symptom, or want to scope the upgrade by searching your code. |
| [Project format, target frameworks and OS versions](xref:Uno.Development.MigratingToUno7.ProjectSetup) | You have separate heads, Mac Catalyst, .NET 9 or pinned OS versions. |
| [Packages and Uno Features](xref:Uno.Development.MigratingToUno7.Packages) | You reference packages explicitly, use lock files, or lost a transitive dependency. |
| [Namespace, assembly and host renames](xref:Uno.Development.MigratingToUno7.Renames) | Your code names `Uno.UI.Toolkit`, a `*.Skia.*` host, or an assembly by name. |
| [App startup, activation and platform state](xref:Uno.Development.MigratingToUno7.AppStartup) | You customize Android startup, activation, lifecycle events or iOS settings. |
| [Libraries and conditional code](xref:Uno.Development.MigratingToUno7.Libraries) | You author a library, or use `#if`, XAML prefixes or file suffixes. |
| [Removed public APIs](xref:Uno.Development.MigratingToUno7.RemovedApis) | A type or member no longer exists. |
| [WinUI API alignment](xref:Uno.Development.MigratingToUno7.WinUIParity) | A member moved, or a base class, signature or constructor changed. |
| [Removed feature flags](xref:Uno.Development.MigratingToUno7.FeatureFlags) | Your code sets `FeatureConfiguration` or `PrimaryLanguageOverride`. |
| [XAML and default styles](xref:Uno.Development.MigratingToUno7.Xaml) | XAML fails to build or a style or URI no longer resolves. |
| [Behavior changes to re-test](xref:Uno.Development.MigratingToUno7.Behavior) | The app builds but looks or acts differently. |
| [Full checklist](xref:Uno.Development.MigratingToUno7.Checklist) | You want every migration task in one list. |

> [!TIP]
> **Using an AI agent for the upgrade?** Point it at this page and have it work through the steps in order, then run the searches in [Search your code](xref:Uno.Development.MigratingToUno7.Lookup#search-your-code) and read only the sections that match. Ask it to fix each error using the linked section, rather than suppressing the diagnostic or adding back a removed package.
