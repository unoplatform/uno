# Theming WinUI Alignment — Phased Implementation Plan

> Read [`architecture.md`](./architecture.md) first. Tests live in [`tests.md`](./tests.md). This file is the sequence of work; each phase is self-contained enough to hand to a single implementation agent.

## How to use this plan to launch implementation agents

Each phase below is a unit of work for one agent. When dispatching a phase, give the agent:

1. This `plan.md` (it should read **only its phase** plus "Global conventions" below), `architecture.md`, and `tests.md`.
2. The instruction to follow the repo's **Root-Cause First** and **Validation Evidence** protocols (see `AGENTS.md`).
3. The constraint: **work only on the current branch; do not consult other branches or prior theming plans.**

Phases are **strictly ordered** by dependency. Do not start phase N+1 until phase N's acceptance criteria pass. Phases 0–4 fix the bugs; 5–8 complete the alignment and extend to native.

## Global conventions

- **Branch:** one feature branch off `master` for the whole effort (e.g. `dev/mazi/theming-winui-alignment`). Commit continuously — a logical-group commit each time a chunk builds clean (per user preference), Conventional Commits, end messages with the Co-Authored-By trailer.
- **No `_Mux`/`Internal` suffixes for the sake of it.** Use natural WinUI method names. Gate superseded code paths with `#if` rather than renaming where practical. When a new public-ish API must coexist with the old during migration, prefer an internal overload over a renamed method.
- **Port fidelity:** match WinUI **behavior** exactly. Cite the C++ `file:line` you ported from in code comments (the repo already does this throughout the theming files — keep the style). Do not simplify away behavior; the one sanctioned mechanism change (parameter vs ambient slot, Mechanism 1 in `architecture.md` §6) is behavior-preserving and must be justified in the commit body.
- **Build (Skia, fast iteration):**
  ```
  cd src
  dotnet build SamplesApp/SamplesApp.Skia.Generic/SamplesApp.Skia.Generic.csproj -c Release -f net10.0 -p:UnoFastDevBuild=true
  ```
- **Runtime tests:** prefer the `/runtime-tests` skill (handles base64 filter + run + parse). Manual fallback:
  ```
  cd src
  $env:UITEST_RUNTIME_TESTS_FILTER = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes("<filter>"))
  cd SamplesApp/SamplesApp.Skia.Generic/bin/Release/net10.0
  dotnet SamplesApp.Skia.Generic.dll --runtime-tests=test-results.xml
  ```
- **Unit tests:** `dotnet test Uno.UI/Uno.UI.Tests.csproj`.
- **WinUI-first validation (MANDATORY for every phase).** These tests encode *WinUI behavior*, so they must be correct against native WinUI **before** they are used to judge Uno:
  1. Every theming test — **old and new** — that can run on native WinUI (i.e. not gated to Uno-only APIs) **must pass on `/winui-runtime-tests`** (WinAppSDK SamplesApp on Windows). A test that fails on WinUI is a *wrong test* — fix the test/assertion until it is green on WinUI. Do this **when the test is authored** (Phase 0 for the new T1–T10) and re-confirm in Phase 8.
  2. Only after a test is green on WinUI, use **`/runtime-tests`** (Skia Desktop default; also WASM, and native heads where applicable) to run it on Uno and confirm Uno matches.
  - Tests that depend on Uno-only surface (e.g. `ThemeHelper.UseApplicationDarkTheme`, `IsThemeSetExplicitly`, `ResourceDictionary.GetActiveTheme`) cannot run through the `/winui-runtime-tests` harness. **Do not fall back to reasoning** — instead **confirm the behavior empirically in a throwaway WinUI Blank App** (see "WinUI probe app" below), reproducing the same scenario with real WinUI APIs (`Application.RequestedTheme`, `FrameworkElement.RequestedTheme`/`ActualTheme`, `ActualThemeChanged`, etc.) and observing the actual values. Record the observed WinUI values in the test as the expected values, citing "confirmed in WinUI probe app". Still keep WinUI-runnable assertions WinUI-portable wherever possible (prefer element-level `RequestedTheme` + sentinel `ThemeDictionaries` over Uno-only helpers).
  - **Rule of thumb:** if Uno disagrees with a WinUI-green test (or a probe-app-confirmed behavior), Uno is wrong — fix Uno, not the expected value.
- **WinUI probe app (for Uno-only-API behaviors).** When a behavior can't be expressed in the `/winui-runtime-tests` harness, build a minimal native WinUI app from the `dotnet new` WinUI templates and confirm the behavior by hand, then encode the observed result as the test's expected value. Setup (see the official blog: <https://devblogs.microsoft.com/ifdef-windows/introducing-dotnet-new-templates-for-winui/>):
  ```
  dotnet new install Microsoft.WindowsAppSDK.ProjectTemplates   # WinUI dotnet new templates
  dotnet new list winui                                         # confirm the exact short name
  dotnet new winui3 -o ThemingProbe                             # Blank App, Packaged (WinUI 3 in Desktop)
  ```
  Reproduce the scenario in `MainWindow.xaml`/`App.xaml(.cs)` (e.g. set `Application` `RequestedTheme`, toggle OS theme, set element `RequestedTheme`, read `ActualTheme` in an `ActualThemeChanged` handler) and observe the rendered/inspected values on Windows. Keep these probe apps out of the repo (throwaway, under a scratch dir); their *purpose* is to produce trustworthy expected values. Note in the test comment which probe scenario confirmed each expected value.
- **Theming test filter (baseline suite):**
  ```
  Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Given_ElementTheme|Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Given_ThemeResource|Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Given_FrameworkElement_ThemeResources|Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Given_MergedAppResources_ThemeResource|Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Given_FrameworkElement_FocusVisuals|Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Given_Theme_Materialization
  ```
- **Decision gate (before Phase 3):** confirm Mechanism 1 vs Mechanism 2 (`architecture.md` §6). The plan assumes **Mechanism 1**.

---

## Phase 0 — Baseline + regression test scaffold

**Goal.** Reproduce all reported scenarios (S1–S5 + uno #23177) as runtime tests that **fail today**, and capture a green/red baseline of the existing theming suite, before touching production code. Satisfies the mandatory "reproduce first" protocol and provides the safety net.

**Depends on.** Nothing.

**Steps.**
1. Run the full existing theming suite (filter above, minus `Given_Theme_Materialization` which does not exist yet) on Skia Desktop and WASM. Record pass/fail counts into `D:\Work\uno\specs\theming-winui-alignment\baseline-results.md` (test name → result). This is the regression guard for Phases 1–8.
2. Add a new test class `src/Uno.UI.RuntimeTests/Tests/Windows_UI_Xaml/Given_Theme_Materialization.cs`. **Do not** inherit `Given_ElementTheme`'s `[PlatformCondition(~(NativeAndroid|NativeIOS))]` exclusion — the popup tests must run on native. Implement the scenarios in [`tests.md`](./tests.md) §A (T1–T9). They should compile and **fail** (or be `[Ignore]`-free and red) on current code, proving the repros.
3. Where a scenario needs deterministic colors, add sentinel `ThemeDictionaries["Light"]/["Dark"]` brushes (follow the pattern in existing `BasicThemeResources_Test.xaml`).
4. For the popup first-open scenario (S4) specifically, add the native-capable popup/flyout first-open tests (T4, T5 in `tests.md`).
5. Format any new XAML: `dotnet xstyler -d src/SamplesApp -r` if SamplesApp XAML was touched (these tests are in RuntimeTests, usually no SamplesApp XAML).

**Acceptance criteria.**
- New tests compile on Skia, WASM, and native heads.
- **Each new test (T1–T10) is green on `/winui-runtime-tests` (native WinUI)** — proving it encodes correct WinUI behavior. If a new test fails on WinUI, the test is wrong: fix its setup/assertion (or, if it depends on Uno-only API, restructure it to be WinUI-portable, or mark it Uno-only with the emulated WinUI behavior documented). This is the gate that makes the repros trustworthy.
- After WinUI-green, the same tests **fail on current `master` Uno** via `/runtime-tests` (red), demonstrating the repros. Document the failing assertion + observed vs expected value for each in `baseline-results.md`.
- Existing theming suite baseline recorded (and, for WinUI-runnable existing tests, confirmed green on `/winui-runtime-tests` so they are a valid oracle for later phases).

**Validation.** Runtime tests (Skia + WASM) for `Given_Theme_Materialization`; confirm red. Existing suite still green at baseline.

**Commit.** `test(theming): add failing regression repros for theme materialization (S1–S5)`. Keep test names and commit messages free of private tracker IDs (map S1–S5 to internal trackers only in local, uncommitted notes — e.g. `baseline-results.md`, which must be `.gitignore`d or kept outside the repo). See `tests.md` "Naming".

---

## Phase 1 — Per-object theme on every `DependencyObject` (D1)

**Goal.** Move the per-object theme field off `UIElement` and onto `DependencyObjectStore`, so that **every** `DependencyObject` carries a resolved theme exactly like `CDependencyObject::m_theme`. Behavior-preserving (resolution still uses the global stack at this point).

**Depends on.** Phase 0.

**WinUI references.** `CDependencyObject.h:1761` (`m_theme : 5`), getter `:1648`; `fIsProcessingThemeWalk` bit `CDependencyObject.h:302`.

**Files.**
- `src/Uno.UI/UI/Xaml/UIElement.cs:67-110` (current `_theme`, `GetTheme`, `SetTheme`, `IsProcessingThemeWalk`).
- `src/Uno.UI/UI/Xaml/DependencyObjectStore.cs` / `DependencyObjectStore.Theming.cs` (destination).
- `src/Uno.UI/UI/Xaml/FrameworkElement.Theming.cs` (consumers).

**Steps.**
1. Add to `DependencyObjectStore` (the object every DO owns): `private Theme _theme = Theme.None;`, `internal Theme GetTheme()`, `internal void SetTheme(Theme)`, and the `IsProcessingThemeWalk`/`SetIsProcessingThemeWalk` bit (port the `UIElementFlag` bits comment, citing `CDependencyObject.h:302`). Default `Theme.None`.
2. Expose convenience accessors so existing callers keep compiling:
   - `DependencyObject`-level: an internal `GetTheme()`/`SetTheme()` on `IDependencyObjectStoreProvider` or via `((IDependencyObjectStoreProvider)do).Store.GetTheme()`.
   - Keep `UIElement.GetTheme()/SetTheme()` as **thin forwarders** to `Store` (so the ~50 existing call sites are unchanged).
3. Add `ResolveOwnerTheme(DependencyObject owner)` helper (new, in `DependencyObjectStore.Theming.cs` or a static `ThemeResolution` helper). Logic (mirrors `architecture.md` §6 + WinUI `ActualTheme` fallback `framework.cpp:3970`):
   - if `owner.Store.GetTheme()` is non-`None` → return it;
   - else walk `GetInheritanceParent`/logical parent chain to the nearest DO whose `_theme != None` → return that;
   - else `Application.Current?.ActualElementTheme` mapped to `Theme.Light/Dark` (with HC OR-ed from app — Phase 6 refines HC).
   - **Do not wire it into resolution yet** (that is Phase 3). It is added here so Phase 3 is a small, focused diff.
4. Verify nothing reads `UIElement._theme` directly anymore (only via `GetTheme`).

**Acceptance criteria.**
- Builds on Skia/WASM/native heads.
- Existing theming suite still matches Phase 0 baseline (no regressions — behavior unchanged).
- `ResolveOwnerTheme` unit-covered (a `Uno.UI.Tests` unit test for the fallback chain on a small DO graph).

**Validation.** Build all heads; full theming suite (Skia + WASM) == baseline; new unit test green.

**Commit.** `refactor(theming): move per-object theme to DependencyObjectStore (D1)`.

---

## Phase 2 — Establish theme at tree `Enter` for every DO (D2) + stop clearing on unload (D4)

**Goal.** Every DO entering the live tree inherits its theme from its **logical inheritance parent** before any resource resolves, on **all platforms**. Make `owner.GetTheme()` reliably non-`None` at first resolution. Stop the unload-time `_theme = None` reset that causes recycle staleness.

**Depends on.** Phase 1.

**WinUI references.** `EnterImpl` theme block `depends.cpp:1023-1048`; `GetInheritanceParentInternal(fLogicalParent)` `framework.cpp:3097-3130`; `NotifyThemeChanged` `Theming.cpp:110-157`; `UpdateAllThemeReferences` `Theming.cpp:260-286`; FE `OnRequestedThemeChanged` `framework.cpp:3485-3543`.

**Files.**
- Uno's enter/load path: `src/Uno.UI/UI/Xaml/UIElement.*` enter/leave, `src/Uno.UI/UI/Xaml/FrameworkElement.cs:419-498` (`OnLoadingPartial`), `DependencyObjectStore` enter hooks.
- `src/Uno.UI/UI/Xaml/FrameworkElement.Theming.cs:540-561` (`ClearThemeStateOnUnloaded`).
- Logical-parent accessor (popups): `src/Uno.UI/UI/Xaml/Controls/Popup/Popup*.cs`, `FrameworkElement` logical-parent plumbing.

**Steps.**
1. Implement an `EnterImpl`-equivalent theme step that runs when a DO goes live (port `depends.cpp:1023-1048`):
   - Resolve the inheritance parent: for `FrameworkElement`, the **logical** inheritance parent (so popups/flyouts inherit from the opener); otherwise the visual/inheritance parent.
   - If `parent.GetTheme() != None && parent.GetTheme() != this.GetTheme()` → `NotifyThemeChanged(parent.GetTheme())` (re-themes this subtree, re-applies this element's own `RequestedTheme` override via the existing override logic at `FrameworkElement.Theming.cs:180-183`).
   - Else → `UpdateAllThemeReferences()` (re-resolve refs against now-available ancestor dictionaries, without changing `_theme`).
   - This must run for **every DO** on enter, not just FE on `OnLoadingPartial`, and **not** be gated by `UNO_HAS_ENHANCED_LIFECYCLE` (native participates — but see Phase 7 for native tree-attach specifics; for this phase, wire it into the cross-platform enter where one exists, and leave a clearly-marked TODO + `#if` for native if the native attach hook is not yet uniform).
2. Reconcile with the existing `OnLoadingPartial` inheritance (`FrameworkElement.cs:425-442`): the new `Enter` step subsumes it. Remove the duplicate inheritance logic from `OnLoadingPartial`, keeping only what is not theme-establishment. Keep the band-aid push for now (removed in Phase 4).
3. **D4:** change `ClearThemeStateOnUnloaded` (`FrameworkElement.Theming.cs:540-561`) to match WinUI — WinUI does **not** clear `m_theme` on leave; theme persists and re-`Enter` re-themes from the parent. Either delete the reset or convert it to a no-op with a comment citing that `EnterImpl` re-establishes theme on re-attach. Verify no test relied on the clear behavior; if one did, it was asserting the buggy behavior — update it.
4. Ensure ordering: the `Enter` theme step (esp. `UpdateAllThemeReferences`) must run **before or as part of** the first theme-ref resolution at load, and before `ApplyResource`-created refs are first resolved. Trace `ApplyResource` (`ResourceResolver.cs:251-362`) → first `UpdateResourceBindings`/`UpdateAllThemeReferences` ordering vs the enter hook. Document the ordering in a comment.

**Acceptance criteria.**
- Builds on all heads.
- After this phase (leaf still uses the global stack), `owner.GetTheme()` is non-`None` for in-tree elements at first resolution — add a temporary debug assert or a runtime test that reads `GetTheme()` on a freshly-loaded child under a themed parent and confirms it equals the parent theme (already covered by `Given_ElementTheme` "Visual Tree Entry/Exit" tests — they must stay green).
- Existing theming suite == baseline (still no behavior change in resolution, since the leaf hasn't changed). The recycle test (T2) may *partially* improve; record.

**Validation.** Build all heads; full theming suite (Skia + WASM) == baseline; `Given_ElementTheme` entry/exit + dynamic-child tests green.

**Commit(s).** `feat(theming): establish element theme at tree Enter for every DO (D2)`; `fix(theming): stop clearing element theme on unload to fix recycle staleness (D4)`.

---

## Phase 3 — Resolve `{ThemeResource}` against the owner's effective theme (D3, Mechanism 1)

**Goal.** The resolution leaf becomes a pure function of (key, owner's effective theme). This is the phase that fixes the bugs. After it, the global push stack is dead code (still present, removed in Phase 4).

**Depends on.** Phases 1–2. **Mechanism 1 confirmed**, under the **zero-behavior-difference constraint** (`architecture.md` §6): the parameter is approved only if it provably cannot change resolution behavior. This phase must *prove* equivalence before Phase 4 deletes the stack:
- Add a transitional choke-point assert while the legacy stack and the new parameter coexist: `Debug.Assert(ResolveOwnerTheme(owner).Key == GetActiveTheme().Key)` (or a logged divergence counter) at the single resolution choke point.
- Run the **entire** theming suite + `Given_Theme_Materialization` on Skia and WASM with **zero** assertion hits. Any divergence is either a genuine pre-existing bug the parameter fixes — capture it as an intended, test-backed change with a note — or a plumbing error to correct. Do not proceed to Phase 4 until the assert is silent across the full suite.

**WinUI references.** `SetThemeResourceBinding` (central owner-theme push) `Theming.cpp:349-400`; `UpdateThemeReference` `Theming.cpp:288-346`; `CThemeResource::RefreshValue` `ThemeResource.cpp:63-129`; `EnsureActiveThemeDictionary` (theme→sub-dict) `Resources.cpp:687-819`; `ThemeWalkResourceCache` key incl. subtree theme `ThemeWalkResourceCache.h:55`.

**Files.**
- `src/Uno.UI/UI/Xaml/ResourceDictionary.cs:259-390` (`TryGetValue` overloads), `:590-616` (`GetActiveThemeDictionary`, `GetThemeDictionary`).
- `src/Uno.UI/UI/Xaml/Data/ThemeResourceReference.cs:105-197` (`RefreshValue`, `RefreshValueWithTreeWalk`).
- `src/Uno.UI/UI/Xaml/DependencyObjectStore.Theming.cs:125-223` (`UpdateThemeReference`), `:79-107` (`UpdateAllThemeReferences`).
- `src/Uno.UI/UI/Xaml/ThemeWalkResourceCache.cs:38, 104, 133`.
- `src/Uno.UI/UI/Xaml/ResourceResolver.cs` (`ResolveResourceStatic`/`TryStaticRetrieval` — add theme-aware overloads used by Phase 6; for now thread theme where an owner is available).

**Steps.**
1. Add a `Theme theme` parameter to the leaf lookups (keep the existing parameterless overloads as thin wrappers that pass `GetActiveTheme()` during migration so unrelated callers compile):
   - `ResourceDictionary.TryGetValue(in ResourceKey key, in ResourceKey themeKey, out object value, bool shouldCheckSystem)` and the `out ResourceDictionary providingDictionary` variant. Replace the `GetActiveThemeDictionary(GetActiveTheme())` calls at `:294` and `:364` with `GetActiveThemeDictionary(themeKey)`. Thread `themeKey` into `GetFromMerged` recursion so merged dictionaries select the same theme.
   - Map `Theme` → `ResourceKey` ("Light"/"Dark"/"Default"/HC keys) with a single helper; HC composition handled in Phase 6 (for now base Light/Dark).
2. `ThemeResourceReference.RefreshValue(Theme ownerTheme, …)` and `RefreshValueWithTreeWalk(Theme ownerTheme, …)`: convert `ownerTheme` → theme key and pass it into `dict.TryGetValue(key, themeKey, …)`. The `owner` parameter that is currently ignored for theme selection is replaced by `ownerTheme` (compute it once in the caller).
3. `DependencyObjectStore.UpdateThemeReference` (the analog of WinUI `SetThemeResourceBinding`): compute `var ownerTheme = ThemeResolution.ResolveOwnerTheme(owner);` **once**, and pass it into Phase A ancestor walk (`dict.TryGetValue(key, ownerTheme, …)`) and Phase B (`themeRef.RefreshValue(ownerTheme, cache)`). This is the single centralized "use the owner's theme" point, mirroring `Theming.cpp:368-376`.
4. `ThemeWalkResourceCache`: add `theme` to the cache key explicitly (`TryGetCachedValue(dict, key, theme, …)` / `CacheValue(dict, key, theme, value)`), replacing the internal `GetActiveTheme()` reads at `:104`/`:133`. Callers pass the same `ownerTheme`.
5. Keep `Themes.Active` as the **fallback** inside `ResolveOwnerTheme` only. Leave the `_requestedThemeForSubTree` stack and the 11 pushes in place (now harmless — the leaf no longer reads the stack). They are removed in Phase 4.
6. Audit the static parse-time path: `GetSimpleStaticResourceRetrieval` emits `ResolveResourceStatic(key, type, context)` (`XamlFileGenerator.cs:4914-4924`) with no theme. For `{StaticResource}` this is fine (theme-invariant or resolved once). For `{ThemeResource}` on a DP, the dynamic `ApplyResource` path (`XamlFileGenerator.cs:4259-4268`) is what matters and now uses the owner theme via step 3. Confirm no `{ThemeResource}` relies solely on the static path.

**Acceptance criteria.**
- Builds all heads.
- **Bugs fixed:** T1, T2, T3, T6, T7 (and the existing element-theme / code-behind-style repros + public uno #23177) go **green** on Skia + WASM.
- Existing theming suite: **all green** (the band-aid pushes are now no-ops but present; tests must still pass). No regressions vs baseline.
- Spot-verify with `/winui-runtime-tests` that nested-theme + dynamic-child scenarios match native WinUI.

**Validation.** Full theming suite + `Given_Theme_Materialization` (Skia + WASM) green; targeted WinUI parity run for nested/dynamic scenarios.

**Commit.** `fix(theming): resolve ThemeResources against owner's effective theme (D3)` — body explains Mechanism 1 and that it is behavior-equivalent to WinUI's `SetThemeResourceBinding` owner-theme push.

---

## Phase 4 — Remove the 11 band-aid pushes + delete the global stack

**Goal.** Prove the architecture by deleting all the scattered pushes and the process-global `_requestedThemeForSubTree` stack. If anything regresses, a materialization path is missing theme-at-Enter (a Phase 2 gap) — fix the root, do not re-add a push.

**Depends on.** Phase 3 green.

**Files (remove pushes — see `architecture.md` §5 for the full table):** `FrameworkElement.Theming.cs:255/318, 461/485`; `FrameworkElement.cs:454/487, 734/745`; `FrameworkTemplate.cs:202/246`; `VisualStateGroup.cs:259/279, 393/420`; `Data/BindingHelper.cs:82/95`; `Documents/Hyperlink.cs:251/289`; `Media/Animation/ObjectAnimationUsingKeyFrames.cs:352/365`; `DependencyProperty.cs:636/650`. Then `ResourceDictionary.cs:916-937, 982-1017` (the `Push/Pop…ForSubTree` API + `Themes._requestedThemeForSubTree`).

**Steps.**
1. Remove each push/pop pair. For each removed site, replace it (where it was doing real work besides pushing) with the now-theme-aware resolution call passing the relevant owner's theme:
   - **#2 foreground freeze** (`NotifyThemeChangedForInheritedProperties`): the `DefaultTextForegroundThemeBrush` lookup now passes the element's theme to `TryStaticRetrieval`/`TryGetValue` instead of pushing. Add a theme-aware `ResolveResourceStatic`/`TryStaticRetrieval` overload if needed.
   - **#11 focus visual default** (`DependencyProperty.cs:633-650`, PR #23243): `ResolveFocusVisualBrushDefault` resolves `SystemControlFocusVisual…Brush` passing `GetEffectiveThemeKey(targetObject)` (== `ResolveOwnerTheme`) instead of pushing. Matches `CDependencyProperty::GetDefaultFocusVisualBrush` resolving against `targetObject->GetTheme()`.
   - **#4 `OnStyleChanged`** (PR #23127), **#5 template `LoadContent`**, **#6/#7 visual states**, **#10 animation keyframes**, **#9 Hyperlink**: each becomes "resolve with the owner element's theme" — no push.
2. Delete `PushRequestedThemeForSubTree`/`PopRequestedThemeForSubTree`/`…ByName` (`ResourceDictionary.cs:916-937`) and the `_requestedThemeForSubTree` stack (`:990, 1003-1016`). Keep `Themes.Active`, `SetActiveTheme`, `GetActiveTheme` (now used only as the app fallback inside `ResolveOwnerTheme` and for app-level resource lookups with no owner).
3. Re-point the control-level `UpdateThemeBindings` overrides (Popup, PopupRoot, CommandBar, ContentPresenter, IconElement, TextBlock, TextBox) to the new theme-aware resolution — they remain as propagation hooks but no longer rely on the stack.
4. Grep the whole repo for any remaining `PushRequestedThemeForSubTree`/`RequestedThemeForSubTree` references and remove/convert them.

**Acceptance criteria.**
- Builds all heads.
- Full theming suite + `Given_Theme_Materialization` (Skia + WASM): **all green** with zero band-aids and no global stack.
- Add a leak-check test: after a top-level theme change, no residual global state (the stack type no longer exists, so this is mostly a "does not compile if reintroduced" guard) — and `Given_ElementTheme` sibling-isolation tests stay green (they prove no cross-subtree bleed without the stack).

**Validation.** Full suite green on Skia + WASM; `/winui-runtime-tests` parity spot-check unchanged.

**Commit(s).** `refactor(theming): resolve focus/style/template/state/animation ThemeResources via owner theme`; `refactor(theming): delete global requested-theme stack and all push band-aids`.

---

## Phase 5 — Popup/Flyout logical-parent inheritance + flyout `ActualTheme` forwarding (D5, D6)

**Goal.** Popup/flyout content is correctly themed on the **first** open from its placement target's effective theme, on all platforms. Fixes the S4 styling defect.

**Depends on.** Phases 1–4. This phase is the enhanced-lifecycle (Skia/WASM) popup/flyout theming fix. Native is out of scope for element-level theming (OS + application theme only); Phase 7 confirms/documents the native scope rather than extending element theming to native.

**WinUI references.** Logical-parent inheritance at Enter `depends.cpp:1023-1048` + `GetInheritanceParentInternal(fLogicalParent)` `framework.cpp:3097-3130`; popup theme handling in the top-level notify `xcpcore.cpp:7826-7831`; `FlyoutBase` forwards target theme — Uno port at `FlyoutBase.cs:744-804`.

**Files.**
- `src/Uno.UI/UI/Xaml/Controls/Popup/Popup.cs:159-180`, `Popup.WithPopupRoot.cs:109-119`, `PopupRoot.cs:91-214`.
- `src/Uno.UI/UI/Xaml/Controls/Flyout/FlyoutBase.cs:387-403, 542-549, 744-804` (`ForwardThemeToPresenter`, `OnPlacementTargetActualThemeChanged`).
- `src/Uno.UI/UI/Xaml/Controls/CommandBarFlyout/TextCommandBarFlyout.mux.cs`, `CommandBarFlyout.cs`.

**Steps.**
1. Ensure the Phase 2 `Enter` theme step uses the **logical** inheritance parent for popup/flyout content so it inherits the opener's theme at first attach. Verify the `Popup.Child` → logical parent linkage exists on all platforms (it is used by the enhanced path today; generalize it).
2. Remove the now-redundant explicit popup-open theme push (`Popup.WithPopupRoot.cs`) — it is subsumed by the `EstablishThemeAtEnter` logical-parent inheritance (step 1) on enhanced-lifecycle. **Keep popup theme propagation gated to `UNO_HAS_ENHANCED_LIFECYCLE`:** element-level theming is a Skia/WASM feature and is **not** brought to native — native targets support OS + application theme only (see Phase 7). Native theming behavior is left unchanged by this phase.
3. **D6 fix in `FlyoutBase.ForwardThemeToPresenter` (enhanced-lifecycle only):** today it walks up looking for the nearest **non-Default `RequestedTheme`** and forwards only that. On Skia/WASM, change it to forward the placement target's **`ActualTheme`** (the effective theme, which includes app/inherited theme), so a flyout opened over content that is themed only at the app level is still themed. Keep the existing `ActualThemeChanged` subscription so runtime target theme changes still propagate. Preserve `m_isFlyoutPresenterRequestedThemeOverridden` semantics (don't override an explicitly-set presenter theme). **Gate the change to `UNO_HAS_ENHANCED_LIFECYCLE`: on native, keep the legacy `RequestedTheme` walk unchanged** (native = OS + application theme; element-level theme out of scope).
4. Confirm `TextCommandBarFlyout`/`CommandBarFlyout` presenters inherit via `FlyoutBase` and need no extra work beyond step 3.

**Acceptance criteria.**
- Builds all heads.
- T4 (`TextCommandBarFlyout` first-open) and T5 (`Popup` first-open) go green on Skia + WASM. They are **excluded on native** (`[PlatformCondition(Exclude, NativeAndroid|NativeIOS)]`) — element-level theming is not a native capability (native = OS + app theme).
- Existing popup/flyout theme tests in `Given_ElementTheme` ("Popup and Flyout Theme Propagation" region) stay green.
- Native theming behavior is unchanged (the D5/D6 changes are gated to `UNO_HAS_ENHANCED_LIFECYCLE`).

**Validation.** Theming suite + materialization tests (Skia + WASM); manual flyout-over-app-themed-content check.

**Commit.** `fix(theming): theme popup/flyout content from placement target ActualTheme on first open (D5/D6)`.

---

## Phase 6 — Application/OS/custom-theme/high-contrast precedence (D7, D8)

**Goal.** Match WinUI's app-vs-OS precedence exactly, define and fix custom-theme composition, and align high-contrast bit composition. Closes the remaining "controls use OS/browser theme" narrative.

**Depends on.** Phases 1–5.

**WinUI references.** `FrameworkTheming::GetTheme` `FrameworkTheming.cpp:119-136`; `SetRequestedTheme`/`OnThemeChanged` `:31-105`; `EnsureActiveThemeDictionary` HC + Default fallback `Resources.cpp:687-819`; `Theme.h:10-65` (bit composition); `FrameworkApplication.put_RequestedTheme` (settable only before App.xaml loaded) `FrameworkApplication_Partial.cpp:981-1037`; OS detection `SystemThemingInterop.cpp:33-177`.

**Files.**
- `src/Uno.UI/UI/Xaml/Application.cs:139-379, 492-600` (`RequestedTheme`, `InitializeSystemTheme`, `UpdateRequestedThemesForResources`, `IsThemeSetExplicitly`, `OnSystemThemeChanged`, `ActualElementTheme`, `OnResourcesChanged`).
- `src/Uno.UWP/Helpers/Theming/SystemThemeHelper.*` (OS theme + HC detection per platform).
- `src/Uno.UI/UI/Xaml/ResourceDictionary.cs:205-214, 590-616` (custom theme key, `GetActiveThemeDictionary` Default fallback).
- Uno `Theme` enum definition (confirm location; align to `base | highContrast` masks).

**Steps.**
1. **App vs OS precedence (D7 base).** Verify the chain matches `FrameworkTheming::GetTheme()`: when `IsThemeSetExplicitly` (app set `RequestedTheme`), OS changes must not flip the base theme (`OnSystemThemeChanged` guard at `Application.cs:372` is correct — add a regression test T9). When not explicit, follow OS. Confirm `ActualElementTheme` and `RequestedThemeForResources` reflect this.
2. **Ditch the custom-theme axis (D7) — RESOLVED, see [`custom-theme.md`](./custom-theme.md) → Option B.**
   - Remove the custom-name branch in `Application.UpdateRequestedThemesForResources` (`Application.cs:205-214`); `RequestedThemeForResources`/`Themes.Active` becomes strictly `"Light"`/`"Dark"` (+ HC).
   - Hard-deprecate `ApplicationHelper.RequestedCustomTheme` to a no-op with an `[Obsolete]` message (keep the property so existing code compiles). Confirm `"Light"`/`"Dark"` custom names still resolve as standard themes (the harmless-bucket case).
   - Document the breaking change: a genuine custom palette must move to a merged dictionary that overrides specific brush/color keys on top of the Light/Dark theme dictionaries.
3. **Element theme is strictly Light/Dark.** With the custom axis gone, element-level `ElementTheme.Light/Dark` selects the standard `"Light"`/`"Dark"` dictionaries — this is just the base model (matches WinUI; nothing custom to compose).
4. **Theme-dictionary fallback robustness (D7).** A dictionary that does not define the active theme key falls back to the app's resolved **base** Light/Dark, **not** the raw `"Default"` sub-dictionary (`ResourceDictionary.cs:590-600`) — Fluent's `"Default"` is the Dark theme, so the old fallback silently yielded dark. (Rarely triggers once `Themes.Active` is always Light/Dark, but keep it as the safe default.)
5. **High contrast (D8).** Align Uno's `Theme` enum / resolution so the effective theme is `base | highContrast` and HC selects the HC sub-dictionaries first (`EnsureActiveThemeDictionary` HC branch `Resources.cpp:718-762`). Ensure HC is OR-ed in `ResolveOwnerTheme` and in app theme. Wire `SystemThemeHelper` HC detection to set the HC bits. (If HC is currently entirely unhandled, scope this to: enum composition + dictionary selection + detection; full HC brush parity can be a follow-up, noted as such.)
6. `FrameworkApplication`-parity: `RequestedTheme` settable only before app resources load — confirm Uno's "before init" guard (`Application.cs:143-149`) matches and document.

**Acceptance criteria.**
- Builds all heads.
- T9 (OS-following suppressed when app theme explicit) green.
- A custom-theme test: app `RequestedCustomTheme="MyLight"` + an element `RequestedTheme="Dark"` resolves the standard dark dictionary; a missing custom key does **not** silently resolve dark (per chosen rule).
- HC: at least dictionary-selection-level test green (HC sub-dictionary chosen when HC active).
- Full theming suite green.

**Validation.** Theming suite + new precedence/custom/HC tests; `/winui-runtime-tests` for app-vs-element precedence.

**Commit(s).** `fix(theming): align app/OS theme precedence with FrameworkTheming (D7)`; `feat(theming): define custom-theme + element-theme composition`; `feat(theming): compose high-contrast with base theme (D8)`.

---

## Phase 7 — Native (Android/iOS) theme scope: OS + application theme only

**Goal.** Confirm and document that native (non-enhanced-lifecycle Android/iOS) targets support **OS theme + application theme only**, and that **element-level theming is intentionally out of scope on native**. The per-object theme + `Enter` inheritance machinery built in Phases 1–5 is a Skia/WASM (`UNO_HAS_ENHANCED_LIFECYCLE`) feature and is **not** ported to the native view hierarchy. Native theming behavior is left **unchanged** by this refactor.

**Rationale (decision).** Native tree mechanics differ fundamentally (native view hierarchy, no uniform "DO entered live tree" hook), and porting per-DO theme + `Enter` inheritance there is high-risk for limited benefit. Native already follows OS + application theme; element-level `RequestedTheme` inheritance (the element-`RequestedTheme`-on-root variant of S4) is a Skia/WASM capability and is accepted as a native limitation. This supersedes the earlier "native parity" goal.

**Depends on.** Phases 1–6 green on Skia/WASM.

**Files.**
- `src/Uno.CrossTargetting.targets` (`UNO_HAS_ENHANCED_LIFECYCLE` definition — understand only; do not change).
- All `#if UNO_HAS_ENHANCED_LIFECYCLE` theme blocks touched in earlier phases (verify the native `#else`/absent path is unchanged).

**Steps.**
1. Audit every `#if UNO_HAS_ENHANCED_LIFECYCLE` theme gate introduced/touched in Phases 1–5 and confirm the native path is **unchanged** from before the refactor: no per-DO theme establishment at native attach, no `Enter` inheritance walk, no element-theme push, no new element-theme code compiled into native. `EstablishThemeAtEnter` and the popup logical-parent inheritance run only on enhanced-lifecycle.
2. Confirm native popup/flyout content follows the application/OS theme: `FlyoutBase.ForwardThemeToPresenter` keeps the legacy `RequestedTheme` walk on native (no `ActualTheme`-based per-object inheritance); `Popup` theme propagation stays enhanced-lifecycle-gated. The element-`RequestedTheme`-on-root S4 scenario is not supported on native by design.
3. Confirm element-theme materialization repros (T4/T5 and any others asserting element-level theme) are excluded on native (`[PlatformCondition(Exclude, NativeAndroid|NativeIOS)]`).
4. Note: native controls (native text-selection menus, native inputs) may render via OS chrome and honor OS appearance independently of Uno — they already follow OS/app theme. Document the native theming scope (OS + app theme; no element-level theme) so future work does not re-attempt element theming on native without an explicit decision.

**Acceptance criteria.**
- Builds and runs on Android + iOS with native theming behavior **unchanged** (OS + application theme switching works as before).
- No element-level theme machinery compiled into native; no native theming regressions.
- Element-theme materialization tests (T4/T5, etc.) excluded on native; existing native-applicable theming tests green.

**Validation.** Build Android + iOS; confirm OS/application theme switching; confirm no native theming regressions vs. pre-refactor.

**Commit.** `docs(theming): scope native to OS + application theme (element theme is Skia/WASM only)`.

---

## Phase 8 — Full validation, WinUI parity, cleanup, docs

**Goal.** Prove the whole thing, on all platforms, against native WinUI; remove dead code; document.

**Depends on.** Phases 0–7.

**Steps.**
1. **WinUI oracle re-confirm:** run the entire theming suite (old + new, all WinUI-runnable tests) on `/winui-runtime-tests` and confirm **all green** on native WinUI. This re-establishes the suite as a correct oracle after any test edits made across the phases.
2. **Uno match:** run the **entire** theming suite + `Given_Theme_Materialization` via `/runtime-tests` on Skia Desktop and WASM (the platforms with element-level theming). All green, matching the WinUI results. On Android + iOS, run the native-applicable subset (element-level theme tests are excluded on native via `[PlatformCondition(Exclude, NativeAndroid|NativeIOS)]`) and confirm OS + application theme switching works with no native regressions. Compare to Phase 0 baseline; explain any test that legitimately changed (e.g. a test that asserted the old buggy unload-clear behavior) — such a change must itself be a WinUI-green assertion.
3. Remove any remaining transitional shims (parameterless `TryGetValue` wrappers added in Phase 3 if no longer needed, dead `#if` branches, `Control.crossruntime.cs:32-46` commented-out WinUI port if now superseded).
4. Performance sanity: run the resource-dictionary benchmarks (`src/SamplesApp/Benchmarks.Shared/.../ResourceDictionaryBench/*`) before/after to confirm no regression (expect neutral-to-better — fewer pushes).
5. Update developer docs: a short "Theming model" note describing the WinUI-aligned invariant (resolution = f(key, owner theme); theme established at Enter; logical-parent inheritance for popups), so future control authors don't reintroduce band-aids.
6. Update memory/spec status doc with final results.

**Acceptance criteria.**
- Skia + WASM: all theming tests green. Android + iOS: native-applicable subset green (element-theme tests excluded), OS + application theme switching works, no native regressions.
- WinUI parity confirmed for ported scenarios (Skia/WASM); native scope (OS + app theme only) documented.
- No `PushRequestedThemeForSubTree` / global theme stack anywhere in the codebase.
- Benchmarks neutral or improved.

**Validation.** Full suite all heads; WinUI parity run; benchmark comparison.

**Commit(s).** `test(theming): full cross-platform + WinUI parity validation`; `chore(theming): remove transitional shims and document theming model`.

---

## Scenario → phase traceability

(Scenarios S1–S5 are defined in `architecture.md` §1; they describe internally-tracked bugs generically. uno #23177 is public.)

| Scenario | Fixed primarily by | Verified by |
|----------|--------------------|-------------|
| S2 (recycle on tab navigation) | Phase 2 (D2 Enter) + D4 (no unload clear) + Phase 3 (D3) | T2 |
| S3 (scrolled-in cells) | Phase 2 (D2) + Phase 3 (D3) | T3 |
| S1 / S5 (virtualized item / runtime-added control) | Phase 2 (D2) + Phase 3 (D3) + Phase 6 (D7 OS/custom) | T1, T6 |
| S4 (popup/flyout first open) | Phase 5 (D5/D6) on Skia/WASM; native = OS/app theme only (Phase 7 — element theme out of scope on native) | T4, T5 (Skia/WASM; excluded on native) |
| uno #23177 (app dark switch) | preserved by Phases 2–4; hardened | existing repros + T7 |
| OS/theme leak narrative | Phase 6 (D7) | T9 + custom-theme test |
