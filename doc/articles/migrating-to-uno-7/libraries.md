---
uid: Uno.Development.MigratingToUno7.Libraries
---

# Libraries and conditional code

Part of [Migrating to Uno Platform 7.0](xref:Uno.Development.MigratingToUno7).

**Read this if** you author a class library or NuGet package, use `#if` platform symbols, conditional XAML prefixes or file suffixes such as `*.skia.cs`, or set `UnoRuntimeIdentifier`.

**Skip it if** your app has no platform-conditional code and no library projects of its own. You still need to update its package dependencies to builds made for 7.0.

## Platform targeting in multi-targeted libraries

A library that multi-targets `net10.0-ios`, `net10.0-android`, or `net10.0-tvos` alongside a plain `net10.0` now
keeps its platform-specific asset on the matching application head. In 6.x those assets were replaced by the
library's `netX.0` asset, so `#if __IOS__` and `#if __ANDROID__` blocks inside a library never ran and a `netX.0`
target framework was mandatory. Both are no longer true: the `net10.0-ios` assembly is what an iOS head compiles
against and deploys.

Three related mechanisms now follow the target framework rather than an internal runtime identifier, for class
libraries and application heads alike:

| Mechanism | 6.x | 7.0 |
|---|---|---|
| `android:` / `ios:` / `wasm:` XAML prefixes | never applied — every target rendered with Skia, which won | apply on `net*-android` / `net*-ios` / `net*-browserwasm` |
| `tvos:` and `desktop:` XAML prefixes | did not exist | apply on `net*-tvos` / `net*-desktop` |
| `winappsdk:` XAML prefix | spelled `win:` | `net*-windows10.x`; `win:` kept as a synonym |
| `*.skia.cs`, `*.crossruntime.cs` | compiled only for `net*-desktop` | compiled for every target framework except the WinAppSDK one |
| `__DESKTOP__` | application heads only | any `net*-desktop` project |
| `__WASM__` | application heads, and libraries referencing the WebAssembly runtime package | any `net*-browserwasm` project |

What this means for an upgrade:

- Re-test `#if`-guarded code and conditional XAML inside multi-targeted libraries. Code that was written under the
  assumption that it was dead on mobile now executes. In particular `not_android:` and `not_ios:` no longer apply
  on Android and iOS.
- Rename a `*.skia.cs` file that was really desktop-only to `*.desktop.cs`. It now compiles on every Uno target.
- Recompile every multi-targeted library against 7.0. Its `net*-ios`/`net*-android` assets are now deployed rather
  than discarded, so a stale asset built against the 6.x binaries fails at run time instead of being silently
  replaced. The build reports [UNOB0020](xref:Build.Solution.error-codes) when it can detect this.
- Nothing to do if your libraries already ship a `netX.0` asset. It is still the asset used by the
  `net10.0-desktop` and `net10.0-browserwasm` heads, and workarounds built for the 6.x behavior keep working.

### Which target frameworks should a library keep

Which target frameworks should a library keep? `Uno.WinUI` shipping only `netX.0` and
`netX.0-windows10.0.19041.0` does not mean a library can drop its other targets:

- A library without platform-specific code (no `#if __ANDROID__`, `__IOS__`, `__WASM__` or `__DESKTOP__`, and no
  `android:`, `ios:`, `wasm:` or `desktop:` XAML prefixes) only needs `netX.0`, plus
  `netX.0-windows10.0.19041.0` if it supports WinAppSDK.
- A library with platform-specific code keeps the matching `-android`, `-ios`, `-tvos`, `-browserwasm` or
  `-desktop` target. Each one compiles with its own symbols and XAML prefixes, and a head with that target framework
  uses that asset rather than the `netX.0` one.
- Dropping a platform target framework does not break consumers, since their heads fall back to the `netX.0` asset,
  but that platform's `#if` blocks and conditional XAML no longer apply in the library.

> [!NOTE]
> `*.skia.cs` means "compiled by Uno Platform rather than by WinUI". It does not name a drawing backend: `Uno.UI`
> is compiled once and resolves its backend at run time.

### Removed XAML prefixes

Every conditional prefix is now named after a target framework, so the prefixes that named a renderer or a
long-gone distinction are removed. A removed prefix is not rejected: it is treated as an ordinary XML namespace,
which silently changes what the markup does. Listed in `mc:Ignorable`, its content is ignored on every target;
declared with the presentation namespace (the usual form for a `not_` prefix), its content applies on every
target. The build reports each such declaration as [UXAML0007](xref:Build.Solution.error-codes).
Rewrite the markup:

| Removed prefix | Replacement |
|---|---|
| `skia:`, `netstdref:` | `not_winappsdk:` |
| `not_skia:`, `not_netstdref:` | `winappsdk:` |
| `androidskia:`, `iosskia:`, `tvosskia:`, `wasmskia:` | `android:`, `ios:`, `tvos:`, `wasm:` |
| `macos:` | drop the attribute along with the markup using it — it named the native macOS head, which has selected nothing since Uno Platform 5.0. Gate macOS-only code with `OperatingSystem.IsMacOS()` instead |
| `not_macos:` | drop the prefix — it has applied on every target since Uno Platform 5.0. `desktop:`/`not_desktop:` are not equivalent, since they also cover Windows and Linux |
| `not_mux:` | drop the attribute — it dates from UWP support and never applied |
| `xamarin:`, and `legacy:` listed in `mc:Ignorable` | drop the prefix |

The removed names stay valid as ordinary aliases of a `using:` namespace, and are not reported then. In particular,
`xmlns:legacy="using:Uno.UI.Controls.Legacy"` names a live namespace (it still holds the `Grid` extensions): keep that
prefix if you use it. `legacy:ProgressRing` no longer exists, see [`Uno.UI.Controls.Legacy.ProgressRing`](xref:Uno.Development.MigratingToUno7.RemovedApis#unouicontrolslegacyprogressring).

### Removed file suffixes

These suffixes no longer have a rule, so a file using one compiles for every target framework, the WinAppSDK one
included. The build reports each such file as [UNOB0027](xref:Build.Solution.error-codes).

| Removed suffix | Replacement |
|---|---|
| `*.Apple.cs` | `*.UIKit.cs` — the rule was always identical |
| `*.reference.cs` | delete the file, or fold it into `*.crossruntime.cs`. It was gated on a build flavor an application never selected, so it compiled nowhere |
| `*.iOSmacOS.cs` | `*.iOS.cs`. It named the native macOS target, removed in 7.0 |

### Removed preprocessor symbols

`HAS_UNO_SKIA_WIN32`, `HAS_UNO_SKIA_X11`, `HAS_UNO_SKIA_MACOS`, `HAS_UNO_SKIA_LINUX_FB`,
`HAS_UNO_SKIA_ANDROID`, `HAS_UNO_SKIA_APPLE_UIKIT`, `HAS_UNO_SKIA_HEADLESS`,
`HAS_UNO_SKIA_WEBASSEMBLY_BROWSER` and their `__UNO_SKIA_*__` counterparts are gone
([#17684](https://github.com/unoplatform/uno/issues/17684)).

They read as a compile-time host discriminator but could never be one: the SDK references every desktop host
package together, so all of them were defined at once in a `netX.0-desktop` head. Use
`OperatingSystem.IsWindows()` / `IsLinux()` / `IsMacOS()`, which is the only check that can be correct for a
target framework that runs on all three. `HAS_UNO_SKIA` and `__UNO_SKIA__` are kept, see below.

### Preprocessor symbols no longer depend on the project shape

The `Uno.WinUI` package now defines every "Uno draws the UI" symbol in one place, for every target framework except
the WinAppSDK one. The host packages (`Uno.WinUI.Runtime.*`, formerly `Uno.WinUI.Runtime.Skia.*`) no longer define any symbol. In 6.x the result
depended on which runtime packages a project happened to reference:

| Symbol | 6.x | 7.0 |
|---|---|---|
| `UNO_REFERENCE_API`, `HAS_UNO_SKIA`, `__UNO_SKIA__` | missing from natively rendered targets, from `net*-desktop` class libraries (unless `UnoFeatures` pulled in `MediaPlayerElement` or `WebView`) and from mobile libraries that don't use the Uno.Sdk | defined wherever `HAS_UNO` is |
| `__APPLE_UIKIT__` | not defined in application or library projects | `net*-ios` and `net*-tvos` |
| `__DESKTOP__` | also defined by `Uno.WinUI.Runtime.Skia.Headless`, even on a plain `net10.0` target framework | `net*-desktop` only |

What this means for an upgrade:

- An `#else` branch under `UNO_REFERENCE_API`, `HAS_UNO_SKIA` or `__UNO_SKIA__` no longer compiles in desktop class
  libraries or libraries without the Uno.Sdk: they now take the Uno branch, as their application heads always did.
- Replace these three symbols with `__UNO__`. `HAS_UNO_SKIA` and `__UNO_SKIA__` are deprecated and planned for
  removal in Uno Platform 8.0.
- A project that references `Uno.WinUI.Runtime.Skia.Headless` on a plain `net10.0` target framework, or a project
  that does not use the Uno.Sdk and relied on a runtime package for `__DESKTOP__` or `__WASM__`, must now target
  `net10.0-desktop` / `net10.0-browserwasm` with the Uno.Sdk, or add the symbol to its own `DefineConstants`.

### The not-implemented warning (`Uno0001`) follows the target framework

Whether [`Uno0001`](xref:Build.Solution.error-codes#uno0001) fired used to depend on the preprocessor symbols above,
so it varied with the project shape: `net*-desktop` class libraries got it for no WinRT API, Android and iOS projects
could get it for WinRT APIs those platforms do implement, and earlier 7.0 previews reported it for no `Uno.UI` or
`Uno.UI.Composition` API at all. The warning now follows the target framework:

- Using a WinUI or Composition API that Uno Platform does not implement reports `Uno0001` on every target.
- A WinRT API (`Windows.*`) reports it only on the target frameworks whose implementation is missing. A plain
  `net10.0` library can run on any of them, so it gets the warning only when the desktop and WebAssembly
  implementations are both missing.

A project that sets `TreatWarningsAsErrors` can fail to build after the upgrade. Replace the API, or suppress the
warning where the call is intentional (`#pragma warning disable Uno0001`, or `<NoWarn>$(NoWarn);Uno0001</NoWarn>`).

## Runtime identifier properties removed

`UnoRuntimeIdentifier`, `UnoUIRuntimeIdentifier` and `UnoWinRTRuntimeIdentifier` no longer affect an
application head. They named which runtime assets to deploy at a time when several renderers existed; with a
single UI runtime, every value they could take is either a constant or the target platform spelled differently,
and the target framework decides instead.

Setting them on a head is reported as [UNOB0024](xref:Build.Solution.error-codes#unob0024-a-runtime-identifier-property-no-longer-selects-runtime-assets)
and can be removed. Almost no application sets them — they were set for you by the runtime packages.

Consequences worth knowing about:

- Assemblies are no longer stamped with an `UnoUIRuntimeIdentifier` assembly metadata attribute. A library built
  for one of the native renderers still carries its own stamp and is still rejected, now as
  [UNOB0026](xref:Build.Solution.error-codes#unob0026-a-referenced-assembly-was-built-for-a-ui-runtime-that-no-longer-exists).
- A runtime-enabled package that provides no runtime assembly is now a build error,
  [UNOB0023](xref:Build.Solution.error-codes#unob0023-a-runtime-enabled-package-provided-no-runtime-assembly),
  rather than a build message followed by a `NotImplementedException` when the application runs. A head that
  references such packages without a runtime host at all is reported as
  [UNOB0025](xref:Build.Solution.error-codes#unob0025-runtime-enabled-packages-are-referenced-without-a-runtime-host).
- `UNO0007` is retired. Its `MediaPlayerElement` check only ever fired for the native WebAssembly and GTK
  targets, which 7.0 removes, and `ProgressRing` no longer needs the Lottie package it asked for.
- The `RuntimeAssetsSelectorTask_v0` MSBuild task no longer accepts the three identifier parameters. This
  matters only if you invoked that task directly, which Uno Platform's own targets are the only known caller of.
- The three properties are no longer exposed to analyzers and source generators as `CompilerVisibleProperty`
  items, so `build_property.UnoRuntimeIdentifier` and its two siblings read as empty. Detect the platform from the
  target framework instead.

### Cross-runtime libraries

A cross-runtime library — one that packs a desktop and a browser build of itself into `uno-runtime/` — now
declares which build each project is with `UnoRuntimeVariant`, and the folders are renamed to match:

| Before 7.0 | 7.0 | Folder |
|---|---|---|
| `<UnoRuntimeIdentifier>skia</UnoRuntimeIdentifier>` | `<UnoRuntimeVariant>Generic</UnoRuntimeVariant>` | `generic` (was `skia`) |
| `<UnoRuntimeIdentifier>webassembly</UnoRuntimeIdentifier>` | `<UnoRuntimeVariant>Wasm</UnoRuntimeVariant>` | `wasm` (was `webassembly`) |
| `<UnoRuntimeIdentifier>Reference</UnoRuntimeIdentifier>` | `<UnoRuntimeVariant>Reference</UnoRuntimeVariant>` | none — `lib/<tfm>` |

A library that packs its builds through `UnoRuntimeProjectReference` gets `uno-runtime/generic` and
`uno-runtime/wasm`; one that lays its package out by hand can also use `uno-runtime/<tfm>/generic` and
`uno-runtime/<tfm>/wasm`. Custom packing targets that call `UnoRuntimeGetTargetPath` read the folder from the
returned item's `UnoRuntimeVariant` metadata, which replaces `UnoRuntimeIdentifier`.

The old property still works and is reported as UNOB0024 with the value to use instead. Such a library has to
be rebuilt against 7.0 anyway; a package still in the old layout is reported as
[UNOB0023](xref:Build.Solution.error-codes#unob0023-a-runtime-enabled-package-provided-no-runtime-assembly)
when an application consumes it, rather than failing when it runs.

A library built with the Uno.Sdk can instead multi-target `net10.0-desktop`, `net10.0-browserwasm`,
`net10.0-android` and so on, which behaves as it does in any .NET project. The cross-runtime model remains for
libraries built with plain `Microsoft.NET.Sdk`, which cannot target the Uno.Sdk's `desktop` and `browserwasm`
platforms.

Most libraries need neither. A library that targets plain `net10.0` or `net11.0` and has no desktop- or
browser-specific code sets no `UnoRuntimeVariant` and packs no `uno-runtime` folder: it compiles against the
platform-neutral assemblies in `lib/`, and the application head deploys the runtime implementations matching its
own target framework, the library's calls included.
