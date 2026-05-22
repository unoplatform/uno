# Distilled diff: Transitions

**Source report:** _ComparisonReport_Transitions.md
**WinUI commit:** 4b206bce3

## TL;DR

Across the Transitions slice there are no clearly visible runtime crashes, but two
items deserve attention:

1. **Public API nullability drift** on `ItemCollectionTransitionProgress.Transition`
   and `.Element` — both became C# nullable (`?`) whereas the WinUI IDL exposes
   them as non-nullable WinRT references. This propagates `?` annotations into the
   public surface that downstream consumers will see.
2. **Strong reference where WinUI uses `weak_ref`** for
   `ItemCollectionTransition._owningProvider`. Not a leak today (provider owns the
   transitions through its maps, so the cycle is bounded by the keep-alive timer),
   but it diverges from the C++ ownership model and is undocumented.

The destructor-replacement-by-comment story in `ItemCollectionTransitionProvider`
is correct per .NET semantics (DispatcherTimer's Tick delegate keeps the provider
alive until the timer fires, then it self-unregisters), so it is **not** a real
lifecycle bug. It is, however, fragile if the design ever changes — flagged as a
note, not a bug.

## Confirmed behavioural / correctness issues

None confirmed. The two semantic differences below were investigated and found to
be non-issues at runtime:

- `OnTransitionProviderChanged` unconditionally clears
  `m_transitionCompletedRevoker.Disposable = null` outside the `if (m_transitionProvider != null)`.
  `SerialDisposable.Disposable = null` is a no-op when already null, so behaviour
  is equivalent to the WinUI flow that revokes only when the previous provider
  was non-null. No drift.
- `QueueTransition`'s `TryGetValue` path replacing the C++
  `if (transitionsWithAnimations)` guard. In both implementations the guard is
  effectively always true when reached (the entry was just inserted on the
  `usesNewTransitionsBatch` branch). No drift.

## Missing functionality

None. All WinUI methods are present with matching signatures and ordering.

## Visibility / API surface

- **`ItemCollectionTransitionProgress.Transition` and `.Element` are declared `T?`** in Uno
  (`ItemCollectionTransitionProgress.h.mux.cs:21-22`, `ItemCollectionTransitionProgress.mux.cs:22-25`).
  The IDL (`ItemsRepeater.idl:340`) declares them as non-nullable
  (`ItemCollectionTransition Transition{ get; };`). WinUI's C++ implementation can
  still return null in practice (the weak ref may have expired), but the WinRT ABI
  exposes them as non-null references. The public C# surface should match — drop
  the `?` annotations or document the deviation if it is deliberate.
  *Note:* this matches a recurring pattern in the slice
  (cf. the same issue called out for `ItemCollectionTransitionCompletedEventArgs.Element`
  — there it is correctly non-nullable).

- **`ItemCollectionTransitionTriggers` underlying type widened to `uint`**
  (`ItemCollectionTransitionTriggers.cs:13`). The IDL omits the underlying type,
  which makes it `Int32` in the projection. Although both are 32-bit, `uint` is
  not CLS-compliant and changes the WinRT/COM ABI shape. Drop the explicit
  `: uint` to match WinUI exactly.

## Lifecycle / leak risk

- **`ItemCollectionTransition._owningProvider` is a strong reference; WinUI uses `weak_ref`**
  (`ItemCollectionTransition.h.mux.cs:12`, WinUI `ItemCollectionTransition.h:47`).
  Not a leak today: the provider strongly owns each transition through
  `_transitionsMap` / `_transitionsWithAnimationsMap`, so the cycle resolves when
  the batch is cleaned (`OnRendering` -> `CleanTransitionsBatch` or after the
  5s keep-alive timer). Sister class `ItemCollectionTransitionProgress` already
  uses the correct `ManagedWeakReference` pattern. Recommend aligning
  `_owningProvider` with the same pattern, or adding a `// Uno:` justification.

- **`ItemCollectionTransitionProvider` does not port the C++ destructor that stops
  every keep-alive `DispatcherTimer`.** Investigated and verified: in .NET the
  chain `Dispatcher -> DispatcherTimer -> Tick delegate -> Provider -> map` keeps
  the provider alive until each timer fires (then `OnKeepAliveTimerTick` stops the
  timer and removes it from `_keepAliveTimersMap`). So no runtime leak today. The
  one observable difference vs WinUI: if a host explicitly releases the provider
  while keep-alive timers are still running, WinUI's destructor would stop them
  immediately; in Uno they continue ticking for up to 5 s. Not a bug, but the
  `#if HAS_UNO` block in `ItemCollectionTransitionProvider.mux.cs:16-25`
  currently exists only as commentary inside a preprocessor block. Recommend
  either lifting the explanation to a plain `// Uno:` comment header (so it
  cannot be accidentally removed by an `#if HAS_UNO`-stripping refactor) or
  adding an explicit `Stop()` pass via an `IDisposable`/Unloaded hook for parity.

## Dropped (rejected from source report)

- MUX reference commit hash mismatch (`5f9e851133b3` / `tag winui3/release/1.8.1`
  vs `4b206bce3`) — pervasive across this group; tracked as one overarching item:
  **MUX headers across this group reference wrong commit.**
- Missing copyright/license header on `TransitionManager.{h.mux,mux,uno}.cs` — style only.
- Naming `m_xxx` vs `_xxx` inconsistency inside `TransitionManager.*` vs the rest
  of the slice — style only.
- `// #pragma region` / `// #pragma endregion` as comments instead of `#region`/`#endregion` — style only.
- `try/finally` vs `gsl::finally` in `OnRendering` — semantically identical.
- `OnItemsSourceChanged` parameter rename / unused `source` — signature equivalent.
- `MUX_ASSERT(args.Element != null)` not present in either implementation — not a divergence.
- `m_transitionProvider(owner)` C++ initializer list vs Uno null field — `tracker_ref` binding only, not a value assignment.
- `Complete()`'s `?.NotifyTransitionCompleted(transition)` defensive null guard — `OwningProvider()` returns the strong field set in the constructor, so it should never be null; harmless guard.
- `ItemCollectionTransitionCompletedEventArgs._transition` strong vs `tracker_ref` — args are short-lived per-event; no leak.
- Missing XML doc / trace parity / file layout / `properties.h` order — style only.
