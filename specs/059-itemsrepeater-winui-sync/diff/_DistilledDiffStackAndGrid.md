# Distilled diff: StackLayout + UniformGridLayout

**Source report:** _ComparisonReport_StackAndGrid.md
**WinUI commit:** 4b206bce3

## TL;DR

Cleanest group in the Repeater port. Source report tallies 0 Critical / 0 High / 7 Medium / 22 Low. After re-verification against the actual `.mux.cs`, `.h.mux.cs`, and `.Properties.cs` files (and the WinUI `.cpp`/`.h`/`.idl`), every Medium and Low finding reduces to style, header, file-naming, or trace-only deltas. No behavioural, arithmetic, DP-wiring, API-surface, or visibility regressions were confirmed. The `#if !__SKIA__` Uno workarounds in `StackLayout.MeasureOverride`, `GetAverageElementSize`, and `StackLayoutState.uno.cs` are correctly scoped and do not change Skia (the WinUI-aligned baseline) behaviour.

## Confirmed behavioural / correctness issues

None confirmed.

Verifications performed:
- `StackLayout.MeasureOverride` (mux.cs:65-95) — math is 1:1 with WinUI. Hoisted `stackState`/`algo` locals are required to feed the `#if !__SKIA__` block; on Skia they remain pure local hoisting with no observable effect.
- `StackLayout.GetAverageElementSize` (mux.cs:400-410) — the inverted-branch refactor (`if (!AreElementsMeasuredRegular) { Round(...) }`) is semantically equivalent to the WinUI `if regular { trace } else { round; trace }` pair; only the rounding behaviour is functional, traces are debug-only.
- `StackLayoutState.InitializeForContext` dead-branch — Uno's `m_estimationBuffer` is field-initialised to `new double[BufferSize]` so the `Length == 0` check is dead, but the runtime buffer size is identical to WinUI's `resize(BufferSize, 0.0)`. No behavioural difference.
- `UniformGridLayout.MeasureOverride` (mux.cs:68) — `MinRowSpacing` / `MinColumnSpacing` are read via the DP getter, matching WinUI cpp:66.
- `UniformGridLayout.Algorithm_GetExtent` (mux.cs:197-201) — `Math.Min(Math.Max(1u, ...), Math.Max(1u, m_maximumRowsOrColumns))` matches WinUI cpp:208-213 including the `uint` cast semantics.

## Missing functionality

None.

All 23 StackLayout entries, all 7 UniformGridLayoutState entries, and all UniformGridLayout method-order entries map 1:1 with WinUI source order. `IFlowLayoutAlgorithmDelegates` is implemented explicitly on both layouts (the correct C# equivalent of the C++ override-via-inheritance).

## Visibility / API surface

None.

- **StackLayout DPs** (`Orientation` default `Vertical`, `Spacing` default `0.0`, `IsVirtualizationEnabled` default `true`) — all wired via `OnPropertyChanged` static callback, defaults match IDL.
- **UniformGridLayout DPs** (all 8) — defaults verified against IDL: `Orientation=Horizontal`, `MinItemWidth/Height=0.0`, `MinRowSpacing/MinColumnSpacing=0.0`, `ItemsJustification=Start`, `ItemsStretch=None`, `MaximumRowsOrColumns=-1`. All wired via the same static callback.
- Method visibilities (`protected internal override` for `Measure/ArrangeOverride/InitializeForContextCore/UninitializeForContextCore/OnItemsChangedCore`; `protected virtual` for the `IStackLayoutOverrides` group) are correct.
- Enums `UniformGridLayoutItemsJustification` and `UniformGridLayoutItemsStretch` have correct values (matching IDL) and the "mapped directly to FlowLayoutLineAlignment" comment is preserved.

## Lifecycle / leak risk

None.

`StackLayoutState`/`UniformGridLayoutState` `InitializeForContext`/`UninitializeForContext` pairs match WinUI. No `SerialDisposable` or revoker patterns are required in this group (no event subscriptions). The `Uno_LastKnown*` fields are plain value-type caches on the state object and share the state object's lifetime.

## Dropped (rejected from source report)

- **S-H1 (trace removal under `#ifdef DBG`)** — debug-only trace, project standard drops these. Style.
- **S-H2 (hoisted `stackState`/`algo` locals)** — needed by `#if !__SKIA__` block, no behavioural impact on Skia.
- **S-M1 (`MeasureOverride` shape vs C++ line-for-line)** — locals hoisted to support gated workaround, behaviourally equivalent. Style preference.
- **S-M2 (`#pragma region` written as `// #pragma region`)** — project convention; markers preserved.
- **S-M3 (`__RP_Marker_ClassById` commented out)** — runtime profiler intentionally not ported.
- **S-M4 (`try_as<VirtualizingLayoutContext>` collapsed to `is` check)** — parameter is already statically typed; behaviour identical.
- **S-M5 (`GetAverageElementSize` if-branch inverted)** — only the `Round` path is functional, traces are debug-only. Behaviour-preserving refactor.
- **S-M6 (`Uno_LastKnownAverageElementSize` write)** — verified properly gated under `#if !__SKIA__`, comment present.
- **S-L1-S-L7** — `MUX_ASSERT(x != null)` syntax, commented `UNREFERENCED_PARAMETER`, `MAXUINT` -> `uint.MaxValue`, blank-line whitespace, `Indent()` removal, inline-header method moved to `.mux.cs` body, `new Size(...)` ctor form. All cosmetic.
- **SS-M1 (dead `if (m_estimationBuffer.Length == 0)` branch)** — pre-sized field initializer makes the branch dead but doesn't change behaviour; `OnElementSizesReset` is also behaviourally equivalent (`Array.Clear` on an already-`BufferSize` array == `clear()+resize(BufferSize, 0.0)`).
- **SS-M2 (`using Uno.Extensions;`)** — author corrected to false alarm in source report.
- **SS-L1** — same as SS-M1, dead-branch redundancy.
- **U-H1** — recanted in source report after re-examination; `MinRowSpacing()`/`MinColumnSpacing()`/`Orientation` calls match WinUI.
- **U-M1 (field default `NaN` vs DP default `0.0`)** — author confirmed both WinUI and Uno DP systems have identical "callback not fired for default value" behaviour. Not a divergence.
- **U-M2 (`Math.Max(1u, ...)` casts)** — author confirmed match.
- **U-M3 (`Algorithm_GetExtent` trace consolidation)** — explicitly excluded per task instructions (trace-only, informational).
- **U-L1 (loss of explicit `(float)` cast)** — `MajorStart` returns `double` in Uno; cast was lossless precision-narrowing in C++.
- **U-L2-U-L5, U-L7-U-L9** — `NAN` -> `double.NaN`, preserved comment, inline-header relocation, profiler comment, `auto property = args.Property()` -> `var property = args.Property`, `unbox_value<int>` -> direct cast. All cosmetic/syntactic.
- **U-L6 (`UniformGridLayout.properties.cs` lowercase `p` vs `StackLayout.Properties.cs`)** — file-name casing. Explicitly excluded.
- **E-M1, E-M2 (missing MUX Reference header on enum files)** — header convention, no behavioural impact. Explicitly excluded.
- **E-L1 (block-form namespace on enum files)** — explicitly excluded.
- **E-L2 (redundant `#nullable enable`)** — cosmetic.
- **Cross-type observations 1-11** — all style/convention notes, no functional impact.
