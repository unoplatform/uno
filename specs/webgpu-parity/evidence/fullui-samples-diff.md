# Full-UI command-stream diff across 40 real SamplesApp pages — neutral vs ramez (2026-08-10)

Method: a local-only harness in BOTH apps (`UNO_WEBGPU_SAMPLE_TRACE=N`) navigates to each of the first N samples
(sorted by full name), lets the real render loop present the settled page, and dumps that frame's WebGPU command
stream (`WebGpuTrace.Dump` / `WebGpuCmdTrace.Dump`) labelled per sample. Both run headless on lavapipe, MSAA=1,
best combo. 40 samples captured in each; all 40 names matched.

## Headline

**Neutral emits 1.59× the GPU draws of ramez across the 40 pages (9589 vs 6033); per-page avg 1.87×, up to 3.17×.**
So on real UIs neutral is NOT yet at parity — it issues materially more draws. The diff pinpoints exactly why.

## Root cause #1 (dominant): no analytic rounded-rectangle fill pipeline

Ramez has an **`rrect`** primitive — every rectangle, square OR rounded, is ONE analytic quad (1264 draws total;
that's why ramez shows zero `solid` draws — square rects go through `rrect` with radius 0). Neutral has **no rrect
pipeline**: it **tessellates every rounded-rect FILL into a path** (`path-stencil-nz` + `path-cover` = 2 draws
each), and only plain squares hit the coalesced `solid` path.

| kind (40-sample totals) | neutral | ramez |
|---|---|---|
| analytic rrect | — (none) | **1264** |
| path fills (stencil+cover pairs) | **3264** | 1216 |
| solid | 312 | 0 (folded into rrect) |
| gradient | 289 | 296 |
| rounded/ path clips | ~2415 (depth-mask + stencil) | ~2036 |

Neutral's path-fill **excess over ramez ≈ 2048**, almost exactly ramez's 1264 `rrect` + the extra draw each
tessellated fill costs. WinUI puts a rounded border/background on nearly every element (nav items, buttons, cards),
so this is pervasive. **This is the #1 real-UI draw-count gap** — bigger than anything addressed so far.

## Root cause #2: glyph/path fans not coalesced across visuals

Ramez coalesces adjacent same-state path fans (glyph runs); neutral emits `path-stencil` + `path-cover` per glyph.
This is the transform-TABLE increment (#28) and accounts for much of the remaining path-fill delta once rrect is
factored out.

## What this turn's work did / didn't move

The cross-visual **solid** coalescing landed this turn (`83a6f5ca85`/`be17641196`) is correct and matches ramez's
solid stream byte-for-byte — but `solid` is only **312 of 9589** neutral draws on real pages. The real levers are
**(1) an analytic rrect fill pipeline** and **(2) glyph coalescing via the transform table**. Priority reorders:
rrect first (largest, and ramez-proven), then #28.

## Per-sample draw counts (neutral / ramez)

NavigationViewTopNavPage 614/464 · NavigationViewRS4Page 660/543 · NavigationViewCustomThemeResourcesPage 504/403
· NavigationViewTopNavOnlyPage 487/329 · PersonPicturePage 479/262 · PipsPagerPage 392/279 · NavigationViewCompact
PaneLengthTestPage 393/277 · RefreshVisualizerPage 362/137 · RefreshContainerPage 357/216 · InfoBarPage 343/252 ·
HierarchicalNavigationViewMarkup 336/254 · CommandBarFlyoutPage 269/191 · RadialGradientBrushPage 261/176 · … (full
table in scratchpad/neutral-samples40.log + ramez-samples40.log).
