# Architecture — WinUI Theming & Enter/Leave, and the 1:1 Port Mapping

> WinUI sources: `D:\Work\microsoft-ui-xaml2\src`, commit `fc2f82117`. All `file:line` refs below are against that commit. Uno refs are against this branch's base, `origin/dev/mazi/theming-winui` (`ffd6ee2631`).

## 0. Fidelity rules (binding for every phase)

These are the `/winui-port` rules as they apply to this subsystem; they are what the prior effort relaxed.

- **One C++ translation unit → one C# file**, with a `MUX Reference <file>, commit fc2f82117` header. Member order matches the C++ source. C++ comments are preserved verbatim (adapted syntax only).
- **Natural WinUI names.** No `_Mux`/`Internal` suffixes for their own sake. Where a new method must coexist with an old Uno path during migration, prefer an internal overload; gate superseded code with `#if` rather than renaming.
- **Never simplify away behavior.** Anything not portable is preserved as a comment with `TODO Uno:` and a reason. Behavior that *looks* redundant (the `Default` fallback order, the `None`-theme gates, async event posting) is ported as-is.
- **Code goes where WinUI has it.** `CDependencyObject` logic goes to the DependencyObject level (in Uno: `DependencyObjectStore`, see §5.1) — not to `UIElement`/`FrameworkElement`. `CFrameworkElement` overrides stay on `FrameworkElement`. Core-services state goes to `Uno.UI.Xaml.Core.CoreServices`.
- **New machinery is enhanced-lifecycle only** (`UNO_HAS_ENHANCED_LIFECYCLE`: Skia + WASM). Native heads keep their current behavior compiled and unchanged.

## 1. The WinUI lifecycle model (what we port)

### 1.1 EnterParams / LeaveParams

`xcp/core/inc/EnterParams.h:13-53` and `LeaveParams.h:12-50`:

| EnterParams | LeaveParams | Meaning |
|---|---|---|
| `fIsLive` | `fIsLive` | entering/leaving the *live* tree (vs dead enter for namescope only) |
| `fSkipNameRegistration` | `fSkipNameRegistration` | skip namescope (un)registration |
| `fCoercedIsEnabled` | `fCoercedIsEnabled` | inherited IsEnabled coercion |
| `fUseLayoutRounding` | `fUseLayoutRounding` | inherited layout rounding |
| `fIsForKeyboardAccelerator` | `fIsForKeyboardAccelerator` | dead enter to register accelerators |
| `fCheckForResourceOverrides` | `fCheckForResourceOverrides` | resource-override scope flag |
| `pParentResourceDictionary` | `pParentResourceDictionary` | parent RD pointer (reset for descendants) |
| `visualTree` | — | VisualTree being entered |
| — | `fVisualTreeBeingReset` | tree teardown: suppress events |

The base has only `IsLive`, `IsForKeyboardAccelerator`, `VisualTree` in both structs (`src/Uno.UI/UI/Xaml/EnterParams.cs`, `LeaveParams.cs`). Port the full structs.

### 1.2 CDependencyObject::Enter / Leave (`core/core/elements/depends.cpp`)

- **`Enter` (depends.cpp:778-969):** cycle guard via `fIsProcessingEnterLeave`; XamlIslandRoot visual-tree override; `SetVisualTree` when live; inherited-properties invalidation; namescope-owner adjustment (standard namescope owners, popup dual-namescope case); then `EnterImpl(pAdjustedNamescopeOwner, enterParams)` with `pParentResourceDictionary` reset for descendants.
- **`EnterImpl` (depends.cpp:971-1072):** the base layer every DO runs:
  1. live: `ActivateImpl()` (sets `fLive`), `m_checkForResourceOverrides` (:976-984)
  2. name registration (:986-1001), deferred-mapping notify
  3. skip value types / templates (:1004-1011)
  4. **enter-property walk** (:1013-1032): for each `CEnterDependencyProperty` of the class (metadata-flagged; `DoNotEnterLeave`, `IsObjectProperty`, `NeedsInvoke`), get the DO-valued property and `EnterObjectProperty(pDO, pNamescopeOwner, params)` — this is how brushes, transforms, setters, keyframes, text elements, resources etc. enter the tree
  5. `EnterSparseProperties` (:1034)
  6. `NotifyInheritanceContextChanged` when live (:1036-1042)
  7. **the theme block** (:1044-1069) — quoted because it is the heart of this whole effort:
     ```cpp
     if (m_bitFields.fLive)
     {
         // If our theme is different from the parent, make sure we walk the subtree.
         CDependencyObject* pParent = nullptr;
         auto thisAsFe = do_pointer_cast<CFrameworkElement>(this);
         if (thisAsFe)
         {
             // Get logical parent so popups and flyouts inherit theme changes
             pParent = GetInheritanceParentInternal(TRUE /* fLogicalParent */);
         }
         else
         {
             pParent = GetParentInternal(false /* public */);
         }
         if (pParent && pParent->GetTheme() != Theming::Theme::None && pParent->GetTheme() != m_theme)
         {
             IFC_RETURN(NotifyThemeChanged(pParent->GetTheme()));
         }
         else
         {
             IFC_RETURN(UpdateAllThemeReferences());
         }
     }
     ```
- **`Leave` (depends.cpp:1079-1237)** mirrors Enter (cycle guard, `bAdjustedLive = fIsLive && IsActive()`, namescope, popup cached-namescope case, `LeaveImpl`).
- **`LeaveImpl` (depends.cpp:1256-1342):** InheritanceContextChanged; `DeactivateImpl()`; name unregistration; leave-property walk (`LeaveObjectProperty`); `LeaveSparseProperties`; focus/input cleanup. **`m_theme` is NOT cleared on Leave** — theme persists; re-`Enter` re-establishes it from the new parent.
- **`m_theme`** is `Theming::Theme m_theme : 5` on `CDependencyObject` (`CDependencyObject.h:1759-1764`), getter at `:1648`; `fIsProcessingThemeWalk : 1` at `:302`. Every DO, not just UIElements.
- **Overrides:** `CUIElement::EnterImpl` (`uielement.cpp:1252-1400`; calls base at `:1356`, enters children `m_pChildren->Enter` at `:1373`, ContextFlyout at `:1367`), `CFrameworkElement::EnterImpl` (`framework.cpp:2337-2406`; resource-override check, base, style application when live), `CControl::EnterImpl` (`Control.cpp:321-362`; built-in styles + StateTriggers when live), `CPopup::EnterImpl` (`popup.cpp:2846-2880`; base, composition, then **dead enter of the child** `:2876` with `fIsLive=FALSE` for name registration only — the child *live*-enters when the popup opens and parents it to PopupRoot), and matching `LeaveImpl`s.
- **`GetInheritanceParentInternal(fLogicalParent)`** (`framework.cpp:3113-3146`): visual parent by default; the logical parent (`m_pLogicalParent` when the `FrameworkElement_Parent` slot is set) when `fLogicalParent` is requested **or** the visual parent is `PopupRoot` **or** `this` is a parentless `Popup`; falls back to the visual parent. Base `CDependencyObject` version (`CDependencyObject.h:586-592`) returns `m_pParent` unless parent-is-inheritance-context-only.

### 1.3 Who triggers Enter

Children collections (`m_pChildren->Enter`), DO-valued property sets on live objects (the enter-property walk + `SetValue` entering new values), popup open, root content set by `CCoreServices`. In Uno these correspond to: `UIElement.ChildEnter` (`UIElement.crossruntime.cs`), `DependencyObjectStore.SetValue` parenting, popup open, and `VisualTree` root attach.

## 2. The WinUI theming model (what we port)

### 2.1 Theme enum — `components/theming/inc/Theme.h:10-65`

5-bit packed: `BaseMask=0x03` (None/Light/Dark), `HighContrastMask=0x1C` (HC/White/Black/Custom), helpers `GetBaseValue`/`GetHighContrastValue`. **Uno's `src/Uno.UI/UI/Xaml/Theming.cs` already matches** (verify against fc2f82117 and re-cite).

### 2.2 FrameworkTheming — `components/theming/FrameworkTheming.{h,cpp}`

The app/system theme state machine, owned by `CCoreServices` (`m_spTheming`):
- State: `m_requestedTheme` (app override; `None` = follow system), `m_systemTheme`, `m_highContrastTheme`, change-in-progress flags `m_isAppThemeChanging` / `m_isSystemThemeChanging` / `m_isHighContrastChanging`.
- `GetTheme()` (FrameworkTheming.cpp:119-124): `(m_requestedTheme != None ? m_requestedTheme : m_systemTheme) | m_highContrastTheme`. `GetBaseTheme()`/`GetHighContrastTheme()` mask accordingly.
- `OnThemeChanged(forceUpdate)` (:31-67): re-reads system + HC from `IThemingInterop`, computes which axes changed, sets the changing flags, invokes the notify callback (→ `CCoreServices::NotifyThemeChange`), clears flags.
- `SetRequestedTheme` (:75-105): app override; triggers `OnThemeChanged`.
- Uno today scatters this across `Application.cs` (`InternalRequestedTheme`, `InitializeSystemTheme`, `UpdateRequestedThemesForResources`, `IsThemeSetExplicitly`, `OnSystemThemeChanged`) plus the `Themes.Active` global. Port `FrameworkTheming` as a real class; `Application` delegates (per `FrameworkApplication_Partial.cpp:987-1061` — settable only pre-load, getter reads `FrameworkTheming.GetBaseTheme()`).

### 2.3 SystemThemingInterop — `components/theminginterop/SystemThemingInterop.cpp`

`GetSystemTheme` (:33-119, OS light/dark), `GetSystemHighContrastTheme` (:121-177, maps OS HC scheme → White/Black/Custom). Uno equivalent: `Uno.UWP/Helpers/Theming/SystemThemeHelper.*` (per-platform) + `WinRTFeatureConfiguration.Accessibility.HighContrast` flag. Port an `IThemingInterop`-shaped bridge so `FrameworkTheming` consumes the same contract; real OS HC detection on Skia stays a documented follow-up (variant classification ports now, sourced from the flag).

### 2.4 CDependencyObject theming — `components/DependencyObject/Theming.cpp` (345 lines)

Methods in file order (all on CDependencyObject):
- `NotifyThemeChanged(theme, forceRefresh)` (:110-157): assert; skip if `IsProcessingThemeWalk`; **FE requested-theme override read here via `do_pointer_cast<CFrameworkElement>`** (:125-129); no-change early-out (:132-135); set walk flag + **scoped save/set of core `RequestedThemeForSubTree` from the incoming theme** (:137-149); `NotifyThemeChangedCore`; persist `m_theme` (:155).
- `NotifyThemeChangedCore` (:159-162) → `NotifyThemeChangedCoreImpl` (:166-255): `UpdateAllThemeReferences()`; walk class properties + sparse properties calling `NotifyThemeChanged` on DO values; notify framework peer (expressions/bindings) via `FxCallbacks`.
- `UpdateAllThemeReferences` (:260-286): snapshot the theme-resource map's property indices (stack_vector, 50), `UpdateThemeReference(propertyIndex)` each.
- `UpdateThemeReference(KnownPropertyIndex)` (:288-311): `ClearThemeResource` → re-`SetThemeResourceBinding`.
- `UpdateThemeReference(CThemeResource*)` (:315-346): if `IsActive()`, ancestor walk via `ResourceResolver::FindNextResolvedValueNoRef` → `SetLastResolvedValue`; else/fallback `RefreshValue()` gated by `IsProcessingThemeWalk() || m_theme != None || !IsValueFromInitialTheme()`.
- `SetThemeResourceBinding(pDP, pModifiedValue, pThemeResource, baseValueSource)` (:349-400): **the owner-theme push** —
  ```cpp
  // Push theme that resource lookup should use to get the property value
  if (!IsProcessingThemeWalk() && m_theme != Theme::None)
  {
      prevTheme = GetRequestedThemeForSubTreeFromCore();
      if (prevTheme != Theming::GetBaseValue(m_theme))
      {
          SetRequestedThemeForSubTreeOnCore(m_theme);
          popTheme = true;
      }
  }
  // … UpdateThemeReference(pThemeResource); GetLastResolvedThemeValue; SetValue; SetThemeResource(pDP, pThemeResource);
  ```
  then scope-restores the previous value.

The base's `DependencyObjectStore.Theming.cs` ports parts of this but (a) splits `SetThemeResourceBinding` responsibilities with `ResourceResolver.ApplyResource`, (b) leaves `NotifyThemeChanged`/`Core` on **FrameworkElement** (`FrameworkElement.Theming.cs:164/214`), and (c) threads `ThemeResolution.ResolveOwnerTheme(owner)` as a parameter instead of the core slot.

### 2.5 The ambient slot — the key structural insight

WinUI **has** a process(core)-global ambient: `CCoreServices::SetRequestedThemeForSubTree` / `GetRequestedThemeForSubTree` / `IsThemeRequestedForSubTree`. It is a **single value with scoped save/restore** (recursion forms the implicit stack), and it is written from **exactly three places**, always derived from a DO's `m_theme` or the walk theme:

1. `CDependencyObject::NotifyThemeChanged` (Theming.cpp:137-149) — during the walk.
2. `CDependencyObject::SetThemeResourceBinding` (Theming.cpp:368-375) — when (re)binding one theme resource outside a walk.
3. `CFrameworkElement::NotifyThemeChangedForInheritedProperties` (framework.cpp:3441-3487) — inherited-property (foreground) resolution.

The reader is `CResourceDictionary::EnsureActiveThemeDictionary` (Resources.cpp:699-791). Because `m_theme` is established for *every* DO at `Enter`, every materialization path (template expansion, VSM, animations, bindings, styles, popups) hits one of the three writers with the right theme — **that** is why WinUI needs no per-site band-aids and no resolution-time ancestor walk. The base's `ThemeResolution.ResolveOwnerTheme` ancestor walk and the theme-parameter threading through `TryGetValue` are reconstructions of this invariant from the wrong end; they are replaced by the real mechanism (theme at Enter + the three writers + the slot reader).

### 2.6 CThemeResource — `components/theming/ThemeResource.{h,cpp}`

Members: `m_strResourceKey`, `m_isValueFromInitialTheme`, `m_lastResolvedThemeValue`, `m_pTargetDictionaryWeakRef`, `m_themeWalkResourceCache`. `RefreshValue()` (:63-129): cache probe → pinned-dictionary `GetKeyNoRef` → cache add → `SetLastResolvedValue` (:138-169, unwraps wrappers, maintains `m_isValueFromInitialTheme`). Uno counterpart: `src/Uno.UI/UI/Xaml/Data/ThemeResourceReference.cs` — already close (prior effort); align members/order/semantics fully.

### 2.7 CThemeResourceExtension — `core/core/elements/ThemeResourceExtension.cpp`

Parse-time `{ThemeResource}`: `ProvideValue` (:44-132) / `LookupResource` (:144-209) → `ResolveInitialValueAndTargetDictionary` (:222-247) → `ResourceResolver::ResolveThemeResource` (`components/resources/ResourceResolver.cpp:83-109`), pinning the providing dictionary. Uno counterpart: `ResourceResolver.ApplyResource`/`TryStaticRetrieval` (`src/Uno.UI/UI/Xaml/ResourceResolver.cs`) — align the pinning + registration flow.

### 2.8 CResourceDictionary theme selection — `core/core/elements/Resources.cpp`

`EnsureActiveThemeDictionary` (:687-819): recompute when base/requested/HC changed; **HC branch first** (:718-762: requested-subtree Light→`HighContrastWhite` / Dark→`HighContrastBlack`; app-wide → OS variant key; generic `"HighContrast"` fallback; `MarkIsHighContrast`), then base (requested-subtree theme if set, else `FrameworkTheming.GetBaseTheme()`; `"Light"`/`"Dark"` key; `"Default"` fallback), cache as `m_pActiveThemeDictionary` + `m_activeTheme`, and **`NotifyThemeChanged(m_activeTheme, highContrastChanged)` on the newly-active sub-dictionary when switching** (:793-815). The base has a partial port (`ResourceDictionary.ResolveActiveThemeDictionary`, `ResourceDictionary.cs:644-686`) **parameterized by theme key** — finish it 1:1 and source the theme from the core slot.

### 2.9 ThemeWalkResourceCache — `components/theming/ThemeWalkResourceCache.{h,cpp}`

Key = `(CResourceDictionary*, Theming::Theme /*subtree theme*/, key)` (`ThemeWalkResourceCache.h:55`); `m_subTreeTheme` set by `CCoreServices::SetRequestedThemeForSubTree` (xcpcore.cpp:7903-7905); active only during walks (`BeginCachingThemeResources` scope, invoked from `CCoreServices::NotifyThemeChange` xcpcore.cpp:8015). Owned by `CCoreServices`. The base has `src/Uno.UI/UI/Xaml/ThemeWalkResourceCache.cs` — align ownership (CoreServices) and the `SetSubTreeTheme` coupling to the slot writers.

### 2.10 CFrameworkElement theming — `core/core/elements/framework.cpp`

- `OnRequestedThemeChanged` (:3501-3559): map `ElementTheme`; `Default` → parent theme else app base, plus *un*freeze inherited properties; set core `IsSwitchingTheme`; OR in HC; `NotifyThemeChanged(theme)`.
- `NotifyThemeChangedCore` override (:~3327): base, then `RaiseActiveThemeChangedEventIfChanging(theme)`.
- `RaiseActiveThemeChangedEventIfChanging` (:3346-3386): raise `HighContrastChanged` when HC changing; suppress `ActualThemeChanged` while entering/staying HC; raise `ActualThemeChanged` when base changes (or first-walk `None`→ with base changing). **Events are raised via the EventManager = posted async** (handlers observe the NEW theme; confirmed by native `ResourceDictionaryBasicTests.cpp:4209-4234`).
- `NotifyThemeChangedForInheritedProperties` (:3402-3489): foreground freeze/unfreeze with the third ambient set-point.
- `ActualTheme` (:3969-3994): `GetBaseValue(GetTheme())`, falling back to `FrameworkTheming.GetBaseTheme()` when `None`.
- Uno counterpart: `FrameworkElement.Theming.cs` (hosts the whole walk today; no `HighContrastChanged` event).

### 2.11 CCoreServices::NotifyThemeChange — `core/dll/xcpcore.cpp:8006-8118`

Orchestrates a full theme change: begin walk-cache scope; update color/brush resources; reset themed brushes/text formatting; set `m_fIsSwitchingTheme`; `NotifyThemeChanged(theme, fForceRefresh=true)` on: global theme resources, app resources, **main popup root**, the visual root (plus `NotifyThemeChangedForInheritedProperties` with `freeze…=true`), XAML island roots; reload theme-variant images; notify out-of-tree listeners. Uno counterpart: scattered in `Application.cs` — port to `Uno.UI.Xaml.Core.CoreServices`.

## 3. Base-branch inventory (`ffd6ee2631`): kept vs replaced

| Area | File(s) | Status |
|---|---|---|
| Theme enum | `UI/Xaml/Theming.cs` | **Keep** (verify vs fc2f82117, re-cite) |
| Per-DO theme on the store | `DependencyObjectStore` `_theme` + `GetTheme/SetTheme` | **Keep** (foundation for Phase 1) |
| Regression tests + helpers | `Uno.UI.RuntimeTests/.../Given_Theme_Materialization.cs`, `Helpers/ThemeHelper.cs` (incl. `UseSystemThemeOverride`), OS-independent suite fixes | **Keep** — the safety net; must stay green through every phase |
| Band-aid deletion | no `PushRequestedThemeForSubTree`, no stack (incl. reverts of merged band-aids) | **Keep** |
| `ThemeResourceReference`, `ThemeWalkResourceCache` | `UI/Xaml/Data/ThemeResourceReference.cs`, `UI/Xaml/ThemeWalkResourceCache.cs` | **Keep as starting point**; finish 1:1 alignment (Phases 2–3) |
| Partial `EnsureActiveThemeDictionary` | `ResourceDictionary.ResolveActiveThemeDictionary` (`ResourceDictionary.cs:644-686`) | **Keep as starting point**; finish 1:1, re-source theme from the core slot (Phase 5) |
| `ThemeResolution.ResolveOwnerTheme` | `UI/Xaml/ThemeResolution.cs` | **Replace** — resolution-time ancestor walk has no WinUI analog; deleted in Phase 3 after the choke-point assert proves slot equivalence |
| Theme-parameter-threaded leaf | `ResourceDictionary.TryGetValue(key, themeKey, …)` overloads + callers | **Replace** — leaf reads the core slot like `EnsureActiveThemeDictionary` (Phases 3+5) |
| `EstablishThemeAtEnter` + property-value theme walk | `DependencyObjectStore.Theming.cs:95-330` | **Replace** — subsumed by the real `EnterImpl` enter-property walk + theme block (Phase 1) |
| FE-level theme walk | `FrameworkElement.Theming.cs:164-341` (`NotifyThemeChanged`/`Core`, push-free but FE-hosted) | **Replace** — moves to the store per Theming.cpp (Phase 3); FE keeps only its overrides (Phase 4) |
| `Themes.Active` global (+ `GetActiveBaseTheme`, test-only `SetActiveTheme`) | `ResourceDictionary.cs:1078-1111` | **Replace** — `FrameworkTheming.GetBaseTheme()` (Phases 2+6) |
| App/OS theming in `Application.cs`; `ApplicationHelper.RequestedCustomTheme` | `UI/Xaml/Application.cs`, `ApplicationHelper.cs` | **Replace** — `FrameworkTheming` + `FrameworkApplication` parity (Phases 2+6) |
| UIElement-level Enter/Leave | `UI/Xaml/UIElement.mux.cs` (`Enter:855`, `EnterImpl`, `DependencyObject_EnterImpl:1028` with property walk + theme block as comments), `EnterParams.cs`/`LeaveParams.cs` (3 fields each) | **Replace/extend** — the CDependencyObject layer moves to the store with full params (Phase 1) |
| Popup/Flyout theming | `Controls/Popup/*`, `Controls/Flyout/FlyoutBase.cs` (`Enter/Leave:82-95`, `ForwardThemeToPresenter`) | **Align** (Phase 7) |

## 4. C++ → C# port mapping table

| WinUI C++ (commit fc2f82117) | C# output (location) | Phase |
|---|---|---|
| `core/inc/EnterParams.h` | `src/Uno.UI/UI/Xaml/EnterParams.cs` (extend, 1:1 fields) | 1 |
| `core/inc/LeaveParams.h` | `src/Uno.UI/UI/Xaml/LeaveParams.cs` (extend, 1:1 fields) | 1 |
| `core/core/elements/depends.cpp` (Enter/Leave/EnterImpl/LeaveImpl/EnterObjectProperty/LeaveObjectProperty/Enter-LeaveSparseProperties only) | `src/Uno.UI/UI/Xaml/DependencyObjectStore.mux.cs` (new partial; rest of depends.cpp explicitly `NOT PORTED` noted) | 1 |
| `CDependencyObject.h` lifecycle bits (`fIsProcessingEnterLeave`, `fIsProcessingThemeWalk`, `m_theme:5` — theme already on store) | `DependencyObjectStore` fields/accessors | 1 |
| `core/core/elements/uielement.cpp` EnterImpl/LeaveImpl | `src/Uno.UI/UI/Xaml/UIElement.mux.cs` (existing; rewire `DependencyObject_EnterImpl` → store) | 1 |
| `framework.cpp:3113-3146` `GetInheritanceParentInternal` | `FrameworkElement` partial (logical-parent-aware; replaces `ThemeResolution.GetInheritanceParent` flat walk) | 1 |
| `components/theming/inc/Theme.h` | `src/Uno.UI/UI/Xaml/Theming/Theme.cs` (move/verify existing `Theming.cs`) | 2 |
| `components/theming/FrameworkTheming.{h,cpp}` | `src/Uno.UI/UI/Xaml/Theming/FrameworkTheming.cs` | 2 |
| `components/theminginterop/SystemThemingInterop.cpp` | `src/Uno.UI/UI/Xaml/Theming/SystemThemingInterop.cs` (bridge over `SystemThemeHelper` + HC flag) | 2 |
| `components/theming/ThemeResource.{h,cpp}` (CThemeResource) | `src/Uno.UI/UI/Xaml/Data/ThemeResourceReference.cs` aligned 1:1 (rename decision in-phase) | 2 |
| `components/theming/ThemeWalkResourceCache.{h,cpp}` | `src/Uno.UI/UI/Xaml/Theming/ThemeWalkResourceCache.cs` (CoreServices-owned) | 2 |
| `components/DependencyObject/Theming.cpp` | `src/Uno.UI/UI/Xaml/DependencyObjectStore.Theming.cs` (full 1:1: NotifyThemeChanged/Core/CoreImpl, UpdateAllThemeReferences, UpdateThemeReference ×2, SetThemeResourceBinding) | 3 |
| `CCoreServices` requested-theme slot | `src/Uno.UI/UI/Xaml/Core/CoreServices` (`RequestedThemeForSubTree`, `IsThemeRequestedForSubTree`, `IsSwitchingTheme`) | 3 |
| `framework.cpp` FE theming (:3327, :3346-3386, :3402-3489, :3501-3559, :3969-3994) | `src/Uno.UI/UI/Xaml/FrameworkElement.Theming.cs` rebuilt 1:1 (+ `HighContrastChanged` event) | 4 |
| `core/core/elements/Resources.cpp` `EnsureActiveThemeDictionary` + theme-dict plumbing | `src/Uno.UI/UI/Xaml/ResourceDictionary.cs` (finish the partial port; slot-sourced) | 5 |
| `core/core/elements/ThemeResourceExtension.cpp` + `components/resources/ResourceResolver.cpp` (theme paths) | `src/Uno.UI/UI/Xaml/ResourceResolver.cs` aligned | 5 |
| `xcpcore.cpp:8006-8118` `NotifyThemeChange` | `src/Uno.UI/UI/Xaml/Core/CoreServices.Theming.cs` (new) | 6 |
| `dxaml/lib/FrameworkApplication_Partial.cpp:987-1061` RequestedTheme | `src/Uno.UI/UI/Xaml/Application.cs` delegating to `FrameworkTheming` | 6 |
| `popup.cpp:2846-2936` EnterImpl/LeaveImpl; popup theme at open | `src/Uno.UI/UI/Xaml/Controls/Popup/Popup*.cs` | 7 |
| `dxaml/lib/FlyoutBase_Partial.cpp` theme forwarding | `src/Uno.UI/UI/Xaml/Controls/Flyout/FlyoutBase.cs` | 7 |

## 5. Resolved design decisions

### 5.1 Where CDependencyObject lands (the interface problem)

Uno's `DependencyObject` is an **interface** on every target (generated implementation; UIElement must inherit native views on Android/iOS). The per-object engine every DO owns is `DependencyObjectStore`. Therefore:

- **State** (`_theme` — already there, `fIsProcessingEnterLeave`, `fIsProcessingThemeWalk`, theme-resource map) lives on `DependencyObjectStore`.
- **Base behavior** (`Enter`, `EnterImpl`, `Leave`, `LeaveImpl`, `NotifyThemeChanged`, `UpdateAllThemeReferences`, …) lives on `DependencyObjectStore`, operating on `ActualInstance`.
- **Virtual dispatch** (WinUI's `EnterImpl`/`LeaveImpl`/`NotifyThemeChangedCore` overrides): `UIElement` keeps its existing `internal virtual` chain (UIElement → FrameworkElement → Control → Popup …) which calls the store base exactly where C++ calls `CDependencyObject::EnterImpl`. Non-UIElement DOs that override in WinUI (e.g. `CResourceDictionary`, `FlyoutBase` which already has `Enter/Leave`) implement an internal interface (`IEnterLeaveAware` — name fixed in Phase 1) that the store dispatches to instead of its base; implementations call the store base first/last exactly as the C++ override calls its base.
- Do **not** attempt to convert `DependencyObject` to a class — out of scope, breaking.

### 5.2 The enter-property walk without CEnterDependencyProperty metadata

WinUI walks a generated per-class table of enter-flagged properties. Uno enumerates the store's set DP values whose value is a `DependencyObject` (or a DO collection), honoring an exclusion set equivalent to `DoNotEnterLeave` — derived in Phase 1 from WinUI's metadata (XamlOM / generated `MetadataAPI` tables) for the hot types, starting conservative: visual children (handled by the `CUIElement` children walk), logical-parent backlinks, `DataContext`, and template properties are excluded exactly as WinUI's flags exclude them. Collections recurse into DO items the way `CDOCollection::Enter` does. Any intentional divergence from the table is a `TODO Uno:` with reason. The base's `EstablishThemeAtEnter` property-value walk (`DependencyObjectStore.Theming.cs:160-330`) already encodes several of these exclusions empirically (ItemsSource, UIElement-valued props handled by the visual walk) — fold those learnings into the metadata-derived table, then delete the bolt-on.

### 5.3 The ambient slot

Port `CCoreServices`' slot to `Uno.UI.Xaml.Core.CoreServices`: a single `Theme RequestedThemeForSubTree` field + `IsThemeRequestedForSubTree`, with scoped save/restore structs (no `Stack<T>`). Writers: the three WinUI sites only (§2.5), coupled to `ThemeWalkResourceCache.SetSubTreeTheme` as in xcpcore.cpp:7903-7905. Reader: the `EnsureActiveThemeDictionary` port. The base's theme-parameter threading (leaf `TryGetValue(key, themeKey, …)`) is retired in favor of slot reads once the Phase 3 choke-point assert proves equivalence; `Themes.Active` reads become `FrameworkTheming.GetBaseTheme()`.

### 5.4 Theme keys stay `ResourceKey`-based at the dictionary leaf

WinUI selects sub-dictionaries by literal string keys (`"Light"`, `"Dark"`, `"HighContrastWhite"`, …, `"Default"`). Uno's `SpecializedResourceDictionary.ResourceKey` lookups map 1:1; a single `Theme → key` helper (mirroring the C++ switch blocks) lives with the `EnsureActiveThemeDictionary` port.

### 5.5 Native + reference targets

All new lifecycle/theming machinery compiles under `#if UNO_HAS_ENHANCED_LIFECYCLE` (Skia + WASM). Native Android/iOS keep their current behavior (OS + application theme only) — *unchanged*; any shared file edits preserve the native path under `#if !UNO_HAS_ENHANCED_LIFECYCLE`.

### 5.6 Validation doctrine

WinUI-green first (`/winui-runtime-tests`), then Uno match (`/runtime-tests` Skia + WASM); Uno-only surfaces confirmed in a WinUI probe app; "if Uno disagrees with a WinUI-green test, fix Uno". Full protocol in `plan.md` Global conventions and `tests.md`.

### 5.7 Behavior-preservation discipline for the replacement

The base is *behaviorally* close to WinUI (its test suite is green). Every replacement phase must therefore prove **no behavioral regression** (suite == baseline) *and* **structural fidelity** (the port matches the C++). Where the base's approximation and the exact port disagree behaviorally, WinUI decides (per §5.6) — such differences are surfaced as explicit, test-backed changes, never silent.
