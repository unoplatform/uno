# Theming WinUI Alignment — Regression Test Suite

> Companion to [`plan.md`](./plan.md) (Phase 0 builds these as failing repros; later phases turn them green) and [`architecture.md`](./architecture.md).

## Principles

- **WinUI is the oracle.** Every test here (old and new) that can run on native WinUI **must be green on `/winui-runtime-tests` first** — it defines correct behavior. Then `/runtime-tests` confirms Uno matches. If Uno disagrees with a WinUI-green test, **Uno is wrong, not the test.** Prefer WinUI-portable constructs (element `RequestedTheme` + sentinel `ThemeDictionaries`) over Uno-only helpers so a test can serve as the oracle.
- **For Uno-only-API behaviors, confirm in a WinUI probe app — not by reasoning.** When a scenario can't run through the `/winui-runtime-tests` harness (e.g. app-level `RequestedTheme` switching, OS-following suppression, `IsThemeSetExplicitly`), reproduce it in a throwaway native WinUI Blank App (`dotnet new` WinUI templates — see `plan.md` "WinUI probe app"), observe the real WinUI values, and encode those as the test's expected values with a "confirmed in WinUI probe app" comment.
- **Reproduce first.** Every reported issue gets a deterministic runtime test that is **green on WinUI**, **fails on current `master` Uno** (via `/runtime-tests`), and **passes after the responsible phase**.
- **Deterministic colors.** Do not assert against system/Fluent brushes (they evolve). Define sentinel brushes in `ThemeDictionaries["Light"]` / `["Dark"]` (and `["Default"]` / HC where relevant) and assert exact `Color` values, following the existing `BasicThemeResources_Test.xaml` pattern.
- **Platform reach.** Place these in a **new** class `Given_Theme_Materialization` that does **not** inherit `Given_ElementTheme`'s `[PlatformCondition(~(NativeAndroid|NativeIOS))]`, so the popup tests (S4) can run on iOS/Android. Tag full-window-dependent tests `[RequiresFullWindow]`. Use `ThemeHelper.UseApplicationDarkTheme()`/`UseApplicationLightTheme()` (`#if HAS_UNO`) to set app/OS theme, and element `RequestedTheme` for subtree islands.
- **Naming.** Use behavior-descriptive names (no private tracker IDs in committed code). Map to issues only in this doc.
- **Existing coverage to keep green** (do not duplicate): `Given_ElementTheme` (~95 tests incl. `When_Child_Added_To_Light_Parent_In_Dark_App_Uses_Light_Resources`, `When_DataTemplate_LoadContent_Inside_Light_Subtree_Resolves_Light`, `When_ContentTemplate_Materialized_In_Themed_Subtree_Uses_Subtree_Theme`, `When_ThemeResource_On_NonFE_DependencyObject_*`, the code-behind-style `Style` tests, the app-dark-switch tests for public uno #23177), `Given_ThemeResource`, `Given_FrameworkElement_ThemeResources`, `Given_MergedAppResources_ThemeResource`, `Given_FrameworkElement_FocusVisuals`.

## Helper utilities

- `ThemeHelper` — `src/Uno.UI.RuntimeTests/Helpers/ThemeHelper.cs`: `UseDarkTheme()` (element-level on root content), `UseApplicationDarkTheme()`/`UseApplicationLightTheme()` (app-level, `#if HAS_UNO`, restore OS-following on dispose), `AssertFullWindowForApplicationTheme()`, `CurrentTheme`.
- `WindowHelper.WindowContent`, `await WindowHelper.WaitForLoaded(e)`, `await WindowHelper.WaitForIdle()`.
- Sentinel-brush pattern: a small `ResourceDictionary` with `ThemeDictionaries` containing `Light`→`#FF111111`, `Dark`→`#FFEEEEEE` (pick visually distinct, unambiguous values).

---

## §A. Regression scenarios

### T1 — Virtualized list item in a light subtree under a dark app resolves light
**Scenario:** S1 (and the OS/app precedence class). **Phase that greens it:** 3 (with 2). **Platforms:** Skia, WASM. `[RequiresFullWindow]`.
**Setup:** `UseApplicationDarkTheme()`. A `ListView` (`RequestedTheme="Light"`, ~200 items) inside a light `Border`, item template binds a `TextBlock.Foreground`/`Border.Background` to a sentinel `{ThemeResource SentinelBrush}`. Scroll an initially-realized item; also force an off-screen item into view (`ScrollIntoView` + `WaitForIdle`).
**Assert:** the realized item's resolved brush == the **Light** sentinel (`#FF111111`), not the Dark/app value. Repeat the assertion for an item realized *after* scrolling (virtualization realization path).

### T2 — List item recycled on tab navigation keeps its theme
**Scenario:** S2. **Phase:** 2 (D4) + 3. **Platforms:** Skia, WASM. `[RequiresFullWindow]`.
**Setup:** `UseApplicationLightTheme()`. Two "tabs" (two `Grid`s toggled via `Visibility`, or a `Pivot`/`TabView`); Tab B hosts a `ListView` of rows whose text binds a sentinel `{ThemeResource}`. Realize Tab B → switch to Tab A (forces Tab B content unload/recycle) → switch back to Tab B. `WaitForIdle` between switches.
**Assert:** after returning to Tab B, the row text brush is still the **Light** sentinel. Add an adjacent variant under `UseApplicationDarkTheme()` with the `ListView` in a `RequestedTheme="Light"` subtree to prove no dark bleed on recycle.

### T3 — Cell scrolled into view resolves the app theme (pure app-level theme, no element override)
**Scenario:** S3. **Phase:** 2 + 3. **Platforms:** Skia, WASM. `[RequiresFullWindow]`.
**Setup:** `UseApplicationLightTheme()` and **no** element `RequestedTheme` anywhere (pure app-level theme — the S3 shape). A wide/tall `ItemsRepeater` or `TreeView` with cells binding a sentinel `{ThemeResource}`; scroll a previously-unrealized cell into view.
**Assert:** the newly materialized cell's brush == the **Light** sentinel. (Targets the "columns scrolled into view use dark styling" + the OS-vs-app precedence gap when the theme is set only at the Application level.)

### T4 — TextCommandBarFlyout first open uses the app theme
**Scenario:** S4. **Phase:** 5 (Skia/WASM) + 7 (native). **Platforms:** Android, iOS, Skia, WASM.
**Setup:** `UseApplicationLightTheme()`. A `TextBox` with selectable text (no element `RequestedTheme`). Programmatically open its `TextCommandBarFlyout` / context flyout (or invoke the selection flyout). On the **first** open, capture a presenter button/label foreground.
**Assert:** first-open foreground matches the **Light** theme (dark text), then close + reopen and assert the value is identical (no "fix on second open"). Pair with a `MenuFlyout`/`Flyout` first-open variant.

### T5 — Popup first open in a light app has light content
**Scenario:** S4 (isolated popup path). **Phase:** 5 + 7. **Platforms:** Android, iOS, Skia, WASM.
**Setup:** `UseApplicationLightTheme()`. A bare `Popup` whose child binds a sentinel `{ThemeResource}`; open it for the first time after load.
**Assert:** child brush == **Light** sentinel on the **first** open (currently relies on second-open healing). Isolates the popup-subtree theme-establishment gap.

### T6 — Dynamically added control in a light subtree under a dark app resolves light
**Scenario:** S5 (runtime-added control). **Phase:** 2 + 3. **Platforms:** Skia, WASM. `[RequiresFullWindow]`.
**Setup:** `UseApplicationDarkTheme()`. After load, add a new real control (e.g. a `ContentControl` with a templated child binding a sentinel `{ThemeResource}` foreground) into an already-loaded `RequestedTheme="Light"` parent.
**Assert:** resolved brush == **Light** sentinel. Also assert a non-FE `{ThemeResource}` (a `Brush` resource referenced by the new control) resolves light (D1 coverage).

### T7 — App theme switch updates ThemeResource values (regression guard for #23177)
**Issue:** uno #23177. **Phase:** preserved by 2–4. **Platforms:** WASM, Skia. `[RequiresFullWindow]`.
**Setup:** `UseApplicationLightTheme()`, an element binding a sentinel `{ThemeResource}`; then switch to `UseApplicationDarkTheme()`.
**Assert:** the bound value flips Light→Dark sentinel; nested elements update; no spurious `ActualThemeChanged` on the very first walk; `ActualTheme` returns the new value inside the handler (mirrors existing #23178/#23197 tests — keep those green too).

### T8 — Inherited foreground at a theme boundary stays frozen to the boundary theme
**Scenario:** general correctness (foreground-freeze emulation) underpinning the S1/S2 text symptoms. **Phase:** 3 (+ unchanged freeze). **Platforms:** Skia, WASM.
**Setup:** `UseApplicationDarkTheme()`; a `RequestedTheme="Light"` `Border` containing a `TextBlock` with no local `Foreground`.
**Assert:** the `TextBlock`'s effective foreground is the **Light** default text brush, and stays light after an app dark→light→dark cycle (freeze at the boundary; matches `NotifyThemeChangedForInheritedProperties`).

### T9 — Explicit app theme suppresses OS following
**Scenario:** OS/theme leak narrative (S1/S3). **Phase:** 6 (D7). **Platforms:** Skia, WASM. `[RequiresFullWindow]`.
**Setup:** `UseApplicationLightTheme()` (simulating App.xaml `RequestedTheme="Light"` while the OS is dark).
**Assert:** `Application.Current.IsThemeSetExplicitly == true`, `ActualElementTheme == Light`, `ResourceDictionary.GetActiveTheme()` resolves Light; raise a simulated `SystemThemeChanged` (OS→dark) and assert the app does **not** flip to dark (guard at `Application.cs:372`) and bound `{ThemeResource}` values stay light.

### T10 — Custom-theme ditched + theme-dictionary fallback (D7)
**Scenario:** OS/theme leak with `RequestedCustomTheme`; decision = **ditch** ([`custom-theme.md`](./custom-theme.md) Option B). **Phase:** 6. **Platforms:** Skia, WASM. `[RequiresFullWindow]`.
**Setup & assert:**
- (a) **Harmless migration:** set `ApplicationHelper.RequestedCustomTheme = "Light"` + `RequestedTheme=Light`; assert a sentinel `{ThemeResource}` resolves the **standard Light** value (the observed `"Light"` usage keeps working). Confirm `RequestedCustomTheme` is `[Obsolete]` and no longer keys a `ThemeDictionaries["Light"]` custom entry distinct from the standard one.
- (b) **Element theme is standard Light/Dark:** under app Light, an element with `RequestedTheme="Dark"` resolves the standard **Dark** sentinel.
- (c) **Fallback robustness:** a dictionary that defines only a `"Default"` (dark) ThemeDictionaries entry and is consumed under app **Light** resolves the app **base Light** value (or the dictionary's Light entry if present), **not** the dark `"Default"` — proving the D7 fallback no longer leaks dark.

---

## §B. Cross-cutting assertions to add as guards

- **No global theme stack leak:** after Phase 4, the `_requestedThemeForSubTree` type no longer exists; add a small reflection/compile guard test or a code-review checklist item that fails if `PushRequestedThemeForSubTree` is reintroduced.
- **Sibling isolation without the stack:** keep `Given_ElementTheme`'s "Context Isolation (Sibling Independence)" tests green — they prove subtrees with different `RequestedTheme` don't bleed once resolution is owner-driven.
- **Non-FE owner theme (D1):** keep/extend `When_ThemeResource_On_NonFE_DependencyObject_*` to assert a `Brush`/`Setter` value inside a themed subtree resolves the subtree theme.

---

## §C. Validation matrix

| Test | Skia | WASM | Android | iOS | WinUI parity (`/winui-runtime-tests`) |
|------|------|------|---------|-----|----------------------------------------|
| T1, T3, T6 | ✓ | ✓ | — | — | ✓ (nested/dynamic) |
| T2 | ✓ | ✓ | (✓ after P7) | (✓ after P7) | ✓ |
| T4, T5 | ✓ | ✓ | ✓ (P7) | ✓ (P7) | ✓ |
| T7, T8 | ✓ | ✓ | — | — | ✓ |
| T9, T10 | ✓ | ✓ | — | — | ✓ (precedence) |

Run via the `/runtime-tests` skill. For native, build/run the appropriate native head; for WinUI parity use `/winui-runtime-tests` on Windows.
