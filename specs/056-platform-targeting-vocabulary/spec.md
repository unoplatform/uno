# Platform targeting vocabulary for Uno Platform 7.0

**Status**: Proposal
**Audience**: Internal engineering (Uno Platform maintainers)

> Follows the work that made XAML conditional prefixes, platform file suffixes and platform symbols resolve
> from the target framework. That change fixed *how* the vocabulary is computed. This one proposes what the
> vocabulary should still contain.

## 1. Why

Three separate naming schemes accumulated across UWP, Xamarin, native renderers, the WASM DOM renderer and the
Reference build flavor. Every one of those is gone in 7.0, but their names remain. The result is a vocabulary
where several terms are dead, several are exact synonyms of each other, and several name things that no longer
exist — with the added problem that the drawing-backend abstraction now in flight wants the name `skia` back
for an actual backend, while `skia` currently means "not WinAppSDK".

Two axes are worth separating explicitly, because conflating them is what produced the mess:

- **Platform** — which OS/target framework. Compile-time, known from the TFM.
- **Host** — whether Uno draws the UI, or WinUI does (WinAppSDK). Compile-time, known from the TFM.
- **Drawing backend** — Skia, WebGPU, … **Not compile-time at all.** `Uno.UI` is compiled once and resolves
  its backend at run time, so no symbol, prefix or file suffix can name it. Any name that appears to is lying.

## 2. XAML conditional prefixes

### 2.1 Keep — the platform axis

| Prefix | Applies to |
|---|---|
| `android` / `not_android` | `netX.0-android` |
| `ios` / `not_ios` | `netX.0-ios` |
| `tvos` / `not_tvos` | `netX.0-tvos` |
| `desktop` / `not_desktop` | `netX.0-desktop` |
| `wasm` / `not_wasm` | `netX.0-browserwasm` |

### 2.2 Rename — the host axis

`win` / `not_win` become **`winappsdk` / `not_winappsdk`**. The old names stay as aliases: `win:` is the single
most used conditional prefix in the ecosystem (269 element uses and 455 `xmlns` declarations in this repository
alone), so removing it is not on the table.

`win` is additionally still hardcoded to always-excluded rather than derived from the target framework. It should
follow `netX.0-windows10.x` like every other prefix. In practice this changes nothing today — Uno's generator does
not run on a WinAppSDK build — but it removes the last prefix that does not follow the stated rule.

### 2.3 Collapse — `skia` and `netstdref` are the host axis under other names

`skia` / `not_skia` and `netstdref` / `not_netstdref` both now evaluate to exactly "drawn by Uno rather than by
WinUI". That is the inverse of the host axis:

```
skia:      ==  not_winappsdk:
not_skia:  ==  winappsdk:
netstdref: ==  not_winappsdk:
```

Proposal: `not_winappsdk` becomes the canonical spelling. `skia` and `netstdref` become deprecated aliases,
documented as such, and are removed in a later major once the ecosystem has migrated.

This is what frees the name `skia` for the drawing-backend work. Note the generator already special-cases
`xmlns:skia="using:SkiaSharp…"` as a non-conditional namespace (`XamlFileParser.IsSkiaNotConditional`); once
`skia` leaves the conditional lists entirely that special case can go too, and SkiaSharp usage gets simpler.

### 2.4 Drop

| Prefix | Reason | In-repo uses |
|---|---|---|
| `macos` / `not_macos` | Never settable — nothing ever produced the value. Mac Catalyst removed in 7.0 | 0 |
| `androidskia` / `iosskia` / `tvosskia` / `wasmskia` (+ `not_`) | Exact synonyms of `android` / `ios` / `tvos` / `wasm` since native rendering was removed | 0 |
| `xamarin` | Hardcoded always-included. Xamarin is gone | 0 |
| `not_mux` | Hardcoded always-excluded since UWP was dropped | 4 — **the XAML under them must be deleted with the prefix**, otherwise it starts being emitted |
| `legacy` | Not a conditional prefix at all: it is `xmlns:legacy="using:Uno.UI.Controls.Legacy"`, force-included to bypass `mc:Ignorable` | 10 — one file lists it in `mc:Ignorable` and needs that entry removed |

`legacy` is worth calling out: keeping a plain CLR namespace alias in the *conditional* include list is a
category error. Removing it makes `legacy:` an ordinary `xmlns`, which is what it always was.

## 3. Platform file suffixes

### 3.1 Keep

`*.Android.cs`, `*.iOS.cs`, `*.tvOS.cs`, `*.UIKit.cs`, `*.desktop.cs`, `*.wasm.cs`, `*.WinAppSDK.cs`

### 3.2 Canonical vs. alias

| Alias | Canonical | Note |
|---|---|---|
| `*.Apple.cs` | `*.UIKit.cs` | Identical rule. 29 vs 31 files in this repository — both are in active use, so this is a documentation decision before it is a deletion |
| `*.browserwasm.cs` | `*.wasm.cs` | Identical rule. `*.wasm.cs` matches the `wasm:` prefix and the `__WASM__` symbol; `*.browserwasm.cs` has 0 uses in this repository |
| `*.skia.cs` | `*.crossruntime.cs` | Identical rule. Same reasoning as the `skia:` prefix — the name must be freed for the backend work |

`*.crossruntime.cs` keeps its name but gets a corrected definition: **every target framework except the
WinAppSDK one**. The old definition ("WebAssembly, Desktop, or Reference") named three runtimes that no longer
exist as a distinction.

### 3.3 Drop

| Suffix | Reason |
|---|---|
| `*.reference.cs` | Gated on `UnoRuntimeIdentifier=='Reference'`, which nothing outside the `Uno.UWP` / `Uno.Foundation` / `Uno.UI.Dispatching` projects sets. Permanently dead for consumers — a consumer file with this suffix compiles nowhere and warns nowhere |
| `*.iOSmacOS.cs` | Gated on `IsIOS` but named for macOS; Mac Catalyst removed in 7.0. 0 files in this repository |

## 4. Preprocessor symbols

### 4.1 The host axis already has a symbol

`HAS_UNO` / `__UNO__` are defined in `build/nuget/uno.winui.common.targets`, which `uno.winui.targets` imports
only when the WinAppSDK is *not* in play. They are therefore already exactly the host axis, and no new symbol is
needed — `#if HAS_UNO` is the C# equivalent of `not_winappsdk:` and of `*.crossruntime.cs`.

| Suffix | Matching symbol | Defined for consumers today |
|---|---|---|
| `*.Android.cs` | `__ANDROID__` | yes (.NET Android SDK) |
| `*.iOS.cs` | `__IOS__` | yes (.NET iOS SDK) |
| `*.tvOS.cs` | `__TVOS__` | yes (.NET tvOS SDK) |
| `*.desktop.cs` | `__DESKTOP__` | yes |
| `*.wasm.cs` | `__WASM__` | yes |
| `*.crossruntime.cs` | `HAS_UNO` | yes |
| `*.WinAppSDK.cs` | `!HAS_UNO` | by absence |

One precision caveat worth documenting rather than fixing: `HAS_UNO`'s absence tracks *the WindowsAppSDK build
assets being applied* (`$(WindowsAppSDKWinUI)` / `$(UseWinUITools)`), not the target framework, while the file
suffixes are target-framework driven. The two disagree only for a `netX.0-windows10.x` project that references
`Uno.WinUI` without the WindowsAppSDK — not a shape worth engineering for.

So the earlier concern that `*.crossruntime.cs` has no matching symbol was wrong: it has one, under a name that
does not look like the suffix. That is a documentation problem, not a missing symbol.

### 4.2 `HAS_UNO_SKIA_<host>` cannot work and should go

Reported as [unoplatform/uno#17684](https://github.com/unoplatform/uno/issues/17684). The desktop hosts each
define their own symbol — `HAS_UNO_SKIA_WIN32`, `HAS_UNO_SKIA_X11`, `HAS_UNO_SKIA_MACOS`,
`HAS_UNO_SKIA_LINUX_FB` — but the SDK references all four packages together for a `netX.0-desktop` executable
head, so **all four are defined simultaneously**. They read as a compile-time host discriminator and cannot be
one: `netX.0-desktop` is a single target framework that runs on Windows, Linux and macOS.

Proposal: drop them. Code that needs the running host must use `OperatingSystem.IsWindows()` /
`IsLinux()` / `IsMacOS()`, which is the only thing that can be correct here. `__UNO_SKIA__` and `HAS_UNO_SKIA`
have the same naming problem as `skia:` and go with the `skia` deprecation.

### 4.3 Rename or document

| Symbol | Problem |
|---|---|
| `UNO_REFERENCE_API` | For consumers it means "the target framework has no platform identifier". Nothing to do with the Reference API, which no longer exists for the UI layer. Either rename with an alias, or document the real meaning |

## 5. Migration

Everything in §2.2, §2.3 and §3.2 is alias-only: existing markup and file names keep working, and the change is
a documentation and deprecation exercise. Everything in §2.4 and §3.3 is a removal and needs a migration note in
`doc/articles/migrating-to-uno-7.md`, plus cleanup of the in-repo XAML listed in the tables.

The only removal with a behavioural consequence is `not_mux`: its content is currently dropped, and deleting the
prefix without deleting the content would start emitting it.

## 6. Open decisions

1. **Do the aliases ever get removed, and when?** A permanent alias is cheap but keeps `skia` occupied. Removing
   in 8.0 frees it for the backend axis. This should be decided with the drawing-backend work, not separately.
2. **`*.Apple.cs` vs `*.UIKit.cs`** — both are in real use in this repository (29 vs 31 files). Picking a
   canonical name means renaming roughly 30 files on one side.
3. **Does `UNO_REFERENCE_API` get renamed?** It is a consumer-visible symbol with unknown external usage; the
   rename is cheap but the alias is forever.
4. **Do `HAS_UNO_SKIA_<host>` get removed outright or deprecated first?** They are actively misleading rather
   than merely redundant, which argues for removal, but they are consumer-visible.
5. **Is the `not_mux` content still wanted?** Dropping the prefix means deleting the four blocks it guards. They
   are UWP-only content that has been dropped from every build since UWP was removed, so this should be a
   formality — but it is a deletion of markup, not of a name.
