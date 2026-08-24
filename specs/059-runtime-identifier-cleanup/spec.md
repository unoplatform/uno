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
| `` (headless), `desktop` | `uno-runtime/<tfm>/skia` — same as everything else | untouched |
| `browserwasm` | `uno-runtime/<tfm>/webassembly` | untouched |
| `android`, `ios`, `tvos` | the package's `lib/netX.0-<platform>` | rewritten |
| anything else | — | build error |

The mobile-yes / browser-no asymmetry in the last column is the only behaviour the two-layer split still
carried, and it is now asserted by a test.

## 4. Two disciplines this depended on

### 4.1 The property value is not the folder name

`uno-runtime/<tfm>/skia` and `.../webassembly` are paths inside packages **already on nuget.org**. They are now
named constants in the task, with a comment saying what they mean, rather than whatever a property happened to
hold.

Moving them is disproportionate to any benefit, because a folder miss is not an error: the resolver returns
`null`, the handler logs and returns, and **the build succeeds while shipping the reference facade**, which
throws `NotImplementedException` when the application runs. Five independent encodings of the convention exist
(two nuspecs, the task, the MSBuild glob in `uno.winui.runtime-replace.targets`, `src/Uno.CrossTargetting.targets`)
and nothing cross-checks them.

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
- **A cross-runtime library keeps runtime replacement.** Such a library sets `UnoRuntimeIdentifier` without
  referencing a runtime host, so `ReplaceUnoRuntime` is gated on either signal. Gating on the host alone would
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

## 6. The two names, now separated

The property carried two unrelated jobs under one name. They are now two names:

| | In-repo build flavour | Library-authoring contract |
|---|---|---|
| Property | `UnoRuntimeFlavor` | `UnoRuntimeIdentifier` |
| Set by | the 33 multi-flavour and single-flavour projects under `src/` | a third-party cross-runtime library |
| Values | `Generic`, `Wasm`, `Reference` | the `uno-runtime/<name>` folder to pack into |
| Read by | `src/Uno.CrossTargetting.targets` (never packed) | `build/nuget/uno.winui.cross-runtime.targets` (shipped) |

`UnoRuntimeFlavor` names **which build of a multi-flavour project this is** — nothing more. It is not a runtime
identifier and not a drawing backend: `Uno.UI` compiles once and resolves its backend at run time, so no
build-time value can name one. `Skia` became `Generic` because that flavour is the build every drawn-by-Uno
target framework shares; `WebAssembly` became `Wasm`, matching `*.wasm.cs`, `wasm:` and `__WASM__`.

**Why the rename could not move the published layout.** `build/nuget/Uno.WinRT.nuspec` and
`Uno.Foundation.nuspec` hardcode both the source path (`bin\Uno.WinRT.Skia\…`, a project name) and the target
(`uno-runtime\net11.0\skia`). Nothing there reads the property, and no in-repo project imports the
`build/nuget` packing targets — those exist for third-party library authors, where `UnoRuntimeIdentifier`
remains the contract. The only in-repo place a flavour value became a folder path was the
`UnoNugetOverrideVersion` dev loop, which now maps through `_UnoRuntimeFolderName` (`Generic` → `skia`,
`Wasm` → `webassembly`) so it writes where the packages actually ship.

The library-authoring model is superseded by multi-targeting now that per-platform target frameworks behave
normally (spec 056 and the 7.0 platform-asset change). Documented as superseded; not removed, because
published packages depend on the *consuming* half.

## 7. Deliberately not done

- **Renaming the `uno-runtime/<tfm>/{skia,webassembly}` folders.** See §4.1. If it ever happens it needs the
  UNOB0023 guard shipped and soaked first, plus a released transition period probing both names.
- **The third-party wasm enumeration defect.** On a browser head, a third-party cross-runtime package's
  assembly is taken from the shared folder rather than its browser build. Real, but a behaviour change for
  shipped packages and not what this work is about.
- **`__SKIA__`, `HAS_UNO_SKIA`, `*.skia.cs`.** Spec 056 owns these. `UnoRuntimeFlavor=Generic` now defines
  `__SKIA__` and selects `*.skia.cs`, so the symbol and the suffix are the last in-repo spellings of `skia` on
  this axis. Renaming them is mechanical but touches thousands of `#if` sites, which is why it is its own
  change rather than a rider on this one.
- **The `uno-runtime/<tfm>/{skia,webassembly}` folder names themselves**, and the `skia` host pseudo-platform
  in the hot-reload protocol. Both are shipped surface that a drawing-backend axis would actually collide
  with, and both need coordinating with the drawing-backend work (unoplatform/uno#24153) rather than being
  decided here.
- **The `skia` host pseudo-platform in the hot-reload protocol** — reported for a desktop head by
  `GetRuntimeTargetFramework` and matched server-side by the `['', 'desktop', 'skia']` family. It re-occupies
  the name the moment this work frees it, so freeing `skia` is incomplete until it moves. Belongs with the
  drawing-backend work.

## 8. An invariant worth writing down

`HandleForRuntimeEnabled` enumerates `*.dll` over the **shared** folder and only then redirects individual
assemblies. That folder's file listing is therefore the *authoritative asset list*: an assembly a package ships
only under `webassembly` and not under `skia` is silently dropped on a browser head. Uno's own packages are
unaffected because their file sets are identical, but anything that changes the enumeration basis — including
fixing §7's third-party defect — must account for this first.
