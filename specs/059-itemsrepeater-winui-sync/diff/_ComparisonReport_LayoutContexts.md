# Layout Context Types Comparison Report

**WinUI commit:** 4b206bce3
**Types covered:** LayoutContext, LayoutContextAdapter, NonvirtualizingLayoutContext, VirtualizingLayoutContext, RepeaterLayoutContext, VirtualLayoutContextAdapter

## Summary

| Type | Critical | High | Medium | Low | Total |
|------|----------|------|--------|-----|-------|
| LayoutContext | 0 | 0 | 3 | 2 | 5 |
| LayoutContextAdapter | 1 | 2 | 3 | 3 | 9 |
| NonvirtualizingLayoutContext | 0 | 1 | 2 | 1 | 4 |
| VirtualizingLayoutContext | 0 | 0 | 3 | 2 | 5 |
| RepeaterLayoutContext | 1 | 2 | 3 | 2 | 8 |
| VirtualLayoutContextAdapter (+ChildrenCollection) | 1 | 3 | 3 | 4 | 11 |
| **TOTAL** | **3** | **8** | **17** | **14** | **42** |

Severity definitions used throughout:
- **Critical**: Behavioral correctness bug (wrong null/throw path, wrong forwarded target, etc.)
- **High**: Public/protected API surface deviation, visibility widening, missing method
- **Medium**: File layout / MUX Reference header / ordering / port-rule violations
- **Low**: Comments, blank lines, expression-body, idiom drift

---

## File layout audit

Per the porting rules, each type should have a split as `Foo.cs` (XML doc only), `Foo.h.mux.cs` (fields), `Foo.mux.cs` (.cpp methods in order), plus optional `Foo.Properties.cs` and `Foo.uno.cs`. All six types are ported as **monolithic single `.cs` files**.

| WinUI files | Expected Uno files | Actual Uno files | Status |
|-------------|--------------------|------------------|--------|
| `LayoutContext.cpp` / `LayoutContext.h` | `LayoutContext.cs`, `LayoutContext.h.mux.cs`, `LayoutContext.mux.cs` | `LayoutContext.cs` (monolithic) | Medium - file split missing |
| `LayoutContextAdapter.cpp` / `LayoutContextAdapter.h` | `LayoutContextAdapter.cs`, `LayoutContextAdapter.h.mux.cs`, `LayoutContextAdapter.mux.cs` | `LayoutContextAdapter.cs` (monolithic) | Medium - file split missing |
| `NonvirtualizingLayoutContext.cpp` / `.h` | `NonVirtualizingLayoutContext.cs`, `.h.mux.cs`, `.mux.cs` | `NonvirtualizingLayoutContext.cs` (monolithic) | Medium - file split missing |
| `VirtualizingLayoutContext.cpp` / `.h` | `VirtualizingLayoutContext.cs`, `.h.mux.cs`, `.mux.cs` | `VirtualizingLayoutContext.cs` (monolithic) | Medium - file split missing |
| `RepeaterLayoutContext.cpp` / `.h` | `RepeaterLayoutContext.cs`, `.h.mux.cs`, `.mux.cs` | `RepeaterLayoutContext.cs` (monolithic) | Medium - file split missing |
| `VirtualLayoutContextAdapter.cpp` / `.h` | `VirtualLayoutContextAdapter.cs`, `.h.mux.cs`, `.mux.cs` | `VirtualLayoutContextAdapter.cs` + `VirtualLayoutContextAdapter.ChildrenCollection.cs` | Medium - file split missing; the second file is an Uno-specific extraction of inner classes |

**MUX Reference header**: NONE of the six Uno files carry the required `// MUX Reference <file>, commit 4b206bce3` line. Every file only has the standard MIT license header. This is a per-file Medium finding (counted once globally rather than 6x to avoid double-counting).

---

## Per-type sections

### LayoutContext

#### Method order verification

WinUI `LayoutContext.cpp` order:
1. `LayoutState()` getter
2. `LayoutState(value)` setter
3. `LayoutStateCore()` getter (virtual)
4. `LayoutStateCore(value)` setter (virtual)

Uno `LayoutContext.cs` order:
1. `LayoutState` property (combined getter/setter)
2. `LayoutStateCore` property (combined getter/setter)
3. `Indent` property

The two members combined into a property are acceptable for the C# idiom but the relative order matches. OK.

#### Field/constant verification

| WinUI field | Uno member | Notes |
|-------------|-----------|-------|
| `m_indent{ 0 }` (DBG only) | `Indent { get; set; }` auto-prop | OK behaviorally |
| `Indent()` non-DBG returns `0` | `Indent => 0` | OK |

#### Findings

**[Medium] Missing MUX Reference header** — no `// MUX Reference LayoutContext.cpp, commit 4b206bce3` (or `.h, commit ...`) line.

**[Medium] File layout** — Should be `LayoutContext.cs` (decl/XML doc), `.h.mux.cs` (Indent field), `.mux.cs` (LayoutState/LayoutStateCore methods). Currently monolithic.

**[Medium] `#pragma region ILayoutContext` and `#pragma region ILayoutContextOverrides` not preserved consistently** — Uno uses `#region ILayoutContext` and `#region ILayoutContextOverrides` which is fine, but the region for `Indent` block in the .h (no region in C++) is also fine. Actually preserved — downgrade to Low.

Re-classified:
- **[Low] Region preserved as C# `#region`** — OK, included as info only.

**[Low] Visibility widening on `LayoutStateCore`** — In WinUI the .h declares `virtual winrt::IInspectable LayoutStateCore();` as **public** (the `public:` block at line 11 covers it). In the Uno port `LayoutStateCore` is `protected internal virtual`. WinUI XAML metadata (IDL) commonly maps `*Core` overrides as protected to consumers; however the .h shows `public`. This is a visibility narrowing relative to .h. Verify against IDL — IDL for ILayoutContextOverrides typically declares Core members as protected, so `protected internal` is the correct mapping. Treat as Low (informational).

**[Low] DBG vs DEBUG conditional** — WinUI uses `#ifdef DBG` while Uno uses `#if DEBUG`. The two macros are different but the C# equivalent is `DEBUG`. OK.

---

### LayoutContextAdapter

#### Method order verification

WinUI `LayoutContextAdapter.cpp` order:
1. ctor
2. `LayoutStateCore()` getter
3. `LayoutStateCore(state)` setter
4. `ItemCountCore()`
5. `GetItemAtCore(index)`
6. `GetOrCreateElementAtCore(index, options)`
7. `RecycleElementCore(element)`
8. `GetElementIndexCore(element)`
9. `VisibleRectCore()`
10. `RealizationRectCore()`
11. `RecommendedAnchorIndexCore()`
12. `LayoutOriginCore()` getter
13. `LayoutOriginCore(value)` setter

Uno `LayoutContextAdapter.cs` order:
1. ctor
2. `LayoutStateCore` property (getter+setter combined)
3. `ItemCountCore()`
4. `GetItemAtCore(index)`
5. `GetOrCreateElementAtCore(index, options)`
6. `RecycleElementCore(element)`
7. `GetElementIndexCore` **under `#if false`** — see Critical finding below
8. `VisibleRectCore()`
9. `RealizationRectCore()`
10. `RecommendedAnchorIndexCore` property
11. `LayoutOriginCore` property (getter+setter combined)

Order matches.

#### Field/constant verification

| WinUI field | Uno field | Notes |
|-------------|----------|-------|
| `winrt::weak_ref<winrt::NonVirtualizingLayoutContext> m_nonVirtualizingContext{ nullptr }` | `private readonly WeakReference<NonVirtualizingLayoutContext> m_nonVirtualizingContext` | OK |

#### Findings

**[Critical] `GetElementIndexCore` is wrapped in `#if false`** — In the WinUI source this is a real virtual override:

```cpp
int32_t LayoutContextAdapter::GetElementIndexCore(winrt::UIElement const& element)
{
    if (auto context = m_nonVirtualizingContext.get())
    {
        auto children = context.Children();
        for (unsigned int i = 0; i < children.Size(); i++)
        {
            if (children.GetAt(i) == element) { return i; }
        }
    }
    return -1;
}
```

In Uno it is commented out behind `#if false`:

```csharp
#if false
private int GetElementIndexCore(UIElement element) { ... }
#endif
```

There is no `GetElementIndexCore` method on `VirtualizingLayoutContext` in Uno (see VirtualizingLayoutContext.cs has no such method) so this entire override is effectively disabled. This means any consumer (e.g., `Layout.GetElementIndex` indirection during recycling) will return wrong indices. The base `VirtualizingLayoutContext` class itself is missing a `GetElementIndexCore`/`GetElementIndex` method in the Uno port — this is a co-located gap. Mark Critical because the dead-coded override silently drops public API parity.

**[High] `LayoutStateCore` setter forwards to wrong target** — WinUI:

```cpp
void LayoutContextAdapter::LayoutStateCore(winrt::IInspectable const& state)
{
    if (auto context = m_nonVirtualizingContext.get())
    {
        context.LayoutStateCore(state);   // <-- calls *Core* directly
    }
}
```

Uno:

```csharp
set
{
    if (m_nonVirtualizingContext.TryGetTarget(out var context))
    {
        context.LayoutState = value;      // <-- calls public LayoutState
    }
}
```

WinUI deliberately calls `LayoutStateCore` (the protected override), bypassing public dispatch. Uno calls the public `LayoutState` property which re-routes through `overridable().LayoutStateCore()` once more. For a simple `NonVirtualizingLayoutContext` derived class with no further override this is behaviorally identical, but for a custom derived `NonVirtualizingLayoutContext` that overrides only the *public* property (impossible in C# since it's not virtual) or, more importantly, for any case where the derived type overrides the *Core* method — the result is the same. Still, this is a deliberate WinUI pattern (call `*Core` directly) that should be preserved verbatim. Mark High because of risk to derived-type dispatching semantics if anyone subclasses.

**[High] `LayoutStateCore` getter forwards to wrong target** — Same pattern: WinUI `LayoutStateCore()` reads `context.LayoutState()` which is the public accessor. So actually the getter matches (Uno also reads the public `LayoutState`). The asymmetry between WinUI getter (calls `LayoutState`) and WinUI setter (calls `LayoutStateCore`) is preserved in WinUI but Uno makes both symmetric (both go through public). Document as High.

**[Medium] `#pragma region IVirtualizingLayoutContextOverrides` placement** — In WinUI the region also wraps `LayoutOriginCore` setter at the bottom. In Uno the `#endregion` is at line 84 after `LayoutOriginCore` property — equivalent. OK.

**[Medium] File layout** — Monolithic; should be split. Medium.

**[Medium] Missing MUX Reference header**.

**[Low] `RecommendedAnchorIndexCore` becomes an auto-property** — WinUI declares it as a method `int RecommendedAnchorIndexCore()` returning `-1`. Uno makes it `protected override int RecommendedAnchorIndexCore => -1;` (property with expression body). This is consistent with how `VirtualizingLayoutContext` exposes it as a property in Uno (matches the IDL `RecommendedAnchorIndex` being a property, not method). OK as expected conversion.

**[Low] Exception type for `LayoutOriginCore` setter** — WinUI throws `winrt::hresult_invalid_argument`; Uno throws `ArgumentException`. Expected conversion.

**[Low] Inline `new Rect(0, 0, double.PositiveInfinity, double.PositiveInfinity)`** — WinUI uses `std::numeric_limits<float>::infinity()`; Uno uses `double.PositiveInfinity`. `Rect` in Uno takes doubles, so this is a precision widening but the value parses to `+inf`. OK.

---

### NonvirtualizingLayoutContext

#### Method order verification

WinUI `NonvirtualizingLayoutContext.cpp` order:
1. `Children()` (public)
2. `ChildrenCore()` (virtual)
3. `GetVirtualizingContextAdapter()`

Uno `NonvirtualizingLayoutContext.cs` order:
1. `Children` property
2. `ChildrenCore` property
3. `GetVirtualizingContextAdapter()`

Order matches.

#### Field/constant verification

| WinUI field | Uno field | Notes |
|-------------|----------|-------|
| `winrt::VirtualizingLayoutContext m_contextAdapter{ nullptr }` | `private LayoutContextAdapter m_contextAdapter` | **Type mismatch — see High finding below** |

#### Findings

**[High] `m_contextAdapter` field type is concrete instead of abstract** — WinUI declares the field as `winrt::VirtualizingLayoutContext m_contextAdapter{ nullptr };` (i.e., the public abstract base type). Uno declares it as `private LayoutContextAdapter m_contextAdapter;` (the concrete internal adapter). The `GetVirtualizingContextAdapter()` return type in Uno is correctly `VirtualizingLayoutContext`, but the field type differs from WinUI. This is mostly cosmetic — it does not affect ABI — but it removes one layer of indirection and is a port deviation. Mark High because field type is an observable .h declaration that should be ported verbatim.

**[Medium] `ChildrenCore` exception type** — WinUI throws `winrt::hresult_not_implemented()`; Uno throws `NotSupportedException()`. The rest of the codebase (LayoutContext.LayoutStateCore, VirtualizingLayoutContext.*Core) uses `NotImplementedException` in Uno. This Uno method uniquely throws `NotSupportedException` — inconsistent with peer methods in the same class hierarchy. Mark Medium (different exception path).

**[Medium] Visibility widening on `ChildrenCore`** — WinUI .h declares `virtual winrt::IVectorView<winrt::UIElement> ChildrenCore();` under `public:`. Uno declares `public virtual` — visibilities match. But every other `*Core` method in the project is `protected`. The IDL for `INonVirtualizingLayoutContextOverrides.ChildrenCore` is protected. Verify against generated metadata. This is likely a **visibility widening** (should be `protected internal`) — Medium.

**[Low] Missing MUX Reference header / monolithic file** — Already covered above. Combined with `LayoutContextAdapter` lifecycle: the `m_contextAdapter` is allocated lazily — equivalent to WinUI `winrt::make<LayoutContextAdapter>(...)`. OK.

---

### VirtualizingLayoutContext

#### Method order verification

WinUI `VirtualizingLayoutContext.cpp` order:
1. `ItemCount()`
2. `GetItemAt(index)`
3. `GetOrCreateElementAt(index)`
4. `GetOrCreateElementAt(index, options)`
5. `RecycleElement(element)`
6. `VisibleRect()`
7. `RealizationRect()`
8. `RecommendedAnchorIndex()`
9. `LayoutOrigin()` getter
10. `LayoutOrigin(value)` setter
11. `GetItemAtCore(index)` (virtual)
12. `GetOrCreateElementAtCore(index, options)` (virtual)
13. `RecycleElementCore(element)` (virtual)
14. `VisibleRectCore()` (virtual)
15. `RealizationRectCore()` (virtual)
16. `RecommendedAnchorIndexCore()` (virtual)
17. `LayoutOriginCore()` getter (virtual)
18. `LayoutOriginCore(value)` setter (virtual)
19. `ItemCountCore()` (virtual)
20. `GetNonVirtualizingContextAdapter()`

Uno `VirtualizingLayoutContext.cs` order:
1. `ItemCount` property
2. `LayoutOrigin` property (getter+setter)
3. `VisibleRect` property
4. `RealizationRect` property
5. `RecommendedAnchorIndex` property
6. `GetItemAt(index)`
7. `GetOrCreateElementAt(index)` (overload 1)
8. `GetOrCreateElementAt(index, options)` (overload 2)
9. `RecycleElement(element)`
10. `LayoutOriginCore` property (virtual)
11. `RecommendedAnchorIndexCore` property (virtual)
12. `GetItemAtCore(index)` (virtual)
13. `GetOrCreateElementAtCore(index, options)` (virtual)
14. `RecycleElementCore(element)` (virtual)
15. `VisibleRectCore()` (virtual)
16. `RealizationRectCore()` (virtual)
17. `ItemCountCore()` (virtual)
18. `GetNonVirtualizingContextAdapter()`

**Order deviation**: Uno groups the public properties before public methods, and groups the Core property (`LayoutOriginCore`, `RecommendedAnchorIndexCore`) before Core methods. WinUI .cpp groups by physical declaration order. Medium - method order in `.mux.cs` should match `.cpp` exactly.

#### Field/constant verification

| WinUI field | Uno field | Notes |
|-------------|----------|-------|
| `winrt::NonVirtualizingLayoutContext m_contextAdapter{ nullptr }` | `private NonVirtualizingLayoutContext m_contextAdapter` | OK |

#### Findings

**[Medium] Method order in `.mux.cs` does not match `.cpp`** — Public properties and methods are interleaved in WinUI by declaration; Uno groups them. This is a port rule violation: ".cpp method order must be preserved". Mark Medium.

**[Medium] Missing `GetElementIndex` / `GetElementIndexCore`** — Although neither WinUI's `VirtualizingLayoutContext.cpp/h` declare `GetElementIndexCore`, the IDL for IVirtualizingLayoutContext (and the existing `LayoutContextAdapter::GetElementIndexCore` override) indicates the method is part of the family. The Uno port omits this method entirely from `VirtualizingLayoutContext`. This is consistent with WinUI .h (no such method), so technically not a port deviation — Medium only because the absence forces `LayoutContextAdapter.GetElementIndexCore` to be `#if false`'d out (see LayoutContextAdapter Critical above). Mark Medium as a coupled finding.

**[Medium] File layout / MUX Reference header** — Same as above.

**[Low] `GetOrCreateElementAt(int index)` simplified** — WinUI has a 4-line comment explaining "Calling this way because GetOrCreateElementAtCore is ambiguous. Use .as instead of try_as because try_as uses non-delegating inner and we need to call the outer for overrides." This comment is **dropped** in Uno. Mark Low (comment lost).

```cpp
winrt::UIElement VirtualizingLayoutContext::GetOrCreateElementAt(int index)
{
    // Calling this way because GetOrCreateElementAtCore is ambiguous.
    // Use .as instead of try_as because try_as uses non-delegating inner and we need to call the outer for overrides.
    return get_strong().as<winrt::IVirtualizingLayoutContextOverrides>().GetOrCreateElementAtCore(index, winrt::ElementRealizationOptions::None);
}
```

vs

```csharp
public UIElement GetOrCreateElementAt(int index)
    => GetOrCreateElementAtCore(index, ElementRealizationOptions.None);
```

**[Low] Visibility widening of `*Core` members** — Uno marks them `protected virtual`. Matches IDL. OK.

---

### RepeaterLayoutContext

#### Method order verification

WinUI `RepeaterLayoutContext.cpp` order:
1. ctor
2. `ItemCountCore()`
3. `GetOrCreateElementAtCore(index, options)`
4. `LayoutStateCore()` getter
5. `LayoutStateCore(value)` setter
6. `GetItemAtCore(index)`
7. `RecycleElementCore(element)`
8. `VisibleRectCore()`
9. `RealizationRectCore()`
10. `RecommendedAnchorIndexCore()`
11. `LayoutOriginCore()` getter
12. `LayoutOriginCore(value)` setter
13. `GetOwner()`

Uno `RepeaterLayoutContext.cs` order:
1. ctor
2. `ItemCountCore()`
3. `GetOrCreateElementAtCore(index, options)`
4. `LayoutStateCore` property (getter+setter)
5. `GetItemAtCore(index)`
6. `RecycleElementCore(element)`
7. `VisibleRectCore()`
8. `RealizationRectCore()`
9. `RecommendedAnchorIndexCore` property
10. `LayoutOriginCore` property (getter+setter)
11. `GetOwner()`

Order matches.

#### Field/constant verification

| WinUI field | Uno field | Notes |
|-------------|----------|-------|
| `winrt::weak_ref<winrt::ItemsRepeater> m_owner` | `private readonly WeakReference<ItemsRepeater> m_owner` | OK |

#### Findings

**[Critical] `GetOwner()` semantic change** — WinUI:

```cpp
winrt::ItemsRepeater RepeaterLayoutContext::GetOwner()
{
    return m_owner.get();    // returns nullptr if owner is gone
}
```

Uno:

```csharp
ItemsRepeater GetOwner()
    => m_owner.TryGetTarget(out var owner)
        ? owner
        : throw new InvalidOperationException("Owning ItemsRepeater has been collected");
```

WinUI silently returns `nullptr` if the owner has been collected. Several call sites check for null (e.g., `LayoutStateCore()` getter explicitly does `if (auto owner = GetOwner())`). Uno's version *throws*, so the `if (m_owner.TryGetTarget...)` analogue in `LayoutStateCore` getter cannot be expressed — and indeed the Uno port now unconditionally calls `GetOwner().LayoutState` in the setter and `GetItemAtCore`, `VisibleRectCore`, `RealizationRectCore`, `RecommendedAnchorIndexCore`, `LayoutOriginCore`, etc. all unconditionally throw if the owner is gone. The original WinUI code explicitly tolerates a collected owner in `LayoutStateCore()` getter. Mark Critical because this changes failure semantics and lifecycle behavior during ItemsRepeater teardown.

**[High] `LayoutStateCore` getter loses null-tolerance** — WinUI:

```cpp
winrt::IInspectable RepeaterLayoutContext::LayoutStateCore()
{
    if (auto owner = GetOwner())
    {
        return winrt::get_self<ItemsRepeater>(owner)->LayoutState();
    }
    return winrt::IInspectable{ nullptr };
}
```

Uno:

```csharp
get => GetOwner().LayoutState;
```

If the owner is gone, WinUI returns null; Uno throws. This is the visible consequence of the previous finding. Mark High (separate from Critical because this specific code path is the most likely one to be hit during teardown).

**[High] `LayoutStateCore` setter loses safety** — Symmetric: WinUI setter does *not* null-check (matches Uno), but the getter does. The WinUI source intentionally asymmetric (only the getter is defensive). Uno makes both throw. Mark High.

**[Medium] Trace macro substitution** — WinUI uses `ITEMSREPEATER_TRACE_INFO_DBG(nullptr, TRACE_MSG_METH_STR_INT, METH_NAME, this, L"RecycleElement:", owner->GetElementIndex(element));`. Uno uses `REPEATER_TRACE_INFO("RepeaterLayout - RecycleElement: %d \n", owner.GetElementIndex(element));`. This appears to be an acceptable substitution to a local helper macro, but the original `METH_NAME`/`this` context bits are lost. Mark Medium (logging fidelity).

**[Medium] File layout / MUX Reference header** — Same as above.

**[Medium] Missing `GetRuntimeClassName` override** — WinUI .h defines:

```cpp
hstring GetRuntimeClassName() const
{
    return VirtualizingLayoutContext::GetRuntimeClassName();
}
```

with comment: *"Explicitly implement GetRuntimeClassName because winrt::implements chooses the first interface as our name and we want the concrete VirtualizingLayoutContext as our name."*

Uno does **not** port this method or its comment. This affects runtime class name reporting (visible from `object.GetType()` and from any IDL consumers). Mark Medium because the comment carries valuable context and the method exists for a known reason.

**[Low] `GetItemAtCore` two-line signature collapsed** — WinUI declares it on two lines (`int index)\n{`). Uno collapses. Cosmetic. Low.

**[Low] `GetOwner()` comment dropped** — WinUI .h has:

```cpp
// We hold a weak reference to prevent a leaking reference
// cycle between the ItemsRepeater and its layout.
winrt::weak_ref<winrt::ItemsRepeater> m_owner;
```

Uno drops the comment. Low.

---

### VirtualLayoutContextAdapter (+ ChildrenCollection)

#### Method order verification

WinUI `VirtualLayoutContextAdapter.cpp` order:
1. ctor
2. `LayoutStateCore()` getter
3. `LayoutStateCore(state)` setter
4. `ChildrenCore()`

Uno `VirtualLayoutContextAdapter.cs` order:
1. ctor
2. `LayoutStateCore` property (getter+setter)
3. `ChildrenCore` property

Order matches.

#### Inner class verification (ChildrenCollection / Iterator)

WinUI .h declares `ChildrenCollection<T>` as a templated `ReferenceTracker<...>` derived class with these members:

| WinUI member | Uno member | Notes |
|-------------|-----------|-------|
| `Size()` | `Count` | Expected conversion (`IVectorView<T>` → `IReadOnlyList<UIElement>`) |
| `GetAt(index)` | `this[int index]` indexer | Expected |
| `IndexOf(value, index)` (throws E_NOTIMPL) | **MISSING** | High - missing method |
| `GetMany(start, values)` (throws E_NOTIMPL) | **MISSING** | High - missing method |
| `First()` → `Iterator` | `GetEnumerator()` | OK |
| `Iterator::~Iterator()` empty destructor | `Dispose() {}` | OK |
| `Iterator::Current()` | `Iterator.Current` | OK (semantic change below) |
| `Iterator::HasCurrent()` | **MISSING** | Medium |
| `Iterator::MoveNext()` | `MoveNext()` | OK (semantic change below) |
| `Iterator::GetMany(values)` | **MISSING** | Medium |

#### Field/constant verification

| WinUI field | Uno field | Notes |
|-------------|----------|-------|
| `m_virtualizingContext{ nullptr }` | `m_virtualizingContext` | OK |
| `m_children{ nullptr }` (IVectorView) | `m_children` (IReadOnlyList<UIElement>) | OK |
| `Iterator::m_childCollection{ nullptr }` (IVectorView) | `m_childCollection` (IReadOnlyList<UIElement>) | OK |
| `Iterator::m_currentIndex = 0` | `m_currentIndex = -1; // UNO:: This is 0 on WinUI` | **Critical - intentional semantic change** |

#### Findings

**[Critical] `Iterator.m_currentIndex` starts at -1 in Uno, 0 in WinUI** — This is acknowledged with an inline `// UNO:: This is 0 on WinUI` comment but the change is **incorrect for IEnumerator semantics** in C# vs. WinRT IIterator semantics. WinRT IIterator's `Current` is valid *before* the first `MoveNext`; .NET IEnumerator's `Current` is only valid *after* the first `MoveNext`. Uno's `-1` starting index is the right C# convention, but the consequence is that `MoveNext` increments first then checks bounds, while WinUI's `MoveNext` checks first then increments. The Uno `MoveNext` is:

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
        throw new IndexOutOfRangeException();
    }
}
```

For an empty collection (Count==0, m_currentIndex==-1): `-1 < 0` so we enter, increment to 0, return `0 < 0` = false. OK for empty case.

For Count==2: starts at -1.
- Call 1: `-1 < 2` enter, increment to 0, return `0 < 2` = true. Current = childCollection[0]. OK.
- Call 2: `0 < 2` enter, increment to 1, return `1 < 2` = true. Current = childCollection[1]. OK.
- Call 3: `1 < 2` enter, increment to 2, return `2 < 2` = false.
- Call 4: `2 < 2` false, throw IndexOutOfRangeException.

This is *almost* correct but Call 4 throws instead of returning false. Per .NET `IEnumerator.MoveNext` contract, repeated calls after end should keep returning false, not throw. Mark Critical because the C# enumerator contract is violated; foreach loops that re-iterate (rare) or any caller checking MoveNext post-end will crash.

WinUI side starting at 0 also has issues by .NET conventions but those don't matter since C++ doesn't use `foreach`.

**[High] `IndexOf` not implemented (silently dropped)** — WinUI:

```cpp
bool IndexOf(T const& value, uint32_t &index) noexcept
{
    winrt::throw_hresult(E_NOTIMPL);
}
```

Uno's `IReadOnlyList<T>` interface doesn't require `IndexOf` (LINQ provides `IndexOf` extension), but the WinUI ChildrenCollection has a documented stub. The fact that `IReadOnlyList` doesn't need it explains the omission, but the contract intent (explicit not-implemented) is lost. Mark High.

**[High] `GetMany(startIndex, array_view<T>)` not implemented** — Same as IndexOf: explicit E_NOTIMPL stub in WinUI, dropped in Uno. High.

**[High] `Iterator::GetMany(array_view<T>)` not implemented** — Working implementation in WinUI, missing in Uno. High.

**[Medium] `Iterator::HasCurrent()` not exposed** — `IEnumerator<T>` doesn't have HasCurrent, but the semantic isn't exposed anywhere else. Medium.

**[Medium] `Iterator::Current` throws different exception** — WinUI throws `winrt::hresult_out_of_bounds()`; Uno throws `IndexOutOfRangeException`. WinUI's hresult maps to `System.Exception` with HRESULT 0x8000000B, not directly to IndexOutOfRangeException. Per .NET conventions, `Current` after the end should throw `InvalidOperationException`, not `IndexOutOfRangeException`. Mark Medium (wrong .NET exception type).

**[Medium] `Iterator::MoveNext` throws different exception** — Same as above: WinUI `hresult_out_of_bounds`; Uno `IndexOutOfRangeException`. Per .NET conventions, `MoveNext` should never throw; it should return `false`. Mark Medium.

**[Medium] `LayoutStateCore` setter forwards `LayoutStateCore` vs public `LayoutState`** — Same as LayoutContextAdapter:

```cpp
context.LayoutStateCore(state);   // WinUI calls protected Core directly
```

Uno:

```csharp
context.LayoutState = value;       // Uno calls public property
```

Medium (same pattern as LayoutContextAdapter).

**[Medium] File layout** — Monolithic + Uno-specific second file for inner classes. The split into `VirtualLayoutContextAdapter.ChildrenCollection.cs` is an Uno-specific organization. Acceptable but not what the porting rules require. Medium.

**[Low] Missing MUX Reference header**.

**[Low] `ChildrenCollection.m_context` ownership** — WinUI uses `tracker_ref<winrt::VirtualizingLayoutContext>` via `ReferenceTracker`. Uno uses raw `private VirtualizingLayoutContext m_context;`. Strong reference; acceptable in C# but creates a strong cycle from ChildrenCollection → VirtualizingLayoutContext, while WinUI uses reference tracker for GC cycle breaking. Low because it's a tracker_ref → T expected conversion per the rules, but worth noting that strong cycles in collection ownership *can* cause leaks.

**[Low] `IReadOnlyList<UIElement>` cast vs `IVectorView<UIElement>` return** — `ChildrenCore` returns `IReadOnlyList<UIElement>` in Uno; `IVectorView<UIElement>` in WinUI. Inside Uno the field is `private IReadOnlyList<UIElement> m_children` — matches the cast. But the public `Children` property in `NonVirtualizingLayoutContext` is `IReadOnlyList<UIElement>`, while WinUI exposes `IVectorView<UIElement>`. The IDL maps `IVectorView<T>` to `IReadOnlyList<T>` in C#. OK as expected conversion.

**[Low] `GetTarget` extension call instead of `TryGetTarget`** — Uno uses `m_virtualizingContext.GetTarget()` (an `Uno.Extensions`-supplied helper) in `ChildrenCore`. WinUI calls `m_virtualizingContext.get()`. The `GetTarget` extension presumably returns the target or null. OK.

**[Low] Inner class moved out of WinUI's templated form** — `ChildrenCollection<T>` is templated in C++; Uno makes it a non-generic `ChildrenCollection : IReadOnlyList<UIElement>`. Acceptable because `T` is always `UIElement` per `ChildrenCore`. Low.

---

## Cross-type observations

1. **MUX Reference headers missing across all six files.** None of the six files carry `// MUX Reference <file.cpp/.h>, commit 4b206bce3`. Per port rules, this header is mandatory. This is a uniform Medium finding.

2. **Monolithic file layout across all six types.** None of the types use the prescribed `Foo.cs` / `Foo.h.mux.cs` / `Foo.mux.cs` split. Each has fields and method bodies in one file. Per porting rules this is a Medium per type.

3. **`*Core` setter calls public property instead of `*Core` directly.** This pattern repeats in `LayoutContextAdapter` and `VirtualLayoutContextAdapter`. WinUI consistently calls `context.LayoutStateCore(state)` (the protected override) directly when wiring through to the wrapped target; Uno consistently calls `context.LayoutState = value` (public property). For `sealed`/leaf use cases this is semantically equivalent, but if a consumer subclasses `NonVirtualizingLayoutContext`/`VirtualizingLayoutContext` and overrides `LayoutStateCore`, the dispatch behavior could differ (in Uno, the override still gets invoked because the public property routes through it; in WinUI, the override is invoked directly). Behaviorally equivalent in C# because property setters always dispatch through virtual `LayoutStateCore`, but a port-rule violation nonetheless.

4. **Owner-collected lifecycle inconsistency.** `RepeaterLayoutContext.GetOwner()` *throws* in Uno but returns nullable in WinUI. Conversely, `LayoutContextAdapter` and `VirtualLayoutContextAdapter` correctly use `TryGetTarget` with null-tolerant handling. This is internally inconsistent inside the Uno port.

5. **Comments and explanatory blocks dropped uniformly.** Notable dropped comments: the GetOrCreateElementAt explanation, the GetRuntimeClassName "first interface" comment, and the m_owner weak-ref leak-cycle comment. These convey design rationale and should be preserved.

6. **`GetElementIndexCore` is an orphan.** It exists in WinUI's `LayoutContextAdapter` (and is part of the implicit IVirtualizingLayoutContextOverrides surface) but is `#if false`'d out in Uno and missing entirely from `VirtualizingLayoutContext`. This is the only deliberately disabled method across the six files and represents a feature gap.

7. **Exception types and dispatch differences.** Across the files we see:
   - WinUI `hresult_not_implemented` → Uno `NotImplementedException` (correct), except `NonVirtualizingLayoutContext.ChildrenCore` uses `NotSupportedException` (inconsistent).
   - WinUI `hresult_invalid_argument` → Uno `ArgumentException` (correct).
   - WinUI `hresult_out_of_bounds` → Uno `IndexOutOfRangeException` (questionable; should be `InvalidOperationException` per IEnumerator contract).

---

## Conclusion

- **Total findings**: 42 (3 Critical, 8 High, 17 Medium, 14 Low)

### Top priority issues

1. **[Critical] `LayoutContextAdapter.GetElementIndexCore` is `#if false`'d** — Restore the method and add the corresponding `GetElementIndexCore` override slot on `VirtualizingLayoutContext` if needed by IDL.
2. **[Critical] `RepeaterLayoutContext.GetOwner()` throws instead of returning null** — Change to return null and update `LayoutStateCore` getter to match WinUI's null-tolerant pattern, otherwise teardown of `ItemsRepeater` can crash mid-layout when a layout still holds the weak reference.
3. **[Critical] `VirtualLayoutContextAdapter.ChildrenCollection.Iterator.MoveNext` throws after end** — Per `IEnumerator` contract this must return `false` repeatedly, not throw. The same applies to `Current` (should throw `InvalidOperationException` per .NET, not `IndexOutOfRangeException`).
4. **[High] `*Core` setter chain in adapters routes through public `LayoutState` not the protected `LayoutStateCore`** — Restore direct `*Core` call to preserve WinUI dispatch semantics.
5. **[High] Three missing methods in `ChildrenCollection`/`Iterator`** (`IndexOf`, two `GetMany`).
6. **[Medium x 7] File-layout / MUX Reference header gaps** across all six files. Should be addressed in a single batch refactor that introduces the proper split and adds the commit-pinned MUX Reference comment.
7. **[Medium] `NonVirtualizingLayoutContext.ChildrenCore` exception type inconsistency** — Switch to `NotImplementedException` for parity with peer methods.
8. **[Medium] `RepeaterLayoutContext.GetRuntimeClassName` missing** — Port the override and its explanatory comment.
9. **[Medium] Method ordering in `VirtualizingLayoutContext.cs`** — Reorder to match `.cpp` declaration sequence (interleaved public/Core).
