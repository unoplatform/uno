# Theming WinUI Alignment — Regression Test Suite

> Companion to [`plan.md`](./plan.md) and [`architecture.md`](./architecture.md). The scaffold described here
> **ships with this branch's base** (`origin/dev/mazi/theming-winui`) — it was authored and WinUI-validated by
> the prior effort. Its role now: keep every guard green while the exact port replaces the mechanism underneath.

## What the prior effort established (read first)

- **Correct polarity.** The repros (`Given_Theme_Materialization`, T1–T10) reproduce the **real defect
  shape**: a **Light** element-level subtree under a **Dark** ambient (production apps theme at the *root
  element* level while the OS/app follows Dark), asserting the materialized / recycled / scrolled-in /
  first-opened element resolves **Light**. (The opposite polarity — dark island under a light app — never
  reproduced anything.)
- **Determinism.** The ambient OS theme is pinned via `SystemThemeHelper.SystemThemeOverride`
  (Uno-only; `ThemeHelper.UseSystemThemeOverride(ApplicationTheme)`), so the repros do not depend on the
  developer's machine OS theme. The override drives the same path a real OS theme change would, and the
  application follows it unless it set an explicit theme.
- **Role of the suite on this base.** The base is green: these tests are the **WinUI oracle** (validated on
  `/winui-runtime-tests` / probe app by the prior effort) and the **regression contract** for Phases 1–7 of
  the exact port. Any phase that turns a guard red has either (a) found a place where the base's
  approximation and WinUI disagree — resolve in WinUI's favor with `/winui-runtime-tests`/probe evidence and
  update the test *with that evidence*, or (b) introduced a porting bug — fix Uno.
- **Test-infra invariants (do not regress).** `ThemeHelper.UseDarkTheme()` restores the shared host's
  **original** theme on dispose (restoring from `Application.Current.RequestedTheme` poisons subsequent tests
  on a Dark OS). Ambient-dependent tests pin the app via `ThemeHelper.UseApplicationLightTheme()` +
  `[RequiresFullWindow]`. The suite is OS-independent — keep it that way.

## Principles

- **WinUI is the oracle.** Every test that can run on native WinUI must be green on `/winui-runtime-tests`
  first — it defines correct behavior. Prefer WinUI-portable constructs (element `RequestedTheme` + sentinel
  `ThemeDictionaries`) so a test serves as the oracle. If Uno disagrees with a WinUI-green test, Uno is wrong.
- **For Uno-only-API behaviors, confirm in a WinUI probe app — not by reasoning.** App-level theme switching,
  OS-following suppression, `SystemThemeOverride`, HC-flag scenarios: reproduce in a throwaway native WinUI
  Blank App (`dotnet new winui3`), observe real values, encode them with a "confirmed in WinUI probe app"
  comment.
- **Deterministic colors.** Sentinel brushes in `ThemeDictionaries["Light"]`/`["Dark"]`/`["Default"]`
  (Light `#FF111111`, Dark `#FFEEEEEE`, `Default` → `#FFEEEEEE` to model Fluent's dark `Default`); assert
  exact `Color` values, never evolving Fluent brushes.
- **Deterministic ambient.** `ThemeHelper.UseSystemThemeOverride(ApplicationTheme.Dark)` (Uno-only,
  `#if HAS_UNO`); it **throws on WinUI**, so callers add
  `[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]`.
- **Platform reach.** `Given_Theme_Materialization` does **not** inherit `Given_ElementTheme`'s class-level
  native exclusion. Element-level theme tests (incl. T4/T5) are excluded on native **per-method**
  (`[PlatformCondition(Exclude, NativeAndroid|NativeIOS)]`) — native supports OS + application theme only.
  Tag full-window-dependent tests `[RequiresFullWindow]`.
- **Naming.** Behavior-descriptive names; **no private tracker IDs in committed code**. Map S1–S5 to internal
  trackers only in the gitignored/out-of-repo `baseline-results.md`.
- **Existing coverage to keep green** (do not duplicate): `Given_ElementTheme`, `Given_ThemeResource`,
  `Given_FrameworkElement_ThemeResources`, `Given_MergedAppResources_ThemeResource`,
  `Given_FrameworkElement_FocusVisuals` — several pinned to a Light app for OS-independence; keep that.

## Helper utilities (present on this base)

- `ThemeHelper` (`src/Uno.UI.RuntimeTests/Helpers/ThemeHelper.cs`):
  - `UseDarkTheme()` — element-level Dark on the content root; restores the host's **original** theme.
  - `UseApplicationDarkTheme()` / `UseApplicationLightTheme()` — app-level (`#if HAS_UNO`); assert
    `[RequiresFullWindow]`.
  - `UseSystemThemeOverride(ApplicationTheme)` — overrides the ambient OS theme deterministically; restores
    on dispose; throws on WinUI.
  - `CurrentTheme` — the content root's `ActualTheme`.
- `WindowHelper.WindowContent`, `await WindowHelper.WaitForLoaded(e)`, `await WindowHelper.WaitForIdle()`.

---

## §A. Regression scenarios (`Given_Theme_Materialization`)

Each repro pins a **Dark ambient** (`UseSystemThemeOverride(Dark)`, `#if HAS_UNO`) and themes the subtree
**Light** at the element level (`RequestedTheme="Light"`), then asserts the materialized element resolves the
**Light** sentinel. The outer region is `RequestedTheme="Dark"` so the test also exercises a real "Light
island inside a Dark region" boundary on native WinUI (where the Uno-only override compiles out).

| Test | Scenario | Phase that re-secures it under the exact port |
|---|---|---|
| T1 `When_Virtualized_Item_In_Light_Island_Under_Dark_Ambient_Resolves_Light` | S1: virtualized item (initial + `ScrollIntoView`) | 1 + 3 |
| T2 `When_Item_Recycled_Across_Unload_Reload_Keeps_Light` | S2: recycle across unload/reload (theme persists on Leave; re-Enter re-themes) | 1 + 3 |
| T3 `When_Nested_Template_Cell_Scrolled_Into_View_Resolves_Light` | S3: nested-template cell on scroll | 1 + 3 |
| T4 `When_Flyout_First_Open_From_Light_Region_Uses_Region_Theme` | S4: flyout first open (excluded native) | 7 |
| T5 `When_Popup_First_Open_In_Light_Region_Has_Region_Theme` | S4: popup first open (excluded native) | 7 |
| T6 `When_Control_Added_At_Runtime_Into_Light_Island_Resolves_Light` | S5: runtime-added control (incl. non-FE owners) | 1 + 3 |
| T7 `When_App_Theme_Switches_ThemeResource_Values_Update` | uno #23177: app switch (Uno-only; probe-confirmed) | 6 |
| T8 `When_Inherited_Foreground_At_Theme_Boundary_Stays_Boundary_Theme` | foreground freeze at boundary | 4 |
| T9 `When_App_Theme_Explicit_OS_Change_Is_Suppressed` | explicit app theme suppresses OS following (Uno-only) | 6 |
| T10 `When_Element_Dark_Island_And_Fallback_Does_Not_Leak_Dark` | element Dark island + `"Default"` fallback order (Uno-only) | 5 |

All ten exist on this base and are green (Skia/WASM; T4/T5 per their phase markers) — verify exact current
state in Phase 0 and record in `baseline-results.md`.

---

## §B. Cross-cutting guards

- **No ambient regression:** no `PushRequestedThemeForSubTree`, no `Stack<ResourceKey>` reintroduced; after
  Phase 5, no `Themes.Active` — the only ambient is `CoreServices.RequestedThemeForSubTree` with its three
  WinUI writers (grep guard in Phase 8).
- **Sibling isolation without a global stack:** `Given_ElementTheme` "Context Isolation" tests stay green.
- **Non-FE owner theme:** Phase 1 adds a test that a non-UIElement DO reaches `Enter` and carries the right
  theme; keep existing `When_ThemeResource_On_NonFE_DependencyObject_*` coverage green.
- **Event semantics (Phase 4):** `ActualThemeChanged` handlers observe the NEW theme; `HighContrastChanged`
  raised + `ActualThemeChanged` suppression per `framework.cpp:3346-3386` (flag-driven on Skia,
  probe-app-confirmed).

## §C. Validation matrix

| Test | Skia | WASM | Android | iOS | WinUI parity (`/winui-runtime-tests`) |
|------|------|------|---------|-----|----------------------------------------|
| T1, T2, T3, T6 | green (guard) | green (guard) | — (excluded) | — (excluded) | ✓ (must be green) |
| T4, T5 | green (guard/Phase 7) | green | — (excluded) | — (excluded) | ✓ |
| T7, T8 | green (guard/Phase 4) | green | — | — | ✓ (T7 Uno-only → probe app) |
| T9, T10 | green (Phase 6/5) | green | — | — | probe app (Uno-only) |

"green (guard)" = green on the base and must stay green through the structural port. "— (excluded)" =
element-level theme test, excluded on native: native targets support OS + application theme only.

Run via the `/runtime-tests` skill (Skia default; WASM second); for WinUI parity use `/winui-runtime-tests`.
