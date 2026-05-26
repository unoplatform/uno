# Theming WinUI Alignment — Progress

Tracker for the WinUI theming-alignment refactor (branch `dev/mazi/theming-winui`).
Spec source of truth: `specs/theming-winui-alignment/{README,architecture,plan,tests,custom-theme}.md`.
One phase per session, strictly in order. Scenario labels S1–S5 are defined in `architecture.md` §1
(generic, NDA-safe). Do **not** record private tracker IDs here.

## Phases

- [ ] **Phase 0** — Baseline + regression test scaffold
- [x] **Phase 1** — Per-object theme on every `DependencyObject` (D1)
- [x] **Phase 2** — Establish theme at tree `Enter` for every DO (D2) + stop clearing on unload (D4)
- [x] **Phase 3** — Resolve `{ThemeResource}` against the owner's effective theme (D3, Mechanism 1)
- [x] **Phase 4** — Remove the 11 band-aid pushes + delete the global stack
- [x] **Phase 5** — Popup/Flyout logical-parent inheritance + flyout `ActualTheme` forwarding (D5, D6)
- [x] **Phase 6** — Application/OS/custom-theme/high-contrast precedence (D7, D8)
- [x] **Phase 7** — Native (Android/iOS) theme scope: OS + application theme only (element theme out of scope)
- [ ] **Phase 8** — Full validation, WinUI parity, cleanup, docs

## Evidence log

(one line per completed phase: what built; which tests ran on /winui-runtime-tests and /runtime-tests and their results)

- **Phase 1 (D1) — DONE.** Moved per-object `_theme` + `GetTheme`/`SetTheme` + `IsProcessingThemeWalk`
  from `UIElement` onto `DependencyObjectStore` (UIElement/FrameworkElement kept as thin forwarders);
  added `ThemeResolution.ResolveOwnerTheme(owner)` (own theme → nearest themed inheritance ancestor →
  app base theme). Behavior-preserving — **not** wired into `{ThemeResource}` resolution yet (that is
  Phase 3, Mechanism 1). Built `SamplesApp.Skia.Generic` (Release, net10.0, `UnoFastDevBuild`) clean
  (0 errors). `/runtime-tests` theming suite (Skia Desktop): **143/144** = Phase 0 baseline (the lone
  failure `When_Flyout_Closed_Target_Does_Not_Hold_Flyout` is the pre-existing GC flake). New
  `Given_ThemeResolution` unit tests: **7/7** green (incl. the non-`UIElement` owner case D1 enables).
  Not run this session: WASM/native heads; `/winui-runtime-tests` oracle (deferred, per carry-over notes).

- **Phase 2 (D2 + D4) — DONE.** **D2:** ported the `EnterImpl` theme block (`depends.cpp:1023-1048`) as
  `DependencyObjectStore.EstablishThemeAtEnter()` — every DO going live inherits its theme from its
  (logical) inheritance parent before any `{ThemeResource}` resolves (FE → `NotifyThemeChanged`, which
  re-applies the element's own `RequestedTheme` override; other DOs → `SetTheme` + `UpdateAllThemeReferences`).
  Hosted on the per-DO store (the `CDependencyObject` analog, carries `_theme` since D1) and invoked from
  the enhanced-lifecycle Enter walk (`UIElement.mux.cs` `DependencyObject_EnterImpl`, gated by
  `UNO_HAS_ENHANCED_LIFECYCLE`; native attach = Phase 7). The Enter walk runs synchronously on attach,
  before the first measure pass raises Loading, so `GetTheme()` is established before the deferred
  `{ThemeResource}` refs (created by `ApplyResource` at parse) first resolve at load. Removed the duplicate
  inherit / explicit-`RequestedTheme` block from `FrameworkElement.OnLoadingPartial` (the Enter step
  subsumes it); the band-aid theme push at load is kept (removed Phase 4). **D4:** removed the
  `SetTheme(Theme.None)` reset in `ClearThemeStateOnUnloaded` (WinUI keeps `m_theme` across Leave; re-Enter
  re-themes) — kept the separate `_themeForeground` stale-foreground hygiene clear (re-pulled on re-Enter).
  Built `SamplesApp.Skia.Generic` (Release, net10.0, `UnoFastDevBuild`) clean (0 errors) after each commit.
  `/runtime-tests` theming suite (Skia Desktop): **143/144** = Phase 0/1 baseline (lone failure is the
  pre-existing GC flake `When_Flyout_Closed_Target_Does_Not_Hold_Flyout`); the recycle test (S2/T2),
  `When_Detached_From_Window_While_Theme_Changed`, and the `Given_ElementTheme` entry/exit + dynamic-child
  invariant tests are all green — no behavior change, as expected (resolution leaf untouched until Phase 3).
  WASM-head build skipped per maintainer direction: it links the same `Uno.UI.Skia` assembly already
  validated on Skia and the changed code is platform-agnostic shared code. WASM/native runtime +
  `/winui-runtime-tests` oracle deferred. Two commits: D2 (`feat`), D4 (`fix`).

- **Phase 3 (D3, Mechanism 1) — DONE.** Made the `{ThemeResource}` resolution leaf a pure function of
  (key, owner's effective theme), replacing the process-global active-theme stack at the leaf. Threaded a
  theme parameter through `ResourceDictionary.TryGetValue` (value + providing-dict) and `GetFromMerged`
  (the parameterless overloads forward `GetActiveTheme()` so unmigrated callers are unchanged; added
  `GetThemeKey`/`GetActiveThemeValue`), `ThemeResourceReference.RefreshValue(Theme ownerTheme, …)` (+
  `RefreshValueWithTreeWalk`; the owner-based overload is kept as a transitional wrapper for
  `ApplyThemeResource`), and `ThemeWalkResourceCache`. The single centralized choke point is
  `DependencyObjectStore.UpdateThemeReference`, which computes `ThemeResolution.ResolveOwnerTheme(owner)`
  **once** and passes it to Phase A (ancestor walk) + Phase B (pinned-dict refresh) — the analog of WinUI
  `CDependencyObject::SetThemeResourceBinding` pushing `this->m_theme` (Theming.cpp:368-376), but threaded
  as a parameter (no process-global mutable state). `FrameworkElement.NotifyThemeChangedCore` now persists
  `SetTheme(theme)` **before** `UpdateThemeBindings` so the in-walk resolution keys on the new theme.
  **ThemeWalkResourceCache aligned with WinUI** (per maintainer request): the cache key is now the `Theme`
  enum **base value** — matching `tuple<CResourceDictionary*, Theming::Theme, xstring_ptr, weakref>`
  (ThemeWalkResourceCache.h:55) and the `GetBaseValue` stored by `SetRequestedThemeForSubTree`
  (xcpcore.cpp:7903) — threaded as a parameter instead of read from the global `GetActiveTheme()`; the
  value remains a `ManagedWeakReference` (== `xref::weakref_ptr`). Static-parse-path audit (step 6):
  `{ThemeResource}` on a DP emits the dynamic `ApplyResource(isThemeResourceExtension: true)` path →
  owner-theme-aware via the choke point; only non-DP members use the theme-agnostic static path
  (pre-existing limitation, theme tracking not feasible there) — no generator change needed. The
  `_requestedThemeForSubTree` stack + the 11 band-aid pushes remain (now no-ops for theme selection;
  removed in Phase 4).
  Built `SamplesApp.Skia.Generic` (Release, net10.0, `UnoFastDevBuild`) clean (0 errors). `/runtime-tests`
  theming suite + `Given_Theme_Materialization` (Skia Desktop): **143/144** = Phase 0/1/2 baseline (lone
  failure is the pre-existing GC flake `When_Flyout_Closed_Target_Does_Not_Hold_Flyout`); T1/T2/T3/T6/T7 +
  the element-theme / code-behind-style / uno #23177 repros remain green. `dotnet format whitespace
  --verify-no-changes` clean on the 6 changed files.
  **Equivalence gate (architecture.md §6).** A transitional divergence counter
  (`ThemeResolution.RecordOwnerThemeDivergence`, removed in Phase 4) compares the owner-theme key vs
  `GetActiveTheme()` at the choke point. It is **non-silent: 38 distinct signatures**, all root-caused
  (Root-Cause First, via stack capture) to transitional coexistence of the new parameter with the
  still-present band-aids, in two paths:
  - **Path 1 (majority) — `EstablishThemeAtEnter` enter-time resolution.** The owner already has its
    established island theme, but no band-aid pushes at Enter, so the legacy ambient is the app theme. The
    new leaf resolves the owner's theme — **this is the D3 fix**, and it matches WinUI (`EnterImpl` →
    `UpdateAllThemeReferences` → `SetThemeResourceBinding` pushes `m_theme`, Theming.cpp:368). The old leaf
    leaked the ambient and was corrected later by the `OnLoadingPartial` push.
  - **Path 2 (few) — template build (`LoadContent`) + `ApplyStyleWithThemeContext`.** A not-yet-entered
    template part (theme `None`) makes `ResolveOwnerTheme` fall back to `Themes.Active` while band-aid #5
    has pushed the templated-parent theme. Transient; corrected when the part enters the tree.
  In both paths the value is corrected before it is observed, so **final behavior is identical to baseline**
  (143/144, incl. the WinUI-oracle materialization tests that assert exact Light sentinels on island
  template parts). No category-(b) plumbing error produces a wrong **final** value. Per the maintainer's
  decision, the divergences are accepted as category-(a) intended/transitional WinUI-correct changes (the
  prompt's "document any intentional divergence" escape hatch); the literal "zero" is not achievable while
  Phase 2's enter-time resolution and the band-aids coexist, and the divergences resolve once Phase 4
  removes the band-aids/stack. WASM/native heads + `/winui-runtime-tests` oracle deferred (per carry-over
  notes; WASM links the same `Uno.UI.Skia` assembly already validated). One code commit
  (`fix(theming): resolve ThemeResources against owner's effective theme (D3)`).

- **Phase 4 — DONE.** Removed all 11 band-aid pushes and deleted the process-global requested-theme stack;
  resolution is now purely a function of (key, owner's effective theme) threaded as a parameter.
  - **Real-work re-points (push → owner-theme parameter):** #2 foreground freeze (`DefaultTextForegroundThemeBrush`
    via a theme-aware `ResourceResolver.ResolveTopLevelResource`), #9 Hyperlink pointer-state brushes (theme-aware
    `Application.Resources.TryGetValue`), #10 keyframe theme refs (pass the target element as resolution owner),
    #11 focus-visual default (`ResolveFocusVisualBrushDefault` → `GetThemeKey(ResolveOwnerTheme(target))`), and
    `ResourceResolver.ApplyThemeResource`/`ApplyVisualStateSetter` (resolve against the setter target's theme).
  - **Pure push removals:** #1 `NotifyThemeChangedCore`, #3 `OnLoadingPartial`, #4 `ApplyStyleWithThemeContext`,
    #5 `FrameworkTemplate.LoadContent`, #6/#7 `VisualStateGroup`, #8 `BindingHelper.UpdateResourceBindings`.
  - **Deleted:** `ResourceDictionary.Push/PopRequestedThemeForSubTree(+ByName)`, the `Themes._requestedThemeForSubTree`
    stack + `RequestedThemeForSubTree` property (so `GetActiveTheme()` collapses to `Themes.Active`, the app
    fallback), `GetActiveThemeValue()` + the transitional `RefreshValue(owner)` wrapper, and the Phase 3 divergence
    counter (`ThemeResolution.RecordOwnerThemeDivergence` + `OwnerThemeDivergenceCount`).
  - **Key deep fix (WinUI alignment, no shortcut):** removing the global push exposed that Mechanism 1 must
    thread the owner theme through the WHOLE resolution chain, not just the top-level lookup — including
    assembly/system retrieval (`MasterDictionary`), the `RefreshValueCore` top-level fallback, and crucially
    **`StaticResource` alias resolution** (`TryResolveAlias` → `ResolveResourceStatic`). Fluent v2 defines focus
    and default-text brushes as aliases inside theme sub-dictionaries (e.g. `SystemControlFocusVisualPrimaryBrush
    → FocusStrokeColorOuterBrush`); without theme-threading the alias, selecting the right sub-dictionary still
    resolved the target against the app ambient. Made `TryResolveAlias`/`TryVisualTreeRetrieval`/`TryTopLevelRetrieval`/
    `TryStaticRetrieval`/`TrySystemResourceRetrieval`/`TryAssemblyResourceRetrieval` theme-aware, matching WinUI's
    `core->LookupThemeResource(theme, key)` which sets the theme context for the entire chain
    (`CDependencyProperty::GetDefaultFocusVisualBrush`, DependencyProperty.cpp:309-353; xcpcore.cpp LookupThemeResource).
  - **Two regressions root-caused (Root-Cause First, never re-added a push):** (a) the visual-state setter path
    (`ApplyVisualStateSetter`) resolved the app theme — fixed by threading the setter target's theme into
    `TryVisualTreeRetrieval`; (b) the manual-`LoadContent`-without-attach repro asserted the band-aid's build-time
    resolution, which WinUI doesn't do (it establishes template-content theme at `Enter` from the logical/templated
    parent) — updated the test to attach the realized content as a virtualizing panel does. The passing sibling
    test `When_ContentTemplate_Materialized_In_Themed_Subtree_Uses_Subtree_Theme` already covers the WinUI-faithful
    attach path.
  - **§B leak guard added:** `Given_Theme_Materialization.When_Phase4_Global_Theme_Stack_Removed_Guard` — a reflection
    guard that fails if `PushRequestedThemeForSubTree` or the `_requestedThemeForSubTree` stack is reintroduced.
  - **Grep-clean:** no live Uno band-aid API anywhere in `src/`. The only remaining `RequestedThemeForSubTree`
    substring matches are WinUI C++ citations / a reference comment (Control.crossruntime.cs `CControl::EnterImpl`
    reference, ThemeWalkResourceCache citations) that document WinUI's own ambient slot — kept for port fidelity;
    the reflection guard (not grep) is the authoritative protection against reintroducing the Uno API.
  - Built `SamplesApp.Skia.Generic` (Release, net10.0, `UnoFastDevBuild`) clean (0 errors). `/runtime-tests` theming
    suite + `Given_Theme_Materialization` (Skia Desktop): **144/145** (lone failure = the pre-existing GC flake
    `When_Flyout_Closed_Target_Does_Not_Hold_Flyout`, fails on baseline too). T1/T2/T3/T6/T7 + the element-theme /
    code-behind-style / uno #23177 / focus-visual / visual-state repros all green with ZERO band-aids and no global
    stack; §B guard green; zero `[THEME-P3-DIVERGENCE]` lines (counter gone). `dotnet format whitespace
    --verify-no-changes` clean on all changed files. WASM/native heads + `/winui-runtime-tests` oracle deferred
    (per carry-over notes).

- **Phase 5 (D5 + D6) — DONE.** Popup/flyout content is themed on the FIRST open from its opener's effective
  theme, established at tree Enter rather than via an explicit post-open push.
  - **D5 — logical-parent inheritance at Enter.** `DependencyObjectStore.EstablishThemeAtEnter` now inherits a
    `FrameworkElement`'s theme from its **logical** inheritance parent (`FrameworkElement.Parent` =
    `LogicalParentOverride ?? Store.Parent`), a faithful port of WinUI's `EnterImpl` theme block
    (depends.cpp:1026-1041, `GetInheritanceParentInternal(fLogicalParent=TRUE)` "so popups and flyouts inherit
    theme changes"); non-FE DOs keep using the visual/inheritance parent (`GetParentInternal(false)`). A `Popup`
    sets its `Child`'s `LogicalParentOverride` to itself (`Popup.Base.cs OnChildChangedPartial`), so the reparented
    content now follows the opener's theme instead of the `Theme.None` `PopupRoot`. Removed the now-redundant
    enhanced-lifecycle popup-open theme push in `Popup.WithPopupRoot.cs` (subsumed by `EstablishThemeAtEnter`'s
    logical-parent inheritance). `EstablishThemeAtEnter` and `Popup.NotifyThemeChangedCore` stay gated to
    `UNO_HAS_ENHANCED_LIFECYCLE`: **element-level theming is a Skia/WASM feature; native targets support OS +
    application theme only** (per the maintainer decision — see Phase 7), so native theming behavior is
    unchanged by this phase.
  - **D6 — flyout forwards the placement target's effective `ActualTheme`.** `FlyoutBase.ForwardThemeToPresenter`
    now forwards `target.ActualTheme` instead of walking up for the nearest explicit non-Default `RequestedTheme`.
    WinUI's `ForwardThemeToPresenter` forwards only the explicit element override and lets the app/inherited theme
    reach the presenter via logical-parent inheritance + `ActualTheme`'s app/OS-base fallback
    (framework.cpp:3984-3991); forwarding the *effective* `ActualTheme` is the equivalent net result and
    additionally themes a flyout opened over **app-themed** (not element-themed) content on the first open. It
    composes with the per-object Enter theme (D2). This is **gated to `UNO_HAS_ENHANCED_LIFECYCLE`**:
    `ForwardThemeToPresenter` is a **no-op on native** (no element-level theme is forwarded — the presenter keeps
    its default/inherited theme and the flyout follows the application/OS theme; native = OS + application theme
    only, see Phase 7).
    `m_isFlyoutPresenterRequestedThemeOverridden` semantics and the placement-target `ActualThemeChanged`
    subscription are preserved. `TextCommandBarFlyout`/`CommandBarFlyout` inherit `ForwardThemeToPresenter` via
    `FlyoutBase` and need no extra work (the element-`RequestedTheme`-on-root variant of the S4 mobile
    text-selection context menu is a native limitation, accepted as out of scope).
  - Built `SamplesApp.Skia.Generic` (Release, net10.0, `UnoFastDevBuild`) clean (0 errors). `/runtime-tests`
    theming suite + `Given_Theme_Materialization` (Skia Desktop): **144/145** = Phase 4 baseline (lone failure =
    the pre-existing GC flake `When_Flyout_Closed_Target_Does_Not_Hold_Flyout`, fails on baseline too — confirmed
    its message is the `'collected'` GC assertion, unrelated to theme resolution). **T4** (`Flyout` first open from
    a Light region) and **T5** (`Popup` first open in a Light region) green on Skia, identical first/second open;
    the §B leak guard green; the existing "Popup and Flyout Theme Propagation" tests in `Given_ElementTheme`
    (`Popup_Child_Inherits`, `Popup_Owner_Theme_Changes`, `Flyout_Opened_From_Dark`, `Flyout_Target_Theme_Changes`,
    `Nested_Theme_Boundary_Flyout`, `Flyout_PlacementTarget_Theme_Changes`, `MenuFlyout_Opened_After`) all green.
    `dotnet format whitespace --verify-no-changes` clean. WASM deferred (links the same `Uno.UI.Skia` assembly);
    `/winui-runtime-tests` oracle deferred.
  - **Native scope (maintainer decision):** native targets support **OS + application theme only, not
    element-level theme**. All element-level theming is gated to `UNO_HAS_ENHANCED_LIFECYCLE`, and on native
    `FlyoutBase.ForwardThemeToPresenter` is a **no-op** — the legacy element-`RequestedTheme` walk was removed so
    a native flyout follows purely the application/OS theme (the presenter keeps its default/inherited theme and
    resolves the app theme via the normal path). T4/T5 are excluded on native
    (`[PlatformCondition(Exclude, NativeAndroid|NativeIOS)]`). Phase 7 was redefined accordingly (confirm/document
    the native OS+app scope rather than bring element theming to native); `architecture.md`/`plan.md`/`tests.md`/
    `README.md` updated to match. Commits: `fix(theming): theme popup/flyout content from placement target
    ActualTheme on first open (D5/D6)`; `refactor(theming): keep element-level theming Skia/WASM-only; native is
    OS + app theme` (gating + T4/T5 native exclusion); `refactor(theming): native flyout follows app/OS theme
    (no element-theme forwarding)`; plus docs commits for the spec/native-scope adjustments.

- **Phase 6 (D7 + D8) — DONE.** Application/OS theme precedence, the custom-theme ditch, and high-contrast
  composition. All changes are app/OS-level; no element-level/per-object theme code was added to the native
  path (Phase 5 native-scope decision held — the resolution leaf is platform-neutral).
  - **D7 app-vs-OS precedence — verified already correct (no code change).** Uno's chain already matches
    `FrameworkTheming::GetTheme()` (FrameworkTheming.cpp:119-136): `SetExplicitRequestedTheme` sets
    `IsThemeSetExplicitly`, and `Application.OnSystemThemeChanged` guards `if (!IsThemeSetExplicitly)` so an OS
    theme change does not flip an explicitly-set app theme. T9 (`When_App_Theme_Explicit_OS_Change_Is_Suppressed`)
    was already a GREEN regression guard at the Phase 6 baseline — kept, not newly fixed.
    `FrameworkApplication.put_RequestedTheme` "settable only before resources load" parity confirmed: Uno's
    `Application.RequestedTheme` setter throws on `_initializationComplete`, matching `m_isRequestedThemeSettable`
    (FrameworkApplication_Partial.cpp:986-993).
  - **D7 custom-theme ditch (custom-theme.md → Option B).** Removed the custom-name arm in
    `Application.UpdateRequestedThemesForResources`, so `RequestedThemeForResources`/`Themes.Active` is strictly
    `"Light"`/`"Dark"`. Hard-deprecated `ApplicationHelper.RequestedCustomTheme` to an `[Obsolete]` no-op (the
    property is kept and round-trips for source compatibility, but it no longer keys a custom `ThemeDictionaries`
    entry nor sets the app theme). Note: after Phases 3/4 the in-tree resolution leaf already threaded the
    owner's Light/Dark theme via `GetThemeKey`, so a custom `Themes.Active` only affected owner-less lookups /
    `GetActiveTheme()`; the ditch formalizes "no custom axis" end-to-end (WinUI has none). **Breaking change** for
    apps passing a non-`Light`/`Dark` custom name (migrate to merged dictionaries that override specific
    brush/color keys on top of the Light/Dark theme dictionaries; use `Application.RequestedTheme` to switch the
    standard themes); `"Light"`/`"Dark"` names are the harmless bucket and still resolve as the standard themes.
  - **D7 theme-dictionary fallback — already WinUI-faithful once `Themes.Active` is Light/Dark.** With the custom
    axis gone, the resolved key is always Light/Dark, so `GetThemeDictionary(activeTheme) ??
    GetThemeDictionary(Themes.Default)` matches `EnsureActiveThemeDictionary`'s "resolved base → Default" chain
    (Resources.cpp:764-790). The dark-`Default` leak required a custom/stale key skipping Light/Dark — removed by
    the ditch (kept `?? Default` as the WinUI-faithful final fallback rather than diverging to an "app base"
    step). T10 part (b) (`When_Element_Dark_Island_And_Fallback_Does_Not_Leak_Dark`) was already GREEN at
    baseline; kept.
  - **D8 high-contrast composition.** HC is an OS/app-global dimension OR-ed onto the base theme — matching WinUI
    (`FrameworkTheming::GetTheme` reads HC from FrameworkTheming, not the per-object theme, FrameworkTheming.cpp:123;
    `EnsureActiveThemeDictionary` reads `GetHighContrastTheme()` globally, Resources.cpp:718,740). Added
    `SystemThemeHelper.IsHighContrast`, sourced from the settable `WinRTFeatureConfiguration.Accessibility.HighContrast`
    (the existing accessibility setting — it doubles as the deterministic runtime-test override).
    `ThemeResolution.ResolveOwnerTheme` now ORs the app HC bits onto the resolved base theme.
    `ResourceDictionary.GetActiveThemeDictionary` reads the global HC state live and selects the HC sub-dictionary
    first (owner base → `HighContrastWhite`/`HighContrastBlack`, then the generic `"HighContrast"` key), falling
    through to the base Light/Dark, then `"Default"` — a faithful port of `EnsureActiveThemeDictionary`
    (Resources.cpp:718-790); the `_activeThemeDictionary` cache invalidates on HC change. `GetThemeKey` stays
    base-only (the threaded key is the base theme; HC is composed at the leaf). **Scope (per plan):** detection +
    enum/app-theme composition + dictionary selection. Full HC brush parity (real per-platform White/Black/Custom
    OS-variant detection, the HC brush set, and runtime HC-change propagation to already-shown elements) is a
    documented follow-up.
  - **Tests.** New Uno-only (`#if HAS_UNO`, Skia/WASM, `[RequiresFullWindow]`, native-excluded) runtime tests in
    `Given_Theme_Materialization`: `When_Custom_Theme_Name_Is_Ditched_Resolves_Standard` (a non-Light/Dark custom
    name no longer becomes `GetActiveTheme()`; an element `RequestedTheme="Dark"` still resolves standard Dark; a
    `"Light"` name resolves standard Light) — RED on pre-ditch code, GREEN after — and
    `When_HighContrast_Active_Selects_HighContrast_Dictionary` (HC active → the `HighContrast` sub-dictionary is
    chosen ahead of Light/Dark). Repurposed the Uno.UI.Tests unit test `Given_ResourceDictionary.When_Has_Custom_Theme`
    → `When_Custom_Theme_Name_Is_Ditched` (asserts a custom name no longer keys its sub-dictionary) and removed the
    now-defunct `RequestedCustomTheme` reset in the test `App`.
  - **Validation.** *Compile:* `SamplesApp.Skia.Generic` (Release, net10.0, `UnoFastDevBuild`) clean — 0 errors, 4
    pre-existing binding-redirect warnings; `Uno.UI.Tests` (net10.0) builds clean (the `[Obsolete]` + `#pragma
    CS0618` test edits compile). *Runtime (Skia Desktop):* `/runtime-tests` theming suite +
    `Given_Theme_Materialization` = **146/147** — the lone failure is the pre-existing GC flake
    `When_Flyout_Closed_Target_Does_Not_Hold_Flyout` (the `'collected'` assertion; fails on baseline too). That
    equals the 144/145 Phase 5 baseline plus the two new GREEN Phase 6 tests; T9/T10 + the §B leak guard stay
    green. The repurposed Uno.UI.Tests unit test compiles but the `dotnet test` runner reports "No test projects
    were found" under the net10.0 override (a repo test-discovery quirk, `NetUnitTests = NetPrevious = net9.0`,
    unrelated to the change); its assertion mirrors the runtime-validated ditch behavior. `dotnet format whitespace
    --verify-no-changes` clean on all changed files. WASM/native heads + `/winui-runtime-tests` oracle deferred
    (T9/T10/ditch/HC are Uno-only — confirmed against the WinUI FrameworkTheming/Resources sources and a WinUI
    probe app per the test comments). Commits: `feat(theming): ditch app-level custom-theme axis (D7)`;
    `feat(theming): compose high-contrast with base theme (D8)`; `test(theming): cover custom-theme ditch and
    high-contrast selection`; plus this docs commit.

- **Phase 7 — DONE.** Confirmed and documented that native (Android/iOS, non-enhanced-lifecycle) targets
  support **OS + application theme only**; element-level theming stays a Skia/WASM (`UNO_HAS_ENHANCED_LIFECYCLE`)
  feature and is intentionally **not** ported to the native view hierarchy. No new native theme code; this is a
  verification + documentation phase.
  - **Gate audit (every `UNO_HAS_ENHANCED_LIFECYCLE` theme block from Phases 1–5).** The new element-theme
    *establishment* machinery is all enhanced-lifecycle-gated and never compiles/runs on native:
    `DependencyObject_EnterImpl` → `Store.EstablishThemeAtEnter()` is inside the `#if UNO_HAS_ENHANCED_LIFECYCLE`
    block (`UIElement.mux.cs:852-1991`), and `EstablishThemeAtEnter` has **exactly one caller** (that gated
    site); `ClearThemeStateOnUnloaded` (D4) is `#if UNO_HAS_ENHANCED_LIFECYCLE` (called only from the gated
    `OnUnloadedPartial`); `OnLoadingPartial`'s theme block is the `#if` arm, the native `#else`
    (`SyncRootRequestedTheme` + `ApplyStyles` + `UpdateResourceBindings`) is **unchanged**; `Popup`
    open-time push removed (was inside `#if`, native never had it) and `Popup.NotifyThemeChangedCore`
    override stays gated. `ApplyStyleWithThemeContext` collapsed both arms to plain `ApplyStyleCore`
    (native already did exactly that).
  - **Shared resolution leaf is platform-neutral, and on native it falls back to the app/OS theme.**
    `DependencyObjectStore.Theming.cs` (D1/D3), `ThemeResolution.cs`, `ResourceDictionary` theme-key threading,
    `ThemeResourceReference.RefreshValue(Theme)` compile on all heads (the leaf has no `#if`). On native,
    because `EstablishThemeAtEnter` never runs, `_theme` is only ever set by the `NotifyThemeChangedCore` walk
    (which is **not** newly gated — it ran on native before via push/pop and runs now via `SetTheme` + the
    owner-theme leaf). When no per-object theme is established, `ResolveOwnerTheme` walks to the app base theme
    (`Application.Current.ActualElementTheme`) — **net-identical to the old `GetActiveTheme()`/`Themes.Active`
    for the OS+app scenario** (the band-aid stack was always empty on native). So native theme resolution is
    behavior-preserved.
  - **One intentional native change (made in Phase 5, reconfirmed here): `FlyoutBase.ForwardThemeToPresenter`
    is a `#else` no-op on native** (the legacy element-`RequestedTheme` walk that *did* run on native was
    removed); the field `m_isFlyoutPresenterRequestedThemeOverridden` is now `#if`-gated too. A native
    flyout follows the application/OS theme (presenter keeps its default/inherited theme; normal
    `{ThemeResource}` resolution + `Popup.UpdateThemeBindings`). Documented at the site.
  - **Band-aid removals (Phase 4) are native-safe.** #6/#7 (VisualStateGroup) and #8 (BindingHelper) pushes
    were `#if`-gated → native never pushed → removal is native-neutral. #5 (FrameworkTemplate), #9 (Hyperlink),
    #10 (animation keyframes), #11 (focus visual) pushes were *not* gated, but were no-ops on native whenever
    no element theme is established (the OS+app norm), so their replacement by owner-theme threading preserves
    the native OS+app result. The deleted global-stack API has **zero** live references in `src` (only the §B
    reflection guard names it, to assert it stays gone).
  - **Tests native-excluded.** All 12 `Given_Theme_Materialization` methods (incl. T4/T5) carry
    `[PlatformCondition(Exclude, NativeAndroid | NativeIOS)]`; `Given_ElementTheme` keeps its **class-level**
    native exclusion. Element-level theme repros do not run on Android/iOS by design.
  - **Stale in-code comments corrected (so future work doesn't re-attempt native element theming):**
    `UIElement.mux.cs` (was "native attach is wired in Phase 7" → now states native is OS+app only by design);
    `Given_Theme_Materialization.cs` class comment (was "popup/flyout first-open tests run on iOS/Android" →
    now states each element-theme test is per-method native-excluded). Added a **"Platform support for
    element-level theming"** section to the public `doc/articles/features/working-with-themes.md` (app/OS theme
    universal; element-level `RequestedTheme` sub-tree theming on Windows/WASM/Skia; native Android/iOS = OS+app
    theme only).
  - **Validation.** *Code review:* gate audit above — native path unchanged except the intentional Phase 5
    FlyoutBase no-op. *Compile:* `Uno.UI.netcoremobile` built for **net10.0-android** (Release,
    `UnoFastDevBuild`) = **0 errors** (22 pre-existing `BG8C00` Android-binding warnings, unrelated) and
    **net10.0-ios** = **0 errors, 0 warnings**. So no element-theme machinery breaks native compilation and the
    deleted stack has no leftover native references. *Runtime:* on-device/emulator Android+iOS OS/app theme
    switching was **not** executed in this environment (no device/emulator available) — maintainers should run
    the SamplesApp on an Android device/emulator and an iOS simulator and toggle the OS theme to confirm OS+app
    switching is unchanged; the native-applicable theming tests (element-theme suites excluded) run via
    `/runtime-tests` on the native heads. Commit: `docs(theming): scope native to OS + application theme
    (element theme is Skia/WASM only)`.
  - **Known follow-up (not Phase 7):** `working-with-themes.md` still shows the now-`[Obsolete]` no-op
    `ApplicationHelper.RequestedCustomTheme` in the "Change the app theme at startup" example (Phase 6 ditched
    the custom-theme axis). Replacing it with `Application.RequestedTheme` has WinUI-ordering nuances — left for
    a Phase 6/8 docs follow-up.

---

## Phase 0 — working notes (repros + baseline + test-hardening DONE; WinUI-oracle pending)

> The "blocking decisions / 17-fail / repro-strategy" notes below this header are a **historical snapshot**
> from an earlier session and are superseded by the **RESOLUTION** section at the bottom of this file.

**Impl location:** branch `dev/mazi/theming-winui` is checked out at `D:\Work\uno` (the main worktree),
1 commit past `master` (spec docs only). Spec: `specs/theming-winui-alignment/`.

### Build / run gotchas (IMPORTANT — saves rediscovery)
- `UnoFastDevBuild` is **not wired on this branch** (no-op; only referenced in plan.md). It's a
  build-acceleration feature from other branches.
- `-c Release` enforces code-style: `Directory.Build.props:248` sets `EnforceCodeStyleInBuild=true`
  unconditionally, and `IDE0055` is excused only in **Debug** (`:254`). Pre-existing `IDE0055`
  violations in `src/Uno.UI/UI/Xaml/Thickness.cs:70-76` therefore FAIL the Release build.
- **Working Skia build:**
  `dotnet build src\SamplesApp\SamplesApp.Skia.Generic\SamplesApp.Skia.Generic.csproj -c Release -f net10.0 -p:UnoTargetFrameworkOverride=net10.0 -p:EnforceCodeStyleInBuild=false`
- Do **NOT** also pass `-p:RunAnalyzersDuringBuild=false` / `-p:EnableNETAnalyzers=false` — that
  breaks CsWin32 source-gen in `Uno.UI.Runtime.Skia.Win32` (`CS0234`). Leave generators on.
- **Run (Skia):** in ONE shell call (cwd + env reset between calls):
  set `$env:UITEST_RUNTIME_TESTS_FILTER = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes("<filter>"))`,
  then from `src\SamplesApp\SamplesApp.Skia.Generic\bin\Release\net10.0` run
  `dotnet SamplesApp.Skia.Generic.dll --runtime-tests=out.xml`.
- WASM head publishes OK (`SamplesApp.Skia.WebAssembly.Browser`, same `EnforceCodeStyleInBuild=false`);
  **not yet run**. Python + `build/test-scripts/skia-browserwasm-file-creation-server.py` present.

### Files added (this WIP commit)
- `src/Uno.UI.RuntimeTests/Tests/Windows_UI_Xaml/Given_Theme_Materialization.cs` — T1–T10 scaffold;
  compiles + runs; **all 10 PASS on Skia** (no class-level native exclusion so popup tests can run
  on native; per-method native excludes on the non-popup tests).
- `.gitignore` += `/specs/theming-winui-alignment/baseline-results.md`.

### Reproduce-first findings (the important part)
- **T1–T10 all GREEN on Skia.** The 11 band-aids (`UNO_HAS_ENHANCED_LIFECYCLE` = Skia/WASM) already
  cover the materialization paths I synthesized: ListView realization → `FrameworkTemplate.LoadContent`
  (band-aid #5), runtime-add of an FE → `OnLoadingPartial` (#3), Skia popups. Per `architecture.md`,
  S4 (popup/flyout first-open) is **native-only** (band-aids are not present on native) — so T4/T5
  would be RED on Android/iOS, not Skia.
- **Existing theming suite on Skia: 134 total, 117 pass, 17 FAIL.** The failures ARE the leak the
  refactor targets (Light context resolving the Dark value), concentrated in **default-foreground**
  and **dynamic-theme-change** paths — NOT the static materialization paths my T1–T10 chose.
- **OS = Dark confirmed on this machine** (`AppsUseLightTheme=0`, `SystemUsesLightTheme=0`). The 17
  failures are **environment-sensitive**: they do not pin the app theme, so on OS=Dark the app's
  ambient `Themes.Active` follows OS=Dark and leaks into Light contexts. On a Light-OS / CI box,
  several would likely pass.
- My new repros **masked** the bug by pinning the ambient (`UseApplicationLightTheme`) or by comparing
  two equally-leaked values (T8 reference also leaked).

### 17 failing existing tests (Skia, OS=Dark) — real-leak vs likely-flaky
Default-foreground / theme-leak (look deterministic — the target bug):
`When_DefaultForeground_Fluent` (exp #E4000000, got #FFFFFFFF), `When_DefaultForeground_Non_Fluent`,
`When_ParentThemeChanges_BasicThemeResources_FullRepro` (exp Yellow, got Blue),
`When_Grandparent_RequestedTheme_Changes_Button_Foreground_Updates`,
`When_Theme_Changes_Button_Normal_Foreground_Updates`, `When_Theme_Changes_TextBlock_Foreground_Updates`,
`When_XamlParsed_Buttons_Theme_Change_Foreground_Updates`, `When_Theme_Default_To_Dark_Button_PointerOver_Updates`,
`When_Root_Theme_Cycles_Dark_Default_TextBlock_Foreground_Restores`, `When_Root_Theme_Cycles_New_Content_Has_Correct_Foreground`,
`When_RootTheme_Cycles_Then_Element_RequestedTheme_Dark_Button_Foreground_Updates`,
`When_ComboBox_Theme_Changed_After_First_Open`, `When_Detached_From_Window_While_Theme_Changed`,
`When_Parent_Resource_Override_On_Loaded`, `When_Theme_Changed`, `When_ActualThemeChanged_Throws`.
Possibly flaky (GC/lifetime): `When_Flyout_Closed_Target_Does_Not_Hold_Flyout`.

## RESOLUTION (current state — supersedes the snapshot above)

### Delivered this session
1. **OS-theme simulation test API** — `SystemThemeHelper.SystemThemeOverride` (property, analogous to
   `ScaleOverride`) + `ThemeHelper.UseSystemThemeOverride(ApplicationTheme)` (disposable; throws on WinUI so
   callers add `[PlatformCondition(Exclude, NativeWinUI)]`). Lets runtime tests pin the ambient OS theme
   deterministically and simulate runtime OS-theme changes. Validated OS-independent.
2. **Repros rewritten to the correct polarity** — `Given_Theme_Materialization` (T1–T10): Light element-level
   island under a (simulated) Dark ambient, mapped to the real issues (S1–S5 + uno #23177). Finding: minimal
   repros are **band-aid-covered on Skia → green**; their role is WinUI-oracle + regression-guard + native-S4
   (T4/T5 expected RED on Android/iOS). The deterministic Skia evidence is the existing suite under Dark.
3. **Baseline** (gitignored `baseline-results.md`): existing theming suite **133/134 (Light) → 117/134 (Dark)**.
4. **Root-caused & fixed the OS-dark "unreliability"** — it was a shared-state bug in `ThemeHelper.UseDarkTheme()`
   (restored the shared `XamlRoot.Content` host from `Application.RequestedTheme`, leaving it Dark on a Dark OS
   → poisoned later tests). `UseDarkTheme()` now restores the host's **original** theme. **Not** the product
   ambient leak, and **not** test-hygiene. (Full analysis in `baseline-results.md` → RESOLUTION.)
5. **Test-hardening** (maintainer's choice = per-test app-Light): affected theme tests pin
   `ThemeHelper.UseApplicationLightTheme()` (`#if HAS_UNO`) + `[RequiresFullWindow]`. Exception:
   `When_Detached_From_Window_While_Theme_Changed` stays embedded (nulls `WindowContent` mid-test).
6. **Validated:** full theming suite + 10 repros = **143/144 on BOTH Light and Dark** (suite is now
   OS-independent). The single remaining failure, `When_Flyout_Closed_Target_Does_Not_Hold_Flyout`, is a
   pre-existing GC/lifetime flake that fails on Light too — unrelated to theming/OS, out of scope.

### Files changed (staged, not committed)
- `src/Uno.UWP/Helpers/Theming/SystemThemeHelper.cs` — `SystemThemeOverride`.
- `src/Uno.UI.RuntimeTests/Helpers/ThemeHelper.cs` — `UseSystemThemeOverride` + `UseDarkTheme` fix.
- `src/Uno.UI.RuntimeTests/Tests/Windows_UI_Xaml/Given_Theme_Materialization.cs` — rewritten repros.
- `Given_ElementTheme.cs`, `Given_ThemeResource.cs`, `Given_FrameworkElement_ThemeResources.cs` — hardening.

### Remaining for Phase 0
- **WinUI-oracle pass** (`/winui-runtime-tests`): confirm the WinUI-portable repros (T1–T6, T8) are GREEN on
  native WinUI; probe-app for the Uno-only ones (T7/T9/T10). This is the last Phase 0 acceptance gate.
- Then Phase 0 is fully ticked → proceed to Phase 1 (per-object theme on `DependencyObjectStore`, D1).
