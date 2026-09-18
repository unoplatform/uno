---
uid: Uno.Contributing.PreprocessorSymbols
---

# Preprocessor symbols in the Uno Platform repository

> [!IMPORTANT]
> The symbols on this page are defined only for projects inside the Uno Platform repository, by
> `src/Uno.CrossTargetting.targets` and a few project files. **They are not defined in application or library
> projects that consume Uno Platform packages**, so `#if __SKIA__` in your own code is always false. For the
> symbols your projects get, see [Platform-specific C# code](xref:Uno.Development.PlatformSpecificCSharp).

Each table lists the consumer symbol to use instead, when there is one.

## Build flavors

`Uno.UI` and the libraries built on it compile once, for Skia: a plain `netX.0` assembly that runs on every Uno
target. The WinRT layer (`Uno.WinRT`, `Uno.Foundation`, `Uno.UI.Dispatching`) still builds one variant per build
flavor, selected by `UnoRuntimeIdentifier` or by the target framework.

| Symbol | Defined for | Consumer equivalent |
| ------ | ----------- | ------------------- |
| `__SKIA__` | `UnoRuntimeIdentifier=Skia`: `Uno.UI` and everything built on it, and the desktop flavor of the WinRT layer | In `Uno.UI` code it is always true, so it is `HAS_UNO`. In the WinRT layer it names the desktop flavor: `__DESKTOP__` |
| `__WASM__` | The WebAssembly flavor of the WinRT layer | `__WASM__` |
| `__NETSTD_REFERENCE__` | The Reference flavor of the WinRT layer, packed as the `lib/netX.0` assembly that plain `netX.0` libraries compile against | None |
| `__CROSSRUNTIME__` | The Skia, WebAssembly and Reference flavors, not the Android, iOS and tvOS variants of the WinRT layer | `HAS_UNO` |
| `UNO_REFERENCE_API` | Same as `__CROSSRUNTIME__` | `HAS_UNO`. Consumers get `UNO_REFERENCE_API` as a legacy synonym of `HAS_UNO`, which is a different condition |
| `__ANDROID__`, `__IOS__`, `__TVOS__` | The Android, iOS and tvOS variants | Same symbols |
| `__APPLE_UIKIT__` | The iOS and tvOS variants | `__APPLE_UIKIT__` |

`#if __ANDROID__` is false in every file the `Uno.UI` project compiles, even though that assembly runs on Android.
Use `OperatingSystem.IsAndroid()` there.

## Windows App SDK builds

| Symbol | Defined for | Consumer equivalent |
| ------ | ----------- | ------------------- |
| `HAS_UNO`, `HAS_UNO_WINUI` | Every compilation except the `-windows10.0.19041.0` ones | `HAS_UNO` |
| `WINDOWS_WINUI` | Every target framework ending in `-windows10.0.19041.0` | `!HAS_UNO`, or `WINDOWS` |
| `WINAPPSDK` | Only the WinAppSDK builds of `SamplesApp`, `Uno.UI.RuntimeTests.Windows` and `Uno.WinUI.Graphics3DGL` | `!HAS_UNO` |

## Feature flags

`Uno.CrossTargetting.targets` defines capability flags per build flavor. Most of them are constant in the Skia
`Uno.UI` build and remain meaningful for the shared runtime tests, which also run on the WinAppSDK. Consumers get
none of them.

| Symbol | Defined for |
| ------ | ----------- |
| `UNO_HAS_MANAGED_POINTERS` | Skia, WebAssembly |
| `UNO_HAS_ENHANCED_LIFECYCLE` | Skia, WebAssembly |
| `HAS_INPUT_INJECTOR` | Skia, WebAssembly |
| `UNO_HAS_MANAGED_SCROLL_PRESENTER` | Skia |
| `SUPPORTS_RTL` | Skia |
| `UNO_SUPPORTS_NATIVEHOST` | Skia |
| `HAS_COMPOSITION_API` | Skia, WinAppSDK |
| `HAS_RENDER_TARGET_BITMAP` | Skia, Android, iOS, tvOS, WinAppSDK |
| `UNO_HAS_UIELEMENT_IMPLICIT_PINNING` | iOS, tvOS |
| `IS_CI` | CI builds |
| `IS_CI_OR_DEBUG` | CI builds and the Debug configuration |

## Project identity symbols

These let a source file that several projects compile tell which project it is being compiled in. There is no
consumer equivalent.

| Symbol | Defined by |
| ------ | ---------- |
| `IS_UNO_UI_PROJECT` | `Uno.UI` |
| `IS_UNO_UI_DISPATCHING_PROJECT` | Every flavor of `Uno.UI.Dispatching` |
| `IS_UNO_FOUNDATION_RUNTIME_WEBASSEMBLY_PROJECT` | `Uno.Foundation.Runtime.WebAssembly` |
| `IS_UNO` | `Uno.UI`, `Uno.UI.Composition`, `Uno.UI.XamlHost`, every flavor of `Uno.WinRT` and `Uno.UI.Dispatching`, and the two test view libraries |
| `IS_UNIT_TESTS` | `Uno.UI.UnitTests`, `Uno.UI.Tests.ViewLibrary` and `Uno.UI.Tests.ViewLibraryProps`. It is absent from `Uno.UI` itself |
