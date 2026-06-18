# Remaining work — phased plan

Phases are ordered by priority. Phases 1–2 are **current-branch adjustments** (the branch is not done until
these land). Phases 3+ are **follow-up** alignment-completion work and may ship separately. Every phase keeps the
theming runtime suite + `Given_AlcContentHost.When_SecondaryAlcApp_ExplicitTheme_Then_IndependentFromHost` green,
and follows the WinUI-oracle doctrine (`/winui-runtime-tests` green first, then `/runtime-tests` Skia + WASM).

Global conventions: enhanced-lifecycle (`UNO_HAS_ENHANCED_LIFECYCLE`, Skia + WASM) only; native paths preserved;
any new cross-ALC subscription must be torn down per ALC (cf. `SystemThemeHelper.ClearNonDefaultAlcHandlers`).

---

## Phase 1 — Fix runtime high contrast (HIGH, current-branch) · A3-2 / A6-3 / A9-4

**Goal.** A runtime high-contrast toggle re-resolves all `{ThemeResource}` values to the HC sub-dictionary,
fires `HighContrastChanged`, and makes `FrameworkTheming` the single HC source of truth — matching WinUI, where
the OS HC change drives `FrameworkTheming::OnThemeChanged` → `NotifyThemeChange` (`JupiterWindow.cpp:1547-1552`,
`FrameworkTheming.cpp:31-67`, `framework.cpp:3364-3367`).

**Steps.**
1. **Route the HC change into the theme pipeline.** Subscribe (on enhanced-lifecycle, per-`Application`,
   ALC-safe-teardown) `AccessibilitySettings.HighContrastChanged` — or add a `SystemThemeHelper`-level HC signal
   sourced from `WinRTFeatureConfiguration.Accessibility.HighContrast` — and forward to
   `WinUICoreServices.Instance.Theming.OnThemeChanged()`. `FrameworkTheming.OnThemeChanged` already re-reads
   `GetSystemHighContrastTheme()` and sets `m_isHighContrastChanging` (`FrameworkTheming.cs:57,66`), so this one
   hook refreshes the snapshot, runs the walk, and raises the events.
   - Files: `src/Uno.UI/UI/Xaml/Application.cs` (near the `:488` `SystemThemeChanged` subscription / `:521-540`
     `OnSystemThemeChanged`), `src/Uno.UWP/Helpers/Theming/SystemThemeHelper.cs`,
     `src/Uno.UWP/FeatureConfiguration/WinRTFeatureConfiguration.Accessibility.cs`. Keep
     `AccessibilitySettings.HighContrastChanged` firing for `BackdropMaterial`.
2. **Make the leaf read the snapshot, not the live flag.** In `ResourceDictionary.cs:752` derive high contrast
   from `core.Theming.GetHighContrastTheme()` (the `FrameworkTheming` snapshot) instead of
   `SystemThemeHelper.IsHighContrast` (`Themes.IsHighContrast`, `:1421`), mirroring `Resources.cpp:701/718/740`.
   After step 1 both reads agree; this removes the structural dual source. Guard a no-core fallback for native/
   unit-test contexts.
3. **ALC teardown.** Tear the subscription down on ALC teardown alongside `SystemThemeHelper.ClearNonDefaultAlcHandlers`
   so inner-app `Application` instances are not leaked.

**Acceptance / tests.**
- New runtime test: app Light, a `Border.Background={ThemeResource Sentinel}` with `ThemeDictionaries`
  `Light=Green`/`HighContrast=Red`; assert Green after load; set `WinRTFeatureConfiguration.Accessibility.HighContrast
  = true` **after** content is loaded; assert the brush re-resolves to Red, `FrameworkTheming.HasHighContrastTheme()
  == true`, and a `FrameworkElement.HighContrastChanged` subscription fired. Restore the flag in `finally`.
- Extend `Given_Theme_Materialization.When_HighContrast_Active_Selects_HighContrast_Dictionary` to cover the
  post-load toggle (today it only sets HC pre-load).
- ALC: re-run `Given_AlcContentHost…IndependentFromHost` plus a secondary-app HC toggle (the stale snapshot fed
  `GetEffectiveWalkTheme` at `Application.Alc.cs:169`).

---

## Phase 2 — Remaining current-branch adjustments · A7-1, doc/policy

**2a. Native secondary-ALC theme drop (A7-1, Medium).** Wrap the `if (_isSecondaryAlcApplication) { … return; }`
diversion in `SetExplicitRequestedTheme` (`Application.cs:326-335`) in `#if UNO_HAS_ENHANCED_LIFECYCLE` (or
`#if __SKIA__ || __WASM__`) so native secondary apps fall through to the `#else` branch (which still applies the
theme). If native ALC theming is intentionally unsupported, document it and make the native
`RequestedTheme`/`IsThemeSetExplicitly` getters (`Application.cs:211,323`) read `_alcRequestedTheme` so the
getters stay self-consistent with what was set. Test: a native (or native-simulated) secondary app's
`RequestedTheme` is observable via the getter and applied.

**2b. Documentation & policy fixes (Low).**
- **A9-5:** update `specs/theming-winui-alignment/README.md` status — replace "ALL PHASES (0–8) COMPLETE" with
  "behaviorally complete; structural follow-ups open", listing the retained-by-design items (`ResolveOwnerTheme`,
  `Themes.Active` native/no-core mirror) and the open ports (Phases 1/3/4/5 here).
- **A1-4/A2-6:** correct the misleading TODO at `DependencyObjectStore.Theming.cs:197-200` (per-child
  `NotifyThemeChanged` already runs on the walk path at `:711-729`).
- **A4-8:** fix the stale `Given_ThemeResource.cs:419` comment (no `GetThemeResolutionParent` exists).
- **A4-9:** replace the `kahua-private` URLs in `Given_MenuFlyout.cs:550` / `Given_ToolTip.cs` with public
  `unoplatform/uno` issue numbers (AGENTS.md one-way public/private rule).
- Stale C++ citations: `Application.Alc.cs:176` (`corep.h:2207` → `2144-2145`), `CoreServices.Theming.cs:38`
  (`xcpcore.cpp:8132` → `7905`), the modifier comment (`Theming.cpp:405-410` → `357-362`).

---

## Phase 3 — Port the modifier-being-set gate (follow-up, Medium) · A2-4 / A8-1 / A8-2

**Goal.** Replace the counter-based `SuppressLocalCanDefeatAnimations` band-aid with WinUI's per-property gate, so
a `{ThemeResource}` re-resolution under a held animation **skips** the `SetValue(Local)` base re-stamp instead of
masking the flag — covering **all** re-resolution paths, including the Enter-walk-only `UpdateAllThemeReferences`
branch (`mux.cs:280`) the current suppress misses.

**WinUI:** `Theming.cpp:357-362` (`SetModifierValueBeingSet(true)` when `HasModifiers`) +
`PropertySystem.cpp:1649-1651` (`SetValue` skips `SetBaseValue(BaseValueSourceLocal)` while
`IsModifierValueBeingSet()`).

**Steps.**
1. Add an `IsModifierValueBeingSet`/`SetModifierValueBeingSet` flag to `ModifiedValue`
   (`src/Uno.UI/UI/Xaml/ModifiedValue.cs`).
2. Arm it (when the property `IsAnimated`/`HasModifiers`) around the theme-ref re-stamp in `UpdateThemeReference`
   (`DependencyObjectStore.Theming.cs` ~`:488`).
3. Make `SetBaseValue` skip the Local re-stamp while set (also fixes A8-2: `_baseValue` no longer overwritten
   under a held animation).
4. Remove the `SuppressLocalCanDefeatAnimations`/`ContinueLocalCanDefeatAnimations` wrappers
   (`DependencyObjectStore.Theming.cs:779-787` and the global one in `CoreServices.Theming.cs:186-253`).

**Interim (if Phase 3 is deferred):** wrap the `mux.cs:280` `UpdateAllThemeReferences` call in the same
`Suppress/Continue` try/finally so the current patch is not silently incomplete on the Enter-walk path.

**Tests.** Keep `When_ThemeResource_Reresolution_Does_Not_Defeat_Active_Animation` (5e14bb64ad) green; add a
variant that triggers the Enter-walk path (a templated child re-entering the live tree with a held animation on a
themed property) to cover the gap the band-aid missed.

---

## Phase 4 — High-contrast variant fidelity (follow-up, Medium) · A3-4 → A3-3/A5-6 → A3-5

Sequenced because the variant work needs real OS system colors first.

**4a. Real OS system-color palette (A3-4).** Decide explicitly: either implement
`SystemThemingInterop.GetSystemColor` against the OS palette (`SystemThemingInterop.cpp:179-203`) and consume
`GetColorAndBrushResourceInfoList` in `CoreServices.NotifyThemeChange` (`UpdateColorAndBrushResources`,
`xcpcore.cpp:8017-8026`), **or** prune the dead `RebuildColorAndBrushResources`/`GetColorAndBrushResourceInfoList`
if Uno permanently sources the palette from generated resources. Today both are dead/stubbed
(`SystemThemingInterop.cs:71-81`, `CoreServices.Theming.cs:122-133`).

**4b. OS HC variant White/Black/Custom (A3-3/A5-6).** Port `GetSystemHighContrastTheme` variant classification
(`SystemThemingInterop.cpp:121-177`, incl. Xbox `0xEBEBEB`/`0x101010`); thread the variant through
`FrameworkTheming`/`Themes`; **add the missing `HighContrastCustom` resource key** (`ResourceDictionary.cs:1407-1409`);
switch the app-wide HC branch on `GetFrameworkTheming()->GetHighContrastTheme()` 1:1 with `Resources.cpp:740-753`
(replacing the base-theme-derived `GetHighContrastKeyForBaseTheme`, `ResourceDictionary.cs:786,852-853`). Keep the
subtree-requested branch base-theme-derived.

**4c. HC root background (A3-5).** After 4a, make `ThemingHelper.GetRootVisualBackground` delegate to
`CoreServices.Instance.Theming.GetRootVisualBackground()` so HC root background (system WINDOW color) is honored
and the ported method stops being dead code.

**Tests.** HC-variant dictionary selection (White vs Black vs Custom) under each base theme; root background in HC.

---

## Phase 5 — Slot-only end-state + DoD reconciliation (follow-up, Low) · A9-1/A9-2/A9-3/A5-4/A1-1/A2-1/A8-7

This is the "finish or formally sanction the hybrid" program item. Two tracks:

**5a. Slot ↔ walk-cache coupling (A9-3, optional 1:1).** Have `CoreServices.SetRequestedThemeForSubTree` call
`ThemeWalkResourceCache.SetSubTreeTheme(GetBaseValue(theme))`; switch the cache to read its stored subtree theme
and drop the `Theme` parameter from `TryGetCachedValue`/`CacheValue`. Behavior-neutral; removes the second
mechanism. Add a guard test asserting `RefreshValue`'s cache theme equals the active slot theme.

**5b. Retire `ResolveOwnerTheme` / the owner-theme parameter (A1-1/A1-2/A2-1/A5-4/A9-1).** **Blocked on** per-DP
InheritanceContext (uno #22949) so non-FE / standalone resource DOs establish `_theme` at Enter. When unblocked:
add WinUI's `m_theme != None` outer gate at the slot push (`Theming.cs:403`), drop the `ownerThemeOverride`
parameter chain and the 4th refresh-gate term (A9-7), migrate all ~9 `ResolveOwnerTheme` call sites to push the
DO's own `GetTheme()`, and delete `ThemeResolution.cs`. Drive a transitional choke-point assert (`slot ==
ResolveOwnerTheme`) to zero hits on Skia + WASM first.

**5c. DoD reconciliation (do now, even if 5a/5b are deferred).** Because `ResolveOwnerTheme` is now a thin,
behaviorally-WinUI-equivalent owner-theme accessor and `Themes.Active` is a justified native/no-core mirror,
**amend** the prior DoDs rather than force deletion:
- `theming-winui-alignment/README.md:50` and `architecture.md:175`: stop asserting "ThemeResolution.cs … gone";
  describe `ResolveOwnerTheme` as the sanctioned single owner-theme accessor (walk + param-threading retired in
  `52ea2e1d9c`/`44ff016192`). Optionally rename/relocate the helper if the wording must be honored literally.
- `architecture.md:179` / `plan.md` Phase 5: mark `Themes.Active`/`GetActiveBaseTheme`/`SetActiveTheme` retained
  as the native + no-CoreServices mirror; port `Given_ThemeResolution.cs` to seed `FrameworkTheming`/
  `ScopeRequestedThemeForSubTree` instead of `SetActiveTheme`.

---

## Phase 6 — Resolution-scope completeness (follow-up, Low) · A4-1 / A4-4 / A8-3 / A4-3

**6a. `GetDictionaryForThemeReference` canonicalization (A4-1).** After the providing-dictionary capture in
`ApplyResource`/`TryStaticRetrieval`, canonicalize the pinned dict per `ResourceResolver.cpp:947-986`:
system-colors → self, global-theme owner → the stable global theme-resources root, app → app resources. The
dangerous "pin a Light/Dark sub-dict" trap is already avoided; this closes the merged-child/app-resources edges.

**6b. Construction-time `IsGlobal` + propagation (A4-4/A8-3).** Emit a fixed `IsThemeDictionariesOwner` flag from
the generator (not inferred from the lazily-allocated `_themeDictionaries is not null`, which `IsEmpty` can flip),
and propagate `IsGlobal` into merged/theme child dictionaries (mirror `MarkAllIsGlobal`,
`Resources.cpp:2160-2181`) — or scope `IsGlobal` to the genuine global theme-resources root only.

**6c. `GetParentFollowPopups` hop (A4-3, R2).** Only if a future repro shows opener-only **live-walk** resolution
failing (the parse-time pin covers the shipped popup repros). Port the hop into
`DependencyObjectStore.GetResourceDictionaries` and the inheritance-parent walk.

---

## Phase 7 — ALC override source & bootstrap (follow-up, Low) · A4-2 / A7-2 / A8-4 / A7-3

**7a. Per-owning-app override (A4-2/A7-2/A8-4).** Thread the resolving owner's `Application`/ALC (available from
the pinned dictionary's parse context / the owning `CoreServices`) into `TryApplicationResourceOverride`
(`ResourceResolver.cs:780-792`) and `LookupThemeResource`/`ResolveTopLevelResource`, so a secondary-ALC element's
own `Application.Resources` override is consulted before the host's — mirroring WinUI's per-core
`GetApplicationResourceDictionary`. Test: a secondary app overriding a Fluent/system theme key sees its own value
on theme re-resolution.

**7b. Bootstrap walk gating (A7-3).** Skip/short-circuit `InitializeSystemTheme` for `_isSecondaryAlcApplication`
on enhanced targets (the shared FrameworkTheming is already host-initialized).

---

## Phase 8 — Misc low-severity follow-ups

- **A1-5 — VisualState theme source.** Source the lazy-chain establishment theme from the VisualState's actual
  inheritance parent (its VSG / the template root part carrying the established `_theme`) instead of
  `GetTemplatedParent()` (`VisualState.cs:191-196`); validate the parent `_theme` is established by `GoToState`
  materialization time. Edge: root part with a distinct `RequestedTheme`.
- **A6-5 — Popup overlay.** Either re-theme the light-dismiss overlay/`PopupPanel` background against
  `Child.GetTheme()` when a child is present (`popup.cpp:3671-3677`), or keep the current approach and document it
  with a parity test for system overlay brushes (`Popup.cs:175-177`).
- **A6-6 — `IThemeChangeAware` double-invoke.** Fire `themeAware.OnThemeChanged()` once (via the
  `UpdateChildResourceBindings` path), removing the up-front call in `NotifyThemeChangedForInheritedProperties`
  (`FrameworkElement.Theming.cs:479-485` vs `DependencyObjectStore.Theming.cs:634-638`).
- **A9-8 — `SetThemeResourceBinding` responsibility split.** Confirm the shipped split (`ApplyResource` ≈
  `ThemeResourceExtension::LookupResource`; `SetThemeResourceBinding` ≈ `Theming.cpp:349-400`) as the final shape
  and amend the prior plan, or fold the two `TODO Uno` setter paths (`ApplyThemeResource`,
  `ApplyVisualStateSetter`) into the `SetThemeResourceBinding` sequence.

---

## What is explicitly *not* to be changed (verified correct — do not "fix")

The resolution leaf reads the **single** core slot (A5-1); R1 providing-dictionary pinning is correctly shipped
(A4); the parameter-vs-slot "hybrid" is **coupled, not competing** (A4-6/A2-2/A7-5); the walk-cache "TODO Uno"
is behavior-neutral (A9-3 refined); `FrameworkTheming` process-shared is the **deliberate ALC foundation** (A3-1);
`HighContrastChanged` is correctly **internal** (A6-2/A9-6); `ActualThemeChanged` async (A6-1); foreground freeze
reaches popups (A6-4); the weak-keyed listener table is ALC-safe (A6-8/A7-6); the secondary-app element-pin
isolation is correct (A7-4); the `ColorAnimation` re-apply and VisualState lazy-chain fixes are root-cause
(A8-5/A8-6). Re-deriving or "simplifying" any of these would regress parity.
