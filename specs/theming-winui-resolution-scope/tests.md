# Resolution Scope & Providing-Dictionary Pinning — Test Suite

> Companion to [`plan.md`](./plan.md) and [`architecture.md`](./architecture.md). Phase 0 baselines + makes the
> repros trustworthy oracles; Phase 1 turns the popup leaks green; later phases keep them green and add parity guards.

## Principles

- **WinUI is the oracle.** Every WinUI-runnable repro must be **green on `/winui-runtime-tests`** first — it defines
  correct behavior. If Uno disagrees with a WinUI-green test, Uno is wrong; fix Uno, not the expected value.
- **Deterministic colors.** Use sentinel brushes in `ThemeDictionaries["Light"]`/`["Dark"]`/`["Default"]` (the
  existing repros use `Light=Green`, `Dark=Red`, `Default=Red`); assert exact `Color` values.
- **Deterministic ambient.** Pin the application theme (`ThemeHelper.UseApplicationDarkTheme()`, `#if HAS_UNO`,
  `[RequiresFullWindow]`) or the OS theme (`ThemeHelper.UseSystemThemeOverride(ApplicationTheme.Dark)`, Uno-only;
  **throws on WinUI** so exclude `NativeWinUI`). Do **not** depend on the developer's OS theme.
- **Platform reach.** Popup element-theme repros are Skia/WASM only; exclude on native
  (`[PlatformCondition(Exclude, NativeAndroid|NativeIOS)]`) — native is OS + application theme only.
- **No private tracker IDs in committed code.** Remove the existing `[GitHubWorkItem(...kahua-private...)]` URLs when
  touching these repros.

---

## §A. Validated Skia baseline (current branch, before this spec's changes)

Run on Skia Desktop (`SamplesApp.Skia.Generic`, net10.0, Release). Each popup repro: `ActualTheme` is correctly
`Light` but the `{ThemeResource}` (a brush in an ancestor's **local** `ThemeDictionaries`) resolves
`#FFFF0000` (the **Dark/Red** sentinel) under a Dark application — proving the value tracks `Themes.Active`, not the
owner theme.

| Repro | Baseline (Skia) | Note |
|---|---|---|
| `Given_Flyout.When_Flyout_Opened_From_Light_Subtree_Under_Dark_App_Resolves_Light_ThemeResource` | **FAIL** — got `#FFFF0000`, expected Green | app pinned Dark; host `RequestedTheme=Light` |
| `Given_MenuFlyout.When_Flyout_Menu_Uses_Owner_Subtree_Theme_Light_Under_Dark_App` | **FAIL** — `#FFFF0000` | |
| `Given_MenuFlyout.When_MenuFlyout_Item_Uses_Owner_Subtree_Theme_Light_Under_Dark_App` | **FAIL** — `#FFFF0000` | |
| `Given_ToolTip.When_ToolTip_Label_Uses_Owner_Subtree_Theme_Light_Under_Dark_App` | **FAIL** — `#FFFF0000` | |
| `Given_ListViewBase.When_Grid_Row_Presented_After_Tab_Navigation_Light_Under_Dark_App` | **FAIL** — `#FFFF0000` | row shown via popup after unload/reload |
| `Given_Flyout.When_Flyout_Opened_From_Inner_Light_Boundary_Resolves_Light_ThemeResource` | **PASS (by luck)** | does not pin app theme; matches host OS theme — must be de-flaked (Phase 0 step 4) |
| `Given_ThemeResource.When_Light_Pinned_Subtree_Inside_Dark_App_Resolves_Light_ThemeResource` | **FAIL (malformed)** | `XmlException: 'x' is an undeclared prefix` — uses `x:Name` without `xmlns:x`; fix in Phase 0 step 3 |

After Phase 1 (R1), the five popup `FAIL`s and the de-flaked `When_Flyout_Opened_From_Inner_Light_Boundary` must be
**green**; the malformed repro must be green after its Phase 0 fix.

---

## §B. Guards to add

- **B1 — providing dictionary is pinned (R1).** After loading popup content whose `{ThemeResource}` references an
  ancestor-local themed brush, assert (via an internal test hook or `IsResolved` + a non-leaking inspection) that the
  `ThemeResourceReference` has a non-null pinned dictionary and that `RefreshValue(Light)` yields the Light sentinel
  from anywhere in the tree (Skia/WASM).
- **B2 — sticky across reload (R1).** Realize popup content (pin → Light), unload the host subtree (tab switch /
  recycle), reload, re-open the popup; assert the value is still the Light sentinel (the `Given_ListViewBase` repro
  covers this; keep it green through every phase).
- **B3 — inline `<Popup>` declaration-site resolution (R2).** A `<Popup>` declared inline under a `RequestedTheme`
  boundary with a local themed brush resolves it through the live tree (declaration site reachable via the
  `GetParentFollowPopups` hop). Skia/WASM; green on WinUI.
- **B4 — theme toggle after open / initial-theme refresh gating (R3/R5).** Open a popup, then toggle the application
  theme; assert the value flips, and that a value still from the initial theme is not needlessly refreshed.
- **B5 — global-theme brush still re-themes (R1).** A `{ThemeResource}` to a *global* (Fluent) themed brush, after
  the R1 pin pins the theme-resources **root** (not a Light/Dark sub-dict), still flips correctly on app theme change.
  Guards against accidentally pinning a theme-specific sub-dictionary.
- **B6 — foreground freeze / HC / non-FE owner (R7/R8/R9).** As described in `plan.md` Phase 4.

---

## §C. Validation matrix

| Test group | Skia | WASM | Android | iOS | WinUI (`/winui-runtime-tests`) |
|---|---|---|---|---|---|
| 5 popup repros | green after Phase 1 | green after Phase 1 | — (excluded) | — (excluded) | ✓ must be green (oracle) |
| `When_Flyout_Opened_From_Inner_Light_Boundary` (de-flaked) | green after Phase 1 | green after Phase 1 | — | — | ✓ |
| `Given_ThemeResource` (fixed) | green after Phase 0 | green after Phase 0 | n/a | n/a | ✓ |
| B1/B2/B5 (pinning, sticky, global) | green | green | — | — | ✓ where runnable |
| B3 inline popup (R2) | green after Phase 2 | green after Phase 2 | — | — | ✓ |
| B4 toggle/refresh (R3) | green after Phase 3 | green after Phase 3 | — | — | ✓ |
| B6 fg/HC/non-FE (R4–R9) | green after Phase 4 | green after Phase 4 | — | — | ✓ where runnable |
| Existing theming suite (`Given_ElementTheme`, `Given_FrameworkElement_ThemeResources`, `Given_Theme_Materialization`, …) | stays green | stays green | native subset | native subset | ✓ (regression guard) |

"— (excluded)" = element-level/popup resource-scope test, excluded on native (OS + application theme only).

Run via `/runtime-tests` (Skia default; WASM and native heads where applicable). For WinUI parity use
`/winui-runtime-tests` on Windows.

## Existing coverage to keep green (do not duplicate)

`Given_ElementTheme`, `Given_ThemeResource`, `Given_FrameworkElement_ThemeResources`,
`Given_MergedAppResources_ThemeResource`, `Given_FrameworkElement_FocusVisuals`, `Given_Theme_Materialization`
(from `theming-winui-alignment`). Several are pinned to a Light app for OS-independence — keep that.
