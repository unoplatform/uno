# RichTextBlock — gap-closure plan (ordered by complexity)

> **Basis:** the 136-file port review + a per-gap root-cause investigation (12 gaps scoped against the ported code, the *kept* Uno `ParsedText` Skia engine, and the WinUI C++ sources at `4a1c6184c`).
> **Effort tiers:** `XS` = hours · `S` = ~1 day · `M` = 2–4 days · `L` = 1–2 weeks · `XL` = multi-week.
> **Key finding:** several "showcase gaps" are *not* gaps — the port keeps Uno's `ParsedText` engine, which already does the work (e.g. `CharacterSpacing`, color-emoji *rendering*). The true gaps cluster in the ported BlockLayout/RichTextServices bridge seams.

## Ordering at a glance

| # | Gap | Tier | Verdict | Risk | Showcase card | Lights up |
|--:|-----|:----:|---------|:----:|:-------------:|-----------|
| — | CharacterSpacing | `XS` | **already works** | low | 1.8 | (verify only) |
| 1 | BaselineOffset | `S` | partial | low | 11.2 | baseline align + `BaselineOffset` |
| 2 | IsColorFontEnabled toggle | `S` | partial | low | 6.2 | emoji already renders; add the on/off DP |
| 3 | TextLineBounds | `S` | full | med | 5.7 | Full/Tight/TrimToCapHeight/TrimToBaseline |
| 4 | Caret hit-testing (SkiaTextLine) | `M` | full | low | 8.1 / 11.2 | selection geometry + UIA parity |
| 5 | Overflow render-slice | `M` | partial | med | 10.1 | mid-paragraph overflow *rendering* |
| 6 | OpticalMarginAlignment | `M` | full | med | 6.2 | TrimSideBearings |
| 7 | OpenType typography | `M` | full | med | 1.9 | super/subscript, small-caps, fractions |
| 8 | Ellipsis / text trimming | `L` | full | med | 5.2 / 5.3 | Character/WordEllipsis + MaxLines ellipsis |
| 9 | InlineUIContainer inline layout | `L` | full | **high** | 3.1 | embedded UIElements flowing inline |
| 10 | Bidi / TextReadingOrder | `L` | partial | med | 7.1 | RTL + DetectFromContent |
| 11 | Touch grippers + caret browsing | `L` | full | med | 8.1 | touch selection + caret nav |

## Dependency graph (do these before those)

- **Overflow render-slice (5) → Ellipsis (8).** Both need the same new "draw a *line sub-range* of a paragraph" primitive on `ParsedText.Draw`. Build it once in (5); (8) reuses it so MaxLines-ellipsis lands on the right line.
- **Caret hit-testing (4) → Grippers/caret-browsing (11).** The five `SkiaTextLine` hit methods are the geometry (11) is built on.
- **BaselineOffset (1) → InlineUIContainer (9).** Correct embedded-child baseline alignment consumes `BlockLayoutHelpers.GetElementBaseline`, which needs the baseline value surfaced.
- Everything in **Tier 1 is independent** — parallelizable.

---

## Tier 0 — already works / verify-only (hours)

- **CharacterSpacing** — *nothing to build.* `ParsedText.skia.cs` already computes `FontSize * CharacterSpacing / 1000` per run and advances glyphs by it (measure + render). Add a confirming runtime assert only.
- **Color-emoji rendering** — already renders through `ParsedText.Draw` → `SKTextBlob`. Only the `IsColorFontEnabled` *toggle* is missing → item 2.

## Tier 0.5 — trivial sample hygiene (minutes)

- Fix `RichTextBlock_Hyperlinks.xaml` — "GitHub" link points at `github.com/nicehash`; should be `github.com/unoplatform/uno`.
- Swap hard-coded chrome colors for `{ThemeResource}` in the 7 pre-existing RichTextBlock samples (they don't adapt to dark theme).

---

## Tier 1 — Quick wins · surface wiring, no engine changes (½–1.5 days each, low risk)

### 1. BaselineOffset — `S`, low risk
**Root cause:** the value is fully computed and populated (`ParagraphNode` → `ContainerNode.GetBaselineAlignmentOffset` → `PageNode`), but the three `GetBaselineOffset` bridge slots throw and the public `BaselineOffset` getter is a `[NotImplemented]` stub.
**Approach:** port `CRichTextBlock::GetBaselineOffset` verbatim — `pBaseline = _pageNode?.GetBaselineAlignmentOffset() ?? 0` — for `RichTextBlock` + `RichTextBlockOverflow`; add the get-only public property to each (drops the generated stub). For `TextBlock` (which renders through the kept engine, not PageNode) add a small `FirstLineBaseline` accessor to `IParsedText`/`ParsedText`/`UnicodeText` (`RenderLines[0].Height + BaselineOffsetY + Padding.Top`).
**Files:** `RichTextBlock.BlockLayout.skia.cs`, `RichTextBlockOverflow.BlockLayout.skia.cs`, `TextBlock.BlockLayout.skia.cs`, `IParsedText.skia.cs`, `ParsedText.skia.cs`, `UnicodeText.skia.cs`.
**Validate:** card 11.2 readout; runtime parity test (watch baseline-*sign*, plan Risk R10) vs native WinUI.

### 2. IsColorFontEnabled toggle — `S`, low risk
**Root cause:** emoji already render in color; the DP is a `[NotImplemented]` stub and `BlockLayoutHelpers.GetIsColorFontEnabled` returns a hardcoded constant.
**Approach:** move `IsColorFontEnabled` out of `Generated` into `*.Properties.cs` (default `false` per WinUI), make `GetIsColorFontEnabled` read it, and gate the color-glyph paint on it in the `ParsedText` draw path.
**Files:** `RichTextBlock.Properties.cs`, `TextBlock.Properties.cs`, `BlockLayoutHelpers.skia.cs`, `ParsedText.skia.cs`.
**Validate:** card 6.2 (on vs off).

### 3. TextLineBounds — `S`, medium risk
**Root cause:** ignored end-to-end; the kept engine never supported it either. `BlockLayoutHelpers.GetTextLineBounds` hard-returns `Full`.
**Approach (localized, keep the kept engine untouched to protect TextBlock):** read the real DP in `BlockLayoutHelpers.GetTextLineBounds`, then adjust each line's reserved vertical extent from font ascent/descent/cap-height metrics in the port layer (`LineMetrics`/`ParagraphNode` stacking) for Tight/TrimToCapHeight/TrimToBaseline.
**Files:** `BlockLayoutHelpers.skia.cs`, `LineMetrics.skia.cs`, `ParagraphNode.skia.cs`, `TextParagraphProperties.skia.cs`.
**Validate:** card 5.7 (the "Ágjpy" frames).

---

## Tier 2 — Bounded engine wiring (2–4 days each, medium risk) · localized, no architectural change

### 4. Caret hit-testing on `SkiaTextLine` — `M`, **low** risk *(do first in this tier)*
**Root cause:** all five methods throw — `GetCharacterHitFromDistance`, `GetDistanceFromCharacterHit`, `GetPreviousCaretCharacterHit`, `GetNextCaretCharacterHit`, `GetTextBounds`.
**Approach:** implement each line-local over the wrapped `_renderLine`, mirroring the proven `ParsedText` algorithms but scoped to one line (offsets are already line-relative). Surrogate/combining-mark stepping and per-range bounds come from existing `RenderSegmentSpan`/`GlyphInfo` cluster data — no new shaping.
**Why first:** lowest risk in the tier, unblocks selection geometry + UIA `TextPattern` parity (Stages 8/10).
**Files:** `SkiaTextLine.skia.cs` (+ read-only reuse of `ParsedText.skia.cs`).
**Validate:** `TextRangeToTextBounds` / `PixelPositionToTextPosition` / `IsAtInsertionPosition` runtime tests; cards 8.1 / 11.2.

### 5. Overflow render-slice — `M`, medium risk *(do before item 8)*
**Root cause:** paging is fully ported and correct (the 4 break overlap cases in `PageNode.MeasureCore`); only the *render* path can't draw a mid-paragraph resume — it draws whole paragraphs.
**Approach:** add a `ParsedText.Draw` overload taking `(firstLine, lineCount)` (defaulting to whole-paragraph so **TextBlock is untouched**); render only that line window with a `leadingCharOffset`. This is the shared "line sub-range render" primitive.
**Files:** `ParsedText.skia.cs`, `RichTextBlockOverflow.skia.cs`, `RichTextBlock.skia.cs`.
**Validate:** card 10.1 with a *single long paragraph* (no line breaks) flowing master→overflow→overflow; `HasOverflowContent`/`IsTextTrimmed`.

### 6. OpticalMarginAlignment — `M`, medium risk
**Root cause:** `BlockLayoutHelpers.GetOpticalMarginAlignment` hard-returns `None` (stale "no Uno DP" TODO — the DP exists).
**Approach:** read the real DP, then thread `TrimSideBearings` into the line-formatting path (first-glyph left side-bearing trim + line-start offset). Sourcing side-bearing metrics from the Skia glyph path is the bulk.
**Files:** `BlockLayoutHelpers.skia.cs`, `ParagraphNode.skia.cs`, `TextParagraphProperties.skia.cs`.
**Validate:** card 6.2 (TrimSideBearings on a large glyph).

### 7. OpenType typography — `M`, medium risk
**Root cause:** `Typography.*` attached props (FontVariants/FontCapitals/FontFraction + ~40 more) are `[NotImplemented]` stubs and never reach the shaper.
**Approach:** un-stub the `Typography` attached DPs (`Inherits | AffectsMeasure`), add a `Typography → HarfBuzz feature-tag` map, and pass features into the per-`Run` Skia shaper (bypasses the WinUI `TextRunProperties` route — the Skia path shapes per-run).
**Files:** `Typography.cs` (out of Generated), the per-run shaper in `Run.skia.cs`/`ParsedText.skia.cs`, `InheritedProperties.cs` (optional).
**Validate:** card 1.9 (visual — glyph correctness needs eyes, not just asserts).

---

## Tier 3 — Feature builds (1–2 weeks each, higher risk) · real engine features / cross-cutting

### 8. Ellipsis / text trimming — `L`, medium risk *(after item 5)*
**Root cause:** every seam is a stub — `SkiaTextLine.Collapse` returns `this`, `TextCollapsingCharacters.Width/Draw` throw, `ComputeCollapsingCharacterWidth` returns 0. The *working* ellipsis in Uno (`UnicodeText.skia.cs`) is on a **different** engine that RichTextBlock doesn't use.
**Approach:** re-implement ellipsis against `ParsedText`'s `RenderLine`/`RenderSegmentSpan` model, adapting `UnicodeText`'s proven trim-point + `…`-append algorithm; render it via the line-range slice from item 5. Implement `ComputeCollapsingCharacterWidth` from `FontDetails.SKFont` metrics.
**Files:** `SkiaTextLine.skia.cs`, `TextCollapsingCharacters.skia.cs`, `BlockLayoutHelpers.skia.cs`, `ParsedText.skia.cs`, `SkiaTextFormatter.skia.cs`.
**Validate:** cards 5.2 / 5.3 side-by-side vs WinUI.

### 9. InlineUIContainer inline layout — `L` (→ `XL` if faithful run-model), **high** risk
**Root cause:** embedded inline UIElements measure/arrange/render **nowhere** on Skia — `InlineUIContainer` is a `[NotImplemented]` stub, its BlockLayout partials throw, the kept engine excludes it from `leafTree`, and the ported run model (`GetTextRun`/`PageHostedObjectRun`) is disconnected. (Orchestration in `PageNode`/`ParagraphNode` *is* ported but hangs off throwing leaf points.)
**Approach (pragmatic, keep-the-engine):** de-stub `InlineUIContainer` (real `Child` DP that parents the element), reroute `UIElement.GetLayoutStorage` to the real `LayoutStorage`, include it in `leafTree`, add an object-run branch in `ParsedText.ParseText` that measures the child and reserves `DesiredSize.Width` as advance + contributes height/baseline, drive `PageHostedObjectRun.Format/Arrange`, arrange the child in the visual tree, and wire invalidation. Keep the 2-position (Open/Close) accounting for selection/hit-test.
**Depends on:** item 1 (baseline). **Files:** the InlineUIContainer + BlockLayout stubs, `InlineCollection.cs`, `ParsedText.skia.cs`, `PageHostedObjectRun.skia.cs`.
**Validate:** card 3.1; runtime asserts on child DesiredSize, reserved width, arranged position, baseline.

### 10. Bidi / TextReadingOrder — `L`, medium risk
**Root cause:** explicit RTL *partially* works; `TextReadingOrder=DetectFromContent` is a no-op and true UBA resolution is absent, because RichTextBlock binds to the older `ParsedText` (not the ICU-bidi-capable `UnicodeText`).
**Approach:** give `ParsedText` a real bidi pass mirroring `UnicodeText`'s ICU logic (`ubidi` para-level `DEFAULT_LTR/RTL` when DetectFromContent, else explicit level; split runs by visual order). This activates the already-ported `AlignmentFollowsReadingOrder`/`GetDetectedParagraphDirection` coordinate-flip code. Add the public `TextReadingOrder` DP if absent.
**Files:** `ParsedText.skia.cs`, `TextParagraphProperties.skia.cs`, `ParagraphNode.skia.cs`.
**Validate:** card 7.1; mixed-direction runtime tests.

### 11. Touch grippers + caret browsing — `L`, medium risk *(after item 4)*
**Root cause:** the manager side is wired, but the gripper host/presenter is bound to a concrete `TextBlock` single-surface; caret-browsing is absent.
**Approach:** two independent pieces — **grippers (M ~3–4d):** generalize `ITextSelectionGripperHost` off the concrete `TextBlock` into a small surface abstraction (`HitTest`, `RectForIndex`, padding, redraw) that RichTextBlock implements; **caret-browsing:** add the mode + arrow-key caret movement over the container↔flat offset seam (`RichTextBlockView.GetCharacterIndex`).
**Depends on:** item 4; spanning linked overflow views also needs item 5's overflow hit-testing.
**Files:** `TextSelectionManager.Gripper.skia.cs`, `TextSelectionManager.skia.cs`, `TextSelectionGripperPresenter.skia.cs`.
**Validate:** touch-pointer runtime test (long-press word select + drag gripper) on a selection-enabled RichTextBlock; card 8.1.

---

## Recommended first sprint (~1 week, low risk, no engine surgery)

**Tier 0.5** (sample hygiene) → **Tier 1** (BaselineOffset, IsColorFontEnabled, TextLineBounds) → **item 4** (caret hit-testing). This lights up showcase cards **5.7, 6.2, 8.1, 11.2** and selection geometry, is all low-risk surface/line-local work, and sets up Tier 2. Then do **item 5 (overflow render-slice) before item 8 (ellipsis)** to build the shared render primitive once.

<sub>Effort tiers are reviewer estimates grounded in the cited code, not commitments. Each item's "Validate" points at the specific showcase card in `RichTextBlock_Showcase` and/or a runtime test; prove parity against native WinUI via `/winui-runtime-tests` where reproducible.</sub>
