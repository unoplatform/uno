# Misc Utilities Comparison Report

**WinUI commit:** 4b206bce3
**Types covered:** BuildTreeScheduler, ChildrenInTabFocusOrderIterable, CustomProperty, Phaser, UniqueIdElementPool, RepeaterTestHooks, LayoutsTestHooks, _Tracing, CachedVisualTreeHelpers, IKeyIndexMapping, IPanel, IRepeaterScrollingSurface, ScrollOrientation

## Summary

| Type | High | Medium | Low | Info |
|------|------|--------|-----|------|
| BuildTreeScheduler | 0 | 3 | 4 | 1 |
| ChildrenInTabFocusOrderIterable | 1 | 2 | 4 | 0 |
| CustomProperty | 0 | 0 | 2 | 0 |
| Phaser | 0 | 1 | 5 | 0 |
| UniqueIdElementPool | 0 | 2 | 3 | 1 |
| RepeaterTestHooks | 2 | 3 | 2 | 0 |
| LayoutsTestHooks | 4 | 4 | 3 | 0 |
| _Tracing (Uno-only) | 0 | 0 | 0 | 1 |
| CachedVisualTreeHelpers (Uno-only) | 0 | 1 | 1 | 1 |
| IKeyIndexMapping | 0 | 0 | 1 | 0 |
| IPanel (Uno-only) | 0 | 0 | 1 | 1 |
| IRepeaterScrollingSurface | 0 | 1 | 1 | 0 |
| ScrollOrientation | 0 | 0 | 1 | 0 |
| **TOTALS** | **7** | **17** | **28** | **5** |

---

## BuildTreeScheduler

**File mapping**

| WinUI | Uno |
|-------|-----|
| `BuildTreeScheduler.h` | `BuildTreeScheduler.cs` (decl) + `BuildTreeScheduler.h.mux.cs` (fields/`WorkInfo` struct) |
| `BuildTreeScheduler.cpp` | `BuildTreeScheduler.mux.cs` |

The split into `.cs`/`.h.mux.cs`/`.mux.cs` matches the project convention.

**Method order verification (.cpp vs .mux.cs)**

| .cpp order | .mux.cs order | Match |
|------------|---------------|-------|
| `RegisterWork` | `RegisterWork` | yes |
| `ShouldYield` | `ShouldYield` | yes |
| `OnRendering` | `OnRendering` | yes |
| `QueueTick` | `QueueTick` | yes |

**Field/constant verification**

| WinUI (`.h`) | Uno (`.h.mux.cs`) | Notes |
|--------------|---------------------|-------|
| `static double m_budgetInMs = 40.0;` | `private const double m_budgetInMs = 40.0;` | OK |
| `static thread_local QPCTimer m_timer{};` | `[ThreadStatic] private static Stopwatch m_timer;` | Behavioural substitution; see findings |
| `static thread_local std::vector<WorkInfo> m_pendingWork{};` | `[ThreadStatic] private static List<WorkInfo> m_pendingWork;` | OK |
| `static thread_local winrt::event_token m_renderingToken{};` | `[ThreadStatic] private static bool m_renderingToken;` | type-change; see findings |
| `struct WorkInfo { ... int Priority() const; void InvokeWorkFunc() const; ... }` | `internal struct WorkInfo { int Priority; void InvokeWorkFunc(); }` | OK |

### Findings

**[Medium] M1. `OnRendering` calls `m_timer.Restart()` instead of `Reset()`**

WinUI:
```cpp
// Reset the timer so it snaps the time just before rendering
m_timer.Reset();
```
Uno (`BuildTreeScheduler.mux.cs:67-69`):
```csharp
// Reset the timer so it snaps the time just before rendering.
// Stopwatch.Reset() also stops the timer, so Restart() is used to keep it measuring from now.
m_timer.Restart();
```

Issue: `QPCTimer::Reset()` in WinUI is a "snapshot the current time" semantic (does not stop the timer); `Stopwatch.Reset()` in .NET stops and zeroes the stopwatch. The Uno change to `Restart()` is semantically the correct mapping. This is a justified substitution but is *not* labelled with `// TODO Uno:` or marked as a deliberate deviation. Suggested fix: leave the comment as-is (it documents the substitution) or wrap in `#if HAS_UNO` to make the deviation explicit per porting rule #6.

**[Medium] M2. `RegisterWork` performs lazy-init not present in WinUI**

WinUI:
```cpp
void BuildTreeScheduler::RegisterWork(int priority, const std::function<void()>& workFunc)
{
    MUX_ASSERT(priority >= 0);
    MUX_ASSERT(workFunc != nullptr);
    QueueTick();
    m_pendingWork.push_back(WorkInfo(priority, workFunc));
}
```
Uno (`BuildTreeScheduler.mux.cs:15-31`):
```csharp
public static void RegisterWork(int priority, Action workFunc)
{
    MUX_ASSERT(priority >= 0);
    MUX_ASSERT(workFunc != null);

    QueueTick();

    // [ThreadStatic] fields cannot have inline initializers, so lazy-init on first use per thread.
    if (m_pendingWork == null)
    {
        m_pendingWork = new List<WorkInfo>();
        m_timer = new Stopwatch();
        m_timer.Start();
    }

    m_pendingWork.Add(new WorkInfo(priority, workFunc));
}
```

Issue: The 9-line block of Uno-specific lazy-init is not wrapped in `#if HAS_UNO` and lacks a `TODO Uno:` marker. This is a legitimate behavioural difference (`[ThreadStatic]` cannot have initializers as inline `thread_local` can in C++), but porting rule #6 requires it to be flagged. In addition, `ShouldYield()` is called before this lazy-init has run (e.g., via `Phaser.DoPhasedWorkCallback`), which dereferences a possibly-null `m_timer`. The lazy-init is only triggered through `RegisterWork`; if `ShouldYield()` is ever called on a fresh thread without a preceding `RegisterWork`, it would NRE.

Suggested fix: move the lazy-init into `ShouldYield` as well, or fold a thread-local static constructor pattern, and add a brief `// TODO Uno:` comment.

**[Medium] M3. `OnRendering` cleanup uses `-=` event detach, comments out TraceLogging**

WinUI:
```cpp
TraceLoggingProviderWrite(
    XamlTelemetryLogging, "BuildTreeScheduler_OutOfWork",
    TraceLoggingLevel(WINEVENT_LEVEL_VERBOSE));
// ...
winrt::Microsoft::UI::Xaml::Media::CompositionTarget::CompositionTarget::Rendering(m_renderingToken);
m_renderingToken.value = 0;
```
Uno (`BuildTreeScheduler.mux.cs:56-63`):
```csharp
// TraceLoggingProviderWrite(
//     XamlTelemetryLogging, "BuildTreeScheduler_OutOfWork",
//     TraceLoggingLevel(WINEVENT_LEVEL_VERBOSE));

// No more pending work, unhook from rendering event since being hooked up will case wux to try to
// call the event at 60 frames per second
Microsoft.UI.Xaml.Media.CompositionTarget.Rendering -= OnRendering;
m_renderingToken = false;
```

Issue: TraceLoggingProviderWrite is commented out without a `// TODO Uno:` marker (porting rule #6). The detach via `-=` is correct semantics but is not labelled. Suggested fix: add `// TODO Uno: TraceLoggingProviderWrite not yet ported.` and inline a brief note about the operator difference.

**[Low] L1. `OnRendering` sort comparator inverts WinUI's order (ascending vs descending)**

WinUI:
```cpp
// Sort in descending order of priority and work from the end of the list to avoid moving around during erase.
std::sort(m_pendingWork.begin(), m_pendingWork.end(),
    [](const auto& lhs, const auto& rhs) { return lhs.Priority() > rhs.Priority(); });
```
Uno (`BuildTreeScheduler.mux.cs:44`):
```csharp
// Sort in descending order of priority and work from the end of the list to avoid moving around during erase.
m_pendingWork.Sort((lhs, rhs) => lhs.Priority - rhs.Priority);
```

Issue: WinUI sorts **descending** (`>` comparator). Uno uses subtraction (`lhs - rhs`) which produces **ascending** order. The comment still says "descending". The code then walks from `Count - 1` down to `0`, so the *consumption* order in Uno picks highest-priority first; this happens to match WinUI's overall consumption sequence by coincidence, because WinUI sorts descending and walks from the back (lowest-priority first). So WinUI consumes lowest priority first and Uno consumes highest priority first. This is a **behavioural regression**.

Suggested fix: change comparator to `rhs.Priority - lhs.Priority` so the list ends up in descending order and the last element (highest index) is the lowest priority, matching WinUI's "drain low-priority first when reaching budget" semantics.

**[Low] L2. `m_timer` member type comment**

`BuildTreeScheduler.h.mux.cs:30-31` — there is no XML doc on `m_timer` describing why `Stopwatch` is used in place of `QPCTimer`. Low priority because it's a private static.

**[Low] L3. WinUI uses `QPCTimer` (custom QPC wrapper), Uno uses `Stopwatch`**

`Stopwatch` on .NET internally uses QPC on Windows; functional parity is fine, but porting rule #1 (lossless) means the deviation should at least be noted in the comment or in a `// TODO Uno:` block. Low priority because behaviour is equivalent.

**[Low] L4. `QueueTick` body comment**

WinUI:
```cpp
void  BuildTreeScheduler::QueueTick()
{
    if (m_renderingToken.value == 0)
    {
        m_renderingToken = winrt::Microsoft::UI::Xaml::Media::CompositionTarget::Rendering(OnRendering);
    }
}
```
Uno (`BuildTreeScheduler.mux.cs:72-79`):
```csharp
private static void QueueTick()
{
    if (!m_renderingToken)
    {
        Microsoft.UI.Xaml.Media.CompositionTarget.Rendering += OnRendering;
        m_renderingToken = true;
    }
}
```

Issue: substitution from `winrt::event_token` to `bool` is correct; comment on `.h.mux.cs:38` documents this. Note that `WorkInfo` struct's `InvokeWorkFunc` is a method while WinUI uses property `Priority()` and method `InvokeWorkFunc()` — substitution is consistent with project rules.

**[Info] I1. `WorkInfo` struct is defined in `.h.mux.cs` rather than as a nested type**

WinUI declares `struct WorkInfo` at file scope in `BuildTreeScheduler.h`. Uno keeps it as a top-level `internal struct` in `BuildTreeScheduler.h.mux.cs`. This is a reasonable layout, though arguably making it a nested type would more directly mirror the upstream layering.

---

## ChildrenInTabFocusOrderIterable

**File mapping**

| WinUI | Uno |
|-------|-----|
| `ChildrenInTabFocusOrderIterable.h` | `ChildrenInTabFocusOrderIterable.cs` (decl) + `ChildrenInTabFocusOrderIterable.h.mux.cs` (fields + inline `MoveNext`) |
| `ChildrenInTabFocusOrderIterable.cpp` | `ChildrenInTabFocusOrderIterable.mux.cs` |

**Method order verification (.cpp + inline `.h` methods vs Uno .mux.cs + .h.mux.cs)**

| WinUI order | Uno order | Match |
|------|------|------|
| Outer ctor | Outer ctor | yes |
| `First()` -> `GetEnumerator()` | `GetEnumerator()` | yes |
| Inner iterator ctor | Inner iterator ctor | yes |
| Inner `Current()` | `Current` getter (correct C# substitution) | yes |
| Inner inline `HasCurrent()` | not present | **missing** |
| Inner inline `MoveNext()` | `MoveNext()` | yes |
| Inner inline `GetMany()` | not present | **missing** |

### Findings

**[High] H1. `HasCurrent()` and `GetMany()` are completely missing**

WinUI (`ChildrenInTabFocusOrderIterable.h:28-62`) defines both methods inline. C# `IEnumerator<T>` does not include them, but the WinRT iterator contract did. If any caller (test harness, x:Bind, FocusManager) goes through `IIterator<>` semantics this will diverge. At a minimum these missing methods should be acknowledged as a deliberate adaptation to `IEnumerator<T>`.

Suggested fix: either add `internal bool HasCurrent => m_index < m_realizedChildren.Count;` and `GetMany(...)` as compatibility helpers, or add a `// TODO Uno:` comment explaining the `IIterator` -> `IEnumerator` substitution.

**[Medium] M1. `MoveNext` semantics differ — Uno starts at `-1` and increments then checks; WinUI starts at `0`**

WinUI (`.h:32-45`):
```cpp
bool MoveNext()
{
    if (m_index < static_cast<int>(m_realizedChildren.size()))
    {
        ++m_index;
        return (m_index < static_cast<int>(m_realizedChildren.size()));
    }
    else
    {
        throw winrt::hresult_out_of_bounds();
    }
    return false;
}
```
Uno (`.h.mux.cs:19-30`):
```csharp
public bool MoveNext()
{
    if (m_index < m_realizedChildren.Count)
    {
        ++m_index;
        return (m_index < m_realizedChildren.Count);
    }
    else
    {
        throw new IndexOutOfRangeException();
    }
}
```

Issue: The bodies are textually identical, but with `m_index = -1` (Uno) vs `m_index = 0` (WinUI), an `IEnumerator<T>` user expects: position before first → `MoveNext()` advances to index 0 → `Current` returns element 0. This part works. But on Uno, `Current` at `m_index = -1` (i.e., before any `MoveNext`) returns `m_realizedChildren[-1]` which is an out-of-range access — but actually Uno's `Current` getter has the same `if (m_index < m_realizedChildren.Count)` guard, which is satisfied by `-1`, and then `m_realizedChildren[-1]` will throw `ArgumentOutOfRangeException`. So a defensive caller may see different exception types. The guard should be `m_index >= 0 && m_index < count`.

Suggested fix: change `Current` getter to `if (m_index >= 0 && m_index < m_realizedChildren.Count)`.

**[Medium] M2. `m_repeater` is `readonly` field instead of `tracker_ref`**

WinUI: `tracker_ref<winrt::ItemsRepeater> m_repeater{ this };` — participates in tracker. Uno: `private readonly ItemsRepeater m_repeater;`. This is fine per the conversion rules (rule: `tracker_ref<T> → T`), but loses the explicit `this`-owner reference. Informational only at this level; flagged Medium because the implicit conversion drops the reference-tracking lifecycle hook.

**[Low] L1. `MUX Reference` header in `.mux.cs` correctly points to `.cpp`**

OK — `// MUX Reference ChildrenInTabFocusOrderIterable.cpp, commit 4b206bce3` is correct.

**[Low] L2. `m_realizedChildren` element type**

WinUI: `std::vector<std::pair<int /* index */, winrt::UIElement>>` → Uno: `List<KeyValuePair<int, UIElement>>`. Correct mapping. The comment `/* index */` is preserved.

**[Low] L3. C++ has an unreachable `return nullptr` at end of `Current()`**

That's dead code in C++; Uno omits it correctly.

**[Low] L4. `#pragma region` removed but replaced with comment**

`.h.mux.cs:17` keeps `// #pragma region IIterable implementation` as a comment, satisfying rule (regions noted). The `.mux.cs` keeps no such comment in the iterator inner ctor / `Current` section even though WinUI's `.h` wraps them in a region.

---

## CustomProperty

**File mapping**

| WinUI | Uno |
|-------|-----|
| `CustomProperty.h` | `CustomProperty.cs` (decl) + `CustomProperty.h.mux.cs` (fields) |
| `CustomProperty.cpp` | `CustomProperty.mux.cs` |

**Method order verification**

| WinUI | Uno | Match |
|------|------|------|
| ctor | ctor | yes |
| `CanRead()` | `CanRead` prop | yes |
| `CanWrite()` | `CanWrite` prop | yes |
| `Name()` | `Name` prop | yes |
| `Type()` | `Type` prop | yes |
| `GetValue` | `GetValue` | yes |
| `SetValue` | `SetValue` | yes |
| `GetIndexedValue` | `GetIndexedValue` | yes |
| `SetIndexedValue` | `SetIndexedValue` | yes |

Note: WinUI `.h` ordering is `CanRead, CanWrite, Name, Type, GetIndexedValue, SetIndexedValue, GetValue, SetValue` but the `.cpp` ordering (which is the one we follow per rule #3) is `CanRead, CanWrite, Name, Type, GetValue, SetValue, GetIndexedValue, SetIndexedValue` — Uno matches the `.cpp`. Good.

### Findings

**[Low] L1. `CustomProperty.cs` decl uses `ICustomProperty` without explicit interface listing in `.h.mux.cs`**

`.cs:9` declares `internal partial class CustomProperty : ICustomProperty`. WinUI does this via `winrt::implements<CustomProperty, winrt::ICustomProperty>`. Correct mapping.

**[Low] L2. Default values for backing fields not initialized**

WinUI: `winrt::hstring m_name{};` and `winrt::TypeName m_typeName{};` are default-initialized in the header. Uno fields are not initialized (`private string m_name;` etc.). Functionally identical since C# default-initializes, but slightly less explicit.

---

## Phaser

**File mapping**

| WinUI | Uno |
|-------|-----|
| `Phaser.h` | `Phaser.cs` (decl) + `Phaser.h.mux.cs` (fields + `ElementInfo` struct) |
| `Phaser.cpp` | `Phaser.mux.cs` |

**Method order verification**

| WinUI .cpp | Uno .mux.cs | Match |
|------|------|------|
| `ElementInfo` ctor | `ElementInfo` ctor (in `.h.mux.cs`) | partial order — see findings |
| `Phaser` ctor | `Phaser` ctor | yes |
| `PhaseElement` | `PhaseElement` | yes |
| `StopPhasing` | `StopPhasing` | yes |
| `DoPhasedWorkCallback` | `DoPhasedWorkCallback` | yes |
| `RegisterForCallback` | `RegisterForCallback` | yes |
| `MarkCallbackRecieved` | `MarkCallbackRecieved` (typo preserved) | yes |
| `ValidatePhaseOrdering` (static) | `ValidatePhaseOrdering` (static) | yes |
| `SortElements` | `SortElements` | yes |

### Findings

**[Medium] M1. `ElementInfo` ctor drops `ITEMSREPEATER_TRACE_VERBOSE_DBG` trace**

WinUI (`Phaser.cpp:13-18`):
```cpp
ElementInfo::ElementInfo(...)
{
    ITEMSREPEATER_TRACE_VERBOSE_DBG(nullptr, TRACE_MSG_METH_INT, METH_NAME, this, virtInfo->Index());
}
```
Uno (`Phaser.h.mux.cs:11-16`): no trace call.

Issue: Missing trace call; should be replaced by `REPEATER_TRACE_INFO` (Uno's tracing helper) per `_Tracing.cs` pattern, or marked with `// TODO Uno:`.

**[Low] L1. `ElementInfo` ctor moved from `.cpp` to `.h.mux.cs`**

In WinUI, `ElementInfo` body is in `Phaser.cpp` (only declared in `.h`). Uno places the constructor in `Phaser.h.mux.cs`. Inconsistent with rule #2 ("Foo.h.mux.cs (decl only)") — the body should live in `Phaser.mux.cs`. Low because the type is a trivial struct.

**[Low] L2. `SortElements` comparator returns subtraction rather than `<` boolean**

WinUI (`Phaser.cpp:221`):
```cpp
return lhs.VirtInfo()->Phase() < rhs.VirtInfo()->Phase();
```
Uno (`Phaser.mux.cs:215`):
```csharp
return lhs.VirtInfo.Phase - rhs.VirtInfo.Phase;
```

Issue: For C# `List<T>.Sort(Comparison<T>)`, returning `lhs - rhs` (negative = lhs comes first) is equivalent to ascending order — same effect as `<`. However `lhsIntersects` branch returns `-1` (lhs first) and `!lhsIntersects` returns `1` (rhs first). WinUI returns `false` (rhs comes first) and `true` (lhs comes first) under `std::sort`'s "strict less" predicate. There's a **logical inversion**:

- WinUI `lhsIntersects`-only: returns `false` → lhs is NOT less → rhs comes first.
- Uno `lhsIntersects`-only: returns `-1` → lhs is less → lhs comes first.

So visible items end up at *front* of vector in Uno, but at *back* in WinUI. The comment says "Sort in descending order (inVisibleWindow, phase)", and the consumption (`DoPhasedWorkCallback` walks from `Count - 1` downward) means in WinUI visible items get processed first (since they end up at the back). In Uno, visible items end up at the front, so non-visible items get processed first. This is a **behavioural regression** in phasing prioritization.

Suggested fix: swap the `-1` and `1` returns:
```csharp
else if (lhsIntersects)
{
    return 1; // lhs (visible) sorts AFTER non-visible -> processed first from back
}
else
{
    return -1;
}
```
Also flip the phase comparison to `rhs.Phase - lhs.Phase` to preserve descending order.

**[Low] L3. `DoPhasedWorkCallback` uses lambda that ignores captured `this`**

WinUI:
```cpp
BuildTreeScheduler::RegisterWork(
    m_pendingElements[m_pendingElements.size() - 1].VirtInfo()->Phase(),
    [this]() { DoPhasedWorkCallback(); });
```
Uno (`Phaser.mux.cs:177-183`):
```csharp
BuildTreeScheduler.RegisterWork(
    m_pendingElements[m_pendingElements.Count - 1].VirtInfo.Phase,
    () => { DoPhasedWorkCallback(); });
```

Issue: trivial closure captures `this` via instance method call. Identical semantics. Could use `DoPhasedWorkCallback` directly as the delegate. Low priority.

**[Low] L4. `var dataIndex` unused**

Both versions have an unused local. Faithful port. Could remove or `_ =`.

**[Low] L5. `PhaseElement` throws `InvalidOperationException` with "Phase was set..." — phrasing matches WinUI.**

OK, parity-preserving.

---

## UniqueIdElementPool

**File mapping**

| WinUI | Uno |
|-------|-----|
| `UniqueIdElementPool.h` | (none — monolithic `UniqueIdElementPool.cs`) |
| `UniqueIdElementPool.cpp` | `UniqueIdElementPool.cs` |

### Findings

**[Medium] M1. File layout violation — monolithic `.cs` instead of split**

Rule #2 requires `Foo.cs` (decl) + `Foo.h.mux.cs` + `Foo.mux.cs`. `UniqueIdElementPool.cs` is a single file containing decl, fields, and method bodies. The MUX Reference header is also missing.

Suggested fix: split into:
- `UniqueIdElementPool.cs` — `internal partial class UniqueIdElementPool : IEnumerable<...>` decl only.
- `UniqueIdElementPool.h.mux.cs` — `m_elementMap`, `m_owner` fields, and `IsEmpty`.
- `UniqueIdElementPool.mux.cs` — ctor and methods, with `// MUX Reference UniqueIdElementPool.cpp, commit 4b206bce3`.

**[Medium] M2. Missing MUX Reference header**

There is no `// MUX Reference UniqueIdElementPool.cpp, commit 4b206bce3` line. Required by rule #4.

**[Low] L1. `Add` error message paraphrased**

WinUI:
```cpp
std::wstring message = L"The unique id provided (" + std::wstring(virtInfo->UniqueId().data()) + L") is not unique.";
```
Uno (`UniqueIdElementPool.cs:36`):
```csharp
throw new InvalidOperationException($"The unique id provided ({virtInfo.UniqueId}) is not unique.");
```

Functionally identical. The C++ uses two-step concat; Uno uses interpolation. Faithful.

**[Low] L2. `Remove` uses `TryGetValue` + `Remove` (two lookups)**

WinUI uses `find` + `erase(it)` (single lookup). Uno could use `Remove(key, out var element)` to match the single-lookup pattern.

**[Low] L3. `Clear` matches WinUI exactly.**

OK.

**[Info] I1. `IsEmpty` is `#if DEBUG` (matches WinUI's `#ifdef DBG`)**

Properly gated.

**[Info] I2. `IEnumerable<KeyValuePair<string, UIElement>>` mapped from C++ `begin()/end()`**

Uno exposes both `IEnumerable<>` and the typed pair, while WinUI exposes raw `begin/end`. Functional parity. Low.

---

## RepeaterTestHooks

**File mapping**

| WinUI | Uno |
|-------|-----|
| `RepeaterTestHooks.h` + `RepeaterTestHooks.cpp` + `.idl` | `RepeaterTestHooks.cs` (single file) |

### Findings

**[High] H1. File layout violation — single file, no split, no `MUX Reference` header**

Same as UniqueIdElementPool — should be split into `.cs` decl + `.h.mux.cs` fields + `.mux.cs` body. Missing MUX Reference header (rule #4).

**[High] H2. Missing `GetGlobalTestHooks`, `EnsureHooks`, `s_testHooks`, and `m_buildTreeCompleted` field**

WinUI (`RepeaterTestHooks.h:18-21`):
```cpp
static com_ptr<RepeaterTestHooks> GetGlobalTestHooks()
{
    return s_testHooks->get_strong();
}
```
plus instance member `winrt::event<...> m_buildTreeCompleted;` and `s_elementFactoryElementIndex`.

Uno has no `s_testHooks`, no `GetGlobalTestHooks`, no `EnsureHooks`. The Uno `BuildTreeCompleted` is implemented as a plain `static event EventHandler`, which works but doesn't expose the instance-based hook pattern (parity with `LayoutsTestHooks` is also broken — there, Uno did add `s_testHooks` and `GetGlobalTestHooks`).

Suggested fix: add `GetGlobalTestHooks`, `EnsureHooks`, `s_testHooks` to mirror WinUI's pattern.

**[Medium] M1. Missing `SetElementFactoryElementIndex` and `s_elementFactoryElementIndex` storage**

WinUI exports `static int GetElementFactoryElementIndex()` and `static void SetElementFactoryElementIndex(int index)`, plus `static int s_elementFactoryElementIndex;`. The IDL also exposes both. Uno replaces this with `GetElementFactoryElementIndex(object getArgs)` that reads `(args as ElementFactoryGetArgs).Index` — completely different signature and semantics:

```csharp
public static int GetElementFactoryElementIndex(object getArgs)
{
    var args = getArgs as ElementFactoryGetArgs;
    return args.Index;
}
```

The Uno version takes the get-args and returns its Index; WinUI returns the stored static value (set by `SetElementFactoryElementIndex`). This is a deliberate behavioural change without a `// TODO Uno:` marker and without preserving the WinUI signature.

Suggested fix: restore parity by adding the WinUI-shaped static field/getter/setter pair. If the alternative form is needed for Uno's internal use, name it differently (e.g., `GetElementFactoryElementIndexFromArgs`).

**[Medium] M2. Extra methods `CreateRepeaterElementFactoryGetArgs` and `CreateRepeaterElementFactoryRecycleArgs` not in WinUI**

`RepeaterTestHooks.cs:29-40` adds two new factories. They are not present in WinUI's `.h` or `.idl`. Should be wrapped in `#if HAS_UNO` per rule #6.

**[Medium] M3. `BuildTreeCompleted` event handler type changed**

WinUI: `TypedEventHandler<IInspectable, IInspectable>`. Uno: `EventHandler`. The C# stdlib equivalent of `TypedEventHandler<object, object>` would be `TypedEventHandler<object, object>` (also available in WinRT projection); `EventHandler` has a different signature (`object sender, EventArgs e`).

Suggested fix: use `TypedEventHandler<object, object>` to match the WinRT delegate (or document the substitution with `// TODO Uno:`).

**[Low] L1. `/* static */` comment markers preserved consistently — good.**

**[Low] L2. Visibility is `public` for static methods**

Per rule #5 ("private by default; widen only with IDL/docs/Generated evidence"), and given IDL exposes these statically, `public` is acceptable. However, the *class* is `internal`, so `public` on members has no widening effect; OK.

---

## LayoutsTestHooks

**File mapping**

| WinUI | Uno |
|-------|-----|
| `LayoutsTestHooks.h` + `LayoutsTestHooks.cpp` + `.idl` (+ EventArgs classes) | `LayoutsTestHooks.cs` (single file) |

### Findings

**[High] H1. File layout violation — single file, no split**

Same complaint as RepeaterTestHooks. MUX Reference header is present (good) but file layout deviates from rule #2.

**[High] H2. Missing `LinedFlowLayoutSnappedAverageItemsPerLineChanged`, `LinedFlowLayoutInvalidated`, `LinedFlowLayoutItemLocked` events**

WinUI defines three events; Uno has a comment block stub indicating they are "TODO PR 9". Per project rules this is acceptable, but the events are part of the public IDL surface and tests that reference them via reflection will fail to bind.

```csharp
// TODO Uno: LinedFlowLayout is not yet ported (PR 9).
// Original C++ events:
// - LinedFlowLayoutSnappedAverageItemsPerLineChanged ...
// - LinedFlowLayoutInvalidated ...
// - LinedFlowLayoutItemLocked ...
```

Suggested fix: at minimum, declare empty event stubs with `[Uno.NotImplemented]` so binding works; or document explicitly that the events themselves are intentionally absent.

**[High] H3. `LayoutsTestHooksLinedFlowLayoutInvalidatedEventArgs` and `LayoutsTestHooksLinedFlowLayoutItemLockedEventArgs` types not ported**

WinUI defines two `runtimeclass` event-args types in the IDL with `InvalidationTrigger`, `ItemIndex`, `LineIndex` properties. These don't exist in Uno. Acceptable as PR 9 deferment but should be tracked in `TODO Uno:` block.

**[High] H4. `LinedFlowLayoutInvalidationTrigger` enum not ported**

IDL declares `enum LinedFlowLayoutInvalidationTrigger { InvalidateLayoutCall = 0, ... }`. Not present in Uno's `LayoutsTestHooks.cs`. Acceptable as PR 9 deferment.

**[Medium] M1. `EnsureHooks` is private but called from internal statics — `RepeaterTestHooks` has no equivalent**

`LayoutsTestHooks.EnsureHooks` exists. Good parity with WinUI. But `RepeaterTestHooks` lacks it (see RepeaterTestHooks H2).

**[Medium] M2. `LayoutAnchorIndexChanged` returns `IDisposable` instead of token semantics**

WinUI:
```cpp
static winrt::event_token LayoutAnchorIndexChanged(...);
static void LayoutAnchorIndexChanged(winrt::event_token const& token);
```
Uno (`LayoutsTestHooks.cs:202-207`):
```csharp
internal static IDisposable LayoutAnchorIndexChanged(TypedEventHandler<object, object> value)
{
    EnsureHooks();
    s_testHooks.m_layoutAnchorIndexChangedEventSource += value;
    return Uno.Disposables.Disposable.Create(() => s_testHooks.m_layoutAnchorIndexChangedEventSource -= value);
}
```

Functionally equivalent (`IDisposable` is the .NET idiom for `event_token`). However per rule #7 (revokers → `SerialDisposable` + `Disposable.Create(...)`), the use of `Disposable.Create` is acceptable. The conversion is documented elsewhere; consider adding a `// TODO Uno:` to mark the deliberate API shape change.

**[Medium] M3. Field naming convention differs from project rule for events**

`m_layoutAnchorIndexChangedEventSource` is an instance event field but uses the lowercase `m_` prefix following WinUI naming. Uno style elsewhere is `private event ... EventName;`. Low/medium consistency issue.

**[Medium] M4. `NotifyLayoutAnchorIndexChanged` is `internal` instance method, not static**

WinUI:
```cpp
static void NotifyLayoutAnchorIndexChanged(winrt::IInspectable const& layout);
```
Uno (`LayoutsTestHooks.cs:209-212`):
```csharp
internal void NotifyLayoutAnchorIndexChanged(object layout)
{
    m_layoutAnchorIndexChangedEventSource?.Invoke(layout, null);
}
```

WinUI exposes `Notify...` as static (it calls `EnsureHooks()` then dispatches through `s_testHooks->...`). Uno makes it an instance method. Suggested fix: make `Notify*` static for parity.

**[Low] L1. Many methods marked `internal` rather than `public`**

The IDL has `[MUX_INTERNAL]`. `internal` is the correct mapping (rule #5). Good.

**[Low] L2. `GetLayoutFirstRealizedItemIndex`/`GetLayoutLastRealizedItemIndex` always return `-1`**

The full method exists but is body-stubbed for PR 9. Acceptable.

**[Low] L3. `LayoutInvalidateMeasure` `relayout` branch always falls through**

Body-stubbed for PR 9. Acceptable.

---

## _Tracing (Uno-only)

**File mapping**: Uno-only helper. No WinUI counterpart needed.

### Findings

**[Info] I1. `_Tracing.cs` provides `REPEATER_TRACE_INFO`, `REPEATER_TRACE_PERF`, and `MUX_ASSERT`**

Reasonable substitution for WinUI's `ITEMSREPEATER_TRACE_*` macros and `MUX_ASSERT`. `[Conditional]` attribute correctly gates them. No issues found.

---

## CachedVisualTreeHelpers (Uno-only)

**File mapping**: Uno-only helper. No WinUI counterpart found in MUX Repeater sources.

### Findings

**[Medium] M1. Mixed concern — wraps three different APIs**

`CachedVisualTreeHelpers` wraps `LayoutInformation.GetLayoutSlot`, `VisualTreeHelper.GetParent`, and `XamlBindingHelper.GetDataTemplateComponent`. The name implies "cached" but no caching is performed — it is a flat passthrough. Suggested fix: rename to `VisualTreeHelpersBridge` or document that the "cached" name is aspirational.

**[Low] L1. Missing MUX Reference header**

It's Uno-only, but a comment explaining its purpose would aid future porting. Mark with `// This is a Uno-only helper. No WinUI counterpart.`

**[Info] I1. The class does not override any WinUI behaviour and is purely additive.**

Acceptable.

---

## IKeyIndexMapping

**File mapping**: declared in `ItemsRepeater.idl` (`MUX_PUBLIC`). Uno: `IKeyIndexMapping.cs`.

### Findings

**[Low] L1. Methods match IDL exactly**

`KeyFromIndex(int) string` and `IndexFromKey(string) int` match. `public` visibility matches `MUX_PUBLIC`. OK.

---

## IPanel (Uno-only)

**File mapping**: Uno-only `internal partial interface IPanel`. No WinUI counterpart with this name; WinUI uses `DeriveFromPanelHelper_base` in C++.

### Findings

**[Low] L1. Doc comment is informative**

Explains the WinUI mechanism it mirrors. Good.

**[Info] I1. Confirmed Uno-only (no IDL or `.h` declaration in WinUI).**

---

## IRepeaterScrollingSurface

**File mapping**: declared in `RepeaterPrivate.idl` (`MUX_INTERNAL`). Uno: `IRepeaterScrollingSurface.cs`.

### Findings

**[Medium] M1. Member ordering does not match IDL**

IDL order: `IsHorizontallyScrollable`, `IsVerticallyScrollable`, `AnchorElement`, `ConfigurationChanged`, `PostArrange`, `ViewportChanged`, `RegisterAnchorCandidate`, `UnregisterAnchorCandidate`, `GetRelativeViewport`.
Uno order matches the IDL. Good.

However the delegate `ViewportChangedEventHandler` declaration uses `bool isFinal` (matches IDL Boolean). Good.

**[Low] L1. Delegate names use Uno's naming, prefer matching IDL**

IDL has `ConfigurationChangedEventHandler`, `PostArrangeEventHandler`, `ViewportChangedEventHandler`. Uno matches. Good.

---

## ScrollOrientation

**File mapping**: declared in `OrientationBasedMeasures.h` (`enum class` — not via IDL). Uno: `ScrollOrientation.cs`.

### Findings

**[Low] L1. Enum values match**

WinUI:
```cpp
enum class ScrollOrientation
{
    Vertical,
    Horizontal
};
```
Uno:
```csharp
internal enum ScrollOrientation
{
    Vertical, // MUST be default
    Horizontal
}
```

OK. Visibility is correctly `internal` (matches non-IDL declaration).

---

## Cross-type observations

1. **MUX Reference headers** — `BuildTreeScheduler.*`, `ChildrenInTabFocusOrderIterable.*`, `CustomProperty.*`, `Phaser.*`, `LayoutsTestHooks.cs` all have the required header. `UniqueIdElementPool.cs`, `RepeaterTestHooks.cs`, `CachedVisualTreeHelpers.cs`, `IKeyIndexMapping.cs`, `IPanel.cs`, `IRepeaterScrollingSurface.cs`, `ScrollOrientation.cs`, `_Tracing.cs` are missing it.

2. **File-layout violations** — `UniqueIdElementPool`, `RepeaterTestHooks`, `LayoutsTestHooks` are monolithic files; rule #2 requires the 3-file split.

3. **Trace-call dropping** — `ElementInfo` ctor (Phaser), `BuildTreeScheduler.OnRendering` (TraceLoggingProviderWrite) silently drop trace calls without `// TODO Uno:` markers. Multiple call sites are affected.

4. **Uno-specific additions un-marked** — `BuildTreeScheduler.mux.cs:23-28` lazy-init, `RepeaterTestHooks.CreateRepeaterElementFactoryGetArgs/RecycleArgs`, and `Stopwatch.Restart()` substitution are all Uno-specific but not wrapped in `#if HAS_UNO` nor marked with `// TODO Uno:`.

5. **Comparator-direction bugs** — Two confirmed inversions (BuildTreeScheduler sort `lhs.Priority - rhs.Priority`, Phaser `SortElements` visible-window branch). Both cause real behavioural drift from WinUI.

6. **IIterator/IEnumerator gap** — `ChildrenInTabFocusOrderIterable` drops `HasCurrent` and `GetMany`; not marked.

7. **Event delegate substitution** — `RepeaterTestHooks` uses `EventHandler` instead of `TypedEventHandler<object, object>`; consider aligning with `LayoutsTestHooks` which uses `TypedEventHandler`.

8. **`m_index = -1` start vs WinUI `0`** — `ChildrenInTabFocusOrderIterator`: justified by `IEnumerator<T>` semantics, but `Current` getter does not guard against `m_index < 0`.

---

## Conclusion

**Total findings**

| Severity | Count |
|----------|-------|
| High | 7 |
| Medium | 17 |
| Low | 28 |
| Info | 5 |

**Top priority issues (must address)**

1. **[H1, BuildTreeScheduler L1]** Sort comparator direction is inverted vs WinUI — fix to descending (and same in Phaser `SortElements`).
2. **[ChildrenInTabFocusOrderIterable H1]** `HasCurrent` / `GetMany` missing — at minimum document or restore.
3. **[RepeaterTestHooks H1, H2]** Missing `s_testHooks`/`GetGlobalTestHooks`/`EnsureHooks`; signature drift on `GetElementFactoryElementIndex`.
4. **[LayoutsTestHooks H1–H4]** File layout, missing events/types — PR 9 markers exist; should be tightened.
5. **[UniqueIdElementPool M1, M2]** Add MUX Reference header and split into 3 files.
6. **[BuildTreeScheduler M1–M3]** Mark Uno-specific deltas with `// TODO Uno:` or `#if HAS_UNO`.
7. **[Phaser M1, L2]** Restore trace call; fix `SortElements` comparator inversion.
