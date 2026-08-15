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

### 4.1 The asymmetry to close

`*.skia.cs` and `*.crossruntime.cs` are compiled for consumers, but **neither `__SKIA__` nor `__CROSSRUNTIME__`
is defined for consumers anywhere**. A consumer file with the suffix, guarded by the matching `#if`, compiles to
nothing. Every suffix should have a symbol with the same meaning:

| Suffix | Symbol | Defined for consumers today |
|---|---|---|
| `*.Android.cs` | `__ANDROID__` | yes (.NET Android SDK) |
| `*.iOS.cs` | `__IOS__` | yes (.NET iOS SDK) |
| `*.tvOS.cs` | `__TVOS__` | yes (.NET tvOS SDK) |
| `*.desktop.cs` | `__DESKTOP__` | yes |
| `*.wasm.cs` | `__WASM__` | yes |
| `*.WinAppSDK.cs` | — | **no** |
| `*.crossruntime.cs` | `__CROSSRUNTIME__` | **no** |

Proposal: define `__CROSSRUNTIME__` and a WinAppSDK symbol for consumers, from the target framework, alongside
`__DESKTOP__` and `__WASM__`.

### 4.2 Rename or document

| Symbol | Problem |
|---|---|
| `UNO_REFERENCE_API` | For consumers it means "the target framework has no platform identifier". Nothing to do with the Reference API, which no longer exists for the UI layer. Either rename with an alias, or document the real meaning |
| `__UNO_SKIA__`, `HAS_UNO_SKIA`, `HAS_UNO_SKIA_<host>` | Name a drawing backend that is no longer a compile-time fact. Same collision as `skia:` |

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
3. **Is a WinAppSDK-vs-Uno symbol worth adding**, or is the absence of `__ANDROID__`/`__IOS__`/… sufficient in
   practice for consumer code?
4. **Does `UNO_REFERENCE_API` get renamed?** It is a consumer-visible symbol with unknown external usage; the
   rename is cheap but the alias is forever.
