# Distilled diff: Factories + Data Sources

**Source report:** _ComparisonReport_FactoriesAndSources.md
**WinUI commit:** 4b206bce3

## TL;DR

The source report flags 16 "High" items, but after verification against the
WinUI IDL and headers, **only two are real port concerns** and one of those
is a contained lifecycle smell rather than a runtime correctness bug:

1. `ItemsSourceView.*Core` overrides collapsed from `virtual` to
   `private protected` in the merged base class — breaks any consumer-side
   `ItemsSourceView` subclass that wants to override them, which the WinUI
   IDL explicitly allows (header uses `virtual ... Core()` in
   `Consume API for internal use only.` region). See "API surface" below.
2. `ItemsSourceView` has a finalizer (`~ItemsSourceView`) that calls
   `UnListenToCollectionChanges()`. The `_collectionChangedListener` is a
   `Disposable.Create(...)` revoker so the finalizer is functionally
   redundant on the happy path, but it violates rule #8 and adds an
   unnecessary GC.SuppressFinalize requirement on consumers that hold
   long-lived references. See "Lifecycle / leak risk".

Everything else flagged in the source report was verified against the IDL
(`ItemsRepeater.idl` lines 137-148 for `ItemsSourceView`, lines 593-612 for
`RecyclePool`) or against prior Repeater port decisions and is either
factually wrong, a non-IDL surface that is correctly internal, or
WinRT-only plumbing (`tracker_ref` / `EnableTracking`) with no .NET
analogue.

## Confirmed behavioural / correctness issues

None. The `OnCollectionChanged` Move decomposition is **intentional and
correct** — verified by commit `7879c056a4`
(`refactor(Repeater): Move INCC.Move decomposition to ItemsSourceView bridge`).
WinUI's `IObservableVector<T>` collection-change action set has no `Move`,
so ItemsRepeater + ViewManager assume Remove/Add only. Decomposing
`NotifyCollectionChangedAction.Move` into a Remove followed by an Add at
the bridge layer preserves move *semantics* (same items, same final
positions) while keeping the ported control 1:1 with WinUI. Move is not
"lost" — it is translated into the two atomic actions the WinUI port
relies on.

## Missing functionality

None.

- **`ItemTemplateWrapper.EnableTracking` — verified not needed.**
  The WinUI `.h` (lines 22-28) documents `EnableTracking` as
  `tracker_ref` / `ITrackerHandleManager` plumbing whose sole purpose is
  to break a WinRT reference-counting cycle
  (`ItemsRepeater → ItemTemplateWrapper → DataTemplate → RecyclePool →
  m_owner → ItemsRepeater`). .NET GC walks cycles directly, so neither
  the `tracker_ref<DataTemplate>` field nor the `EnableTracking` call
  has a runtime analogue. Grep across the Repeater tree finds
  `EnableTracking` only in comparison-report markdown — there is no
  ItemsRepeater.mux.cs call site to wire up. Already rejected in
  `_DistilledDiffItemsRepeater.md` lines 71-75.

## Visibility / API surface

- **`ItemsSourceView.*Core` overrides collapsed to `private protected`.**
  WinUI `ItemsSourceView.h` lines 28-37 declares `GetSizeCore`,
  `GetAtCore`, `HasKeyIndexMappingCore`, `KeyFromIndexCore`,
  `IndexFromKeyCore`, `IndexOfCore` as `virtual` inside
  `Consume API for internal use only.`, then `InspectingDataSource`
  overrides them. The IDL marks the runtime class `unsealed`
  (`ItemsRepeater.idl:137`), so consumer-side derivation is the
  documented WinUI design. Uno fused both classes into one
  (`ItemsSourceView.Impl.cs` lines 88-137) and marked every `*Core`
  method `private protected` and non-virtual. Any custom
  `ItemsSourceView` subclass that wants to plug in a non-INCC backing
  store cannot replace the protected override — they currently get
  sealed-in `InspectingDataSource` behaviour or hit `NotImplementedException`.
  - Fix: make the six `*Core` methods `protected virtual` on
    `ItemsSourceView`, keep the merged implementations as default
    implementations there (or move them back to `InspectingDataSource`
    as overrides and restore the `throw NotImplementedException()`
    defaults on the base — closer to WinUI shape).

- **`ItemsSourceView.IndexOf` narrowed from public to internal.**
  Verified against IDL (`ItemsRepeater.idl:147`):
  `Int32 IndexOf(IInspectable item);` is part of the public
  `runtimeclass ItemsSourceView` surface. Uno marks it
  `internal int IndexOf(object item)` in `ItemsSourceView.cs:53`.
  Should be `public`.

- **`ItemsSourceView.OnItemsSourceChanged` narrowed to `private protected`.**
  C++ exposes it in the `Consume API for internal use only.` region as a
  plain (non-virtual) member function intended to be reachable by
  derived types implementing custom data sources. In C# terms the
  closest match is `protected` (or `protected internal`), not
  `private protected`. `private protected` means "derived classes in
  the same assembly only", which is strictly tighter than the WinUI
  intent. Recommend `protected` to match the C++ access surface.

- **`HasKeyIndexMapping` is correctly a property in Uno** (verified
  IDL line 144: `Boolean HasKeyIndexMapping{ get; };`). Source report
  flagged this as needing verification — confirmed already correct.

- **`RecyclePool.{ReuseKeyProperty, GetReuseKey, SetReuseKey}` internal
  is correct.** Verified against the public IDL
  (`ItemsRepeater.idl:593-612`): the `RecyclePool` runtimeclass exposes
  only the `PoolInstance` family, the `PutElement` / `TryGetElement`
  overloads, and the `*Core` virtuals. `ReuseKey*` is **not** in the
  public IDL — Uno's `internal` is the correct projection. Source
  report finding rejected.

- **`ElementFactoryGetArgs.Index` internal is correct.** WinUI puts the
  `Index()` getter/setter outside `#pragma region IElementFactoryGetArgs`,
  which is the public interface region. The C++ "downlevel" class is a
  WUX/MUX bridging shim. Uno's `internal` projection matches the
  effective public surface.

## Lifecycle / leak risk

- **`~ItemsSourceView()` finalizer (`ItemsSourceView.Impl.cs:82-85`).**
  Confirmed present. It calls `UnListenToCollectionChanges()`, which
  disposes `_collectionChangedListener` (a `Disposable.Create(...)`
  revoker pointing back at the source collection via an `incc`/
  `IObservableVector`/`IBindableObservableVector` event subscription).
  Two real concerns:
  1. Rule #8 forbids finalizers.
  2. The finalizer creates a long GC chain back to the source
     collection, because the lambda inside `Disposable.Create` captures
     the source. If the source outlives the `ItemsSourceView` (common
     when `ItemsSource` is a long-lived `ObservableCollection` and the
     repeater scrolls offscreen), the finalizer will eventually run on
     the GC finalizer thread and detach the handler. This is
     redundant work — every realistic consumer either disposes the
     view via repeater teardown or never disposes it; in neither case
     is the finalizer load-bearing.
  - Fix: delete the finalizer. The repeater already calls
    `UnListenToCollectionChanges` indirectly when the source changes
    (via the ctor on the replacement view picking up the new
    listener). If a leak is observed in a specific scenario, the
    correct fix is to make `ItemsSourceView` `IDisposable` and have
    the repeater dispose it on `ItemsSource` set, not to rely on
    finalization.

- **`_collectionChangedListener` is a single `IDisposable`, not
  `SerialDisposable`.** Worth a follow-up but not a correctness bug
  here — the listener is assigned exactly once (in the ctor) and
  never re-listens, so `SerialDisposable`'s reset semantics are not
  needed.

## Dropped (rejected from source report)

- **High — Missing MUX Reference headers** on multiple files. Style only.
- **High — `ReuseKey*` visibility mismatch.** Rejected — verified not in
  public IDL.
- **High — `RecyclePool.Clear()` Uno-only without TODO marker.** Marker
  text style, no behavioural impact.
- **High — `ElementFactoryGetArgs.Index` visibility.** Rejected — outside
  the public C++ region; current internal is correct.
- **High — Missing XML docs on `ElementFactoryRecycleArgs` /
  `SelectTemplateEventArgs`.** Doc gap only.
- **High — `SelectTemplateEventArgs` setter visibility narrowed.** The
  source report's own analysis (line 383) concludes Uno's `internal set`
  is correct against the IDL region.
- **High — `RecyclingElementFactory` ctor body relocated to field init.**
  Behaviourally identical; verified `m_templates` field initializer
  produces the same default value.
- **High — `ItemTemplateWrapper.EnableTracking` missing.** WinRT
  `tracker_ref` plumbing, no .NET analogue. See "Missing functionality".
- **High — `OnCollectionChanged` Move decomposition lacks `// TODO Uno:`
  marker.** The block is intentional Uno-bridge behaviour with a
  comment already in place; the marker phrasing is style only and the
  semantics are correct (verified via commit `7879c056a4`).
- **High — `InspectingDataSource` source-type detection differs.** WinUI
  C++ uses WinRT projection (`IVector<IInspectable>` etc.); Uno uses the
  standard .NET projection (`IList<object>`). This *is* the WinRT-to-.NET
  ABI mapping; not a divergence.
- **Medium — Monolithic `.cs` vs three-file `.cs` / `.h.mux.cs` /
  `.mux.cs` split.** Layout only.
- **Medium — `#pragma region` text not preserved verbatim.** Style only.
- **Medium — Comment paraphrasing.** Documentation only.
- **Medium — `m_args = new SelectTemplateEventArgs()` vs `winrt::make<>`.**
  Standard projection.
- **Medium — `IsNullOrEmpty` vs `empty()` in
  `OnSelectTemplateKeyCore`.** Functionally equivalent (`templateKey`
  is sourced from a property and is never null in practice).
- **Medium — `IDictionary` vs `IMap`.** Standard projection.
- **Medium — Stale MUX commit shas `37ade09` / `dc8d573` on
  `ItemsSourceView`.** Documentation only — bump on next sync pass.
- **Medium — `UnListenToCollectionChanges` simplified to a single
  `IDisposable`.** Functionally equivalent; the C++ three-tracker
  pattern exists because WinRT cannot use a polymorphic disposable.
- **Medium — `_collectionChangedListener` underscore prefix.** Naming
  style.
- **Medium — `OnUntypedVectorChanged` is Uno-only.** Bridges
  `IObservableVector` (untyped) which is a legacy Uno
  interface; no WinUI parity impact.
- **Low — Unused `using System.Linq;`, missing XML docs, `IPanel`
  vs `Panel`, target-typed `new()`, field initializer style, etc.**
  All cosmetic per the brief.
