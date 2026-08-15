# Dead conditional branches in Uno Platform 7.0

**Status**: Proposal
**Audience**: Internal engineering (Uno Platform maintainers)

> Third of three. `specs/056-platform-targeting-vocabulary/spec.md` covers the platform/host vocabulary;
> `specs/057-preprocessor-constant-cleanup/spec.md` covers the constants themselves. This one covers the
> **`#if` branches that can no longer be taken** — the code behind the names.

## 1. Method

A conditional is dead when its expression evaluates to a constant for every compilation the file takes part in.
Establishing that needs three inputs, not one:

1. **The effective define set per project**, taken from MSBuild rather than inferred — e.g. for
   `src/Uno.UI/Uno.UI.csproj`:
   `__SKIA__ __CROSSRUNTIME__ UNO_REFERENCE_API HAS_UNO HAS_UNO_WINUI IS_UNO IS_UNO_UI_PROJECT
   HAS_COMPOSITION_API HAS_INPUT_INJECTOR HAS_RENDER_TARGET_BITMAP SUPPORTS_RTL UNO_HAS_BORDER_VISUAL
   UNO_HAS_ENHANCED_LIFECYCLE UNO_HAS_MANAGED_POINTERS UNO_HAS_MANAGED_SCROLL_PRESENTER UNO_MIXIN_GENERATION
   UNO_SUPPORTS_NATIVEHOST ENABLE_LEGACY_TEMPLATED_PARENT_SUPPORT`
2. **Which files are compiled into more than one project.** Roughly 15 files under `src/Uno.UI` are
   `Compile Include`d by `Uno.UI.Composition`, `Uno.UI.SourceGenerators` and the SamplesApp heads. Their
   conditionals are *not* dead and must be excluded from any sweep.
3. **Configuration-dependent symbols must not be folded.** `DEBUG`, `TRACE`, `IS_CI`, `IS_CI_OR_DEBUG`,
   `RUNTIME_CORECLR` and `RUNTIME_NATIVE_AOT` vary by configuration or publish mode; treating a Debug dump as
   the truth would wrongly mark every `#if DEBUG` as constant.

Each `#if` / `#elif` expression was then parsed and evaluated (`!`, `&&`, `||`, parentheses) against that set,
with file-local `#define`s honoured. Only expressions that reduce to a constant are reported.

## 2. Results

### 2.1 `src/Uno.UI` — the bulk of it

**356 always-false** and **643 always-true** conditionals across ~348 files, excluding shared files and
`Generated/`.

The symbols making conditionals **always false** — i.e. code that is never compiled:

| Count | Symbol | Disposition |
|---:|---|---|
| 89 | `IS_UNIT_TESTS` | **Delete.** Nothing outside `Uno.UI.csproj` compiles these files |
| 26 | `__NETSTD_REFERENCE__` | **Delete.** Uno.UI no longer builds a Reference flavor |
| 13 | `NETFX_CORE` | **Delete.** UWP era |
| 8 | `IS_UNO_COMPOSITION` | Keep — these sit in the files shared with `Uno.UI.Composition` |
| 7 | `UNO_HAS_UIELEMENT_IMPLICIT_PINNING` | **Delete.** Not defined for Uno.UI |
| 5 | `WASM_SKIA` | ~~Delete~~ — **keep, all sites are in shared files** |
| 4 | `NETSTANDARD` | ~~Delete~~ — **keep, all sites are in shared files** |
| 3 | `__ANDROID__`, 3 `WINUI` | ~~Delete~~ — **keep, all sites are in shared files** |

> **Correction.** Four of the symbols above turned out to have *every* site inside the 41 files Uno.UI shares
> with another project, where they are genuinely defined: `WASM_SKIA` and `__ANDROID__` in the WebView native
> sources compiled by `Uno.UI.Runtime.Skia.WebAssembly.Browser` / `.Android` / `.AppleUIKit`, `NETSTANDARD` in the
> sources compiled by `Uno.UI.SourceGenerators`, and `WINUI` in `VisibleBoundsPadding.cs`, compiled by
> `Uno.UI.Toolkit.Windows`. Sweeping them would have deleted live code. The "always false" counts in this table
> were computed for the `Uno.UI.csproj` compilation only, so they are correct *for that project* and wrong as a
> deletion list — §1 input 2 is not an optional refinement, it decides the answer.
| 26 + 15 + 11 + … | `ApplicableRangeType`, `MUX_PRERELEASE`, `MUX_DEBUG`, `TRACE_HIT_TESTING`, `PROFILE`, `MFSI_DEBUG`, `TICKBAR_DBG`, `CHECK_LAYOUTED`, `DBG`, `IsMouseWheelZoomDisabled`, … | **Keep** — ported-code parking, MUX conventions and developer diagnostics, per spec 057 |

The symbols making conditionals **always true**:

| Count | Symbol | Disposition |
|---:|---|---|
| 275 | `__SKIA__` | Redundant — Uno.UI is Skia-only. But see §3 |
| 221 | `HAS_UNO` | **Keep.** In MUX-ported files this marks Uno's deviation from upstream — see §3 |
| 45 | `__CROSSRUNTIME__` | Redundant |
| 40 | `UNO_REFERENCE_API` | Redundant |
| 26 | `IS_UNO_UI_PROJECT` | Redundant |
| 24 / 22 / 21 / 14 / 13 / 12 / 7 / 3 / 2 / 1 | `UNO_HAS_MANAGED_POINTERS`, `IS_UNO`, `ENABLE_LEGACY_TEMPLATED_PARENT_SUPPORT`, `HAS_UNO_WINUI`, `UNO_HAS_ENHANCED_LIFECYCLE`, `HAS_RENDER_TARGET_BITMAP`, `SUPPORTS_RTL`, `UNO_HAS_BORDER_VISUAL`, `UNO_SUPPORTS_NATIVEHOST`, `UNO_HAS_MANAGED_SCROLL_PRESENTER` | Feature flags that are now unconditional — see §4 |

### 2.2 `src/Uno.UI.Composition`

**7 always-false**, **25 always-true**, across 14 files. Same drivers, much smaller.

Worth noting while here: `Uno.UI.Composition.csproj:16` defines `XAMARIN`, and `Uno.UI.csproj` does not. Nothing
in Composition branches on it. It is a leftover.

### 2.3 `#if false` blocks

**144 blocks across 93 files.** These are unambiguous — the author disabled the code deliberately. They are
worth a separate decision from the rest: some are "keep for reference", some are abandoned. They should be
triaged individually, not swept.

### 2.4 Legacy platform symbols, repository-wide

Never defined anywhere, so dead in every compilation:

`SILVERLIGHT` (8), `DOTNET` (6), `METRO` (3), `WINPRT` (2), `__XAMARIN__` (2), `XAMARIN_ANDROID` (2),
`WINDOWS_PHONE` (1), `WPF_APP` (1), `__UWP__` (1), `UAP10_0_19041` (1).

Native-renderer leftovers of the same shape: `HAS_NATIVE_COMMANDBAR` (3), `IS_NATIVE_ELEMENT` (3),
`SUPPORTS_NATIVE_DATEPICKER` (1).

## 3. What must not be swept

This is the part a mechanical sweep gets wrong.

**`#if HAS_UNO` in MUX-ported files is an annotation, not a condition.** 221 occurrences in `src/Uno.UI`. It marks
where Uno deviates from the upstream WinUI source, which is exactly the information the porting rules exist to
preserve — `/winui-port` requires fidelity to the original so a future re-port can diff against upstream.
Unwrapping them because they are "always true" would delete the provenance and make the next sync harder. The
same argument covers `MUX_PRERELEASE` and `MUX_DEBUG`.

**`#if __SKIA__` is a weaker case but the same shape.** It is genuinely redundant inside `Uno.UI` today, but it
also documents "this is the Skia path" in files that used to have several. With the drawing-backend abstraction
in flight, removing 275 markers immediately before a large rendering refactor lands is poor timing.

**Ported-code parking and developer diagnostics** are covered by spec 057 §3.3 and §3.5 and must be excluded by
name from any automated sweep.

## 4. The feature flags are a separate question

`UNO_HAS_MANAGED_POINTERS`, `UNO_HAS_ENHANCED_LIFECYCLE`, `UNO_HAS_BORDER_VISUAL`,
`UNO_HAS_MANAGED_SCROLL_PRESENTER`, `UNO_SUPPORTS_NATIVEHOST`, `HAS_RENDER_TARGET_BITMAP`,
`HAS_COMPOSITION_API`, `HAS_INPUT_INJECTOR`, `SUPPORTS_RTL`, `ENABLE_LEGACY_TEMPLATED_PARENT_SUPPORT` are all
unconditionally defined for Uno.UI now. Each was a migration switch for a feature that has since landed
everywhere.

They are dead in the same technical sense as the rest, but each represents a completed migration, and unwrapping
one is a small readability win with a small regression risk. They are best handled one flag at a time by whoever
owns the feature, not in a bulk sweep.

## 5. Suggested sequencing

1. **`IS_UNIT_TESTS` (89) and `__NETSTD_REFERENCE__` (26)** in `src/Uno.UI` — the largest genuinely-dead groups,
   both with an unambiguous cause. Delete the always-false blocks.
2. **`NETFX_CORE`, `NETSTANDARD`, `WASM_SKIA`, `WINUI`, `__ANDROID__`, `UNO_HAS_UIELEMENT_IMPLICIT_PINNING`** —
   small, same treatment.
3. **The legacy platform symbols in §2.4** — repository-wide, mechanical.
4. **`#if false`** — triage individually; do not sweep.
5. **The feature flags in §4** — per flag, per owner.
6. **`__SKIA__` / `HAS_UNO`** — defer until the drawing-backend work has landed, and decide the MUX-annotation
   question explicitly rather than by sweep.

## 6. Open decisions

1. **Is `#if HAS_UNO` kept as a permanent porting annotation?** If yes, it should be documented as such in the
   porting rules so no future audit proposes removing it. If no, 221 sites unwrap.
2. **Does `__SKIA__` survive the backend abstraction?** It is repository-internal, so it is cheaper to change
   than the consumer-facing names — but it should change once, with the backend work, not twice.
3. **Who owns the feature-flag retirements in §4?** Ten flags, each a completed migration, each needing someone
   who knows whether the flag still guards a real difference on some target.
4. **Are the 144 `#if false` blocks worth keeping at all?** They are invisible to every build and to most
   searches; git history preserves them either way.
