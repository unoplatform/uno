# Structural Diff: Uno Repeater port vs WinUI MUX

**WinUI commit:** 4b206bce3
**Uno root:** `src/Uno.UI/UI/Xaml/Controls/Repeater/` (+ `UniformGridLayout/` subfolder)
**WinUI root:** `src/controls/dev/Repeater/` (top-level only, excluding `APITests/`, `InteractionTests/`, `TestUI/`)

## Summary

- **Total Uno type roots:** 89
- **Total WinUI type roots:** 81
- **Missing in Uno:** 11 (test hooks: 2, newer features: 6, factories: 3, header-only: 1, helper: 1, trace-only: 1; note: `SelectedItems` is present but lacks header-style split. Counted unique types.)
- **Missing in WinUI (Uno-only):** 8 (Uno helpers: 4, partial-split shims: 4)
- **Renames / mappings:** 2 (Downlevel suffix dropped on `ElementFactoryGetArgs`, `ElementFactoryRecycleArgs`)

Notes on counting:
- Type roots are computed by stripping suffixes: `.h.mux.cs`, `.mux.cs`, `.uno.cs`, `.Properties.cs`, `.Templates.cs`, `.UIKit.cs`, `.Impl.cs`, `.ChildrenCollection.cs`, `.partial.h.mux.cs`, `.partial.mux.cs` and on the WinUI side: `.cpp`, `.h`, `.idl`, `.common.cpp`, `.common.h`.
- `CachedVisualTreeHelpers` is **not Uno-only** — it lives in `ItemsRepeater.common.h` in WinUI; Uno splits it into its own file.
- `IFlowLayoutAlgorithmDelegates` is now present in Uno (`IFlowLayoutAlgorithmDelegates.cs`), so it is no longer a missing item even though WinUI ships it header-only.

---

## 1. Types present in WinUI but missing in Uno

| WinUI type | Source file(s) | Classification | Recommended action |
|---|---|---|---|
| `LinedFlowLayout` | `LinedFlowLayout.cpp/.h` | Newer WinUI feature | Port (high priority — public API surface used by `ItemsRepeater` consumers) |
| `LinedFlowLayoutItemAspectRatios` | `LinedFlowLayoutItemAspectRatios.cpp/.h` | Newer WinUI feature (support for LinedFlowLayout) | Port together with LinedFlowLayout |
| `LinedFlowLayoutItemCollectionTransitionProvider` | `LinedFlowLayoutItemCollectionTransitionProvider.cpp/.h` | Newer WinUI feature | Port together with LinedFlowLayout |
| `LinedFlowLayoutItemsInfoRequestedEventArgs` | `LinedFlowLayoutItemsInfoRequestedEventArgs.cpp/.h` | Newer WinUI feature | Port together with LinedFlowLayout |
| `LinedFlowLayoutTrace` | `LinedFlowLayoutTrace.h` | Tracing header for LinedFlowLayout | Skip — Uno uses `_Tracing` helper instead |
| `LayoutsTestHooksLinedFlowLayoutInvalidatedEventArgs` | `LayoutsTestHooksLinedFlowLayoutInvalidatedEventArgs.cpp/.h` | Test hook event args (LinedFlowLayout) | Port together with LinedFlowLayout |
| `LayoutsTestHooksLinedFlowLayoutItemLockedEventArgs` | `LayoutsTestHooksLinedFlowLayoutItemLockedEventArgs.cpp/.h` | Test hook event args (LinedFlowLayout) | Port together with LinedFlowLayout |
| `QPCTimer` | `QPCTimer.cpp/.h` | Native QueryPerformanceCounter helper | Skip — Uno can use `System.Diagnostics.Stopwatch` directly where needed (currently no consumers) |
| `LayoutsTestHooksFactory` | `LayoutsTestHooksFactory.cpp/.h` | WinRT activation factory | Skip — not needed in C# managed model |
| `RepeaterTestHooksFactory` | `RepeaterTestHooksFactory.cpp/.h` | WinRT activation factory | Skip — not needed in C# managed model |
| `RecyclePoolFactory` | `RecyclePoolFactory.cpp/.h` | WinRT activation factory | Skip — `DependencyProperty.RegisterAttached` covers this in Uno |
| `ItemsSourceViewFactory` | `ItemsSourceViewFactory.cpp/.h` | WinRT activation factory creating `InspectingDataSource` | Already covered by `ItemsSourceView` constructor logic in Uno; no action |
| `ItemsRepeaterTrace` | `ItemsRepeaterTrace.h` | Tracing header (macros) | Skip — Uno uses `_Tracing` helper |

### Subtotals for missing-in-Uno

- Test hooks (LinedFlow-only event args): 2
- Newer WinUI features (LinedFlowLayout family): 4
- Header-only / trace-only includes: 2 (`LinedFlowLayoutTrace.h`, `ItemsRepeaterTrace.h`)
- Factories (not relevant in managed model): 4 (`LayoutsTestHooksFactory`, `RepeaterTestHooksFactory`, `RecyclePoolFactory`, `ItemsSourceViewFactory`)
- Other (timer utility): 1 (`QPCTimer`)

---

## 2. Types present in Uno but no longer in WinUI

| Uno type | Source file(s) | Classification | Justification |
|---|---|---|---|
| `_Tracing` | `_Tracing.cs` | Uno-only helper | Replaces WinUI `REPEATER_TRACE_INFO` macro / `ItemsRepeaterTrace.h`. Required for trace-call ports. |
| `CachedVisualTreeHelpers` | `CachedVisualTreeHelpers.cs` | File split | Defined in WinUI inside `ItemsRepeater.common.h`. Uno splits into own file. Not Uno-only conceptually. |
| `ListAdapter` | `ListAdapter.cs` | Uno-only helper | Provides `IList<T>` view/cast helpers used by `ItemsSourceView` / `InspectingDataSource` to mirror WinRT `IBindableVector` semantics. |
| `IElementFactoryShim` | `IElementFactoryShim.cs` | Uno-only interface shim | WinUI uses `IElementFactory` directly (WinRT). Uno needs an internal shim to model the same surface internally for `ItemTemplateWrapper`. |
| `IPanel` | `IPanel.cs` | Uno-only interface shim | Replaces WinUI's C++ `DeriveFromPanelHelper_base` mixin — internal-only. Documented in source. |
| `ItemsSourceView.Impl` | `ItemsSourceView.Impl.cs` | File split | `ItemsSourceView` partial — bridges `InspectingDataSource` logic into the public API; one large file split for editorial reasons. |
| `VirtualLayoutContextAdapter.ChildrenCollection` | `VirtualLayoutContextAdapter.ChildrenCollection.cs` | File split | Nested `ChildrenCollection` class extracted to its own partial. In WinUI it's a local class inside `VirtualLayoutContextAdapter.cpp`. |
| `IFlowLayoutAlgorithmDelegates` | `IFlowLayoutAlgorithmDelegates.cs` | Conceptual rename | WinUI's `IFlowLayoutAlgorithmDelegates.h` declares this as a pure-virtual C++ interface. Uno expresses it as a managed interface in its own .cs (still 1:1 mapping). |

### Subtotals for missing-in-WinUI

- Uno-only helpers: 4 (`_Tracing`, `ListAdapter`, `IElementFactoryShim`, `IPanel`)
- Partial-split shims of WinUI types: 4 (`CachedVisualTreeHelpers`, `ItemsSourceView.Impl`, `VirtualLayoutContextAdapter.ChildrenCollection`, `IFlowLayoutAlgorithmDelegates`)
- Genuinely deprecated upstream: 0

---

## 3. Renames / mappings

| WinUI name | Uno name | Reason | Verified |
|---|---|---|---|
| `ElementFactoryGetArgsDownlevel` | `ElementFactoryGetArgs` | "Downlevel" suffix dropped — Uno targets the WinUI public type name directly (WinUI's downlevel class was a workaround to expose `Microsoft.UI.Xaml.ElementFactoryGetArgs` on legacy platforms). | OK (verified via `ElementFactoryGetArgsDownlevel.h` comment block) |
| `ElementFactoryRecycleArgsDownlevel` | `ElementFactoryRecycleArgs` | Same reason as above. | OK |
| `IFlowLayoutAlgorithmDelegates` (C++ pure-virtual interface) | `IFlowLayoutAlgorithmDelegates` (managed C# interface) | Same name, different language semantics. Behaves identically. | OK |

Inline-type mappings (no file split in WinUI, own file in Uno — listed for completeness):

| WinUI source | Uno file | Notes |
|---|---|---|
| `FlowLayoutLineAlignment` (declared in `ItemsRepeater.idl`) | `FlowLayoutLineAlignment.cs` | enum |
| `ItemCollectionTransitionOperation` (idl) | `ItemCollectionTransitionOperation.cs` | enum |
| `ItemCollectionTransitionTriggers` (idl) | `ItemCollectionTransitionTriggers.cs` | flags enum |
| `ElementRealizationOptions` (idl) | `ElementRealizationOptions.cs` | flags enum |
| `FlowLayoutAnchorInfo` (idl) | `FlowLayoutAnchorInfo.cs` | struct |
| `ScrollOrientation` (idl / private) | `ScrollOrientation.cs` | internal enum |
| `SelectTemplateEventArgs` (idl) | `SelectTemplateEventArgs.cs` | sealed class |

---

## 4. File split layout audit

The `winui-port` rule expects each WinUI `.cpp/.h` pair to map to three Uno files:
- `Foo.h.mux.cs` — header-equivalent (fields, decl, ctor signatures)
- `Foo.mux.cs` — implementation methods (1:1 with cpp)
- `Foo.cs` — Uno additions / glue

For pure-data types (enums/structs declared only in idl), a monolithic `.cs` is acceptable.

| WinUI pair | Required Uno files | Actual Uno files | Status |
|---|---|---|---|
| `BuildTreeScheduler.cpp/.h` | `.cs` + `.h.mux.cs` + `.mux.cs` | all 3 | OK |
| `ChildrenInTabFocusOrderIterable.cpp/.h` | all 3 | all 3 | OK |
| `CustomProperty.cpp/.h` | all 3 | all 3 | OK |
| `ElementFactory.cpp/.h` | all 3 | only `.cs` (monolithic) | Monolithic |
| `ElementFactoryGetArgsDownlevel.cpp/.h` | all 3 | only `.cs` (`ElementFactoryGetArgs.cs`) | Monolithic (small type) |
| `ElementFactoryRecycleArgsDownlevel.cpp/.h` | all 3 | only `.cs` (`ElementFactoryRecycleArgs.cs`) | Monolithic (small type) |
| `ElementManager.cpp/.h` | all 3 | only `.cs` (monolithic) | Monolithic — large file, consider splitting |
| `FlowLayout.cpp/.h` | all 3 | all 3 + `.uno.cs` + `.Properties.cs` | OK |
| `FlowLayoutAlgorithm.cpp/.h` | all 3 | all 3 + `.uno.cs` | OK |
| `FlowLayoutState.cpp/.h` | all 3 | all 3 + `.uno.cs` | OK |
| `IFlowLayoutAlgorithmDelegates.h` (header-only) | `.cs` only | `.cs` | OK |
| `IndexPath.cpp/.h` | all 3 | only `.cs` (monolithic) | Monolithic |
| `IndexRange.cpp/.h` | all 3 | only `.cs` (monolithic) | Monolithic (small type, acceptable) |
| `InspectingDataSource.cpp/.h` | all 3 | only `.cs` (monolithic) | Monolithic — large file, consider splitting |
| `ItemCollectionTransition.cpp/.h` | all 3 | all 3 | OK |
| `ItemCollectionTransitionCompletedEventArgs.cpp/.h` | all 3 | all 3 | OK |
| `ItemCollectionTransitionProgress.cpp/.h` | all 3 | all 3 | OK |
| `ItemCollectionTransitionProvider.cpp/.h` | all 3 | all 3 + `.Properties.cs` | OK |
| `ItemsRepeater.cpp/.h` + `.common.cpp/.h` | all 3 | all 3 + `.Properties.cs` + `.Templates.cs` + `.UIKit.cs` + `.uno.cs` | OK (most thorough split in tree) |
| `ItemsRepeaterElementClearingEventArgs.cpp/.h` | all 3 | only `.cs` (monolithic) | Monolithic (event args, acceptable) |
| `ItemsRepeaterElementIndexChangedEventArgs.cpp/.h` | all 3 | only `.cs` (monolithic) | Monolithic (event args, acceptable) |
| `ItemsRepeaterElementPreparedEventArgs.cpp/.h` | all 3 | only `.cs` (monolithic) | Monolithic (event args, acceptable) |
| `ItemsRepeaterScrollHost.cpp/.h` | all 3 | all 3 | OK |
| `ItemsSourceView.cpp/.h` | all 3 | `.cs` + `.Impl.cs` (no `.h.mux.cs` / `.mux.cs`) | Non-standard split — uses `.Impl.cs` instead of `.mux.cs` |
| `ItemTemplateWrapper.cpp/.h` | all 3 | only `.cs` (monolithic) | Monolithic |
| `Layout.cpp/.h` | all 3 | all 3 + `.uno.cs` | OK |
| `LayoutContext.cpp/.h` | all 3 | only `.cs` (monolithic) | Monolithic |
| `LayoutContextAdapter.cpp/.h` | all 3 | only `.cs` (monolithic) | Monolithic |
| `LayoutsTestHooks.cpp/.h/.idl` | all 3 | only `.cs` (monolithic) | Monolithic |
| `NonVirtualizingLayout.cpp/.h` | all 3 | `.cs` + `.mux.cs` (no `.h.mux.cs`) | Partial split — missing `.h.mux.cs` |
| `NonvirtualizingLayoutContext.cpp/.h` | all 3 | only `.cs` (monolithic) | Monolithic |
| `OrientationBasedMeasures.cpp/.h` | all 3 | `.cs` + `.mux.cs` (no `.h.mux.cs`) | Partial split — missing `.h.mux.cs` |
| `Phaser.cpp/.h` | all 3 | all 3 | OK |
| `RecyclePool.cpp/.h` | all 3 | all 3 + `.Properties.cs` | OK |
| `RecyclingElementFactory.cpp/.h` | all 3 | all 3 + `.Properties.cs` | OK |
| `RepeaterAutomationPeer.cpp/.h/.idl` | all 3 | `.cs` + `.mux.cs` (no `.h.mux.cs`) | Partial split — missing `.h.mux.cs` |
| `RepeaterLayoutContext.cpp/.h` | all 3 | only `.cs` (monolithic) | Monolithic |
| `RepeaterTestHooks.cpp/.h/.idl` | all 3 | only `.cs` (monolithic) | Monolithic |
| `SelectedItems.h` (header-only) | `.cs` only | `.cs` | OK |
| `SelectionModel.cpp/.h` | all 3 | all 3 + `.Properties.cs` | OK |
| `SelectionModelChildrenRequestedEventArgs.cpp/.h` | all 3 | only `.cs` (monolithic) | Monolithic (event args) |
| `SelectionModelSelectionChangedEventArgs.cpp/.h` | all 3 | only `.cs` (monolithic) | Monolithic (event args) |
| `SelectionNode.cpp/.h` | all 3 | all 3 | OK |
| `SelectionTreeHelper.cpp/.h` | all 3 | all 3 | OK |
| `SelectTemplateEventArgs.cpp/.h` | all 3 | only `.cs` (monolithic) | Monolithic (event args) |
| `StackLayout.cpp/.h` | all 3 | all 3 + `.Properties.cs` + `.uno.cs` | OK |
| `StackLayoutState.cpp/.h` | all 3 | all 3 + `.uno.cs` | OK |
| `TransitionManager.cpp/.h` | all 3 | `.h.mux.cs` + `.mux.cs` + `.uno.cs` (no `.cs`) | Non-standard — has all but the "glue" `.cs`. May be intentional. |
| `UniformGridLayout.cpp/.h` | all 3 | all 3 + `.properties.cs` + `.uno.cs` | OK |
| `UniformGridLayoutState.cpp/.h` | all 3 | all 3 | OK |
| `UniqueIdElementPool.cpp/.h` | all 3 | only `.cs` (monolithic) | Monolithic |
| `ViewManager.cpp/.h` | all 3 | all 3 | OK |
| `ViewportManager.h` (header-only base) | `.cs` only | `.cs` | OK |
| `ViewportManagerDownlevel.cpp/.h` | all 3 | all 3 | OK |
| `ViewportManagerWithPlatformFeatures.cpp/.h` | all 3 | all 3 + `.uno.cs` | OK |
| `VirtualizationInfo.cpp/.h` | all 3 | all 3 | OK |
| `VirtualizingLayout.cpp/.h` | all 3 | `.cs` + `.mux.cs` + `.uno.cs` (no `.h.mux.cs`) | Partial split — missing `.h.mux.cs` |
| `VirtualizingLayoutContext.cpp/.h` | all 3 | only `.cs` (monolithic) | Monolithic |
| `VirtualLayoutContextAdapter.cpp/.h` | all 3 | `.cs` + `.ChildrenCollection.cs` (no `.h.mux.cs` / `.mux.cs`) | Monolithic + nested-type split |

### Split-layout audit summary

- **Fully split (.cs + .h.mux.cs + .mux.cs)**: ~22 types
- **Monolithic (.cs only)**: ~24 types
  - Of these, ~9 are small event-args / data types where monolithic is acceptable.
  - ~15 are larger types where splitting would improve diff readability against WinUI:
    `ElementFactory`, `ElementManager`, `IndexPath`, `InspectingDataSource`, `ItemTemplateWrapper`, `LayoutContext`, `LayoutContextAdapter`, `LayoutsTestHooks`, `NonvirtualizingLayoutContext`, `RepeaterLayoutContext`, `RepeaterTestHooks`, `UniqueIdElementPool`, `VirtualizingLayoutContext`, `VirtualLayoutContextAdapter`.
- **Partial split (.cs + .mux.cs only, missing .h.mux.cs)**: 4 types
  - `NonVirtualizingLayout`, `OrientationBasedMeasures`, `RepeaterAutomationPeer`, `VirtualizingLayout`.
- **Non-standard splits**:
  - `ItemsSourceView` uses `.Impl.cs` instead of `.mux.cs`.
  - `TransitionManager` has `.h.mux.cs` + `.mux.cs` + `.uno.cs` but no plain `.cs` glue file.

---

## 5. Recommendations

### Priority 1 — Newer WinUI features to port

1. **`LinedFlowLayout` family** (5 types + 2 test-hook event-args)
   - `LinedFlowLayout`
   - `LinedFlowLayoutItemAspectRatios`
   - `LinedFlowLayoutItemCollectionTransitionProvider`
   - `LinedFlowLayoutItemsInfoRequestedEventArgs`
   - `LayoutsTestHooksLinedFlowLayoutInvalidatedEventArgs`
   - `LayoutsTestHooksLinedFlowLayoutItemLockedEventArgs`
   - This is the largest functional gap in the port.

### Priority 2 — Structural alignment with winui-port rules

Add `.h.mux.cs` + `.mux.cs` splits for the four currently partial types:
- `NonVirtualizingLayout`
- `OrientationBasedMeasures`
- `RepeaterAutomationPeer`
- `VirtualizingLayout`

Convert `ItemsSourceView.Impl.cs` → `ItemsSourceView.mux.cs` (+ `ItemsSourceView.h.mux.cs`) to match the standard split — `Impl.cs` is the only of its kind in the folder and breaks the convention.

Add the missing glue `.cs` to `TransitionManager`, even if empty, for consistency.

### Priority 3 — Optional cleanup (low value)

Splitting the 15 still-monolithic non-trivial types would aid future syncs but is not blocking. Recommend tackling on-demand when next syncing each type from upstream.

### Priority 4 — Uno-only types justification check

All eight Uno-only types are justified:
- `_Tracing`, `ListAdapter`, `IElementFactoryShim`, `IPanel` are legitimate Uno-only shims documented in source.
- `CachedVisualTreeHelpers`, `IFlowLayoutAlgorithmDelegates`, `ItemsSourceView.Impl`, `VirtualLayoutContextAdapter.ChildrenCollection` are file-split shims of WinUI logic (not true Uno-only).

### Items intentionally NOT ported (no action needed)

- WinRT activation factories (`*Factory.cpp/.h`) — managed C# does not need these.
- WinUI trace headers (`ItemsRepeaterTrace.h`, `LinedFlowLayoutTrace.h`) — replaced by `_Tracing` helper.
- `QPCTimer` — no current consumers in the C# port; use `System.Diagnostics.Stopwatch` if/when needed.
