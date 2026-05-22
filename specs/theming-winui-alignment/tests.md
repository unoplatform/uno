# Theming WinUI Alignment — Regression Test Suite

> Companion to [`plan.md`](./plan.md) and [`architecture.md`](./architecture.md). Phase 0 builds these
> repros and baselines the existing suite; later phases turn the product-leak scenarios green on every
> platform (and native S4) while keeping the rest green.

## What Phase 0 established (read first)

- **Correct polarity.** The repros (`Given_Theme_Materialization`, T1–T10) reproduce the **real defect
  shape**: a **Light** element-level subtree under a **Dark** ambient (production apps theme at the *root
  element* level while the OS/app follows Dark), asserting the materialized / recycled / scrolled-in /
  first-opened element resolves **Light**. (An earlier draft used the opposite polarity — dark island under
  a light app — which the Skia band-aids already cover, so it never reproduced anything.)
- **Determinism.** The ambient OS theme is pinned via the new `SystemThemeHelper.SystemThemeOverride`
  (Uno-only; `ThemeHelper.UseSystemThemeOverride(ApplicationTheme)`), so the repros do not depend on the
  developer's machine OS theme. The override drives the same path a real OS theme change would (so it also
  exercises *runtime* OS-theme changes), and the application follows it unless it set an explicit theme.
- **Reality check (important).** On **Skia/WASM the minimal repros are GREEN** — the 11 band-aids
  (`UNO_HAS_ENHANCED_LIFECYCLE`) already cover the minimal materialization paths. So these new repros are
  **not** "RED on master (Skia)"; their role is:
  1. **WinUI oracle** — WinUI-portable assertions that encode correct WinUI behavior (must be green on
     `/winui-runtime-tests`).
  2. **Regression guards** — must stay green through the refactor (Phases 1–4) and after the band-aids are
     deleted (Phase 4).
  3. **Native S4 repros** — T4/T5 (popup/flyout first open) are expected **RED on Android/iOS**, where the
     band-aids don't run; Phase 5/7 turn them green.

  The deterministic *Skia* evidence of the leak is the **existing theming suite under a Dark ambient** (it
  was OS-theme-dependent — see below).
- **Test-infra fix (separate from the product bug).** The existing theming suite was OS-theme-dependent
  (green on a Light OS, red on a Dark OS). Root cause: `ThemeHelper.UseDarkTheme()` restored the **shared**
  `XamlRoot.Content` host theme from `Application.Current.RequestedTheme`; on a Dark OS that is Dark, so it
  left the shared host explicitly Dark and poisoned every subsequent test's inherited theme. Fix:
  `UseDarkTheme()` now restores the host's **original** theme, and the affected ambient-dependent tests pin
  the app with `ThemeHelper.UseApplicationLightTheme()` + `[RequiresFullWindow]`. The theming suite is now
  **OS-independent**.

## Principles

- **WinUI is the oracle.** Every test that can run on native WinUI must be green on `/winui-runtime-tests`
  first — it defines correct behavior. Prefer WinUI-portable constructs (element `RequestedTheme` + sentinel
  `ThemeDictionaries`) so a test serves as the oracle. If Uno disagrees with a WinUI-green test, Uno is wrong.
- **For Uno-only-API behaviors, confirm in a WinUI probe app — not by reasoning.** App-level theme switching,
  OS-following suppression, `IsThemeSetExplicitly`, `SystemThemeOverride`, custom-theme: reproduce in a
  throwaway native WinUI Blank App (`dotnet new` WinUI templates — see `plan.md`), observe the real values,
  and encode them with a "confirmed in WinUI probe app" comment.
- **Reproduce first, but know where each repro is RED.** Product-leak scenarios are green on Skia/WASM
  (band-aid-covered) and serve as oracle + regression guards there; they are RED on **native** (S4) and are
  proven on Skia only post-refactor (Phases 3–4) once the band-aids are removed.
- **Deterministic colors.** Don't assert against evolving system/Fluent brushes. Define sentinel brushes in
  `ThemeDictionaries["Light"]`/`["Dark"]`/`["Default"]` (Light `#FF111111`, Dark `#FFEEEEEE`) and assert exact
  `Color` values.
- **Deterministic ambient.** Pin the ambient OS theme with `ThemeHelper.UseSystemThemeOverride(ApplicationTheme.Dark)`
  (Uno-only, `#if HAS_UNO`). The helper **throws on WinUI** (no OS-theme override exists there), so any test
  that calls it must be excluded on WinUI with `[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]`.
- **Platform reach.** These live in a class (`Given_Theme_Materialization`) that does **not** inherit
  `Given_ElementTheme`'s native exclusion, so the popup tests (S4) run on iOS/Android. Tag full-window-dependent
  tests `[RequiresFullWindow]`.
- **Naming.** Behavior-descriptive names; **no private tracker IDs in committed code**. Map S1–S5 to internal
  trackers only in the gitignored `baseline-results.md`.
- **Existing coverage to keep green** (do not duplicate): `Given_ElementTheme`, `Given_ThemeResource`,
  `Given_FrameworkElement_ThemeResources`, `Given_MergedAppResources_ThemeResource`,
  `Given_FrameworkElement_FocusVisuals`. Several of these are now pinned to a Light app
  (`UseApplicationLightTheme()` + `[RequiresFullWindow]`) for OS-independence — keep that.

## Helper utilities

- `ThemeHelper` (`src/Uno.UI.RuntimeTests/Helpers/ThemeHelper.cs`):
  - `UseDarkTheme()` — element-level Dark on the content root; **restores the host's original theme** on
    dispose (not the app-theme-derived value — that was the OS-dependence bug).
  - `UseApplicationDarkTheme()` / `UseApplicationLightTheme()` — app-level (`#if HAS_UNO`); require
    `[RequiresFullWindow]` (they assert it) so the app theme reaches the test content.
  - `UseSystemThemeOverride(ApplicationTheme)` — **new.** Overrides the ambient OS/system theme
    deterministically (analogous to `ScaleOverride`); the app follows it (unless explicitly themed); restores
    on dispose. Uno-only — **throws on WinUI** (callers add `[PlatformCondition(Exclude, NativeWinUI)]`).
  - `CurrentTheme` — the content root's `ActualTheme`.
- `WindowHelper.WindowContent`, `await WindowHelper.WaitForLoaded(e)`, `await WindowHelper.WaitForIdle()`.
- Sentinel-brush pattern: a `ResourceDictionary` with `ThemeDictionaries` `Light`→`#FF111111`,
  `Dark`→`#FFEEEEEE` (and `Default`→`#FFEEEEEE` to model Fluent's dark `Default`).

---

## §A. Regression scenarios (`Given_Theme_Materialization`)

Each repro pins a **Dark ambient** (`UseSystemThemeOverride(Dark)`, `#if HAS_UNO`) and themes the subtree
**Light** at the element level (`RequestedTheme="Light"`, like the production apps' root), then asserts the
materialized element resolves the **Light** sentinel. The outer region is `RequestedTheme="Dark"` so the test
also exercises a real "Light island inside a Dark region" boundary on native WinUI (where the Uno-only
override compiles out).

### T1 — S1: virtualized list item in a Light island resolves Light
`When_Virtualized_Item_In_Light_Island_Under_Dark_Ambient_Resolves_Light`. A `ListView` `RequestedTheme="Light"`
with a `{ThemeResource}` item template; assert an initially-realized **and** a `ScrollIntoView`-realized item
resolve the Light sentinel. **Green on WinUI/Skia/WASM** (band-aid #5 LoadContent). Skia/WASM, `[RequiresFullWindow]`.

### T2 — S2: list item recycled across unload/reload keeps its theme (D4)
`When_Item_Recycled_Across_Unload_Reload_Keeps_Light`. Realize a row in a Light island, unload the island
(tab-switch/recycle), reload, assert the row still resolves Light. Targets the unload-clear staleness (D4).
Skia/WASM, `[RequiresFullWindow]`.

### T3 — S3: nested-template cell scrolled into view resolves Light
`When_Nested_Template_Cell_Scrolled_Into_View_Resolves_Light`. A cell materialized on scroll through a nested
`ContentControl` template (like a data-grid cell) in a Light island. Skia/WASM, `[RequiresFullWindow]`.

### T4 — S4: flyout first open from a Light region uses that region's theme
`When_Flyout_First_Open_From_Light_Region_Uses_Region_Theme`. Open a `Flyout` from a Light island; assert the
content resolves Light on the **first** open and identically on the second (no "fix on second open"). **Expected
RED on native (Android/iOS)** — Phase 5/7. Runs on all platforms (no native exclusion).

### T5 — S4: popup first open in a Light region has Light content
`When_Popup_First_Open_In_Light_Region_Has_Region_Theme`. A bare `Popup` whose child binds the sentinel; assert
Light on first open. **Expected RED on native** — Phase 5/7. All platforms.

### T6 — S5: control added at runtime into a Light island resolves Light
`When_Control_Added_At_Runtime_Into_Light_Island_Resolves_Light`. Add a real control after load into an
already-loaded `RequestedTheme="Light"` parent; assert Light (also covers D1 — non-FE `{ThemeResource}` owners).
Skia/WASM, `[RequiresFullWindow]`.

### T7 — uno #23177: app theme switch updates ThemeResource values
`When_App_Theme_Switches_ThemeResource_Values_Update`. `UseApplicationLightTheme()` → element bound to a sentinel
`{ThemeResource}` → `UseApplicationDarkTheme()`; assert nested values flip Light→Dark. Regression guard (largely
fixed by #23178/#23197). Uno-only (`#else` → `Assert.Inconclusive`, confirmed in WinUI probe app). Skia/WASM,
`[RequiresFullWindow]`.

### T8 — inherited foreground frozen at a theme boundary
`When_Inherited_Foreground_At_Theme_Boundary_Stays_Boundary_Theme`. A `RequestedTheme="Light"` boundary with a
`TextBlock` (no local `Foreground`) under a Dark ambient must use the **Light** default text brush. Exercises the
foreground-freeze emulation. Skia/WASM, `[RequiresFullWindow]`.

### T9 — explicit app theme suppresses OS following (Uno-only)
`When_App_Theme_Explicit_OS_Change_Is_Suppressed`. `UseApplicationLightTheme()` (explicit), then a simulated OS
switch to Dark via `UseSystemThemeOverride(Dark)`; assert the app stays Light (`IsThemeSetExplicitly` guard) and
bound values stay Light. Uno-only (`#if HAS_UNO`); confirmed in WinUI probe app. Skia/WASM, `[RequiresFullWindow]`.

### T10 — element Dark island + theme-dictionary fallback (Uno-only; Phase 6)
`When_Element_Dark_Island_And_Fallback_Does_Not_Leak_Dark`. Under app Light: (a) an element `RequestedTheme="Dark"`
island resolves the standard Dark sentinel; (b) a dictionary that defines only a Dark `"Default"` entry resolves
the **Light** entry (not the dark `"Default"` fallback). Custom-theme decision = ditch (`custom-theme.md`).
Uno-only; Phase 6 turns part (b) green. Skia/WASM, `[RequiresFullWindow]`.

---

## §B. Cross-cutting assertions to add as guards

- **No global theme stack leak** (after Phase 4): the `_requestedThemeForSubTree` stack type no longer exists;
  a compile/reflection guard fails if `PushRequestedThemeForSubTree` is reintroduced.
- **Sibling isolation without the stack:** keep `Given_ElementTheme`'s "Context Isolation" tests green.
- **Non-FE owner theme (D1):** keep/extend `When_ThemeResource_On_NonFE_DependencyObject_*`.

## §C. Validation matrix

| Test | Skia | WASM | Android | iOS | WinUI parity (`/winui-runtime-tests`) |
|------|------|------|---------|-----|----------------------------------------|
| T1, T3, T6 | green (guard) | green (guard) | — | — | ✓ (must be green) |
| T2 | green (guard) | green (guard) | (✓ after P7) | (✓ after P7) | ✓ |
| T4, T5 | green (guard) | green (guard) | **RED → green P7** | **RED → green P7** | ✓ |
| T7, T8 | green (guard) | green (guard) | — | — | ✓ (T7 Uno-only → probe app) |
| T9, T10 | green (guard) | green (guard) | — | — | probe app (Uno-only) |

"green (guard)" = passes today (band-aid-covered) and must keep passing through the refactor. The leak's
deterministic Skia evidence is the existing suite under a Dark ambient (now hardened to be OS-independent).

Run via the `/runtime-tests` skill (Skia default; WASM and native heads where applicable); for WinUI parity use
`/winui-runtime-tests` on Windows.
