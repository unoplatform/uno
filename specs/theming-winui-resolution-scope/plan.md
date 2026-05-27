# Resolution Scope & Providing-Dictionary Pinning — Phased Implementation Plan

> Read [`architecture.md`](./architecture.md) first; tests in [`tests.md`](./tests.md). Each phase is a self-contained
> unit of work for one implementation agent. Phases are **strictly ordered**: do not start phase N+1 until N's
> acceptance criteria pass.

## How to use this plan

Give each agent: this `plan.md` (its phase + "Global conventions"), `architecture.md`, and `tests.md`. Require the
repo's **Root-Cause First** and **Validation Evidence** protocols (`AGENTS.md`). Work only on the current branch.

## Global conventions

- **Branch:** continue on the current theming branch (`dev/mazi/theming-winui`). Commit continuously, Conventional
  Commits, Co-Authored-By trailer. Never auto-commit beyond staging unless asked (user preference) — stage with
  `git add`, run `dotnet format` first.
- **Port fidelity:** match WinUI **behavior** exactly; cite the C++ `file:line` in code comments (keep the existing
  `MUX Reference:` style). The providing-dictionary model is a faithful port of `CThemeResource`, not a new mechanism.
- **No private tracker references** in committed code/specs (AGENTS.md). The existing repros carry
  `[GitHubWorkItem(...kahua-private...)]` attributes — when touching those tests, **remove** the private URLs (or
  replace with a neutral comment); do not add new private references.
- **Build (Skia, fast iteration):**
  ```
  cd src
  dotnet build SamplesApp/SamplesApp.Skia.Generic/SamplesApp.Skia.Generic.csproj -c Release -f net10.0 -p:UnoFastDevBuild=true
  ```
- **Runtime tests:** prefer the `/runtime-tests` skill (Skia Desktop default; also WASM). Manual fallback:
  ```
  cd src/SamplesApp/SamplesApp.Skia.Generic/bin/Release/net10.0
  $env:UITEST_RUNTIME_TESTS_FILTER = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes("<filter>"))
  dotnet SamplesApp.Skia.Generic.dll --runtime-tests=test-results.xml
  ```
- **WinUI oracle:** use the `/winui-runtime-tests` skill on Windows. Every WinUI-runnable repro must be **green on
  native WinUI** before it is used to judge Uno (it encodes correct WinUI behavior). If a repro fails on WinUI, the
  *test* is wrong — fix the test, not the expected value.
- **Theming test filter (popup repros + suite):**
  ```
  Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls.Given_Flyout|Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls.Given_MenuFlyout|Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls.Given_ToolTip|Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls.Given_ListViewBase|Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Given_ThemeResource|Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Given_ElementTheme|Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Given_FrameworkElement_ThemeResources|Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Given_Theme_Materialization
  ```

---

## Phase 0 — Baseline, oracle, and test hygiene

**Goal.** Lock the red/green baseline, prove the repros are correct WinUI oracles, and fix the two broken repros so
the suite is a trustworthy judge before any product change.

**Depends on.** Nothing.

**Steps.**
1. Run the full theming suite + the five popup repros on Skia Desktop and WASM; record pass/fail (see the validated
   Skia run in [`tests.md`](./tests.md) §A). This is the regression guard for all later phases.
2. **WinUI oracle (mandatory):** run every WinUI-runnable repro (the five popup repros + `Given_ThemeResource`,
   `Given_ElementTheme`, `Given_FrameworkElement_ThemeResources`) on `/winui-runtime-tests`. Confirm each is **green
   on native WinUI**. Record the observed WinUI colors as the expected values.
3. **Fix the malformed repro:** `Given_ThemeResource.When_Light_Pinned_Subtree_Inside_Dark_App_Resolves_Light_ThemeResource`
   fails with `XmlException: 'x' is an undeclared prefix` — its `XamlReader.Load` markup uses `x:Name` without
   declaring `xmlns:x`. Add `xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"`. Re-run; confirm it now passes
   on Skia (non-popup app-resource resolution already works) and is green on WinUI.
4. **De-flake the pass-by-luck repro:** `Given_Flyout.When_Flyout_Opened_From_Inner_Light_Boundary_Resolves_Light_ThemeResource`
   only passes when the host OS theme happens to match. Make it deterministic by pinning the ambient
   (`ThemeHelper.UseSystemThemeOverride(ApplicationTheme.Dark)` `#if HAS_UNO`, excluded on `NativeWinUI`) so the
   outer-Dark / inner-Light boundary is exercised regardless of OS theme. Confirm it then **fails** on current Skia
   (proving the leak) and is green on WinUI.
5. Remove private `[GitHubWorkItem(...kahua-private...)]` URLs from the touched repros (AGENTS.md); keep a neutral
   behavior comment.

**Acceptance criteria.**
- Every repro compiles on Skia, WASM, native heads, and WinUI.
- The five popup repros + the de-flaked `When_Flyout_Opened_From_Inner_Light_Boundary` are **red on current Skia** and
  **green on native WinUI**. The malformed repro is **green** on both.
- Baseline of the existing suite recorded.

**Commit(s).** `test(theming): fix malformed + OS-dependent ThemeResource repros`; `test(theming): record resolution-scope baseline`.

---

## Phase 1 — Capture & pin the providing dictionary at resolution (R1, core)

**Goal.** Port `CThemeResource::m_pTargetDictionaryWeakRef`: capture the providing `ResourceDictionary` at the point
of resolution from the lexical scope and pin it (sticky) into the `ThemeResourceReference`. **This phase turns every
failing popup repro green.**

**Depends on.** Phase 0.

**WinUI references.** `ThemeResource.cpp:39-51` (`SetInitialValueAndTargetDictionary`), `ThemeResource.cpp:63-129`
(`RefreshValue` re-queries pinned dict), `ResourceResolver.cpp:947-986` (`GetDictionaryForThemeReference` rules),
`Resources.cpp:687-819` (`EnsureActiveThemeDictionary`).

**Files.**
- `src/Uno.UI/UI/Xaml/ResourceResolver.cs:323-388` (`ApplyResource`), `:556-630` (`TryStaticRetrieval` overloads;
  the providing-dictionary overload at `:608-630`).
- `src/Uno.UI/UI/Xaml/Data/ThemeResourceReference.cs` (ctor `targetDictionary`, `RefreshValue`).
- `src/Uno.UI/UI/Xaml/DependencyObjectStore.Theming.cs:616-689` (`InnerUpdateResourceBindingsUnsafe` re-pin —
  secondary path), `:243-348` (`UpdateThemeReference`).

**Steps.**
1. In `ApplyResource`, when `isThemeResource`, resolve the initial value using the **providing-dictionary** overload
   `TryStaticRetrieval(specializedKey, context, out value, out providingDictionary)` against the lexical/ambient
   scope, and construct the `ThemeResourceReference` with `targetDictionary: providingDictionary` instead of `null`
   (both the resolved branch `:352-355` and the deferred branch `:378-381`).
2. Apply the `GetDictionaryForThemeReference` mapping when choosing the dictionary to pin (`architecture.md` §5.1):
   local → pin the providing local dict; **global theme dictionary → pin the theme-resources *root*, not the
   theme-specific Light/Dark sub-dict** (so a later theme change re-selects the sub-dict); app resources → pin app
   resources; system/assembly → leave unpinned (rely on `RefreshValue`'s `TryTopLevelRetrieval` fallback). Implement
   this as a small `ResolveDictionaryToPin(providingDictionary)` helper citing `ResourceResolver.cpp:947-986`.
3. Confirm (trace with logging) the lexical scope actually contains the opener-local dictionary at the
   `ApplyResource` call for popup content (the empirical Skia run shows the local brush *is* found there). If the
   scope is only complete at deferred resolution for some construction paths (compiled XAML templates), capture &
   pin at the deferred `InnerUpdateResourceBindingsUnsafe` re-pin instead — but ensure the dictionary searched there
   is the lexical/parse scope (via `ResourceBinding.ParseContext`/`CurrentScope`), not the popup-truncated visual
   walk.
4. Keep the pin **sticky**: never null `_targetDictionary` on unload/reparent. Verify `RefreshValue(ownerTheme)`
   (`ThemeResourceReference.cs:113-153`) re-queries the pinned dict with the owner theme key (it already does once
   non-null).

**Acceptance criteria.**
- Builds on Skia/WASM/native heads.
- **All five popup repros green** on Skia + WASM; still green on WinUI.
- Existing theming suite == Phase 0 baseline (no regressions).
- A new guard test asserts the `ThemeResourceReference` for popup content has a non-null pinned dictionary after
  first resolution (see `tests.md` §B).

**Validation.** Popup repros + full theming suite (Skia + WASM); WinUI parity spot-check.

**Commit.** `fix(theming): pin providing dictionary at resolution for ThemeResource (R1)` — body cites
`CThemeResource::m_pTargetDictionaryWeakRef` and `GetDictionaryForThemeReference`.

---

## Phase 2 — `GetParentFollowPopups` in the parent walks (R2)

**Goal.** Make the resource-dictionary scope walk and the inheritance-parent walk follow popups (`PopupRoot →
Popup → real tree parent` for inline popups), matching WinUI. Required for inline-`<Popup>` parity and the
override/alias walk; flyout content keeps relying on the R1 pin.

**Depends on.** Phase 1.

**WinUI references.** `dependencyobject.cpp:486-518` (`GetParentFollowPopups`), `ScopedResources.cpp:135-171`
(`TraverseVisualTreeResources`), `Popup.cpp:3621-3633` (`ShouldPopupRootNotifyThemeChange` — parented vs parentless).

**Files.**
- `src/Uno.UI/UI/Xaml/DependencyObjectStore.cs:1491-1525` (`GetResourceDictionaries`).
- `src/Uno.UI/UI/Xaml/ThemeResolution.cs:84-86` (`GetInheritanceParent`) and `EstablishThemeAtEnter`
  (`DependencyObjectStore.Theming.cs:101-141`).
- `src/Uno.UI/UI/Xaml/Controls/Popup/Popup*.cs` (PlacementTarget / inline-vs-flyout distinction).

**Steps.**
1. Add a `FollowPopups(DependencyObject)` parent step: when the current node is a `Popup` (or its panel is under a
   `PopupRoot`), continue to the popup's **real tree/logical parent** if it has one (inline popup); for a parentless
   flyout popup, stop at the popup (WinUI parity). Mirror `GetParentFollowPopups`.
2. Use it in `GetResourceDictionaries` (`:1512`) so an inline popup's content reaches its declaration-site
   dictionaries.
3. Use the same hop in `ThemeResolution.GetInheritanceParent` and `EstablishThemeAtEnter` so an inline popup's
   content inherits theme from its declaration site (consistent with the resource walk).
4. Do **not** parent a flyout's `Popup` to its `PlacementTarget` (that would diverge from WinUI — see
   `architecture.md` §2.5). Flyout content scope is the R1 pin; flyout theme is the existing `ForwardThemeToPresenter`.

**Acceptance criteria.**
- Builds all heads.
- Inline-`<Popup>` resource + theme resolution reaches the declaration site (add a repro in `tests.md` §B).
- Popup repros + existing suite stay green; no flyout regression.

**Commit.** `fix(theming): follow popups in resource and inheritance parent walks (R2)`.

---

## Phase 3 — `UpdateThemeReference` parity + `IsValueFromInitialTheme` (R3, R5)

**Goal.** Match WinUI's `UpdateThemeReference` active-walk-vs-refresh decision exactly, including the
`m_isValueFromInitialTheme` perf-guard.

**Depends on.** Phases 1–2.

**WinUI references.** `Theming.cpp:315-346`; `ThemeResource.cpp:140` (`m_isValueFromInitialTheme`).

**Files.**
- `src/Uno.UI/UI/Xaml/Data/ThemeResourceReference.cs` (add `IsValueFromInitialTheme`).
- `src/Uno.UI/UI/Xaml/DependencyObjectStore.Theming.cs:243-348` (`UpdateThemeReference`).

**Steps.**
1. Add `IsValueFromInitialTheme` to `ThemeResourceReference` (true until the first non-initial resolution; set in the
   value-setter analog of `SetLastResolvedValue`).
2. In `UpdateThemeReference`: when the owner is active, attempt the ancestor walk; if it does not resolve, call
   `RefreshValue` only when `IsProcessingThemeWalk || owner.GetTheme() != Theme.None || !themeRef.IsValueFromInitialTheme`
   (port `Theming.cpp:340-342`).
3. Reconcile Phase A's `owner is FrameworkElement { IsLoaded: true }` + `includeAppResources:false` with WinUI's
   `IsActive()` + `LookupScope::LocalOnly` so the same scenarios take the walk vs. the pinned refresh. Document the
   mapping in comments.

**Acceptance criteria.**
- Builds all heads; full theming suite + popup repros green on Skia + WASM; WinUI parity spot-check unchanged.
- A targeted test exercises "theme toggled after popup open" and "value from initial theme not needlessly refreshed".

**Commit.** `refactor(theming): align UpdateThemeReference decision + IsValueFromInitialTheme (R3/R5)`.

---

## Phase 4 — Cache, foreground freeze, high contrast, non-FE owners (R4, R6, R7, R8, R9)

**Goal.** Verify and close the remaining parity items so no adjacent scenario regresses.

**Depends on.** Phases 1–3.

**WinUI references.** `ThemeWalkResourceCache.{h,cpp}` (R6); `framework.cpp` foreground freeze /
`PullInheritedTextFormatting` (R7); `Resources.cpp:718-762` HC selection (R8); `Theming.cpp:166-255` non-FE property
walk (R9).

**Files.**
- `src/Uno.UI/UI/Xaml/ThemeWalkResourceCache.cs` (verify keying with pinned dict + owner theme).
- `src/Uno.UI/UI/Xaml/FrameworkElement.Theming.cs:399-469` (foreground freeze) — confirm `DefaultTextForegroundThemeBrush`
  resolves against owner theme and reaches popup/template content.
- `src/Uno.UI/UI/Xaml/ThemeResolution.cs:42-70` (HC composition) and `ResourceDictionary` HC sub-dict selection.
- `src/Uno.UI/UI/Xaml/DependencyObjectStore.Theming.cs:405-508` (non-FE / behavior owners).

**Steps.**
1. **R4/R6:** confirm `RefreshValue` consults `ThemeWalkResourceCache` with the pinned dict + owner theme, cache
   valid only within `BeginCachingThemeResources`. Adjust only if the R1 pin changed cache identity.
2. **R7:** add a repro where popup/template content with no local `Foreground` inherits the **owner-theme** default
   text brush (Light island under Dark app). Fix if it leaks.
3. **R8:** add a dictionary-selection HC test (HC sub-dict chosen when HC active) through the pinned-dict path.
4. **R9:** add a repro for a `{ThemeResource}`-bound DP on a non-FE DO (a behavior-style host) resolving against the
   owner/`AssociatedObject` theme from a pinned dict.

**Acceptance criteria.**
- Builds all heads; new R7/R8/R9 tests green on Skia + WASM (and WinUI where runnable); full suite green.

**Commit(s).** `test(theming): cover foreground-freeze/HC/non-FE owner resolution (R7-R9)`; fixes as needed.

---

## Phase 5 — Full validation, WinUI parity, cleanup, docs

**Goal.** Prove the whole thing on all enhanced-lifecycle platforms against native WinUI; confirm native unchanged;
document.

**Depends on.** Phases 0–4.

**Steps.**
1. **WinUI oracle re-confirm:** run the entire theming suite (old + new WinUI-runnable tests) on
   `/winui-runtime-tests`; all green.
2. **Uno match:** run the entire theming suite + popup repros via `/runtime-tests` on Skia Desktop and WASM; all
   green, matching WinUI. On Android + iOS run the native-applicable subset (popup element-theme repros excluded);
   confirm OS + application theme switching unchanged (no native regression).
3. **Benchmarks:** run the resource-dictionary benchmarks before/after; confirm neutral-to-better (R1 reduces the
   load-time walk to a fallback).
4. **Docs:** add a short "ThemeResource resolution model" note: a value = f(key, owner theme, **pinned providing
   dictionary**); the providing dictionary is captured at resolution from the lexical scope and is sticky across
   reparenting; popups follow WinUI's split (theme forwarded at open, scope pinned at parse). Update the
   `theming-winui-alignment` README with a pointer to this follow-up.
5. Update memory/spec status with final results.

**Acceptance criteria.**
- Skia + WASM: all theming tests green; WinUI parity confirmed. Android + iOS: native subset green, no regressions.
- Every `{ThemeResource}` carries a pinned providing dictionary after first resolution; pin sticky across
  reparent/reload.
- Benchmarks neutral or improved.

**Commit(s).** `test(theming): full resolution-scope validation + WinUI parity`; `docs(theming): document ThemeResource providing-dictionary model`.

---

## Repro → phase traceability

| Repro / scenario | Fixed primarily by | Verified by |
|---|---|---|
| `Given_Flyout` / `Given_MenuFlyout` / `Given_ToolTip` / `Given_ListViewBase` popup leaks | Phase 1 (R1) | Phase 1 + Phase 5 |
| Inline `<Popup>` declaration-site resolution | Phase 2 (R2) | Phase 2 |
| Theme toggled after popup open; initial-theme refresh gating | Phase 3 (R3/R5) | Phase 3 |
| Foreground freeze / HC / non-FE owner | Phase 4 (R4/R6/R7/R8/R9) | Phase 4 |
| Malformed + OS-dependent repros | Phase 0 (test hygiene) | Phase 0 |
