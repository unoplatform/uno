# Stage 9 — RichTextBlock control flip (blueprint from CRichTextBlock.cpp)

Reshape `RichTextBlock.{cs,skia.cs}` onto BlockLayoutEngine + PageNode + RichTextBlockView + TextSelectionManager, replacing the managed `ParseAllParagraphs`/`_paragraphLayouts`. Source: `dxaml/xcp/core/text/RichTextBlock/RichTextBlock.cpp` (3631 lines), commit 4a1c6184c. **Hard gate: all 66 `Given_RichTextBlock` tests stay green.**

## Fields to add (skia)
`BlockLayoutEngine m_pBlockLayout`, `PageNode m_pPageNode`, `RichTextBlockView m_pTextView`, `TextSelectionManager m_pSelectionManager`, `RichTextBlockBreak m_pBreak`, `float m_layoutRoundingHeightAdjustment`, `uint m_maxLines`.

## EnsureBlockLayout() (RichTextBlock.cpp:1697)
1. EnsureTextFormattingForRead / EnsureInheritedPropertiesForRead.
2. Create Blocks if null.
3. If `m_pBlockLayout == null`: EnsureFontContext; `m_pBlockLayout = new BlockLayoutEngine(this)`.
4. If selection manager null AND (IsTextSelectionEnabled || HighContrast): `CreateTextSelectionManager()` → `TextSelectionManager.Create(this, Blocks.GetTextContainer(), out m_pSelectionManager)` + UpdateSelectionHighlightColor + `m_pSelectionManager.TextViewChanged(null, view)`.
5. If `m_pPageNode == null`: `m_pBlockLayout.CreatePageNode(Blocks, this)` → cast PageNode.
6. If pageNode != null && view null: `m_pTextView = new RichTextBlockView(m_pPageNode, this)`; wire `m_pSelectionManager.TextViewChanged(null, m_pTextView)`.

> Need `BlockCollection.GetTextContainer()` returning `this as ITextContainer` (BlockCollection implements ITextContainer per Stage 4b).

## MeasureOverride (1326)
- Measure embedded children (InlineUIContainer) with previous constraints.
- `EnsureBlockLayout()`.
- `m_pPageNode.Measure(availableSize, m_maxLines, 0f, allowEmptyContent:FALSE, measureBottomless:FALSE, suppressTopMargin:TRUE, null, out _)`; `desiredSize = m_pPageNode.GetDesiredSize()`.
- Break (overflow): if `GetBreak()` changed vs `m_pBreak.GetBlockBreak()`, `SetBreak(new RichTextBlockBreak(GetBreak()))`.
- Layout rounding: ceil(size*plateauScale)/scale; `m_layoutRoundingHeightAdjustment = rounded.h - pageNode.h`.
- HighContrastAdjustment refresh.

## ArrangeOverride (1435)
- If pageNode && !MeasureDirty: `m_pPageNode.Arrange(finalSize)`; `renderSize = GetRenderSize()`.
- selection highlight region (if selection enabled+visible): `m_pSelectionManager.GetSelectionHighlightRegion(useHighContrast, out selection)`.
- DrawingContext SetControlEnabled/SetBackPlateConfiguration.
- if no selection: `m_pPageNode.Draw(FALSE)` + mark content dirty; else `m_redrawForArrange = true`.
- `newFinalSize = max(finalSize, renderSize)`; arrange CaretBrowsingCaret at (0,0); UpdateIsTextTrimmed.

## Render (RichTextVisual.Paint) — Uno-specific paint-walk
WinUI Draw routes through PageNode→ParagraphNode DrawingContext (D2D). On Uno: `RichTextVisual.Paint` walks the arranged PageNode children; for each ParagraphNode draw its ParsedText at the paragraph offset (`ContainerNode.GetChildOffset`) via `ParsedText.Draw(session, caret, highlighters, compositionRange)`. Highlighters come from `m_pSelectionManager.GetSelectionHighlightRegion` + TextHighlighters (Stage 8 TextHighlightRenderer rects). The per-paragraph ParsedText is the same one the engine measured (SkiaTextFormatter cache), so engine line positions == ParsedText draw positions (validated: Given_BlockLayoutEngine height match).

Accessors needed: `ParagraphNode.GetParsedText()` (the paragraph's cached ParsedText, from its first line's SkiaTextLine), `SkiaTextLine.ParsedText`.

## Selection / hit-test / copy
Route to `m_pSelectionManager` (Porter-B) + `m_pTextView` (RichTextBlockView): pointer/key events → manager; `GetCharacterIndexAtPoint` → `m_pTextView.PixelPositionToTextPosition`; SelectAll/Copy → manager; `Selection` Range getter/setter ↔ manager anchor/moving (keep public Range API for the 66 tests). Implement `ITextViewHost.GetTextView() => m_pTextView`.

## Remove
`ParseAllParagraphs`, `_paragraphLayouts`, `ParagraphLayout`, `GetParagraphHighlighters`, the managed `Draw` paragraph loop, managed `GetCharacterIndexAtPointSkia` (replace with view), managed `Selection` Range backing (reshape onto manager).

## Validation
Run `Given_RichTextBlock` (66) + `Given_BlockLayoutEngine` + `Given_SkiaTextFormatter` after the flip; iterate to 66/66. Then RichTextBlockOverflow + linked chain.
