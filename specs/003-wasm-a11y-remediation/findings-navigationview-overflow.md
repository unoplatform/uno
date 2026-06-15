# Finding: NavigationView pane items are unreachable to AT on Skia/WASM (overflow split)

**Date**: 2026-06-07
**Branch**: `003-wasm-a11y-remediation`
**How found**: Building the A11yInspector multi-section sample (a `NavigationView` shell, one
scenario page per section) and driving the inspector headlessly. Could not reach non-default
sections over CDP because their nav items were absent from the accessibility tree.

> **STATUS — FOLDED into 003**: research §9; requirements FR-031 (virtualized registration +
> backfill at AOM build) and FR-032 (don't expose Collapsed/hidden subtrees); tasks T057–T059.
> The original width-measurement hypothesis is **REFUTED** (by code review *and* the A11y
> Inspector run); the corrected, runtime-confirmed root cause replaces it below.
>
> **Evidence level**: runtime-observed (A11y Inspector over CDP) + code review by inspection.

## Symptom (runtime-observed)

A `NavigationView` with `PaneDisplayMode="Left" IsPaneOpen="True" IsPaneToggleButtonVisible="False"`
renders, but in the Skia semantic DOM (`#uno-semantics-root`) **only the selected menu item is
exposed**. The other `NavigationViewItem`s are not present as accessible elements; a **"More"
overflow button** appears instead. This persists at both 800×600 and 1500×1000 headless window
sizes. Semantic-DOM dump over CDP showed `role=navigation` (the pane) + `More` + `Close Navigation`
but no per-item nodes for the unselected sections.

Net effect: **a screen-reader / rotor user cannot reach most navigation destinations** in the
default state — they are hidden behind an overflow menu that itself isn't surfaced as menu items.

## Root cause (CORRECTED — width hypothesis refuted)

The original "top-nav overflow split runs due to under-sized width" hypothesis is **wrong**:
`IsTopNavigationView()` is exactly `PaneDisplayMode == Top` (`NavigationView.cs:4285`), so in
`Left` mode the entire overflow machinery (`MeasureOverride`→`HandleTopNavigationMeasureOverride`
→`ShrinkTopNavigationSize`/`MoveItemsOutOfPrimaryList`, :1505-1523/:3852-3917/:4088-4119) is
unreachable — no width can trigger it. The inspector confirms the items are *absent*, not parked
in an open overflow flyout. Two real causes instead:

1. **(A) Virtualized items not registered at AOM build.** Left-nav items live in an
   `ItemsRepeater` (`MenuItemsHost`). Both tree walks skip `ItemsRepeater`
   (`WebAssemblyAccessibility.cs:342`, :1176-1179); items are surfaced only via
   `TryRegisterVirtualizedContainer`→`ElementPrepared` (:408-434), which is wired **only** from
   `OnChildAdded` (:308, gated `!_isCreatingAOM`). `CreateAOM`/`BuildSemanticsTreeRecursive`
   (`_isCreatingAOM=true`, :713-720) never registers the region, so items realized before enable
   are never surfaced and get no later `ElementPrepared`. Generalizes to any pre-populated
   `ItemsRepeater`/`ListViewBase`. → **FR-031 / T057**.
2. **(B) Collapsed/hidden subtree exposed to AT.** Runtime-confirmed: the "More" node is
   `x:Name="TopNavOverflowButton"`, whose `TopNavGrid` is `Collapsed` in non-Top mode
   (`NavigationView.cs:4716-4730`), yet the inspector shows `rendered? hidden` → `in AT tree?
   exposed` (`ignored=False`). The walker / `IsSemanticElement` does not prune
   `Visibility=Collapsed` subtrees. → **FR-032 / T058**.

**Classification**: a11y-mapping-layer (NOT a NavigationView control-layer bug — `Left`/`Expanded`
mode is honored correctly). Same family as the existing virtualized-parity gap (003 §5/§8.2, T055).

## Follow-up (folded into 003)

- ~~Probe `availableSize.Width` / fix width plumbing~~ — **DROPPED**: targets the top-nav overflow
  path, which is unreachable in Left mode (hypothesis refuted).
- [ ] **T057 (FR-031)**: register virtualized containers + backfill already-realized items at AOM
      build time (not only via the live `OnChildAdded` path).
- [ ] **T058 (FR-032)**: prune `Visibility=Collapsed`/hidden subtrees from the semantic walk.
- [ ] **T059 (SC-011)**: re-verify with the A11y Inspector (all destinations reachable; no
      hidden-but-exposed phantom).

## Workaround in the inspector sample

Because nav items aren't reliably reachable to AT (and CDP synthetic clicks destabilize the agent's
activation), the sample does **not** rely on driving the NavigationView for headless per-section
inspection. Instead it reads a section key at startup from `localStorage['a11y.section']` (query
`?section=` as a best-effort fallback; note the Uno WASM bootstrap normalizes the document URL, so
query/hash do **not** survive boot — `localStorage` does). Headless flow: boot once, set
`localStorage['a11y.section']`, reload, then read the snapshot. See
`samples/A11yInspector.Sample/.../MainPage.xaml.cs` in the A11yInspector repo.
