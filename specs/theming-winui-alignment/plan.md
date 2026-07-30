# Theming WinUI Alignment — Exact-Port Implementation Plan

> Read [`architecture.md`](./architecture.md) first (the WinUI model + port-mapping table + design decisions). Tests live in [`tests.md`](./tests.md). Phases are **strictly ordered**; do not start phase N+1 until phase N's acceptance criteria pass.

## Global conventions

- **Branch:** `dev/mazi/align-theming`, based on `origin/dev/mazi/theming-winui` (`ffd6ee2631`). All commits land here, on top of that base. Commit continuously — one focused Conventional-Commit per chunk that builds clean, Co-Authored-By trailer. The base's own code is the *starting point being replaced* — consult it freely; but port from the **C++ sources**, not from the base's approximation.
- **Port fidelity:** the `/winui-port` skill rules + `architecture.md` §0 are binding. Every ported file: MUX/MIT header + `MUX Reference <file>, commit fc2f82117`; member order matches C++; C++ comments preserved; nothing silently dropped (`TODO Uno:` + reason); natural WinUI names; Allman + tabs; `#nullable enable` in new files.
- **WinUI sources:** `D:\Work\microsoft-ui-xaml2\src` @ `fc2f82117`. Always re-read the actual C++ before porting a method — do not port from this spec's summaries.
- **Build (Skia, fast iteration):**
  ```
  cd src
  dotnet build SamplesApp/SamplesApp.Skia.Generic/SamplesApp.Skia.Generic.csproj -c Release -f net10.0 -p:UnoFastDevBuild=true
  ```
  NEVER cancel builds; 15+ minute timeouts.
- **Runtime tests:** the `/runtime-tests` skill (Skia Desktop default, WASM second). **WinUI parity:** the `/winui-runtime-tests` skill.
- **WinUI-first validation (MANDATORY every phase).** Tests encode WinUI behavior, so: (1) every WinUI-runnable theming test — old and new — must be green on `/winui-runtime-tests` before it judges Uno; (2) Uno-only-API behaviors (e.g. `ThemeHelper` app-theme helpers, `SystemThemeOverride`) are confirmed empirically in a throwaway WinUI probe app (`dotnet new install Microsoft.WindowsAppSDK.ProjectTemplates; dotnet new winui3 -o ThemingProbe`, kept out of the repo), and the observed values are encoded with a "confirmed in WinUI probe app" comment; (3) if Uno disagrees with a WinUI-green test, **Uno is wrong — fix Uno, not the test**.
- **Theming test filter (baseline suite):**
  ```
  Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Given_ElementTheme|Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Given_ThemeResource|Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Given_FrameworkElement_ThemeResources|Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Given_MergedAppResources_ThemeResource|Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Given_FrameworkElement_FocusVisuals|Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Given_Theme_Materialization|Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Given_ElementTheme_Resolution_Regression
  ```
- **Debugging discipline:** `.claude/rules/debugging-discipline.md` applies — root cause before guards; label evidence Code review / Compile / Runtime; never present compile-only as runtime validation.
- **No private tracker IDs** in committed code, tests, or commit messages (S1–S5 stay generic; uno #23177 is public).

---

## Phase 0 — Spec + baseline on the new base

**Goal.** This spec committed; the existing scaffold verified; a green/red baseline of the full theming suite recorded on this base.

**Steps.**
1. Commit this spec (replacing the prior-effort spec; parity reports moved to `prior-effort/`).
2. Verify the scaffold exists and compiles: `Given_Theme_Materialization` (T1–T10), `ThemeHelper` helpers (`UseSystemThemeOverride`, app-theme helpers). No re-authoring expected — they ship with the base.
3. Run the baseline suite on Skia (and WASM if practical); record results in a gitignored/out-of-repo `baseline-results.md`. The base is expected green — this baseline is the regression contract for every later phase.

**Acceptance.** Spec committed; suite results recorded; any pre-existing red is documented (not fixed here).

**Commits.** `docs(theming): respec WinUI alignment as exact 1:1 port`.

---

## Phase 1 — DependencyObject-level Enter/Leave (depends.cpp)

**Goal.** The Enter/Leave lifecycle becomes a real `DependencyObject`-level mechanism on enhanced-lifecycle targets: full `EnterParams`/`LeaveParams`; `CDependencyObject::Enter/Leave/EnterImpl/LeaveImpl` ported to `DependencyObjectStore`; the enter-property walk live; the `depends.cpp` theme block live inside it; the base's `EstablishThemeAtEnter` bolt-on subsumed and deleted. This phase is the foundation everything else stands on.

**WinUI refs.** `EnterParams.h:13-53`, `LeaveParams.h:12-50`, `depends.cpp:778-969` (Enter), `:971-1072` (EnterImpl incl. property walk `:1013-1032` and theme block `:1044-1069`), `:1079-1237` (Leave), `:1256-1342` (LeaveImpl), `CDependencyObject.h:1759-1764`/`:302`/`:586-592`, `framework.cpp:3113-3146` (`GetInheritanceParentInternal`), `uielement.cpp:1252-1400`/`1626-1772` (CUIElement Enter/LeaveImpl as the override pattern).

**Files.** `EnterParams.cs`/`LeaveParams.cs` (extend), `DependencyObjectStore.mux.cs` (new ← depends.cpp lifecycle subset), `DependencyObjectStore.cs` (flag bits), `UIElement.mux.cs` (rewire `DependencyObject_EnterImpl`/`DependencyObject_LeaveImpl` → store; the commented property walk + theme block become the store's live code), `DependencyObjectStore.Theming.cs:95-330` (`EstablishThemeAtEnter` + property-value walk — subsume/delete), `FrameworkElement` logical-parent (`GetInheritanceParentInternal` port; replaces `ThemeResolution.GetInheritanceParent`'s flat store-parent walk **at Enter** — resolution-time use is removed in Phase 3).

**Steps.**
1. Port `EnterParams`/`LeaveParams` 1:1 (all fields, constructor defaults, comments). Keep existing usage compiling.
2. Create `DependencyObjectStore.mux.cs` hosting `Enter(DependencyObject? namescopeOwner, EnterParams)`, `EnterImpl`, `EnterObjectProperty`, `EnterSparseProperties`, `Leave(...)`, `LeaveImpl`, `LeaveObjectProperty`, `LeaveSparseProperties` per `architecture.md` §5.1–§5.2. Namescope blocks not yet portable stay as preserved comments with `TODO Uno:` (exactly like the current `UIElement.mux.cs` does, but at the right level). The theme block is **live code**; until Phase 3 lands, its `NotifyThemeChanged`/`UpdateAllThemeReferences` calls bridge to the existing implementations (FE-level walk / store update methods) via a clearly-marked transitional shim.
3. Add `fIsProcessingEnterLeave`/`fIsProcessingThemeWalk` bits to the store (UIElement's `_uiElementFlags` copies become forwarders or move).
4. Rewire `UIElement.mux.cs`: `DependencyObject_EnterImpl/LeaveImpl` bodies move to the store; UIElement's `EnterImpl(params, depth)` calls `Store.EnterImpl` exactly where `CUIElement::EnterImpl` calls the base (`uielement.cpp:1356`). The ContextFlyout / KeyboardAccelerators / FE-Resources explicit blocks are re-expressed through `EnterSparseProperties`/the property walk where the metadata supports it, or kept with their existing `TODO Uno` notes if not yet.
5. ~~Define the `IEnterLeaveAware` internal interface; convert `FlyoutBase.Enter/Leave` to it.~~ **Amended during implementation:** `FlyoutBase.Enter/Leave` are the receivers of WinUI's *explicit* enter calls (`CUIElement::EnterImpl` ContextFlyout at uielement.cpp:1367, `CButton::EnterImpl`), not table-driven `EnterImpl` overrides — store-dispatching them would double the KA dead-enters. The dispatch interface is introduced together with its first real overrider (`ResourceDictionary`/`Popup`, Phases 5/7).
6. Property-walk exclusion set per §5.2 (metadata-derived; fold in the empirical exclusions encoded by `EstablishThemeAtEnter`'s walk, then delete the bolt-on — `EstablishThemeAtEnter`, `PropagateThemeEnterToDPPropertyValues`-style helpers, and their call sites in `UIElement.mux.cs`).
7. Wire DO-valued property sets on live owners to Enter/Leave the value (WinUI `SetValue`→enter semantics; locate Uno's `DependencyObjectStore.SetValue` parenting hook and add the live enter/leave call there, citing the C++ source).

**Acceptance.**
- Builds on Skia + WASM (+ native heads unchanged/compiling).
- Full theming suite == Phase 0 baseline (the real Enter walk must reproduce what `EstablishThemeAtEnter` covered — divergences are root-caused, and resolved in WinUI's favor with test evidence).
- New runtime test: a non-UIElement DO (e.g. a `Brush` set as a property value, a `Setter` in VSM) reaches `Enter` and carries the right theme when its owner is live.

**Commits (suggested split).** `feat(lifecycle): port full EnterParams/LeaveParams`; `feat(lifecycle): port CDependencyObject Enter/Leave to DependencyObjectStore`; `refactor(lifecycle): rewire UIElement enter walk through the store`; `refactor(theming): subsume EstablishThemeAtEnter into the real EnterImpl theme block`.

---

## Phase 2 — components/theming 1:1 (FrameworkTheming, interop, CThemeResource, cache)

**Goal.** The theming *component* exists as WinUI has it: `FrameworkTheming` state machine owned by core, an `IThemingInterop`-shaped OS bridge, `CThemeResource` semantics on the reference type, walk cache owned/keyed correctly.

**WinUI refs.** `FrameworkTheming.{h,cpp}` (whole files), `SystemThemingInterop.cpp:33-177`, `ThemeResource.{h,cpp}` (whole), `ThemeWalkResourceCache.{h,cpp}` (whole), `Theme.h`.

**Steps.**
1. `UI/Xaml/Theming/` folder: move/align `Theme.cs` (verify against fc2f82117; re-cite); port `FrameworkTheming.cs` (state + `GetTheme/GetBaseTheme/GetHighContrastTheme/HasHighContrastTheme/HasRequestedTheme/SetRequestedTheme/UnsetRequestedTheme/OnThemeChanged` + changing flags, member order per .cpp); port `SystemThemingInterop.cs` bridging `SystemThemeHelper` (base theme) + `WinRTFeatureConfiguration.Accessibility.HighContrast` (HC variant classification; OS HC detection on Skia = documented follow-up `TODO Uno:`).
2. Own it: `Uno.UI.Xaml.Core.CoreServices` gets the `FrameworkTheming` instance (`m_spTheming` analog) + the notify callback plumbed (target of Phase 6's `NotifyThemeChange`; until then the callback bridges to the existing `Application` flow with a transitional shim).
3. Align `ThemeResourceReference` to `CThemeResource` member-for-member (`m_strResourceKey`, `m_isValueFromInitialTheme`, `m_lastResolvedThemeValue`, target-dictionary weak ref, cache ref; `RefreshValue` `:63-129`; `SetLastResolvedValue` `:138-169`). Decide rename (`ThemeResource` vs keep) in-phase; file maps 1:1 either way.
4. `ThemeWalkResourceCache`: CoreServices-owned instance (not a static singleton), key `(dictionary, subtree theme, key)` incl. `SetSubTreeTheme`, scope guard semantics per `.cpp`.

**Acceptance.** Builds all heads; suite == baseline (component is wired but `Application` still drives via shims); unit tests for `FrameworkTheming.GetTheme` composition (`requested ?: system | HC`) and HC variant classification.

**Commits.** `feat(theming): port FrameworkTheming and SystemThemingInterop`; `refactor(theming): align ThemeResourceReference with CThemeResource`; `refactor(theming): move theme walk cache to CoreServices ownership`.

---

## Phase 3 — components/DependencyObject/Theming.cpp exact port + the ambient slot

**Goal.** All CDependencyObject theming methods live on `DependencyObjectStore`, 1:1, in file order; the core ambient slot exists with its three WinUI writers; the resolution leaf reads it. `ThemeResolution.cs` and the theme-parameter threading are retired. This is the phase that replaces Mechanism 1 with WinUI's real mechanism.

**WinUI refs.** `Theming.cpp` whole file (`:110-157`, `:159-255`, `:260-286`, `:288-346`, `:349-400`); `framework.cpp:3327` (FE `NotifyThemeChangedCore` override shape); `xcpcore.cpp:7903-7905` (slot ↔ walk-cache coupling).

**Steps.**
1. Rebuild `DependencyObjectStore.Theming.cs` as the full Theming.cpp port in C++ member order: `NotifyThemeChanged` (FE override read via `ActualInstance as FrameworkElement`, early-outs, walk flag, scoped ambient set, persist `_theme`), `NotifyThemeChangedCore`(+`Impl`: `UpdateAllThemeReferences`, property + sparse walk notifying DO values, peer/binding notify), `UpdateAllThemeReferences`, both `UpdateThemeReference` overloads (active-ancestor walk via the resolver, `RefreshValue` gate `IsProcessingThemeWalk || _theme != None || !IsValueFromInitialTheme`), and the **full** `SetThemeResourceBinding` (owner-theme scoped push → update → get → SetValue → store ref) — taking back the responsibilities currently split into `ResourceResolver.ApplyResource`.
2. `CoreServices.RequestedThemeForSubTree` + `IsThemeRequestedForSubTree` + `IsSwitchingTheme` (single slot + scope structs; §5.3), coupled to `ThemeWalkResourceCache.SetSubTreeTheme`.
3. `FrameworkElement`'s walk (`FrameworkElement.Theming.cs:164-341`) is deleted; FE keeps only the `NotifyThemeChangedCore` override calling base then `RaiseActiveThemeChangedEventIfChanging` (event body finalized Phase 4). `UIElement`'s children propagation goes where WinUI has it (`CUIElement::NotifyThemeChangedCore` walks children — port that override on UIElement).
4. Retire the parameter threading: leaf `TryGetValue(key, themeKey, …)` callers stop computing `ThemeResolution.ResolveOwnerTheme`; the dictionary's theme selection reads the slot/`FrameworkTheming` (minimal change here; the full `EnsureActiveThemeDictionary` port is Phase 5). Delete `ThemeResolution.cs`.
5. Transitional choke-point assert (debug): at the leaf, the slot-derived theme equals what `ResolveOwnerTheme(owner)` would have produced; run the full suite on Skia + WASM with **zero** hits before deleting the old path. Any hit is either a pre-existing approximation bug the port fixes (document + test) or a missing Enter (fix Phase 1's coverage — never re-add a resolution-time walk).

**Acceptance.** Builds all heads; full theming suite + T1–T10 == baseline (green) on Skia + WASM; assert silent across the suite; `/winui-runtime-tests` spot-check for nested-theme + dynamic-child scenarios; `ThemeResolution.cs` gone.

**Commits.** `feat(theming): port CDependencyObject theming to DependencyObjectStore 1:1`; `feat(theming): port the core RequestedThemeForSubTree slot`; `refactor(theming): retire ThemeResolution in favor of theme-at-Enter + the core slot`.

---

## Phase 4 — framework.cpp FrameworkElement theming 1:1

**Goal.** FE-level theming is exactly WinUI's: `OnRequestedThemeChanged`, `RaiseActiveThemeChangedEventIfChanging` (incl. the `HighContrastChanged` event + suppression rules), `ActualTheme`, `NotifyThemeChangedForInheritedProperties` (third ambient writer; foreground freeze).

**WinUI refs.** `framework.cpp:3501-3559`, `:3346-3386`, `:3969-3994`, `:3402-3489`; `FrameworkElement_Partial.cpp` (RequestedTheme plumbing; ActualThemeChanged delivery is posted async — match Uno's dispatcher semantics and document).

**Steps.** Rebuild `FrameworkElement.Theming.cs` to the C++ member set/order; add `public event TypedEventHandler<FrameworkElement, object> HighContrastChanged` (public API — check Generated stubs and update `[NotImplemented]` accordingly); port the freeze/unfreeze logic replacing the inlined approximation; keep Uno's `_themeForeground` storage only if it maps to WinUI's inherited-property freeze — otherwise port the WinUI mechanism and note.

**Acceptance.** Builds; suite green; T8 (foreground freeze) green; event-order test vs WinUI (`/winui-runtime-tests`): `ActualThemeChanged` sees the NEW theme; HC suppression rules probe-app-confirmed (flag-driven on Skia).

**Commit.** `feat(theming): port FrameworkElement theming (framework.cpp) incl. HighContrastChanged`.

---

## Phase 5 — Resources.cpp + ThemeResourceExtension + leaf finalization

**Goal.** `EnsureActiveThemeDictionary` finished 1:1 (HC branches incl. White/Black/Custom keys, `"Default"` fallback order, `MarkIsHighContrast`, sub-dictionary `NotifyThemeChanged` on switch) reading the core slot + `FrameworkTheming`; parse-time pinning aligned with `ThemeResourceExtension`/`ResourceResolver`; the remaining theme-parameter overloads and `Themes.Active` deleted.

**WinUI refs.** `Resources.cpp:644-819` (+ dictionary `NotifyThemeChanged` plumbing), `ThemeResourceExtension.cpp:44-247`, `components/resources/ResourceResolver.cpp:83-182`.

**Steps.**
1. Finish the `EnsureActiveThemeDictionary` port in `ResourceDictionary` (the base's `ResolveActiveThemeDictionary` is the starting point), sourcing requested-subtree theme from `CoreServices` and base/HC from `FrameworkTheming`; include the sub-dictionary `NotifyThemeChanged(m_activeTheme, highContrastChanged)` on switch (`Resources.cpp:793-815`).
2. Align the parse-time `{ThemeResource}` flow (`ResourceResolver.ApplyResource`/`TryStaticRetrieval`) with `ThemeResourceExtension::ProvideValue`/`LookupResource` semantics (pin + register dependency); `SetThemeResourceBinding` (Phase 3) is the single write path.
3. Delete the theme-parameter `TryGetValue`/`ContainsKey` overload threading and `Themes` (`Active`, `GetActiveBaseTheme`, test-only `SetActiveTheme` — update the tests that used it to drive `FrameworkTheming`/app theme instead). Remove Phase 1–3 transitional shims.

**Acceptance.** Builds; full suite + T1–T10 green Skia + WASM; grep-clean for `Themes.Active`/`GetActiveTheme()`-style ambient remnants outside the ported slot; native theming unchanged.

**Commits.** `feat(theming): finish EnsureActiveThemeDictionary 1:1`; `refactor(theming): retire Themes.Active and the theme-parameter leaf`.

---

## Phase 6 — CCoreServices::NotifyThemeChange + app/OS plumbing

**Goal.** Full-app theme changes flow exactly like WinUI: `FrameworkTheming.OnThemeChanged` → `CoreServices.NotifyThemeChange` walking all roots; `Application.RequestedTheme` matches `FrameworkApplication` (pre-load settable); OS theme changes route through the interop; custom-theme axis ditched.

**WinUI refs.** `xcpcore.cpp:8006-8118`; `FrameworkApplication_Partial.cpp:987-1061`; `custom-theme.md` (decision).

**Steps.** Port `CoreServices.Theming.cs` `NotifyThemeChange` (cache scope; themed-brush/text resets where Uno has equivalents — `TODO Uno:` where not; global theme resources, app resources, **popup root**, visual root + inherited-properties freeze, island roots, out-of-tree listeners); `Application.RequestedTheme` delegates to `FrameworkTheming` (pre-load guard per `:993-996`); `SystemThemeHelper` change events → `FrameworkTheming.OnThemeChanged`; `ApplicationHelper.RequestedCustomTheme` → `[Obsolete]` no-op; HC toggle (flag) triggers the same `OnThemeChanged` path.

**Acceptance.** Builds; suite green; T7 (app switch), T9 (explicit-app-theme suppresses OS), T10 (fallback) green; popup-root + island walk covered by tests; custom-theme breaking change documented.

**Commits.** `feat(theming): port CCoreServices::NotifyThemeChange`; `feat(theming): align Application.RequestedTheme with FrameworkApplication`; `feat!(theming): retire RequestedCustomTheme (no-op)`.

---

## Phase 7 — Popup/Flyout 1:1

**Goal.** Popup child dead-enter + live-enter-at-open; logical-parent theme inheritance (mechanically provided by Phase 1's theme block + `GetInheritanceParentInternal`); FlyoutBase presenter theme forwarding per `FlyoutBase_Partial.cpp`.

**WinUI refs.** `popup.cpp:2846-2936`; `framework.cpp:3113-3146`; `FlyoutBase_Partial.cpp` (theme forwarding; re-read at fc2f82117); `xcpcore.cpp:8068-8072` (popup root in the walk — done Phase 6).

**Steps.** Align `Popup` Enter/Leave overrides (dead-enter child for namescope; live enter on open through PopupRoot parenting); verify popup/flyout content first-open theming falls out of the mechanism (T4/T5); rebuild `FlyoutBase.ForwardThemeToPresenter` 1:1 from the C++ (enhanced-lifecycle; native no-op).

**Acceptance.** T4/T5 green Skia + WASM (excluded native); `Given_ElementTheme` popup/flyout regions green; `/winui-runtime-tests` parity for flyout-over-themed-content.

**Commit.** `fix(theming): popup/flyout first-open theming via logical-parent Enter inheritance`.

---

## Phase 8 — Full validation, parity, cleanup, docs

**Steps.**
1. WinUI oracle re-confirm: entire suite on `/winui-runtime-tests` all green.
2. Uno match: entire suite + T1–T10 via `/runtime-tests` Skia + WASM; Android/iOS native-applicable subset; compare to Phase 0 baseline, explain every legitimate change (each must itself be WinUI-green).
3. Remove remaining transitional shims/dead `#if` branches; grep-clean ambient remnants.
4. Benchmarks: `src/SamplesApp/Benchmarks.Shared/.../ResourceDictionaryBench/*` before/after — neutral or better.
5. Developer doc: short "Theming model" note (resolution = f(key, owner `m_theme` via the core slot); theme at Enter; logical-parent for popups; the three ambient writers) so band-aids don't return.

**Commits.** `test(theming): full cross-platform + WinUI parity validation`; `chore(theming): remove transitional shims and document theming model`.

---

## Scenario → phase traceability

| Scenario (see `tests.md`) | Secured primarily by | Verified by |
|---|---|---|
| S1/S5 virtualized/runtime-added | Phase 1 (Enter theme) + 3 (slot leaf) | T1, T6 |
| S2 recycle | Phase 1 (no theme clear on Leave; re-Enter re-themes) + 3 | T2 |
| S3 scrolled-in cells | Phase 1 + 3 | T3 |
| S4 popup/flyout first open | Phase 7 (on 1+3 foundations) — Skia/WASM; native = app/OS theme | T4, T5 |
| uno #23177 app switch | Phase 6 | T7 + existing |
| OS-follow suppression | Phase 6 (`FrameworkTheming`) | T9 |
| Foreground freeze | Phase 4 | T8 |
| Fallback/dark-leak | Phase 5 (`EnsureActiveThemeDictionary` order) | T10 |
