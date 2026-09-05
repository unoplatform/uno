# Distilled diff: View Management

**Source report:** `_ComparisonReport_ViewManagement.md`
**WinUI commit:** 4b206bce3
**Types covered:** ViewManager, ViewportManager, ViewportManagerDownlevel, ViewportManagerWithPlatformFeatures, VirtualizationInfo

## TL;DR

After re-verifying the 4 "critical" findings from the source report against
both code bases, **three are genuine behavioural bugs**, one is a known Uno
API limitation (FocusNoActivate). The most damaging is `ViewManager`'s
constructor mistakenly assigning the owner repeater to
`m_lastFocusedElement` (and similarly pre-allocating
`ElementFactoryGetArgs` / `ElementFactoryRecycleArgs`), all of which are
direct mistranslations of the C++ `tracker_ref<T>(owner)` constructor —
that ctor only registers the tracker host, it does **not** seed the held
value. Behaviour downstream depends on those fields starting null.
Additional real divergences: `RegisterCacheBuildWork` routes through
`CoreDispatcher.RunIdleAsync` instead of the WinUI
`DispatcherQueue.TryEnqueue`, and plain `IDisposable` revoker fields can
double-subscribe on re-register paths (would auto-revoke in C++).

---

## Confirmed behavioural / correctness issues

### 1. `ViewManager` ctor seeds `m_lastFocusedElement` with the owner repeater

- **File:** `ViewManager.mux.cs:25`
- **WinUI:** `ViewManager.cpp:11-20` — initializer list
  `m_lastFocusedElement(owner)` invokes
  `tracker_ref<UIElement>::tracker_ref(ITrackerHandleManager* owner)`
  which **stores the host pointer for change-tracking** and leaves the
  held value null.
- **Uno:** `m_lastFocusedElement = owner;` — assigns the `ItemsRepeater`
  itself into a `UIElement` field.
- **Why it matters:** Several downstream branches test
  `if (m_lastFocusedElement != null)` or
  `m_lastFocusedElement == element`:
  - `MoveFocusFromClearedIndex` (line 217) takes the
    `is Control focusedAsControl` branch with the repeater as candidate,
    rather than short-circuiting and using `FocusState.Programmatic`.
  - `ClearElementToElementFactory` / `ClearElementToAnimator` compare
    against `element` — works only by coincidence because the owner is
    never a child element.
- **Fix:** Drop the assignment (default `null` is correct).
  ```csharp
  // remove: m_lastFocusedElement = owner;
  ```

### 2. `ViewManager` ctor eagerly allocates `ElementFactory*Args`

- **File:** `ViewManager.mux.cs:27-28`
- **WinUI:** `ViewManager.cpp:11-20` — `m_ElementFactoryGetArgs(owner)` /
  `m_ElementFactoryRecycleArgs(owner)` are `tracker_ref<...>` ctors taking
  the owner as **host**. Both start null and are **lazy-created** on
  first use (see `GetElementFromElementFactory` and
  `ClearElementToElementFactory`, both gated by `if (!m_ElementFactory*Args)`).
- **Uno:** ctor instantiates both eagerly.
- **Why it matters:**
  - The lazy `if (!m_ElementFactoryGetArgs)` / `if (!m_ElementFactoryRecycleArgs)`
    null-checks (mux.cs L160, L730) become dead code, so users that pass
    custom args via `OnRecycle`/`OnPrepare` may now observe cached state
    from previous recycle cycles that WinUI's lazy create would have
    cleared.
  - Wastes one allocation per ViewManager.
- **Fix:** Remove the eager allocation; let the lazy `if (... == null)`
  guards down-stream do the work.

### 3. `ViewportManagerWithPlatformFeatures.RegisterCacheBuildWork` uses `CoreDispatcher.RunIdleAsync` instead of `DispatcherQueue.TryEnqueue`

- **File:** `ViewportManagerWithPlatformFeatures.mux.cs:843-857`
- **WinUI:** `ViewportManagerWithPlatformFeatures.cpp:1127-1147` —
  `m_cacheBuildActionOutstanding =
   m_owner->DispatcherQueue().TryEnqueue([this, strongOwner](auto&&...){...})`.
- **Uno:** still inherits the `Downlevel` pattern:
  `m_cacheBuildAction = m_owner.Dispatcher.RunIdleAsync(_ => OnCacheBuildActionCompleted())`.
- **Why it matters:**
  - `RunIdleAsync` is the legacy `CoreDispatcher` pre-WinUI3 path; it is
    not always available on all Uno targets the same way `DispatcherQueue`
    is, and the WinUI commit deliberately moved off it.
  - The field type also diverges (`IAsyncAction` instead of `bool
    m_cacheBuildActionOutstanding`), which propagates Uno-specific
    cancellation semantics that WinUI no longer has.
- **Fix:** Switch to
  `m_owner.DispatcherQueue.TryEnqueue(() => OnCacheBuildActionCompleted())`
  and store the resulting `bool` in a new
  `m_cacheBuildActionOutstanding`. Keep `IAsyncAction` only behind a
  `#if !HAS_DISPATCHER_QUEUE` guard if any target genuinely lacks it.

### 4. Plain `IDisposable` revoker fields can double-subscribe on re-register paths

- **Files:** `ViewportManagerWithPlatformFeatures.h.mux.cs` (fields
  `m_effectiveViewportChangedRevoker`, `m_layoutUpdatedRevoker`,
  `m_renderingToken`); call sites in `OnLayoutChanged`,
  `EnsureScroller`, `SetLayoutExtent`, `UpdateViewport`.
- **WinUI:** uses `winrt::auto_revoke` revoker types — reassigning the
  revoker disposes the previous registration automatically.
- **Uno:** stores plain `IDisposable`. The `OnLayoutChanged` branch
  guards with `m_effectiveViewportChangedRevoker == null` (safe), but
  `EnsureScroller` (mux.cs L720) re-creates the disposable
  unconditionally inside the `!m_managingViewportDisabled` branch. If
  the previous disposable was not nulled (e.g., because
  `m_ensuredScroller` was reset without going through
  `ResetScrollers`), the event handler is added twice with no auto-revoke.
- **Why it matters:** Silent double subscription leaks the previous
  delegate and runs `OnEffectiveViewportChanged` twice per viewport
  change — produces duplicate `InvalidateMeasure` calls and can amplify
  layout cycles.
- **Fix:** Convert the three fields to
  `SerialDisposable` (matches the porting rule already applied to
  `m_gotFocus` / `m_lostFocus` in `ViewManager`). The current
  `Disposable.Create(...)` lambdas can be kept; only the field type and
  assignment via `.Disposable = ...` change. Apply same change to the
  per-`ScrollerInfo` tokens in `ViewportManagerDownlevel`.

### 5. `VirtualizationInfo.MoveOwnershipToElementFactory` sets `m_uniqueId = null` instead of `""`

- **File:** `VirtualizationInfo.mux.cs:65`
- **WinUI:** `VirtualizationInfo.cpp:117` — `m_uniqueId.clear();` →
  empty (non-null) hstring.
- **Why it matters:** `UniqueId` is used as a `Dictionary<string,
  UIElement>` key inside `UniqueIdElementPool.Add(...)`
  (`UniqueIdElementPool.cs:32`). Once an element has been moved back to
  the ElementFactory and is later re-prepared, any read of
  `virtInfo.UniqueId` before `UpdatePhasingInfo`/data-bind completes
  returns `null` in Uno and `""` in WinUI. Callers that do
  `UniqueId.Length == 0` or `string.IsNullOrEmpty(UniqueId)` work in
  both; callers that do `UniqueId == ""` (or rely on hashing into a
  dictionary with empty string key) diverge silently — and `null` keys
  in `Dictionary<string, ...>` throw `ArgumentNullException`.
- **Fix:** `m_uniqueId = string.Empty;`.

### 6. `ViewManager.GetElementFromElementFactory` injects an `args.Index = index;` not present in WinUI

- **File:** `ViewManager.mux.cs:745` (and the surrounding restructured
  `GetElement` local function).
- **WinUI:** `ViewManager.cpp:743-790` calls
  `RepeaterTestHooks::SetElementFactoryElementIndex(index);` — a static
  test hook, **not** an addition to the args contract.
- **Why it matters:** `ElementFactoryGetArgs.Index` is exposed to user
  factories. WinUI never sets it from production code, so any third-
  party factory that reads `args.Index` would see different values
  across WinUI vs. Uno. This is a hidden API surface widening.
- **Fix:** Either remove the assignment (Uno doesn't expose the
  RepeaterTestHooks static), or wrap it in `#if HAS_UNO` with a
  `// TODO Uno: route through RepeaterTestHooks instead` comment.

### 7. `ViewportManagerWithPlatformFeatures.SetLayoutExtent` unconditional early-return

- **File:** `ViewportManagerWithPlatformFeatures.mux.cs:204-207`
- **WinUI:** `cpp` version always recomputes
  `expectedViewportShiftX/Y`, registers the `LayoutUpdated` handler,
  accumulates `m_pendingViewportShift`, **then** invalidates arrange.
- **Uno:** if `m_layoutExtent == layoutExtent`, returns immediately,
  skipping `SetExpectedViewportShift`, the `LayoutUpdated`
  subscription, and `SetPendingViewportShift`.
- **Why it matters:** The shortcut is explained as an Android-specific
  guard against `InvalidateArrange→InvalidateMeasure` cycles, but the
  guard is **unconditional** (no `#if __ANDROID__`). On Skia/Wasm a
  concurrent shift in flight that doesn't change the extent will be
  lost.
- **Fix:** Either gate with `#if __ANDROID__`, or restructure to only
  skip the trailing `InvalidateArrange` call while preserving the
  shift-accumulation logic.

### 8. `ViewportManagerWithPlatformFeatures.OnLayoutChanged` / `EnsureScroller` add `ItemsSourceView?.Count > 0` perf-gate

- **File:** mux.cs L330-335 and L684-690.
- **Why it matters:** An `ItemsRepeater` whose `ItemsSource` is set
  *after* the first layout pass will never receive viewport-changed
  events — there is no code path that re-runs `EnsureScroller` once
  items become available. Guarded by `#if !UNO_HAS_ENHANCED_LIFECYCLE`,
  so only legacy lifecycle is affected, but that's still the default
  for many targets.
- **Fix:** When `ItemsSourceView` becomes non-empty (e.g., from
  `OnItemsSourceChanged`), force `m_ensuredScroller = false` and call
  `EnsureScroller()` again, or revisit whether the perf gate still
  pays off post `UNO_HAS_ENHANCED_LIFECYCLE` rollout.

### 9. `ViewManager.GetElementFromElementFactory` substitutes the default `<TextBlock Text='{Binding}'/>` DataTemplate via a code-built factory

- **File:** `ViewManager.mux.cs:717-723`
- **WinUI:** `XamlReader.Load(L"<DataTemplate...><TextBlock Text='{Binding}'/></DataTemplate>")`.
- **Why it matters:** The replacement is functionally equivalent for
  this template, but it's unguarded and uncommented. If a future
  WinUI sync changes the default template (e.g. adds styles), the
  Uno copy will silently fall out of parity.
- **Fix:** Wrap in `#if HAS_UNO` with a `// TODO Uno:` note explaining
  that inline `XamlReader.Load` strings are unsupported on some Uno
  targets and that the factory must mirror the WinUI XAML literally.

---

## Missing functionality

### 10. `IScrollPresenter*` anticipatory-view handlers not ported

- **File:** `ViewportManagerWithPlatformFeatures.h.mux.cs:96-99`,
  `.mux.cs:627-633`.
- **Missing:** `OnScrollPresenterScrollStarting`,
  `OnScrollPresenterZoomStarting`,
  `OnScrollPresenterViewChangeStarting`, plus matching `Completed`
  handlers and the four `m_scrollPresenter*Revoker` fields.
- **Impact:** When `ItemsRepeater` is hosted inside a `ScrollPresenter`,
  Uno cannot anticipate the scroll destination and pre-realize items
  around it — fast scrolls show blank regions until
  `EffectiveViewportChanged` fires. Documented "parity gap I6,
  blocked on IScrollPresenter2".
- **Action:** Track as a known parity gap. No fix required for this
  pass.

### 11. WinUI bug `FindFocusCandidate` (`if (!previousElement)` should be `if (!focusCandidate)`)

- **File:** `ViewManager.mux.cs:292-304`, mirrors `cpp:306-317`.
- This is a faithful 1:1 port of an upstream WinUI bug — the
  descendant-lookup fallback branch is dead code. No fix needed in the
  port; tracked here only so we don't "discover" it again.

---

## Visibility / API surface

### 12. `ElementFactoryGetArgs.Index` written from production code (see #6 above)

Already covered. The fix is to stop writing the property from
production code; the property itself can stay.

---

## Lifecycle / leak risk

### 13. Strong-reference local `var strongOwner = m_owner;` dead-stored

- **Files:** `ViewportManagerDownlevel.mux.cs:499` and
  `ViewportManagerWithPlatformFeatures.mux.cs:854`.
- **WinUI:** `auto strongOwner = m_owner->get_strong();` and the lambda
  captures it to keep the owner alive until dispatcher work runs.
- **Uno:** the lambda is `_ => OnCacheBuildActionCompleted()` — captures
  `this` instead, and `strongOwner` is never referenced. C# lambda
  capture of `this` *is* a strong root, so the owner stays alive
  transitively, but the local is misleading.
- **Action:** Either reference `strongOwner` inside the lambda body to
  preserve the documented intent, or delete the local. Combine with the
  `DispatcherQueue.TryEnqueue` fix in #3.

### 14. `OnCacheBuildActionCompleted` ignores "already cleared" case

- **File:** `ViewportManagerWithPlatformFeatures.mux.cs:618-625`.
- WinUI guards on `if (m_cacheBuildActionOutstanding)` before clearing
  the bool. Uno unconditionally writes `m_cacheBuildAction = null`. Not
  a leak per se, but masks a re-entrancy bug if the dispatched work
  fires twice (which `DispatcherQueue.TryEnqueue` would normally
  prevent).
- **Action:** Add a `MUX_ASSERT(m_cacheBuildAction != null)` to detect
  the re-entrancy regression, or guard the clear.

---

## Known Uno limitations (not bugs, but worth tracking)

### 15. `IUIElementPrivate.FocusNoActivate` unavailable in Uno

- **File:** `ViewManager.mux.cs:224` uses `focusCandidate.Focus(focusState)`
  in place of `cpp:240`
  `focusCandidate.as<IUIElementPrivate>().FocusNoActivate(focusState)`.
- Confirmed via grep: `FocusNoActivate` does not exist anywhere in the
  Uno tree.
- **Impact:** On platforms where focus can trigger window activation
  (Windows host, some Skia desktop targets), recycling a focused
  ItemsRepeater item may steal activation. Probably not observable on
  mobile/Wasm.
- **Action:** Mark the call with a `// TODO Uno: FocusNoActivate not
  available — see microsoft-ui-xaml IUIElementPrivate.` comment. If a
  bug is filed against scroll-recycle activation, consider exposing
  `FocusNoActivate` on Uno's `UIElement` as an internal extension.

### 16. `VirtualizationInfo.MoveOwnershipToLayoutFromElementFactory` assert disabled

- **File:** `VirtualizationInfo.mux.cs:39-40` (`// TODO Uno: issue
  #4691`). Already tagged; pre-existing.

### 17. `OnCompositionTargetRendering` / `UnregisterScrollAnchorCandidates` defensive `child is null` guards

- **Files:** `ViewportManagerWithPlatformFeatures.mux.cs:585-590` and
  L891-894.
- The C++ iteration assumes no nulls. The Uno guard is undocumented —
  if there is a concrete repro requiring it, mark with a `// TODO Uno:`
  comment; otherwise prefer removing it so a real bug surfaces.

---

## Dropped (rejected from source report)

- "OnFocusChanged / OnItemsSourceChanged param named `_`" — pure
  cosmetic, no behaviour difference.
- "MUX Reference header / commit drift" — out of scope per task.
- "Monolithic vs split layout" — out of scope per task.
- "`#pragma region` → `// #pragma region`" — out of scope per task.
- "XML doc / `→` comment glyph substitution" — purely cosmetic.
- "`ClearElementToElementFactory` throws `InvalidOperationException`
  instead of `hresult_error(E_FAIL)`" — exception-type style choice,
  consistent with rest of port.
- "`ClearElement` `#if DEBUG` MUX_ASSERT wrap" — `MUX_ASSERT` already
  no-ops in release; redundant wrap is harmless.
- "`UpdatePin` uses `as UIElement` (silent null) vs C++ `.as<>` (throws)"
  — Uno path is provably non-null; semantic divergence does not
  manifest.
- "Trace `%ls`/`%.0f` format leftovers" — trace macros are no-ops in Uno.
- "Class is `abstract partial` but has no `.h.mux.cs`" — out of scope.
- "`m_postArrangeToken` field dropped in Downlevel" — re-verified
  unused in WinUI cpp (only the per-`ScrollerInfo` token is wired),
  so dropping it is correct.
- "`ScrollerInfo` struct vs class" — current `Add(scrollerInfo)` after
  field mutation is safe; flagged only as foot-gun, not a real bug.
- "`SetExpectedViewportShift` `double` vs `float`" — `Point` is double
  in C#; widening is correct.
- "`GetLayoutId()` direct property access" — Uno's `Layout` exposes the
  property publicly; simplification is correct.
- "`MUX_ASSERT(!m_managingViewportDisabled)` → `Debug.Assert(...)` in
  one site" — style only.
- "`OnViewportChanged`/`OnConfigurationChanged` parameter naming" —
  cosmetic.
- "`Focus` return-value discarded" — also discarded in WinUI by
  language (void caller); style only.
- "`UpdateFocusedElement` mixes `is null` / `!= null`" — cosmetic.
- "`OnCompositionTargetRendering` uses fully-qualified
  `CompositionTarget`" — cosmetic.
- "Commented-out `UpdateViewport(new Rect())` for null-scroller path
  (microsoft-ui-xaml#2349)" — port author documented the intentional
  divergence; not a parity bug.
- "`UpdatePhasingInfo` always allocates `WeakReference<>`" —
  `WeakReference<T>(null)` is legal in .NET; behaviour matches.
