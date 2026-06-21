---
description: How to pick the right platform-specialization mechanism (file suffix vs #if vs runtime check vs ApiExtensibility) in Uno source. Auto-loaded when editing src C# files.
paths:
  - "src/**/*.cs"
---

# Platform targeting (Uno)

Preprocessor symbols and file-suffix exclusion are injected by `src/Uno.CrossTargetting.targets` from `UnoRuntimeIdentifier` / `TargetPlatformIdentifier` — **never** set platform `DefineConstants` or `Compile Remove` for suffixes in a `.csproj`. Symbols are **mutually exclusive per build**: a single compilation never has both `__SKIA__` and `__WASM__`.

Current symbols: `__ANDROID__`, `__APPLE_UIKIT__` (iOS/tvOS/MacCatalyst), `__WASM__`, `__SKIA__`, `__NETSTD_REFERENCE__` / `UNO_REFERENCE_API`, `__CROSSRUNTIME__` (true for Skia, WebAssembly, Reference).

## Scope: Skia-first
New UI features target **Skia** (incl. Skia-on-Android/iOS/WASM); the **native** UI targets (native Android Views, iOS/UIKit, WASM DOM) are **maintenance-only** — keep them building and behaving, but don't add features there unless the task says so. This is the *UI* layer only: platform-specific **non-UI WinRT APIs** in `Uno.UWP`/`Uno.Foundation` (rule 5 below) are still actively enhanced, since Skia compiles and uses those per-platform implementations. See AGENTS.md → "Development scope".

## Decision rule — pick the narrowest that fits
1. **Entire implementation is platform-specific** → separate partial **file suffix**: `.Android.cs`, `.iOS.cs`, `.UIKit.cs` (iOS+tvOS), `.wasm.cs`, `.skia.cs`, `.reference.cs`. One file = exactly one platform/runtime; the suffix is auto-excluded elsewhere.
2. **Code shared by all cross-runtime targets** (Skia generic + WASM + Reference, but not native Android/iOS) → **`.crossruntime.cs`**. This is *not* "shared by everything" — native platforms have their own `.Android.cs`/`.UIKit.cs`.
3. **A cross-platform file needs a small platform branch** (e.g. a `using` alias or one method body) → **`#if`** with the symbols above, `#elif` chains not nested `#if`.
4. **One assembly runs on many OSes at runtime** (Skia `netX.0` runs on Win32/macOS/Linux/Android-Skia/iOS-Skia) → **`OperatingSystem.IsAndroid()` / `.IsBrowser()` / `.IsMacOS()`** runtime checks. Never use these for compile-time exclusion; never use them in a `.Android.cs`/`.UIKit.cs` file where the platform is already statically known.
5. **`Uno.UWP`/`Uno.Foundation` generic target needs a platform-specific implementation loaded at runtime** → **`ApiExtensibility.CreateInstance<IXxxExtension>()`** with the concrete impl in a `Uno.UI.Runtime.Skia.*` project. Keeps generic code free of native (JNI/UIKit) references.

## Traps
- `#if __ANDROID__` is **false** in a `.skia.cs` file (Skia-Android uses `.skia.cs`, not `.Android.cs`). Use `#if __SKIA__` there.
- "Skia on Android" (`.skia.cs`) and "native Android" (`.Android.cs`) are different compilations of the same control — don't conflate them.
- Reference (`.reference.cs`) is the stub API surface (throws NotImplemented), distinct from `.crossruntime.cs`.
