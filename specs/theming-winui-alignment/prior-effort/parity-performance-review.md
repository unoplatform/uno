# Theming Rework Parity + Performance Report — `dev/mazi/theming-winui` vs WinUI C++

## 1. Overall verdict

The rework is a **high-fidelity 1:1 port** of WinUI's per-DependencyObject theming model. The hardest and most consequential pieces are faithful: the per-DO theme field (`_theme` ↔ WinUI `m_theme`) established at tree Enter and never cleared on Leave; the snapshot→clear→re-apply pattern in `UpdateAllThemeReferences`; the ancestor-walk-then-pinned-dictionary refresh order in `UpdateThemeReference`; the parse-time providing-dictionary pin (`CThemeResource::SetInitialValueAndTargetDictionary`); the theme sub-dictionary selection precedence in `EnsureActiveThemeDictionary`; the `ThemeWalkResourceCache` semantics; and the re-entrancy guards. The genuinely *new* design decision — resolving `{ThemeResource}` against the owner's own established theme (threaded via `ownerThemeOverride`) instead of a process-global active theme — is the correct analog of WinUI pushing `m_theme` onto `RequestedThemeForSubTree`, and is a real correctness improvement over the prior global-stack model. Several adversarially-checked "gaps" were correctly **refuted** (notably the claimed `ResolveOwnerTheme` O(depth) cost, which is O(1) in steady state because `_theme` is established at Enter), which raises confidence in the analysis discipline.

The confirmed divergences are mostly **mechanism differences with converging end-state** (popup theme pinning, HC bits dropped then re-OR'd at the leaf, ordering inversion at Enter) plus a small set of **real correctness edges** (missing `IsBaseThemeChanging` disjunct for first-walk `ActualThemeChanged`, no out-of-tree theme-change listeners, shared-dictionary owner mis-attribution) and **real new per-Enter/per-theme-change overhead** introduced by the lack of WinUI's `CEnterDependencyProperty` metadata. No confirmed gap produces a clearly wrong user-visible color in the common path; the most material item is a structural over-walk that is a perf cost, not a correctness bug.

## 2. What matches WinUI 1:1

**Per-DO theme + Enter establishment**
- Per-DO `_theme`, only set in `SetTheme` paths, never cleared on Leave — `DependencyObjectStore.Theming.cs:41,52` ↔ `Theming.cpp:155`.
- Theme established at Enter from the logical inheritance parent for FE, visual parent for non-FE — `DependencyObjectStore.Theming.cs:104-107` ↔ `depends.cpp:1027-1037`.
- Inherit-vs-`UpdateAllThemeReferences` branch — `DependencyObjectStore.Theming.cs:126-147` ↔ `depends.cpp:1039-1047`.
- Live-UIElement skip during the DP value walk — `DependencyObjectStore.Theming.cs:304` ↔ `Theming.cpp:218,242`.
- `IsProcessingThemeWalk` re-entrancy guard — `FrameworkElement.Theming.cs:170-173` ↔ `Theming.cpp:119-122`.
- Element-level Enter theming gated to enhanced-lifecycle (Skia/WASM) — `UIElement.mux.cs:1130-1139` (intentional native divergence).

**Runtime theme-change walk + foreground freeze**
- Walk order (freeze → set theme → update bindings → recurse children → raise event last) — `FrameworkElement.Theming.cs:214-288` ↔ `framework.cpp:3297-3317` + `uielement.cpp:14469-14495`.
- `GetRequestedThemeOverride` per-element override — `FrameworkElement.Theming.cs:180-183` ↔ `framework.cpp:3268-3288`.
- `themeChanged` via `GetBaseValue`, first-walk `None` handling, `effectiveThemeChanged` base-substitution — `FrameworkElement.Theming.cs:217-232` ↔ `framework.cpp:3363-3369`.
- Foreground-freeze emulation via inherited DP + `_themeForeground`/`_isForegroundFrozen` — `FrameworkElement.Theming.cs:240-265,396-465` ↔ `framework.cpp:3385-3476`.
- `ActualTheme` getter app-base fallback, never `Default` — `FrameworkElement.Theming.cs:133-153` ↔ `framework.cpp:3953-3978`.

**ThemeResource resolution**
- Snapshot+clear+re-apply, cascading-removal guard — `DependencyObjectStore.Theming.cs:369-398,432` ↔ `Theming.cpp:260-311`.
- Ancestor walk then pinned-dict `RefreshValue` ordering — `DependencyObjectStore.Theming.cs:453-479` ↔ `Theming.cpp:315-346`.
- Sticky parse-time providing-dictionary pin — `ResourceResolver.cs:342-364` ↔ `ThemeResource.cpp:39-51`.
- Dead-dictionary fallback to last value — `ThemeResourceReference.cs:113-153` ↔ `ThemeResource.cpp:63-129`.
- Owner-theme choke point (`ownerThemeOverride`) replacing global active theme — `DependencyObjectStore.Theming.cs:443-451` ↔ `Theming.cpp:364-379`.

**ResourceDictionary selection**
- HC variant precedence + base + `Default` fallback — `ResourceDictionary.cs:668-686` ↔ `Resources.cpp:687-819`.
- Active-theme-dictionary caching with dual invalidation — `ResourceDictionary.cs:644-661` ↔ `Resources.cpp:699-704`.
- Merged-dictionary last-wins precedence — `ResourceDictionary.cs:423-438` ↔ `Resources.cpp:461-473`.
- Key-not-found cache + propagating invalidation — `ResourceDictionary.cs:283-296,1046-1083` ↔ `Resources.cpp:414-430,2234-2278`.

**App/OS/HighContrast precedence**
- Explicit app theme suppresses OS — `Application.cs:239-245,372-379` ↔ `FrameworkTheming.cpp:119-124`.
- Theme enum bit layout byte-for-byte — `Theming.cs:8-28` ↔ `Theme.h:10-40`.
- HC OR-ed onto base at the leaf — `ThemeResolution.cs:41,51,57` ↔ `FrameworkTheming.cpp:123`.

**Popup/Flyout forwarding** — clean: override-only-when-Default guard (`FlyoutBase.cs:773-778`), latch + dual presenter/popup set (`:785-792`), runtime push via `Popup.NotifyThemeChangedCore` (`Popup.cs:164-172`), logical-parent inheritance with PopupRoot kept theme-`None` (`DependencyObjectStore.Theming.cs:104-107`). All 1:1.

**ThemeWalkResourceCache** — clean: key shape (`:47`), weak value (`:149`), base-theme normalization (`:115`), re-entrancy no-op (`:63`), session-end clear (`:85`), guarded reads/writes (`:109`), `RemoveCacheEntry` keyed by resource-key-only (`:163`). All 1:1.

## 3. Confirmed gaps (isReal=true), by severity

### MEDIUM — `ActualThemeChanged` missing on first-walk elements during app/system theme change
- **WinUI**: fires when `(oldBase != newBase) || (oldTheme==None && IsBaseThemeChanging())` — `framework.cpp:3364-3369`; `IsBaseThemeChanging` is true for the whole app/system switch (`FrameworkTheming.cpp:53,98`). Also delivers to **out-of-tree** listeners via `AddThemeChangedListener`/`NotifyThemeChangedListeners` (`framework.cpp:3321-3328,3987-3998`).
- **Uno**: only the first disjunct — `FrameworkElement.Theming.cs:230-232`. Because `Application.cs:499` sets the new theme *before* the walk, a never-walked element (`oldTheme==None`) has `appBaseTheme==newBase`, so the event is suppressed. No out-of-tree listener mechanism exists (Grep confirms zero `AddThemeChangedListener` references).
- **Consequence**: an element added but not yet theme-walked, with an `ActualThemeChanged` handler, misses the event on the first app/system switch; out-of-tree listeners never get it.
- **Fix**: thread an `isBaseThemeChanging` flag from `Application.OnRequestedThemeChanged`/`OnSystemThemeChanged` into the walk and OR it into `effectiveThemeChanged` when `oldTheme==None`. Port a minimal out-of-tree listener registry if any control relies on it.

### MEDIUM — ThemeResource Phase A ancestor walk gated on `IsLoaded`, not `IsActive()`
- **WinUI**: runs `FindNextResolvedValueNoRef` whenever `IsActive()` — `Theming.cpp:321` — i.e. during Enter, before Loaded, for any `CDependencyObject`.
- **Uno**: gates on `owner is FrameworkElement { IsLoaded: true }` — `DependencyObjectStore.Theming.cs:456` (confirmed). `IsActiveInVisualTree` is set in `EnterImpl` (`UIElement.mux.cs:1044-1046`) strictly before `RaiseLoaded` (deferred a tick). Non-FE active DOs never get Phase A at all.
- **Consequence**: an active-but-not-yet-loaded element (the exact moment `EstablishThemeAtEnter` runs, and after reparenting before Loaded re-fires) skips the ancestor walk and uses the pinned dict only. Parse-time pin masks locally-declared keys; diverges only when an ancestor dictionary in the *new* tree should now win after reparenting.
- **Fix**: gate Phase A on `IsActiveInVisualTree`/`IsInLiveTree` instead of `IsLoaded`, matching WinUI's `IsActive()` window; allow non-FE active DOs through.

### MEDIUM — Owner backref repurposed for THEME selection (WinUI uses it only for lookup/implicit-style)
- **WinUI**: `GetResourceOwnerNoRef` feeds only (a) unresolved-key lookup continuation (`ResourceResolver.cpp:767-774`) and (b) implicit-style notify (`ResourceDictionary.cpp:189-204`); theme comes from each DO's own `m_theme` pushed in `SetThemeResourceBinding` (`Theming.cpp:368-376`).
- **Uno**: `GetResourceOwner` (`ResourceDictionary.cs:954-965`) feeds `ResolveOwnerTheme` for lazy materialization (`:552-560`) and `UpdateThemeBindings` (`:967-991`) — a second theme source parallel to the per-DO `_theme`.
- **Consequence**: two theme sources can disagree for a resource DO whose own theme differs from its hosting element's theme.
- **Fix**: prefer the consuming DO's own established `_theme` for resolution; keep the owner backref for lookup escalation only, as WinUI does.

### MEDIUM — Shared/merged dictionary mis-attributed to one owner's theme (last-writer-wins)
- **WinUI**: also last-writer-wins on the owner field (`framework.cpp:343,362`), but it does **not** theme via the owner — each consuming DO carries `m_theme`, so a shared brush themes correctly through every element's walk.
- **Uno**: `FrameworkElement.Resources` setter calls `SetResourceOwner(this)` unconditionally (`FrameworkElement.cs:273-278`); a dictionary instance shared by two elements with different `RequestedTheme` resolves all consumers' `{ThemeResource}` against the last owner's theme (`ResourceDictionary.cs:947-965`).
- **Consequence**: a `SolidColorBrush` in a shared dictionary, materialized once, can be wrong-theme for one of two elements with differing `RequestedTheme`.
- **Fix**: do not rely on the single owner field for theme; resolve shared-dictionary brushes against each consuming DO's `_theme` (ties into the previous gap).

### LOW — Enter ordering inverted (Uno themes self then walks DP children; WinUI walks children first)
- WinUI: child Enter (and each child's theme block) runs before the parent's theme block — `depends.cpp:992-1013` then `:1023-1048`. Uno: inherit/notify at `DependencyObjectStore.Theming.cs:126-147` then DP walk at `:162`. End state converges (child theme == parent theme); only logical-only not-yet-visual children differ in timing. **Fix**: none required; documented divergence.

### LOW — `_isPropagatingThemeEnter` is per-call, not WinUI's per-object `IsProcessingEnterLeave`
- WinUI sets `IsProcessingEnterLeave` on the object across the whole nested Enter (`depends.cpp:777-794`), breaking cross-object DP cycles. Uno's flag (`DependencyObjectStore.Theming.cs:165,195-200,287`) only guards this store; a 2-object DP cycle of non-active DOs terminates only via the `parentTheme==_theme` short-circuit (`:126`). **Fix**: only if a real cycle is observed — promote the guard to per-object.

### LOW — Phase B `RefreshValue` lacks WinUI's `IsValueFromInitialTheme`/themeWalk gate
- WinUI defers the pinned-dict refresh until first theme change when `m_theme==None && IsValueFromInitialTheme()` (`Theming.cpp:338-343`). Uno always refreshes (`:476-479`) because `ResolveOwnerTheme` never returns `None`. Same resolved value; a laziness/perf divergence (one extra lookup on the initial pass). **Fix**: optional — gate on `IsResolved` to mirror WinUI's deferral.

### LOW — HighContrast collapsed to a boolean; White/Black/Custom lost on the global dimension
- WinUI reads the real OS HC variant (`SystemThemingInterop.cpp:121-174`) and ORs the exact variant. Uno reduces to `IsHighContrast` bool (`SystemThemeHelper.cs:39`), re-derives the variant at the leaf from the *base* theme (`ResourceDictionary.cs:685-686`). **Consequence**: OS HC-White + app base Dark resolves to HC-Black; `HighContrastCustom` unrepresented. Acknowledged as follow-up in the doc comment (`ResourceDictionary.cs:663-667`). **Fix**: carry the real OS HC variant end-to-end.

### LOW — Other confirmed-but-benign
- `HighContrastChanged` never raised from the walk; pure HC toggle raises neither event (`FrameworkElement.Theming.cs:227-232`) — HC resource selection still works at the leaf.
- HC bits dropped when an element has explicit `RequestedTheme` (`FrameworkElement.Theming.cs:180-183`) — re-OR'd at the leaf.
- No theme-switch suppression while in HC; WinUI skips notify (`FrameworkTheming.cpp:87-101`), Uno walks unconditionally (`Application.cs:495-512`) — redundant work, correct result.
- Lazy materialization re-resolve gated on owner theme `!= None`, skipping ownerless app/system dicts (`ResourceDictionary.cs:552-560`) — corrected by the separate `UpdateThemeBindings` walk.
- Popup presenter pinned to explicit Light/Dark where WinUI leaves `Default` (`FlyoutBase.cs:783-789`) — compensated by `OnPlacementTargetActualThemeChanged` re-forward; mechanism difference only.
- `m_isFlyoutPresenterRequestedThemeOverridden` never reset (`FlyoutBase.cs:788`) — **parity-correct**; WinUI's latch is also sticky (`FlyoutBase_partial.cpp:1583`).
- Legacy binding `OnThemeChanged`/`RefreshTarget` value-source gate absent and reference-equality vs WinUI's `AreEqual` (`Bindings.cs:269-294`) — over-fires Reapply on distinct-but-equal boxed value types; small impact (most themed values are cached Brush reference types). See perf #4.
- `ThemeWalkResourceCache` holds the dictionary as a strong key vs WinUI's raw pointer (`ThemeWalkResourceCache.cs:47`) — Uno is *safer* (no dangling pointer), at the cost of bounded extra lifetime cleared at walk end.
- `CloneForTarget` shares the source dictionary weakref without re-pinning to target scope (`ThemeResourceReference.cs:242-243`) — only surfaces if the control's local scope shadows the key.

## 4. Performance analysis (isReal=true), by severity

> **NEW overhead introduced by the rework** is flagged with ⚠️. The dominant new cost is the absence of a `CEnterDependencyProperty` metadata table, which forces a full property scan at every Enter.

### ⚠️ HIGH — `PropagateThemeEnterToDPPropertyValues` walks ALL DP values on every DO Enter
- **Hot path**: `EstablishThemeAtEnter` → `PropagateThemeEnterToDPPropertyValues` (`DependencyObjectStore.Theming.cs:162,193-289`), invoked from `UIElement.EnterImpl` (`UIElement.mux.cs:1136-1139`) for **every DO going live**.
- **Cost**: iterates `_properties.GetAllDetails()` (`:203`), calls `GetValue` per non-skipped entry (`:230`, only `DataContext`/`CommandParameter`×2/`ItemsSource`-by-name skipped), and enumerates **any** non-string `IEnumerable` value with a per-collection try/catch (`:252-273`). WinUI walks only the curated `CEnterDependencyProperty` list (`depends.cpp:993-1011`). This is O(total realized DP slots) + `GetValue` materialization per Enter vs WinUI's O(enter-properties); `GetValue` can fire coercion WinUI never touches, and arbitrary user `IEnumerable` DPs get enumerated.
- **Frequency**: once per element materialization / popup-flyout open / page load — the hottest tree-construction path.
- **Mitigation**: introduce an Enter-property allowlist (analog of `CEnterDependencyProperty`) tagging the handful of DPs that carry logical-tree children (`Child`, `Content`, `Items`, `PrimaryCommands`, `Flyout`, `Resources`). Short of that, skip details whose `Property.Type` is not assignable to `DependencyObject`/`IEnumerable` before calling `GetValue`, and restrict enumeration to `IEnumerable<DependencyObject>` backed by `ICollection`/`DependencyObjectCollectionBase` — exactly the gate the sibling `InnerUpdateChildResourceBindings` already applies (`:637-640`).

### ⚠️ MEDIUM — Two full passes over `_bindings` per theme change (gated + ungated)
- **Hot path**: per themed DO, `UpdateResourceBindings` runs `UpdateBindingExpressions` (value-gated Reapply, `Bindings.cs:248-262`, from `Theming.cs:741`) **and** Phase 3 `OnThemeChanged` → `RefreshBindingValueIfNecessary` → `RefreshTarget` (ungated, `Bindings.cs:296-316`, from `Theming.cs:607`). The Phase 3 gate `(reason & PropagatesThroughTree)` passes on every ThemeResource change (`ResourceUpdateReason.cs:35`).
- **Cost**: second pass re-runs `RefreshTarget`→converters for every binding merely *carrying* a `*ThemeResource` key, including ones the first pass already determined unchanged — the exact "blindly refreshing every Binding" anti-pattern WinUI rejects (`BindingExpression_Partial.cpp:2516-2520`). New: the legacy path was left wired alongside the new gated path.
- **Frequency**: full-tree walk on every app/RequestedTheme toggle.
- **Mitigation**: remove the legacy `RefreshBindingValueIfNecessary`/`OnThemeChanged` binding refresh, or have it delegate to the same value-gated `UpdateBindingPropertiesFromThemeResources` result so each binding refreshes at most once and only when its resolved value changed. Add a `m_bindingValueSource` analog so only the in-use slot triggers.

### ⚠️ MEDIUM — `ResolveOwnerTheme` not hoisted on the Enter path → N ancestor walks per DO
- **Hot path**: `EstablishThemeAtEnter` calls `UpdateAllThemeReferences(owner, cache:null)` with **no** `ownerThemeOverride` (`Theming.cs:139,146`), so `UpdateThemeReference` computes `ResolveOwnerTheme(owner)` per property entry (`:450`). For a DO with N theme refs that is N ancestor climbs per Enter.
- **Note**: the *theme-change* path passes a precomputed override (`:716-717`) and is fine; only Enter is uncovered. (The blanket "O(depth) per resolution" claim was **refuted** — in steady state `_theme` is established at Enter so `ResolveOwnerTheme` is O(1).)
- **Mitigation**: `EstablishThemeAtEnter` already knows the just-established `_theme`; pass it as `ownerThemeOverride` so the walk runs once per DO, not once per property.

### MEDIUM — `GetResourceDictionaries(...).ToArray()` per element on every theme change
- **Hot path**: `UpdateResourceBindings` allocates a `ResourceDictionary[]` at `Theming.cs:727` (and `??=` reuse at `:755`) for any element with bindings/resource bindings; `GetResourceDictionaries` is a yield-iterator walking the parent chain (`DependencyObjectStore.cs:1491-1525`). WinUI uses a `stack_vector` with no per-element scope array.
- **Frequency**: entire themed subtree on an app theme toggle.
- **Cost**: one heap array + O(depth) walk per binding-bearing element. Also gated only by `HasBindings` (true for *any* binding), so it allocates even when no binding carries a `TargetNull`/`Fallback` ThemeResource.
- **Mitigation**: pre-check whether any binding has a `*ThemeResource` key before building/pushing scope; iterate the enumerable directly or cache the scope array on the store, invalidating on tree changes.

### MEDIUM — `PropagateThemeToChildren` uses `VisualTreeHelper.GetChildrenCount`/`GetChild(i)` (O(n²))
- **Hot path**: `FrameworkElement.Theming.cs:296-308`, for every element in the subtree on every theme switch.
- **Cost**: `VisualTreeHelper.GetChild` is `GetChildren().Where(...).ElementAtOrDefault(i)` and `GetChildrenCount` is `GetChildren().Count(...)` (`VisualTreeHelper.cs:116-144`) — each index re-enumerates the children through a LINQ chain, giving O(n²) + per-index allocations for a wide panel. (WinUI's indexed `GetItemWithAddRef(i)` is genuinely O(1).)
- **Mitigation**: enumerate the children collection once directly (`GetChildren()`/`_children`) instead of indexed `Count`+`GetChild`.

### ⚠️ MEDIUM — `UpdateAllThemeReferences` allocates a fresh snapshot array per call
- **Hot path**: `Theming.cs:379-385` allocates `new ThemeResourceMap.Entry[snapshotCount]` (full structs) on every call — per theme change **and** per Enter (via `EstablishThemeAtEnter`). WinUI uses `stack_vector<KnownPropertyIndex,50>` and snapshots only the index. Templates like `ListViewItemPresenter`/`CalendarView` carry 40+ refs each.
- **Mitigation**: pooled/thread-static scratch buffer sized ~50, or stackalloc `Span<Entry>` for the small-count case; snapshot only `(property,precedence)`.

### ⚠️ HIGH — `ThemeWalkResourceCache` reflection/boxing on every cache probe
- **Hot path**: `TryGetCachedValue` (`:116`) and `CacheValue` (`:147-149`) key on `(ResourceDictionary, Theme, ResourceKey)` ValueTuple with the **default** comparer. `ResourceKey` (confirmed via Grep) has no `IEquatable`/`GetHashCode`/`Equals(object)` override — only `Equals(in ResourceKey)` and a `HashCode` field. Its reference-type fields (`string Key`, `Type TypeKey`) force the slow reflection-based `ValueType.GetHashCode`/`ValueType.Equals` path with boxing.
- **Frequency**: once per `{ThemeResource}` resolution per element during the theme/Enter walk (WinUI header cites ~69k lookups). The cache built to *spare* lookups pays a reflection+boxing tax on every hit and miss.
- **Mitigation**: make `ResourceKey` implement `IEquatable<ResourceKey>` (forward to `Equals(in ResourceKey)`) and override `GetHashCode()` to return `(int)HashCode`, **or** construct `_cache` with a custom `IEqualityComparer` delegating to `ResourceKeyComparer.Default`. Highest ROI of all perf items.

### LOW — confirmed minor
- Duplicate `IThemeChangeAware.OnThemeChanged()` per walk for RequestedTheme elements (`FrameworkElement.Theming.cs:399-402` + `DependencyObjectStore.Theming.cs:601-607`) — move to a single choke point.
- Reapply re-runs full `ApplyBinding` (subscription teardown + closure alloc, `BindingExpression.cs:324,566-634`) rather than just re-pushing the fallback constant.
- Indiscriminate `IEnumerable` enumeration (part of the HIGH Enter item; `Theming.cs:252-274`).
- Full visual-tree walk on every theme change with no HC short-circuit (`Application.cs:507-633`) — perf face of the HC-suppression gap.
- `RemoveCacheEntry` full scan + `List` alloc per mutation (`ThemeWalkResourceCache.cs:171`); `GetResourceOwner` parent-walk repeated across merged/source recursion (`ResourceDictionary.cs:972,982-990`); triple `ResolveOwnerTheme` per `UpdateResourceBindings` (`:716,740,762`); WeakReferencePool rent/return churn (`ThemeWalkResourceCache.cs:202`).

**Refuted (do not action)**: `ResolveOwnerTheme` per-resolution O(depth) (it's O(1) at steady state — `_theme` set at Enter); `PropagateResourcesChanged` per-node enumerator allocation (the allocation-free `Panel.Children` path is taken first; Skia/Android fallbacks don't materialize lists); `Phase A GetResourceDictionaries` per-ref is real but only hits loaded FEs, not Enter.

## 5. Top recommendations (prioritized)

1. **Add `IEquatable<ResourceKey>` + `GetHashCode` override (or a tuple comparer delegating to `ResourceKeyComparer.Default`).** Removes reflection+boxing from the single hottest theming path (~tens of thousands of probes per walk). Smallest change, largest win. — `SpecializedResourceDictionary.cs:28-104`, consumed at `ThemeWalkResourceCache.cs:116,147`.
2. **Introduce an Enter-property allowlist (Uno's `CEnterDependencyProperty` analog).** Cuts the full per-Enter DP scan + `GetValue` materialization + arbitrary `IEnumerable` enumeration down to the curated child-carrying DPs. Biggest tree-construction win and removes the principal correctness divergence (side-effecting `GetValue`/enumeration WinUI never performs). — `DependencyObjectStore.Theming.cs:193-289`.
3. **De-duplicate the binding theme-refresh.** Remove the legacy ungated `OnThemeChanged`/`RefreshTarget` path or route it through the value-gated `UpdateBindingPropertiesFromThemeResources`; this fixes both the converter-side-effect correctness hazard and the double-pass cost. Consider a `m_bindingValueSource` analog. — `Bindings.cs:248-316`, `Theming.cs:607,741`.
4. **Fix the first-walk `ActualThemeChanged` gap.** Thread an `isBaseThemeChanging` signal from `Application.OnRequestedThemeChanged`/`OnSystemThemeChanged` into the walk and OR it into `effectiveThemeChanged` when `oldTheme==None`. Evaluate whether any control needs out-of-tree listener delivery. — `FrameworkElement.Theming.cs:230-232`, `Application.cs:499-558`.
5. **Widen the ThemeResource Phase A gate from `IsLoaded` to `IsActiveInVisualTree`** (and allow non-FE active DOs) to match WinUI's `IsActive()` window, fixing the reparent-before-Loaded ancestor-resolution gap. — `DependencyObjectStore.Theming.cs:456`.
6. **Resolve shared/merged-dictionary brushes against each consuming DO's `_theme`, not the single owner backref.** Closes both the dual-theme-source and shared-dictionary mis-attribution gaps; aligns the owner field with WinUI's lookup-only role. — `ResourceDictionary.cs:552-560,947-991`.
7. **Cheap perf hygiene (batch):** pass the just-established `_theme` as `ownerThemeOverride` from `EstablishThemeAtEnter`; pool the `UpdateAllThemeReferences` snapshot buffer; replace indexed `GetChild` with single-pass child enumeration in `PropagateThemeToChildren`; pre-check binding keys before `GetResourceDictionaries().ToArray()`; compute `ResolveOwnerTheme` once per `UpdateResourceBindings`.
8. **Defer / document:** full OS HighContrast variant (White/Black/Custom) propagation and HC theme-switch suppression are real but low-severity and already noted as follow-ups; leave as tracked downstream work.

Relevant files (all absolute):
- `D:\Work\uno-worktrees\theming\src\Uno.UI\UI\Xaml\DependencyObjectStore.Theming.cs`
- `D:\Work\uno-worktrees\theming\src\Uno.UI\UI\Xaml\FrameworkElement.Theming.cs`
- `D:\Work\uno-worktrees\theming\src\Uno.UI\UI\Xaml\ResourceDictionary.cs`
- `D:\Work\uno-worktrees\theming\src\Uno.UI\UI\Xaml\ThemeResolution.cs`
- `D:\Work\uno-worktrees\theming\src\Uno.UI\UI\Xaml\ThemeWalkResourceCache.cs`
- `D:\Work\uno-worktrees\theming\src\Uno.UI\UI\Xaml\SpecializedResourceDictionary.cs`
- `D:\Work\uno-worktrees\theming\src\Uno.UI\UI\Xaml\DependencyPropertyDetailsCollection.Bindings.cs`
- `D:\Work\uno-worktrees\theming\src\Uno.UI\UI\Xaml\Application.cs`
- `D:\Work\uno-worktrees\theming\src\Uno.UI\UI\Xaml\Controls\Flyout\FlyoutBase.cs`
- `D:\Work\uno-worktrees\theming\src\Uno.UI\UI\Xaml\Data\ThemeResourceReference.cs`
- `D:\Work\uno-worktrees\theming\src\Uno.UI\UI\Xaml\ResourceResolver.cs`