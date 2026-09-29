# ViewManager + Viewport + VirtualizationInfo Comparison Report

**WinUI commit:** 4b206bce3
**Types covered:** ViewManager, ViewportManager, ViewportManagerDownlevel, ViewportManagerWithPlatformFeatures, VirtualizationInfo

## Summary

| Type | Critical | High | Medium | Low | Info |
|------|---------:|-----:|-------:|----:|-----:|
| ViewManager                          | 3 | 5 | 6 | 4 | 3 |
| ViewportManager (abstract)           | 0 | 1 | 2 | 1 | 1 |
| ViewportManagerDownlevel             | 0 | 2 | 4 | 4 | 2 |
| ViewportManagerWithPlatformFeatures  | 1 | 4 | 6 | 5 | 4 |
| VirtualizationInfo                   | 0 | 1 | 2 | 1 | 1 |
| **Total**                            | **4** | **13** | **20** | **15** | **11** |

Severity legend:
- **Critical**: behavior diverges in a way that produces wrong results, may crash, or silently mutates user data.
- **High**: parity gap or behavior change that user code can reach (visibility, missing feature, wrong arg parsing).
- **Medium**: structural divergence from WinUI (method order, field order, missing TODO Uno, dropped comments).
- **Low**: stylistic / cosmetic / minor (extra `using`, comments substituting `→` for `.`, dropped `#pragma region`).
- **Info**: observed but justified or already-tagged Uno divergences (logged for traceability).

---

## Per-type sections

### ViewManager

#### Method order verification (.cpp top-to-bottom vs ViewManager.mux.cs)

| # | C++ method                          | C# location (line) | Order matches |
|---|--------------------------------------|--------------------|----------------|
| 1 | Constructor                          | L18                | yes |
| 2 | GetElement                           | L31                | yes |
| 3 | ClearElement                         | L87                | yes |
| 4 | RecycleWithoutOwner                  | L128               | yes |
| 5 | ClearElementToElementFactory         | L142               | yes |
| 6 | MoveFocusFromClearedIndex            | L210               | yes |
| 7 | FindFocusCandidate                   | L237               | yes |
| 8 | GetElementIndex                      | L309               | yes |
| 9 | PrunePinnedElements                  | L320               | yes |
| 10 | UpdatePin                           | L344               | yes |
| 11 | OnItemsSourceChanged                | L377               | yes |
| 12 | EnsureFirstLastRealizedIndices      | L546               | yes |
| 13 | OnLayoutChanging                    | L555               | yes |
| 14 | OnOwnerArranged                     | L564               | yes |
| 15 | GetElementIfAlreadyHeldByLayout     | L592               | yes |
| 16 | GetElementFromUniqueIdResetPool     | L636               | yes |
| 17 | GetElementFromPinnedElements        | L659               | yes |
| 18 | GetElementFromElementFactory        | L700               | yes |
| 19 | ClearElementToUniqueIdResetPool     | L849               | yes |
| 20 | ClearElementToAnimator              | L860               | yes |
| 21 | ClearElementToPinnedPool            | L879               | yes |
| 22 | UpdateFocusedElement                | L903               | yes |
| 23 | OnFocusChanged                      | L957               | yes |
| 24 | EnsureEventSubscriptions            | L962               | yes |
| 25 | UpdateElementIndex                  | L975               | yes |
| 26 | InvalidateRealizedIndicesHeldByLayout | L985             | yes |
| 27 | PinnedElementInfo ctor              | (struct in .h.mux.cs) | moved to .h.mux.cs |

Method ordering is fully consistent with WinUI.

#### Field verification (.h vs ViewManager.h.mux.cs)

| C++ field                                   | C# field                                 | Match |
|---------------------------------------------|------------------------------------------|-------|
| `ItemsRepeater* m_owner`                    | `ItemsRepeater m_owner`                  | yes |
| `std::vector<PinnedElementInfo> m_pinnedPool` | `List<PinnedElementInfo> m_pinnedPool` | yes |
| `UniqueIdElementPool m_resetPool`           | `UniqueIdElementPool m_resetPool`        | yes |
| `tracker_ref<UIElement> m_lastFocusedElement` | `UIElement m_lastFocusedElement`       | yes |
| `bool m_isDataSourceStableResetPending{}`   | `bool m_isDataSourceStableResetPending`  | yes |
| `bool m_recycleWithoutOwner{false}`         | `bool m_recycleWithoutOwner`             | yes |
| `GotFocus_revoker m_gotFocus{}`             | `SerialDisposable m_gotFocus = new`      | yes (matches new revoker pattern) |
| `LostFocus_revoker m_lostFocus{}`           | `SerialDisposable m_lostFocus = new`     | yes (matches new revoker pattern) |
| `::Phaser m_phaser`                          | `Phaser m_phaser`                        | yes |
| `tracker_ref m_ElementFactoryGetArgs`        | `ElementFactoryGetArgs m_ElementFactoryGetArgs` | yes |
| `tracker_ref m_ElementFactoryRecycleArgs`    | `ElementFactoryRecycleArgs m_ElementFactoryRecycleArgs` | yes |
| `int m_firstRealizedElementIndexHeldByLayout` | same                                    | yes |
| `int m_lastRealizedElementIndexHeldByLayout` | same                                     | yes |
| `static constexpr int FirstRealizedElementIndexDefault` | `const int FirstRealizedElementIndexDefault = int.MaxValue` | yes |
| `static constexpr int LastRealizedElementIndexDefault`  | `const int LastRealizedElementIndexDefault = int.MinValue`  | yes |

#### Findings

##### CRITICAL — ViewManager constructor initializes `m_lastFocusedElement = owner`

- **C++ (.cpp:11-20)**: `m_lastFocusedElement(owner)` — this is a `tracker_ref<winrt::UIElement>` ctor which takes the owning `ITrackerHandleManager*` and **leaves the held element null**. No element is actually stored.
- **C# (ViewManager.mux.cs:25)**: `m_lastFocusedElement = owner;` — directly assigns the `ItemsRepeater` to the field declared as `UIElement m_lastFocusedElement`.
- **Issue**: This makes `m_lastFocusedElement` non-null and equal to the owner before any focus event happens. Downstream tests `if (m_lastFocusedElement)` and `if (m_lastFocusedElement == element)` will behave incorrectly:
  - `ClearElementToElementFactory` / `ClearElementToAnimator` compare `m_lastFocusedElement == element` to decide if focus needs to move. Since the field is the owner repeater, never an element, the check works, but...
  - `MoveFocusFromClearedIndex` does `if (m_lastFocusedElement) { ... focusedAsControl.FocusState }`. With non-null owner the code path runs unconditionally; in WinUI it would short-circuit and use `FocusState.Programmatic`.
- **Suggested fix**: Set `m_lastFocusedElement = null;` (or remove the line) in the constructor.

##### CRITICAL — `ClearElementToAnimator` uses `Focus(focusState)` instead of `FocusNoActivate`

- **C++ (.cpp:240)**: `focusCandidate.as<winrt::IUIElementPrivate>().FocusNoActivate(focusState);`
- **C# (ViewManager.mux.cs:224)**: `focusCandidate.Focus(focusState);`
- **Issue**: WinUI uses an internal `FocusNoActivate` because the move is initiated by element recycle and must not activate the host window. Using `Focus` may cause an unwanted window activation, especially on Windows hosts.
- **Suggested fix**: Either map to `Uno.UI.IUIElementPrivate.FocusNoActivate` if available, or add a `TODO Uno:` comment documenting the gap. Currently no marker exists.

##### CRITICAL — `FindFocusCandidate` previous-element fallback bug (mirror of WinUI bug ported faithfully but commented incorrectly)

- **C++ (.cpp:306-317)**:
  ```cpp
  if (!focusCandidate && previousElement) {
      focusedChild = previousElement.try_as<UIElement>();
      focusCandidate = previousElement.try_as<Control>();
      if (!previousElement) {     // BUG mirrored: should be !focusCandidate
          if (auto lastFocus = FocusManager::FindLastFocusableElement(previousElement))
  ```
- **C# (ViewManager.mux.cs:292-304)**: ports the WinUI bug verbatim (`if (previousElement == null)`). This is consistent with the porting rule, but the bug means the descendant lookup branch is dead code.
- **Issue**: 1:1 port preserves the upstream bug. Acceptable — flagged here only for traceability. **No fix needed**.

##### HIGH — `OnFocusChanged` parameter named `_` instead of `sender`

- **C++**: `OnFocusChanged(const winrt::IInspectable& sender, const winrt::RoutedEventArgs& args)` — the C++ defines two unused params but names them.
- **C# (ViewManager.mux.cs:957)**: `void OnFocusChanged(object _, RoutedEventArgs args)` — first param renamed. Behavior identical, naming difference only.
- **Issue**: Style mismatch with WinUI; pure cosmetic.

##### HIGH — `GetElementFromElementFactory` does not match WinUI ordering of the inner lambda

- **C++ (.cpp:743-790)**: structured as a single nested lambda chain `element = [...]() { providedElementFactory check returns dataAsElement directly; else factory then args then scopeGuard then return GetElement }`.
- **C# (ViewManager.mux.cs:705-748)**: equivalent logic but flattened into a local function `GetElement()`. Importantly:
  - C# adds `args.Index = index;` (L745). The C++ uses `RepeaterTestHooks::SetElementFactoryElementIndex(index);` (a test-hook static), not args. The C# port has changed the contract: `ElementFactoryGetArgs` now exposes an `Index`. This is a meaningful API addition not present in WinUI.
- **Suggested fix**: Either remove `args.Index = index;` and route through a test-hook equivalent, or wrap in `#if HAS_UNO` with a `TODO Uno:` justification. Currently completely unguarded.

##### HIGH — `GetElementFromElementFactory` default DataTemplate creation diverges

- **C++ (.cpp:763)**: `auto const factory = winrt::XamlReader::Load(L"<DataTemplate ..."<TextBlock Text='{Binding}'/></DataTemplate>").as<winrt::DataTemplate>();`
- **C# (ViewManager.mux.cs:717-723)**: commented-out `XamlReader.Load(...)` line, replaced with a `DataTemplate(() => { var tb = new TextBlock(); tb.SetBinding(TextBlock.TextProperty, new Binding()); return tb; })` factory.
- **Issue**: This is an Uno-specific deviation (XamlReader.Load with inline XAML is not supported in all targets). It should be `#if HAS_UNO` wrapped or carry a `TODO Uno:` comment explaining why. Currently unguarded with no marker.

##### HIGH — `ClearElementToElementFactory`: `m_ElementFactoryRecycleArgs` is always non-null

- **C++ (.cpp:171)**: `if (!m_ElementFactoryRecycleArgs)` — creates lazily.
- **C# (ViewManager.cs / ViewManager.mux.cs:27-28)**: constructor sets `m_ElementFactoryGetArgs = new ElementFactoryGetArgs();` and `m_ElementFactoryRecycleArgs = new ElementFactoryRecycleArgs();`.
- **Issue**: The eager construction inverts the lazy-creation pattern in WinUI; subsequent null-checks (mux L160, L730) are dead code. Wastes one allocation per ViewManager instance, but also alters semantics if RecycleArgs caches state across recycle calls.
- **Suggested fix**: Remove eager allocation in ctor, restore lazy pattern. Field initialization in `ViewManager.h.mux.cs` stays as default-null.

##### HIGH — `ClearElementToElementFactory` exception type

- **C++ (.cpp:203)**: `throw winrt::hresult_error(E_FAIL, ...)`
- **C# (ViewManager.mux.cs:191)**: `throw new InvalidOperationException(...)`.
- **Issue**: Style choice; `E_FAIL` is closer to `Exception`/`COMException`. Other places in this file (e.g., L441,L446,L451) all use `InvalidOperationException`. Inconsistent with upstream but uniformly Uno-style. Flag as low-priority style diff.

##### HIGH — `EnsureEventSubscriptions` strong reference to `m_owner` in capture

- **C++ (.cpp:1024-1025)**: `m_gotFocus = m_owner->GotFocus(winrt::auto_revoke, { this, &ViewManager::OnFocusChanged });` — `auto_revoke` keeps weak references.
- **C# (ViewManager.mux.cs:967-971)**: captures `var owner = m_owner;` into the disposable. The strong capture is fine for revocation (`m_owner` is set in ctor and never reassigned), and `SerialDisposable` is the correct mapping per `Disposable.Create` rule. **Correctly refactored.**

##### MEDIUM — `OnItemsSourceChanged`: signature uses `object _` instead of `object sender`

- **C++ (.cpp:403)**: `OnItemsSourceChanged(const winrt::IInspectable&, ...)` — unnamed.
- **C# (ViewManager.mux.cs:377)**: `OnItemsSourceChanged(object _, ...)`. Cosmetic, but inconsistent with the `OnFocusChanged` style; both should be uniform.

##### MEDIUM — `#pragma region` markers reduced to comments

- C++ has `#pragma region GetElement providers` / `#pragma region ClearElement handlers` / `#pragma region Ownership state machine`. C# converts to `// #pragma region GetElement providers` / `// #pragma endregion`.
- Rule 1 requires the markers be preserved at the same relative position. The comment form is acceptable as a fallback (and is consistent across this codebase), but flagged for tracking.

##### MEDIUM — `#ifdef DBG` trace calls dropped without a `TODO Uno:` marker

- Numerous WinUI `#ifdef DBG`-only `ITEMSREPEATER_TRACE_INFO_DBG`/`ITEMSREPEATER_TRACE_PERF_DBG` calls are absent in the C# port (e.g., the multiple "log index" traces in `GetElement`, `GetElementFromUniqueIdResetPool`, `ClearElementToPinnedPool`). These are diagnostic-only; rule 1 (lossless port) is missed but only at debug-tracing granularity. Add a single `// TODO Uno: VirtualizationInfo.s_logItemIndexDbg / TRACE_*_DBG verbose tracing not ported.` near the top of the file or per region.

##### MEDIUM — `ClearElement` debug-only assertion `MUX_ASSERT(m_resetPool.IsEmpty)` wrapped in `#if DEBUG`

- C++ (.cpp:541) — `MUX_ASSERT(m_resetPool.IsEmpty())` is unconditional.
- C# (ViewManager.mux.cs:516-519) — wrapped in `#if DEBUG`.
- Issue: `MUX_ASSERT` in Uno's `_Tracing` typically already becomes a Debug.Assert; manual `#if DEBUG` wrap is redundant and inconsistent with surrounding code.

##### MEDIUM — `GetElement` `out focusedChild` not initialized in the no-element path

- In `FindFocusCandidate(int clearedIndex, out UIElement focusedChild)` (ViewManager.mux.cs:237), the `focusedChild = null;` initialization at L277 happens only after the loop. Looks fine; verified the early returns assign first. **No issue, just inspection note.**

##### MEDIUM — `Focus` call returns bool, C++ has no return — return value discarded silently

- C# `focusCandidate.Focus(focusState)` returns a bool (focus success/fail). Result is discarded. **OK** but could be logged on failure.

##### LOW — Missing destructor preservation
- WinUI `ViewManager` is `final` with no explicit destructor. There is **no** destructor to preserve. The note in the task brief ("destructor was preserved as comments") does not apply to this class. **No fix needed.**

##### LOW — `UpdateFocusedElement` uses `is null` / `is not null` mixed with `!= null`
- C++ uses `if (xamlRoot)` / `if (child)`. C# alternates between `is null` (L910, L915) and `!= null` (L921, L943, L948). Cosmetic. Suggest uniformity.

##### LOW — `UpdatePin` casts `child as UIElement` — C++ uses `child.as<winrt::UIElement>()` which would throw

- C++ (.cpp:379): `child.as<winrt::UIElement>()` — `.as<>` throws on mismatch.
- C# (ViewManager.mux.cs:353): `child as UIElement` — silent null. Since `child` is initialized from `(DependencyObject)element` and reassigned from `parent` (always UIElement-derived), this is generally safe, but the semantic divergence should be noted.

##### LOW — Comments containing `→` substituted with `.`

- C# (ViewManager.mux.cs:687-697): Documentation comment for `GetElementFromElementFactory` replaces all arrow `→` characters with `.` (e.g., `If data is a UIElement . the data is returned`). Likely from a non-Unicode copy-paste. Restoring `->` or `→` improves clarity.

##### INFO — `m_lastFocusedElement` assignment from `Control` cast pattern

- C# (L217): `if (m_lastFocusedElement is Control focusedAsControl)` is the correct pattern translation of `m_lastFocusedElement.try_as<winrt::Control>()`. Looks good.

##### INFO — Recent `m_gotFocus` -> `SerialDisposable` refactor

- The refactor (commit 082326e737) replaces `auto_revoke` revoker fields with `SerialDisposable` wrapping `Disposable.Create(...)`. This is the documented pattern from the porting rules. The implementation in `EnsureEventSubscriptions` correctly:
  - Checks `m_gotFocus.Disposable == null` for the "not subscribed" state.
  - Asserts `m_lostFocus.Disposable == null` mirrors the C++ assert.
  - Wires both events and stores revoker delegates.
- Slight concern: assigns the disposable **after** subscribing the event. If `Disposable.Create` ever throws (e.g., out-of-memory), the event would remain subscribed without a revoker. Order is acceptable but a try/catch + manual unsubscribe path would be safer. **Style only.**

##### INFO — `m_lastFocusedElement` not converted to `SerialDisposable`

- It is a plain `UIElement` reference, not a revoker. Correctly kept as-is.

---

### ViewportManager (abstract base)

#### Mapping (.h vs ViewportManager.cs)

| C++ virtual | C# abstract | Match |
|-------------|-------------|-------|
| `winrt::UIElement SuggestedAnchor() const = 0` | `UIElement SuggestedAnchor { get; }` | yes |
| `double HorizontalCacheLength() const = 0` + `void HorizontalCacheLength(double)` | `double HorizontalCacheLength { get; set; }` | yes (collapsed into property) |
| `double VerticalCacheLength()` get/set | `double VerticalCacheLength { get; set; }` | yes |
| `winrt::Rect GetLayoutVisibleWindow() const = 0` | `Rect GetLayoutVisibleWindow()` | yes |
| `winrt::Rect GetLayoutRealizationWindow() const = 0` | `Rect GetLayoutRealizationWindow()` | yes |
| `void SetLayoutExtent(const winrt::Rect&) = 0` | `void SetLayoutExtent(Rect)` | yes |
| `winrt::Rect GetLayoutExtent() const = 0` | `Rect GetLayoutExtent()` | yes |
| `winrt::Point GetOrigin() const = 0` | `Point GetOrigin()` | yes |
| `void OnLayoutChanged(bool isVirtualizing) = 0` | same | yes |
| `void OnElementPrepared(const UIElement&) = 0` | same | yes |
| `void OnElementCleared(const UIElement&) = 0` | same | yes |
| `void OnOwnerMeasuring() = 0` | same | yes |
| `void OnOwnerArranged() = 0` | same | yes |
| `void OnMakeAnchor(const UIElement&, bool) = 0` | same | yes |
| `void OnBringIntoViewRequested(const BringIntoViewRequestedEventArgs&) = 0` | same | yes |
| `void ResetLayoutRealizationWindowCacheBuffer() = 0` | same | yes |
| `void ResetScrollers() = 0` | same | yes |
| `winrt::UIElement MadeAnchor() const = 0` | `UIElement MadeAnchor { get; }` | yes |

#### Findings

##### HIGH — `using Microsoft.UI.Xaml;` is unused

- The base class only references types in `Windows.Foundation` and `Microsoft.UI.Xaml` already in scope via namespace. Cleanup: harmless but flagged. The `Windows.UI.Core` using is not present (good).

##### MEDIUM — Class is `abstract partial` but has no `.mux.cs` counterpart
- Layout rule expects abstract base to be split into `ViewportManager.h.mux.cs` (header equivalent). Since this is a tiny interface-like abstract, the single `ViewportManager.cs` file is acceptable, but is it inconsistent with the other Repeater types. Add a one-line `ViewportManager.h.mux.cs` for symmetry **OR** rename `ViewportManager.cs` to `ViewportManager.h.mux.cs`.

##### MEDIUM — No `MUX Reference` is missing from anywhere except the renamed-future file

- Wait — the file does include `// MUX Reference ViewportManager.h, commit 4b206bce3` on L3 of `ViewportManager.cs`. **Verified present. No issue.**

##### LOW — No `internal abstract partial` keywords for the file pair

- The Uno `ViewportManager.cs` declares the class `internal abstract partial`. Other variants (`ViewportManagerDownLevel`) declare `internal partial`. This is correct: the base needs `abstract`, the derived doesn't.

##### INFO — Constructor not declared

- WinUI abstract base has no explicit ctor either; default `= default`. No mismatch.

---

### ViewportManagerDownlevel

#### Method order verification (.cpp vs ViewportManagerDownlevel.mux.cs)

| # | C++ method | C# location | Order matches |
|---|------------|-------------|----------------|
| 1 | Constructor                       | L25  | yes |
| 2 | SuggestedAnchor (getter)          | L31  | yes |
| 3 | HorizontalCacheLength setter      | L72  | yes (merged into property) |
| 4 | VerticalCacheLength setter        | L86  | yes |
| 5 | GetLayoutVisibleWindow            | L100 | yes |
| 6 | GetLayoutRealizationWindow        | L127 | yes |
| 7 | SetLayoutExtent                   | L141 | yes |
| 8 | OnLayoutChanged                   | L163 | yes |
| 9 | OnElementCleared                  | L171 | yes |
| 10 | OnOwnerMeasuring                 | L184 | yes |
| 11 | OnOwnerArranged                  | L201 | yes |
| 12 | OnMakeAnchor                     | L236 | yes |
| 13 | OnBringIntoViewRequested         | L242 | yes |
| 14 | ResetScrollers                   | L256 | yes |
| 15 | OnCacheBuildActionCompleted      | L273 | yes |
| 16 | OnViewportChanged                | L279 | yes |
| 17 | OnPostArrange                    | L294 | yes |
| 18 | OnConfigurationChanged           | L333 | yes |
| 19 | EnsureScrollers                  | L339 | yes |
| 20 | AddScroller                      | L377 | yes |
| 21 | UpdateViewport                   | L413 | yes |
| 22 | ResetLayoutRealizationWindowCacheBuffer | L464 | yes |
| 23 | ResetCacheBuffer                 | L469 | yes |
| 24 | ValidateCacheLength              | L481 | yes |
| 25 | RegisterCacheBuildWork           | L489 | yes |
| 26 | TryInvalidateMeasure             | L504 | yes |
| 27 | GetOuterScroller                 | L517 | yes |
| 28 | GetLayoutId                      | L529 | yes |

Order is correct.

#### Field verification (.h vs ViewportManagerDownlevel.h.mux.cs)

| C++ field | C# field | Match |
|-----------|----------|-------|
| `ItemsRepeater* m_owner{nullptr}`  | `ItemsRepeater m_owner` | yes |
| `bool m_ensuredScrollers{false}`  | `bool m_ensuredScrollers` | yes |
| `vector<ScrollerInfo> m_parentScrollers` | `List<ScrollerInfo> m_parentScrollers = new()` | yes |
| `tracker_ref<IRepeaterScrollingSurface> m_horizontalScroller` | `IRepeaterScrollingSurface m_horizontalScroller` | yes |
| `tracker_ref<IRepeaterScrollingSurface> m_verticalScroller` | same | yes |
| `tracker_ref<IRepeaterScrollingSurface> m_innerScrollableScroller` | same | yes |
| `tracker_ref<UIElement> m_makeAnchorElement` | `UIElement m_makeAnchorElement` | yes |
| `bool m_isAnchorOutsideRealizedRange{}` | same | yes |
| `tracker_ref<IAsyncAction> m_cacheBuildAction` | `IAsyncAction m_cacheBuildAction` | yes |
| `Rect m_lastLayoutRealizationWindow{}` | same | yes |
| `Rect m_visibleWindow{}` | same | yes |
| `Rect m_layoutExtent{}` | same | yes |
| `Point m_expectedViewportShift{}` | same | yes |
| `double m_maximumHorizontalCacheLength{2.0}` | `double m_maximumHorizontalCacheLength = 2.0` | yes |
| `double m_maximumVerticalCacheLength{2.0}` | same | yes |
| `double m_horizontalCacheBufferPerSide{}` | same | yes |
| `double m_verticalCacheBufferPerSide{}` | same | yes |
| `bool m_managingViewportDisabled{false}` | same | yes |
| `PostArrange_revoker m_postArrangeToken` | **MISSING**, see finding | **no** |

The `ScrollerInfo` nested struct preserves the 3 token fields (`ViewportChangedToken`, `PostArrangeToken`, `ConfigurationChangedToken`) — match.

#### Findings

##### HIGH — Outer `m_postArrangeToken` field dropped

- **C++ (.h:106)**: `winrt::IRepeaterScrollingSurface::PostArrange_revoker m_postArrangeToken;` — class-level revoker, distinct from the `ScrollerInfo::PostArrangeToken`.
- **C# (ViewportManagerDownlevel.h.mux.cs)**: no equivalent class-level field. Uno comment claims tokens are stored "directly inside each ScrollerInfo entry".
- **Issue**: WinUI deliberately uses both: `ScrollerInfo::PostArrangeToken` is only used for the outer scroller (see `EnsureScrollers` .cpp:348), and assigning to it through the struct slot. But there *is* a class-level `m_postArrangeToken` declared yet never explicitly assigned in the .cpp visible. Searching the WinUI sources — it appears unused at runtime. **Not a real semantic gap** but the Uno comment dismisses it without verification. Suggested action: confirm via grep across the WinUI codebase; if unused, drop the Uno workaround comment as misleading.

##### HIGH — `RegisterCacheBuildWork` capture diverges (no strong-ref simulation)

- **C++ (.cpp:467-478)**: takes `auto strongOwner = m_owner->get_strong();` and captures it in the dispatcher lambda **to keep ItemsRepeater alive**.
- **C# (ViewportManagerDownlevel.mux.cs:499-500)**: declares `var strongOwner = m_owner;` but **never references it in the dispatched lambda** (`_ => OnCacheBuildActionCompleted()`). The lambda implicitly captures `this`, which would extend the lifetime of `ViewportManagerDownLevel` (and via it, `m_owner`) anyway — but only if .NET considers `this` a strong root. In .NET, lambda capture of `this` is strong, so functional behavior matches.
- **Suggested fix**: Either remove the unused `strongOwner` local, or capture it explicitly: `_ => { var keep = strongOwner; OnCacheBuildActionCompleted(); }`. Currently the local is dead.

##### MEDIUM — `EnsureScrollers`: outerScrollerInfo write-back to `m_parentScrollers`

- **C++ (.cpp:347-349)**: `auto& outerScrollerInfo = m_parentScrollers.back(); outerScrollerInfo.PostArrangeToken = ...` — reference, in-place mutation.
- **C# (ViewportManagerDownlevel.mux.cs:364-370)**: `var outerScrollerInfo = m_parentScrollers[...]; outerScrollerInfo.PostArrange += OnPostArrange; outerScrollerInfo.PostArrangeToken = Disposable.Create(...); m_parentScrollers[...] = outerScrollerInfo;` — because `ScrollerInfo` is a struct, the explicit write-back is required.
- **Issue**: The write-back is correctly handled, but `AddScroller` (L392) creates `var scrollerInfo = new ScrollerInfo(scroller);` then mutates fields after `m_parentScrollers.Add(scrollerInfo)`. The mutations don't propagate to the list! Look at lines 394-407 carefully:
  ```
  var scrollerInfo = new ScrollerInfo(scroller);
  scroller.ConfigurationChanged += OnConfigurationChanged;
  scrollerInfo.ConfigurationChangedToken = Disposable.Create(...);   // mutates the local
  ...
  scrollerInfo.ViewportChangedToken = Disposable.Create(...);         // mutates the local
  m_parentScrollers.Add(scrollerInfo);                                 // copies the local with tokens set
  ```
  Order: tokens are assigned **before** `Add`, so the copy that lands in the list does have tokens. **OK.** But fragile — a future edit that calls `Add` first will silently break. Suggested fix: change `ScrollerInfo` to a class, or move the token assignments to a return value pattern.

##### MEDIUM — `ScrollerInfo` is a struct in C#

- **C++**: struct (value type), but tracked by `tracker_ref<>` for the scroller — effectively reference-semantics in the host scenario.
- **C# (ViewportManagerDownlevel.h.mux.cs:74)**: `internal struct ScrollerInfo` — value type with mutable fields. Subtle aliasing issue noted above.
- **Suggested fix**: Either keep as struct (matching WinUI's struct decl) but document the write-back rule, or convert to a sealed class to remove the foot-gun.

##### MEDIUM — `GetOuterScroller()` is not a `winrt::IRepeaterScrollingSurface` const method

- WinUI: `winrt::IRepeaterScrollingSurface GetOuterScroller() const;` — const, public-but-private. Uno marks it `IRepeaterScrollingSurface GetOuterScroller()` — package-private, fine.

##### MEDIUM — `Windows.UI.Core` using imported but only the `IAsyncAction` type used

- `ViewportManagerDownlevel.h.mux.cs:8` brings in `Windows.UI.Core`. The actual type used is `Windows.Foundation.IAsyncAction` (already imported via `Windows.Foundation`). The `Windows.UI.Core` `using` brings in `CoreDispatcher` referenced in the .mux.cs as `m_owner.Dispatcher` — verify; if `Dispatcher` property returns `CoreDispatcher`, this is correct.

##### LOW — `AddScroller`: `MUX_ASSERT(!(m_horizontalScroller && m_verticalScroller))`

- C++: `MUX_ASSERT(!(m_horizontalScroller && m_verticalScroller));` — direct.
- C#: `MUX_ASSERT(!(m_horizontalScroller != null && m_verticalScroller != null));` — semantically identical. OK.

##### LOW — `GetLayoutId()` uses `m_owner.Layout?.LayoutId` directly

- C++ (.cpp:507-516): casts via `layout.as<Layout>()->LayoutId()`. Uno's `Layout` already exposes `LayoutId` as a public property; simplification is correct.

##### LOW — `MUX_ASSERT(!m_managingViewportDisabled)` replaced with `Debug.Assert`

- C# (`UpdateViewport` L415): uses `System.Diagnostics.Debug.Assert(!m_managingViewportDisabled);` instead of the project's `MUX_ASSERT` helper. Inconsistent with other call sites (e.g., AddScroller uses `MUX_ASSERT`). Use the project helper uniformly.

##### LOW — Trace strings reference `%ls`/`%.0f` printf format from C++

- Uno trace macros (e.g., `REPEATER_TRACE_INFO("%ls: \tViewport: ..."`) keep the original `%ls`/`%.0f` printf-style format strings. Functionally inert if `_Tracing` is no-op, but is a hint that tracing was not adapted to .NET. **No fix needed.**

##### INFO — `ResetScrollers` disposes tokens before clearing — added Uno-specific code path

- C# (L259-264): adds a foreach loop disposing all 3 tokens per scroller info, with a comment `// Uno specific: dispose tokens before clearing the list so listeners are detached.`. In WinUI the auto-revokers self-revoke when the struct is destroyed. Necessary for the IDisposable token model. **Justified.**

##### INFO — `OnViewportChanged` / `OnConfigurationChanged` parameter naming

- C++ unnamed (`const winrt::IRepeaterScrollingSurface&`), C# names them `sender`. Cosmetic.

---

### ViewportManagerWithPlatformFeatures

#### Method order verification (.cpp vs ViewportManagerWithPlatformFeatures.mux.cs)

| # | C++ method | C# location | Order matches |
|---|------------|-------------|----------------|
| 1 | Constructor                                  | L26   | yes |
| 2 | SuggestedAnchor                              | L32   | yes |
| 3 | HorizontalCacheLength setter                 | L69 (SetHorizontalCacheLength + property in .h.mux.cs) | yes |
| 4 | VerticalCacheLength setter                   | L79   | yes |
| 5 | GetLayoutVisibleWindowDiscardAnchor          | L91 (commented under #if false) | **partial — see findings** |
| 6 | GetLayoutVisibleWindow                       | L105  | yes |
| 7 | GetLayoutRealizationWindow                   | L133  | yes |
| 8 | SetVisibleWindow                             | L147  | yes |
| 9 | SetLastLayoutRealizationWindow               | L156  | yes |
| 10 | SetPendingViewportShift                     | L165  | yes |
| 11 | SetExpectedViewportShift                    | L173  | yes |
| 12 | SetUnshiftableShift                         | L181  | yes |
| 13 | SetLastScrollPresenterViewChangeCorrelationId | L190 | yes (under `#pragma warning disable IDE0051`) |
| 14 | SetLayoutExtent                             | L199  | yes |
| 15 | ResetLayoutExtent                           | L253  | yes |
| 16 | ResetVisibleWindow                          | L262  | yes |
| 17 | ResetLastLayoutRealizationWindow            | L272  | yes (under warning disable) |
| 18 | ResetExpectedViewportShift                  | L282  | yes |
| 19 | ResetPendingViewportShift                   | L290  | yes |
| 20 | ResetUnshiftableShift                       | L298  | yes |
| 21 | ResetLastScrollPresenterViewChangeCorrelationId | L306 | yes |
| 22 | OnLayoutChanged                             | L318  | yes |
| 23 | OnElementPrepared                           | L349  | yes |
| 24 | OnElementCleared                            | L362  | yes |
| 25 | OnOwnerMeasuring                            | L377  | yes |
| 26 | OnOwnerArranged                             | L414  | yes |
| 27 | OnLayoutUpdated                             | L467  | yes |
| 28 | OnMakeAnchor                                | L496  | yes |
| 29 | OnBringIntoViewRequested                    | L502  | yes |
| 30 | GetImmediateChildOfRepeater                 | L542  | yes |
| 31 | OnCompositionTargetRendering                | L560  | yes |
| 32 | ResetScrollers                              | L603  | yes |
| 33 | OnCacheBuildActionCompleted                 | L618  | yes |
| 34 | (ScrollPresenter ...) handlers              | **NOT PORTED** (`TODO Uno:` block at L627) | **gap** |
| 35 | OnEffectiveViewportChanged                  | L634  | yes |
| 36 | EnsureScroller                              | L678  | yes |
| 37 | UpdateViewport                              | L737  | yes |
| 38 | ResetLayoutRealizationWindowCacheBuffer     | L777  | yes |
| 39 | ResetCacheBuffer                            | L782  | yes |
| 40 | ValidateCacheLength                         | L794  | yes |
| 41 | RegisterPreparedElementsAsArranged          | L802  | yes |
| 42 | RegisterPreparedAndArrangedElementsAsScrollAnchorCandidates | L820 | yes |
| 43 | RegisterCacheBuildWork                      | L843  | yes |
| 44 | TryInvalidateMeasure                        | L859  | yes |
| 45 | UnregisterScrollAnchorCandidates            | L888  | yes |
| 46 | GetLayoutId                                 | L909  | yes |
| 47 | DBG-only TraceFieldsDbg/TraceScrollerDbg    | comment at L912-914 | not ported (DBG-only) |

#### Field verification (.h vs ViewportManagerWithPlatformFeatures.h.mux.cs)

| C++ field | C# field | Match |
|-----------|----------|-------|
| `ItemsRepeater* m_owner{nullptr}`          | `ItemsRepeater m_owner` | yes |
| `bool m_ensuredScroller{false}`            | `bool m_ensuredScroller` | yes |
| `tracker_ref<IScrollAnchorProvider> m_scroller` | `IScrollAnchorProvider m_scroller` | yes |
| `vector<tracker_ref<UIElement>> m_preparedElements` | `List<UIElement> m_preparedElements = new()` | yes |
| `vector<tracker_ref<UIElement>> m_preparedAndArrangedElements` | same | yes |
| `tracker_ref<UIElement> m_makeAnchorElement` | `UIElement m_makeAnchorElement` | yes |
| `bool m_isAnchorOutsideRealizedRange{}` | same | yes |
| `bool m_skipScrollAnchorRegistrationsDuringNextMeasurePass{}` | same | yes |
| `bool m_skipScrollAnchorRegistrationsDuringNextArrangePass{}` | same | yes |
| `bool m_cacheBuildActionOutstanding{}` | `IAsyncAction m_cacheBuildAction` | **DIVERGES — see finding** |
| `Rect m_lastLayoutRealizationWindow{}` | same | yes |
| `Rect m_visibleWindow{}` | same | yes |
| `Rect m_layoutExtent{}` | same | yes |
| `Point m_expectedViewportShift{}` | same | yes |
| `Point m_pendingViewportShift{}` | same | yes |
| `Point m_unshiftableShift{}` | same | yes |
| `int m_lastScrollPresenterViewChangeCorrelationId{-1}` | same | yes |
| `double m_maximumHorizontalCacheLength{2.0}` | same | yes |
| `double m_maximumVerticalCacheLength{2.0}` | same | yes |
| `double m_horizontalCacheBufferPerSide{}` | same | yes |
| `double m_verticalCacheBufferPerSide{}` | same | yes |
| `bool m_isBringIntoViewInProgress{false}` | same | yes (with #pragma warning) |
| `bool m_managingViewportDisabled{false}` | same | yes |
| `winrt::ScrollPresenter::*_revoker` (4 fields) | **MISSING** (TODO Uno noted) | gap |
| `EffectiveViewportChanged_revoker` | `IDisposable m_effectiveViewportChangedRevoker` | yes |
| `LayoutUpdated_revoker` | `IDisposable m_layoutUpdatedRevoker` | yes |
| `CompositionTarget::Rendering_revoker m_renderingToken` | `IDisposable m_renderingToken` | yes |

#### .uno.cs additions review

| Block | Justification | Verdict |
|-------|---------------|---------|
| `_uno_viewportUsedInLastMeasure` (Rect) — read in `OnOwnerMeasuring`, written in `UpdateViewport` | Doc-comment in `.uno.cs:4-9` explains: tracks visible window at last measure to skip `InvalidateMeasure` for minor viewport changes; guarded by `!UNO_HAS_ENHANCED_LIFECYCLE`. | **Justified.** Could be moved into the `.mux.cs` file inside the same `#if !UNO_HAS_ENHANCED_LIFECYCLE` guard to keep state and behavior together, but functionally correct. |

#### Findings

##### CRITICAL — `m_cacheBuildActionOutstanding` semantics replaced; `OnCacheBuildActionCompleted` does not respect `m_managingViewportDisabled` guard

- **C++ (.cpp:744-757)**: `OnCacheBuildActionCompleted` clears the flag `m_cacheBuildActionOutstanding` **and** guards the `InvalidateMeasure` call with `if (!m_managingViewportDisabled)`. The flag clearing happens regardless.
- **C# (ViewportManagerWithPlatformFeatures.mux.cs:618-625)**: clears the IAsyncAction-equivalent `m_cacheBuildAction = null` and **also** has `if (!m_managingViewportDisabled)`. Matches.
- However, the **registration path** differs. In C++ `RegisterCacheBuildWork` sets `m_cacheBuildActionOutstanding = m_owner->DispatcherQueue().TryEnqueue(...)` (bool). In C# `m_cacheBuildAction = m_owner.Dispatcher.RunIdleAsync(_ => OnCacheBuildActionCompleted())`. Two issues:
  1. The C# port uses `Dispatcher.RunIdleAsync` (CoreDispatcher) **not** `DispatcherQueue.TryEnqueue`. The WinUI port (commit 4b206bce3) explicitly moved to `DispatcherQueue.TryEnqueue` — Uno is **behind** that change.
  2. The C# port reuses the `Downlevel` path (RunIdleAsync) for the platform-features path. This is a code-smell. **Suggested fix**: use `m_owner.DispatcherQueue.TryEnqueue` or document the parity gap.

##### HIGH — Scroller revokers for ScrollPresenter (4 fields) not ported

- WinUI fields:
  ```
  m_scrollPresenterScrollStartingRevoker
  m_scrollPresenterScrollCompletedRevoker
  m_scrollPresenterZoomStartingRevoker
  m_scrollPresenterZoomCompletedRevoker
  ```
- C# omits them with a `// TODO Uno: WinUI uses auto-revoke handles for the IScrollPresenter ...` comment (.h.mux.cs L96-99).
- **Issue**: Justified gap but means anticipatory view updates via `OnScrollPresenterScrollStarting/ZoomStarting` are absent. This degrades scroll perf when ItemsRepeater is hosted in a ScrollPresenter. Tracked as "parity gap I6, blocked on IScrollPresenter2".

##### HIGH — `EnsureScroller` always sets up `m_effectiveViewportChangedRevoker` from inside `if (!m_ensuredScroller)`, **but** OnLayoutChanged also does so

- **C++ (.cpp:384-406 `OnLayoutChanged`, .cpp:943-986 `EnsureScroller`)**: Both `OnLayoutChanged` and `EnsureScroller` may register `m_effectiveViewportChangedRevoker`. The `auto_revoke` pattern silently replaces an existing token if reassigned.
- **C# (ViewportManagerWithPlatformFeatures.mux.cs:330-343 OnLayoutChanged, L717-726 EnsureScroller)**: Both register via `Disposable.Create(...)`. If both register, you get two subscriptions because `IDisposable` field assignment in `Disposable.Create(() => ... -= ...)` doesn't auto-dispose the previous.
- **Suggested fix**: Switch to `SerialDisposable` for `m_effectiveViewportChangedRevoker` (and `m_layoutUpdatedRevoker`, `m_renderingToken`) so a re-assign auto-disposes the previous registration.

##### HIGH — `OnLayoutChanged` adds Uno workaround conditional on `m_owner.ItemsSourceView?.Count > 0`

- **C++ (.cpp:399-402)**: registers `m_effectiveViewportChangedRevoker` whenever it's null and we're not disabled.
- **C# (ViewportManagerWithPlatformFeatures.mux.cs:330-343)**: adds `#if !UNO_HAS_ENHANCED_LIFECYCLE && m_owner.ItemsSourceView?.Count > 0`. This is an Uno perf workaround.
- **Issue**: The workaround means an ItemsRepeater with a *future* ItemsSource (set after layout) won't receive viewport changes. Subscribing later requires the dependency to be retriggered. Verify this works correctly in `OnDataSourcePropertyChanged` / equivalents. Mark with explicit `// Uno workaround` and revisit.

##### HIGH — `SetLayoutExtent`: early-return mutation order

- **C++ (.cpp:226-275)**: always recomputes `expectedViewportShift`, then sets `m_layoutExtent` if changed, then `SetPendingViewportShift`, then `InvalidateArrange`.
- **C# (ViewportManagerWithPlatformFeatures.mux.cs:199-251)**: **early-returns at L204** if `m_layoutExtent == layoutExtent`. Comment says "Uno specific: On Android the InvalidateArrange will actually cause an invalidate measure...". This is a valid platform workaround but the early-return **skips** the `SetExpectedViewportShift` recomputation, the layout-updated subscription, and the pending-shift accumulation that may be required even when the extent hasn't changed (e.g., another concurrent shift in flight).
- **Issue**: Could mask state divergence on non-Android platforms. The guard is unconditional (not `#if __ANDROID__`).
- **Suggested fix**: Guard with `#if __ANDROID__` or compute shifts first, then skip only the `InvalidateArrange` call.

##### HIGH — `EnsureScroller` Uno-injected `if (m_owner.ItemsSourceView?.Count <= 0) { return; }` (.mux.cs:686-690)

- WinUI registers `OnEffectiveViewportChanged` unconditionally when not managingDisabled.
- C# adds an early return when ItemsSourceView is empty. Same concern as OnLayoutChanged finding above. Wrap with `#if !UNO_HAS_ENHANCED_LIFECYCLE` (it is). **Acceptable**, but combined with the layout-change guard creates two overlapping perf workarounds that can leak the `m_ensuredScroller=true` state without registering a revoker.

##### MEDIUM — Commented-out null-update-then-update-viewport path

- C# (L704-716) has a commented-out block describing a known issue `microsoft/microsoft-ui-xaml#2349`. This is preserved as a comment; the runtime behavior diverges from WinUI because we skip the `UpdateViewport(new Rect())` for the null-scroller case. Verify this isn't masking a bug.

##### MEDIUM — `TryInvalidateMeasure` Android workaround

- C# (mux.cs:873-884) wraps the `InvalidateMeasure` call in `Dispatcher.RunAnimation(...)` on Android.
- **Justified**: explicit comment block, `#if __ANDROID__` guarded, traces to specific Android arrange-phase invalidate restriction.

##### MEDIUM — `TryInvalidateMeasure` `m_owner.ItemsSourceView?.Count > 0` perf guard

- C# (mux.cs:862-866): under `#if !UNO_HAS_ENHANCED_LIFECYCLE` adds extra check. Justified; documented.

##### MEDIUM — `OnCompositionTargetRendering` adds null-element guard

- C++ (.cpp:709-719): `for (const auto& child : m_owner->Children())` — assumes no nulls.
- C# (mux.cs:585-590): `if (child is null) { continue; }`. Defensive. Should be removed unless a concrete repro justifies it; otherwise add `// TODO Uno:` explaining why nulls can appear in `Children`.

##### MEDIUM — `UnregisterScrollAnchorCandidates` adds null-element guard

- Same pattern as above (mux.cs:891-894). Same recommendation.

##### MEDIUM — `OnCacheBuildActionCompleted` does not log/early-return when `m_cacheBuildAction` was already null

- WinUI checks `if (m_cacheBuildActionOutstanding)` before clearing and tracing. C# just sets `m_cacheBuildAction = null` unconditionally. Minor logic difference.

##### MEDIUM — `SetExpectedViewportShift` uses `double` parameters

- C++: `float expectedViewportShiftX, float expectedViewportShiftY`
- C#: `double expectedViewportShiftX, double expectedViewportShiftY`
- **Issue**: C# Point uses double, so this is a natural widen. But callers (`SetLayoutExtent`) compute `expectedViewportShiftX = m_expectedViewportShift.X + m_layoutExtent.X - layoutExtent.X` as `double`. Should also rename to maintain WinUI float semantics for parity testing, or accept the widen. **Cosmetic.**

##### LOW — `using Microsoft.UI.Xaml.Controls;` redundant (file is in this namespace)

- C# (mux.cs:11). Harmless.

##### LOW — `m_cacheBuildAction` field declaration moved to `.h.mux.cs:56` with paragraph-long comment

- The comment in `.h.mux.cs:53-55` explains the WinUI ↔ Uno divergence (bool vs IAsyncAction). Acceptable as the canonical place to document Uno deviations.

##### LOW — `#pragma warning disable IDE0051` blocks around `SetLastScrollPresenterViewChangeCorrelationId` and `ResetLastLayoutRealizationWindow`

- Justified inline (called only by unported ScrollPresenter handlers / WinUI scaffolding). Good practice. Acceptable.

##### LOW — `using Uno.UI.Helpers.WinUI;` imported but only `SharedHelpers` referenced

- Standard Uno-shared helpers namespace. Fine.

##### LOW — `OnCompositionTargetRendering` uses `Microsoft.UI.Xaml.Media.CompositionTarget` fully-qualified

- The repeated full qualification `Microsoft.UI.Xaml.Media.CompositionTarget.Rendering` at L534, L537 could be replaced with a `using static` or `using` alias. Cosmetic.

##### INFO — `_uno_viewportUsedInLastMeasure` lives in `.uno.cs`

- Properly tagged with `_uno_` prefix and wrapped in `#if !UNO_HAS_ENHANCED_LIFECYCLE`. Documented in the `.uno.cs` header. **Justified.**

##### INFO — `m_lastScrollPresenterViewChangeCorrelationId` field present but unused; `#pragma warning disable 414` wraps it

- Acceptable; mirrors WinUI for the eventual port. Two `#pragma warning disable 414` blocks (one for the int, one for `m_isBringIntoViewInProgress`) — both documented.

##### INFO — `OnLayoutUpdated` uses `m_layoutUpdatedRevoker?.Dispose()` — multiple dispatches of the event won't re-dispose

- The disposable's `Dispose` should call `LayoutUpdated -= OnLayoutUpdated` and then null the field (see L228-232). Calling Dispose multiple times would re-attempt the unsub which is benign. Slight concern: the field is nulled in the Create closure, but the closure captures `m_layoutUpdatedRevoker` by reference indirectly via field reads. Acceptable but `SerialDisposable` is the cleaner pattern.

##### INFO — `RegisterCacheBuildWork` doesn't capture `strongOwner`

- Same as the Downlevel finding — `var strongOwner = m_owner;` (L854) is declared but unused.

---

### VirtualizationInfo

#### Method order verification (.cpp top-to-bottom vs VirtualizationInfo.mux.cs)

| # | C++ method | C# location | Order matches |
|---|------------|-------------|----------------|
| 1 | Constructor                                  | L13   | yes |
| 2 | IsPinned                                     | L18   | yes |
| 3 | IsHeldByLayout                               | L20   | yes |
| 4 | IsInPinnedPool                               | L22   | yes |
| 5 | IsInUniqueIdResetPool                        | L24   | yes |
| 6 | IsRealized                                   | L26   | yes |
| 7 | UpdatePhasingInfo                            | L28   | yes |
| 8 | MoveOwnershipToLayoutFromElementFactory      | L37   | yes |
| 9 | MoveOwnershipToLayoutFromUniqueIdResetPool   | L46   | yes |
| 10 | MoveOwnershipToLayoutFromPinnedPool         | L52   | yes |
| 11 | MoveOwnershipToElementFactory               | L59   | yes |
| 12 | MoveOwnershipToUniqueIdResetPoolFromLayout  | L69   | yes |
| 13 | MoveOwnershipToAnimator                     | L77   | yes |
| 14 | MoveOwnershipToPinnedPool                   | L88   | yes |
| 15 | AddPin                                      | L96   | yes |
| 16 | RemovePin                                   | L106  | yes |
| 17 | UpdateIndex                                 | L121  | yes |

#### Field verification (.h vs VirtualizationInfo.h.mux.cs)

| C++ field | C# field | Match |
|-----------|----------|-------|
| `unsigned m_pinCounter{0u}` | `uint m_pinCounter = 0u` | yes |
| `int m_index{-1}` | `int m_index = -1` | yes |
| `winrt::hstring m_uniqueId` | `string m_uniqueId` | yes |
| `ElementOwner m_owner{ElementOwner::ElementFactory}` | same | yes |
| `winrt::Rect m_arrangeBounds` | same | yes |
| `int m_phase{PhaseNotSpecified}` | same | yes |
| `bool m_keepAlive{false}` | same | yes |
| `bool m_autoRecycleCandidate{false}` | same | yes |
| `bool m_mustClearDataContext{false}` | same | yes |
| `weak_ref<winrt::IInspectable> m_data` | `WeakReference<object> m_data` | yes |
| `weak_ref<winrt::IDataTemplateComponent> m_dataTemplateComponent` | same | yes |
| `static int s_logItemIndexDbg` (DBG) | same (#if DEBUG) | yes |

`ElementOwner` enum order matches exactly.

#### Findings

##### HIGH — `MoveOwnershipToLayoutFromElementFactory` assertion suppressed

- **C++ (.cpp:68)**: `MUX_ASSERT(m_owner == ElementOwner::ElementFactory);`
- **C# (VirtualizationInfo.mux.cs:39-40)**: comments the assert out with `// TODO Uno: this assert is failing - issue #4691`.
- **Issue**: A known Uno bug (referenced by issue link) where state moves into Layout from an unexpected owner. Acceptable for now; properly tagged. **Justified gap.**

##### MEDIUM — `MoveOwnershipToElementFactory` clears `m_uniqueId` via `null` rather than `clear()`

- **C++ (.cpp:117)**: `m_uniqueId.clear();`
- **C# (mux.cs:65)**: `m_uniqueId = null;`
- **Issue**: WinUI hstring `.clear()` produces an empty (non-null) hstring. C# nullification breaks semantic equivalence: `UniqueId` getter returns `null` instead of `""`. If a caller checks `UniqueId.Length == 0`, both work; if a caller does `UniqueId == ""`, the C# returns false. Map: `m_uniqueId = string.Empty;`.

##### MEDIUM — `UpdatePhasingInfo` always allocates `WeakReference<>`

- C++ uses `winrt::make_weak(data)` which gracefully handles null.
- C# (mux.cs:31-32): `m_data = new WeakReference<object>(data); m_dataTemplateComponent = new WeakReference<IDataTemplateComponent>(component);`. `WeakReference<T>` constructor throws on null argument? Actually in .NET, `new WeakReference<T>(null)` does not throw, just creates a dead reference. **OK.**

##### LOW — Properties combined into a `Phase` get/set property where C++ has both `Phase()` getter and `Phase(int)` setter

- Matches the documented expected conversion. **OK.**

##### INFO — `#pragma region Ownership state machine` comment in C# (`// #pragma region Ownership state machine`)

- Mirrors the pattern used throughout the Repeater port. **OK.**

---

## Cross-type observations

1. **Eager allocation of cached factory args in `ViewManager` ctor** breaks WinUI's lazy creation pattern. The pattern is local to ViewManager but worth a fix.

2. **SerialDisposable vs. plain IDisposable**: `ViewManager` uses `SerialDisposable` for `m_gotFocus` / `m_lostFocus` (matches porting rule 7). `ViewportManagerWithPlatformFeatures` and `ViewportManagerDownlevel` still use plain `IDisposable` for revoker fields (`m_effectiveViewportChangedRevoker`, `m_layoutUpdatedRevoker`, `m_renderingToken`, the per-`ScrollerInfo` tokens). Recommend converting all to `SerialDisposable` for re-subscribe safety and consistency.

3. **`RegisterCacheBuildWork` uses `Dispatcher.RunIdleAsync`** in both Downlevel and WithPlatformFeatures. WinUI 4b206bce3 moved WithPlatformFeatures to `DispatcherQueue.TryEnqueue`. Uno is behind on that migration.

4. **Strong-ref `var strongOwner = m_owner;` is declared but unused** in both `RegisterCacheBuildWork` paths (Downlevel and WithPlatformFeatures). Either delete or capture into the lambda.

5. **Defensive null checks against `m_owner.Children` iteration** added in `OnCompositionTargetRendering` and `UnregisterScrollAnchorCandidates`. Either justify with a `// TODO Uno:` comment or remove.

6. **Eager `ResetScrollers` token disposal loop in Downlevel** is the right pattern; same pattern should be considered for `WithPlatformFeatures.ResetScrollers` so the multiple revoker fields are uniformly disposed.

7. **`m_lastFocusedElement = owner` in ViewManager ctor** (Critical) is the highest-priority semantic bug found. This is a direct translation error of `m_lastFocusedElement(owner)` (tracker_ref init) into a field assignment.

8. **`Focus` vs `FocusNoActivate`** (Critical) is the second-highest. `IUIElementPrivate.FocusNoActivate` may not be available in Uno; if not, an alternative that doesn't activate the window must be sourced or a `TODO Uno:` added.

9. **`#pragma region` markers**: uniformly converted to `// #pragma region` comments. Acceptable codebase convention but not strict rule-1 compliance.

10. **`#ifdef DBG` tracing** dropped without `TODO Uno:` markers in most places. Add a single file-level note per ported file.

11. **Method order** is matched in every ported file except the `GetLayoutVisibleWindowDiscardAnchor` placeholder in `ViewportManagerWithPlatformFeatures.mux.cs`, which is wrapped in `#if false` rather than placed at the same position — this preserves source-line order while keeping it dead.

12. **MUX Reference headers**: every `.mux.cs` and `.h.mux.cs` file has the correct `MUX Reference <file>, commit 4b206bce3` header.

---

## Conclusion

### Total findings by severity
- Critical: 4
- High: 13
- Medium: 20
- Low: 15
- Info: 11

### Top priority issues (in fix order)

1. **CRITICAL** — `ViewManager` ctor: `m_lastFocusedElement = owner;` should be `m_lastFocusedElement = null;` (true 1:1 with WinUI's tracker_ref ctor).
2. **CRITICAL** — `ViewManager.MoveFocusFromClearedIndex`: `focusCandidate.Focus(focusState)` → `IUIElementPrivate.FocusNoActivate(focusState)` equivalent.
3. **CRITICAL** — `ViewManager` ctor: eager allocation of `ElementFactoryGetArgs` / `ElementFactoryRecycleArgs`; defer to first-use.
4. **CRITICAL** — `ViewportManagerWithPlatformFeatures.RegisterCacheBuildWork`: use `DispatcherQueue.TryEnqueue` to match WinUI 4b206bce3.
5. **HIGH** — Switch `m_effectiveViewportChangedRevoker`, `m_layoutUpdatedRevoker`, `m_renderingToken` to `SerialDisposable` to prevent double-subscription on re-register paths in OnLayoutChanged + EnsureScroller.
6. **HIGH** — `ViewportManagerWithPlatformFeatures.SetLayoutExtent` early-return: guard the `return;` with `#if __ANDROID__` or restructure to skip only `InvalidateArrange`.
7. **HIGH** — `ViewManager.GetElementFromElementFactory`: remove or justify `args.Index = index;` (no equivalent in WinUI 4b206bce3).
8. **HIGH** — `ViewManager.GetElementFromElementFactory`: wrap the default DataTemplate substitution in `#if HAS_UNO` with a `TODO Uno:` explaining XamlReader.Load limitation.
9. **HIGH** — `VirtualizationInfo.MoveOwnershipToElementFactory`: replace `m_uniqueId = null;` with `m_uniqueId = string.Empty;` to mirror `hstring.clear()`.
10. **MEDIUM** — Remove unused `var strongOwner = m_owner;` declarations in both `RegisterCacheBuildWork` methods (or capture into the lambda body to preserve documented intent).
