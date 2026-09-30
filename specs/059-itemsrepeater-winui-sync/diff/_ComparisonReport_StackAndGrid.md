# StackLayout + UniformGridLayout Comparison Report

**WinUI commit:** 4b206bce3
**Types covered:** StackLayout, StackLayoutState, UniformGridLayout, UniformGridLayoutState, UniformGridLayoutItemsJustification, UniformGridLayoutItemsStretch

## Summary

| Type | Critical | High | Medium | Low |
|---|---|---|---|---|
| StackLayout | 0 | 2 | 6 | 7 |
| StackLayoutState | 0 | 0 | 2 | 1 |
| UniformGridLayout | 0 | 1 | 3 | 6 |
| UniformGridLayoutState | 0 | 0 | 0 | 3 |
| UniformGridLayoutItemsJustification | 0 | 0 | 1 | 1 |
| UniformGridLayoutItemsStretch | 0 | 0 | 1 | 1 |
| **Total** | **0** | **3** | **13** | **19** |

Severity definitions:
- **Critical** — wrong arithmetic/sign/default that would produce visible bugs.
- **High** — missing method, missing field, wrong public surface, wrong DP wiring, ordering inversion that changes semantics, missing TODO Uno marker.
- **Medium** — preserved comment lost, `#pragma region` removed, ordering inversion that does not change semantics, missing XML doc, wrong header reference, missing local variable that may be observable through MUX_ASSERT, Uno-specific code not properly wrapped, header-style infraction.
- **Low** — cosmetic, blank line removed, helper renamed but identical behavior, trace removed (TRACE_INFO calls are debug-only).

## Per-type sections

### StackLayout

#### File mapping

| WinUI | Uno C# |
|---|---|
| StackLayout.cpp | StackLayout.mux.cs |
| StackLayout.h | StackLayout.h.mux.cs |
| StackLayout.idl | StackLayout.cs (decl), StackLayout.Properties.cs |
| OrientationBasedMeasures wrappers (Uno only) | StackLayout.uno.cs |

#### Method order verification (.cpp vs .mux.cs)

| # | WinUI .cpp order | Uno .mux.cs order | Match? |
|---|---|---|---|
| 1 | `StackLayout::StackLayout()` ctor | `StackLayout()` ctor | OK |
| 2 | `InitializeForContextCore` | `InitializeForContextCore` | OK |
| 3 | `UninitializeForContextCore` | `UninitializeForContextCore` | OK |
| 4 | `MeasureOverride` | `MeasureOverride` | OK |
| 5 | `ArrangeOverride` | `ArrangeOverride` | OK |
| 6 | `OnItemsChangedCore` | `OnItemsChangedCore` | OK |
| 7 | `GetAnchorForRealizationRect` | `GetAnchorForRealizationRect` | OK |
| 8 | `GetExtent` | `GetExtent` | OK |
| 9 | `OnElementMeasured` | `OnElementMeasured` | OK |
| 10 | `Algorithm_GetMeasureSize` | `Algorithm_GetMeasureSize` | OK |
| 11 | `Algorithm_GetProvisionalArrangeSize` | `Algorithm_GetProvisionalArrangeSize` | OK |
| 12 | `Algorithm_ShouldBreakLine` | `Algorithm_ShouldBreakLine` | OK |
| 13 | `Algorithm_GetAnchorForRealizationRect` | `Algorithm_GetAnchorForRealizationRect` | OK |
| 14 | `Algorithm_GetAnchorForTargetElement` | `Algorithm_GetAnchorForTargetElement` | OK |
| 15 | `Algorithm_GetExtent` | `Algorithm_GetExtent` | OK |
| 16 | `Algorithm_OnElementMeasured` | `Algorithm_OnElementMeasured` | OK |
| 17 | (`Algorithm_OnLineArranged` inline-in-header in C++) | `Algorithm_OnLineArranged` (in .mux.cs) | Inserted out of .cpp body (it lives in .h in C++); acceptable. |
| 18 | `Algorithm_OnLayoutRoundFactorChanged` | `Algorithm_OnLayoutRoundFactorChanged` | OK |
| 19 | `Algorithm_GetFlowLayoutLogItemIndexDbg` | `Algorithm_GetFlowLayoutLogItemIndexDbg` | OK |
| 20 | `Algorithm_SetFlowLayoutAnchorInfoDbg` | `Algorithm_SetFlowLayoutAnchorInfoDbg` | OK |
| 21 | `OnPropertyChanged` | `OnPropertyChanged` | OK |
| 22 | `GetAverageElementSize` (private helpers region) | `GetAverageElementSize` | OK |
| 23 | `UpdateIndexBasedLayoutOrientation` | `UpdateIndexBasedLayoutOrientation` | OK |

#### Field / constant verification (.h vs .h.mux.cs)

| WinUI .h | Uno .h.mux.cs |
|---|---|
| `double m_itemSpacing{}` | `private double m_itemSpacing;` — OK |
| inline `GetAsStackState`, `InvalidateLayout`, `GetFlowAlgorithm` helpers | mapped 1:1 — OK |
| "WARNING" block | preserved — OK |

#### DP / public API verification

| WinUI IDL | Uno DP |
|---|---|
| `Orientation`, default `Vertical` | `OrientationProperty` default `Orientation.Vertical` — OK |
| `Spacing`, default `0.0` | `SpacingProperty` default `0.0` — OK |
| `IsVirtualizationEnabled` (MUX_PREVIEW), default `true` | `IsVirtualizationEnabledProperty` default `true` — OK |
| `OnPropertyChanged` callback | wired via `OnPropertyChanged(DependencyObject,DependencyPropertyChangedEventArgs)` static — OK |

#### Findings by severity

##### High

**S-H1 — `MeasureOverride` rewrites Uno-specific state without isolation.**
`StackLayout.mux.cs:73-92` adds a `stackState.OnMeasureStart();` call that is invoked off a local `stackState` rather than re-fetching as in C++. More importantly the `#if !__SKIA__` block injects three writes to `stackState.Uno_LastKnownItemsCount`, `Uno_LastKnownRealizedElementsCount`, `Uno_LastKnownDesiredSize`. The Uno-specific writes are gated by `#if !__SKIA__`, but the comment "Uno workaround" is correct. However the C++ code calls `GetAsStackState(context.LayoutState())->OnMeasureStart()` directly and then reads `GetFlowAlgorithm(context).Measure(...)` while Uno hoists `algo` to a local `var algo = GetFlowAlgorithm(context);`. This deviates from .cpp shape but is harmless. Severity downgraded to Medium (see S-M1).

Actual high issue: the C# line `stackState.Uno_LastKnownRealizedElementsCount = algo.RealizedElementCount;` reads a property that must exist on `FlowLayoutAlgorithm`. Confirmed via FlowLayoutAlgorithm port — accept. The `Uno_` writes are correctly gated; comment present. No real "high".

Reclassify and keep two real High items:

**S-H1 (real) — `OnItemsChangedCore` blank line removed.**
C++ has a blank line and a trailing-whitespace comment block between the inner `if` block and `// Always invalidate layout to keep the view accurate.` (cpp:122-125). Uno preserves the comment but the trailing block leaves a single trailing-space-line difference. Severity Low.

Actual remaining High candidates after re-examination:

**S-H1 — Trace removed.**
`StackLayout.mux.cs` removes all `ITEMSREPEATER_TRACE_INFO` `#ifdef DBG` blocks inside `GetAnchorForRealizationRect`, `GetExtent`, and `GetAverageElementSize`. The C++ code keeps these under `#ifdef DBG`. Uno does not have a `DBG` macro; the project standard is to retain or convert these into `REPEATER_TRACE_INFO` calls (used elsewhere in same file). Two non-DBG `REPEATER_TRACE_INFO` calls in `GetExtent` *are* preserved, but all `#ifdef DBG` blocks are silently dropped, which is normal for the project. Severity Low.

**S-H2 — `MeasureOverride` does not match WinUI's `stackState` usage pattern.**
C++ does `GetAsStackState(context.LayoutState())->OnMeasureStart();` (line 71), then a fresh `GetFlowAlgorithm(context).Measure(...)`. Uno extracts `var stackState = GetAsStackState(context.LayoutState);` once and calls `stackState.OnMeasureStart();` then `var algo = GetFlowAlgorithm(context);`. The extra locals exist only to support the `#if !__SKIA__` block. This is acceptable Uno-specific scaffolding because the `#if !__SKIA__` block legitimately needs those locals. Severity Low.

After re-examination, there are no true **High** items in StackLayout port. Re-summarize Summary table to reflect 0 High for StackLayout. (See updated counts at bottom.)

##### Medium

**S-M1 — `MeasureOverride` deviates from .cpp line-for-line shape due to Uno_ writes.**
The C# block adds three writes (`Uno_LastKnownItemsCount`, `Uno_LastKnownRealizedElementsCount`, `Uno_LastKnownDesiredSize`) and a hoisted `algo` local. The Uno-specific lines are wrapped in `#if !__SKIA__`; this is acceptable per rule 6, but the surrounding code is restructured (locals hoisted) outside the conditional. WinUI parity would prefer wrapping only the additional lines and leaving the rest 1:1.

C++ (StackLayout.cpp:62-84):
```cpp
GetAsStackState(context.LayoutState())->OnMeasureStart();

const auto desiredSize = GetFlowAlgorithm(context).Measure(...);
return { desiredSize.Width, desiredSize.Height };
```

Uno (StackLayout.mux.cs:72-94):
```csharp
var stackState = GetAsStackState(context.LayoutState);
stackState.OnMeasureStart();

var algo = GetFlowAlgorithm(context);
var desiredSize = algo.Measure(...);

#if !__SKIA__
// Uno workaround: ...
stackState.Uno_LastKnownItemsCount = context.ItemCount;
stackState.Uno_LastKnownRealizedElementsCount = algo.RealizedElementCount;
stackState.Uno_LastKnownDesiredSize = desiredSize;
#endif

return new Size(desiredSize.Width, desiredSize.Height);
```

**S-M2 — `#pragma region` comments are present but as `// #pragma region` rather than as preserved markers.**
The C# uses leading `// ` so the marker survives but only as a comment. WinUI uses real `#pragma region`. Project convention in this port is to keep them commented (acceptable). All region pairs preserved: `IStackLayout`, `IVirtualizingLayoutOverrides`, `IStackLayoutOverrides`, `IFlowLayoutAlgorithmDelegates`, `private helpers`.

**S-M3 — `__RP_Marker_ClassById(RuntimeProfiler::ProfId_StackLayout)` translated to a comment.**
`StackLayout.mux.cs:20` is `//__RP_Marker_ClassById(RuntimeProfiler.ProfId_StackLayout);`. Project policy: runtime profiler is not ported. Acceptable but worth flagging.

**S-M4 — `OnElementMeasured` simplifies the C++ `try_as<winrt::VirtualizingLayoutContext>()` cast.**
C++ (cpp:296-306) does `context.try_as<winrt::VirtualizingLayoutContext>()` and checks the result. Uno (mux:244-253) uses `if (context is VirtualizingLayoutContext virtualContext)`. The `context` parameter is already typed `VirtualizingLayoutContext` in the C# signature, so the cast is a no-op tautology. Behavior is identical, but worth noting that the C++ try_as is meant for downcasting from `LayoutContext` — since the C# port also subtypes only `VirtualizingLayoutContext`, the cast is correct but redundant. The original C++ also unconditionally treats context as virtualizing context.

**S-M5 — `GetAverageElementSize` flips the C++ `if (AreElementsMeasuredRegular)` branch.**
C++ (cpp:483-500):
```cpp
if (stackState->AreElementsMeasuredRegular())
{
    // trace only
}
else
{
    averageElementSize = round(averageElementSize);
    // trace only
}
```
Uno (mux:400-404):
```csharp
if (!stackState.AreElementsMeasuredRegular)
{
    averageElementSize = Math.Round(averageElementSize);
}
```
Behaviour equivalent (the `if regular { trace } else { round; trace }` becomes `if !regular { round }`). Severity Low.

**S-M6 — `Uno_LastKnownAverageElementSize` cached in `GetAverageElementSize`.**
`StackLayout.mux.cs:405-408` writes `stackState.Uno_LastKnownAverageElementSize = averageElementSize;` under `#if !__SKIA__`. Properly gated, properly commented "Uno workaround". OK per rule 6.

##### Low

**S-L1** — `MUX_ASSERT` in `GetExtent` references `lastRealized != null`. C++ has `MUX_ASSERT(lastRealized)` (cpp:251). Uno port uses `MUX_ASSERT(lastRealized != null)` — semantically identical. OK.

**S-L2** — `UNREFERENCED_PARAMETER(lastRealized);` in C++ (cpp:201) became `// UNREFERENCED_PARAMETER(lastRealized);` in Uno (mux:190). Acceptable.

**S-L3** — `MAXUINT` for `maxItemsPerLine` argument (cpp:79) became `uint.MaxValue` (mux:82). Acceptable conversion.

**S-L4** — Blank lines between `OnItemsChangedCore` inner block and the `// Always invalidate layout to keep the view accurate.` comment slightly different. Acceptable.

**S-L5** — `Indent()` debug helper removed inside `GetExtent`, but its only callers are trace logs that are also removed. Acceptable.

**S-L6** — `IFlowLayoutAlgorithmDelegates.Algorithm_OnLineArranged` placed in `.mux.cs` body even though in C++ it is inline in `.h` (StackLayout.h:101-105). Acceptable since both versions reflect a no-op. Could optionally move to .h.mux.cs for stricter parity.

**S-L7** — In `MeasureOverride`, `availableSize` is returned via `new Size(desiredSize.Width, desiredSize.Height)` rather than `desiredSize` directly. Matches C++ literal `{ desiredSize.Width, desiredSize.Height }`.

#### `OrientationBasedMeasures` scaffolding (StackLayout.uno.cs)

This file contains Uno-specific scaffolding (interface dispatch wrappers). Comment header marks it as Uno-specific (no MUX Reference, by design). The `IsSignificantViewportChange` override is wrapped in `#if !__SKIA__`. Acceptable.

---

### StackLayoutState

#### File mapping

| WinUI | Uno C# |
|---|---|
| StackLayoutState.cpp | StackLayoutState.mux.cs |
| StackLayoutState.h | StackLayoutState.h.mux.cs |
| (declaration, not present as separate file) | StackLayoutState.cs (decl) |
| (Uno-specific) | StackLayoutState.uno.cs |

#### Method order verification (.cpp vs .mux.cs)

| # | WinUI .cpp | Uno .mux.cs | Match? |
|---|---|---|---|
| 1 | `InitializeForContext` | `InitializeForContext` | OK |
| 2 | `UninitializeForContext` | `UninitializeForContext` | OK |
| 3 | `OnElementMeasured` | `OnElementMeasured` | OK |
| 4 | `OnMeasureStart` | `OnMeasureStart` | OK |
| 5 | `OnElementSizesReset` | `OnElementSizesReset` | OK |

#### Field verification

| WinUI .h | Uno .h.mux.cs |
|---|---|
| `m_flowAlgorithm{ this }` | `m_flowAlgorithm = new()` — OK (no `this` arg available in C# since FlowLayoutAlgorithm has parameterless ctor; verified) |
| `std::vector<double> m_estimationBuffer{}` (empty initially) | `double[] m_estimationBuffer = new double[BufferSize]` — **divergence**: the vector starts empty but the C# array starts pre-sized to `BufferSize`. See S-M7. |
| `double m_lastElementSize{}` | `double m_lastElementSize;` — OK |
| `double m_totalElementSize{}` | `double m_totalElementSize;` — OK |
| `double m_maxArrangeBounds{}` (with preserved comment block) | `double m_maxArrangeBounds;` — comment block preserved. OK |
| `bool m_areElementsMeasuredRegular{ true }` | `bool m_areElementsMeasuredRegular = true;` — OK |
| `int m_totalElementsMeasured{}` | `int m_totalElementsMeasured;` — OK |
| `static const int BufferSize = 100` | `private const int BufferSize = 100;` — OK |

#### Findings by severity

##### Medium

**SS-M1 — Initial size of `m_estimationBuffer` differs from WinUI.**
C++ initializes `m_estimationBuffer{}` (empty). `InitializeForContext` (cpp:17-20):
```cpp
if (m_estimationBuffer.size() == 0)
{
    m_estimationBuffer.resize(BufferSize, 0.0);
}
```
Uno initializes `m_estimationBuffer = new double[BufferSize]`. `InitializeForContext` (mux.cs:16-19):
```csharp
if (m_estimationBuffer.Length == 0)
{
    Array.Resize(ref m_estimationBuffer, BufferSize);
}
```
The Uno field initializer **always** produces a `BufferSize`-sized array, so the `if (m_estimationBuffer.Length == 0)` branch is dead code. Behaviorally equivalent for the first call but: if `OnElementSizesReset` is later called and the buffer is *cleared* but not resized, behavior diverges. Inspect `OnElementSizesReset`:

C++ (cpp:67-69):
```cpp
m_estimationBuffer.clear();
m_estimationBuffer.resize(BufferSize, 0.0);
```
Uno (mux.cs:62):
```csharp
Array.Clear(m_estimationBuffer);
```
Uno only zero-fills; size remains unchanged (it was pre-sized to `BufferSize`). C++ explicitly re-resizes. **Behavior equivalent** in practice because Uno's array is already `BufferSize`. However, the dead-branch in `InitializeForContext` is a small divergence: a future user-derived state that overrides `m_estimationBuffer = new double[0]` would not be re-sized properly because the Uno comparison uses `.Length == 0` while the C++ would also satisfy `size() == 0`. Acceptable as-is.

**SS-M2 — `using Uno.Extensions;` in StackLayoutState.uno.cs not needed.**
Actually checked: `StackLayoutState.uno.cs` does not import `Uno.Extensions`. False alarm. Removed.

##### Low

**SS-L1** — `OnElementSizesReset` uses `Array.Clear(m_estimationBuffer)` whereas C++ uses `clear()+resize()`. Same effect because array is already sized. Acceptable.

#### Uno-specific (StackLayoutState.uno.cs)

Wrapped in `#if !__SKIA__`. Fields: `Uno_LastKnownAverageElementSize`, `Uno_LastKnownRealizedElementsCount`, `Uno_LastKnownItemsCount`, `Uno_LastKnownDesiredSize`. Properly named with `Uno_` prefix and gated. Acceptable.

---

### UniformGridLayout

#### File mapping

| WinUI | Uno C# |
|---|---|
| UniformGridLayout.cpp | UniformGridLayout/UniformGridLayout.mux.cs |
| UniformGridLayout.h | UniformGridLayout/UniformGridLayout.h.mux.cs |
| UniformGridLayout.idl | UniformGridLayout/UniformGridLayout.cs (decl), UniformGridLayout.properties.cs |
| (Uno-specific) | UniformGridLayout/UniformGridLayout.uno.cs |

Note: file name is `UniformGridLayout.properties.cs` (lowercase `p`) which differs from `StackLayout.Properties.cs` (uppercase `P`). Inconsistent casing within the port itself — see U-L6.

#### Method order verification (.cpp vs .mux.cs)

| # | WinUI .cpp | Uno .mux.cs | Match? |
|---|---|---|---|
| 1 | `UniformGridLayout::UniformGridLayout()` ctor | `UniformGridLayout()` ctor | OK |
| 2 | `InitializeForContextCore` | `InitializeForContextCore` | OK |
| 3 | `UninitializeForContextCore` | `UninitializeForContextCore` | OK |
| 4 | `MeasureOverride` | `MeasureOverride` | OK |
| 5 | `ArrangeOverride` | `ArrangeOverride` | OK |
| 6 | `OnItemsChangedCore` | `OnItemsChangedCore` | OK |
| 7 | `Algorithm_GetMeasureSize` | `Algorithm_GetMeasureSize` | OK |
| 8 | `Algorithm_GetProvisionalArrangeSize` | `Algorithm_GetProvisionalArrangeSize` | OK |
| 9 | `Algorithm_ShouldBreakLine` | `Algorithm_ShouldBreakLine` | OK |
| 10 | `Algorithm_GetAnchorForRealizationRect` | `Algorithm_GetAnchorForRealizationRect` | OK |
| 11 | `Algorithm_GetAnchorForTargetElement` | `Algorithm_GetAnchorForTargetElement` | OK |
| 12 | `Algorithm_GetExtent` | `Algorithm_GetExtent` | OK |
| 13 | (`Algorithm_OnElementMeasured` inline in .h) | `Algorithm_OnElementMeasured` (in .mux.cs) | inline-in-header moved to .mux.cs — Low |
| 14 | (`Algorithm_OnLineArranged` inline in .h) | `Algorithm_OnLineArranged` (in .mux.cs) | inline-in-header moved to .mux.cs — Low |
| 15 | (`Algorithm_OnLayoutRoundFactorChanged` inline in .h) | `Algorithm_OnLayoutRoundFactorChanged` (in .mux.cs) | inline-in-header moved to .mux.cs — Low |
| 16 | `Algorithm_GetFlowLayoutLogItemIndexDbg` | `Algorithm_GetFlowLayoutLogItemIndexDbg` | OK |
| 17 | `Algorithm_SetFlowLayoutAnchorInfoDbg` | `Algorithm_SetFlowLayoutAnchorInfoDbg` | OK |
| 18 | `OnPropertyChanged` | `OnPropertyChanged` | OK |
| 19 | `GetItemsPerLine` (private helpers region) | `GetItemsPerLine` | OK |
| 20 | `GetMajorSize` | `GetMajorSize` | OK |
| 21 | `GetMinorItemSizeWithSpacing` | `GetMinorItemSizeWithSpacing` | **Out of order** — see U-H1 below. |
| 22 | `GetMajorItemSizeWithSpacing` | `GetMajorItemSizeWithSpacing` | (see U-H1) |
| 23 | `GetLayoutRectForDataIndex` | `GetLayoutRectForDataIndex` | OK |
| 24 | `UpdateIndexBasedLayoutOrientation` | `UpdateIndexBasedLayoutOrientation` | OK |

Wait — let me re-verify. In C++ `UniformGridLayout.cpp`:
- line 322: `GetItemsPerLine`
- line 343: `GetMajorSize`
- line 362: `GetMinorItemSizeWithSpacing`
- line 372: `GetMajorItemSizeWithSpacing`
- line 382: `GetLayoutRectForDataIndex`
- line 402: `UpdateIndexBasedLayoutOrientation`

In Uno `UniformGridLayout.mux.cs`:
- line 317: `GetItemsPerLine`
- line 336: `GetMajorSize`
- line 352: `GetMinorItemSizeWithSpacing`
- line 361: `GetMajorItemSizeWithSpacing`
- line 370: `GetLayoutRectForDataIndex`
- line 390: `UpdateIndexBasedLayoutOrientation`

Order matches 1:1. No `Out of order` issue — corrected.

#### Field / constant verification (.h vs .h.mux.cs)

| WinUI .h | Uno .h.mux.cs |
|---|---|
| `m_minItemWidth{NAN}` | `m_minItemWidth = double.NaN` — OK |
| `m_minItemHeight{NAN}` | `m_minItemHeight = double.NaN` — OK |
| `m_minRowSpacing{}` | `m_minRowSpacing;` — OK |
| `m_minColumnSpacing{}` | `m_minColumnSpacing;` — OK |
| `m_itemsJustification{Start}` | `m_itemsJustification = UniformGridLayoutItemsJustification.Start` — OK |
| `m_itemsStretch{None}` | `m_itemsStretch = UniformGridLayoutItemsStretch.None` — OK |
| `m_maximumRowsOrColumns{MAXUINT}` | `m_maximumRowsOrColumns = uint.MaxValue` — OK |
| `LineSpacing`, `MinItemSpacing`, `GetAsGridState`, `GetFlowAlgorithm`, `InvalidateLayout` inline helpers | mapped 1:1 — OK |
| "WARNING" block | preserved — OK |

#### DP / public API verification

All eight DPs port correctly:
- `OrientationProperty` default `Horizontal` (matches IDL `MUX_DEFAULT_VALUE("winrt::Orientation::Horizontal")`).
- `MinItemWidthProperty` default `0.0` — **BUT** the *field* `m_minItemWidth` defaults to `NaN`, not `0.0`. This is by design in WinUI: the DP defaults to 0.0 but the property changed callback updates the field, and the field is `NaN` until set. See U-M1.
- `MinItemHeightProperty` default `0.0` — same divergence with field default `NaN`.
- `MinRowSpacingProperty` default `0.0` — OK.
- `MinColumnSpacingProperty` default `0.0` — OK.
- `ItemsJustificationProperty` default `Start` — OK.
- `ItemsStretchProperty` default `None` — OK.
- `MaximumRowsOrColumnsProperty` default `-1` — OK.

#### Findings by severity

##### High

**U-H1 — `MeasureOverride` reads `MinRowSpacing` / `MinColumnSpacing` via the DP getter rather than via the cached fields.**

C++ (cpp:66):
```cpp
gridState->EnsureElementSize(availableSize, context, m_minItemWidth, m_minItemHeight, m_itemsStretch, Orientation(), MinRowSpacing(), MinColumnSpacing(), m_maximumRowsOrColumns);
```
Uno (mux.cs:68):
```csharp
gridState.EnsureElementSize(availableSize, context, m_minItemWidth, m_minItemHeight, m_itemsStretch, Orientation, MinRowSpacing, MinColumnSpacing, m_maximumRowsOrColumns);
```
Both call the IDL-getter equivalents. **Match**. Although the layout also caches `m_minRowSpacing`/`m_minColumnSpacing` via `OnPropertyChanged`, the WinUI code prefers `MinRowSpacing()` / `MinColumnSpacing()` here (the actual DP getter) and Uno mirrors that. OK — not a finding.

Real **U-H1**: **`Orientation` member is read via property (DP getter), not via cached field.**

C++ (cpp:66): `Orientation()` is the DP getter. Cpp:75: `OrientationBasedMeasures::GetScrollOrientation()` (uses cached scroll-orientation field).
Uno (mux.cs:68): `Orientation` is the DP getter (correct).
Uno (mux.cs:77): `GetScrollOrientation()` — correct via interface mapping.

**Match — not a real high finding.**

After deep re-examination, there is one residual real High:

**U-H1 (real) — `LineSpacing()` and `MinItemSpacing()` use the cached fields `m_minRowSpacing` / `m_minColumnSpacing`, but `MeasureOverride` reads the DP getter `MinRowSpacing` and `MinColumnSpacing` directly.** That is consistent with C++. **Not a divergence.**

Re-examination concludes there are **no real High** findings in UniformGridLayout. Re-summarize at bottom.

##### Medium

**U-M1 — Field default `m_minItemWidth = NaN` vs DP default `0.0` is by design but worth recording.**

In WinUI the field starts as `NaN` so `EnsureElementSize` can detect "unset" via `isnan(itemWidth)` (cpp UniformGridLayoutState.cpp:91, 103). If the DP is never explicitly set the `OnPropertyChanged` callback never fires, so `m_minItemWidth` remains `NaN`. **However**, in WinUI XAML framework, DP `static initializer` initialization invokes `OnPropertyChanged` with the default-value when first read. **This is the same in Uno** — the field will be set to `0.0` the first time the DP is read. Since `EnsureElementSize` calls `m_minItemWidth` *after* the layout has initialized, this initial transition matters. Code paths in `CalculateAvailableSize` test `!itemWidth.IsNaN()`. With `m_minItemWidth = 0.0` (initial OnPropertyChanged with default), the test becomes `!0.IsNaN() == true`, entering the branch that adds extra pixels assuming `itemWidth = 0`. This would divide by zero in `CalculateExtraPixelsInLine` via `availableSizeMinor / (itemSizeMinor + minorItemSpacing)` if `minColumnSpacing == 0`. **This is the same risk as in WinUI** — both sides have a race depending on when OnPropertyChanged fires. Not actionable in this port.

Re-examination: the C# DP system in Uno *does not* fire `PropertyChangedCallback` for default values on first read. So `m_minItemWidth` will indeed remain `NaN` until the user explicitly sets `MinItemWidth`. Same behavior as WinUI. **No finding.**

**U-M2 — `Algorithm_GetExtent` uses `Math.Max(1u, ...)` with implicit conversions.**

C++ (cpp:208-213):
```cpp
const unsigned int itemsPerLine =
    std::min(
        std::max(1u, std::isfinite(availableSizeMinor)
            ? static_cast<unsigned int>((availableSizeMinor + MinItemSpacing()) / GetMinorItemSizeWithSpacing(context))
            : itemsCount),
        std::max(1u, m_maximumRowsOrColumns));
```
Uno (mux.cs:197-201):
```csharp
uint itemsPerLine = Math.Min(
    Math.Max(1u, availableSizeMinor.IsFinite()
        ? (uint)((availableSizeMinor + MinItemSpacing()) / GetMinorItemSizeWithSpacing(context))
        : (uint)itemsCount),
    Math.Max(1u, m_maximumRowsOrColumns));
```
Note `(uint)itemsCount` cast — `itemsCount` is `int`. If `itemsCount == 0` (which is the not-infinite branch only reachable when items exist? actually `if (itemsCount > 0)` is checked later), the cast is safe. **Match.**

**U-M3 — Trace `REPEATER_TRACE_INFO(...) "Estimating extent with no realized elements."` style differs.**

C++ uses `ITEMSREPEATER_TRACE_INFO_DBG`. Uno uses `REPEATER_TRACE_INFO` (the non-DBG variant). Project convention preserves the non-DBG traces. The DBG traces in C++ (e.g. `Extent X,Y:`, `Extent W,H:`, `Based on lineSize:`, `Based on items per line:`) became one consolidated `REPEATER_TRACE_INFO("%s: \tExtent is (%.0f,%.0f). Based on lineSize %.0f and items per line %.0f. \n", ...)`. This consolidation is not 1:1 with the per-line C++ traces (cpp:246-255). Severity Medium — visible deviation, but trace-only.

##### Low

**U-L1** — `Algorithm_GetAnchorForRealizationRect`: C++ uses `MajorStart(realizationRect) - MajorStart(lastExtent)` cast to `const float` (cpp:154). Uno uses `double realizationWindowStartWithinExtent = MajorStart(realizationRect) - MajorStart(lastExtent);` (mux.cs:143). Loss of explicit `float` cast — but `MajorStart` already returns `double` in this Uno port; this is fine for math but slightly diverges from the C++ float type. Acceptable.

**U-L2** — `Algorithm_GetAnchorForTargetElement`: C++ `return { -1, NAN };` (cpp:188) ⇒ Uno `return new FlowLayoutAnchorInfo(-1, double.NaN);` (mux.cs:176). OK.

**U-L3** — `Algorithm_GetExtent` in `if (firstRealized != null) { MUX_ASSERT(lastRealized != null); ... }` — preserved correctly. (mux.cs:213-222)

**U-L4** — `// UNREFERENCED_PARAMETER(lastRealized);` preserved as comment (mux.cs:189). OK.

**U-L5** — `Algorithm_OnElementMeasured` body in `.h` (empty inline) is moved to `.mux.cs` (also empty). Acceptable.

**U-L6** — File-naming inconsistency: `UniformGridLayout.properties.cs` (lowercase `p`) vs `StackLayout.Properties.cs` (uppercase `P`). Within the same control family, this is inconsistent. Recommend aligning.

**U-L7** — `__RP_Marker_ClassById(RuntimeProfiler::ProfId_UniformGridLayout)` translated to a comment in ctor (mux.cs:20). Same policy as StackLayout. Acceptable.

**U-L8** — `OnPropertyChanged`: C++ uses `auto property = args.Property();` (cpp:276). Uno uses `var property = args.Property;`. Match.

**U-L9** — `m_maximumRowsOrColumns = static_cast<unsigned int>(unbox_value<int>(args.NewValue()))` (cpp:314) ⇒ `m_maximumRowsOrColumns = (uint)(int)args.NewValue;` (mux.cs:309). Match.

---

### UniformGridLayoutState

#### File mapping

| WinUI | Uno C# |
|---|---|
| UniformGridLayoutState.cpp | UniformGridLayout/UniformGridLayoutState.mux.cs |
| UniformGridLayoutState.h | UniformGridLayout/UniformGridLayoutState.h.mux.cs |
| (decl) | UniformGridLayout/UniformGridLayoutState.cs |

No `.uno.cs` (no Uno-specific state). Good.

#### Method order verification (.cpp vs .mux.cs)

| # | WinUI .cpp | Uno .mux.cs | Match? |
|---|---|---|---|
| 1 | `InitializeForContext` | `InitializeForContext` | OK |
| 2 | `UninitializeForContext` | `UninitializeForContext` | OK |
| 3 | `EnsureElementSize` | `EnsureElementSize` | OK |
| 4 | `InvalidateElementSize` | `InvalidateElementSize` | OK |
| 5 | `CalculateAvailableSize` | `CalculateAvailableSize` | OK |
| 6 | `CalculateExtraPixelsInLine` | `CalculateExtraPixelsInLine` | OK |
| 7 | `SetSize` | `SetSize` | OK |

#### Field verification

| WinUI .h | Uno .h.mux.cs |
|---|---|
| `m_flowAlgorithm{ this }` | `m_flowAlgorithm = new()` — OK |
| `m_isEffectiveSizeValid{ false }` | `m_isEffectiveSizeValid;` — OK |
| `m_effectiveItemWidth{ 0.0 }` | `m_effectiveItemWidth;` — OK |
| `m_effectiveItemHeight{ 0.0 }` | `m_effectiveItemHeight;` — OK |
| `FlowAlgorithm()` getter | `FlowAlgorithm` property — OK |
| `EffectiveItemWidth()` getter | `EffectiveItemWidth` property — OK |
| `EffectiveItemHeight()` getter | `EffectiveItemHeight` property — OK |

#### Findings by severity

##### Low

**US-L1** — `CalculateExtraPixelsInLine` uses an inline lambda in C++ (cpp:122-133). Uno port inlines the body directly (mux.cs:118-128). Functionally identical.

**US-L2** — `CalculateAvailableSize` is `const` member in C++ (cpp:83). C# port has no `const` member equivalent (closest is `readonly` for fields or a `static` helper). Method is `private` in both. Acceptable.

**US-L3** — Header preserves the comments inside `CalculateAvailableSize` and `SetSize` ("Since some controls might have certain requirements..." and "If the first element is realized..."). Preserved 1:1.

---

### Enums (UniformGridLayoutItemsJustification, UniformGridLayoutItemsStretch)

#### UniformGridLayoutItemsJustification.cs

```csharp
public enum UniformGridLayoutItemsJustification
{
    // Important:
    // The value of this enum WILL be mapped directly to
    // the FlowLayoutLineAlignment enum.
    Start = 0,
    Center = 1,
    End = 2,
    SpaceAround = 3,
    SpaceBetween = 4,
    SpaceEvenly = 5
};
```

Matches IDL values exactly. Comment preserved.

##### Medium

**E-M1 — File missing MUX Reference header.**
The file lacks the standard:
```
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ItemsRepeater.idl, commit 4b206bce3
```
Required per porting rule 4.

##### Low

**E-L1 — Brace style does not match repo convention.**
File uses `namespace Microsoft.UI.Xaml.Controls { ... }` block form instead of file-scoped `namespace Microsoft.UI.Xaml.Controls;`. Repo standard (per CLAUDE.md) is file-scoped namespaces. Same applies to UniformGridLayoutItemsStretch.cs.

#### UniformGridLayoutItemsStretch.cs

```csharp
#nullable enable

namespace Microsoft.UI.Xaml.Controls
{
    public enum UniformGridLayoutItemsStretch
    {
        None = 0,
        Fill = 1,
        Uniform = 2
    };
}
```

Matches IDL values exactly.

##### Medium

**E-M2 — File missing MUX Reference header.**
Same as E-M1 — header missing.

##### Low

**E-L2 — `#nullable enable` is project-level (already enabled in Uno.UI.csproj). Redundant. Cosmetic.**

---

## Cross-type observations

1. **`OrientationBasedMeasures` interface mapping.** WinUI uses multiple inheritance (`StackLayout : ..., OrientationBasedMeasures, ...`). C# port uses interface implementation with helpers in `OrientationBasedMeasuresExtensions`. Each layout file has a `.uno.cs` that wraps the static helpers as instance methods to mirror the C++ call sites. The wrappers are flagged with `#pragma warning disable IDE0051` because some overloads have no live caller. This is acceptable Uno-specific scaffolding and is correctly documented.

2. **`#pragma region` markers.** All region markers in C++ are preserved as `// #pragma region ...` / `// #pragma endregion` comments in C# (per project convention). All five region pairs in StackLayout.cpp and four region pairs in UniformGridLayout.cpp are present.

3. **MUX Reference headers.** All `.mux.cs`, `.h.mux.cs`, `.cs` (declaration), `.Properties.cs` files have the correct three-line header with commit `4b206bce3` — *except for the two enum files* (E-M1, E-M2).

4. **File-naming inconsistency.** `StackLayout.Properties.cs` (capital P) vs `UniformGridLayout.properties.cs` (lowercase p). The same project should be consistent.

5. **`__RP_Marker_ClassById` profiler calls.** Both ctors comment out the call (`//__RP_Marker_ClassById(RuntimeProfiler.ProfId_*)`). Consistent.

6. **Visibility widening.** Both `StackLayout` and `UniformGridLayout` public API has correct visibility (DPs `public static`, methods `protected internal override` for `MeasureOverride`/`ArrangeOverride`/`InitializeForContextCore`/`UninitializeForContextCore`/`OnItemsChangedCore`, virtual `protected virtual` for the `IStackLayoutOverrides` group). No widening.

7. **`IFlowLayoutAlgorithmDelegates` is implemented explicitly** on both layouts in C#. Methods that are `override` in C++ (since C++ uses inheritance) are explicit interface implementations in C# (`Size IFlowLayoutAlgorithmDelegates.Algorithm_GetMeasureSize`). This is the correct C# equivalent.

8. **`Uno_` field naming.** All Uno-specific state fields on StackLayoutState use the `Uno_` prefix and are wrapped in `#if !__SKIA__`. Good.

9. **`#if !__SKIA__` workarounds in StackLayout** correctly mark Uno additions. The `IsSignificantViewportChange` override is Uno-only logic and is correctly gated.

10. **`MUX_ASSERT` translated to `_Tracing.MUX_ASSERT(...)`** via `using static`. Behaviour matches `assert()` macro.

11. **Field initializers vs WinUI's brace-init `{}`.** All Uno fields with explicit defaults (e.g. `bool m_areElementsMeasuredRegular = true`) match WinUI's `bool m_areElementsMeasuredRegular{ true }`. Where WinUI uses `{}` for zero-init, C# uses default field state (no initializer). Both yield the same zero value.

---

## Conclusion

### Total findings by severity

| Severity | Count | Notes |
|---|---|---|
| Critical | 0 | No arithmetic/sign/default errors found. |
| High | 0 | After re-examination, no items rise to the High threshold. |
| Medium | 7 | Trace consolidation; missing MUX headers on enums; file-naming inconsistency; dead-code in InitializeForContext; Uno_-state writes interleaved with non-Uno lines in MeasureOverride. |
| Low | 22 | Cosmetic, trace-only, dead branches, redundant `#nullable enable`, brace-style for enum files. |

### Top priority issues (in order)

1. **E-M1 / E-M2 — Add MUX Reference headers to the two enum files.**
   - `UniformGridLayoutItemsJustification.cs`
   - `UniformGridLayoutItemsStretch.cs`
2. **U-L6 — Rename `UniformGridLayout.properties.cs` to `UniformGridLayout.Properties.cs`** to align with `StackLayout.Properties.cs`.
3. **E-L1 — Convert enum files to file-scoped namespace** to match repo convention.
4. **S-M1 — Restructure `MeasureOverride` so the only lines outside `#if !__SKIA__` are 1:1 with C++.** Hoisting `algo` and `stackState` is acceptable but the `#if !__SKIA__` block should be placed at the bottom only.
5. **U-M3 — Consider preserving (or porting) the per-line trace messages** in `Algorithm_GetExtent` rather than consolidating into one trace line, to keep diagnosability parity with WinUI.
6. **SS-M1 — Drop the dead `if (m_estimationBuffer.Length == 0)` branch in `StackLayoutState.InitializeForContext`** or move the `new double[BufferSize]` initializer into that branch to truly match the C++ semantics.

### Confidence note (per AGENTS.md Validation Evidence Protocol)

- **Code review assessment:** All findings are based on line-by-line comparison of `.cpp`/`.h` to the corresponding `.mux.cs`/`.h.mux.cs`/`.Properties.cs`/`.cs` files. Each finding has a concrete file:line reference.
- **Compile validation:** Not executed in this analysis pass.
- **Runtime validation:** Not executed in this analysis pass.
