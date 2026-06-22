# WASM-Skia DataGrid accessibility validation

External-runner validation for the Community Toolkit `DataGrid` ARIA grid pattern on the
**WASM Skia** backend (`Uno.UI.Runtime.Skia.WebAssembly.Browser`).

The Community Toolkit `DataGrid` is **not** referenced by this repository, so it is validated
against a small standalone Uno app that mirrors the client (kahua) usage, driven by an external
browser runner (`dump-semantics.cjs`, Playwright) that reads the semantic DOM overlay.

## The semantic DOM overlay

On WASM Skia the accessibility tree is a parallel DOM under `#uno-semantics-root`:

- each node is `<… id="uno-semantics-{handle}" role="…" aria-…>`,
- `AutomationProperties.AutomationId` is surfaced as the `xamlautomationid` attribute,
- the tree is built only after the in-app **"Enable accessibility"** button is clicked (or
  `FeatureConfiguration.AutomationPeer.AutoEnableAccessibility = true`). `EnableAccessibility()`
  retries for ~2 s waiting for `Window.RootElement`; in an interpreter-mode cold start that budget
  can expire before the window attaches, so the runner clicks the button **after** the app renders.

## Reproducing

A minimal app matching the client stack — Uno.Sdk `6.7.0-dev.62` (which resolves Uno.WinUI
`6.7.0-dev.211`) and `Uno.CommunityToolkit.WinUI.UI.Controls.DataGrid` `7.1.205`:

```bash
dotnet new unoapp -o DataGridA11yRepro -preset blank -tfm net9.0 -platforms wasm -renderer skia
```

- `global.json`: pin `Uno.Sdk` to `6.7.0-dev.62`.
- add `<PackageReference Include="Uno.CommunityToolkit.WinUI.UI.Controls.DataGrid" />` (`7.1.205`).
- `MainPage.xaml`: a `<toolkit:DataGrid>` with `AutoGenerateColumns="False"`,
  `CanUserSortColumns="True"`, `IsReadOnly="True"`, `SelectionMode="Extended"`, three
  `DataGridTextColumn`s, bound to ~500 rows so virtualization realizes/recycles.

Build, serve `bin/Release/net9.0-browserwasm/wwwroot` with `python3 -m http.server 8080`, then:

```bash
npm install --no-save playwright && npx playwright install chromium
URL=http://localhost:8080/ OUT=tree.json node dump-semantics.cjs
```

## Baseline (before fix) — observed, Uno.WinUI 6.7.0-dev.211

`roleHist: { grid: 1, row: 24, headeritem: 3, scrollbar: 1 }` — note `columnheader: 0, gridcell: 0`.

| Element | Observed | Problem |
|---|---|---|
| grid    | `role=grid`, `aria-rowcount=500`, `aria-colcount=3` | no `aria-multiselectable` |
| rows    | `role=row`, **every row `aria-rowindex=1`** | bogus uniform index |
| headers | **`role="headeritem"`** (not a valid ARIA role) | should be `columnheader`; no `aria-sort`/`aria-colindex` |
| cells   | **`role` absent** (rendered role-less) | should be `gridcell`; no `aria-rowindex`/`aria-colindex`/`aria-selected` |

Root cause: `AriaMapper.GetSemanticElementType` mapped `DataGrid`→Grid and `DataItem`→GridRow, but
had no mapping for the column header (`HeaderItem`) or the cell (`Custom`), so both fell through to
the role-less generic path and `CreateColumnHeaderElement`/`CreateGridCellElement` were dead code.

## Fix

WASM-Skia ARIA layer only (the Win32 UIA bridge already surfaces these because it is pattern-generic
and the toolkit peers implement the patterns — see below):

- `AriaMapper.GetSemanticElementType`: `HeaderItem → ColumnHeader`; any peer exposing the **GridItem**
  pattern → `GridCell` (cells report `Custom`, too generic to map by type).
- `SemanticElementFactory` + `SemanticElements.ts`:
  - grid emits `aria-multiselectable` from `ISelectionProvider.CanSelectMultiple`,
  - cells/rows emit `aria-selected` from `ISelectionItemProvider`,
  - column header emits `aria-colindex` (1-based, aligned with cells) and `aria-sort`,
  - row `aria-rowindex` is emitted only when actually known (no more uniform `1`); the per-row index
    is carried by each cell's `aria-rowindex` from `IGridItemProvider.Row`.

### Peer interfaces (reflected from `CommunityToolkit.WinUI.UI.Controls.DataGrid` 7.1.205)

- `DataGridAutomationPeer`: IGrid, ISelection, ITable, IScroll
- `DataGridCellAutomationPeer`: **IGridItem**, **ISelectionItem**, ITableItem, IInvoke
- `DataGridColumnHeaderAutomationPeer`: IInvoke, IScrollItem, ITransform — **no sort channel**
- `DataGridItemAutomationPeer` (row): ISelectionItem, ISelection

### aria-sort (#4) is upstream-blocked

`DataGridColumnHeaderAutomationPeer` exposes **no** sort state through any UIA pattern or property
(no `GetItemStatusCore`, no sort pattern). The fix adds the emission path, sourced from the generic
`AutomationPeer.GetItemStatus()` channel ("ascending"/"descending"), so an app — or an enhanced
toolkit peer — can light it up. Out of the box with WCT 7.1.205 `aria-sort` stays absent on both
backends until the toolkit peer is enhanced. Same root limitation applies to Desktop UIA.

## Validation evidence

- **Runtime (baseline):** observed via `dump-semantics.cjs` against the real WCT DataGrid on WASM
  Skia (Uno.WinUI 6.7.0-dev.211) — the table above.
- **Compile:** `Uno.UI.Runtime.Skia.WebAssembly.Browser` builds with 0 errors; the emitted
  `Uno.Runtime.Wasm.js` now contains `aria-sort`/`aria-multiselectable`/`aria-selected`.
- **Runtime (unit of the fix):** `Given_AccessibleDataGrid` asserts `GetSemanticElementType` returns
  `ColumnHeader`/`GridCell` for the mock peers (runs on Skia / CI).
- **Runtime (after, end-to-end):** re-run the runner against the app built on locally-built Uno.
  Override the cache with the fixed build, then rebuild the app:

  ```bash
  # in src/crosstargeting_override.props: UnoTargetFrameworkOverride=net10.0,
  #   UnoNugetOverrideVersion=6.7.0-dev.211
  dotnet build src/Uno.UI.Runtime.Skia.WebAssembly.Browser/Uno.UI.Runtime.Skia.WebAssembly.Browser.csproj \
    -c Release -p:UnoNugetOverrideVersion=6.7.0-dev.211
  # set the repro app to net10.0-browserwasm, clear obj/bin, rebuild, re-serve, re-run the runner
  ```

  Caveat: in this version the cache override replaces `Uno.UI.Tasks` with an unhashed name while the
  targets reference a content-hashed task assembly (`Uno.UI.Tasks.<hash>.dll`), which breaks the app
  build. Run the end-to-end on CI / a clean environment, or consume locally-`dotnet pack`ed packages
  via a local feed instead of the in-place cache override.
