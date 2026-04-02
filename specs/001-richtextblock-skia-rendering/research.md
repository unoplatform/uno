# Research: RichTextBlock Skia Rendering (Phase 1)

## R1: Can UnicodeText be reused for RichTextBlock?

**Decision**: Yes, reuse directly with no modifications.

**Rationale**: UnicodeText's constructor accepts `Inline[] inlines` (leaf nodes from `InlineCollection.TraversedTree.leafTree`). RichTextBlock's content model for a single Paragraph produces the exact same data structure — an array of leaf `Inline` nodes (Run, LineBreak). UnicodeText has no coupling to TextBlock beyond the `IFontCacheUpdateListener` callback interface.

**Alternatives considered**:
- Custom rendering pipeline for RichTextBlock blocks: Rejected. Would duplicate ~1554 lines of complex BiDi, line breaking, HarfBuzz shaping, and layout code. Only needed for multi-paragraph spacing (Phase 2), not Phase 1.

**Key evidence**:
- `UnicodeText` constructor signature: `UnicodeText(Size, Inline[], FontDetails, int maxLines, float lineHeight, LineStackingStrategy, FlowDirection, TextAlignment?, TextWrapping, bool isSpellCheckEnabled, IFontCacheUpdateListener, out Size)`
- TextBlock passes `Inlines.TraversedTree.leafTree` — RichTextBlock passes `paragraph.Inlines.TraversedTree.leafTree`
- UnicodeText extracts text, font properties, and foreground from each Inline independently

## R2: How does the DependencyProperty parent chain work for property inheritance?

**Decision**: Use `DependencyObjectCollection.SetParent()` to establish the chain: RichTextBlock → (via BlockCollection) → Paragraph → (via InlineCollection) → Run/Span.

**Rationale**: Uno's `DependencyObjectCollection<T>` propagates its parent to items added to the collection. When `BlockCollection.SetParent(richTextBlock)` is called, any Paragraph added to the collection gets RichTextBlock as its DP parent. InlineCollection already does this for Paragraph → Inline items.

Font properties (FontFamily, FontSize, FontWeight, FontStyle, FontStretch) on TextElement use `FrameworkPropertyMetadataOptions.Inherits`, which walks up the DP parent chain. So a Run with no explicit FontSize will inherit from its Paragraph, which inherits from the RichTextBlock.

**Alternatives considered**:
- Manually propagating properties at measurement time: Rejected. Would duplicate the DP inheritance system and miss dynamic property changes.
- Setting properties directly on Inlines from RichTextBlock: Rejected. Violates WinUI's property model where explicit values on Inlines override inherited values.

**Key evidence**:
- TextBlock registers FontStyle with `Inherits | AffectsMeasure`: `new FrameworkPropertyMetadata(FontStyle.Normal, Inherits | AffectsMeasure, ...)`
- InlineCollection constructor: `_collection.SetParent(parent)` — items inherit from parent
- TextElement already has FontFamily/FontSize/FontWeight/FontStyle/FontStretch as DependencyProperties

## R3: What is the correct invalidation chain for content changes?

**Decision**: Three-point fix in existing code plus new methods on Paragraph and RichTextBlock.

**Rationale**: The current invalidation chain (`Inline.InvalidateInlines` → `Span` or `TextBlock`) was designed before RichTextBlock had content. Adding `Paragraph` as a recognized parent type bridges the chain.

**Fix points**:
1. `Inline.InvalidateInlines()`: Add `case Paragraph paragraph: paragraph.InvalidateInlines(updateText); break;`
2. `InlineCollection.OnCollectionChanged()`: Add `case Paragraph paragraph: paragraph.InvalidateInlines(true); break;`
3. `Paragraph`: Add `internal void InvalidateInlines(bool updateText)` that propagates to `RichTextBlock`
4. `RichTextBlock.skia.cs`: Add `internal void InvalidateForContentChange()` that calls `InvalidateMeasure()` and invalidates render

**Alternatives considered**:
- Event-based subscription model (Paragraph publishes event, RichTextBlock subscribes): Rejected. Adds complexity and subscription management. The parent-chain walk is simpler and follows the existing pattern.

## R4: What are the correct default values for RichTextBlock DependencyProperties?

**Decision**: Use WinUI 3 defaults, verified against WinUI C++ source and docs.

| Property | Default | Notes |
|----------|---------|-------|
| FontFamily | (inherited/system default) | Null = system default font |
| FontSize | 14.0 | WinUI default for RichTextBlock |
| FontWeight | Normal (400) | `FontWeights.Normal` |
| FontStyle | Normal | `FontStyle.Normal` |
| FontStretch | Normal | `FontStretch.Normal` |
| Foreground | (inherited/system default) | Null = system foreground from theme |
| TextWrapping | **Wrap** | **Different from TextBlock (NoWrap)!** |
| TextTrimming | None | `TextTrimming.None` |
| TextAlignment | Left | `TextAlignment.Left` |
| HorizontalTextAlignment | Left | Mirrors TextAlignment |
| Padding | 0,0,0,0 | `default(Thickness)` |
| MaxLines | 0 | 0 = unlimited |
| LineHeight | 0 | 0 = automatic from font metrics |
| LineStackingStrategy | MaxHeight | `LineStackingStrategy.MaxHeight` |
| CharacterSpacing | 0 | 0 = no extra spacing |
| TextDecorations | None | `TextDecorations.None` |
| IsTextSelectionEnabled | false | Not implemented in Phase 1 |

**Key difference**: RichTextBlock.TextWrapping defaults to `Wrap` while TextBlock defaults to `NoWrap`.

## R5: How should the composition visual be set up?

**Decision**: Create `RichTextBlockTextVisual` modeled on `TextVisual`, overriding `CreateElementVisual()`.

**Rationale**: TextBlock uses a dedicated `TextVisual : ContainerVisual` that holds a `WeakReference<TextBlock>` and delegates `Paint()` to `TextBlock.Draw()`. RichTextBlock needs the same pattern. The visual is created via `CreateElementVisual()` override.

**Key pattern from TextBlock.skia.cs**:
```csharp
private protected override ContainerVisual CreateElementVisual() => new TextVisual(Compositor.GetSharedCompositor(), this);
```

RichTextBlock will use an identical pattern with its own `RichTextBlockTextVisual`.

## R6: How to handle the Block base class being fully NotImplemented?

**Decision**: The generated `Block.cs` constructor is guarded with `[NotImplemented]` for all platforms including Skia. Since Paragraph inherits Block, and Block's constructor raises `TryRaiseNotImplemented`, this would log warnings. However, `TryRaiseNotImplemented` is a warning, not an exception — the code continues. For Phase 1, we accept the warning. A clean fix would be to create a hand-written `Block.cs` that provides a proper constructor, but this is a minor concern that can be addressed separately.

**Decision update**: Actually, examine the code more carefully — if `Paragraph : Block` and Block's generated constructor raises NotImplemented, then creating a Paragraph already triggers this warning in the current codebase. This means the warning is already present for anyone using `<Paragraph>` in XAML. Phase 1 should fix this by creating a hand-written Block constructor or removing the `__SKIA__` guard from the generated constructor.

## R7: BlockCollection parent setup

**Decision**: In `RichTextBlock.cs` constructor, call `Blocks.SetParent(this)` and subscribe to `Blocks.VectorChanged` for invalidation.

**Rationale**: Currently `BlockCollection` is created without a parent reference. This means Paragraphs added to it don't have RichTextBlock in their DP parent chain, breaking both property inheritance and invalidation.

**Fix**: `Blocks.SetParent(this)` in constructor + `Blocks.VectorChanged += OnBlocksChanged` for collection change notifications.
