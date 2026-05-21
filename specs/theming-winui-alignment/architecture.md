# Theming WinUI Alignment — Architecture & Discrepancy Analysis

> Companion documents: [`README.md`](./README.md) (index + conventions), [`plan.md`](./plan.md) (phased implementation), [`tests.md`](./tests.md) (regression suite).
>
> This document is the **authoritative reference** for *why* the refactor is needed and *what the target design is*. Every implementation phase in `plan.md` cites back to sections here. All WinUI C++ citations are rooted at `D:\Work\microsoft-ui-xaml2\src\`. All Uno citations are rooted at the repo root (worktree `D:\Work\uno-worktrees\theming-revamp`).

---

## 1. Problem statement

Element-level theming (`FrameworkElement.RequestedTheme`, PR #22803) and lazy theme-resource evaluation (PR #22887) landed, followed by a steady stream of corrective PRs (#23127, #23178, #23197, #23243) and still-open production bugs. The reported bugs are tracked internally; they are described here only by their generic, reproducible symptom (the public spec must stand on its own without private trackers):

| Scenario | Symptom | Platform |
|----------|---------|----------|
| **S1** virtualized item, opposite-theme app | Realized list/grid item text renders with the app/OS theme instead of the element's theme (e.g. dark labels in a light-themed control). | Skia desktop, WASM |
| **S2** recycle on tab navigation | Container content recycled when switching tabs re-renders with the wrong theme while the app is light. | Skia desktop, WASM |
| **S3** scrolled-into-view cell | A cell/column materialized on scroll uses the wrong (dark) styling; toggling the app theme dark→light "fixes" it. | WASM, Skia desktop |
| **S4** popup/flyout first open | A popup/flyout (incl. the mobile text-selection context menu, `TextCommandBarFlyout`) shows the wrong style on **first** open, correct on second open. | iOS, Android |
| **S5** runtime-added control | A control created and added to the tree at runtime resolves the wrong theme. | Skia desktop, WASM |
| **uno #23177** (public) | `ThemeResource` returns light values after switching the app to dark (regression from #22803/#22887). | WASM |

These are not separate bugs. They are surfacings of **one architectural defect**.

---

## 2. The one defect, stated precisely

Uno's theme **propagation** machinery is already a faithful WinUI port:

- Per-element theme field `UIElement._theme` (`Theme.None` default) — `src/Uno.UI/UI/Xaml/UIElement.cs:70`, getter/setter `:86`/`:91`.
- The notify walk `FrameworkElement.NotifyThemeChanged` / `NotifyThemeChangedCore` — `src/Uno.UI/UI/Xaml/FrameworkElement.Theming.cs:164` / `:214`.
- The per-reference pinned dictionary `ThemeResourceReference` (Uno's `CThemeResource`) — `src/Uno.UI/UI/Xaml/Data/ThemeResourceReference.cs`.
- The per-walk dedup cache `ThemeWalkResourceCache` — `src/Uno.UI/UI/Xaml/ThemeWalkResourceCache.cs`.

The defect is in the **resolution leaf**. When any `{ThemeResource}` resolves a value, the Light/Dark sub-dictionary is chosen by `ResourceDictionary.GetActiveTheme()`:

```
src/Uno.UI/UI/Xaml/ResourceDictionary.cs:914
    internal static ResourceKey GetActiveTheme() => Themes.RequestedThemeForSubTree;

src/Uno.UI/UI/Xaml/ResourceDictionary.cs:982-998   (private static class Themes)
    public static ResourceKey Active { get; set; } = Default;                 // app/OS theme
    private static readonly Stack<ResourceKey> _requestedThemeForSubTree = new();  // PROCESS-GLOBAL
    public static ResourceKey RequestedThemeForSubTree =>
        _requestedThemeForSubTree.Count > 0 ? _requestedThemeForSubTree.Peek() : Active;
```

`GetActiveTheme()` reads a **process-global** value: the top of a static stack, or the global `Themes.Active`. It **never consults the resolving element's own `_theme`**. Confirm in the leaf lookups:

```
src/Uno.UI/UI/Xaml/ResourceDictionary.cs:294   GetActiveThemeDictionary(GetActiveTheme()) ...   // value lookup
src/Uno.UI/UI/Xaml/ResourceDictionary.cs:364   GetActiveThemeDictionary(GetActiveTheme()) ...   // value + providing-dict lookup
```

And `ThemeResourceReference.RefreshValue(owner, …)` **accepts `owner` but never uses it to choose the theme** — it just calls `dict.TryGetValue(ResourceKey, …)` which internally reads `GetActiveTheme()`:

```
src/Uno.UI/UI/Xaml/Data/ThemeResourceReference.cs:105-122
    public object? RefreshValue(DependencyObject? owner, ThemeWalkResourceCache? cache = null)
    {
        if (_targetDictionary is not null && _targetDictionary.TryGetTarget(out var dict))
        {
            ...
            if (dict.TryGetValue(ResourceKey, out var value, shouldCheckSystem: false))  // ← theme is ambient/global
            { ... }
        }
    }
```

**Consequence — the whack-a-mole.** Because the leaf ignores the element's theme, any code that resolves a `{ThemeResource}` *outside* the main `NotifyThemeChanged` walk must first **manually push** the element's theme onto the global stack so the leaf picks the right sub-dictionary. There are **11 such band-aid push sites across 8 files today** (§5). Every *new* materialization path that someone forgets to instrument leaks the ambient theme — which is exactly what each reported bug is: a materialization path (virtualized rows, recycled tab content, runtime-created controls, first popup open) with no push.

---

## 3. How WinUI actually does it (and the subtle truth)

It is tempting to say "WinUI has no global theme stack." **That is wrong** and the plan must not be built on that false premise. WinUI *also* uses an ambient slot: `CCoreServices::m_requestedThemeForSubTree`. The dictionary's theme selection reads it:

```
ThemeResourceExtension/CThemeResource ─ GetKeyNoRef(key)
  → CResourceDictionary::GetKeyFromThemeDictionariesNoRef            Resources.cpp:644-685
  → CResourceDictionary::EnsureActiveThemeDictionary                 Resources.cpp:687-819
        auto requestedTheme = core->IsThemeRequestedForSubTree()
            ? core->GetRequestedThemeForSubTree()                    // ← ambient slot
            : core->GetFrameworkTheming()->GetBaseTheme();           // ← app/OS base theme
```

The **two real differences** that make WinUI correct where Uno is not:

### Difference A — every `CDependencyObject` carries a resolved theme, established at `Enter`

`m_theme` is a 5-bit field on **`CDependencyObject`** (not just elements): `CDependencyObject.h:1761`. It is set during the tree-attach walk `CDependencyObject::EnterImpl` for **every** DO that goes live:

```
depends.cpp:1023-1048   (EnterImpl, only when m_bitFields.fLive)
    if (thisAsFe)  pParent = GetInheritanceParentInternal(TRUE /* fLogicalParent */);
    else           pParent = GetParentInternal(false);
    if (pParent && pParent->GetTheme() != Theme::None && pParent->GetTheme() != m_theme)
        IFC_RETURN(NotifyThemeChanged(pParent->GetTheme()));     // inherit + walk subtree
    else
        IFC_RETURN(UpdateAllThemeReferences());                  // re-resolve refs vs new ancestors
```

So a freshly materialized element (virtualized row, template output, popup child, runtime-created widget) gets its theme from its **(logical) inheritance parent** *before any property/resource is resolved*. A light subtree under a dark OS can never resolve dark on materialization, because the new element inherits `Light` at `Enter`.

### Difference B — resolving a theme ref *outside a walk* always pushes the **owner's own** `m_theme`

WinUI never relies on "whatever happens to be on the slot". The per-property apply primitive pushes the owner's resolved theme every time:

```
Theming.cpp:368-397   (CDependencyObject::SetThemeResourceBinding)
    if (!IsProcessingThemeWalk() && m_theme != Theme::None) {
        prevTheme = GetRequestedThemeForSubTreeFromCore();
        if (prevTheme != Theming::GetBaseValue(m_theme)) {
            SetRequestedThemeForSubTreeOnCore(m_theme);   // ← push THIS object's own theme
            popTheme = true;
        }
    }
    ... UpdateThemeReference(pThemeResource);              // resolves against the just-pushed theme
    pThemeResource->GetLastResolvedThemeValue(&value);
    SetValue(SetValueParams(pDP, value, baseValueSource));
```

In other words, WinUI has exactly **one** "push the owner's theme" site, and it is *inside the resolution primitive*, keyed off `this->m_theme`. Uno scattered the equivalent push across 11 *call sites* (and keyed them off the element discovered at each site) — and missed the materialization paths that the reported bugs exercise.

### The precedence rules WinUI uses (to be matched exactly)

- **Effective theme during the walk** (`CDependencyObject::NotifyThemeChanged`, Theming.cpp:119-156): the incoming inherited theme, then `CFrameworkElement::GetRequestedThemeOverride(theme)` (framework.cpp:3268-3288) replaces the **base** bits with Light/Dark if `RequestedTheme != Default`, **preserving** high-contrast bits.
- **`ActualTheme`** (framework.cpp:3953-3978): `GetBaseValue(m_theme)`, falling back to `FrameworkTheming::GetBaseTheme()` when `m_theme == None`. Always Light or Dark, HC stripped.
- **App vs OS** (`FrameworkTheming::GetTheme()`, FrameworkTheming.cpp:119): `(m_requestedTheme != None ? m_requestedTheme : m_systemTheme) | m_highContrastTheme`. **If the app set `RequestedTheme`, the OS light/dark setting is ignored.** OS theme only applies when the app left `RequestedTheme` unset. High contrast is orthogonal and always OR-ed in.
- **`Theme` enum is bit-composed** (Theme.h:10-65): `BaseMask=0x03` (None/Light/Dark), `HighContrastMask=0x1C`. Effective theme = `base | highContrast`. `None` (0x00) is the "no theme established yet / use default" sentinel.
- **Enter inheritance parent** is the **logical** parent for FrameworkElements (`GetInheritanceParentInternal(fLogicalParent=TRUE)`, framework.cpp:3097-3130) so popups/flyouts follow the theme of the control that opened them, not the `PopupRoot`.
- **`ActualThemeChanged` ordering** (framework.cpp:3297-3370): fired *after* the subtree is updated (inside `NotifyThemeChangedCore`) but *before* `m_theme` is assigned (Theming.cpp:155), gated on `GetBaseValue(old) != GetBaseValue(new)` (or `old == None && base theme is changing`). HighContrastChanged fires separately and suppresses ActualThemeChanged unless transitioning out of HC.

---

## 4. Discrepancy table (Uno today vs WinUI)

| # | Concern | WinUI | Uno today | Impact |
|---|---------|-------|-----------|--------|
| D1 | Where per-object theme lives | `m_theme` on **every** `CDependencyObject` (`CDependencyObject.h:1761`) | `_theme` on **`UIElement` only** (`UIElement.cs:70`) | Non-UIElement owners (brushes, setters, storyboards) have no theme; their `{ThemeResource}` resolves against the global ambient. (`When_ThemeResource_On_NonFE_DependencyObject_*` test) |
| D2 | Theme established at tree attach | `EnterImpl` theme block runs for every DO going live, inheriting from logical parent (`depends.cpp:1023-1048`) | Only `FrameworkElement.OnLoadingPartial` inherits, and only if `parent.GetTheme() != None` (`FrameworkElement.cs:434-436`); enhanced-lifecycle (`__SKIA__`/`__WASM__`) only | Materialized/virtualized/recycled elements often have `_theme == None` at first resolution → fall back to `Themes.Active`. Root of S1/S2/S3/S5. |
| D3 | What the resolution leaf keys on | Ambient slot, but the slot is **always pushed from the owner's own `m_theme`** inside `SetThemeResourceBinding` (`Theming.cpp:368`) | Ambient global stack/`Themes.Active`; leaf ignores `owner.GetTheme()` (`ResourceDictionary.cs:294,364`; `ThemeResourceReference.cs:117`) | The central defect. Requires 11 band-aid pushes (§5). |
| D4 | Theme persistence across unload/recycle | `m_theme` persists; re-`Enter` re-themes from parent | `ClearThemeStateOnUnloaded` resets `_theme = None` (`FrameworkElement.Theming.cs:540-561`) | Recycled rows lose theme → resolve ambient on reload. Root of S2 (recycle). |
| D5 | Popup/flyout theme at first open | Logical-parent inheritance at `Enter`, on all platforms | Popup theme propagation is `#if UNO_HAS_ENHANCED_LIFECYCLE` (Skia/WASM only); native uses legacy path with no push (`Popup.WithPopupRoot.cs:109-119`, `Popup.cs:159-180`) | First-open wrong style on iOS/Android. Root of S4 (popup first open). |
| D6 | Flyout theme source | Placement target's effective theme (`ActualTheme`) | `FlyoutBase.ForwardThemeToPresenter` only forwards the nearest **non-Default `RequestedTheme`** (`FlyoutBase.cs:755-794`); misses app-level/inherited theme | Flyouts opened over app-themed (not element-themed) content aren't themed → ambient leak. Contributes to S4. |
| D7 | Custom theme names vs standard themes | Only Light/Dark/HC + "Default" fallback in dictionaries | App-level `RequestedCustomTheme` produces an arbitrary `Themes.Active` key (`Application.cs:205-214`); element-level `ElementTheme` only yields Light/Dark/Default | A custom theme name that does not match a system `ThemeDictionaries` key falls back to the **`Default`** sub-dictionary (`GetActiveThemeDictionary` → `Themes.Default`, `ResourceDictionary.cs:590-600`), which in Fluent generic resources is the **dark** theme. Plausible contributor to "dark leak" with custom themes. |
| D8 | High-contrast composition | `base | highContrast` bitfield, HC always wins, separate dictionaries | Uno `Theme` enum / resolution does not compose HC with base in the same bit-packed way | HC correctness gaps (out of direct scope of the reported bugs but part of faithful alignment). |

---

## 5. Inventory of the 11 band-aid push sites (to be removed in Phase 4)

All gated by `UNO_HAS_ENHANCED_LIFECYCLE` (Skia/WASM). Each pushes the element's `_theme`-derived key onto the global stack so the leaf `TryGetValue` selects the right sub-dictionary.

| # | File:line (push / pop) | Trigger | Why it exists |
|---|---|---|---|
| 1 | `FrameworkElement.Theming.cs:255` / `:318` | Theme-change walk `NotifyThemeChangedCore` | Canonical walk push (self-documented `:235-238`). |
| 2 | `FrameworkElement.Theming.cs:461` / `:485` | Foreground freeze `NotifyThemeChangedForInheritedProperties` | Resolve `DefaultTextForegroundThemeBrush` under element theme. |
| 3 | `FrameworkElement.cs:454` / `:487` | `OnLoadingPartial` | Styles + `UpdateThemeBindings(ResolvedOnLoading)` at element entry. |
| 4 | `FrameworkElement.cs:734` / `:745` | `OnStyleChanged` → `ApplyStyleWithThemeContext` | Style ThemeResource setters from code-behind (PR #23127). |
| 5 | `FrameworkTemplate.cs:202` / `:246` | `LoadContent` template materialization | `{ThemeResource}` in templates (DataGrid rows, ContentPresenter). |
| 6 | `VisualStateGroup.cs:259` / `:279` | `GoToState` visual state materialization | VisualState Storyboard/Setters. |
| 7 | `VisualStateGroup.cs:393` / `:420` | `GoToState` → `ApplyTargetStateSetters` | State setter `ApplyValue`. |
| 8 | `Data/BindingHelper.cs:82` / `:95` | `UpdateResourceBindings` initial resolution | Deferred (unpinned) refs tree-walk. |
| 9 | `Documents/Hyperlink.cs:251` / `:289` | `SetCurrentForeground` (pointer states) | Brushes resolved during pointer events outside any walk. |
| 10 | `Media/Animation/ObjectAnimationUsingKeyFrames.cs:352` / `:365` | `EnsureKeyFrameThemeResources` | Keyframe ThemeResources at `Begin`. |
| 11 | `DependencyProperty.cs:636` / `:650` | `ResolveFocusVisualBrushDefault` | Focus visual default brushes (PR #23243). |

Plus control-level `UpdateThemeBindings` overrides that reach DOs outside the visual-tree walk (Popup, PopupRoot, CommandBar, ContentPresenter, IconElement, TextBlock, TextBox) — these stay (they are propagation hooks, not theme-selection band-aids) but must be re-pointed to the new resolution primitive.

---

## 6. Target design

**Invariant (the whole point):**

> The value of any `{ThemeResource}` is a pure function of **(resource key, the resolving owner's effective theme)**. The owner's effective theme is established the moment it enters the live tree, inherited from its (logical) inheritance parent, and is *never* derived from a process-global ambient that a caller forgot to set.

Two equivalent mechanisms achieve the invariant. The plan **recommends Mechanism 1**; Mechanism 2 is documented for completeness and as a fallback if a parameter cannot be threaded everywhere cheaply.

### Mechanism 1 (recommended) — thread the owner's effective theme as a parameter

- Add an explicit `Theme theme` argument to the resolution leaf:
  - `ResourceDictionary.TryGetValue(in ResourceKey key, Theme theme, out object value, …)` and the providing-dictionary overload — replacing the `GetActiveTheme()` reads at `:294`/`:364` with the passed `theme` (note `GetActiveThemeDictionary(in ResourceKey)` *already* takes a theme key, so the change is plumbing, not new lookup logic).
  - `ThemeResourceReference.RefreshValue(Theme ownerTheme, …)` and `RefreshValueWithTreeWalk(Theme ownerTheme, …)` pass `ownerTheme` down.
  - `ThemeWalkResourceCache` keys on the passed `theme` rather than `GetActiveTheme()` (`ThemeWalkResourceCache.cs:104,133`).
- The theme is computed once, centrally, in `DependencyObjectStore.UpdateThemeReference` (the analog of WinUI's `SetThemeResourceBinding`) as `ResolveOwnerTheme(owner)`:
  - `owner.GetTheme()` if non-`None`;
  - else nearest themed inheritance ancestor's theme (for non-UIElement owners — see Phase 1/D1);
  - else `Application.Current.ActualElementTheme` (the app/OS base theme).
- Why recommended: thread-safe (no process-global mutable state), makes the 11 band-aids deletable, and the resolution result is provably identical to "push then resolve". This matches WinUI **behavior** exactly (resolution uses the owner's own theme); it differs only in that the owner's theme travels as an argument instead of via a mutable core slot — an implementation detail with no observable behavior difference.

### Mechanism 2 (WinUI-literal) — keep an ambient slot, push it centrally from `owner.GetTheme()`

- Keep `Themes._requestedThemeForSubTree` but make it an instance-of-walk concept, and push `owner.GetTheme()` **inside** `UpdateThemeReference`/`RefreshValue` (mirroring `Theming.cpp:368`) rather than at 11 call sites.
- Closest to the C++ line-for-line, but retains process-global mutable state and its re-entrancy/threading hazards. Only choose this if Mechanism 1's parameter threading proves impractical in a hot path.

> **Decision (resolved): Mechanism 1, under a hard zero-behavior-difference constraint.** The parameter approach is approved **only if it cannot produce any observable behavior difference** versus today's resolution. This is not a hope — it must be *proven* during migration:
> - While both the legacy global stack and the new `theme` parameter coexist (Phase 3), the centralized `ResolveOwnerTheme(owner)` must, at every in-tree resolution, equal what `GetActiveTheme()` would have returned. Add a transitional `Debug.Assert(ResolveOwnerTheme(owner).Key == GetActiveTheme().Key)` (or a logged divergence counter) at the single resolution choke point, and run the **entire** theming suite + `Given_Theme_Materialization` on Skia and WASM with **zero** assertion hits before the stack is deleted in Phase 4.
> - Any divergence the assert catches is either (a) a real pre-existing bug the parameter fixes — in which case capture it as an intended, test-backed change and document it — or (b) a plumbing mistake to fix. The stack is removed only after the assert is silent across the full suite.
> - The parameter is behavior-identical to WinUI's ambient slot by construction (the slot is itself a dynamically-scoped "theme parameter" pushed/popped via `scope_exit`); the equivalence proof guarantees it is also behavior-identical to *Uno's current* resolution where that is already correct.

### Per-object theme on every DependencyObject (D1)

Move `_theme` + `GetTheme`/`SetTheme` + `IsProcessingThemeWalk` from `UIElement` to **`DependencyObjectStore`** (every `DependencyObject` owns a store), so non-UIElement DOs carry a theme exactly like `CDependencyObject::m_theme`. `Theme` is a small enum (one byte); the per-object cost is negligible. `FrameworkElement`/`UIElement` keep their existing accessors as thin forwarders to the store, so existing call sites compile unchanged.

### Establish theme at `Enter` for every DO (D2)

Port the `EnterImpl` theme block (`depends.cpp:1023-1048`) into Uno's enter/load path so that **every** DO entering the live tree inherits its theme from its logical inheritance parent and resolves its refs — not just `FrameworkElement` on `OnLoadingPartial`, and on **all platforms**, not only enhanced-lifecycle. This is what makes `owner.GetTheme()` reliably non-`None` at first resolution, which makes Mechanism 1 correct without any band-aids.

### `Themes.Active` is retained, but demoted

`Themes.Active` (the app/OS base theme) stays as the **fallback** for `ResolveOwnerTheme` when an owner genuinely has no theme (e.g. resolving a value directly off `Application.Resources` with no element context). It is no longer the *primary* input for in-tree resolution. The static `_requestedThemeForSubTree` **stack is deleted** (Mechanism 1) along with the 11 push sites.

---

## 7. How the target design fixes each issue

- **S1 / S2 / S3 / S5 (materialization leak):** with D1+D2, a virtualized row / recycled tab content / scrolled-in cell / runtime-created control inherits its theme from its (light) parent at `Enter`, and the leaf resolves against that theme — never `Themes.Active`. D4 (stop clearing `_theme` on unload, or re-establish at re-`Enter`) removes the recycle staleness specifically behind S2.
- **S4 (popup first open):** D2 establishes theme for popup/flyout content from its logical parent at `Enter` on **all platforms**; D6 makes `FlyoutBase` forward the placement target's `ActualTheme` (effective theme incl. app/inherited) so a flyout over app-themed content is themed on the **first** open.
- **uno #23177 (app dark switch):** already largely fixed (#23178/#23197); the refactor preserves the fix and removes the special-casing that made it fragile, since resolution now follows `owner.GetTheme()` which the app-theme walk sets correctly.
- **OS/theme leak narrative (S1/S3):** D7 ensures explicit app theme suppresses OS following everywhere and that the active theme does not silently fall back to the dark `Default` sub-dictionary; combined with D2 the only way OS theme reaches an element is if that element actually inherits it.

---

## 8. Scope, platforms, and risk notes

- **Enhanced lifecycle gating.** Today the theme walk is `__SKIA__`/`__WASM__` only. The reported bugs span Skia, WASM **and native iOS/Android (the S4 mobile context-menu case)**. The target design moves the per-DO theme + `Enter` inheritance to a platform-agnostic layer so native participates. Native tree mechanics differ (native view hierarchy); Phase 7 handles native specifically and is the highest-risk phase — keep it last and behind validation.
- **`Foreground` is an inherited DP in Uno but not in WinUL.** Uno emulates WinUI's foreground freeze with `_themeForeground`/`_isForegroundFrozen` (`FrameworkElement.Theming.cs:28-29`, `NotifyThemeChangedForInheritedProperties`). The refactor must preserve this emulation; the freeze path's push (band-aid #2) is replaced by passing the element's theme to the `DefaultTextForegroundThemeBrush` lookup.
- **Compiled-XAML template materialization.** `{ThemeResource}` on a DP emits `ResourceResolverSingleton.Instance.ApplyResource(target, DP, key, isThemeResourceExtension: true, …)` (`XamlFileGenerator.cs:4259-4268`). With D2, the target element's theme is established at `Enter`, so `ApplyResource`/first resolution must defer to (or re-resolve at) `Enter` time — verify the ordering of `ApplyResource` vs `Enter` for both compiled XAML and `XamlReader.Load`.
- **Performance.** The pinned-dictionary mechanism (#22887) and `ThemeWalkResourceCache` are retained; they already solve the "which dictionary" and "dedup lookups" problems. The change is only the *theme input*, so no new tree walks are introduced; if anything, removing band-aid pushes reduces work.

---

## 9. Key WinUI source map (for implementing agents)

| Concern | File:line |
|---------|-----------|
| `Theme` enum + masks | `dxaml\xcp\components\theming\inc\Theme.h:10-65` |
| `m_theme` on every DO | `dxaml\xcp\core\inc\CDependencyObject.h:1761`; getter `:1648` |
| `NotifyThemeChanged` walk | `dxaml\xcp\components\DependencyObject\Theming.cpp:110-157` |
| `NotifyThemeChangedCoreImpl` | `Theming.cpp:166-255` |
| `UpdateAllThemeReferences` / `UpdateThemeReference` | `Theming.cpp:260-346` |
| `SetThemeResourceBinding` (central owner-theme push) | `Theming.cpp:349-400` |
| Visual-tree recursion | `dxaml\xcp\core\core\elements\uielement.cpp:14469-14495` |
| `GetRequestedThemeOverride` | `dxaml\xcp\core\core\elements\framework.cpp:3268-3288` |
| FE `NotifyThemeChangedCore` + freeze + event | `framework.cpp:3297-3317` |
| `NotifyThemeChangedForInheritedProperties` (foreground freeze) | `framework.cpp:3385-3476` |
| `OnRequestedThemeChanged` | `framework.cpp:3485-3543` |
| `ActualTheme` getter | `framework.cpp:3953-3978` |
| `RaiseActiveThemeChangedEventIfChanging` | `framework.cpp:3330-3370` |
| `GetInheritanceParentInternal` (logical parent) | `framework.cpp:3097-3130` |
| `EnterImpl` theme block | `dxaml\xcp\core\core\elements\depends.cpp:1023-1048` |
| `CThemeResource` / `RefreshValue` | `dxaml\xcp\components\theming\ThemeResource.cpp:63-169` |
| `CThemeResourceExtension` | `dxaml\xcp\core\core\elements\ThemeResourceExtension.cpp` |
| `ThemeResourceExpression` | `dxaml\xcp\dxaml\lib\ThemeResourceExpression.cpp:153-201` |
| Theme-aware dictionary lookup | `dxaml\xcp\core\core\elements\Resources.cpp:386-512, 644-685` |
| `EnsureActiveThemeDictionary` (theme→sub-dict) | `Resources.cpp:687-819` |
| `ThemeWalkResourceCache` | `dxaml\xcp\components\theming\ThemeWalkResourceCache.{h,cpp}` |
| `FrameworkTheming::GetTheme` (app/OS precedence) | `dxaml\xcp\components\theming\FrameworkTheming.cpp:119-136` |
| `SetRequestedTheme` / `OnThemeChanged` | `FrameworkTheming.cpp:31-105` |
| OS/system theme + HC detection | `dxaml\xcp\components\theminginterop\SystemThemingInterop.cpp:33-303` |
| `FrameworkApplication.put_RequestedTheme` | `dxaml\xcp\dxaml\lib\FrameworkApplication_Partial.cpp:981-1055` |
| Top-level `NotifyThemeChange` orchestration | `dxaml\xcp\core\dll\xcpcore.cpp:7771-7898` |
| OS-change observation | `JupiterWindow.cpp:1543-1559`, `JupiterControl.cpp:674-1093`, `DXamlCore.cpp:1284-1295` |
