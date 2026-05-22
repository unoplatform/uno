# Distilled diff: Layout Context types

**Source report:** _ComparisonReport_LayoutContexts.md
**WinUI commit:** 4b206bce3

## TL;DR

Two real lifecycle bugs in the Uno port: `RepeaterLayoutContext.GetOwner()` throws on a collected `ItemsRepeater` (WinUI returns null and `LayoutStateCore` getter relies on that null tolerance), and `VirtualLayoutContextAdapter.Iterator.MoveNext` throws past the end (violating `IEnumerator` contract). Everything else in the source report is either cosmetic (file split, MUX headers, comments) or a non-issue once the IDL constraints are considered.

## Confirmed behavioural / correctness issues

### 1. `RepeaterLayoutContext.GetOwner()` throws instead of returning null

**File:** `RepeaterLayoutContext.cs:75-78`
**WinUI:** `RepeaterLayoutContext.cpp:100-103`

```cpp
// WinUI
winrt::ItemsRepeater RepeaterLayoutContext::GetOwner()
{
    return m_owner.get();   // nullable
}

winrt::IInspectable RepeaterLayoutContext::LayoutStateCore()
{
    if (auto owner = GetOwner())          // explicit null tolerance
    {
        return winrt::get_self<ItemsRepeater>(owner)->LayoutState();
    }
    return winrt::IInspectable{ nullptr };
}
```

```csharp
// Uno
ItemsRepeater GetOwner()
    => m_owner.TryGetTarget(out var owner)
        ? owner
        : throw new InvalidOperationException("Owning ItemsRepeater has been collected");

protected internal override object LayoutStateCore
{
    get => GetOwner().LayoutState;   // throws if owner gone
    set => GetOwner().LayoutState = value;
}
```

**Why it matters:** WinUI's `LayoutStateCore` getter explicitly tolerates a collected owner — this code path is reachable during `ItemsRepeater` teardown where the layout still holds the (now-dead) weak reference to the repeater. The Uno port turns every `Core` callback (`LayoutStateCore`, `ItemCountCore`, `GetItemAtCore`, `RecycleElementCore`, `VisibleRectCore`, `RealizationRectCore`, `RecommendedAnchorIndexCore`, `LayoutOriginCore`) into a teardown-time crash hazard.

**Recommended fix:** Restore null-returning `GetOwner()` (returns `null` when target is dead). At minimum, port the `LayoutStateCore` getter's `if (auto owner = GetOwner())` null guard verbatim. Other call sites match WinUI behaviour once `GetOwner()` is null-tolerant (WinUI dereferences `nullptr` in many of them too — that's by design, those paths only run while the repeater is alive). Sister adapter classes (`LayoutContextAdapter`, `VirtualLayoutContextAdapter`) already use `TryGetTarget` with null-tolerant fallbacks — `RepeaterLayoutContext` is internally inconsistent.

### 2. `VirtualLayoutContextAdapter.Iterator.MoveNext` throws after end of collection

**File:** `VirtualLayoutContextAdapter.ChildrenCollection.cs:60-71`

```csharp
public bool MoveNext()
{
    if (m_currentIndex < m_childCollection.Count)
    {
        ++m_currentIndex;
        return m_currentIndex < m_childCollection.Count;
    }
    else
    {
        throw new IndexOutOfRangeException();   // contract violation
    }
}
```

**Why it matters:** `IEnumerator.MoveNext` is contractually required to keep returning `false` once exhausted — it must not throw. For a collection of size N, the (N+2)-th `MoveNext` call throws. `foreach` itself only calls `MoveNext` until it returns false, so a single-pass `foreach` is safe; but any consumer that re-checks `MoveNext` after `false`, or that wraps the enumerator in LINQ operators that probe past the end, will crash. `Current` after end also throws `IndexOutOfRangeException` rather than the `InvalidOperationException` that `IEnumerator<T>` contractually mandates.

**Recommended fix:** Drop the `else throw`; have `MoveNext` simply return `false` when `m_currentIndex >= m_childCollection.Count`. Change `Current`'s post-end throw to `InvalidOperationException`.

## Missing functionality

### `LayoutContextAdapter.GetElementIndexCore` is dead-coded under `#if false`

**File:** `LayoutContextAdapter.cs:48-65`
**WinUI:** `LayoutContextAdapter.cpp:70-85`, `LayoutContextAdapter.h:29`

In WinUI this overrides an IDL-projected `IVirtualizingLayoutContextOverrides.GetElementIndexCore` slot. The C++ base `VirtualizingLayoutContext.h` does not declare the virtual either — the override comes from the projected interface. In Uno, `VirtualizingLayoutContext` has no `GetElementIndexCore` virtual to override and no caller invokes it (verified: in Uno `GetElementIndex` is called only on `ItemsRepeater`, never on the layout context). So the `#if false` is currently benign, but the public API surface (`VirtualizingLayoutContext.GetElementIndex` / `GetElementIndexCore`) is genuinely missing.

**Recommended fix:** Either (a) add the public `GetElementIndex` method and `protected virtual GetElementIndexCore` slot to `VirtualizingLayoutContext` and enable the override on `LayoutContextAdapter`, or (b) leave the gap and remove the dead `#if false` block. Option (a) preserves WinUI API parity.

## Visibility / API surface

### `ChildrenCollection` does not expose `IndexOf` / `GetMany` not-implemented stubs

**File:** `VirtualLayoutContextAdapter.ChildrenCollection.cs`
**WinUI:** `VirtualLayoutContextAdapter.h` `ChildrenCollection<T>` declares `IndexOf`, `GetMany(uint32_t, array_view<T>)`, and `Iterator::GetMany(array_view<T>)` (the first two as `E_NOTIMPL` stubs; the iterator's `GetMany` has a real loop).

The C# `IReadOnlyList<UIElement>` interface does not require `IndexOf`/`GetMany`, so consumers see no missing method — but the projected `IVectorView<UIElement>` ABI that WinUI exposes does include them. For C#-only consumers this is invisible; for any consumer that obtains an `IVectorView`-projected wrapper, the methods are absent. Severity is low absent a known caller.

## Lifecycle / leak risk

### `ChildrenCollection.m_context` holds a strong reference to `VirtualizingLayoutContext`

**File:** `VirtualLayoutContextAdapter.ChildrenCollection.cs:16`
**WinUI:** uses `tracker_ref<winrt::VirtualizingLayoutContext>` (GC-aware via ReferenceTracker).

`VirtualLayoutContextAdapter` already holds a weak reference to the virtualizing context, but the inner `ChildrenCollection` (constructed in `ChildrenCore`) keeps a strong reference. Because `ChildrenCollection` instances are returned to layout callers and may outlive the adapter, this can pin the `VirtualizingLayoutContext` past its expected lifetime. Not a confirmed leak on Uno given typical short-lived enumerator usage, but worth tracking.

## Dropped (rejected from source report)

- File-split / `Foo.h.mux.cs` / `Foo.mux.cs` layout — port-rule cosmetic, no behavioural impact.
- Missing `// MUX Reference ..., commit 4b206bce3` header on all six files — port-rule cosmetic.
- Method order in `VirtualizingLayoutContext.cs` differing from `.cpp` — port-rule cosmetic; C# semantics identical.
- `#region` vs `#pragma region` — naming convention only.
- `RecommendedAnchorIndexCore` ported as auto-property vs WinUI method — IDL maps it as a property; the C# form is correct.
- `LayoutOriginCore` setter throws `ArgumentException` vs WinUI `hresult_invalid_argument` — expected projection.
- `NonVirtualizingLayoutContext.ChildrenCore` throws `NotSupportedException` vs `NotImplementedException` peers — cosmetic naming inconsistency, behaviour unchanged.
- `RepeaterLayoutContext.GetRuntimeClassName` override missing — affects WinRT class-name reflection only, no managed caller depends on it.
- `*Core` setter in `LayoutContextAdapter`/`VirtualLayoutContextAdapter` calls the public `LayoutState` property instead of `LayoutStateCore` directly — in C#, public setter routes through the virtual `LayoutStateCore` anyway, so dispatch is identical.
- `LayoutStateCore` getter in WinUI calls `context.LayoutState()` (public) vs setter calls `context.LayoutStateCore(state)` (protected) — preserved as symmetric public access in Uno; semantically equivalent for any C# derived class.
- `m_contextAdapter` field type on `NonVirtualizingLayoutContext` declared as concrete `LayoutContextAdapter` vs abstract `VirtualizingLayoutContext` — private field, no observable impact.
- `ChildrenCore` returning `IReadOnlyList<UIElement>` vs `IVectorView<UIElement>` — expected WinRT-to-CLR projection.
- Trace macro fidelity loss in `RepeaterLayoutContext.RecycleElementCore` — logging only.
- Inline comments dropped (`GetOrCreateElementAt` ambiguity note, `m_owner` weak-ref leak-cycle note) — documentation only.
- `Iterator.HasCurrent` not exposed — not part of `IEnumerator<T>`.
- WinUI vs Uno `DBG`/`DEBUG` macro naming — expected conversion.
