# Layout Core Types Comparison Report

**WinUI commit:** 4b206bce3
**Types covered:** Layout, NonVirtualizingLayout, VirtualizingLayout, OrientationBasedMeasures

## Summary

| Type | Critical | High | Medium | Low | Info |
|------|----------|------|--------|-----|------|
| Layout | 0 | 4 | 5 | 5 | 4 |
| NonVirtualizingLayout | 0 | 1 | 1 | 1 | 1 |
| VirtualizingLayout | 0 | 1 | 1 | 1 | 1 |
| OrientationBasedMeasures | 0 | 3 | 3 | 3 | 2 |
| **Total** | **0** | **9** | **10** | **10** | **8** |

---

## Per-type sections

### Layout

**File mapping**

| Role | C++ file | Uno file |
|------|----------|----------|
| Declaration | `Layout.h` (class shell) | `Layout.cs` |
| Fields/constants/inline | `Layout.h` (private/public-inline) | `Layout.h.mux.cs` |
| `.cpp` body | `Layout.cpp` | `Layout.mux.cs` |
| Uno additions | n/a | `Layout.uno.cs` |

**Method order verification (Layout.cpp vs Layout.mux.cs)**

| # | C++ order (Layout.cpp) | C# order (Layout.mux.cs) | Match |
|---|------------------------|---------------------------|-------|
| 1 | `LayoutId` getter | `LayoutId` (combined property) | Mostly |
| 2 | `LayoutId` setter | (combined into property) | Mostly |
| 3 | `LogItemIndexDbg()` const | `LogItemIndexDbg()` | Yes |
| 4 | `LogItemIndexDbg(int)` | `LogItemIndexDbg(int logItemIndex)` | Yes |
| 5 | `LayoutAnchorIndexDbg()` | `LayoutAnchorIndexDbg()` | Yes |
| 6 | `LayoutAnchorOffsetDbg()` | `LayoutAnchorOffsetDbg()` | Yes |
| 7 | `GetForcedIndexBasedLayoutOrientationDbg()` | `GetForcedIndexBasedLayoutOrientationDbg()` | Yes |
| 8 | `SetForcedIndexBasedLayoutOrientationDbg()` | `SetForcedIndexBasedLayoutOrientationDbg()` | Yes |
| 9 | `ResetForcedIndexBasedLayoutOrientationDbg()` | `ResetForcedIndexBasedLayoutOrientationDbg()` | Yes |
| 10 | `SetLayoutAnchorInfoDbg()` (`#ifdef DBG`) | `SetLayoutAnchorInfoDbg()` | Yes |
| 11 | `IndexBasedLayoutOrientation()` (getter) | `IndexBasedLayoutOrientation` property | Yes |
| 12 | anonymous-namespace `GetVirtualizingLayoutContext` | `GetVirtualizingLayoutContext` static | Yes |
| 13 | anonymous-namespace `GetNonVirtualizingLayoutContext` | `GetNonVirtualizingLayoutContext` static | Yes |
| 14 | `InitializeForContext` | `InitializeForContext` | Yes |
| 15 | `UninitializeForContext` | `UninitializeForContext` | Yes |
| 16 | `Measure` | `Measure` | Yes |
| 17 | `Arrange` | `Arrange` | Yes |
| 18 | `MeasureInvalidated` add | `MeasureInvalidated` event | Yes |
| 19 | `MeasureInvalidated` remove | (combined into event) | Yes |
| 20 | `ArrangeInvalidated` add | `ArrangeInvalidated` event | Yes |
| 21 | `ArrangeInvalidated` remove | (combined) | Yes |
| 22 | `InvalidateMeasure` | `InvalidateMeasure` | Yes |
| 23 | `InvalidateArrange` | `InvalidateArrange` | Yes |
| 24 | `SetIndexBasedLayoutOrientation` | `SetIndexBasedLayoutOrientation` | Yes |

**Field/constant verification (Layout.h vs Layout.h.mux.cs)**

| C++ field | C# field | Match |
|-----------|----------|-------|
| `m_measureInvalidatedEventSource` | (replaced by `MeasureInvalidated` event in `.mux.cs`) | Different placement |
| `m_arrangeInvalidatedEventSource` | (replaced by `ArrangeInvalidated` event in `.mux.cs`) | Different placement |
| `m_layoutId` | `m_layoutId` | Yes |
| `m_indexBasedLayoutOrientation { IndexBasedLayoutOrientation::None }` | `m_indexBasedLayoutOrientation = IndexBasedLayoutOrientation.None` | Yes |
| `m_logItemIndexDbg { -1 }` | `m_logItemIndexDbg = -1` | Yes |
| `m_layoutAnchorInfoDbg { -1, -1.0 }` | `m_layoutAnchorInfoDbg = new(-1, -1.0)` | Yes |
| `m_forcedIndexBasedLayoutOrientationDbg { ::None }` | `m_forcedIndexBasedLayoutOrientationDbg = .None` | Yes |
| `m_isForcedIndexBasedLayoutOrientationSetDbg { false }` | `m_isForcedIndexBasedLayoutOrientationSetDbg` (default `false`) | Yes |
| `CreateDefaultItemTransitionProvider` (inline) | `CreateDefaultItemTransitionProvider` (inline, `protected internal`) | Visibility widened |

**Findings**

#### L-1 (High) — `LayoutId` getter/setter is split in C++ but coalesced as a property in C#

C++ (Layout.cpp lines 14–22):
```cpp
winrt::hstring Layout::LayoutId() { return m_layoutId; }
void Layout::LayoutId(winrt::hstring const& value) { m_layoutId = value; }
```
C# (Layout.mux.cs lines 16–20):
```csharp
internal string LayoutId
{
    get => m_layoutId;
    set => m_layoutId = value;
}
```
Issue: Layout.h declares `LayoutId` as a public WinRT projection (it is one of the public properties exposed via the projected `winrt::Layout` interface; see Layout.idl). The Uno port reduces its visibility to `internal`, breaking ABI/API parity. WinUI exposes `Layout.LayoutId` publicly.
Suggested fix: Change to `public string LayoutId { get; set; }` (backed by `m_layoutId`) per the public IDL of Layout.

#### L-2 (High) — `InvalidateMeasure` visibility widened to `protected internal`

C++ (Layout.h line 45 within `#pragma region ILayoutProtected`):
```cpp
void InvalidateMeasure();
```
C# (Layout.mux.cs):
```csharp
protected internal void InvalidateMeasure()
```
Issue: In the WinUI IDL these are `protected` members of the public `Layout` class. The Uno port widens `InvalidateMeasure` to `protected internal` "so `LayoutsTestHooks` can call it directly". The same justification was not applied to `InvalidateArrange`, which remains `protected` — inconsistent. Widening here also leaks the API to all consumers of the assembly.
Suggested fix: Keep `protected` to match WinUI. Use `internal void InvalidateMeasureInternal()` helper that calls `InvalidateMeasure()` via reflection or via `LayoutsTestHooks` friendship if cross-assembly access is genuinely required.

#### L-3 (High) — `CreateDefaultItemTransitionProvider` visibility widened to `protected internal`

C++ (Layout.h line 41):
```cpp
virtual winrt::ItemCollectionTransitionProvider CreateDefaultItemTransitionProvider() { return nullptr; }
```
C# (Layout.h.mux.cs line 22):
```csharp
protected internal virtual ItemCollectionTransitionProvider CreateDefaultItemTransitionProvider() => null;
```
Issue: Comment explicitly states the widening so that ItemsRepeater can invoke it. WinUI exposes this as `protected`. Widening to `protected internal` violates the porting rule "Private by default; widen only with IDL/Microsoft Learn/Generated evidence" and the user feedback "match WinUI API surface rather than filtering/suppressing differences".
Suggested fix: Restore `protected`. Have `ItemsRepeater` call through a non-public bridge (`internal CreateDefaultItemTransitionProviderInternal()` shim, or unsafe-cast).

#### L-4 (High) — Anonymous-namespace helpers `GetVirtualizingLayoutContext` / `GetNonVirtualizingLayoutContext` widened to `internal static`

C++ (Layout.cpp lines 95–130):
```cpp
namespace
{
    winrt::VirtualizingLayoutContext GetVirtualizingLayoutContext(winrt::LayoutContext const& context) { ... }
    winrt::NonVirtualizingLayoutContext GetNonVirtualizingLayoutContext(winrt::LayoutContext const& context) { ... }
}
```
C# (Layout.mux.cs lines 87–111):
```csharp
internal static VirtualizingLayoutContext GetVirtualizingLayoutContext(LayoutContext context) { ... }
internal static NonVirtualizingLayoutContext GetNonVirtualizingLayoutContext(LayoutContext context) { ... }
```
Issue: The C++ anonymous namespace gives these file-private linkage. They should be `private static` in C# (only `Layout` itself uses them). The port marked them `internal`, which exposes them to the entire Uno.UI assembly.
Suggested fix: `private static`.

#### L-5 (Medium) — `#pragma region` markers converted to comments instead of preserved

C++ uses `#pragma region ILayout`, `#pragma region ILayoutProtected`. C# port writes `// #pragma region ILayout`, `// #pragma endregion`.
Issue: C# does support `#region` / `#endregion`, so the markers can be preserved verbatim. Converting to comments hides the region from IDE outlining and violates rule "`#pragma region` preserved at same relative position".
Suggested fix: Use `#region ILayout` / `#endregion` and `#region ILayoutProtected` / `#endregion`.

#### L-6 (Medium) — `LogItemIndexDbg()` getter modeled as method, not property

C++ uses overloaded `LogItemIndexDbg()` / `LogItemIndexDbg(int)`. In C# this is mapped to two methods `LogItemIndexDbg()` and `LogItemIndexDbg(int)`. WinUI exposes `LogItemIndexDbg` via getter/setter naming that the project rule normally maps `Foo()` getter → `Foo` property. Same applies to `LayoutAnchorIndexDbg`, `LayoutAnchorOffsetDbg`, `GetForcedIndexBasedLayoutOrientationDbg`.
Issue: Inconsistent application of "Foo() getter → Foo property" rule across the type. These are test-hook accessors; the C# overload pattern works but readers expect properties.
Suggested fix: Convert read-only `*Dbg()` getters into properties (e.g., `internal int LayoutAnchorIndexDbg => m_layoutAnchorInfoDbg.Index;`). Or leave a comment explaining why the method form was preferred.

#### L-7 (Medium) — `SetLayoutAnchorInfoDbg` widened from `protected` to `private protected`

C++ (Layout.h line 53):
```cpp
protected:
#ifdef DBG
    void SetLayoutAnchorInfoDbg(int index, double offset);
#endif
```
C# (Layout.mux.cs line 46):
```csharp
private protected void SetLayoutAnchorInfoDbg(int index, double offset)
```
Issue: `private protected` in C# only allows access from derived types in the same assembly. The C++ `protected` allows any subclass (including external). For test-hook code, the C++ surface is `protected`. The port narrows it, then declares "always available" but the visibility is too narrow for true `protected` semantics. Combined with the `// #ifdef DBG` comment-only guard, the test hook is silently always-on but only callable from same-assembly subclasses.
Suggested fix: Use `protected` (or `internal` if used only from test hooks). Provide explicit rationale comment about removing the `#ifdef DBG` guard.

#### L-8 (Medium) — `m_isForcedIndexBasedLayoutOrientationSetDbg` lacks explicit `= false` initializer

C++ (Layout.h line 70):
```cpp
bool m_isForcedIndexBasedLayoutOrientationSetDbg{ false };
```
C# (Layout.h.mux.cs line 34):
```csharp
private bool m_isForcedIndexBasedLayoutOrientationSetDbg;
```
Issue: C# default for `bool` is `false`, so behavior matches, but the C++ explicit initializer was elided. Minor stylistic divergence; for parity readability, keep the explicit `= false`.
Suggested fix: `private bool m_isForcedIndexBasedLayoutOrientationSetDbg = false;`

#### L-9 (Medium) — `_measureInvalidatedHandlers` / `_arrangeInvalidatedHandlers` placement in `.uno.cs`

C# (Layout.uno.cs lines 19–20) declares `WeakEventHelper.WeakEventCollection _measureInvalidatedHandlers` and `_arrangeInvalidatedHandlers`. They are referenced from `Layout.mux.cs` `InvalidateMeasure` / `InvalidateArrange` under `#if HAS_UNO`. This couples `.mux.cs` to the `.uno.cs` additions instead of keeping the bridge contained inside `.uno.cs`. The hook fires the weak-event list before the WinUI event — which mostly matches `m_measureInvalidatedEventSource(*this, nullptr);` semantics, but the dual-collection model is Uno-specific and not documented in the file header beyond `Layout.uno.cs`.
Issue: Logic divergence from C++: WinUI invokes `m_measureInvalidatedEventSource(*this, nullptr)` once; Uno invokes both a weak-event list and the .NET event. Order: weak first, then strong. This is mostly fine but should be documented at the call site.
Suggested fix: Add comment at the `#if HAS_UNO` block in `InvalidateMeasure` / `InvalidateArrange` referencing `Layout.uno.cs`. Alternatively, expose an `internal partial void OnMeasureInvalidatedUno()` partial method declared in `.uno.cs` so the conditional code in `.mux.cs` is uniform.

#### L-10 (Low) — `// Anonymous namespace in C++ — Uno: internal static helpers on the type itself.` comment moved

The C++ uses `namespace { ... }` (anonymous namespace) at lines 95–130. The Uno port has an explanatory comment but places it on the same line as the first helper. This is informational only.
Suggested fix: Move comment to a separate line directly above both helpers for clarity (and reduce visibility per L-4).

#### L-11 (Low) — `m_layoutId` initializer comment "TODO" reformatted

C++ (Layout.h lines 60–62):
```cpp
// TODO: This is for debugging purposes only. It should be removed when 
// the Layout.LayoutId API is removed.
winrt::hstring m_layoutId;
```
C# (Layout.h.mux.cs lines 24–26):
```csharp
// TODO: This is for debugging purposes only. It should be removed when
// the Layout.LayoutId API is removed.
internal string m_layoutId;
```
Issue: `m_layoutId` is now `internal` field. The trailing space in C++ line 60 was trimmed (harmless). The widening from implicit private (C++ default after `private:` access modifier) to `internal` is unnecessary if `LayoutId` is properly modeled as a public property (see L-1).
Suggested fix: After fixing L-1, change to `private string m_layoutId;`.

#### L-12 (Low) — `m_indexBasedLayoutOrientation` use of fully-qualified `IndexBasedLayoutOrientation.None`

C++ uses `winrt::IndexBasedLayoutOrientation::None`. C# uses `IndexBasedLayoutOrientation.None`. The class name `Layout` has an `IndexBasedLayoutOrientation` property of the same name, which could shadow the enum at certain compile sites. Verify no ambiguity. (In practice, the property is on `this`, so a bare enum identifier in field initializer still refers to the enum type.)
Suggested fix: None required; consider using fully-qualified `IndexBasedLayoutOrientation.None` consistently in body code.

#### L-13 (Low) — Comment "// In C++ these use winrt::event_token add/remove; in C# they are plain events." inserted

C++ (Layout.cpp lines 213–231) has four overloaded methods. C# (Layout.mux.cs lines 199–205) collapses to two `event` declarations with an explanatory comment. Acceptable conversion, but the comment is informational and not in C++.
Suggested fix: Acceptable. No change.

#### L-14 (Low) — `// TODO Uno:` not used for `LogItemIndexDbg` overload accessors

The two `Log*` methods convert from C++ overload to a single property in C# (per L-6 suggested), but since they remain as methods, no `TODO Uno:` is required. This is informational only.

#### L-15 (Info) — File layout

`Layout.cs` correctly contains only the declaration shell. `Layout.h.mux.cs` contains fields + inline `CreateDefaultItemTransitionProvider`. `Layout.mux.cs` contains the `.cpp` body. `Layout.uno.cs` contains Uno-only additions. This matches the project's prescribed layout (rule 2).

#### L-16 (Info) — MUX Reference headers

All four files contain the required MUX Reference header at line 3 (commit `4b206bce3`). `Layout.uno.cs` does not have a MUX Reference (correct — it is Uno additions).

#### L-17 (Info) — Destructor

C++ has no explicit destructor for `Layout`. No C# finalizer was added or needed.

#### L-18 (Info) — XML documentation

`InitializeForContext`, `UninitializeForContext`, `Measure`, `Arrange`, `IndexBasedLayoutOrientation`, `MeasureInvalidated`, `ArrangeInvalidated`, `InvalidateMeasure`, `InvalidateArrange`, `SetIndexBasedLayoutOrientation`, `CreateDefaultItemTransitionProvider` all carry XML doc comments. Compliant with rule 9.

---

### NonVirtualizingLayout

**File mapping**

| Role | C++ file | Uno file |
|------|----------|----------|
| Declaration | `NonVirtualizingLayout.h` | `NonVirtualizingLayout.cs` |
| `.cpp` body | `NonVirtualizingLayout.cpp` | `NonVirtualizingLayout.mux.cs` |

**Method order verification**

| # | C++ order (NonVirtualizingLayout.cpp) | C# order (NonVirtualizingLayout.mux.cs) | Match |
|---|----------------------------------------|------------------------------------------|-------|
| 1 | `NonVirtualizingLayout()` ctor (`__RP_Marker_ClassById`) | `NonVirtualizingLayout()` ctor (comment) | Yes |
| 2 | `InitializeForContextCore` | `InitializeForContextCore` | Yes |
| 3 | `UninitializeForContextCore` | `UninitializeForContextCore` | Yes |
| 4 | `MeasureOverride` | `MeasureOverride` | Yes |
| 5 | `ArrangeOverride` | `ArrangeOverride` | Yes |

**Field/constant verification:** No fields in either file. Match.

**Findings**

#### NVL-1 (High) — `Initialize/UninitializeForContextCore`, `Measure/ArrangeOverride` visibility widened to `protected internal`

C++ (NonVirtualizingLayout.h lines 16–20):
```cpp
virtual void InitializeForContextCore(winrt::LayoutContext const& context);
virtual void UninitializeForContextCore(winrt::LayoutContext const& context);
virtual winrt::Size MeasureOverride(winrt::NonVirtualizingLayoutContext const& context, winrt::Size const& availableSize);
virtual winrt::Size ArrangeOverride(winrt::NonVirtualizingLayoutContext const& context, winrt::Size const& finalSize);
```
Public in C++ projection (override-able by derived types).
C# (NonVirtualizingLayout.mux.cs lines 24–56):
```csharp
protected internal virtual void InitializeForContextCore(LayoutContext context)
protected internal virtual void UninitializeForContextCore(LayoutContext context)
protected internal virtual Size MeasureOverride(NonVirtualizingLayoutContext context, Size availableSize)
protected internal virtual Size ArrangeOverride(NonVirtualizingLayoutContext context, Size finalSize)
```
Issue: WinUI marks these as **public** overridable methods on `NonVirtualizingLayout` (they ship as `*Core`/`*Override` virtuals in the IDL). The Uno port narrows them to `protected internal`. Documented WinUI signature on `NonVirtualizingLayout` lists `MeasureOverride` and `ArrangeOverride` as `protected` (overridable) — there is no `*Core` in the IDL because the names `MeasureOverride`/`ArrangeOverride` are the public IDL surface. Compare to `Microsoft.UI.Xaml.Controls.NonVirtualizingLayout` API on Microsoft Learn: methods are listed as `protected`.
Suggested fix: Change to `protected virtual` to match WinUI. If `Layout` needs to dispatch to these from outside the inheritance chain, add a non-public bridge method or use `Layout.GetMeasureOverride()` helper.

#### NVL-2 (Medium) — `__RP_Marker_ClassById(RuntimeProfiler::ProfId_NonVirtualizingLayout)` not invoked

C++ (NonVirtualizingLayout.cpp line 17):
```cpp
__RP_Marker_ClassById(RuntimeProfiler::ProfId_NonVirtualizingLayout);
```
C# (NonVirtualizingLayout.mux.cs line 16):
```csharp
//__RP_Marker_ClassById(RuntimeProfiler.ProfId_NonVirtualizingLayout);
```
Issue: Commented out. There is no `TODO Uno:` marker and no `#if !HAS_UNO` block, per porting rule 6.
Suggested fix:
```csharp
#if !HAS_UNO
    // TODO Uno: __RP_Marker_ClassById(RuntimeProfiler.ProfId_NonVirtualizingLayout);
#endif
```

#### NVL-3 (Low) — `#pragma region INonVirtualizingLayoutOverrides` converted to comments

Same issue as L-5 above. Use `#region` / `#endregion`.

#### NVL-4 (Info) — XML doc, header, file layout

XML doc on all four virtuals, MUX header on both files (commit `4b206bce3`), no destructor needed, no fields. Compliant.

---

### VirtualizingLayout

**File mapping**

| Role | C++ file | Uno file |
|------|----------|----------|
| Declaration | `VirtualizingLayout.h` | `VirtualizingLayout.cs` |
| `.cpp` body | `VirtualizingLayout.cpp` | `VirtualizingLayout.mux.cs` |
| Uno additions | n/a | `VirtualizingLayout.uno.cs` |

**Method order verification**

| # | C++ order | C# order | Match |
|---|-----------|----------|-------|
| 1 | `VirtualizingLayout()` ctor | `VirtualizingLayout()` ctor | Yes |
| 2 | `InitializeForContextCore` | `InitializeForContextCore` | Yes |
| 3 | `UninitializeForContextCore` | `UninitializeForContextCore` | Yes |
| 4 | `MeasureOverride` | `MeasureOverride` | Yes |
| 5 | `ArrangeOverride` | `ArrangeOverride` | Yes |
| 6 | `OnItemsChangedCore` | `OnItemsChangedCore` | Yes |

**Field/constant verification:** No fields in either file. Match.

**Findings**

#### VL-1 (High) — `Initialize/UninitializeForContextCore`, `MeasureOverride`, `ArrangeOverride`, `OnItemsChangedCore` visibility widened to `protected internal`

Same root issue as NVL-1. C++ exposes these as public virtual; WinUI IDL marks them as `protected`. The C# port narrows to `protected internal`. Use `protected virtual` instead.

#### VL-2 (Medium) — `__RP_Marker_ClassById(RuntimeProfiler::ProfId_VirtualizingLayout)` not invoked

C++ (VirtualizingLayout.cpp line 16):
```cpp
__RP_Marker_ClassById(RuntimeProfiler::ProfId_VirtualizingLayout);
```
C# (VirtualizingLayout.mux.cs line 17):
```csharp
//__RP_Marker_ClassById(RuntimeProfiler.ProfId_VirtualizingLayout);
```
Issue: Commented out; needs `TODO Uno:` and `#if !HAS_UNO` wrapping.

#### VL-3 (Low) — `#pragma region IVirtualizingLayoutOverrides` converted to comments

Same as L-5. Use `#region` / `#endregion`.

#### VL-4 (Info) — `VirtualizingLayout.uno.cs` `IsSignificantViewportChange`

Uno-only API, correctly wrapped under `#if !__SKIA__` and marked `[UnoOnly]`. The method is `protected internal virtual` (extension hook for non-Skia repeaters). This is acceptable per rule 6 ("`#if HAS_UNO` for Uno-specific code"). Note the guard is `#if !__SKIA__`, which is stricter than `HAS_UNO` — the method exists on non-Skia targets only. Consistent with the user memory "Skia is now baseline; Uno workarounds must be removed or wrapped in `#if !__SKIA__`".

---

### OrientationBasedMeasures

**File mapping**

| Role | C++ file | Uno file |
|------|----------|----------|
| Declaration | `OrientationBasedMeasures.h` | `OrientationBasedMeasures.cs` (interface) |
| `.cpp` body | `OrientationBasedMeasures.cpp` | `OrientationBasedMeasures.mux.cs` (static extension class) |

The port uses an **interface** (`OrientationBasedMeasures`) + static extension class (`OrientationBasedMeasuresExtensions`) approach to substitute for the C++ `OrientationBasedMeasures` base class (C# does not allow multiple base inheritance).

**Method order verification (OrientationBasedMeasures.cpp vs OrientationBasedMeasures.mux.cs)**

| # | C++ order | C# order | Match |
|---|-----------|----------|-------|
| 1 | `Major(Size&)` | `Major(Size)` | Yes |
| 2 | `Minor(Size&)` | `Minor(Size)` | Yes |
| 3 | `Major(Point&)` | `Major(Point)` | Yes |
| 4 | `Minor(Point&)` | `Minor(Point)` | Yes |
| 5 | `MajorSize(Rect&)` | `MajorSize(Rect)` | Yes |
| 6 | — (C++ has only `MajorSize` returning ref) | `SetMajorSize(ref Rect, double)` | Extra (port helper) |
| 7 | `MinorSize(Rect&)` | `MinorSize(Rect)` | Yes |
| 8 | — | `SetMinorSize(ref Rect, double)` | Extra (port helper) |
| 9 | `MajorStart(Rect&)` | `MajorStart(Rect)` | Yes |
| 10 | — | `SetMajorStart(ref Rect, double)` | Extra (port helper) |
| 11 | `MajorEnd(Rect&)` const | `MajorEnd(Rect)` | Yes |
| 12 | `MinorStart(Rect&)` | `MinorStart(Rect)` | Yes |
| 13 | — | `SetMinorStart(ref Rect, double)` | Extra (port helper) |
| 14 | — | `AddMinorStart(ref Rect, double)` | Extra (port helper) |
| 15 | `MinorEnd(Rect&)` const | `MinorEnd(Rect)` | Yes |
| 16 | `MinorMajorRect` | `MinorMajorRect` | Yes |
| 17 | `MinorMajorPoint` | `MinorMajorPoint` | Yes |
| 18 | `MinorMajorSize` | `MinorMajorSize` | Yes |

Order of base helpers matches; insertion of `SetXxx`/`AddMinorStart` is justified by C# lacking `ref`-returning property semantics.

**Field/constant verification**

| C++ | C# | Match |
|-----|-----|-------|
| `enum class ScrollOrientation { Vertical, Horizontal };` (OrientationBasedMeasures.h lines 6–10) | `internal enum ScrollOrientation { Vertical, Horizontal }` in `ScrollOrientation.cs` | Yes (definition moved to a separate file) |
| `m_orientation { ScrollOrientation::Vertical }` | `ScrollOrientation ScrollOrientation { get; set; }` on interface (default value supplied by implementer; not guaranteed) | **Divergent** (see OBM-1) |

**Findings**

#### OBM-1 (High) — Default `m_orientation` value not enforced on the interface

C++ (OrientationBasedMeasures.h line 37):
```cpp
private:
    ScrollOrientation m_orientation { ScrollOrientation::Vertical };
```
C# (OrientationBasedMeasures.cs lines 15–18):
```csharp
internal interface OrientationBasedMeasures
{
    ScrollOrientation ScrollOrientation { get; set; }
}
```
Issue: The C++ base class guarantees default-Vertical for any concrete derived class because `m_orientation` is initialized in the base. The C# interface places responsibility on each implementer (`StackLayout`, `FlowLayout`, `UniformGridLayout`) to initialize their backing field. Any future implementer that forgets to initialize will get the default `ScrollOrientation.Vertical` only because the enum's first value is `Vertical` (and the `// MUST be default` comment in `ScrollOrientation.cs` calls this out). The contract is implicit, not enforced.
Suggested fix: Either add `ScrollOrientation ScrollOrientation { get; set; } = ScrollOrientation.Vertical;` (default interface member, C# 8+), or document the invariant in the interface doc comment. Verify each implementer (StackLayout, FlowLayout, UniformGridLayout) initializes its backing field.

#### OBM-2 (High) — Accessors named `Major(rect)`/`MajorStart(rect)`/etc. take `Rect` by value, not by reference

C++ (OrientationBasedMeasures.cpp lines 27–51):
```cpp
float& OrientationBasedMeasures::MajorSize(const winrt::Rect& rect) { ... }
float& OrientationBasedMeasures::MajorStart(const winrt::Rect& rect) { ... }
float& OrientationBasedMeasures::MinorStart(const winrt::Rect& rect) { ... }
```
These return **mutable references** so call sites can do `MajorStart(rect) = 5.0f;` or `MinorStart(rect) += increment;`.

C# (OrientationBasedMeasures.mux.cs lines 30, 45, 60, 78):
```csharp
public static double MajorSize(this OrientationBasedMeasures obm, Rect rect) => ...
public static double MajorStart(this OrientationBasedMeasures obm, Rect rect) => ...
public static double MinorStart(this OrientationBasedMeasures obm, Rect rect) => ...
```
These take `Rect` by value and return `double` — the call site cannot mutate.
Issue: The reader of a C++ call site like `MajorStart(rect) = 0;` must locate the paired `SetMajorStart(ref rect, 0);` in the C# port. Every C++ assignment to the returned ref required a discovery+rename pass at every call site. This is correct given C#'s constraints (`Rect` is a value type, no mutable ref-returning extension methods possible on a field of `this`), but the divergence should be documented in the file (it already is, lines 9–13).
Suggested fix: Acceptable workaround. Verify each C++ assignment was mapped: `Major(point) = x`, `Minor(point) = y`, `Major(size) = x`, `Minor(size) = y`, `MajorSize(rect) = v`, `MinorSize(rect) = v`, `MajorStart(rect) = v`, `MinorStart(rect) = v`. The Uno port provides setters for `MajorSize`, `MinorSize`, `MajorStart`, `MinorStart` only (4 of 8). **Missing**: `SetMajor(ref Point)`, `SetMinor(ref Point)`, `SetMajor(ref Size)`, `SetMinor(ref Size)`. If any C++ call site does `Major(size) = v` for a `winrt::Size`, the port is incomplete.

#### OBM-3 (High) — Element types changed from `float` to `double`

C++ (OrientationBasedMeasures.cpp/h) consistently uses `float` (since `winrt::Size`, `winrt::Point`, `winrt::Rect` carry `float` Width/Height/X/Y).

C# (OrientationBasedMeasures.mux.cs):
```csharp
public static double Major(this OrientationBasedMeasures obm, Size size) => ...
```
.NET `Windows.Foundation.Size`, `Point`, `Rect` carry `double` fields, so widening from `float` to `double` is the correct mapping when reading them back. This is fine. However, `MinorMajorRect(float minor, float major, float minorSize, float majorSize)` still takes `float` parameters; the return is a `Rect` whose constructor accepts `double` — the `float` args silently widen. Inconsistent parameter type.
Suggested fix: Change `MinorMajorRect`/`MinorMajorPoint`/`MinorMajorSize` parameters to `double` for consistency with the rest of the file.

#### OBM-4 (Medium) — `ScrollOrientation` enum moved to separate file `ScrollOrientation.cs`, declared `internal`

C++ (OrientationBasedMeasures.h lines 6–10): `enum class ScrollOrientation { Vertical, Horizontal };` is co-located in the same header.
C# `ScrollOrientation.cs`:
```csharp
internal enum ScrollOrientation { Vertical, Horizontal }
```
Issue: Moving the enum to a dedicated file is fine. But `ScrollOrientation.cs` includes an unused `using System.Linq;` and predates the namespace-style block (it uses block-scoped `namespace { ... }` instead of file-scoped — verify against project style guide).
Suggested fix: Use file-scoped namespace, drop unused `using`.

#### OBM-5 (Medium) — `AddMinorStart` extension method has no C++ counterpart

C# (OrientationBasedMeasures.mux.cs lines 94–104):
```csharp
public static void AddMinorStart(this OrientationBasedMeasures obm, ref Rect rect, double increment)
{
    ...
}
```
C++ uses `MinorStart(rect) += increment;` at call sites (e.g., FlowLayoutAlgorithm). The port substitutes a dedicated helper. This is necessary because C# cannot return `ref` to a struct field. Acceptable but should be noted; the comment on line 93 documents this.
Suggested fix: None required. Confirm every C++ `MinorStart(rect) +=` was migrated to `obm.AddMinorStart(ref rect, …)`.

#### OBM-6 (Medium) — `GetScrollOrientation()`/`SetScrollOrientation(value)` pair vs. `ScrollOrientation` property

C++ (OrientationBasedMeasures.h lines 15–16):
```cpp
ScrollOrientation GetScrollOrientation() const { return m_orientation; }
void SetScrollOrientation(ScrollOrientation value) { m_orientation = value; }
```
C# uses `ScrollOrientation ScrollOrientation { get; set; }` on the interface. The "Foo() getter → Foo property" rule maps to a property. However, call sites in `StackLayout.mux.cs` etc. still call `GetScrollOrientation()` and `SetScrollOrientation(value)`. These methods are not defined on the interface in `OrientationBasedMeasures.cs` — they must exist as extension/instance methods elsewhere, or call sites need updating.
Suggested fix: Either add `GetScrollOrientation()`/`SetScrollOrientation()` extension methods that delegate to the property (port-friendly), or rewrite every call site to use the property. The current state of `OrientationBasedMeasures.cs` does not satisfy callers like `StackLayout.mux.cs:83` (`GetScrollOrientation()`) and `:364` (`SetScrollOrientation(scrollOrientation)`).

#### OBM-7 (Low) — `MajorEnd`/`MinorEnd` lack `Set` counterpart

C++ `MajorEnd`/`MinorEnd` return `float` by value (not ref) — they are not mutable. C# port mirrors this correctly with `double` return and no setter.
No issue.

#### OBM-8 (Low) — File header references `.cpp` and `.h` separately

`OrientationBasedMeasures.cs` is annotated `// MUX Reference OrientationBasedMeasures.h, commit 4b206bce3`. `OrientationBasedMeasures.mux.cs` is annotated `// MUX Reference OrientationBasedMeasures.cpp, commit 4b206bce3`. Correct.

#### OBM-9 (Low) — Comment "// MUST be default" on `ScrollOrientation.Vertical`

Only present in `ScrollOrientation.cs`. The C++ header does not mention this. The comment captures the implicit invariant from `m_orientation { ScrollOrientation::Vertical }`. Acceptable defensive comment.

#### OBM-10 (Info) — Static extension class is not in IDL

The C++ `OrientationBasedMeasures` is an implementation-detail base class (not in IDL). Hiding it as a C# internal interface + static helpers is acceptable; no public API impact.

#### OBM-11 (Info) — No destructor

C++ has none. C# has none. Correct.

---

## Cross-type observations

1. **Pervasive `#pragma region` → `// #pragma region` regression.** All four C++ regions (`ILayout`, `ILayoutProtected`, `INonVirtualizingLayoutOverrides`, `IVirtualizingLayoutOverrides`) were converted to inactive comments instead of C# `#region` directives. This loses IDE outlining and is fixable mechanically. Affects: `Layout.mux.cs`, `NonVirtualizingLayout.mux.cs`, `VirtualizingLayout.mux.cs`.

2. **Systematic `protected` → `protected internal` widening on overrideable layout methods.** `CreateDefaultItemTransitionProvider`, `InitializeForContextCore`, `UninitializeForContextCore`, `MeasureOverride`, `ArrangeOverride`, `OnItemsChangedCore`, `InvalidateMeasure` are all widened. This violates the user preference "match WinUI API surface rather than filtering/suppressing differences" and the porting rule "Private by default; widen only with IDL/Microsoft Learn/Generated evidence". The cumulative effect: any consumer of the Uno.UI assembly (not just derivers) can call these. Either:
   - Restore `protected` and route cross-class invocations through dedicated bridge methods (e.g., `Layout.MeasureCore(child, context, size)`), or
   - Document each widening with explicit `// Uno-specific: …` comments and a unified rationale.

3. **`__RP_Marker_ClassById` consistently commented without `TODO Uno:`.** Both `NonVirtualizingLayout` and `VirtualizingLayout` ctors silently drop the runtime profiler marker. Should be wrapped:
   ```csharp
   #if !HAS_UNO
       // TODO Uno: __RP_Marker_ClassById(...);
   #endif
   ```

4. **Foo() getter → Foo property rule applied inconsistently.** `IndexBasedLayoutOrientation` was converted to a property, but `LayoutId`, `LogItemIndexDbg()`, `LayoutAnchorIndexDbg()`, `LayoutAnchorOffsetDbg()`, `GetForcedIndexBasedLayoutOrientationDbg()`, `MajorEnd()`, `MinorEnd()` retain method form. (Some are justified by overload-with-setter pattern; others are not.)

5. **`OrientationBasedMeasures` interface adapter pattern is incomplete.** Missing `GetScrollOrientation()`/`SetScrollOrientation()` extension methods that callers reference; missing `SetMajor/Minor` for `Size` and `Point`; and inconsistent `float`/`double` parameter typing.

6. **`Layout.uno.cs` weak event additions are well-isolated.** The `WeakEventHelper` pattern correctly handles long-lived dictionary references. The `#if HAS_UNO` integration in `Layout.mux.cs` is acceptable but could be cleaner via a `partial void` hook.

7. **No destructors / finalizers**, consistent with rule 8. `auto_revoke` revokers are not used in these files (`Layout` only owns plain events). Rules 7 (revokers) and 8 (no finalizers) are observed.

---

## Conclusion

**Total findings by severity:** 0 Critical, 9 High, 10 Medium, 10 Low, 8 Info.

**Top priority issues** (must address before sign-off):
1. **L-1** — `Layout.LayoutId` is `internal` instead of `public`. Restores public API parity.
2. **NVL-1 / VL-1** — `*Override` and `*Core` virtuals widened to `protected internal`. Restore `protected virtual` to match WinUI IDL.
3. **OBM-6** — `OrientationBasedMeasures` interface lacks `GetScrollOrientation()`/`SetScrollOrientation()` methods used by `StackLayout`/`UniformGridLayout`/`FlowLayout` call sites. Either add helpers or rewrite call sites.
4. **L-2 / L-3 / L-4** — `InvalidateMeasure`, `CreateDefaultItemTransitionProvider`, and the anonymous-namespace helpers are unnecessarily widened. Tighten visibility.
5. **NVL-2 / VL-2** — Wrap dropped `__RP_Marker_ClassById` calls with `#if !HAS_UNO` + `TODO Uno:` per porting rule 6.

**Secondary priorities** (code-quality / consistency):
6. **Cross-type observation 1** — Restore real `#region` / `#endregion` directives (mechanical replace).
7. **OBM-2** — Audit `OrientationBasedMeasures` for missing `SetMajor`/`SetMinor` overloads on `Size`/`Point`.
8. **OBM-1** — Document or enforce `ScrollOrientation.Vertical` default.
9. **L-9** — Document Uno-specific weak-event integration inside `Layout.mux.cs`.

No functional/logic regressions were detected in the core method bodies — the algorithmic translations (switch-on-type, exception paths, event invocation order, `MeasureOverride`/`ArrangeOverride` defaults) are line-by-line faithful to the C++. The findings concentrate on API surface (visibility), porting-rule conformance (regions, TODO markers), and the OrientationBasedMeasures interface contract.
