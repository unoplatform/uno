# Verified discrepancy register

> Severities are **post-adversarial-verification** (a skeptic independently re-read both sides). `Verdict`
> records that re-check: **confirmed** (real as stated), **refined** (real but mis-stated/re-scoped),
> **refuted** (Uno is actually correct/equivalent). WinUI refs are against `fc2f82117`; Uno refs against this
> branch. Two verdicts (A7-1, A4-2) were lost to API rate-limiting during the audit and are marked
> *needs-verification* — both were corroborated by direct reading while writing this spec.

Disposition legend: **CB** = current-branch adjustment · **FU** = follow-up · **OK** = confirmed correct/justified (no action).

---

## A. Top defect — runtime high contrast (CB, High)

### A3-2 — High contrast has two sources of truth; the snapshot is never refreshed on a runtime toggle · **HIGH · CB · confirmed**
- **WinUI:** one HC value `m_highContrastTheme`, snapshotted in `FrameworkTheming::OnThemeChanged` and read by
  both the resolution leaf (`Resources.cpp:701/718/740` `GetFrameworkTheming()->GetHighContrastTheme()`) and the
  change/event code. The OS HC change invokes `OnThemeChanged` (`JupiterWindow.cpp:1547-1552`, via
  `UISettings.ColorValuesChanged`), which walks all roots with `forceRefresh`.
- **Uno:** the leaf reads a **live** flag — `ResourceDictionary.cs:752,1421` → `SystemThemeHelper.IsHighContrast`
  → `WinRTFeatureConfiguration.Accessibility.HighContrast`. `FrameworkElement.Theming.cs:107,340,347` and
  `Application.Alc.cs:169` read the **cached** `FrameworkTheming` snapshot. The HC setter
  (`WinRTFeatureConfiguration.Accessibility.cs:18-25`) raises only `AccessibilitySettings.HighContrastChanged`,
  whose **sole** subscriber is `BackdropMaterial.cs:134`. `Application.cs:488` subscribes only
  `SystemThemeHelper.SystemThemeChanged`, which never fires on HC. The Win32 listener
  (`Win32SystemThemeHelperExtension.cs:27-39`) watches only `…\Personalize` (light/dark).
- **Effect:** a runtime HC toggle flips the live flag the leaf reads but **never calls `OnThemeChanged`** → the
  snapshot stays `HighContrastNone`, `HasHighContrastTheme()`/`IsHighContrastChanging()` stay false, no walk
  runs, already-materialized values keep non-HC values, and `HighContrastChanged` never fires. The two HC reads
  disagree. The existing test (`Given_Theme_Materialization.cs:1048-1049`) only sets HC *before* load.
- **ALC:** the stale snapshot is OR-ed into `GetEffectiveWalkTheme` (`Application.Alc.cs:169`), so secondary-app
  HC is corrupted too.
- **Fix:** (1) route `AccessibilitySettings.HighContrastChanged` (and a real OS HC signal when added) →
  `WinUICoreServices.Instance.Theming.OnThemeChanged()`; (2) switch the leaf to read
  `core.Theming.GetHighContrastTheme()` instead of `SystemThemeHelper.IsHighContrast`. Add a post-load HC-toggle
  test asserting re-resolution to the HC sub-dictionary + `HasHighContrastTheme()==true` + `HighContrastChanged`
  fired. Make the subscription per-Application/ALC-safe (cf. `SystemThemeHelper.ClearNonDefaultAlcHandlers`).

### A6-3 / A9-4 — (same root cause, separate angles) · **Medium · FU/CB · confirmed**
A6-3 frames it as "the `HighContrastChanged` event A6 added is unreachable on Skia"; A9-4 frames it as
"the resolution-scope spec's own deferred follow-up." Both reduce to A3-2's missing wiring. `FrameworkTheming.
OnThemeChanged` already detects the HC axis (`FrameworkTheming.cs:57,66`) — only the invocation is missing.
Fold into the A3-2 fix.

---

## B. CDependencyObject lifecycle & slot writers (A1, A2)

### A2-4 / A8-1 — Modifier-being-set gate unported; the counter-based suppress is a band-aid · **Medium · FU · confirmed**
- **WinUI:** `SetThemeResourceBinding` arms `pModifiedValue->SetModifierValueBeingSet(true)` when `HasModifiers`
  (`Theming.cpp:357-362`); `SetValue`'s `if (modifiedValue && !modifiedValue->IsModifierValueBeingSet())
  SetBaseValue(.., BaseValueSourceLocal)` (`PropertySystem.cpp:1649-1651`) then **skips the Local re-stamp
  entirely** — the local-newer bit is never set, on *any* re-resolution path.
- **Uno:** no such gate. `5e14bb64ad` wraps `UpdateResourceBindingsCore` in the counter-based
  `ModifiedValue.SuppressLocalCanDefeatAnimations()` (`DependencyObjectStore.Theming.cs:779-787`), which
  *masks* the flag-flip. `bcc8191375` honestly re-documents this as a stand-in for the real gate.
- **Gap (A8-1):** the Enter-walk `else` branch (`DependencyObjectStore.mux.cs:280`) calls
  `UpdateAllThemeReferences` **directly**, bypassing the suppress wrapper, and re-stamps theme refs at Local
  (`Theming.cs:488`) — so a templated child re-entering with a held animation is unguarded. Narrow reachability,
  but WinUI's universal gate has no hole. **A8-2** (low): Uno's suppress still re-stamps `_baseValue`; WinUI's
  gate skips `SetBaseValue` entirely.
- **Fix:** port a `ModifierValueBeingSet` flag onto `ModifiedValue`; arm it around the theme-ref re-stamp when
  animated; make `SetBaseValue` skip the Local re-stamp while set — covering all paths incl. `mux.cs:280`. Then
  remove the suppress. **Interim:** wrap `mux.cs:280` in the same suppress so the patch isn't silently incomplete.
- Note: in-code comment cites `Theming.cpp:405-410`; the gate body is `Theming.cpp:357-362` (the file ends ~404).
  `_localCanDefeatAnimationSuppressed` is a *static* counter, not per-object (the ALC note "per-object" is imprecise;
  conclusion not-cross-ALC still holds — it's a value counter that stores no references).

### A2-1 / A1-1 — Slot push lacks WinUI's `m_theme != None` gate; Enter threads an owner-theme parameter · **Low · FU · refined**
- **WinUI:** `SetThemeResourceBinding` pushes the slot **only** when `!IsProcessingThemeWalk() && m_theme !=
  Theme::None` (`Theming.cpp:368-376`). The `EnterImpl` else-branch is a bare `UpdateAllThemeReferences()` with
  no theme arg (`depends.cpp:1043-1047`).
- **Uno:** `DependencyObjectStore.Theming.cs:403-411` pushes whenever `!IsProcessingThemeWalk` (no `_theme !=
  None` term), pushing `ResolveOwnerTheme(owner)` (which never returns `None` — `ThemeResolution.cs:47-65`); and
  `mux.cs:280` threads `ResolveOwnerTheme(owner)` as an explicit `ownerThemeOverride`, which WinUI has no analog
  for.
- **Verdict (refined → low):** value-equivalent for the base Light/Dark dimension (the `None` fallback *is* the
  app base theme). The only bit that branches on `IsThemeRequestedForSubTree()` is HC variant selection, which
  Uno already collapses (A3-3), so **no new regression**. This is the documented transitional shape; do not
  surgically rewrite one site — fold into the program item below.

### A1-2 / A5-4 / A9-1 — `ThemeResolution.ResolveOwnerTheme` still exists & is threaded into ~9 sites (DoD said delete) · **Low–Medium · FU · refined**
- **WinUI:** the three slot writers each read the owning DO's persisted `m_theme`/walk theme directly; there is
  no resolution-time owner-theme helper.
- **Uno:** `ThemeResolution.cs:47-65` (the walk is **gone** — it now reads `provider.Store.GetTheme()` with a
  `GetActiveBaseTheme()` fallback, i.e. WinUI's `ActualTheme` semantics). Callers:
  `DependencyObjectStore.Theming.cs:396,799`, `mux.cs:280`, `ResourceResolver.cs:422,454`,
  `DependencyProperty.cs:648`, `Hyperlink.cs:240`, `VisualState.cs:192`, `VisualStateGroup.cs:265`.
- **Verdict (refined → low):** the substantive grievances (ancestor walk, theme-param-threaded leaf) were
  genuinely retired (`52ea2e1d9c`, `44ff016192`); what remains is a thin, behaviorally-WinUI-equivalent owner-theme
  accessor feeding the slot. The verification recommends **amending the DoD** (and optionally renaming the file)
  rather than inlining at 9 sites with no behavioral change — *unless* full 1:1 deletion is wanted, which is
  blocked on establishing `_theme` at Enter for **non-FE / standalone resource DOs** (per-DP InheritanceContext,
  uno #22949).

### A9-7 — Transitional shim `ownerThemeOverride is not null` in the Phase-B refresh gate · **Low · FU · (no verdict; low)**
- WinUI's gate is exactly `IsProcessingThemeWalk() || m_theme != None || !IsValueFromInitialTheme()`
  (`Theming.cpp:338-343`). Uno adds a 4th term `|| ownerThemeOverride is not null`
  (`DependencyObjectStore.Theming.cs:444-448`) as a stand-in for an established `m_theme` on standalone resource
  DOs. Harmless (only ever *enables* a refresh WinUI would also do). Removable together with A1-2 once #22949
  lands. (A2-3/A4-10 are the same term, **OK/justified**.)

### A1-5 — VisualState establishes `_theme` from the templated parent, not its inheritance parent · **Low · CB · refined**
- `044ebd3f49` correctly replaced a Begin-time band-aid with a real `Store.NotifyThemeChanged` establishment on
  the lazy VSM chain (A8-6, **root-cause**). But it sources the theme from `GetTemplatedParent()` (the control
  instance) — `VisualState.cs:191-196` — whereas WinUI inherits from the VisualState's inheritance parent
  (VSG → … → the template **root part**, `depends.cpp:1023-1047` via `CVisualState::EnterImpl` which *does* call
  the base — the audit's first "no EnterImpl override" claim was **refuted**). Agree unless the root part carries
  a distinct `RequestedTheme` — the exact island/element-theme edge this work targets. **Fix:** source from the
  VisualState's actual inheritance parent (its VSG / the root part carrying the established `_theme`); validate
  the parent's `_theme` is established by the time `GoToState` lazily materializes.

### A1-4 / A2-6 — Misleading TODO: per-child `NotifyThemeChanged` is **not** missing on the walk path · **Low · FU(doc) · (no verdict)**
- The TODO at `DependencyObjectStore.Theming.cs:197-200` claims non-UIElement children resolve only via the
  ambient slot, deferring per-child `NotifyThemeChanged` to Phase 5 — but the walk path **already** calls
  `walkProvider.Store.NotifyThemeChanged(_walkTheme, _walkForceRefresh)` (`:711-729`), persisting each child's
  `_theme` like `Theming.cpp:218-244`. **Fix:** correct the TODO; optionally re-point the *non-walk* (loading/
  hot-reload) propagation to a literal per-child walk (Phase 5 structural item).

### A1-6 — Popup dead-enter & namescope adjustments not ported · **Low · FU · (no verdict; out of theming scope)**
- `popup.cpp:2868` dead-enters the child (`fIsLive=FALSE`) for name registration; namescope adjustments at
  `depends.cpp:836-928`. Uno has no namescope tracking (flagged `NOT PORTED` at `mux.cs:120-133`,
  `PropertySystem.mux.cs:260-263`). **For theming this is fine** — popup-content theme inheritance comes via
  `LogicalParentOverride` feeding the EnterImpl theme block. Track namescope porting as separate non-theming work.

### Confirmed-correct in this area (no action)
- **A1-7 (OK):** `EnterParams`/`LeaveParams` extended to the full WinUI field set; `_theme` correctly **not**
  cleared on Leave (load-bearing for reparenting).
- **A1-8 (OK):** Enter ordering (store `EnterImpl` theme block before `ChildEnter`) and `NotifyThemeChanged` are
  1:1 (FE override read, slot push gated on `GetBaseValue`, `_theme` persisted after `NotifyThemeChangedCore`).
- **A2-2 / A2-7 (OK):** the slot↔cache "TODO" is harmless (cache theme is slot-derived at the read site); slot,
  cache, and listener table all live on the per-ALC `CoreServices` singleton — ALC-correct.
- **A2-5 (OK):** `6cac205035` replaced an approximation with WinUI's exact `IsGlobalThemeDictionaries` flag
  (`m_isGlobal && m_bIsThemeDictionaries`) — a move toward parity. (But see A4-4/A8-3 for the lazy-flag/propagation
  edges.)

---

## C. FrameworkTheming / interop / Application (A3)

### A3-3 / A5-6 — OS high-contrast variant (White/Black/Custom) collapsed to one bool; no `HighContrastCustom` key · **Medium · FU · confirmed**
- **WinUI:** `SystemThemingInterop.cpp:121-177` classifies the OS HC variant from WINDOW/WINDOWTEXT (incl. Xbox
  `0xEBEBEB`/`0x101010`) into White/Black/Custom; `Resources.cpp:740-753` selects the matching HC sub-dict.
- **Uno:** `SystemThemingInterop.cs:49-69` always returns generic `Theme.HighContrast`;
  `ResourceDictionary.cs:786,852-853` derives the HC key from the **base** theme (Light→White, Dark→Black) and
  falls back to generic `HighContrast`. **There is no `HighContrastCustom` resource key at all**
  (`ResourceDictionary.cs:1407-1409`), so a `HighContrastCustom`-only `ThemeDictionary` is unreachable.
- **Fix (gated on A3-4):** port the variant classification, thread it through `FrameworkTheming`/`Themes`,
  add the `HighContrastCustom` key, and switch the app-wide branch on `GetHighContrastTheme()` 1:1 with
  `Resources.cpp:740-753`. Until then the generic fallback keeps the common cases correct.

### A3-4 — `GetSystemColor` is a constant stub; `RebuildColorAndBrushResources` output is dead · **Low · FU · (no verdict)**
- `SystemThemingInterop.cs:71-81` returns a fixed `0xFFAABBCC`. `FrameworkTheming.RebuildColorAndBrushResources`
  runs every `OnThemeChanged` but `GetColorAndBrushResourceInfoList` has **zero consumers**
  (`CoreServices.NotifyThemeChange` deliberately skips `UpdateColorAndBrushResources` — Uno's palette comes from
  generated resources). **Decide explicitly:** either implement the real OS palette + consume the list, or prune
  the dead faithful port. Blocks A3-3.

### A3-5 — `ThemingHelper.GetRootVisualBackground` bypasses `FrameworkTheming`, ignores HC · **Low · FU · (no verdict)**
- WinUI's `GetHwndBackground` special-cases HC (root = system WINDOW color). `ThemingHelper.cs:7-9`
  independently maps `RequestedTheme` Light→White/Dark→Black with no HC branch; the ported
  `FrameworkTheming.GetRootVisualBackground` is **never called**. Reconcile **after A3-4** (delegating now is
  worse — the ported HC branch would return the `GetSystemColor` placeholder).

### Confirmed-correct in this area (no action)
- **A3-1 (OK, justified ALC divergence):** `FrameworkTheming` is process-shared (one per singleton
  `CoreServices`), not per-`CCoreServices`. This **is** the ALC foundation: secondary apps are pinned
  element-level rather than mutating shared theming. Revisit only if `CoreServices` becomes per-content-root.
  (Stale citation: `Application.Alc.cs:176` says `corep.h:2207`; actual is `corep.h:2144-2145`/accessor `1355`.)
- **A3-6 (OK):** `Application.RequestedTheme` pre-load-only semantics + OS-change suppression faithfully ported;
  `ClearRequestedTheme` is a justified Uno-only extension (no `ApplicationTheme.None`).
- **A3-7 (OK):** `FrameworkTheming` state machine, `GetTheme` composition, change flags, and the `Theme` enum are
  line-accurate.

---

## D. CThemeResource + providing-dictionary pin (A4)

### A4-2 / A7-2 / A8-4 — App-override / `LookupThemeResource` route to host `Application.Current`, not the resolving ALC's app · **Low · FU · A4-2 needs-verification, A7-2 confirmed-as-stated**
- **WinUI:** `GetKeyOverrideFromApplicationResourcesNoRef` is **inside** the theme-dictionary leaf and uses the
  resolving **core's** app dictionary (`Resources.cpp:644-685,906-939`); the per-core model makes that the right
  app.
- **Uno:** the override is applied only in `ThemeResourceReference.RefreshValue` (`:159-181`, gated on
  `IsGlobalThemeDictionaries`), and `TryApplicationResourceOverride` (`ResourceResolver.cs:780-792`) and
  `LookupThemeResource`/`ResolveTopLevelResource` consult **host** `Application.Current` only — context-less
  lookups (Hyperlink brushes, default text foreground) can't identify the resolving ALC, so a secondary app's
  override of a system/theme key is missed for its own content (host value leaks in; host not corrupted).
  Widened by the new per-keyframe override path (`044ebd3f49`/`6cac205035`).
- **Fix:** thread the owning `Application`/ALC into `TryApplicationResourceOverride` and `LookupThemeResource`,
  mirroring `GetApplicationResourceDictionary`. Low (system/Fluent keys rarely overridden per-app).

### A4-1 — `GetDictionaryForThemeReference` canonicalization has no Uno analog · **Low · FU · (no verdict)**
- WinUI canonicalizes the pinned dict (system-colors→self, global-theme→`GetThemeResources()` root,
  app→app-resources) (`ResourceResolver.cpp:947-986`); Uno pins the **raw** owning dict
  (`ResourceResolver.cs:661-681`, `ResourceDictionary.cs:379-444`). Equivalent for the dominant
  owner-of-`ThemeDictionaries` case (the dangerous "pin a Light/Dark sub-dict" trap is **correctly avoided**);
  edge gap for a merged-child-of-global-theme and app-resources canonicalization. **Fix:** add a canonicalization
  step after the providing-dictionary capture.

### A4-4 / A8-3 — `IsGlobal` marking is broader than `MarkAllIsGlobal`, lazy, and not propagated to children · **Low · FU · (no verdict)**
- WinUI marks only the one global theme-resources tree and propagates `IsGlobal` recursively into merged/theme
  children (`xcpcore.cpp:7765`, `Resources.cpp:2160-2181`). Uno's generator marks **every** top-level Uno.UI/Fluent
  dict and **does not propagate** (`XamlFileGenerator.cs`, `ResourceDictionary.cs:186`), and infers
  `IsGlobalThemeDictionaries` from the **lazily-allocated** `_themeDictionaries is not null` — which `IsEmpty`/the
  public `ThemeDictionaries` getter can flip on as a side effect (A8-3). **Fix:** emit a fixed construction-time
  `IsThemeDictionariesOwner` flag and propagate `IsGlobal` to children (or scope it to the genuine global root).

### Confirmed-correct in this area (no action)
- **A4 / A4-6 / A5-1 (OK):** **R1 providing-dictionary pinning is genuinely shipped and correct** —
  `ApplyResource` constructs the `ThemeResourceReference` with a **real** `targetDictionary`, pinning the dict
  that *owns* the `ThemeDictionaries` (not a Light/Dark sub-dict). `IsValueFromInitialTheme` (R5) and the
  re-query-pinned-dict + last-resolved fallback (R4) are faithful. The five popup repros exist, gated only on
  Native (justified). The "parameter vs slot" hybrid is **coupled, not competing**.
- **A4-3 (OK):** `GetParentFollowPopups` hop (R2) is absent from the resource/inheritance walks, deliberately
  substituted by the parse-time pin — covers the shipped repros. Port only if a future repro needs live-walk
  opener resolution.
- **A4-5 / A4-7 (OK):** the slot↔cache "TODO" is benign; the process-global slot/cache is safe (per-reference
  pin + reference-identity cache keys).
- **A4-8 (CB, doc):** stale test comment `Given_ThemeResource.cs:419` references a removed
  `GetThemeResolutionParent`/`Store.Parent` walk — update it.
- **A4-9 (FU, policy):** `Given_MenuFlyout.cs:550` / `Given_ToolTip.cs` embed `kahua-private` URLs in
  checked-in tests — violates AGENTS.md's one-way public/private rule. Replace with public issue numbers.

---

## E. EnsureActiveThemeDictionary / slot reader / walk cache (A5)

### A9-3 / A5-2 — Core slot not coupled to `ThemeWalkResourceCache.SetSubTreeTheme` · **Low · FU · refined (behavior-neutral)**
- WinUI couples slot→cache at the **write** site (`xcpcore.cpp:7901-7906` `SetSubTreeTheme`); Uno couples at the
  **read** site (`RefreshValue` threads `GetActiveBaseTheme()` — itself a slot read — into the cache key).
  Verified: the cache **cannot** resolve under the wrong theme today (single caller, same slot-derived theme for
  both key and dictionary selection). **Fix (optional 1:1 parity):** port `SetSubTreeTheme` so the cache reads
  the slot rather than a per-call parameter, removing the second mechanism. Fix stale citation in
  `CoreServices.Theming.cs:38` (says `xcpcore.cpp:8132`; correct is `7905`).

### A9-2 / A5-5 — `Themes.Active` / `GetActiveBaseTheme` / `SetActiveTheme` retained (DoD said delete) · **Low · FU · refined**
- On enhanced-lifecycle, `GetActiveTheme` reads the slot/`FrameworkTheming`; `Themes.Active`
  (`ResourceDictionary.cs:1415,1355-1356,1317,1320`) is only the **no-CoreServices fallback + native
  source-of-truth + coherence mirror** (written by `Application.cs:303`). It was **not** deleted, and unit tests
  (`Given_ThemeResolution.cs:59,71,83,101`) still drive `SetActiveTheme`. **ALC note:** it's a process/ALC-static
  — the one remaining process-global theme value (low risk; slot path wins when a core exists). **Fix:** keep as
  the documented native/no-core mirror but **amend the DoD** to stop claiming deletion; port the unit tests to
  seed `FrameworkTheming`/`ScopeRequestedThemeForSubTree` (as `When_Owner_Null_Follows_Active_Theme` already does).

### Confirmed-correct in this area (no action)
- **A5-1 (OK):** the leaf reads the core slot, not a threaded parameter — the brief's central hypothesis is
  **disproven**; the parameter-threaded leaf is already retired.
- **A5-3 (OK):** the missing `m_theme != None` push gate yields an identical resolved value (only the cache-key
  *shape* differs for unthemed Enter — no correctness impact).
- **A5-7/A5-8/A5-9 (OK):** Default-fallback order + HC-branch-first, the newly-active sub-dict
  `NotifyThemeChanged`, and by-key cache invalidation on Add/Remove are all faithful (weak-ref cache values).

---

## F. FE HC rules / ActualThemeChanged / freeze (A6)

### A6-5 — Popup overlay not re-themed to child theme on a runtime theme change · **Low · FU · (no verdict)**
- WinUI re-themes the light-dismiss overlay to `child->GetTheme()` (`popup.cpp:3671-3677`); Uno relies on the
  `PopupPanel` background re-resolving `LightDismissOverlayBackground` against the popup's own theme
  (`Popup.cs:175-177` TODO). Diverges only when the child carries a `RequestedTheme` distinct from the popup.

### A6-6 — `IThemeChangeAware.OnThemeChanged` can fire twice per walk · **Low · FU · (no verdict)**
- `NotifyThemeChangedForInheritedProperties` calls `themeAware.OnThemeChanged()` up-front
  (`FrameworkElement.Theming.cs:479-485`) and the child walk calls it again
  (`DependencyObjectStore.Theming.cs:634-638`). Idempotent today (redundant work + latent risk). **Fix:** fire
  once via the `UpdateChildResourceBindings` path (WinUI's `NotifyThemeChangedForInheritedProperties` is
  foreground-freeze-only).

### Confirmed-correct in this area (no action)
- **A6-1 (OK):** `ActualThemeChanged` raised **async** (posted via `NativeDispatcher.Main`, `51de345c4d`);
  handler observes the **new** theme (`_theme` persisted after `NotifyThemeChangedCore`).
- **A6-2 / A9-6 (OK):** `HighContrastChanged` added and correctly **internal** — WinUI's is a non-public core
  event, so the alignment plan's "public API" instruction was the error, not the code. (Currently unreachable —
  see A3-2.)
- **A6-4 (OK):** foreground freeze resolves against the owner theme (slot-push + `LookupThemeResource`) and
  reaches popup content (R7).
- **A6-7 (OK, justified):** the Uno-specific foreground re-pull in the `NotifyThemeChangedCore` else-branch
  compensates for Foreground-as-inherited-DP (no WinUI TextFormatting pull system).
- **A6-8 (OK, ALC-safe):** out-of-tree listeners via a weak-keyed `ConditionalWeakTable`, gated on
  `!IsActiveInVisualTree`.
- **A6-9 (OK):** `ActualTheme` base-theme-with-app-fallback is 1:1.

---

## G. ALC theming preservation (A7)

### A7-1 — Native secondary-ALC app's explicit `RequestedTheme` is silently dropped · ~~Medium · CB~~ → **non-issue (verified) · OK**
- **Original concern:** the unconditional `if (_isSecondaryAlcApplication) { SetAlcRequestedTheme(...); return; }`
  at the top of `SetExplicitRequestedTheme` (`Application.cs:328`, **outside** `#if UNO_HAS_ENHANCED_LIFECYCLE`)
  diverts native secondary apps to `SetAlcRequestedTheme`, whose element pin is `#if __SKIA__ || __WASM__`-gated
  (`Application.Alc.cs:203-213`, skipped on native) and whose value the native getters never read → apparent no-op.
- **Resolution (2026-06-18, follow-up verification — the original A7-1 verdict was rate-limited):** **not a real
  regression.** `Application.Alc.cs:203-205` states it explicitly: *"ALC app hosting only exists on Skia and WASM
  (see ExitAlcApplication); on native platforms Window maps to the native window type which doesn't have the ALC
  partial."* A secondary-ALC app is never **hosted** on native (no window/content), so there is no native subtree
  to theme and the "dropped" theme has nothing to apply to. Touching the native path here would risk the
  maintenance-only native build for an unreachable scenario, contradicting the "native behavior unchanged" scope.
  **No code change.** (If native ALC hosting is ever added, restore the `#else` apply path then.)

### A7-3 — Secondary app bootstrap runs `InitializeSystemTheme → OnThemeChanged` on the shared FrameworkTheming · **Low · FU · (no verdict)**
- `Application.skia.cs:149-151` calls `InitializeSystemTheme()` unconditionally for secondary apps →
  `OnThemeChanged()` on the **host-owned** FrameworkTheming; if the OS theme changed since host start, the
  secondary app's bootstrap drives a host-wide walk. Benign (no-op in steady state) but an entanglement. **Fix:**
  skip/short-circuit `InitializeSystemTheme` for `_isSecondaryAlcApplication`.

### Confirmed-correct in this area (no action)
- **A7-4 (OK, the ALC mechanism):** per-instance `_alcRequestedTheme` pinned element-level on the
  `AlcContentHost` boundary; `GetEffectiveWalkTheme`/`GetOwningApplication` keep walks app-scoped; verified by
  `Given_AlcContentHost.When_SecondaryAlcApp_ExplicitTheme_Then_IndependentFromHost`.
- **A7-5 (OK):** the cache "not coupled to slot" TODO is benign and arguably **safer** for multi-ALC islands.
- **A7-6 (OK):** collectible-ALC safety sound (weak-keyed CWT + `CleanupNonDefaultAlcCaches` +
  `ResourceResolver` per-ALC registry teardown).
- **A7-7 (OK):** `5f3bc58f7a` host-dictionary restore-on-release interacts correctly with the R1 pin.
- **A7-8 (OK):** `1b6eff515d` override-before-cache ordering in `SystemThemeHelper` is correct/ALC-neutral.

---

## H. Freshest #23472 fixes (A8) — three root-cause, one band-aid

- **A8-5 (OK, root-cause):** `bd89410340` `ColorAnimationUsingKeyFrames.OnThemeChanged` re-applies the recomputed
  value while live — mirrors `CAnimation::NotifyThemeChangedCore → RequestTickForPendingThemeChange`
  (`animation.cpp:1030-1049`).
- **A8-6 (OK, root-cause):** `044ebd3f49` establishes `_theme` on the lazy VSM chain via `Store.NotifyThemeChanged`
  (the real Enter primitive), dropping the Begin-time band-aid. (Theme-source edge → A1-5.)
- **A2-5 (OK):** `6cac205035` `IsGlobalThemeDictionaries` gating is a parity improvement. (Edges → A4-4/A8-3.)
- **A8-1/A8-2 (band-aid):** `5e14bb64ad`/`bcc8191375` — see A2-4.
- **A8-7 (FU, program-level):** all four code fixes thread `ResolveOwnerTheme`, adding dependents to the
  parameter path the DoD wants deleted — sequence the slot-only end-state before removing it.
- **A8-8 (OK):** the test commits are sound; `54b2844f70` re-enables a NativeWinUI test only after validating
  against a real WinUI 3 app (oracle discipline). `5e14bb64ad`'s red/green is genuinely diagnostic.

---

## I. Definition-of-done reconciliation (A9)

- **A9-5 (CB, doc):** the alignment README's "**ALL PHASES (0–8) COMPLETE**" is behaviorally true but
  structurally false against its own DoD (ThemeResolution.cs and Themes.Active not deleted; slot↔cache coupling
  and HC runtime toggle open). Update the status to the hybrid reality and link the open items.
- **A9-8 (FU/clarify):** the DoD said `SetThemeResourceBinding` should "take back" initial-value resolution from
  `ResourceResolver.ApplyResource`; the shipped split (`ApplyResource` ≈ `ThemeResourceExtension::LookupResource`,
  `SetThemeResourceBinding` ≈ `Theming.cpp:349-400`) is **arguably more WinUI-faithful**. Confirm the chosen shape
  and amend the plan, or fold the two remaining `TODO Uno` setter paths (`ApplyThemeResource`,
  `ApplyVisualStateSetter`) into the `SetThemeResourceBinding` sequence.
- **A9-9 (OK):** the remaining `TODO Uno: NOT PORTED` cluster (namescope, sparse-property enter, ETW, OS HC
  variant, color/brush/text core resets) is correctly classified no-analog — only `ResourceDictionary.cs:783`
  (HC variant) has a latent behavioral edge (tracked as A3-3/A5-6).
