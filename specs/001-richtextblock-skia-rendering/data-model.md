# Data Model: RichTextBlock Skia Rendering (Phase 1)

## Entity Relationship

```
RichTextBlock (FrameworkElement)
  │
  ├── Blocks: BlockCollection (DependencyObjectCollection<Block>)
  │     │
  │     └── Paragraph (Block : TextElement)
  │           │
  │           ├── Inlines: InlineCollection
  │           │     │
  │           │     ├── Run (Inline : TextElement)
  │           │     │     └── Text: string
  │           │     │
  │           │     ├── Span (Inline : TextElement)
  │           │     │     └── Inlines: InlineCollection (recursive)
  │           │     │
  │           │     ├── Bold (Span) — sets FontWeight=Bold
  │           │     │
  │           │     ├── Italic (Span) — sets FontStyle=Italic
  │           │     │
  │           │     └── Underline (Span) — sets TextDecorations=Underline
  │           │
  │           └── TextIndent: double
  │
  └── [DependencyProperties: Font*, Text*, Layout*]
```

## DependencyProperty Parent Chain (for inheritance)

```
RichTextBlock  ← BlockCollection.SetParent(this)
  └── Paragraph  ← DependencyObjectCollection propagates parent to items
        └── Run/Span  ← InlineCollection._collection.SetParent(paragraph)
              └── (nested Span children)  ← InlineCollection._collection.SetParent(span)
```

## Invalidation Chain

```
Content change (Run.Text, Inlines.Add, etc.)
  → Inline.InvalidateInlines(updateText: true)
    → parent is Span? → Span.InvalidateInlines(updateText) → recurse up
    → parent is Paragraph? → Paragraph.InvalidateInlines(updateText) [NEW]
      → parent is RichTextBlock? → RichTextBlock.InvalidateForContentChange() [NEW]
        → InvalidateMeasure()
        → Visual.Compositor.InvalidateRender(Visual)

Property change (FontSize, TextWrapping, etc.)
  → DependencyProperty callback
    → RichTextBlock.InvalidateMeasure() (via AffectsMeasure)
    → RichTextBlock.InvalidateInlineAndRequireRepaint() (for render-only changes)
```

## Rendering Pipeline Data Flow

```
MeasureOverride(availableSize)
  │
  ├── padding = Padding
  ├── availableSizeWithoutPadding = availableSize - padding
  │
  ├── paragraph = Blocks.OfType<Paragraph>().FirstOrDefault()
  ├── leafInlines = paragraph?.Inlines.TraversedTree.leafTree ?? []
  │
  ├── defaultFontDetails = GetDefaultFontDetails()
  │   └── FontDetailsCache.GetFont(FontFamily, FontSize, FontWeight, FontStretch, FontStyle)
  │
  └── ParsedText = new UnicodeText(
  │       availableSizeWithoutPadding,
  │       leafInlines,              ← Inline[] from Paragraph
  │       defaultFontDetails,       ← fallback font from RichTextBlock
  │       MaxLines,
  │       LineHeight,
  │       LineStackingStrategy,
  │       FlowDirection,
  │       TextAlignment,
  │       TextWrapping,
  │       isSpellCheckEnabled: false,
  │       fontCacheListener: this,
  │       out desiredSize)
  │
  └── return desiredSize + padding

ArrangeOverride(finalSize)
  │
  ├── Re-parse text with final size (same as MeasureOverride)
  ├── InvalidateRender(Visual)
  └── return finalSize

Draw(PaintingSession)
  │
  ├── canvas.Translate(Padding.Left, Padding.Top)
  ├── ParsedText.Draw(session, caret: null, highlighters: empty)
  └── canvas.Restore()
```

## DependencyProperties (Phase 1 Scope)

### Font Properties (Inherits + AffectsMeasure)

| Property | Type | Default | DP Options |
|----------|------|---------|------------|
| FontFamily | FontFamily | null (system default) | Inherits, AffectsMeasure |
| FontSize | double | 14.0 | Inherits, AffectsMeasure |
| FontWeight | FontWeight | Normal | Inherits, AffectsMeasure |
| FontStyle | FontStyle | Normal | Inherits, AffectsMeasure |
| FontStretch | FontStretch | Normal | Inherits, AffectsMeasure |
| Foreground | Brush | null (theme default) | Inherits |
| CharacterSpacing | int | 0 | Inherits, AffectsMeasure |
| TextDecorations | TextDecorations | None | Inherits |

### Layout Properties (AffectsMeasure)

| Property | Type | Default | DP Options |
|----------|------|---------|------------|
| TextWrapping | TextWrapping | **Wrap** | AffectsMeasure |
| TextTrimming | TextTrimming | None | AffectsMeasure |
| TextAlignment | TextAlignment | Left | AffectsMeasure |
| HorizontalTextAlignment | TextAlignment | Left | AffectsMeasure |
| Padding | Thickness | 0,0,0,0 | AffectsMeasure |
| MaxLines | int | 0 | AffectsMeasure |
| LineHeight | double | 0 | AffectsMeasure |
| LineStackingStrategy | LineStackingStrategy | MaxHeight | AffectsMeasure |

### Read-Only / State Properties

| Property | Type | Default | Notes |
|----------|------|---------|-------|
| IsTextTrimmed | bool | false | Computed after arrange |

### Deferred to Later Phases

| Property | Phase | Reason |
|----------|-------|--------|
| IsTextSelectionEnabled | Phase 4 | Selection requires pointer handling |
| SelectionHighlightColor | Phase 4 | Selection rendering |
| SelectedText | Phase 4 | Selection state |
| SelectionFlyout | Phase 4 | Selection UI |
| OverflowContentTarget | Phase 5 | RichTextBlockOverflow |
| HasOverflowContent | Phase 5 | RichTextBlockOverflow |
| ContentStart/ContentEnd | Phase 6 | TextPointer |
| SelectionStart/SelectionEnd | Phase 6 | TextPointer |
| TextHighlighters | Phase 3 | Text highlighting |
| IsColorFontEnabled | Phase 3+ | Color font support |
| IsTextScaleFactorEnabled | Phase 3+ | Text scaling |
| OpticalMarginAlignment | Phase 3+ | Advanced typography |
| TextLineBounds | Phase 3+ | Advanced typography |
| TextReadingOrder | Phase 3+ | BiDi enhancement |
| TextIndent | Phase 2 | Multi-paragraph |

## Files Modified/Created

### New Files

| File | Purpose |
|------|---------|
| `RichTextBlock.Properties.cs` | ~16 DependencyProperty registrations with defaults and callbacks |
| `RichTextBlock.skia.cs` | MeasureOverride, ArrangeOverride, Draw, CreateElementVisual, invalidation |
| `RichTextBlockTextVisual.skia.cs` | ContainerVisual subclass for composition rendering |
| `Given_RichTextBlock.cs` | Runtime tests |

### Modified Files

| File | Change |
|------|--------|
| `RichTextBlock.cs` | Remove `[Uno.NotImplemented]` from class and constructor, add `Blocks.SetParent(this)`, subscribe to `Blocks.VectorChanged` |
| `Inline.cs` | Add `case Paragraph` to `InvalidateInlines` switch |
| `InlineCollection.cs` | Add `case Paragraph` to `OnCollectionChanged` switch |
| `Paragraph.cs` | Add `InvalidateInlines(bool)` method |
| Generated `RichTextBlock.cs` | Remove `__SKIA__` from `#if` guards for implemented properties |
