# Distilled diff: Misc utilities

**Source report:** _ComparisonReport_Misc.md
**WinUI commit:** 4b206bce3

(Layout/header style nits from the source report dropped: file split, MUX header presence, region pragmas, XML doc, trace parity, naming.)

## TL;DR

- Two confirmed sort-comparator inversions (BuildTreeScheduler `OnRendering`, Phaser `SortElements`) cause real ordering drift vs WinUI.
- `RepeaterTestHooks` drops the static `s_elementFactoryElementIndex` storage and the `SetElementFactoryElementIndex` setter; the renamed `GetElementFactoryElementIndex(object)` now reads the args.Index instead of the value `ViewManager` stashed in WinUI — tests that depend on observing the index produced inside `GetElementCore` cannot work.
- `RepeaterTestHooks` also lacks the `s_testHooks` / `EnsureHooks` / `GetGlobalTestHooks` instance pattern that `LayoutsTestHooks` already mirrors, and uses `EventHandler` for `BuildTreeCompleted` instead of `TypedEventHandler<object, object>`.
- `ChildrenInTabFocusOrderIterable` drops `HasCurrent` / `GetMany` (relevant only to WinRT `IIterator<T>` consumers).
- `LayoutsTestHooks.NotifyLayoutAnchorIndexChanged` / `NotifyLayoutAnchorOffsetChanged` are instance methods; WinUI exposes them as `static` (call sites use them as statics).

## Confirmed behavioural / correctness issues

### 1. BuildTreeScheduler `OnRendering` sort comparator is inverted

`BuildTreeScheduler.mux.cs:44`

```csharp
m_pendingWork.Sort((lhs, rhs) => lhs.Priority - rhs.Priority); // ascending
```

WinUI `BuildTreeScheduler.cpp:36` uses a strict-less predicate that returns `lhs.Priority() > rhs.Priority()`, so under `std::sort` highest priority lands at the FRONT, lowest at the BACK. WinUI then walks from `size - 1` downward, processing lowest priority first (and the comment in code says "descending order ... work from the end").

Uno's `lhs - rhs` sorts ascending (lowest at front, highest at back), and the loop walks from `Count - 1`. Result: Uno consumes highest priority first — the exact opposite of WinUI, and inconsistent with the preserved comment.

Fix: flip to `rhs.Priority - lhs.Priority` (or `rhs.Priority.CompareTo(lhs.Priority)`).

### 2. Phaser `SortElements` visible-window branch is inverted

`Phaser.mux.cs:201-228`

```csharp
else if (lhsIntersects) { return -1; } // lhs (visible) sorts FIRST
else                    { return  1; }
```

WinUI `Phaser.cpp:204-234` strict-less predicate: `lhsIntersects → false` (lhs not less → lhs to BACK), `else → true` (lhs less → lhs to FRONT). So in WinUI, visible items end up at the BACK of the vector, and the consumer (`DoPhasedWorkCallback`) walks from `size - 1` downward — visible items are processed first.

Uno's `-1`/`1` returns put visible items at the FRONT, so the back-to-front walk processes non-visible items first. Combined with the preserved comment "Sort in descending order (inVisibleWindow, phase)" being false, phasing prioritization is regressed.

Fix: swap the branches (`lhsIntersects → 1`, `else → -1`). Also swap the in-group phase comparison to `rhs.Phase - lhs.Phase` so higher phases land at the back and are processed first, matching WinUI's `<` predicate combined with back-to-front consumption.

## Missing functionality

### 3. `RepeaterTestHooks.SetElementFactoryElementIndex` + static storage

WinUI declares `static int s_elementFactoryElementIndex;` and the pair `GetElementFactoryElementIndex()` / `SetElementFactoryElementIndex(int)` (RepeaterTestHooks.h:29-30, .cpp:49-58, .idl:14-15). `ViewManager.cpp:787` calls `SetElementFactoryElementIndex(index)` inside `GetElement`; tests like `MockViewGenerator.cs` and `RecyclingViewGeneratorDerived.cs` call the parameterless `GetElementFactoryElementIndex()`.

Uno (`RepeaterTestHooks.cs:22-26`) replaces this with `GetElementFactoryElementIndex(object getArgs)` that reads `(args as ElementFactoryGetArgs).Index`. The setter and static field are absent, and `ViewManager` does not stash anything. WinUI parity tests that observe the index emitted during the factory call cannot run unmodified.

Fix: add `internal static int s_elementFactoryElementIndex;`, restore the WinUI-shaped `GetElementFactoryElementIndex()` / `SetElementFactoryElementIndex(int)`, and call the setter from Uno's `ViewManager.GetElement` equivalent. Keep the existing args-based helper under a different name if it is still needed internally.

### 4. `RepeaterTestHooks` lacks `s_testHooks` / `EnsureHooks` / `GetGlobalTestHooks`

WinUI declares an instance event `m_buildTreeCompleted` plus the static accessor `GetGlobalTestHooks()` and (in the projection layer) `EnsureHooks` to lazily allocate the singleton (RepeaterTestHooks.h:18-21, 36-38). Uno (`RepeaterTestHooks.cs`) keeps `BuildTreeCompleted` as a static `event EventHandler`, with no instance/singleton plumbing.

This is a parity break with `LayoutsTestHooks.cs:14-24`, which already provides `s_testHooks` + `EnsureHooks` + `GetGlobalTestHooks`. Tests that retrieve the singleton, or that subscribe via the instance, will not work.

Fix: add the same `s_testHooks` / `EnsureHooks` / `GetGlobalTestHooks` pattern, and route subscribe/unsubscribe/notify through it.

### 5. `RepeaterTestHooks.BuildTreeCompleted` delegate type drift

WinUI uses `TypedEventHandler<IInspectable, IInspectable>` (RepeaterTestHooks.h:23, .idl). Uno uses `EventHandler` (`(object, EventArgs)`). The delegate signatures are not compatible, so reflection-based test hookup will diverge. `LayoutsTestHooks` already standardized on `TypedEventHandler<object, object>`.

Fix: change `BuildTreeCompleted` to `TypedEventHandler<object, object>`.

### 6. `LayoutsTestHooks` missing LinedFlowLayout surface (tracked under PR 9)

`LinedFlowLayoutSnappedAverageItemsPerLineChanged`, `LinedFlowLayoutInvalidated`, `LinedFlowLayoutItemLocked` events; `LayoutsTestHooksLinedFlowLayoutInvalidatedEventArgs` / `LayoutsTestHooksLinedFlowLayoutItemLockedEventArgs` runtimeclasses; and `LinedFlowLayoutInvalidationTrigger` enum are absent. The body stubs for all `GetLinedFlowLayout*` / `SetLinedFlowLayout*` / `LayoutInvalidateMeasure(relayout: true)` paths are also intentionally inert.

Already gated with `// TODO Uno: ... PR 9` comments. Listed here only as a reminder that the parity surface lands with the LinedFlowLayout port — no action required in this PR beyond tracking.

## Visibility / API surface

### 7. `LayoutsTestHooks.NotifyLayoutAnchorIndexChanged` / `NotifyLayoutAnchorOffsetChanged` are instance, WinUI exposes them as `static`

`LayoutsTestHooks.cs:209-212, 223-226` are `internal void Notify...(...)`. WinUI declares them `static` (LayoutsTestHooks.cpp:338-346, 366-374), with bodies that `EnsureHooks()` then dispatch through `s_testHooks->m_...EventSource`. Call sites in WinUI use them as statics. Uno's instance form requires a caller to first obtain the singleton.

Fix: make both `static`, call `EnsureHooks()`, and dispatch via `s_testHooks.m_...EventSource?.Invoke(...)`.

### 8. `ChildrenInTabFocusOrderIterator` drops `HasCurrent` and `GetMany`

WinUI inline-defines `bool HasCurrent()` and `uint32_t GetMany(array_view<DependencyObject>)` (ChildrenInTabFocusOrderIterable.h:28-62). C# `IEnumerator<T>` has no equivalents, and the only Uno call path uses `IEnumerable<DependencyObject>`. The functional impact is limited to any consumer that goes through the WinRT `IIterator<T>` projection (e.g., XAML focus diagnostic hooks). Reflection-based parity callers will not bind.

Fix: at minimum, mark the deliberate `IIterator` → `IEnumerator` substitution with `// TODO Uno:` so future readers don't think it was an accidental drop. Restoring the two helpers as `internal` methods is cheap if any caller starts to need them.

## Lifecycle / leak risk

### 9. `BuildTreeScheduler` lazy-init makes `ShouldYield()` brittle on fresh threads

`BuildTreeScheduler.mux.cs:23-28` lazy-allocates `m_pendingWork` and starts `m_timer` only inside `RegisterWork`. `ShouldYield()` directly reads `m_timer.ElapsedMilliseconds` (line 35) without a null check. If `ShouldYield()` is called on a thread before any `RegisterWork`, it `NullReferenceException`s. WinUI's `thread_local QPCTimer m_timer{};` is constructed automatically per thread, so this asymmetry only exists in Uno.

Fix: move the same null-check + lazy-init into `ShouldYield` (and `QueueTick` for consistency), or move it into a single helper invoked at every entry point.

## Dropped (rejected from source report)

- BuildTreeScheduler M1 (`Restart` vs `Reset`) — semantically correct mapping for `Stopwatch`.
- BuildTreeScheduler M3 (`TraceLoggingProviderWrite` commented out) — trace parity gap, out of scope.
- BuildTreeScheduler L2/L3 (`Stopwatch` vs `QPCTimer`) — functionally equivalent on Windows/.NET.
- BuildTreeScheduler L4 (`winrt::event_token` → `bool` substitution) — correct, documented.
- BuildTreeScheduler I1 (`WorkInfo` placement) — style.
- ChildrenInTabFocusOrderIterable M1 (`m_index = -1` start) — required by `IEnumerator<T>` contract; `Current`-before-`MoveNext` is undefined per spec.
- ChildrenInTabFocusOrderIterable M2 (`tracker_ref` → readonly field) — standard conversion rule.
- ChildrenInTabFocusOrderIterable L1–L4 — header/style/dead-code cleanup.
- CustomProperty L1–L2 — implicit init / interface listing — style.
- Phaser M1 (`ElementInfo` ctor trace) — trace parity gap.
- Phaser L1 (`ElementInfo` ctor placement) — file layout.
- Phaser L3–L5 — closure inlining / unused local / message phrasing.
- UniqueIdElementPool M1/M2/L1–L3/I1–I2 — file layout, MUX header, message wording, double lookup.
- RepeaterTestHooks H1 — file layout.
- RepeaterTestHooks M2 (extra `CreateRepeaterElementFactory*Args`) — Uno-specific additive helpers, mark with `#if HAS_UNO` only if reorganizing the file.
- RepeaterTestHooks L1/L2 — comments / `public` on internal class.
- LayoutsTestHooks H1 — file layout.
- LayoutsTestHooks M2 (`IDisposable` vs `event_token`) — standard revoker rule.
- LayoutsTestHooks M3 (`m_` event field naming) — style.
- LayoutsTestHooks L1–L3 — visibility, PR 9 stubs.
- _Tracing I1, CachedVisualTreeHelpers M1/L1/I1, IKeyIndexMapping L1, IPanel L1/I1, IRepeaterScrollingSurface M1/L1, ScrollOrientation L1 — Uno-only helpers / interface ordering / naming.
