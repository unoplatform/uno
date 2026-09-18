---
description: How to pick the right platform-specialization mechanism (file suffix vs #if vs runtime check vs ApiExtensibility) in Uno source. Auto-loaded when editing src C# files.
paths:
  - "src/**/*.cs"
---

# Platform targeting (Uno)

> **Scope: this rule covers the Uno repository's own projects**, which use `src/Uno.CrossTargetting.targets`. The
> consumer-facing vocabulary shipped in `Uno.Sdk` / `Uno.WinUI` is target-framework driven and differs — see
> `doc/articles/platform-specific-csharp.md`, `doc/articles/platform-specific-xaml.md`, and the proposal in
> `specs/056-platform-targeting-vocabulary/spec.md`.

Preprocessor symbols and file-suffix exclusion are injected by `src/Uno.CrossTargetting.targets` from `UnoRuntimeFlavor` / `TargetPlatformIdentifier` — **never** set platform `DefineConstants` or `Compile Remove` for suffixes in a `.csproj`. Symbols are **mutually exclusive per build**: a single compilation never has both `__SKIA__` and `__WASM__`.

`UnoRuntimeFlavor` names **which build of a multi-flavour project this is**, and takes three values — or none at all. It is not a drawing backend and not a runtime identifier — `Uno.UI` picks its backend (Skia, WebGPU, …) at run time, so no build-time value can name one.

| `UnoRuntimeFlavor` | Which projects | Selects | Ships in |
|---|---|---|---|
| `Generic` | `*.Skia.csproj` and every single-flavour project | `*.skia.cs`, `__SKIA__` | `uno-runtime/<tfm>/skia` |
| `Wasm` | `*.Wasm.csproj` | `*.wasm.cs`, `__WASM__` | `uno-runtime/<tfm>/webassembly` |
| `Reference` | `*.Reference.csproj` | `*.reference.cs`, `__NETSTD_REFERENCE__` | `lib/<tfm>` |
| *(empty)* | `*.netcoremobile.csproj` on an android/ios/tvos TFM, and the WinAppSDK builds | nothing — `TargetPlatformIdentifier` and the `.Android.cs` / `.UIKit.cs` suffixes take over | `lib/<tfm>-<platform>` |

Empty is a **valid state, not a missing value**, which is why `Uno.CrossTargetting.targets` has neither a fallback nor an error for it: defaulting it would hand `__SKIA__` to the native builds, and failing on it would break every project that is not multi-flavoured.

So **`__SKIA__` is not on everywhere**. `Uno.UI` and everything above it ship a single `Generic` build, so it is always defined there; the WinRT layer below (`Uno.WinRT`, `Uno.Foundation`, `Uno.UI.Dispatching`) still builds the `Wasm`, `Reference` and native-mobile flavours, which do not get it.

The `uno-runtime` folder names in the last column are a **frozen packaging convention** set by the nuspecs, deliberately not derived from the value — renaming the value must never move the published layout.

Current symbols: `__ANDROID__`, `__APPLE_UIKIT__` (iOS/tvOS), `__WASM__`, `__SKIA__`, `__NETSTD_REFERENCE__` / `UNO_REFERENCE_API`, `__CROSSRUNTIME__` (true for all three flavours).

## Scope: Skia-only UI
`Uno.UI` compiles once, for Skia (`UnoRuntimeFlavor=Generic`, plain `netX.0`), and that single assembly runs on Desktop, Android, iOS/tvOS and WebAssembly. The native Android View, UIKit and WASM DOM renderers were removed in 7.0, so there is no native UI target to maintain. Platform-specific behavior in the UI layer uses runtime checks or `ApiExtensibility` (rules 4-5). Per-platform **file suffixes** remain meaningful only in projects that still build per-platform variants: the WinRT layer (`Uno.WinRT`, `Uno.Foundation`, `Uno.UI.Dispatching`) and platform-specific runtime/add-in projects. The non-UI WinRT APIs there are still actively enhanced, since Skia apps consume those implementations. See AGENTS.md → "Development scope".

## Decision rule — pick the narrowest that fits
1. **Entire implementation is platform-specific, in a per-platform project** → separate partial **file suffix**: `.Android.cs`, `.iOS.cs`, `.UIKit.cs` (iOS+tvOS), `.wasm.cs`, `.skia.cs`, `.reference.cs`. One file = exactly one platform/runtime; the suffix is auto-excluded elsewhere. The `Uno.UI` project itself compiles only `.skia.cs` and `.crossruntime.cs`. A platform runtime project may still link a suffixed file from under `src/Uno.UI` (e.g. `Uno.UI.Runtime.Skia.WebAssembly.Browser` compiles `NativeWebView.wasm.cs`), so judge a file by the project that compiles it, not by its path.
2. **Code shared by all cross-runtime variants** (Skia + WASM + Reference, but not the Android/iOS variants) → **`.crossruntime.cs`**. This is *not* "shared by everything": in the WinRT layer the Android/iOS variants have their own `.Android.cs`/`.UIKit.cs`.
3. **A cross-platform file needs a small platform branch** (e.g. a `using` alias or one method body) → **`#if`** with the symbols above, `#elif` chains not nested `#if`.
4. **One assembly runs on many OSes at runtime** (Skia `netX.0` runs on Win32/macOS/Linux/Android-Skia/iOS-Skia) → **`OperatingSystem.IsAndroid()` / `.IsBrowser()` / `.IsMacOS()`** runtime checks. Never use these for compile-time exclusion; never use them in a `.Android.cs`/`.UIKit.cs` file where the platform is already statically known.
5. **`Uno.WinRT`/`Uno.Foundation` generic target needs a platform-specific implementation loaded at runtime** → **`ApiExtensibility.CreateInstance<IXxxExtension>()`** with the concrete impl in a `Uno.UI.Runtime.Skia.*` project. Keeps generic code free of native (JNI/UIKit) references.

## Traps
- `#if __ANDROID__` is **false** in files the `Uno.UI` project compiles and in any `.skia.cs` file: that code compiles for plain `netX.0` and runs on Android unchanged. Use `OperatingSystem.IsAndroid()` for Android-only behavior there, or `#if __SKIA__` to separate it from other variants.
- In the WinRT layer, the Skia variant (`.skia.cs`) and the Android variant (`.Android.cs`) are different compilations of the same API. A Skia app on Android loads the Android variant (`RuntimeAssetsSelectorTask`), not the `.skia.cs` one — don't conflate them.
- Reference (`.reference.cs`) is the stub API surface (throws NotImplemented), distinct from `.crossruntime.cs`.
