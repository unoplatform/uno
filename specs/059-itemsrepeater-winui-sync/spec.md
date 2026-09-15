# ItemsRepeater WinUI sync — parity analysis

Reference material for the `ItemsRepeater` / layout sync against WinUI commit `4b206bce3`
(`controls/dev/Repeater/` in the WinUI sources).

These documents were produced while porting and are kept as long-lived parity notes so a future
re-sync starts from a known baseline instead of re-deriving it. They live here rather than under
`src/Uno.UI/UI/Xaml/Controls/Repeater/` so that grepping the control's source directory returns
code, not prose.

## Contents — `diff/`

| Document | Purpose |
|---|---|
| `StructuralDiff.md` | File- and type-level mapping between the Uno port and the WinUI sources: what exists on each side, renames, and Uno-only helpers. |
| `_ComparisonReport_*.md` | Per-area member-by-member comparison. Raw analysis output; treat as a snapshot of the sync, not as a maintained contract. |
| `_DistilledDiff*.md` | The findings worth acting on, distilled from the comparison reports — confirmed behavioural divergences and deliberate deviations. |

## Caveats

- The reports describe the tree **as it was at the time of the sync**. They are not regenerated on
  every change and will drift; re-run the comparison rather than trusting a stale line.
- A "deviation" recorded here is not automatically a bug. Several are deliberate — WinRT-vs-CLR
  interface contracts, `IPanel` versus `Panel`, and the partial-class split the port uses in place
  of C++ header/implementation pairs.
