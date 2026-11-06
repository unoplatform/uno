---
uid: Uno.Development.MigratingToUno7.ProjectSetup
---

# Project format, target frameworks and OS versions

Part of [Migrating to Uno Platform 7.0](xref:Uno.Development.MigratingToUno7).

**Read this if** your app has separate head projects, targets .NET 9 or Mac Catalyst, pins a minimum OS version, or sets `UnoSplashScreenColor`.

**Skip it if** you already have a single `Sdk="Uno.Sdk"` project on `net10.0-*` or later, with no Mac Catalyst target and no pinned OS versions.

## The `Uno.Sdk` is required for applications

The legacy project format is removed. An Uno Platform **application** must now be a single
`Sdk="Uno.Sdk"` project:

```diff
- <Project Sdk="Microsoft.NET.Sdk">
+ <Project Sdk="Uno.Sdk">
```

Concretely, the following are no longer supported:

- Getting Uno Platform into an app head through a `PackageReference` to `Uno.WinUI` on a plain
  `Microsoft.NET.Sdk` project. Application heads that do this now fail with
  [UNOB0029](xref:Build.Solution.error-codes#unob0029-uno-platform-application-projects-must-use-the-unosdk).
- The one-head-project-per-platform layout (`MyApp` shared library + `MyApp.Mobile`,
  `MyApp.Wasm`, `MyApp.Skia.Gtk`, `MyApp.Skia.Linux.FrameBuffer`, `MyApp.Windows`, `MyApp.UWP`,
  and `MyApp.Shared`/`.shproj` projects). One cross-targeted project replaces all of them.
- The Uno Platform 5.1-and-earlier `SingleProject` property, superseded by `UnoSingleProject`
  ([UNOB0022](xref:Build.Solution.error-codes#unob0022-the-singleproject-property-is-no-longer-supported)).
- `uap10.0.*` (UWP) heads. The `Uno.WinUI` package no longer carries UWP build logic.

Follow the [Migrating Projects to Single Project](xref:Uno.Development.MigratingToSingleProject)
guide to convert an app. Once converted, the Uno Platform package versions are driven by the
`Uno.Sdk` entry in `global.json` rather than by individual `PackageReference` versions.

**Class libraries are not affected by this diagnostic.** A library may keep referencing the
`Uno.WinUI` package from a `Microsoft.NET.Sdk` project, although `Sdk="Uno.Sdk"` is the
recommended and templated form. Windows App SDK heads are likewise unaffected.

See also: [Packages and Uno Features](xref:Uno.Development.MigratingToUno7.Packages), [Which target frameworks should a library keep](xref:Uno.Development.MigratingToUno7.Libraries#which-target-frameworks-should-a-library-keep).

## Templates and project heads

New apps are a single `Sdk="Uno.Sdk"` project in which every target framework renders with Skia.
An existing app that still has separate head projects (`*.Mobile`, `*.Wasm`, `*.Skia.*`, …) cannot
keep them, even as Skia heads: application projects must use the `Uno.Sdk`, so convert it to a single
project as described in [The `Uno.Sdk` is required for applications](#the-unosdk-is-required-for-applications),
and remove native bootstrap code.

See also: [Android head uses the host builder](xref:Uno.Development.MigratingToUno7.AppStartup#android-head-uses-the-host-builder).

## Rendering is Skia everywhere

The `NativeRenderer` Uno Feature and the renderer-selection logic are gone — Skia is
the default renderer, and `WebGpu` is the opt-in alternative. In an app, the legacy
`skiarenderer` feature is mapped to `Skia`, which is also implied when no renderer feature is
listed, so you can leave `<UnoFeatures>skiarenderer</UnoFeatures>` in place or remove it. In a
library it is ignored: the renderer is the app's choice, and a library that draws with SkiaSharp
lists `Skia` instead (see [`Skia` Uno Feature no longer implied on libraries](xref:Uno.Development.MigratingToUno7.Packages#skia-uno-feature-no-longer-implied-on-libraries)).

WebAssembly renders to a canvas through Skia; there is no per-element DOM tree, no
`Uno.UI.css` styling layer, and no `WindowManager.ts`.

If your project was created before Uno Platform 6.0 and still selects a renderer, follow
the [Uno 6.0 migration guide](xref:Uno.Development.MigratingToUno6) first to move to the
Uno.SDK single-project model.

See also: [Rendering differences](xref:Uno.Development.MigratingToUno7.Behavior#rendering-differences).

## .NET 9 is no longer supported

Uno Platform 7.0 ships `net10.0` and `net11.0` assets only. The `net9.0` assets that 6.x shipped are
removed, so a head or library that still targets `net9.0-*` fails to restore against 7.0 with
`NU1202`. Retarget it to `net10.0-*` (or `net11.0-*`) before upgrading. See
[.NET version support](xref:Uno.Development.NetVersionSupport) for the supported versions.

See also: [Which target frameworks should a library keep](xref:Uno.Development.MigratingToUno7.Libraries#which-target-frameworks-should-a-library-keep).

## Mac Catalyst is removed

The `net*-maccatalyst` target framework is no longer supported. The `Uno.Sdk` no longer
produces a Mac Catalyst head: `maccatalyst` is not a recognized `TargetFramework`, the
`Platforms/MacCatalyst/` folder is no longer built (a leftover one is left out of every target
framework, with warning [UNOB0033](xref:Build.Solution.error-codes#unob0033-a-platformsmaccatalyst-or-platformsmacos-folder-is-not-built)),
and the `MacCatalystProjectFolder` property is gone. The `__MACCATALYST__` conditional symbol is never defined, and the
`.iOS.cs` and `.UIKit.cs` file suffixes now apply only to `net10.0-ios` and `net10.0-tvos`.

macOS remains a fully supported target through the **`net10.0-desktop`** head, which runs
on macOS with Skia rendering. To migrate:

1. Remove `net*-maccatalyst` from `<TargetFrameworks>` in every project.
2. Add `net10.0-desktop` if the solution does not already have a desktop head.
3. Move anything still needed from `Platforms/MacCatalyst/` to `Platforms/Desktop/`, and
   drop the Catalyst `Info.plist` and `Entitlements.plist`.
4. Replace `#if __MACCATALYST__` blocks with `#if __DESKTOP__` combined with an
   `OperatingSystem.IsMacOS()` check — `__DESKTOP__` alone is also true on Windows and Linux.
5. Publish with the [macOS desktop packaging](xref:uno.publishing.desktop.macos) flow
   instead of the Mac Catalyst one.

The `IsMacCatalyst` and `IsIOSOrCatalyst` MSBuild properties are also gone. Replace
`IsIOSOrCatalyst` with `IsIOS`, not `IsAppleUIKit`, which is also `true` for tvOS. Remove
conditions on `IsMacCatalyst`, and put anything they guarded that macOS still needs under
`IsDesktop`. A condition that references either property as a bare boolean
(for example `Condition="$(IsAndroid) or $(IsIOSOrCatalyst)"`) now fails with
`MSB4100` instead of silently evaluating to `false`, since the property is undefined.

See also: [Platform targeting in multi-targeted libraries](xref:Uno.Development.MigratingToUno7.Libraries#platform-targeting-in-multi-targeted-libraries), [Removed preprocessor symbols](xref:Uno.Development.MigratingToUno7.Libraries#removed-preprocessor-symbols).

## Minimum OS versions raised

Uno Platform 7.0 raises the default minimum OS version on the mobile and WinAppSDK
targets. These are the values the `Uno.Sdk` applies when a head does not set
`SupportedOSPlatformVersion` / `TargetPlatformMinVersion` itself.

| Target | 6.x | 7.0 | Why |
|---|---|---|---|
| iOS | 14.2 | **15.0** | Xcode 27 — the toolchain .NET 11 builds with — refuses deployment targets below iOS 15. |
| tvOS | 14.2 | **15.0** | Same Xcode 27 floor. |
| Android | 21 on `net10.0-android`, 24 on `net11.0-android` | **24 on both** | .NET 11 requires API 24 (Android 7.0). 7.0 applies the same floor to `net10.0-android` so a single value covers every target framework. |
| WinAppSDK | `10.0.18362.0` | **`10.0.19041.0`** | Windows 10 1903 is out of support and is not listed as a supported OS for the Windows App SDK. 19041 matches the `windows10.0.19041.0` target framework already used throughout. |

The Android Wear floor is unchanged at API 26, and no minimum OS version is enforced
for the `net*-desktop` and `net*-browserwasm` targets.

A head that sets these properties explicitly keeps its own value, so an app can still
target lower versions where the underlying SDK allows it — but those combinations are no
longer tested by Uno Platform.

See also: [UIKit scene lifecycle on iOS and tvOS](xref:Uno.Development.MigratingToUno7.AppStartup#uikit-scene-lifecycle-on-ios-and-tvos), [Windows App SDK minimum](xref:Uno.Development.MigratingToUno7.Packages#windows-app-sdk-minimum-raised-from-1x-to-240).

## iOS Hot Restart is not supported

Visual Studio iOS Hot Restart, which deployed to a device connected to a Windows PC without a Mac, is not
supported. Visual Studio 2026 no longer offers it, and Visual Studio 2022 does not support the `net10.0`
target frameworks that 7.0 requires. Uno Platform no longer generates the `__UnoHotRestartDelegate` application
delegate it relied on, which started the app without the Skia iOS host. Build and deploy iOS apps through a
connected macOS host instead. The `UnoDisableHotRestartHelperGeneration` property no longer has any effect and can
be removed, and the `Uno0004` and `Uno0005` diagnostics are no longer reported.

## `UnoSplashScreenColor` removed

The Uno.Sdk no longer reads `UnoSplashScreenColor`; it is ignored and reported as
[UNOB0028](xref:Build.Solution.error-codes#unob0028-the-unosplashscreencolor-property-is-no-longer-supported).
Rename it to `UnoSplashScreenBackgroundColor`, and use `BackgroundColor` instead of `Color` on hand-written
`UnoSplashScreen` items. WebAssembly apps that never set a splash color now get a loader that follows the
browser theme instead of white. See [Splash screen](xref:Uno.Development.SplashScreen).

## Related pages

- [Packages and Uno Features](xref:Uno.Development.MigratingToUno7.Packages) for what the `Uno.Sdk` references for you.
- [Libraries and conditional code](xref:Uno.Development.MigratingToUno7.Libraries) if the project is a library.
- [App startup, activation and platform state](xref:Uno.Development.MigratingToUno7.AppStartup) for the Android head and iOS lifecycle changes.
- [Errors, symptoms and code search](xref:Uno.Development.MigratingToUno7.Lookup) to go from an error or symptom to its fix.
- [Upgrade steps](xref:Uno.Development.MigratingToUno7) to return to the main path.
