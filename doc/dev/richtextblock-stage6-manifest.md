# Stage 6 (ITextView + views + break) — port manifest

Captured from the Stage-6 mapping pass (commit 4a1c6184c). ~2,290 lines C++ across 12 files. The view layer is **mostly glue** that surfaces geometry from the Stage-5 BlockLayout node tree.

## Sources → targets

| WinUI source | lines | Uno target | type |
|---|---|---|---|
| `core/inc/TextView.h` | 80 | `Documents/RichTextServices/TextView.cs` (abstract ITextView) | P |
| `text/Inc/TextBlockView.h` + `TextBlock/TextBlockView.cpp` | 74+674 | `Controls/Text/Core/TextBlockView.skia.cs` | P |
| `text/Inc/RichTextBlockView.h` + `RichTextBlock/RichTextBlockView.cpp` | 98+570 | `Controls/Text/Core/RichTextBlockView.skia.cs` | P |
| `text/Inc/RichTextBlockBreak.h` + .cpp | 47 | `Documents/BlockLayout/RichTextBlockBreak.skia.cs` (wraps `BlockNodeBreak`) | P |
| `text/Inc/TextPosition.h` + `common/TextPosition.cpp` | 89+91 | `Documents/RichTextServices/TextPosition.skia.cs` (wraps `CPlainTextPosition`) | **P — BLOCKED on Stage 4** |
| `text/Inc/LineMetricsCache.h` + `RichTextArea/LineMetricsCache.cpp` | 100+102 | `Documents/BlockLayout/LineMetricsCache.skia.cs` | P (NOT done — distinct from Stage-5 `LineMetrics`) |
| `LinkedRichTextBlockView.{h,cpp}` | 85+280 | `Controls/Text/Core/LinkedRichTextBlockView.skia.cs` | **deferred to Stage 9 (overflow); scaffold only** |

## ITextView surface → maps onto existing node tree

| ITextView method | maps to |
|---|---|
| `IsAtInsertionPosition(pos)` | `BlockNode.IsAtInsertionPosition` (1:1) |
| `PixelPositionToTextPosition(pixel, inclNL)` | `BlockNode.PixelPositionToTextPosition(out gravity)` (1:1; gravity translation) |
| `TextRangeToTextBounds(start,end)` | `BlockNode.GetTextBounds(start,len,List<TextBounds>)` (coalesce + offset) |
| `GetContentStartPosition()` | `PageNode.GetStartPosition()` |
| `GetContentLength()` | `BlockNode.GetContentLength()` |
| `TextPositionToPixelPosition(pos,gravity)` | **net-new** — caret math (line box stack + alignment offset + baseline). Precision risk. |
| `GetUIScopeForPosition` / `ContainsPosition` / `TextSelectionToTextBounds` | **net-new — scaffold, full impl Stage 7** |

## Recommended sub-order
1. `ITextView` abstract (no deps).
2. `TextBlockView` (single page @ offset 0; delegates to BlockNode).
3. `RichTextBlockView` (+ `TransformPositionToPage`/`FromPage` offset math).
4. Scaffold `LinkedRichTextBlockView` + Stage-7 placeholders (`throw NotImplemented`).
5. `LineMetricsCache` (value holder).
6. `TextPosition` — only after Stage 4 `CPlainTextPosition` lands.

## Top risks
- **TextPositionToPixelPosition caret geometry** — multi-layer (line metrics + alignment + baseline sign); add an early single-char caret-bounds runtime test.
- **Offset arithmetic** TextBlock (page@0) vs RichTextBlock (pages@arbitrary) — off-by-one in page transforms; validate with runtime tests.
- **TextGravity forward/backward** at cluster boundary — ParsedText.GetIndexAt returns a single index; gravity is derived in ParagraphNode.
