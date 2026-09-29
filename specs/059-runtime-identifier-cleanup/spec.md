# Runtime identifier cleanup for Uno Platform 7.0

**Status**: Implemented
**Audience**: Internal engineering (Uno Platform maintainers)

> Continues [spec 056](../056-platform-targeting-vocabulary/spec.md), which made XAML conditional prefixes,
> platform file suffixes and preprocessor symbols resolve from the target framework. That spec deliberately
> scoped itself to the *compile-time vocabulary*. This one covers the MSBuild properties that decide which
> **runtime assets** a head deploys.

## 1. Why

Three properties name the thing to deploy:

| Property | Values it can take |
|---|---|
| `UnoRuntimeIdentifier` | `Skia`, `WebAssembly`, `Reference` |
| `UnoUIRuntimeIdentifier` | `Skia` |
| `UnoWinRTRuntimeIdentifier` | `Android`, `iOS`, `tvOS`, `WebAssembly` |

They were meaningful when several renderers existed and a head had to say which one it was built for. In 7.0
none of them names a distinction that still exists, and none of them names a drawing backend — which matters
now, because the drawing-backend abstraction resolves Skia, WebGPU or a managed engine **at run time**. A
compile-time property spelled `Skia` is not just redundant, it is wrong about what it describes, and it
occupies a name the backend work needs.

Measured against the code rather than against intent:

- **`UnoUIRuntimeIdentifier` has exactly one value.** All three sites that set it assign the literal `Skia`,
  and `RuntimeAssetsSelectorTask` required it to equal `"skia"` to enter two-layer mode at all — then
  re-asserted that with a `throw` the caller had already made unreachable.
- **`UnoWinRTRuntimeIdentifier` is `TargetPlatformIdentifier` respelled.** `Uno.WinUI.Runtime.Skia.AppleUIKit.props`
  computed it by calling `$([MSBuild]::GetTargetPlatformIdentifier(...))`, and the mobile branch resolved
  `lib/netX.0-<platform>` by substring-matching `-{value}` — which only works because the value already *is*
  the platform identifier.
- **Consumer-side `UnoRuntimeIdentifier` only ever held `Skia`**, meaning "the target platform is `desktop`
  or empty".

## 2. What the properties actually encoded

Two facts, conflated with three names:

1. **The target platform** — which is in the target framework, and is what decides where the WinRT assemblies
   come from.
2. **Whether a concrete runtime host is referenced** — which the target framework genuinely cannot express: a
   headless test head is a plain `netX.0` project with an empty `TargetPlatformIdentifier`, indistinguishable
   by target framework from a plain `netX.0` class library that must keep NuGet's own asset selection.

Fact 2 becomes `$(UnoHasRuntimeHost)`, set at exactly the nine sites that set an identifier before. It is
deliberately **not** derived from `TargetPlatformIdentifier != ''`: a raw `Microsoft.NET.Sdk` Android project
referencing `Uno.WinUI` directly receives `uno.winui.runtime-replace.targets` through `buildTransitive` but
never the `Uno.WinUI.Runtime.Skia.Android` `build/` props, and today correctly keeps NuGet's selection. A
target-framework-derived gate would start rewriting its compile references.

## 3. The selection, restated

The single-layer/two-layer apparatus collapses to one lookup of where the WinRT assemblies come from.
Everything else always comes from the shared runtime folder.

| `TargetPlatformIdentifier` | WinRT assemblies | Compile references |
|---|---|---|
| `` (headless), `desktop` | `uno-runtime/<tfm>/generic` — same as everything else | untouched |
| `browserwasm` | `uno-runtime/<tfm>/wasm` | untouched |
| `android`, `ios`, `tvos` | the package's `lib/netX.0-<platform>` | rewritten |
| anything else | — | build error |

The mobile-yes / browser-no asymmetry in the last column is the only behaviour the two-layer split still
carried, and it is now asserted by a test.

## 4. Two disciplines this depended on

### 4.1 The folder is the variant

The `uno-runtime/<tfm>/<folder>` names are the `UnoRuntimeVariant` values lowercased: `generic` and `wasm`,
renamed in 7.0 from `skia` and `webassembly`. Inside Uno, only the nuspecs, the selector task and the packing
targets read the folders, and they ship together. Outside it, every published cross-runtime package ships the
old names — `SkiaSharp.Views.Uno.WinUI` among them — so each has to republish for 7.0.

The selector deliberately does not fall back to `skia`/`webassembly`. A package still in that layout was built
against 6.x, and 7.0 breaks binary compatibility with those (assembly and namespace renames such as
`Uno.UI.Toolkit`), so resolving its old folders would trade a build error naming the package for a crash at
startup. Failing with UNOB0023 is the better of the two.

A folder miss is still not an error inside the task: the resolver returns `null`, the handler logs and returns.
Left alone, **the build would succeed while shipping the reference facade**, which throws
`NotImplementedException` when the application runs. That is why the rename waited on §4.2: UNOB0023 turns
a package in the old layout, or a version skew between 7.0 previews, into a build error. Five encodings of the
convention exist (two nuspecs, the task, the MSBuild glob in `uno.winui.runtime-replace.targets`,
`src/Uno.CrossTargetting.targets`); the task's constants document them.

### 4.2 Every silent path became loud first

Three verified silent failures gated this work, and were fixed before anything moved:

1. `RuntimeAssetsSelectorTask.Execute()` returned `true` with no diagnostic when neither mode matched, while
   the single-layer path hard-errored on an unrecognised value one branch above. Asymmetric by accident.
2. A runtime-enabled package resolving **zero** assemblies was not an error → **UNOB0023**.
3. An unsupported target platform is now an error rather than a no-op.
4. The gate itself being wrongly false — the one failure `UnoHasRuntimeHost` *introduces* — cannot be
   reported from inside the target it gates, so it is **UNOB0025**, raised outside it. A version-skewed
   `Uno.WinUI.Runtime.Skia.*` is the shape that produces it.

UNOB0023 is suppressed during design-time builds: `ReplaceUnoRuntime` runs before `ResolveLockFileReferences`,
which the IDE evaluates mid-restore, where a partially restored package is expected rather than an error.

Without these, a mistake anywhere in this change ships as a runtime `NotImplementedException` instead of a
build failure. The selector tests also all passed an empty `UnoRuntimeEnabledPackage`, so every one of them
exercised the do-nothing path and would have stayed green through a change that broke selection outright.

## 5. Back-compat

- **The `UnoUIRuntimeIdentifier` assembly stamp check survives**, comparing against the constant instead of a
  property, and is now **UNOB0026** with an opt-out. It keeps rejecting an assembly built for one of the
  native renderers — those stamped their platform there — and accepts an unstamped one, which is what 7.0
  produces. The writer is removed with the property that fed it: with a single UI runtime, a stamp recording
  it carries no information. The stamp is a foreign assembly's string-heap content, so it is sanitised before
  it reaches the log, the same way UNOB0020's type names already were.
- **UNOB0024 warns rather than errors** on a head still setting one of the properties. No documentation ever
  described them, so who sets them is unknown, and silently dropping their effect is the outcome to prevent.
  It is gated on `UnoHasRuntimeHost`, not `IsUnoHead` — the latter is set only by the Uno.Sdk, and a
  hand-rolled head is exactly the shape likely to still carry these.
- **A cross-runtime library keeps runtime replacement.** Such a library sets `UnoRuntimeVariant` (or the
  deprecated `UnoRuntimeIdentifier`) without referencing a runtime host, so `ReplaceUnoRuntime` is gated on
  either signal. Gating on the host alone would
  have left the library's own output on the reference facades.
- `_UnoValidateReferencesUnoRuntimeIdentifier` is renamed to `_UnoValidateRuntimeAssets`, with the old name
  kept as an alias target. The alias carries `BeforeTargets="CoreCompile"` of its own: MSBuild schedules a
  consumer's `Before/AfterTargets` hook only when the anchor target actually executes, so an alias with only
  `DependsOnTargets` would never fire one.
- **The `MediaPlayerElement` half of UNO0007 is removed** — a consumer-visible diagnostic, called out rather
  than slipped in. Both of its branches were unreachable: one compared `UnoRuntimeIdentifier` against a value
  no shipped package has set since native WebAssembly was removed, the other looked for
  `Uno.UI.Runtime.Skia.Gtk`. All three packages it recommended no longer ship. The `ProgressRing` half does
  not read the property and is untouched.
- **The Lottie and Svg dependency checks keep firing on exactly the heads they fired on before** (desktop and
  headless), now testing that condition directly. They have never run on mobile or browser heads, so the
  dependency gap there is real — widening them turns a silent gap into a new hard build error on four target
  frameworks, and whether `SkiaSharp.Skottie` and `Svg.Skia` are usable on `browserwasm` has to be
  established first. Separate change.

## 6. One name: `UnoRuntimeVariant`

`UnoRuntimeVariant` names **which build of a multi-variant project this is** — nothing more. It is not a .NET
`RuntimeIdentifier` (a browser head sets `RuntimeIdentifier=browser-wasm` right next to it) and not a drawing
backend: `Uno.UI` compiles once and resolves its backend at run time, so no build-time value can name one.
`Skia` became `Generic` because that variant is the build every drawn-by-Uno target framework shares;
`WebAssembly` became `Wasm`, matching `*.wasm.cs`, `wasm:` and `__WASM__`.

| Values | `Generic`, `Wasm`, `Reference` |
|---|---|
| Set by | the multi-variant and single-variant projects under `src/`, and third-party cross-runtime libraries |
| Read by | `src/Uno.CrossTargetting.targets` (in-repo symbols and suffixes) and `build/nuget/uno.winui.*.targets` (packing and replacement) |
| Folder | the value lowercased: `uno-runtime/<tfm>/generic`, `uno-runtime/<tfm>/wasm` |

`UnoRuntimeIdentifier` stays accepted from a cross-runtime library as a deprecated spelling: `skia` maps to
`generic`, `webassembly` to `wasm`, and UNOB0024 names the value to use instead. It is not reused for the new
values, because packages versioned apart from Uno test it for `'Skia'` and `'WebAssembly'` — Uno.Resizetizer
decides "is this a Skia app" from it, which is also why `Uno.Common.Desktop.targets` and the X11, Win32,
macOS, FrameBuffer and Headless host props still set `UnoRuntimeIdentifier=Skia` (the hosts cover heads that
do not use the `-desktop` target framework) until Resizetizer reads `UnoHasRuntimeHost`. The mobile and
browser hosts never set it, so Resizetizer keeps classifying those heads the way it did in 6.x.

**The cross-runtime model stays.** An Uno.Sdk library can multi-target `net10.0-desktop` and
`net10.0-browserwasm` instead, but those target platforms are defined by the Uno.Sdk: a library built with
plain `Microsoft.NET.Sdk` cannot target them, and .NET's own `net10.0-browser` is a different platform to
NuGet. For such a library the `uno-runtime` replacement is the only build-time desktop/browser split, and
at least one is published (`SkiaSharp.Views.Uno.WinUI`).

## 7. Deliberately not done

- **The third-party wasm enumeration defect.** On a browser head, a third-party cross-runtime package's
  assembly is taken from the shared folder rather than its browser build. Real, but a behaviour change for
  shipped packages and not what this work is about.
- **`__SKIA__`, `HAS_UNO_SKIA`, `*.skia.cs`.** Spec 056 owns these. `UnoRuntimeVariant=Generic` now defines
  `__SKIA__` and selects `*.skia.cs`, so the symbol and the suffix are the last in-repo spellings of `skia` on
  this axis. Renaming them is mechanical but touches thousands of `#if` sites, which is why it is its own
  change rather than a rider on this one.
- **The `skia` host pseudo-platform in the hot-reload protocol** — reported for a desktop head by
  `GetRuntimeTargetFramework` and matched server-side by the `['', 'desktop', 'skia']` family. It re-occupies
  the name the moment this work frees it, so freeing `skia` is incomplete until it moves. Belongs with the
  drawing-backend work (unoplatform/uno#24153).
- **Dropping the `UnoRuntimeIdentifier=Skia` desktop shim.** It waits on a Uno.Resizetizer release that detects
  a Skia app from the target platform and `UnoHasRuntimeHost`.

## 8. An invariant worth writing down

`HandleForRuntimeEnabled` enumerates `*.dll` over the **shared** folder and only then redirects individual
assemblies. That folder's file listing is therefore the *authoritative asset list*: an assembly a package ships
only under `wasm` and not under `generic` is silently dropped on a browser head. Uno's own packages are
unaffected because their file sets are identical, but anything that changes the enumeration basis — including
fixing §7's third-party defect — must account for this first.
