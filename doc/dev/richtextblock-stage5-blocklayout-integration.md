# Stage 5 (BlockLayout) — integration notes & pickup plan

The BlockLayout node tree has been **drafted** (faithful C++→C# ports) but is **not yet
buildable**: the nodes depend on element-model accessors that belong to **Stage 4**
(ITextContainer / text pointers), which is not done. So the correct order is **Stage 4
first, then wire Stage 5**. The drafts are parked under `doc/dev/stage5-wip/` (not compiled)
until the dependencies exist.

## What's drafted (in `doc/dev/stage5-wip/`)
- `BlockNode.skia.cs` — **lead-ported, believed correct** (base node: Measure/Arrange/Draw
  wrappers, margin collapsing, layout bypass, dirty flags, TransformOffsetTo/FromRoot).
  Keeps the C++ field names (`m_desiredSize`, `m_margin`, …) so the node drafts integrate.
- `PageNode.skia.cs`, `PageHostedObjectRun.skia.cs` — subagent draft (root container +
  embedded-object run).
- `ParagraphNode.skia.cs`, `ParagraphTextSource.skia.cs` — subagent draft (leaf paragraph
  node that drives the `SkiaTextFormatter.FormatLine` loop + builds line metrics; the text
  source implements `ISkiaParagraphSource`).
- Still to port (small): `ContainerNode.skia.cs` (have the .cpp), `BlockLayoutEngine.skia.cs`,
  the minimal `DrawingContext`/`ContainerDrawingContext`/`ParagraphDrawingContext`, a
  `TextGravity` enum, and a `BlockLayoutHelpers.skia.cs` (subset of the 1064-line C++:
  GetBlockMargin, GetFlowDirection, IsCloseReal, GetLineHeight/Stacking/Alignment/Wrapping/
  Trimming/MaxLines, GetPagePadding, GetTextParagraphProperties, GetTextFormatter →
  SkiaTextFormatter.Instance, baseline/font helpers).

## Rendering decision (settled)
The dxaml D2D/HWRender `DrawingContext` pipeline is **dropped**. The node tree computes
layout (sizes, offsets, and a cached `ParsedText`/line metrics per paragraph) in
Measure/Arrange; **rendering is a Paint-walk**: `RichTextBlock.skia.cs`'s `RichTextVisual.Paint`
walks the arranged tree and draws each `ParagraphNode`'s `ParsedText` at its root-relative
offset (via `SkiaTextLine.ParsedText` — add an internal accessor). `Draw(forceDraw)`/`DrawCore`
are ported structurally but are not the active glyph path.

## Stage-4 accessors the node drafts require (the integration glue)
From the subagents' TODO reports:
- `BlockCollection`: `GetCollection()`, `GetCount()`, `GetItemWithAddRef(index)` → indexer,
  `GetElementEdgeOffset(container, ElementEdge.ElementStart, out offset, out found)`.
- `Paragraph.GetInlineCollection()`; `InlineCollection` position/run accessors (ITextContainer:
  `GetPositionCount`, `GetRun`, char-offset mapping) — the **core Stage-4 deliverable**.
- `InlineUIContainer`: `GetChild()`, `GetCachedHost()/ClearCachedHost()`,
  `SetChildLayoutCache(w,h,baseline)`, `GetChildLayoutCache(out w,out h,out baseline)`,
  `EnsureAttachedToHost()/EnsureDetachedFromHost()`.
- `FrameworkElement/UIElement`: `AddChild/RemoveChild`, `GetParentInternal()`, `Arrange(Rect)`,
  `HasLayoutStorage()`/desired-size access — likely thin Uno wrappers/extensions.
- `TextBlockViewHelpers.FindIUCPositionInInlines(inlines, iuc, ref pos)`.
- `ElementEdge` enum, `TextGravity` enum.

## Draft reconciliation TODOs (lead)
- `BlockNode.Measure` C# signature uses `out MarginCollapsingState mcsOut` (no HRESULT);
  the PageNode draft assumes an extra `out mcs` + a `Result` return — reconcile to the
  exception model. Drop `Result.INTERNAL_ERROR` special-case (note it).
- Replace the subagents' synthesized placeholders `DeleteBlockNode(node)` (C++ `delete`) and
  `ThrowOnRtsError`/`TxerrFromXResult` with the GC/exception model.
- Confirm `ObjectRunMetrics` positional ctor order (Width, Height, Baseline) and `ObjectRun`
  signatures (`HasFixedSize`, `Format`, `ComputeBoundingBox`, `Arrange` — currently abstract,
  return `void`/values not `Result`).
- Query methods (`IsAtInsertionPosition`/`PixelPositionToTextPosition`/`GetTextBounds`) call
  `TextLine` hit-test methods that throw until **Stage 6** — fine to leave; they light up then.

## Pickup order
1. Stage 4 element model (ITextContainer on InlineCollection + the accessors above).
2. Port `ContainerNode`, `BlockLayoutEngine`, minimal `DrawingContext`s, `TextGravity`,
   `BlockLayoutHelpers` subset.
3. Move the drafts back into `src/Uno.UI/UI/Xaml/Documents/BlockLayout/`, reconcile TODOs,
   build (`Uno.UI.Skia`), fix.
4. Wire `RichTextBlock.skia.cs`: `MeasureOverride`→`BlockLayoutEngine.CreatePageNode`+`Measure`;
   `ArrangeOverride`→`PageNode.Arrange`; `RichTextVisual.Paint`→tree-walk `ParsedText.Draw`.
   Validate against the existing `Given_RichTextBlock` runtime tests (the regression oracle).
