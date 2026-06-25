# Distilled diff: Selection system

**Source report:** _ComparisonReport_Selection.md
**WinUI commit:** 4b206bce3

## TL;DR

Three issues actually matter:

1. **`SelectedItems<T>` has a live `~SelectedItems()` finalizer** that mutates `m_infos` on the GC thread. Confirmed in source (lines 28-31 of `SelectedItems.cs`). Non-deterministic, unnecessary in managed code, and out of step with the `#if HAS_UNO` finalizer-suppression pattern used for `SelectionModel`/`SelectionNode`.

2. **`SelectionNode` `auto_revoke` was flattened to direct `+=`/`-=`** rather than `SerialDisposable` + `Disposable.Create(...)`. The fallout: `ClearSelection` and `OnItemsRemoved` had to add `#if HAS_UNO` blocks that walk children and manually unhook — extra Uno-specific control flow that wouldn't exist with the standard revoker pattern. No leak observed yet, but the cleanup contract is now duplicated across three call sites.

3. **`SelectionModel.ResolvePath` auto-resolve type check** uses generic `IList<object>` / `IEnumerable<object>` where WinUI uses non-generic `IBindableVector` / `IBindableIterable`. Generic XAML data sources (`List<MyType>`, `ObservableCollection<MyType>` with `T != object`) silently fail to auto-resolve as child sources — they only match `IList<object>` if `T == object`.

Most of the other 41 findings in the source report are layout/header/style and have no behavioural impact.

## Confirmed behavioural / correctness issues

### 1. `SelectionModel.ResolvePath` interface check uses wrong types

**File:** `SelectionModel.mux.cs` lines 554-557.

```csharp
if (data is ItemsSourceView ||
    data is IBindableObservableVector ||
    data is IList<object> ||
    data is IEnumerable<object>)
```

WinUI uses `IBindableVector` / `IBindableIterable` (non-generic, XAML-projected) and `IObservableVector<IInspectable>`. The Uno port substitutes generic-covariant `IList<object>` / `IEnumerable<object>`, which is **not** equivalent: `List<MyType>` is not an `IList<object>` unless `MyType == object`. Result: any non-`object` generic collection that the user does **not** wrap in `ItemsSourceView` and for which they did not subscribe `ChildrenRequested` will be treated as a leaf instead of being auto-resolved as a child source.

**Severity:** Medium-High. Soft for the typical case (Repeater wraps via `InspectingDataSource` upstream), but a real divergence from documented `SelectionModel` auto-resolution.

**Fix sketch:** Replace with `data is System.Collections.IList || data is System.Collections.IEnumerable` (matching the XAML non-generic projections) and keep the `IBindableObservableVector` branch — or use the non-generic `IBindableVector` / `IBindableIterable` types that Uno already projects in `Microsoft.UI.Xaml.Interop`.

## Missing functionality

None of substance. The C++ `SelectedItems<T>` template intentionally throws `E_NOTIMPL` from `IndexOf` and `GetMany`; the C# port omits them because `IReadOnlyList<T>` doesn't require them. No behavioural gap.

## Visibility / API surface

No real narrowing on public surface.

`IndexPath` (the only Selection type with an IDL projection — declared `runtimeclass IndexPath` in `ItemsRepeater.idl` lines 639-649) keeps `public sealed partial class IndexPath : IStringable` with the three `CreateFrom*` factories and `CompareTo`/`GetSize`/`GetAt`/`ToString` all public. OK.

The source report's "visibility narrowing" findings on `SelectionNode.IndexPath`, `SelectionNode.AnchorIndex`, and `SelectionNode.RealizedChildrenNodeCount` are not real concerns — `SelectionNode` is an internal C++ class with no IDL projection and is `internal sealed` in both implementations. C++ `public:` blocks are access control for internal callers, not a WinRT public boundary. Verified: `RealizedChildrenNodeCount` is only used inside `SelectionNode.cpp:721`; `IndexPath()` is only used inside the class.

## Lifecycle / leak risk

### 1. `SelectedItems<T>` has a live finalizer (HIGH)

**File:** `SelectedItems.cs` lines 28-31.

```csharp
~SelectedItems()
{
    m_infos.Clear();
}
```

Confirmed by direct read. Other Selection types correctly comment out destructors under `#if HAS_UNO` (e.g. `SelectionNode.mux.cs` lines 27-32). Risks:
- Touches `m_infos` (a possibly-shared `IList<SelectedItemInfo>`) on the GC finalizer thread.
- `m_infos` is supplied by `SelectionModel` (`new SelectedItems<IndexPath>(infos, ...)`) and shared with the caller's cache lifetime — clearing it from the finalizer can race the next `SelectedIndices` access.
- Adds finalizer-queue pressure for short-lived `SelectedIndices`/`SelectedItems` reads.

**Fix:** Wrap in `#if HAS_UNO` comment block matching the SelectionModel/SelectionNode pattern. The C++ `m_infos.clear()` is redundant in managed code (`m_infos` becomes collectable when `SelectedItems<T>` does).

### 2. `SelectionNode` revoker flattened to direct subscribe/unsubscribe (HIGH)

**File:** `SelectionNode.h.mux.cs` lines 32-34; `SelectionNode.mux.cs` lines 39-58, 314-328, 454-465, 663-670.

Confirmed:
- `m_itemsSourceViewChanged` revoker field is replaced by a TODO comment with **no** `SerialDisposable` substitute.
- `Source` setter, `HookupCollectionChangedHandler`, `UnhookCollectionChangedHandler` use direct `+=` / `-=`.
- `ClearSelection` (lines 454-465) and `OnItemsRemoved` (lines 663-670) had to gain extra `#if HAS_UNO` blocks that walk children and manually call `UnhookCollectionChangedHandler` to compensate for the absence of shared_ptr destruction.

**Subscription lifetime risk:** Low for the steady state (every `Source` setter calls `UnhookCollectionChangedHandler` first). But the manual cleanup contract is now duplicated in three places, and any future code path that drops a `SelectionNode` without going through `Source = null` or `ClearSelection` will leak the `CollectionChanged` handler on `ItemsSourceView`, which in turn keeps the data source rooted.

**Fix sketch:** Add `private readonly SerialDisposable m_itemsSourceViewChanged = new();` field; in `HookupCollectionChangedHandler` set `m_itemsSourceViewChanged.Disposable = Disposable.Create(() => m_dataSource.CollectionChanged -= OnSourceListChanged);`; in `UnhookCollectionChangedHandler` set `.Disposable = null`. The `#if HAS_UNO` blocks in `ClearSelection`/`OnItemsRemoved` can then be removed (the `SelectionNode` reference drop will let the `SerialDisposable` finalize the handler) — note this still relies on the finalizer chain, so explicit disposal in those two spots remains the cleanest path; the gain is just moving the subscription identity into one place.

## Dropped (rejected from source report)

- **Monolithic .cs layouts** (`SelectionModelChildrenRequestedEventArgs`, `SelectedItems`, `IndexPath`, `IndexRange`) — file layout only, no behaviour impact.
- **MUX Reference header missing / stale commit `de78834` on `IndexPath.cs` and `IndexRange.cs`** — comment hygiene, no behaviour impact (pervasive across Selection support types; recorded once).
- **`#pragma region` -> `// #pragma region` comments** in `SelectionModel.mux.cs` — cosmetic.
- **`Type ICustomPropertyProvider.Type` returns `GetType()` instead of WinRT class-name `TypeName`** — `ICustomPropertyProvider` is consumed only by XAML binding diagnostics; the IDL exposes nothing dependent on `Kind = Metadata`.
- **`IndexRange` is `class` not `struct`** — verified: reads in `SelectionNode.OnItemsAdded/Removed` reassign `m_selected[i]` immediately, so the alias-vs-copy difference is unobservable. `Split` writes to its `ref` outputs, not the source range.
- **`IndexRange` adds `operator !=`, `Equals`, `GetHashCode` without `#if HAS_UNO`** — required because `List<IndexRange>.Remove` uses equality; not a divergence, an obligatory C# adaptation.
- **`SelectedItemsEnumerator` is Uno-only and not guarded** — required adapter for `IEnumerator<T>` in absence of WinRT `IIterator<T>`. Style note only.
- **`IndexPath` missing `IVector<int>` ctor overload** — `CreateFromIndices(IList<int>)` covers all Repeater callers; the WinRT `IVector<Int32>` projection lands on `IList<int>` from C#.
- **`IndexRange.Begin/End` off-by-one** — explicitly verified by source report cross-check: inclusive `[Begin, End]` semantics match WinUI exactly. `Contains`, `Intersects`, `Split` all line up.
- **Tree-walk logic** in `SelectionTreeHelper` — verified faithful by source report; depth-first stack-based traversal matches including `MUX_ASSERT(start.CompareTo(end) == -1)`.
- **`SelectionNode` visibility narrowing on `IndexPath`/`AnchorIndex`/`RealizedChildrenNodeCount`** — verified no external callers; `SelectionNode` has no IDL projection. Source report flagged these but they are not API-observable.
- **Style / formatting / empty-line counts / TODO comment phrasing** — no behavioural impact.
