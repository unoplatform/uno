# Distilled diff: FlowLayout family

**Source report:** _ComparisonReport_FlowLayout.md
**WinUI commit:** 4b206bce3

## TL;DR

The port is structurally faithful — method order, fields, and the core `Measure`/`Arrange`/`Generate`/`PerformLineAlignment` math are line-for-line accurate, and no off-by-one, sign, or divide-order bugs were found. The actionable items are narrow:

1. `FlowLayoutState.InitializeForContext` contains a dead `Array.Resize` branch because the backing arrays are sized at field init (will never matter, but signals intent drift).
2. `Algorithm_SetFlowLayoutAnchorInfoDbg` is called on every `GetAnchorIndex` pass in release — a debug-only hook leaked into the production hot path.
3. Two `Math.Round` sites (`LayoutRound` and `GetAverageLineInfo`) use banker's rounding where WinUI uses round-half-away-from-zero. Real but only differs at exact `.5` boundaries.

Everything else flagged in the source report is style/trace parity and was rejected.

## Confirmed behavioural / correctness issues

### 1. `Math.Round` vs C++ `round` in `LayoutRound`

**File:** `FlowLayoutAlgorithm.mux.cs:588-597` (and `FlowLayout.mux.cs:419` for `GetAverageLineInfo`)

```csharp
// Uno
return new Rect(
    Math.Round(value.X * m_layoutRoundFactor) / m_layoutRoundFactor,
    Math.Round(value.Y * m_layoutRoundFactor) / m_layoutRoundFactor,
    Math.Round(value.Width * m_layoutRoundFactor) / m_layoutRoundFactor,
    Math.Round(value.Height * m_layoutRoundFactor) / m_layoutRoundFactor);
```

```cpp
// WinUI
static_cast<float>(round(value.X * m_layoutRoundFactor) / m_layoutRoundFactor),
```

**Why it matters:** `Math.Round` defaults to `MidpointRounding.ToEven` (banker's rounding); C `round` is round-half-away-from-zero. On exact `.5` boundaries the two produce different pixel-aligned results. This is the extent rect that drives final layout origin and size, so a `.5px` divergence is observable for content authored to land on half-pixel boundaries with non-integer rasterization scale.

**Fix:** Use `Math.Round(x, MidpointRounding.AwayFromZero)` at all four call sites in `LayoutRound` and at `FlowLayout.mux.cs:419` in `GetAverageLineInfo`.

### 2. Debug-only delegate invoked on every measure pass

**File:** `FlowLayoutAlgorithm.mux.cs:319`

```csharp
m_algorithmCallbacks.Algorithm_SetFlowLayoutAnchorInfoDbg(anchorIndex, Major(anchorPosition));
```

WinUI guards this call with `#ifdef DBG`. The Uno port invokes it unconditionally inside `GetAnchorIndex` — one extra virtual dispatch per measure pass per FlowLayout. The implementation in `FlowLayout.mux.cs:359` forwards to `SetLayoutAnchorInfoDbg` on the base `Layout` type. Not a correctness bug, but a release-build performance/behaviour widening of a clearly debug-intended hook.

**Fix:** Wrap the call site (and the two interface methods in `IFlowLayoutAlgorithmDelegates.cs:50-52`, plus the explicit-interface stubs at `FlowLayout.mux.cs:357-360`) in `#if DEBUG`. Confirm the base-class `LogItemIndexDbg`/`SetLayoutAnchorInfoDbg` virtuals are also `#if DEBUG`; if they exist in non-debug builds, this finding downgrades to "harmless extra call" but should still be gated.

### 3. Dead lazy-init branch in `FlowLayoutState.InitializeForContext`

**File:** `FlowLayoutState.mux.cs:17-21` paired with `FlowLayoutState.h.mux.cs:23-24`

```csharp
// .h.mux.cs
private double[] m_lineSizeEstimationBuffer = new double[BufferSize];
private double[] m_itemsPerLineEstimationBuffer = new double[BufferSize];

// .mux.cs
if (m_lineSizeEstimationBuffer.Length == 0)
{
    Array.Resize(ref m_lineSizeEstimationBuffer, BufferSize);
    Array.Resize(ref m_itemsPerLineEstimationBuffer, BufferSize);
}
```

The buffers are field-initialised to size `BufferSize`, so the `Length == 0` branch is unreachable. WinUI starts with empty vectors and only allocates inside `InitializeForContext`. Net runtime effect is identical (the buffer is the right size either way), but the code reads as if the port author expected lazy init and the reader is misled.

**Fix:** Either (a) match WinUI semantics — change the field initialisers to `Array.Empty<double>()` so the resize block becomes the actual allocation site; or (b) delete the dead `if` block. Option (a) preserves the C++ allocation timing.

## Missing functionality

None. All method overrides, callbacks, and helpers from `FlowLayout.cpp`/`FlowLayoutAlgorithm.cpp`/`FlowLayoutState.cpp` have C# counterparts. `GetElementIfRealized` is `internal` in C# vs `public` in C++, but `FlowLayoutAlgorithm` itself is internal in Uno, so the effective surface is unchanged — see next section.

## Visibility / API surface

### `GetElementIfRealized` widened differently

**File:** `FlowLayoutAlgorithm` (whole type is `internal`)

C++ declares `GetElementIfRealized` `public:`. Uno declares it `internal`. Because the containing `FlowLayoutAlgorithm` type is itself `internal` in the Uno port (and not on the IDL surface), the effective external visibility is the same. Not actionable.

## Lifecycle / leak risk

None found. `FlowLayoutState` does not hold disposable references, `FlowLayoutAlgorithm` no longer carries the tracker-ref owner that the C++ version uses for GC bookkeeping, and there are no event subscriptions in this family that would require unsubscription on `UninitializeForContext`.

## Dropped (rejected from source report)

- **F1-H3 `(double)args.NewValue` will throw on boxed int** — `LineSpacing`/`MinItemSpacing` DPs are registered with `typeof(double)` (`FlowLayout.Properties.cs:46-50, 65-69`); the DP framework coerces or rejects mismatched types before the callback sees them. Rejected.
- **F1-H2 trace-logging verbosity** — Trace-only, no behavioural impact.
- **F1-H4 `Math.Round` in `GetAverageLineInfo`** — Real but folded into finding #1 above.
- **F2-H2/H3/H4/H5 various semantic-equivalent translations** — Source report itself concludes "OK".
- **F2-H7 / F2-M8 dropped `MUX_ASSERT(LayoutOrigin == m_lastExtent)`** — Pure DBG assert, no silent-failure path; both code paths still set the value. Optional `#if DEBUG` port at author discretion.
- **F1-H1 / F4-M1 DBG members exposed in interface** — Already covered by finding #2 above (the call site is what matters; an unused interface member is harmless).
- **F3-H1 algorithm constructed without owner** — By-design, owner only feeds tracker_ref bookkeeping that Uno doesn't need.
- **F1-L8 `InvalidOperationException` vs `hresult_error(E_FAIL)`** — Sibling layouts (`StackLayout`) use the same `InvalidOperationException`; consistent with port convention.
- **F2-M5/M6/M7/M9/M11/M12 trace/ETW parity gaps** — Telemetry only, explicitly excluded.
- **F1-M1/M2/M3, F2-M2/M3/M4/M10/M13/M14, F2-L1..L13, F5-*, F6-*** — Style, file placement, header text, comments, unused usings, parameter modifiers, var/auto — all excluded.
- **F5-M1 `IEquatable<FlowLayoutAnchorInfo>` added in Uno** — Extra surface, no parity regression for callers expecting the WinUI shape.
