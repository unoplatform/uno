# Full-UI command-stream diff — neutral vs ramez (2026-08-10)

Scene (identical geometry both sides): **10 solid sidebar rows** (each a separate visual) + a **2×3 grid of
mixed cards** (each visual = background rect + a glyph triangle). Neutral builds them as separate per-visual
recordings (ReplayRefCmd — exactly how a real visual tree reaches the backend); ramez builds the same geometry
in one drawlist. Captured on lavapipe, MSAA differs (neutral smoke default 4×, ramez 1×) — irrelevant to the
DRAW sequence.

## Solid coalescing — BYTE-IDENTICAL ✅

| # | neutral | ramez |
|---|---|---|
| 1 | `DRAW solid v=66` | `DRAW solid v=66` |
| 2 | `DRAW solid v=6` | `DRAW solid v=6` |
| 3 | `DRAW solid v=6` | `DRAW solid v=6` |
| 4 | `DRAW solid v=6` | `DRAW solid v=6` |
| 5 | `DRAW solid v=6` | `DRAW solid v=6` |
| 6 | `DRAW solid v=6` | `DRAW solid v=6` |

`v=66` = the **10 sidebar rows + card-0's background** (11 rects × 6 verts) coalesced into ONE draw ACROSS 11
separate visuals — the cross-visual coalescing this branch just gained. The five `v=6` are cards 1–5's
backgrounds (each broken off by the intervening glyph). **The break points are identical on both sides**, which
proves the glyph paths occupy the same stream positions in ramez (they split the solid runs the same way).

Before this work neutral emitted one `solid v=6` **per visual** (11 draws for the sidebar alone) because each
visual baked its transform into a per-op clip bind group → distinct bind group → unmergeable.

## Glyph/path draws — neutral emits them, ramez-harness (current build) does not

Neutral emits, per glyph: `path-stencil-nz v=3` + `path-cover v=6` (6 glyphs → 12 draws), interleaved between
the solids exactly where the solid runs break.

Ramez's **current** trace-harness build emits **no** path draw calls for the glyphs (the paths are still present
in the stream — they break the solid coalescing identically — but their draw calls don't fire/trace). This
reproduces even for the standalone `path` scene in the current build, while the **stored** `ramez-trace.txt`
(captured earlier) shows the standalone path as `path-stencil-eo v=9` + `path-cover v=6`. So it's a ramez-side
harness/build artifact, not a neutral gap — and in the real app ramez renders text (the user sees it).

Against the stored ramez baseline, the only per-draw glyph difference is the documented, intentional near-miss:
neutral `path-stencil-nz v=3` (honours the fill-rule, tight 3-vertex fan) vs ramez `path-stencil-eo v=9`
(even-odd only, 9-vertex fan). Same draw *structure*, neutral tighter/more correct.

## Verdict

On the full-UI scene the **coalesced solid command stream is byte-identical to ramez** (same draws, same vertex
counts, same break points). Glyph/path cross-visual coalescing is the next increment (transform TABLE, #28) and
its stream diff needs a ramez harness build that emits path draws.
