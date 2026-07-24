# Distilled structural diff: Uno Repeater port vs WinUI MUX

**Source report:** StructuralDiff.md
**WinUI commit:** 4b206bce3

## TL;DR

The only user-facing functional gap is the **LinedFlowLayout family** (5 public types + 2 test-hook event-args). Everything else flagged in the structural diff is either a justified Uno shim, a managed-model rename, or a cosmetic split-layout preference — except `OrientationBasedMeasures`, which silently dropped the WinUI base-class storage/inline math into an interface with only the orientation property, leaving the geometry helpers as Uno-only extensions rather than 1:1 ported instance members.

## Missing user-facing WinUI features in Uno

### 1. LinedFlowLayout family (high priority)

A complete, shipping WinUI layout that lays items out in lines (think Windows 11 Photos / People app gallery). It is a public `VirtualizingLayout` subclass with its own transition provider and supporting types. Currently entirely absent from the Uno port.

Source files in WinUI (`src/controls/dev/Repeater/`):

| WinUI type | Role |
|---|---|
| `LinedFlowLayout` (`LinedFlowLayout.cpp/.h`) | Public `VirtualizingLayout` subclass — items laid out into justified/stretched lines |
| `LinedFlowLayoutItemAspectRatios` (`LinedFlowLayoutItemAspectRatios.cpp/.h`) | Internal cache of per-item aspect ratios used to pack lines |
| `LinedFlowLayoutItemCollectionTransitionProvider` (`LinedFlowLayoutItemCollectionTransitionProvider.cpp/.h`) | Default transition provider returned by `CreateDefaultItemTransitionProvider` |
| `LinedFlowLayoutItemsInfoRequestedEventArgs` (`LinedFlowLayoutItemsInfoRequestedEventArgs.cpp/.h`) | Args for the `ItemsInfoRequested` event letting hosts provide aspect ratios up front |
| `LayoutsTestHooksLinedFlowLayoutInvalidatedEventArgs` | Test-hook event args for invalidation observation |
| `LayoutsTestHooksLinedFlowLayoutItemLockedEventArgs` | Test-hook event args for item lock observation |

**Consumers / why this matters:** `LinedFlowLayout` is part of the public Microsoft.UI.Xaml.Controls surface used by `ItemsRepeater` consumers (gallery-style apps). Apps relying on it cannot run on Uno today; this is the single largest functional gap in the port.

**Recommended port priority:** P1 — port the whole family together. The test-hook event-args can be added when the test hooks for them are needed; the other 4 types are required.

## Uno-only types worth scrutiny

Only one Uno-only type has a real semantic divergence from WinUI; the rest are justified shims.

- **`OrientationBasedMeasures` (`OrientationBasedMeasures.cs` + `OrientationBasedMeasures.mux.cs`)** — In WinUI this is a non-polymorphic helper *base class* with `m_orientation` storage and inline `Major/Minor/MajorSize/...` methods. The Uno port turned it into an interface exposing only `ScrollOrientation`, and moved the geometry math into a separate `OrientationBasedMeasuresExtensions` plus per-class `{Class}.OrientationBasedMeasures.cs` shims. This is documented in the source and the public API surface is unchanged, but the structural deviation is wide enough that future syncs will be harder. Flag for review (not a defect — see comment in file).
- `_Tracing.cs` — replacement for `REPEATER_TRACE_INFO` macro. Justified, no divergence. Skip.
- `ListAdapter.cs` — `IList`/`IList<T>` adapters mirroring WinRT `IBindableVector` semantics for `ItemsSourceView` / `InspectingDataSource`. Justified, no divergence. Skip.
- `IElementFactoryShim.cs` — internal interface for `ItemTemplateWrapper`; mirrors WinRT `IElementFactory`. Justified. Skip.
- `IPanel.cs` — internal interface replacing WinUI `DeriveFromPanelHelper_base` mixin used to expose protected `Children`. Justified. Skip.
- `CachedVisualTreeHelpers.cs`, `ItemsSourceView.Impl.cs`, `VirtualLayoutContextAdapter.ChildrenCollection.cs`, `IFlowLayoutAlgorithmDelegates.cs` — pure file-split shims of WinUI logic (not conceptually Uno-only). No divergence.

## Renames affecting consumers

- `ElementFactoryGetArgsDownlevel` → `ElementFactoryGetArgs`
- `ElementFactoryRecycleArgsDownlevel` → `ElementFactoryRecycleArgs`

**Impact:** None — verified by grepping the full Uno tree. The only hits for the `Downlevel` suffix are inside the structural-audit markdown files (`_ComparisonReport_FactoriesAndSources.md` etc.); no production C# code references the old names. The WinUI public type is `Microsoft.UI.Xaml.Controls.ElementFactoryGetArgs` / `…RecycleArgs`, which is exactly what Uno exposes. The `Downlevel` C++ files in WinUI exist only to bridge the WinUI internal C++ class to that public WinRT name on legacy platforms; the rename is correct.

No action required.

## Partial-split files genuinely missing (.h content lost)

After spot-checking the four "partial split" candidates against the WinUI headers, none of them lose real content:

| Uno type | WinUI `.h` content | Verdict |
|---|---|---|
| `NonVirtualizingLayout` | No fields, no inline methods — only virtual method declarations already represented in C# via `virtual` keywords on the partial. | Style-only; not load-bearing. |
| `VirtualizingLayout` | Same as above. | Style-only. |
| `RepeaterAutomationPeer` | No fields; one private method declaration (`GetElement`). Already covered as a private method in `RepeaterAutomationPeer.mux.cs`. | Style-only. |
| `OrientationBasedMeasures` | Has `m_orientation` field and ~12 inline accessor methods. | **Content was reshaped, not lost** — see "Uno-only types worth scrutiny" above. Listed there, not here. |

**Conclusion:** No `.h.mux.cs` shim is required to recover hidden content. The four "missing" splits are layout preference only.

## Intentionally omitted (and why)

- **WinRT activation factories** (`LayoutsTestHooksFactory`, `RepeaterTestHooksFactory`, `RecyclePoolFactory`, `ItemsSourceViewFactory`) — N/A in managed C#; covered by normal type instantiation and `DependencyProperty.RegisterAttached`.
- **Trace headers** (`ItemsRepeaterTrace.h`, `LinedFlowLayoutTrace.h`) — replaced by `_Tracing.cs`.
- **`QPCTimer`** — `System.Diagnostics.Stopwatch` is the C# equivalent; no consumers exist in the Uno port yet, so no shim needed.
- **Repeater test-hook event args, generally** — lower priority for API parity. Two LinedFlowLayout-related ones are listed in the LinedFlowLayout port group above because they belong with that feature; standalone test-hook types are not blocking parity.
- **Monolithic-vs-split layout for small event-args types** — no behavioural impact; split on-demand during next sync.

## Recommendations

1. **P1 — Port the LinedFlowLayout family** (5 public types + 2 LFL-bound test-hook event-args). This is the only user-visible feature gap and the largest functional divergence from WinUI in the Repeater area.
2. **P2 — Review `OrientationBasedMeasures` reshaping.** Decide whether to keep the current interface + extensions model (cheap, already documented) or migrate to a 1:1 helper class with stored `_orientation` plus instance methods. Either is defensible, but the choice should be deliberate before the next WinUI sync touches the affected algorithms.
3. **P3 — Optional consistency cleanup.** Adding `.h.mux.cs` for `NonVirtualizingLayout`, `VirtualizingLayout`, `RepeaterAutomationPeer`, converting `ItemsSourceView.Impl.cs` → `ItemsSourceView.mux.cs`, and adding an empty glue `TransitionManager.cs` would improve diff readability against WinUI. Low value; do on-demand when next syncing each type.
