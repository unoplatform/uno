# Resolution Scope & Providing-Dictionary Pinning — Architecture & Discrepancy Analysis

> Companion documents: [`README.md`](./README.md) (index), [`plan.md`](./plan.md) (phased implementation),
> [`tests.md`](./tests.md) (regression suite). This is the **authoritative reference** for *why* the remaining
> divergence exists and *what the target design is*. All WinUI C++ citations are rooted at
> `D:\Work\microsoft-ui-xaml2\src\`. All Uno citations are rooted at the repo root.
>
> Prerequisite reading: [`../theming-winui-alignment/architecture.md`](../theming-winui-alignment/architecture.md).
> That document established the **theme-input** alignment (Mechanism 1 / D3). This document addresses the
> orthogonal, still-unaligned **providing-dictionary / resource-scope** layer. The two compose: a `{ThemeResource}`
> value is a pure function of **(resource key, owner effective theme, providing dictionary)**. The prior spec fixed
> the second input; this spec fixes the third.

---

## 1. Problem statement

A `{ThemeResource Key}` whose `Key` is defined in an **ancestor element's local `Resources.ThemeDictionaries`**
(not in `Application.Resources`) resolves the **wrong theme's value** when the bound element is hosted inside a
popup surface (`Flyout`, `MenuFlyout`, `ToolTip`, bare `Popup`, or a row presented through a popup), even though the
element's `ActualTheme` is correctly inherited from its (logical) opener.

Validated on Skia Desktop (full run in [`tests.md`](./tests.md)): with the application pinned Dark and a host
subtree pinned `RequestedTheme="Light"`, popup content with `ActualTheme == Light` resolves the **Dark** sentinel.
The value tracks `Themes.Active` (the application/OS theme), proving resolution is **not** keyed on the providing
dictionary the owner should see.

This is **not** a theme-establishment bug (the per-object theme and `ActualTheme` are correct — that was fixed by
`theming-winui-alignment`). It is a **resource-scope** bug: the dictionary that *provides* the value is lost when the
element is reparented out of its declaring scope.

---

## 2. How WinUI does it (the `CThemeResource` providing-dictionary model)

WinUI's per-property theme binding object is `CThemeResource`. The crucial member is a **weak reference to the
providing dictionary**, captured once at resolution time and never re-derived from the live tree:

```
dxaml\xcp\components\theming\inc\ThemeResource.h:75-92
    CValue m_lastResolvedThemeValue;                              // cached resolved value
    xref::weakref_ptr<CResourceDictionary> m_pTargetDictionaryWeakRef;   // the PROVIDING dictionary
```

### 2.1 Capture: at the point of resolution, from the lexical (ambient) scope

The providing dictionary is set in `SetInitialValueAndTargetDictionary` (parse time for inline markup; deferred
time for templates/styles, replaying the saved parser context):

```
ThemeResource.cpp:39-51            CThemeResource::SetInitialValueAndTargetDictionary(... targetDictionary)
    m_pTargetDictionaryWeakRef = xref::get_weakref(targetDictionary);
    IFC_RETURN(SetLastResolvedValue(pValue));
ThemeResourceExtension.cpp:257-275  (the markup-extension equivalent)
ThemeResource.cpp:22-30            CThemeResource(CThemeResourceExtension*) copies the weakref + last value across
```

The `targetDictionary` is chosen by `GetDictionaryForThemeReference`. For a **locally-declared** themed brush it pins
**the ambient (local) dictionary itself** — the dictionary that lexically contained the key:

```
ResourceResolver.cpp:947-986   GetDictionaryForThemeReference(core, ambientDictionary, dictionaryReadFrom)
    if (dictionaryReadFrom->IsSystemColorsDictionary())            → pin dictionaryReadFrom (system colors)
    else if (dictionaryReadFrom->IsGlobal() && IsThemeDictionary())→ pin core->GetThemeResources()  (global theme root)
    else if (dictionaryReadFrom->UseAppResourcesForThemeRef())     → pin core->GetApplicationResourceDictionary()
    if (!dictionaryForThemeReference)                              → pin ambientDictionary  ← LOCAL CASE
```

The lexical/ambient scope itself is the parser's stack of `FrameworkElement.Resources` dictionaries (the markup's
lexical position), **not** the live visual tree:

```
ResourceResolver.cpp:111-243   ResolveResourceImpl  — iterates ambient values
ResourceResolver.cpp:486-509   GetAmbientValues → GetAllAmbientValues(... FrameworkElement_Resources ...)
Resources.h:34-36              LookupScope::LocalOnly = Merged | LocalTheme
Resources.cpp:476-482          GetKeyNoRefImpl LocalTheme branch → GetKeyFromThemeDictionariesNoRef
```

### 2.2 The pinned dictionary always reflects the *current* theme

The pinned `CResourceDictionary` lazily selects its active theme sub-dictionary on each access. So re-querying the
*same* dictionary object after a theme change returns the new theme's value:

```
Resources.cpp:687-819   EnsureActiveThemeDictionary  — selects Default/Light/Dark/HighContrast* sub-dict
Resources.cpp:766-790       requestedTheme = IsThemeRequestedForSubTree() ? GetRequestedThemeForSubTree()
                                                                          : GetFrameworkTheming()->GetBaseTheme();
Resources.cpp:644-685   GetKeyFromThemeDictionariesNoRef → EnsureActiveThemeDictionary → active->GetKeyNoRefImpl(LocalOnly)
```

### 2.3 Refresh: re-query the pinned dictionary, do not re-walk the tree

```
ThemeResource.cpp:63-129   CThemeResource::RefreshValue()
    CResourceDictionary* pTargetDictionaryNoRef = m_pTargetDictionaryWeakRef.lock();
    if (pTargetDictionaryNoRef) {
        ... pTargetDictionaryNoRef->GetKeyNoRef(m_strResourceKey, &pValueDO);   // re-query SAME dict
        SetLastResolvedValue(pValueDO);
    }
    // else (dictionary destroyed during teardown): keep m_lastResolvedThemeValue
```

The pin is **sticky** — `m_pTargetDictionaryWeakRef` is never mutated by Enter/Leave/reparent. Moving content under
`PopupRoot` cannot lose it. `m_lastResolvedThemeValue` is the teardown fallback when the dictionary is gone.

### 2.4 `UpdateThemeReference` — the active-walk-vs-refresh decision

The per-DO update prefers a live ancestor walk *when active*, then falls back to the pinned-dictionary refresh:

```
Theming.cpp:315-346   CDependencyObject::UpdateThemeReference(CThemeResource* themeResource)
    bool refreshed = false;
    if (IsActive()) {
        Resources::ResourceResolver::FindNextResolvedValueNoRef(this, themeResource, ShouldCheckForResourceOverrides(), &valueNoRef);
        if (valueNoRef) { themeResource->SetLastResolvedValue(valueNoRef); refreshed = true; }
    }
    if (!refreshed && (IsProcessingThemeWalk() || m_theme != Theme::None || !themeResource->IsValueFromInitialTheme()))
        themeResource->RefreshValue();
```

- `FindNextResolvedValueNoRef` → `ScopedResources::GetOrCreateOverrideForVisualTree` (`ScopedResources.cpp:490-607`)
  → `TraverseVisualTreeResources` (`ScopedResources.cpp:135-171`) walks **`GetParentFollowPopups()`** with per-FE
  `LookupScope::LocalOnly`.
- The `RefreshValue` fallback is gated by `m_isValueFromInitialTheme` (`ThemeResource.cpp:140`): a value still from
  the initial theme, with no theme walk in progress and no per-object theme, is left as-is (perf).

### 2.5 Popups: scope and theme are handled by **two different mechanisms**

A flyout's `CPopup` is **parentless** — it is never logically/inheritance/visually parented to its opener:

```
FlyoutBase_partial.cpp:1376   spPopup->put_Child(spPresenterAsUI.Get());     // presenter becomes the popup child
FlyoutBase_partial.cpp:1381   put_AssociatedFlyout(this);                    // weak callback ref only
Popup.cpp:3957                pPopup->m_associatedFlyoutWeakRef = xref::get_weakref(...);   // NOT a parent
Popup.cpp:2580                AddLogicalChild(m_pChild);  → framework.cpp:3169  pChild->m_pLogicalParent = this;  // child's logical parent = CPopup
```

**Resource scope** for flyout content therefore comes **only** from the parse-pinned providing dictionary (§2.1).
`GetParentFollowPopups` dead-ends at the parentless `CPopup`:

```
dependencyobject.cpp:486-518  GetParentFollowPopups()
    parent = GetParentInternal();
    if (parent is PopupRoot) { popup = popupRoot->GetOpenPopupWithChild(this); return popup ?? parent; }  // hop → CPopup, then stops
```

For an **inline `<Popup>`** that genuinely lives in the tree, the `CPopup` has a real parent, so the walk continues
to the declaration site (`ShouldPopupRootNotifyThemeChange` returns false for parented popups — `Popup.cpp:3621-3633`).

**Theme** is forwarded explicitly at open time, not inherited through a parent chain:

```
FlyoutBase_partial.cpp:1534-1592   ForwardThemeToPresenter — walk up from placement target to a non-Default
                                   RequestedTheme (hopping PopupRoot→parent), put_RequestedTheme on presenter AND popup
Popup.cpp:4359-4372    CPopupRoot::CompleteAdditionToOpenPopupList — push app theme to parentless popups at open
Popup.cpp:3660-3669    CPopup::NotifyThemeChangedCore — propagate the pushed theme to m_pChild
Popup.cpp:5413-5418    PopupRoot's own theme is forced None so content does NOT inherit PopupRoot's theme on Enter
```

So at the flyout content's `EnterImpl` (`depends.cpp:1023-1048`), its logical parent is the `CPopup`, whose
`m_theme` was already set by the open-time forwarding — and the content inherits the opener's theme. **This is what
Uno's `FlyoutBase.ForwardThemeToPresenter` already mirrors; it is correct and stays.**

---

## 3. Discrepancy table (Uno today vs WinUI)

| # | Concern | WinUI | Uno today | Impact |
|---|---------|-------|-----------|--------|
| **R1** | Providing dictionary captured & pinned at resolution | `m_pTargetDictionaryWeakRef` set in `SetInitialValueAndTargetDictionary` at parse/deferred resolution (`ThemeResource.cpp:39-51`), per `GetDictionaryForThemeReference` rules (`ResourceResolver.cpp:947-986`) | `ApplyResource` creates `ThemeResourceReference(targetDictionary: null, …)` (`ResourceResolver.cs:352-355,378-381`); pin deferred to a load-time walk that only pins on a hit | **The core defect.** For reparented (popup) content the load-time walk never reaches the opener dict, so the pin never happens and the stale parse value (against `Themes.Active`) sticks. |
| **R2** | Resource scope follows popups | `TraverseVisualTreeResources` walks `GetParentFollowPopups()` (`ScopedResources.cpp:158`, `dependencyobject.cpp:486-518`) | `GetResourceDictionaries` walks `fe.Parent` (`DependencyObjectStore.cs:1512`); the `Popup`'s child has `LogicalParentOverride = Popup` but the `Popup` is not linked onward, so the walk dead-ends at the popup | Inline `<Popup>` and the override/alias walk cannot reach the declaration site; for flyouts this path is moot (parentless), but parity requires the hop. |
| **R3** | `UpdateThemeReference` active-walk-vs-refresh | active→`FindNextResolvedValueNoRef`; else `RefreshValue` gated by `IsProcessingThemeWalk \|\| m_theme != None \|\| !IsValueFromInitialTheme` (`Theming.cpp:315-346`) | Phase A walk gated on `owner is FrameworkElement { IsLoaded: true }` over `GetResourceDictionaries(includeAppResources:false)`; Phase B always `RefreshValue` when `!resolved` (`DependencyObjectStore.Theming.cs:279-302`) | Missing the `IsValueFromInitialTheme` perf-guard; the `IsLoaded`/`includeAppResources:false` scoping differs from WinUI's `IsActive`/`LocalOnly`. Behavioral parity gap. |
| **R4** | Refresh re-queries the pinned dict | `RefreshValue` re-queries `m_pTargetDictionaryWeakRef`; falls back to `m_lastResolvedThemeValue` on teardown (`ThemeResource.cpp:63-129`) | `RefreshValue(ownerTheme)` re-queries `_targetDictionary` weakref, falls back to `TryTopLevelRetrieval` then `LastResolvedValue` (`ThemeResourceReference.cs:113-153`) | Correct **iff** `_targetDictionary` was pinned (R1). When null, falls to top-level (app) resources — which lack a locally-declared key → wrong value. |
| **R5** | Initial-theme value flag | `m_isValueFromInitialTheme` set at first resolution (`ThemeResource.cpp:140`), gates refresh | No equivalent flag; `IsResolved` distinguishes only resolved/unresolved | Cannot replicate WinUI's refresh gating (R3); minor extra work or stale-value risk in edge cases. |
| **R6** | `ThemeWalkResourceCache` keying & lifetime | keyed `(dictionary, key)` within a theme-walk scope set by `SetSubTreeTheme` (`ThemeWalkResourceCache.{h,cpp}`) | keyed `(dict, key, theme)`, valid during `BeginCachingThemeResources` (`ThemeWalkResourceCache.cs`) | Likely aligned; must verify the cache is consulted with the pinned dict + owner theme after R1. |
| **R7** | Foreground freeze reaches popup content | `TextFormatting` freeze + `PullInheritedTextFormatting`; popup content pulls via logical parent | `_themeForeground`/`_isForegroundFrozen` propagated in the theme walk + popup content (`FrameworkElement.Theming.cs:399-469`) | Must confirm the frozen default-text brush is resolved against the owner theme and reaches popup/template content after R1/R2. |
| **R8** | High-contrast composition | effective theme `base \| highContrast`; HC sub-dicts selected first (`Resources.cpp:718-762`) | HC OR-ed in `ResolveOwnerTheme`; HC sub-dict selection at the leaf (`ThemeResolution.cs:42-70`) | Verify HC selection composes with the pinned-dict path; close any gap. |
| **R9** | Non-FE DO / behaviors `{ThemeResource}` | non-FE DOs themed via the property walk; refs refreshed from their pinned dict | non-FE DOs carry theme on the store (D1); refreshed via `UpdateChildResourceBindings` (`DependencyObjectStore.Theming.cs:405-508`) | Verify a `{ThemeResource}`-bound DP on a non-FE DO (e.g. a behavior) resolves against the owner/`AssociatedObject` theme and from a pinned dict after R1. |

---

## 4. How the target design fixes each failing repro

With **R1** (pin the providing dictionary at parse from the lexical scope) alone:

- Flyout/MenuFlyout/ToolTip/row content is parsed inside the opener's lexical scope → its `{ThemeResource}` pins the
  **opener-local** dictionary.
- The element's owner theme is already correct (`Light`) at load via `EstablishThemeAtEnter` +
  `ForwardThemeToPresenter` (unchanged).
- `UpdateThemeReference` → `RefreshValue(Light)` → re-queries the **pinned opener-local** dictionary →
  `EnsureActiveThemeDictionary` selects the `Light` sub-dict → the Light sentinel. **Green. Every popup repro green.**

**R2** adds the `GetParentFollowPopups` hop so inline `<Popup>` and the override/alias walk reach the declaration
site (full parity; not required for the flyout repros but required to match WinUI exactly).

**R3–R9** complete the 1:1 alignment so no adjacent scenario (theme toggle after open, recycle/reload, HC, non-FE
owners) can regress.

---

## 5. Target design

### Invariant (extends the `theming-winui-alignment` invariant)

> The value of any `{ThemeResource}` is a pure function of **(resource key, the owner's effective theme, the
> providing `ResourceDictionary`)**. The providing dictionary is captured at the moment of resolution from the
> **lexical (parse/ambient) scope** following WinUI's `GetDictionaryForThemeReference` rules, pinned as a weak
> reference, and is **never re-derived from the element's live (post-reparent) visual position**. Re-resolution
> re-queries that pinned dictionary, which always reflects the current theme via its active theme sub-dictionary.

### 5.1 R1 — capture & pin the providing dictionary (core)

- In `ResourceResolver.ApplyResource` (`ResourceResolver.cs:323-388`), resolve the initial value using the
  **providing-dictionary** lookup (the `out ResourceDictionary providingDictionary` overload —
  `TryStaticRetrieval(..., out value, out providingDictionary)` at `:608-630`) against the **lexical/ambient scope**,
  and construct the `ThemeResourceReference` with that dictionary instead of `null`.
- Apply WinUI's `GetDictionaryForThemeReference` mapping when deciding *which* dictionary to pin:
  - found in a local dictionary → pin that local dictionary (the common popup case);
  - found in a global theme dictionary → pin the theme-resources root (not the theme-specific sub-dict);
  - found in `Application.Resources` → pin app resources;
  - found in system/assembly resources → these need no pin (`RefreshValue` already falls back to
    `TryTopLevelRetrieval`); preserve that.
- For **deferred/template/style/visual-state** resolution (compiled XAML `{ThemeResource}` on a DP, templates,
  Setters), capture & pin the providing dictionary at the deferred resolution point using the same lexical scope that
  Uno already replays via `ResourceBinding.ParseContext` / `CurrentScope` (the analog of WinUI's
  `TryResolveResourceFromCachedParserContext`). The existing re-pin in `InnerUpdateResourceBindingsUnsafe`
  (`DependencyObjectStore.Theming.cs:650-665`) stays as the secondary path but is no longer the *only* pin source.
- Keep the pin **sticky**: never null it on unload/reparent. `RefreshValue(ownerTheme)` re-queries it (R4 already
  does this once `_targetDictionary` is non-null).

### 5.2 R2 — `GetParentFollowPopups` in the parent walks

- Add a popup-following step to `DependencyObjectStore.GetResourceDictionaries` (`:1491-1525`): when the walk reaches
  a `Popup`/`PopupRoot`, continue to the popup's tree/logical parent for an **inline** popup (mirror
  `GetParentFollowPopups`: hop `PopupRoot → Popup`, then the popup's real parent). For a parentless flyout popup the
  hop ends at the popup (matching WinUI) — flyout content relies on R1.
- Apply the same hop to the inheritance-parent walk used by `ThemeResolution.ResolveOwnerTheme`
  (`ThemeResolution.cs:84-86`) and `EstablishThemeAtEnter` so an inline popup's content inherits theme from its
  declaration site, consistent with the resource walk.

### 5.3 R3/R5 — `UpdateThemeReference` parity + `IsValueFromInitialTheme`

- Add an `IsValueFromInitialTheme` flag to `ThemeResourceReference` (port `m_isValueFromInitialTheme`,
  `ThemeResource.cpp:140`): true until the first non-initial resolution.
- Match `Theming.cpp:315-346`: when the owner is active, attempt the ancestor walk; otherwise (or if the walk finds
  nothing) call `RefreshValue` gated by `IsProcessingThemeWalk || owner.GetTheme() != None || !IsValueFromInitialTheme`.
- Reconcile Uno's Phase A `IsLoaded`/`includeAppResources:false` with WinUI's `IsActive`/`LocalOnly` so the same
  scenarios take the walk vs. the pinned refresh.

### 5.4 R4/R6 — refresh + cache verification

- Verify `RefreshValue(ownerTheme)` consults `ThemeWalkResourceCache` with the **pinned dict + owner theme** and that
  the cache is only valid within a theme walk (`BeginCachingThemeResources`). No design change expected; this is a
  parity check after R1.

### 5.5 R7/R8/R9 — foreground freeze, high contrast, non-FE owners

- Confirm `DefaultTextForegroundThemeBrush` is resolved against the **owner** theme (already threaded post-`D3`) and
  that the frozen brush reaches popup/template content; add a repro if a gap is found.
- Confirm HC composition (`base | highContrast`) selects HC sub-dicts through the pinned-dict path.
- Confirm a `{ThemeResource}`-bound DP on a non-FE DO (behavior) resolves against the owner/`AssociatedObject` theme
  from a pinned dict.

### 5.6 What stays (do not change)

- `FlyoutBase.ForwardThemeToPresenter` and the popup theme-forwarding path (faithful to WinUI §2.5).
- The `theming-winui-alignment` Mechanism 1 owner-theme threading and the absence of a global requested-theme stack.
- `PopupRoot`'s neutral (`None`) own theme so content does not inherit it on Enter.

---

## 6. Scope, platforms, risk

- **Enhanced lifecycle (Skia/WASM) only**, consistent with `theming-winui-alignment`. Native (Android/iOS) remains
  OS + application theme only; element-level/popup resource-scope theming is not extended to native. The popup repros
  are `[PlatformCondition(Exclude, NativeAndroid|NativeIOS)]`.
- **Performance:** R1 captures a weak reference at the existing resolution call (no new walk). It should *reduce*
  load-time work by making the load-time tree walk a fallback rather than the primary pin source. Confirm with the
  resource-dictionary benchmarks.
- **Risk — parse-time scope fidelity:** the exact moment Uno's lexical scope contains the opener-local dictionary for
  popup content (parse vs deferred) must be confirmed by trace during Phase 1 (the empirical Skia result shows the
  local brush *is* found at parse against `Themes.Active`, so the scope is reachable; the pin must be captured there).
- **Risk — global-theme pinning:** pinning the theme-resources *root* (not a theme-specific sub-dict) for global
  brushes is essential so a later theme change re-selects the sub-dict. Follow `GetDictionaryForThemeReference`
  exactly; do not pin a Light/Dark sub-dictionary.

---

## 7. Key WinUI source map

| Concern | File:line |
|---------|-----------|
| `CThemeResource` members (`m_pTargetDictionaryWeakRef`, `m_lastResolvedThemeValue`) | `components\theming\inc\ThemeResource.h:75-92` |
| Capture providing dict (`SetInitialValueAndTargetDictionary`) | `components\theming\ThemeResource.cpp:39-51`; ext `core\core\elements\ThemeResourceExtension.cpp:257-275` |
| `RefreshValue` (re-query pinned dict) | `components\theming\ThemeResource.cpp:63-129` |
| `m_isValueFromInitialTheme` | `components\theming\ThemeResource.cpp:140` |
| `GetDictionaryForThemeReference` (which dict to pin) | `components\resources\ResourceResolver.cpp:947-986` |
| Lexical/ambient parse scope | `components\resources\ResourceResolver.cpp:111-243, 486-509` |
| `LookupScope::LocalOnly` | `core\inc\Resources.h:34-36`; `core\core\elements\Resources.cpp:476-482` |
| `EnsureActiveThemeDictionary` (theme→sub-dict) | `core\core\elements\Resources.cpp:687-819` |
| `UpdateThemeReference(CThemeResource*)` | `components\DependencyObject\Theming.cpp:315-346` |
| `TraverseVisualTreeResources` / override walk | `components\resources\ScopedResources.cpp:135-171, 490-607` |
| `GetParentFollowPopups` | `components\DependencyObject\dependencyobject.cpp:486-518` |
| `EnterImpl` theme block | `core\core\elements\depends.cpp:1023-1048` |
| `GetInheritanceParentInternal(fLogicalParent)` | `core\core\elements\framework.cpp:3113-3146` |
| `AddLogicalChild` / `m_pLogicalParent` | `core\core\elements\framework.cpp:3156-3173`; `Popup.cpp:2580` |
| Flyout: `ForwardThemeToPresenter`, `EnsurePopupAndPresenter`, `Open` | `dxaml\lib\FlyoutBase_partial.cpp:1351-1424, 1238-1349, 1534-1592` |
| Popup parentless / associated-flyout weakref | `core\core\elements\Popup.cpp:3935-3964, 3957` |
| PopupRoot pushes app theme; neutral own theme | `core\core\elements\Popup.cpp:4359-4372, 3660-3669, 3621-3633, 5408-5444, 5413-5418` |
| `ThemeResourceExpression` (DXAML layer) | `dxaml\lib\ThemeResourceExpression.cpp:77-201` |
