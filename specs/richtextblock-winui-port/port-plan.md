# RichTextBlock Deep-Core Port — Staged Plan (WinUI → Uno Skia)

> Scope decision (fixed by maintainer): **deep core port**. Port the whole orchestration tree as faithful 1:1-structured C# — control, ITextView, BlockLayout node tree, selection, highlight merge, text-element model, text pointers, automation peers, all interfaces/enums. **Keep Uno-specific** only the low-level glyph-shaping/line-formatting engine (Uno's Skia `ParsedText` + `Documents/TextFormatting/*`). **Bridge** the ported `RichTextServices::TextFormatter` family onto that engine. Target = **Skia only** (`.skia.cs`); native targets stay maintenance-only and must keep compiling.
>
> Every new `.cs` file: UTF-8 **BOM** + **CRLF**, MUX header (`// Copyright … MIT` + `// MUX Reference <file>, tag <tag>, commit <hash>`), file-scoped namespace, tabs, `#nullable enable`. Drop `HRESULT`/`IFC`/`Result::Enum` (→ exceptions / value returns), `TextObject`/`AddRef`/`Release`/`xref_ptr`/`com_ptr` (→ GC + plain refs), `CValue` (→ plain values), `KnownTypeIndex`/`KnownPropertyIndex` (→ `is`/`typeof`).

---

## 1. Architecture & boundary

### 1.1 Layered design

```
┌──────────────────────────────────────────────────────────────────────────┐
│ CONTROL LAYER  (FrameworkElement subclasses, public WinRT API)             │
│   RichTextBlock / RichTextBlockOverflow                                    │
│   - owns: BlockCollection (backing store root), BlockLayoutEngine,         │
│           ITextView, TextSelectionManager, RichTextBlockBreak              │
│   - MeasureOverride/ArrangeOverride drive the node tree                    │
│   - render: Skia RichTextVisual (composition) — UNCHANGED draw path        │
└───────────────┬──────────────────────────────────────────┬───────────────┘
                │ query (hit-test/bounds/position)          │ layout (measure/arrange)
                ▼                                            ▼
┌──────────────────────────────┐          ┌──────────────────────────────────┐
│ VIEW LAYER  : ITextView       │          │ BLOCKLAYOUT NODE TREE             │
│   RichTextBlockView           │  consumes│   BlockLayoutEngine               │
│   TextBlockView               │◄─────────│    └ PageNode (root, ContainerNode)│
│   LinkedRichTextBlockView     │          │        └ ParagraphNode (leaf) ×N   │
│   (ITextView = the selection/  │          │   + *NodeBreak, DrawingContexts   │
│    pointer geometry contract)  │          │   + LineMetrics cache             │
└──────────────┬───────────────┘          └───────────────┬──────────────────┘
               │                                           │ ParagraphNode.MeasureCore
               │ both ultimately resolve geometry          │ calls FormatLine per line
               │ from ParagraphNode line cache             ▼
               │                          ┌──────────────────────────────────────┐
               │                          │ ★ BRIDGE CONTRACT (ported abstracts)  │
               │                          │   TextFormatter / TextLine / TextSource│
               │                          │   TextRun(+subtypes) / TextRunProperties│
               │                          │   TextParagraphProperties / TextBounds  │
               │                          │   CharacterHit / TextBreak / TextLineBreak│
               │                          │   TextDrawingContext / TextRunCache     │
               │                          │   TextCollapsingSymbol                   │
               │                          └───────────────┬────────────────────────┘
               │                                          │ implemented by Skia adapter
               ▼                                          ▼
┌────────────────────────────────────────────────────────────────────────────┐
│ KEEP-UNO-SPECIFIC: Skia text engine  (Documents/TextFormatting/*, ParsedText)│
│   ParsedText.ParseText  (whole-collection line breaking → List<RenderLine>)  │
│   ParsedText.Draw       (SKTextBlobBuilder + UnoSkiaApi glyph emission)      │
│   ParsedText.GetIndexAt / GetRectForIndex / GetWordAt / GetLineAt            │
│   RenderLine / RenderSegmentSpan / Segment / GlyphInfo / FontDetails         │
│   HarfBuzz shaping, Unicode line-break, FontDetailsCache                     │
└────────────────────────────────────────────────────────────────────────────┘
```

### 1.2 The exact bridge seam

The single load-bearing seam is **`ParagraphNode` → `TextFormatter.FormatLine(...) → TextLine`**. WinUI's `ParagraphNode.MeasureCore` calls `pTextFormatter->FormatLine(...)` once per line in a loop and reads `GetWidth/GetHeight/GetBaseline/GetLength/GetTextLineBreak` back off the returned `TextLine`. We reproduce that *call shape exactly*; the **implementation** behind it is Uno's existing `ParsedText`.

Two boundary objects the engine must supply:
- **`TextLine`** wraps an Uno **`RenderLine`** (metrics + hit-test + `Draw`).
- **`TextDrawingContext`** is the glyph-recording sink `TextLine.Draw` writes into; its Skia impl routes through the *existing* `ParsedText.Draw` / `IParsedText.Draw` path (brush, caret, highlighters, composition range) — no new glyph emission code.

**Critical impedance mismatch** (the #1 engineering risk, see §6): WinUI `FormatLine` is **per-line** with an opaque `TextLineBreak` continuation token threaded call-to-call; Uno `ParsedText.ParseText` formats the **whole inline collection** in one pass into `List<RenderLine>`. The adapter resolves this with **Strategy B** (recommended): `TextFormatterImpl` runs `ParseText` **once** per paragraph (lazily, on first `FormatLine`), caches the `List<RenderLine>`, and **vends** them sequentially — `FormatLine(call N)` returns `SkiaTextLine(renderLines[N])` and mints a trivial `TextLineBreak` carrying just the next line index. `GetTextLineBreak()` returns `null` on the last line (loop terminator). This keeps the high-value node tree (paging, embedded elements, selection, overflow) **1:1** while leaving Uno's proven shaping untouched.

### 1.3 Integration with the existing `IParsedText` seam

Uno already has the precise managed boundary interface **`IParsedText`** (verified, `Documents/IParsedText.skia.cs`):

```csharp
internal interface IParsedText
{
    void Draw(in Visual.PaintingSession session,
        (int index, CompositionBrush brush, float thickness)? caret,
        IEnumerable<TextHighlighter> highlighters,
        (int startIndex, int length)? compositionRange);
    Rect GetRectForIndex(int adjustedIndex);
    int GetIndexAt(Point p, bool ignoreEndingNewLine, bool extendedSelection);
    Hyperlink GetHyperlinkAt(Point point);
    (int start, int length) GetWordAt(int index, bool right);
    internal (int start, int length, bool firstLine, bool lastLine, int lineIndex) GetLineAt(int index);
    bool IsBaseDirectionRightToLeft { get; }
}
```

The ported `TextLine`/`TextDrawingContext` are **reconciled onto `IParsedText`**, not stood up as a parallel boundary. The adapter holds the `IParsedText` (an Uno `ParsedText`) and forwards.

---

## 2. The bridge contract (the linchpin C# abstractions)

All live in **`Microsoft.UI.Xaml.Documents.RichTextServices`** namespace, under `src/Uno.UI/UI/Xaml/Documents/RichTextServices/`. Abstract contracts are platform-neutral `.cs`; Skia implementations are `.skia.cs` under a `Skia/` subfolder.

### 2.1 `TextFormatter` (abstract) + `SkiaTextFormatter` (impl)

```csharp
internal abstract class TextFormatter
{
    // The single core method. Called once per line by ParagraphNode.MeasureCore,
    // again by FormatLineAtIndex for arrange/draw (MUST reuse the same wrappingWidth).
    public abstract TextLine FormatLine(
        TextSource textSource,
        int firstCharIndex,
        double wrappingWidth,
        TextParagraphProperties paragraphProperties,
        TextLineBreak? previousLineBreak,
        TextRunCache? runCache);
}
```

`SkiaTextFormatter` (`.skia.cs`): on first `FormatLine` for a paragraph it builds the `Inline[]` leaf array (from the `TextSource`, which itself reads `InlineCollection.TraversedTree.leafTree`), calls `ParsedText.ParseText(...)`, stores `List<RenderLine>` + `out desiredSize`, and vends `SkiaTextLine` per line keyed by the index inside `previousLineBreak`/`runCache`.

### 2.2 `TextLine` (abstract) + `SkiaTextLine` (impl wrapping `RenderLine`)

Preserve the WinUI protected-metrics field set **and order** (as a comment block + properties):
`Width, WidthIncludingTrailingWhitespace, Start, Height, TextHeight, Baseline, TextBaseline, OverhangLeading, OverhangTrailing, Length, TrailingWhitespaceLength, NewlineLength, DependentLength, TextLineBreak, AlignmentFollowsReadingOrder`.

```csharp
internal abstract class TextLine
{
    // Metrics (concrete getters over protected fields in WinUI; here, properties)
    public double Width { get; protected set; }
    public double WidthIncludingTrailingWhitespace { get; protected set; }
    public double Start { get; protected set; }
    public double Height { get; protected set; }
    public double TextHeight { get; protected set; }
    public double Baseline { get; protected set; }
    public double TextBaseline { get; protected set; }
    public double OverhangLeading { get; protected set; }
    public double OverhangTrailing { get; protected set; }
    public int Length { get; protected set; }
    public int TrailingWhitespaceLength { get; protected set; }
    public int NewlineLength { get; protected set; }          // 0 / 1 (LF/LS) / 2 (CRLF)
    public int DependentLength { get; protected set; }
    public TextLineBreak? TextLineBreak { get; protected set; } // null ⇒ end of paragraph
    public bool AlignmentFollowsReadingOrder { get; protected set; }

    // Operations (abstract)
    public abstract void Arrange(Rect bounds);
    public abstract void Draw(TextDrawingContext drawingContext, Point origin, double viewportWidth);
    public abstract TextLine Collapse(double collapsingWidth, TextTrimming trimming, TextCollapsingSymbol? symbol);
    public abstract bool HasCollapsed { get; }
    public abstract bool HasMultiCharacterClusters { get; }

    // Hit-testing / caret nav over CharacterHit
    public abstract CharacterHit GetCharacterHitFromDistance(double distance);
    public abstract double GetDistanceFromCharacterHit(CharacterHit characterHit);
    public abstract CharacterHit GetPreviousCaretCharacterHit(CharacterHit characterHit);
    public abstract CharacterHit GetNextCaretCharacterHit(CharacterHit characterHit);
    public abstract TextBounds[] GetTextBounds(int firstCharacterIndex, int textLength);

    public abstract FlowDirection GetParagraphDirection();
    public abstract FlowDirection GetDetectedParagraphDirection();
}
```

`SkiaTextLine` mapping onto the verified `RenderLine` surface:
| `TextLine` member | `RenderLine` source |
|---|---|
| `Width` | `Width` |
| `WidthIncludingTrailingWhitespace` | `Width` (RenderLine `Width` already includes trailing; `WidthWithoutTrailingSpaces` → `TextHeight`-style trim) |
| `Height` | `Height` |
| `Baseline` | `-BaselineOffsetY` (sign-reconcile; see §6) |
| `Length` / `TrailingWhitespaceLength` / `NewlineLength` | sum over `SegmentSpans` (`Segment.LineBreakLength`, `RenderSegmentSpan.TrailingSpaces`) |
| alignment offset | `RenderLine.GetOffsets(width, alignment)` |
| `Draw` | `ParsedText.Draw(...)` via `TextDrawingContext` |
| `GetTextBounds` | `ParsedText.GetRectForIndex` per index, coalesced into per-run rects + `FlowDirection` |
| `GetCharacterHitFromDistance` | `ParsedText.GetIndexAt(point,…)` + trailing-edge resolution |
| `Collapse` / `HasCollapsed` | **net-new** (ellipsis; see §6) |

### 2.3 `TextSource` (abstract) — host→formatter input

```csharp
internal abstract class TextSource
{
    public abstract TextRun GetTextRun(int characterIndex);  // TextCharactersRun / ObjectRun / EndOfLine / …
    public abstract IEmbeddedElementHost? GetEmbeddedElementHost();
}
```
Concrete impl = `ParagraphTextSource` (BlockLayout cluster) walking the `InlineCollection`.

### 2.4 `TextRun` hierarchy + properties

```csharp
internal abstract class TextRun
{
    public int Length { get; }            // m_length
    public int CharacterIndex { get; }    // m_characterIndex
    public TextRunType Type { get; }      // m_type
}
```
Subtypes: `TextCharactersRun(string/ReadOnlyMemory<char>, TextRunProperties; IsTab; Split)`, `ObjectRun(abstract; Format→ObjectRunMetrics; ComputeBoundingBox; Arrange)`, `EndOfLineRun`, `EndOfParagraphRun`, `HiddenRun`, `DirectionalControlRun(DirectionalControl)`.

```csharp
internal sealed class TextRunProperties
{
    public const int CharacterSpacingScale = 1000;
    public FontDetails FontTypeface { get; }           // RtsInterop FontTypeface → Uno FontDetails/SKTypeface
    public double FontSize { get; }
    public bool HasUnderline { get; }
    public bool HasStrikethrough { get; }
    public int CharacterSpacing { get; }               // 1/1000 em
    public WeakReference<DependencyObject>? ForegroundBrushSource { get; }
    public CultureInfo? Culture { get; }
    public bool EqualsForShaping(TextRunProperties other);  // drives run-merge/cache
}
```

### 2.5 `TextParagraphProperties` (+ `Flags`)

```csharp
internal class TextParagraphProperties
{
    [Flags]
    public enum Flags { None = 0, Justify = 1, TrimSideBearings = 2 /*OpticalMarginAlignment*/,
                        DetermineTextReadingOrderFromContent = 4, DetermineAlignmentFromContent = 8 }

    public FlowDirection FlowDirection { get; }
    public TextRunProperties DefaultTextRunProperties { get; }
    public double FirstLineIndent { get; }
    public TextWrapping TextWrapping { get; }
    public TextLineBounds TextLineBounds { get; }
    public TextAlignment TextAlignment { get; }
    public Flags ParagraphFlags { get; }
    public double DefaultIncrementalTab => 4 * DefaultTextRunProperties.FontSize; // reconcile vs Segment tab=48 (§6)
}
```

### 2.6 Value types & enums (pure, no deps)

```csharp
internal readonly record struct CharacterHit(int FirstCharacterIndex, int TrailingLength);
internal readonly record struct TextBounds(Rect Rect, FlowDirection FlowDirection);
internal readonly record struct ObjectRunMetrics(double Width, double Height, double Baseline);

internal abstract class TextBreak { public virtual bool Equals(TextBreak? other) => ReferenceEquals(this, other); }
internal class TextLineBreak : TextBreak { /* Skia impl subclass carries next-line index */ }
internal abstract class TextRunCache { public abstract void Clear(); }

internal enum LogicalDirection { Backward = 0, Forward = 1 }     // OR reuse public Documents.LogicalDirection
internal enum ElementType { Paragraph = 0, Inline = 1, LineBreak = 2, Object = 3 }
internal enum LayoutNodeType { Page, Paragraph, Line }
internal enum TextRunType { Text, Hidden, EndOfLine, EndOfParagraph, Object, DirectionalControl }
internal enum DirectionalControl { None, LeftToRightEmbedding, RightToLeftEmbedding,
                                   PopDirectionalFormatting, LeftToRightMark, RightToLeftMark }
```
**Reuse existing public enums** (no new file): `FlowDirection`, `TextAlignment` (RTS `DetermineAlignmentFromContent` flag carried separately via `TextParagraphProperties.Flags`).

### 2.7 `TextDrawingContext` (abstract) + `SkiaTextDrawingContext`

```csharp
internal abstract class TextDrawingContext
{
    public abstract void DrawGlyphRun(Point position, double runWidth,
        IReadOnlyList<GlyphInfo> glyphs, FontDetails font,
        WeakReference<DependencyObject>? brushSource, Rect? clip);
    public abstract void DrawLine(Point position, double width, double thickness,
        int bidiLevel, WeakReference<DependencyObject>? brushSource, Rect? clip); // underline/strikethrough
    public abstract void Clear();
    public abstract bool HasRenderingData { get; }
    public abstract void SetLineInfo(double viewportWidth, bool invertHorizontalAxis, double yOffset, double verticalAdvance);
    public abstract void SetIsColorFontEnabled(bool value);
    public abstract void SetColorFontPaletteIndex(int index);
}
```
The DWrite-typed `DrawGlyphs(cluster-map/ShapingProperties/IFssFontFace)` overload is **NOT ported**. `SkiaTextDrawingContext` folds into the existing `ParsedText.Draw` glyph-emission path.

### 2.8 Host interfaces

`ILayoutEngineHost` (GetBackingStore, InterruptMeasure, GetElementType, GetTextFormatter/ReleaseTextFormatter, GetParagraphTextSource, GetTextParagraphProperties), `ITextElement` (drop `AddRef/Release`; `GetElementRecord`=undo → minimal stub for read-only RTB), `ILinkedTextContainer` (overflow chain — **deferred**, see stages). `TextFormatterCache`/`TextRunCache` collapse to trivial/no-op single-instance pools since the Skia formatter is cheap.

---

## 3. Target file layout (WinUI → Uno)

Namespaces: bridge contract & node tree & element-model additions → `Microsoft.UI.Xaml.Documents.*`; views/positions → `Microsoft.UI.Xaml.Controls.Text.Core` (confirm against control cluster before scaffolding); controls → `Microsoft.UI.Xaml.Controls`; peers → `Microsoft.UI.Xaml.Automation.Peers`; selection → `Microsoft.UI.Xaml.Controls` (TextBlock folder). Actions: **P**=port-new, **A**=align-existing-uno, **B**=bridge-to-skia, **K**=keep-uno-specific, **S**=skip.

### Cluster A — RichTextServices contract (the TextFormatter boundary)
| WinUI source | Uno target | Act |
|---|---|---|
| `RichTextServices/inc/Result.h` | — (drop; exceptions) | S |
| `…/FlowDirection.h` | reuse `UI/Xaml/FlowDirection.cs` | A |
| `…/LogicalDirection.h` | `Documents/RichTextServices/LogicalDirection.cs` (or reuse public) | P |
| `…/ElementType.h` | `Documents/RichTextServices/ElementType.cs` | P |
| `…/LayoutNodeType.h` | `Documents/RichTextServices/LayoutNodeType.cs` | P |
| `…/TextRunType.h` | `Documents/RichTextServices/TextRunType.cs` | P |
| `…/TextAlignment.h` | reuse `UI/Xaml/TextAlignment.cs` | A |
| `…/CharacterHit.h` | `Documents/RichTextServices/CharacterHit.cs` | P |
| `…/TextBounds.h` | `Documents/RichTextServices/TextBounds.cs` | P |
| `…/ObjectRunMetrics.h` | `Documents/RichTextServices/ObjectRunMetrics.cs` | P |
| `…/TextObject.h` | — (drop; GC) | S |
| `…/TextFormatter.h` | `…/RichTextServices/TextFormatter.cs` + `…/Skia/SkiaTextFormatter.skia.cs` | B |
| `…/TextFormatterCache.h` | `…/RichTextServices/TextFormatterCache.cs` | B |
| `…/TextLine.h` (511) | `…/RichTextServices/TextLine.cs` + `…/Skia/SkiaTextLine.skia.cs` | B |
| `…/TextLineBreak.h` | `…/RichTextServices/TextLineBreak.cs` | B |
| `…/TextBreak.h` | `…/RichTextServices/TextBreak.cs` | P |
| `…/TextRun.h` | `…/RichTextServices/TextRun.cs` | P |
| `…/TextRunProperties.h` (225) | `…/RichTextServices/TextRunProperties.cs` | P |
| `…/TextRunCache.h` | `…/RichTextServices/TextRunCache.cs` | B |
| `…/TextParagraphProperties.h` (221) | `…/RichTextServices/TextParagraphProperties.cs` | P |
| `…/TextSource.h` | `…/RichTextServices/TextSource.cs` | P |
| `…/TextCharactersRun.h` | `…/RichTextServices/TextCharactersRun.cs` | P |
| `…/ObjectRun.h` | `…/RichTextServices/ObjectRun.cs` | P |
| `…/EndOfLineRun.h` | `…/RichTextServices/EndOfLineRun.cs` | P |
| `…/EndOfParagraphRun.h` | `…/RichTextServices/EndOfParagraphRun.cs` | P |
| `…/HiddenRun.h` | `…/RichTextServices/HiddenRun.cs` | P |
| `…/DirectionalControl.h` | `…/RichTextServices/DirectionalControl.cs` | P |
| `…/DirectionalControlRun.h` | `…/RichTextServices/DirectionalControlRun.cs` | P |
| `…/TextCollapsingSymbol.h` | `…/RichTextServices/TextCollapsingSymbol.cs` | B |
| `…/TextCollapsingCharacters.h` | `…/RichTextServices/Skia/TextCollapsingCharacters.skia.cs` | B |
| `…/TextDrawingContext.h` (138) | `…/RichTextServices/TextDrawingContext.cs` + `…/Skia/SkiaTextDrawingContext.skia.cs` | B |
| `…/ILayoutEngineHost.h` | `…/RichTextServices/ILayoutEngineHost.cs` | P |
| `…/ILinkedTextContainer.h` | `…/RichTextServices/ILinkedTextContainer.cs` (defer) | P |
| `…/ITextElement.h` | `…/RichTextServices/ITextElement.cs` (trim to used) | P |
| `…/LinkedContainer.h` / `BlockLayout.h` | — (umbrella; folded) | S |
| `…/RtsInterop.h` | resolve aliases inline (FontTypeface→FontDetails) | B |

### Cluster B — BlockLayout node tree
| WinUI source | Uno target | Act |
|---|---|---|
| `BlockLayout/BlockLayoutEngine.{h,cpp}` | `Documents/BlockLayout/BlockLayoutEngine.cs` | P |
| `BlockLayout/BlockNode.{h,cpp}` (279/438) | `Documents/BlockLayout/BlockNode.cs` | P |
| `BlockLayout/BlockNodeBreak.{h,cpp}` | `Documents/BlockLayout/BlockNodeBreak.cs` | P |
| `BlockLayout/ContainerNode.{h,cpp}` (80/380) | `Documents/BlockLayout/ContainerNode.cs` | P |
| `BlockLayout/PageNode.{h,cpp}` (147/944) | `Documents/BlockLayout/PageNode.cs` | P |
| `BlockLayout/PageNodeBreak.{h,cpp}` | `Documents/BlockLayout/PageNodeBreak.cs` | P |
| `BlockLayout/ParagraphNode.{h,cpp}` (183/1441) | `Documents/BlockLayout/ParagraphNode.cs` | P |
| `BlockLayout/ParagraphNodeBreak.{h,cpp}` | `Documents/BlockLayout/ParagraphNodeBreak.cs` | P |
| `BlockLayout/ParagraphTextSource.{h,cpp}` (86/548) | `Documents/BlockLayout/ParagraphTextSource.cs` | B |
| `BlockLayout/PageHostedObjectRun.{h,cpp}` | `Documents/BlockLayout/PageHostedObjectRun.cs` | B |
| `BlockLayout/DrawingContext.{h,cpp}` | `Documents/BlockLayout/DrawingContext.cs` (portable surface only) | A |
| `BlockLayout/ContainerDrawingContext.{h,cpp}` | `Documents/BlockLayout/ContainerDrawingContext.cs` | A |
| `BlockLayout/ParagraphDrawingContext.{h,cpp}` | `Documents/BlockLayout/ParagraphDrawingContext.cs` | A |
| `BlockLayout/LineMetrics.h` | `Documents/BlockLayout/LineMetrics.cs` | P |
| `BlockLayout/BlockLayoutHelpers.{h,cpp}` (139/1064) | `Documents/BlockLayout/BlockLayoutHelpers.cs` | B |

### Cluster C — ITextView + views + break
| WinUI source | Uno target | Act |
|---|---|---|
| `core/inc/TextView.h` | `Controls/Text/Core/ITextView.skia.cs` | P |
| `text/Inc/TextBlockView.h` + `TextBlock/TextBlockView.cpp` | `Controls/Text/Core/TextBlockView.skia.cs` | P/B |
| `text/Inc/RichTextBlockView.h` + `RichTextBlock/RichTextBlockView.cpp` | `Controls/Text/Core/RichTextBlockView.skia.cs` | P/B |
| `text/Inc/LinkedRichTextBlockView.h` + `…/LinkedRichTextBlockView.cpp` | `Controls/Text/Core/LinkedRichTextBlockView.skia.cs` (defer) | P |
| `text/Inc/RichTextBlockBreak.h` + `…/RichTextBlockBreak.cpp` | `Controls/Text/Core/RichTextBlockBreak.skia.cs` | P |
| `text/Inc/PlainTextPosition.h` + `TextBox/PlainTextPosition.cpp` | `Controls/Text/Core/PlainTextPosition.skia.cs` | P |
| `text/Inc/TextPosition.h` + `common/TextPosition.cpp` | `Controls/Text/Core/TextPosition.skia.cs` | P |
| `text/Inc/LineMetricsCache.h` + `RichTextArea/LineMetricsCache.cpp` | `Controls/Text/Core/LineMetricsCache.skia.cs` | P |

### Cluster D — Text element model
| WinUI source | Uno target | Act |
|---|---|---|
| `RichTextArea/TextElement.cpp` | `Documents/TextElement.cs` + new `TextElement.TextPointers.cs` | A |
| `RichTextArea/TextElementCollection.cpp` | `Microsoft/UI/Xaml/Documents/TextElementCollection.cs` (new base) | A |
| `RichTextArea/TextPointerWrapper.cpp` | `Microsoft/UI/Xaml/Documents/TextPointerWrapper.cs` | P |
| `RichTextArea/paragraph.cpp` | `Documents/Paragraph.cs` + `Paragraph.TextContainer.cs` | A |
| `RichTextArea/TextSchema.cpp` + `Inc/TextSchema.h` | `Microsoft/UI/Xaml/Documents/TextSchema.cs` | P |
| `RichTextArea/RichTextServicesHelper.cpp` | `Microsoft/UI/Xaml/Documents/TextFormatting/RichTextServicesHelper.cs` | B |
| `TextBlock/CRun.cpp` | `Documents/Run.cs` + `Run.TextContainer.cs` | A |
| `TextBlock/Inline.cpp` | `Documents/Inline.cs` + `Span.cs`/`Bold/Italic/Underline.cs` + `Inline.TextContainer.cs` | A |
| `TextBlock/InlineCollection.cpp` (637) | `Documents/InlineCollection.cs` + `InlineCollection.TextContainer.cs` (**ITextContainer**) | A |
| `TextBlock/Hyperlink.cpp` (889) | `Documents/Hyperlink.cs` | A |
| `TextBlock/LineBreak.cpp` | `Documents/LineBreak.cs` + `LineBreak.TextContainer.cs` | A |
| `common/BlockCollection.cpp` | `Documents/BlockCollection.cs` + `BlockCollection.TextContainer.cs` | A |
| `common/InheritedProperties.cpp` (815) | `Microsoft/UI/Xaml/Documents/InheritedProperties.cs` (Typography/TextOptions **structs only**) | A |
| `common/TextFormatting.cpp` (362) | resolved-format read off DPs (snapshot struct only if needed) | A |
| `TextLayout/InlineUIContainer.cpp` + `Inc/InlineUIContainer.h` | `Microsoft/UI/Xaml/Documents/InlineUIContainer.cs` | P |
| `Inc/Span.h` (SpanVector/SpanRider) | `Microsoft/UI/Xaml/Documents/SpanVector.cs` (**only if a consumer needs it**) | P |

### Cluster E — Selection
| WinUI source | Uno target | Act |
|---|---|---|
| `common/TextSelectionManager.{cpp,h}` (3881/421) | `Controls/TextBlock/TextSelectionManager.skia.cs` (split; grippers → existing presenter) | A |
| `common/TextSelectionSettings.{cpp,h}` | `Controls/TextBlock/TextSelectionSettings.skia.cs` | P |
| `TextBox/TextSelection.cpp` + `inc/TextSelection.h` | `Controls/TextBlock/TextSelection.skia.cs` | A |
| `Inc/ITextSelection.h` | `Controls/TextBlock/IJupiterTextSelection.skia.cs` | P |
| `TextBox/PlainTextPosition.cpp` | shared with Cluster C `PlainTextPosition.skia.cs` | A |
| `common/TextPosition.cpp` + `Inc/TextPosition.h` | shared with Cluster C `TextPosition.skia.cs` | P |
| `common/SelectionWordBreaker.{cpp,h}` (714/108) | `Controls/TextBlock/SelectionWordBreaker.skia.cs` (back with Uno `Unicode.skia.cs`) | A |

### Cluster F — Text highlight
| WinUI source | Uno target | Act |
|---|---|---|
| `components/text/inc/HighlightRegion.h` | `Documents/HighlightRegion.cs` | P |
| `components/text/{inc/TextHighlightMerge.h,TextHighlightMerge.cpp}` | `Documents/TextHighlightMerge.cs` | P |
| `components/text/{inc/TextHighlightRenderer.h,TextHighlightRenderer.cpp}` | `Documents/TextHighlightRenderer.skia.cs` | P |
| `components/text/{inc/TextHighlighter.h,TextHighlighter.cpp}` | `Documents/TextHighlighter.cs` | A |
| `components/text/{inc/TextHighlighterCollection.h,…cpp}` | `Documents/TextHighlighterCollection.cs` | P |
| `components/text/{inc/TextRangeCollection.h,…cpp}` | `Documents/TextRangeCollection.cs` | P |
| `components/text/inc/TextCommon.h` | — (empty) | S |
| `components/text/inc/IFocusable.h` | reuse `Input/Internal/IFocusable.cs` | A |
| `components/text/{inc/FocusableHelper.h,FocusableHelper.cpp}` | reuse `Input/Internal/FocusableHelper.cs` | A |

### Cluster G — Controls + overflow + automation peers
| WinUI source | Uno target | Act |
|---|---|---|
| `core/inc/RichTextBlock.h` + `text/RichTextBlock/RichTextBlock.cpp` (3631) | `Controls/RichTextBlock/RichTextBlock{.cs,.Properties.cs,.skia.cs}` | A |
| `core/inc/RichTextBlockOverflow.h` + `…/RichTextBlockOverflow.cpp` (2316) | `Controls/RichTextBlock/RichTextBlockOverflow{.cs,.Properties.cs,.skia.cs}` | A |
| `RichTextBlockAutomationPeer.{h,cpp}` (core + `_Partial`) | `Automation/Peers/RichTextBlockAutomationPeer.cs` | A |
| `RichTextBlockOverflowAutomationPeer.{h,cpp}` (core + `_Partial`) | `Automation/Peers/RichTextBlockOverflowAutomationPeer.cs` | A |
| `lib/RichTextBlock_Partial.{h,cpp}` | folded into `RichTextBlock.cs` | A |
| `lib/RichTextBlockOverflow_Partial.{h,cpp}` | folded into `RichTextBlockOverflow.cs` | A |

### Cluster H — Keep-Uno-specific (reference only; do not port from C++)
`Documents/TextFormatting/{FontDetails,RenderLine,RenderSegmentSpan,Segment,GlyphInfo}.skia.cs`, `Documents/ParsedText.skia.cs`, `Documents/Inline.skia.cs`, `Controls/TextBlock/TextBlock.skia.cs` — **K**. Add only small accessors the bridge needs.

---

## 4. Existing-Uno reconciliation

| Uno asset (today) | Disposition |
|---|---|
| `Documents/TextElement.cs` (inherited DPs via `FrameworkPropertyMetadataOptions.Inherits`, `GetContainingFrameworkElement`) | **Keep DP-inherit mechanism**; **add** text-pointer/position layer as `TextElement.TextPointers.cs` partial (`GetContentStart/End/ElementStart/End`, `GetOffsetForEdge`, `GetPositionCount`, `ElementEdge` enum). Do **not** port WinUI's gen-counter inheritance cache. |
| `Documents/InlineCollection.cs` (`IList<Inline>` over `DependencyObjectCollection`, `TraversedTree` preorder/leaf cache) | **Keep storage + TraversedTree** (the `leafTree` is exactly the `TextSource` backing). **Add** `ITextContainer` as `InlineCollection.TextContainer.cs` (`CachePositionCounts`/`GetPositionCount`/`GetRun`/`GetText`/`GetContainingElement`/`GetElementEdgeOffset`/`AppendText`). **THE central element-model deliverable.** |
| `Documents/BlockCollection.cs`, `Paragraph.cs`, `Block.cs`, `Run.cs`, `Inline.cs`, `Span.cs`, `LineBreak.cs`, `Bold/Italic/Underline.cs` | **Keep DP surface**; add `.TextContainer.cs` partials with `GetRun`/`GetPositionCount` (Run emits its text; LineBreak emits U+2028; Span/Hyperlink recurse; Paragraph adds EOP; BlockCollection adds inter-paragraph separators). Wire invalidation into the new BlockLayout tree. |
| `Documents/Hyperlink.cs` (Click/Navigate/pointer states/foreground already done, `EventHandler`-based) | **Keep event mechanism** (never regress to `event Action`). **Add** `GetTextContentStart/End` pointer accessors + `UnderlineVisible` resource-directive cache. Reshape toward `CSpan` structure. |
| `Documents/TextHighlighter.cs` (Foreground/Background Brush DPs, `ObservableCollection<TextRange>`) | **Replace** `ObservableCollection` with owner-aware `TextRangeCollection`; **add** SolidColorBrush validation (WinUI `ERROR_TEXTHIGHLIGHTER_NOSOLIDCOLORBRUSH`) + invalidation chain (Ranges→`InvalidateTextRanges`→collection `OnCollectionChanged`→control `InvalidateRender`). |
| `Documents/TextRange.cs` (`StartIndex`, `Length`) | **Keep public WinRT-projected names**; it is the `TextRangeData` equivalent. |
| `Documents/ParsedText.skia.cs` + `IParsedText.skia.cs` + `TextFormatting/*` | **Keep untouched**; these are the bridge **implementation target**. Confirm the **3-arg `Draw` overload** (no `compositionRange`) that `RichTextBlock.skia.cs` calls exists alongside the 4-arg `IParsedText.Draw`. |
| `Controls/RichTextBlock/RichTextBlock.skia.cs` (`ParagraphLayout` record, `ParseAllParagraphs` flat loop, `\r\n`=2 inter-paragraph offset, `MaxLines`, `RichTextVisual`) | **Heaviest reconciliation.** `_paragraphLayouts`/`ParseAllParagraphs` **replaced** by `BlockLayoutEngine`→`PageNode`→`ParagraphNode`. **Keep** the `ParsedText.Draw` call (now driven by the node tree's `Draw`), `RichTextVisual`, and selection/pointer/hyperlink/flyout logic. |
| `Controls/RichTextBlock/RichTextBlock.cs` (`Range` struct, `Blocks`, hyperlink tracking, `GetPlainText`, `CopySelectionToClipboard`, `SelectAll`, AP) | **Reshape** toward `RichTextBlock_Partial.cpp` (own `TextSelectionManager`, `ITextView`, `BlockLayoutEngine`) while preserving public API. `Range` superseded by `TextSelection`/`TextSelectionManager`. **Add** `ContentStart/End/SelectionStart/End/Select/GetPositionFromPoint` returning `TextPointer`, `BaselineOffset`. |
| `Controls/RichTextBlock/RichTextBlockOverflow.cs` (near-empty stub; `OnOverflowContentTargetChanged` TODO in `RichTextBlock.Properties.cs`) | **Mostly net-new**: linked-chain layer (`SetupLinkedBlockLayout` consuming master's `RichTextBlockBreak`, static `InvalidateAllOverflow*` walkers, `ContentSource`). **Deferred** behind core RTB. |
| `Controls/TextBlock/TextBlock.skia.cs` (uses `UnicodeText` as `IParsedText`, measure→`ParseText`, hit-test→`GetIndexAt`) | **Reference precedent only.** Don't port from C++; mirror its boundary-call pattern. Refactor shared helpers carefully. |
| `Input/Internal/{IFocusable,FocusableHelper}.cs` | **Already aligned.** Add `ElementSoundMode`/`IsTabStop`/DO-accessor only if a consumer needs them. |
| `Documents/TextSelectionGripperPresenter.skia.cs` | **Keep**; touch grippers from `TextSelectionManager` map here (don't re-port gripper logic). |

---

## 5. Dependency-ordered STAGES

> Build target each stage: `cd src; dotnet build Uno.UI-Skia-only.slnf --no-restore -p:UnoTargetFrameworkOverride=net10.0 -p:UnoFastDevBuild=true`. "Done" = compiles clean on Skia **and** native targets still build (the new files are `.skia.cs`/skia-guarded, so native is unaffected — verify), plus the stage's tests.

### Stage 1 — Foundation value types, enums, pure interfaces *(no deps; no boundary)*
**Files:** `CharacterHit`, `TextBounds`, `ObjectRunMetrics`, `TextBreak`, `TextLineBreak`, `TextRunCache` (abstract), `LogicalDirection`, `ElementType`, `LayoutNodeType`, `TextRunType`, `DirectionalControl`; reuse `FlowDirection`/`TextAlignment`. Highlight leaves: `HighlightRegion`, `TextHighlightMerge` (+ algorithm). Selection leaves: `FindBoundaryType` + `ISimpleTextBackend`, `IJupiterTextSelection`, `TextSelectionSettings`. Element-model leaves: `ElementEdge` enum, `TextNestingType` enum (verify owner), `TextSchema`.
**Risks:** `TextHighlightMerge` four overlap cases — `std::map` bidirectional iterator (O(1) prev / erase-returns-next) has no `SortedDictionary` equivalent; emulate with an ordered key list or manual structure. Keep the BEFORE/AFTER diagram comments verbatim.
**Done:** compiles; **carry over WinUI `TextHighlightMerge` unit tests** into `Uno.UI.UnitTests` (pure logic, `net9.0`) — fail-before/pass-after on overlap cases.

### Stage 2 — Bridge contract (abstracts) + run model *(deps: Stage 1 + KEEP-side FontDetails)*
**Files:** `TextRunProperties`, `TextParagraphProperties`, `TextRun` + subtypes (`TextCharactersRun`, `EndOfLineRun`, `EndOfParagraphRun`, `HiddenRun`, `DirectionalControlRun`, `ObjectRun` abstract), `TextSource` (abstract), `TextFormatter` (abstract), `TextLine` (abstract), `TextDrawingContext` (abstract), `TextCollapsingSymbol` (abstract), `TextFormatterCache`, `RtsInterop` alias resolution, `RichTextServicesHelper` (FlowDirection map).
**Risks:** `TextRunProperties.ForegroundBrushSource` weakref → `WeakReference<DependencyObject>` (not `xref_ptr`). `DefaultIncrementalTab=4*fontSize` vs Uno `Segment` hardcoded tab=48 — record the reconciliation decision now.
**Done:** compiles; no behavior yet (contracts only).

### Stage 3 — Skia bridge implementation *(deps: Stage 2 + ParsedText)*
**Files:** `SkiaTextFormatter.skia.cs`, `SkiaTextLine.skia.cs`, `SkiaTextDrawingContext.skia.cs`, `TextCollapsingCharacters.skia.cs`.
**Risks (highest in the project):** (a) **whole-collection vs per-line** — implement Strategy B (ParseText once, vend `RenderLine`s; mint `TextLineBreak` carrying line index; `null` on last line). (b) **Baseline sign** — `RenderLine.BaselineOffsetY` is negative-below-baseline; map carefully to WinUI `Baseline`. (c) **`Collapse`/trimming + ellipsis** — net-new on top of Skia (no Uno model today). (d) **`CharacterHit` trailing-edge** — Uno exposes only int index via `GetIndexAt`; add leading/trailing-edge resolution + cluster-aware `GetPrevious/NextCaretCharacterHit` (surrogate pairs/combining marks). (e) `ParsedText` is a **readonly struct** — beware boxing/copy when held by a class `TextLine`.
**Done:** a focused runtime test formats a single paragraph via `TextFormatter.FormatLine` loop and asserts wrapped-line count + per-line `Width/Height` **match** the current `RichTextBlock.skia.cs` flat `ParseAllParagraphs` output for the same content/width (regression oracle, since there's no paging path to regress yet). Use `/runtime-tests`.

### Stage 4 — Element-model alignment (ITextContainer backing store) *(deps: Stage 2; ITextView stubbed)*
**Files:** `ITextContainer` interface; `CPlainTextPosition`→`PlainTextPosition.skia.cs` (nav routes through `ITextView`, stubbed); `InlineCollection.TextContainer.cs` (**core**); then `.TextContainer.cs` partials for `Run`/`LineBreak`/`Inline`/`Span`/`Paragraph`/`Hyperlink`/`BlockCollection`; `InlineUIContainer.cs` (object run); `TextElementCollection.cs` base; `InheritedProperties.cs` (Typography/TextOptions structs **only**).
**Risks:** decide resolved-format strategy **here** — read Uno DP values directly, expose a small `ResolvedTextFormat` record (do **not** port WinUI gen-counter cache; biggest Uno-specific-minimization call — confirm with Martin). `Span.h` (SpanVector/SpanRider) — port **only if** a real consumer needs it; verify first. Surrogate/CRLF boundaries in `GetRun` (never split mid-cluster).
**Done:** compiles; unit tests for `InlineCollection` position/`GetRun` mapping (offsets, nested spans, inter-paragraph separators, `PlaceHolderPositionsForInlines=2` convention).

### Stage 5 — BlockLayout node tree *(deps: Stages 2–4)*
**Files (sub-order):** ① `LineMetrics`, `BlockNodeBreak`, `PageNodeBreak`, `ParagraphNodeBreak`. ② `BlockNode`→`ContainerNode`→`BlockLayoutEngine` (query API stubbed). ③ `BlockLayoutHelpers` (property+formatter bridge), `ParagraphTextSource` (GetRun over `InlineCollection`), `ParagraphNode` (FormatLine loop + line cache + stacking/trimming — **the meat**). ④ `PageNode` (block iteration, paging, embedded elements) + `PageHostedObjectRun` + `IEmbeddedElementHost`. ⑤ `DrawingContext`/`ContainerDrawingContext`/`ParagraphDrawingContext` (portable transform + foreground-highlight distribution; **drop** D2D/HWRender bodies, route glyph emission through `RichTextVisual`).
**Risks:** `ParagraphNode.FormatLineAtIndex` **must reformat at the same width** the line was measured at (metrics-cache validity). Margin-collapsing arithmetic + "measure at least one line" policy. Bitfield layout-state flags → bool fields (keep order in comments). Embedded `UIElement.Arrange` integration. `ContainerDrawingContext` highlight-rect advance distribution is load-bearing for high-contrast selection foreground — keep the algorithm.
**Done:** runtime test — `BlockLayoutEngine.Measure/Arrange` of multi-paragraph content yields identical wrapped lines/sizes to Stage 3 oracle; embedded `InlineUIContainer` reserves correct inline space.

### Stage 6 — ITextView + views + break *(deps: Stage 5 + control stubs)*
**Files:** `ITextView.skia.cs`, `LineMetricsCache.skia.cs`, `TextPosition.skia.cs`, then `TextBlockView.skia.cs` → `RichTextBlockView.skia.cs` → (defer) `LinkedRichTextBlockView.skia.cs`; `RichTextBlockBreak.skia.cs`.
**Risks:** WinUI dual-path (`TextMode::Normal` PageNode vs `DWriteLayout` fast-path) **collapses to single ParsedText path** on Uno. Reproduce `PlaceHolderPositionsForInlines=2` and the "last position on empty-break page → LineForwardCharacterBackward gravity" special-case for UIA/selection parity. Gravity derivation (trailing-hit/end-of-line) is **new** on Uno.
**Done:** runtime tests for `TextRangeToTextBounds`/`PixelPositionToTextPosition`/`IsAtInsertionPosition` parity (validate against native WinUI via `/winui-runtime-tests` where reproducible).

### Stage 7 — Selection *(deps: Stage 6)*
**Files:** `TextSelection.skia.cs` (anchor/moving + gravity), `SelectionWordBreaker.skia.cs` (back with `Unicode.skia.cs`; substitute WinRT segmenter), `TextSelectionManager.skia.cs` (**split**: CORE mouse/keyboard/SelectAll/copy/highlight-region/flyout → here; GRIPPER/touch → existing `TextSelectionGripperPresenter.skia.cs`; CARET-browsing last).
**Risks:** reshape Uno `Range(int,int)` → anchor/moving `CPlainTextPosition` + `TextGravity` (the single most structural selection change). Continue the existing `// Ported from: TextSelectionManager.cpp line NNNN` comment convention. `GetXaml` + caret-browsing may ship stubbed.
**Done:** existing TextBlock selection runtime tests stay green; new RTB selection (mouse drag, Shift+arrow, double-tap word, SelectAll, copy) tests pass.

### Stage 8 — Highlight rendering *(deps: Stages 4, 6 + render layer)*
**Files:** `TextRangeCollection.cs`, `TextHighlighter.cs` (align: validation + invalidation), `TextHighlighterCollection.cs`, `TextHighlightRenderer.skia.cs` (the only boundary-touching highlight file).
**Risks:** inclusive `[Start,End]` (merge) → exclusive `[start, end+1)` at `TextRangeToTextBounds`. Theme-resource lookup `TextControlHighlighterForeground/Background` via Uno `ResourceResolver`. Pixel-snap can be simplified on Skia.
**Done:** highlighter render runtime test; invalidation chain (Ranges edit → re-render) verified.

### Stage 9 — Controls + overflow *(deps: Stages 5–8)*
**Files:** reshape `RichTextBlock{.cs,.Properties.cs,.skia.cs}` onto BlockLayout/PageNode/TextFormatter seam (replace `ParseAllParagraphs`); then `RichTextBlockOverflow{.cs,.Properties.cs,.skia.cs}` + linked-overflow chain (`SetupLinkedBlockLayout`, static `InvalidateAllOverflow*`, `ContentSource`, wire `OnOverflowContentTargetChanged`); add `LinkedRichTextBlockView`.
**Risks:** preserve **all** working RTB behavior (selection/hyperlink/highlight/flyout/theme/high-contrast) while restructuring. Overflow chaining + `ILinkedTextContainer` is the **largest net-new** behavior. `RichTextBlockBreak` must carry enough state to resume mid-paragraph on the overflow page.
**Done:** full RTB runtime test suite + new overflow tests (text flows master→overflow, `HasOverflowContent`, `IsTextTrimmed`); visual sample in SamplesApp; WinUI parity spot-checks.

### Stage 10 — Automation peers *(deps: Stage 9 + TextAdapter cross-cluster)*
**Files:** finish `RichTextBlockAutomationPeer.cs` / `RichTextBlockOverflowAutomationPeer.cs` — `GetPatternCore` (Text pattern via `TextAdapter`/`ITextProvider`) + recursive `GetChildrenCore` (`TextElement.AppendAutomationPeerChildren`).
**Risks:** gated on `TextAdapter`/`ITextProvider` port (cross-cluster, may itself be deferred). Overflow peer gets Text pattern only when `master != null`.
**Done:** AP tests; peers already tagged 1.8.4 — just remove the TODO stubs.

### Stage 11 — Tests & samples sweep
Carry remaining WinUI unit tests; add SamplesApp pages (`/add-sample`); `dotnet xstyler -d src/SamplesApp -r`; run full `Uno.UI.UnitTests` + RTB/TextBlock runtime suites on Skia and WinUI.

---

## 6. Risk register (hardest mappings)

| # | Risk | Mitigation |
|---|---|---|
| R1 | **Per-line `FormatLine` + `TextLineBreak` continuation vs Uno whole-collection `ParseText`** | Strategy B: `ParseText` once per paragraph, vend `RenderLine`s sequentially, `TextLineBreak` carries next-line index, `null` terminates. Document in `SkiaTextFormatter`. Revisit only if paging needs true mid-line resume. |
| R2 | **`Collapse`/text-trimming + ellipsis model** (no Uno equivalent) | New `TextCollapsingSymbol`/`TextCollapsingCharacters` producing ellipsis glyphs from `FontDetails`/`SKFont`; `TextLine.Collapse` truncates the wrapped `RenderLine`. Net-new; isolate in Stage 3. |
| R3 | **`CharacterHit` cluster-aware caret nav + per-range `GetTextBounds`** | Uno exposes int index + single `GetRectForIndex`. Add trailing-length/leading-vs-trailing-edge resolution and surrogate/combining-mark stepping atop `GetIndexAt`/`GetLineAt`/`RenderSegmentSpan` cluster data. |
| R4 | **`CDependencyObject`/`CValue` property system** | Use `[GeneratedDependencyProperty]`; keep Uno's `FrameworkPropertyMetadataOptions.Inherits` for text inheritance (do **not** port the gen-counter cache). `CValue` → plain values. |
| R5 | **`xref_ptr`/`com_ptr`/`weakref_ptr` lifetime + `TextObject` refcount** | Drop entirely (GC). Brush-source weakrefs → `WeakReference<DependencyObject>`. Break/cache "AddRef on store" → plain references. |
| R6 | **`HRESULT`/`IFC`/goto-Cleanup/`Result::Enum`** | Methods that returned `Result::Enum` → `void`/value-returning that **throw** on error. `RichTextServicesHelper.MapTxErr` evaporates; keep only the `FlowDirection` map. |
| R7 | **`ILinkedTextContainer` overflow chaining** | Largest net-new orchestration. **Defer** to Stage 9; port minimal `ILinkedTextContainer`/`ITextElement` stubs earlier so signatures compile. `RichTextBlockBreak` is the only overflow state with no Uno analogue. |
| R8 | **`TextSelectionManager` gripper/touch + caret-browsing** | **Keep-Uno-specific / defer.** CORE selection reshapes onto the WinUI structure; grippers map to existing `TextSelectionGripperPresenter.skia.cs`; caret-browsing is lowest priority. |
| R9 | **Measure/Arrange integration with Uno layout** | `ParagraphNode.FormatLineAtIndex` reformats at the measured width; node-tree `Measure/Arrange` must slot into `FrameworkElement.MeasureOverride/ArrangeOverride` and keep the existing `RichTextVisual` draw path. Validate sizes against the flat-output oracle before adding paging. |
| R10 | **Tab width discrepancy** (`4*fontSize` vs Uno `Segment` tab=48) + **baseline sign** | Resolve in Stage 2/3; add a parity test. |
| R11 | **`PlaceHolderPositionsForInlines=2` + gravity special-cases** | Reproduce exactly in the views for UIA/selection offset parity; cover with tests against native WinUI. |
| R12 | **`ParsedText` readonly-struct boxing** when wrapped by class `TextLine` | Hold the `IParsedText`/index, not a copied struct, inside `SkiaTextLine`. |

---

## 7. Recommended first concrete commits (smallest leaf files, compile against Uno today)

These are pure value types/enums/algorithms with **zero** dependency on the bridge or layout tree — each is an independently buildable, reviewable commit, and several unblock everything downstream:

1. **`feat: Add RichTextServices value types`** — `CharacterHit.cs`, `TextBounds.cs`, `ObjectRunMetrics.cs` (readonly record structs; `TextBounds` uses public `FlowDirection` + `Windows.Foundation.Rect`). Namespace `Microsoft.UI.Xaml.Documents.RichTextServices`. No deps.
2. **`feat: Add RichTextServices enums`** — `ElementType.cs`, `LayoutNodeType.cs`, `TextRunType.cs`, `DirectionalControl.cs`, `LogicalDirection.cs`. Pure enums, port verbatim.
3. **`feat: Port TextHighlightMerge overlap algorithm`** — `HighlightRegion.cs` + `TextHighlightMerge.cs` **with** the carried-over WinUI unit tests in `Uno.UI.UnitTests` (no visual tree). Highest immediate ROI: locks the four overlap cases with a fail-before/pass-after suite and validates the `std::map`→ordered-collection translation.
4. **`feat: Add selection word-break leaves`** — `FindBoundaryType` enum + `ISimpleTextBackend` interface (from `SelectionWordBreaker.h`). Tiny; referenced by `TextSelectionManager`.
5. **`feat: Add TextBreak/TextLineBreak/TextRunCache abstracts`** — `TextBreak.cs` (virtual `Equals`), `TextLineBreak.cs` (empty subclass), `TextRunCache.cs` (abstract `Clear`). Unblocks the bridge contract in Stage 2.

Each builds clean via `dotnet build Uno.UI-Skia-only.slnf` with **no** changes to existing files, lands behind no feature flag (unused internal types), and respects the encoding/header rules.
