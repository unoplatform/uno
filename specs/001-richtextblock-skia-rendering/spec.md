# Feature Specification: RichTextBlock WinUI Port & Skia Rendering

**Feature Branch**: `001-richtextblock-skia-rendering`
**Created**: 2026-02-12
**Status**: Draft
**Input**: Port the WinUI C++ RichTextBlock implementation to Uno Platform C# following the WinUI Porting Agent conventions, then connect the ported API to the existing Skia text rendering pipeline.

## Context

RichTextBlock is a core WinUI 3 control for displaying formatted text with support for multiple paragraphs, inline formatting, hyperlinks, and text selection. It is currently unimplemented (stubbed with `[Uno.NotImplemented]`) in Uno Platform for Skia targets, with only a skeleton `RichTextBlock.cs` and basic document model classes in place.

This specification describes a **porting-first approach**: the primary goal is to faithfully port the WinUI C++ source code to C# following the conventions in `.github/agents/winui-porting-agent.md`. The porting is done in two layers:

1. **Layer 1 — API Surface Port**: Port the class declarations, dependency properties, property-changed callbacks, and event wiring from the WinUI C++ headers and property files. This produces a complete, compilable API surface that matches WinUI.
2. **Layer 2 — Rendering Integration**: Connect the ported API to Uno Platform's existing Skia text rendering pipeline (UnicodeText/HarfBuzz/SkiaSharp) to make the control actually render text. This layer bridges the gap between WinUI's internal text engine (BlockLayout, TextFormatter) and Uno's existing infrastructure.

### WinUI C++ Source Files (Reference)

The WinUI sources at `D:\Work\microsoft-ui-xaml2` contain the following key files for RichTextBlock:

| WinUI C++ File | Contents |
|----------------|----------|
| `src/dxaml/xcp/core/inc/RichTextBlock.h` | CRichTextBlock class declaration, fields, method signatures |
| `src/dxaml/xcp/core/core/elements/RichTextBlock.cpp` | CRichTextBlock implementation (constructor, property changes, measure/arrange) |
| `src/dxaml/xcp/core/inc/BlockTextElement.h` | CBlock, CParagraph, CBlockCollection class declarations |
| `src/dxaml/xcp/core/text/common/BlockCollection.cpp` | CBlockCollection implementation |
| `src/dxaml/xcp/core/text/BlockLayout/` | Block layout engine (BlockLayoutEngine, BlockNode, ParagraphNode, etc.) |
| `src/dxaml/xcp/core/text/RichTextBlock/` | RichTextBlock-specific text processing |
| `src/dxaml/xcp/dxaml/lib/RichTextBlock_Partial.cpp/.h` | DXAML-layer RichTextBlock partial (WinRT API surface, events) |
| `src/dxaml/xcp/dxaml/lib/Span_Partial.cpp/.h` | Span partial implementation |
| `src/dxaml/xcp/dxaml/lib/winrtgeneratedclasses/Bold.g.*` | Bold generated WinRT wrapper |
| `src/dxaml/xcp/dxaml/lib/winrtgeneratedclasses/Italic.g.*` | Italic generated WinRT wrapper |
| `src/dxaml/xcp/dxaml/lib/winrtgeneratedclasses/Underline.g.*` | Underline generated WinRT wrapper |

### Existing Uno Platform State

The following Uno files already exist with partial implementations:

| File | Current State |
|------|---------------|
| `src/Uno.UI/UI/Xaml/Controls/RichTextBlock/RichTextBlock.cs` | Constructor, Blocks property, InvalidateForContentChange |
| `src/Uno.UI/UI/Xaml/Documents/Block.cs` | Protected constructor (Skia only) |
| `src/Uno.UI/UI/Xaml/Documents/Paragraph.cs` | InvalidateInlines, TextIndent, Inlines collection |
| `src/Uno.UI/UI/Xaml/Documents/Inline.cs` | InvalidateInlines with Paragraph support |
| `src/Uno.UI/UI/Xaml/Documents/InlineCollection.cs` | TraversedTree, OnCollectionChanged with Paragraph support |
| `src/Uno.UI/Generated/3.0.0.0/.../RichTextBlock.cs` | All DPs stubbed with `[NotImplemented]` on all platforms |
| `src/Uno.UI/Generated/3.0.0.0/.../Block.cs` | Generated Block stub |

### Prerequisites

- **WinUI C++ sources**: Must be accessible at `D:\Work\microsoft-ui-xaml2`.
- **WinUI Porting Agent**: All ported code MUST follow `.github/agents/winui-porting-agent.md` conventions (MUX reference comments, file naming, `#if HAS_UNO` guards, `TODO Uno:` markers, SerialDisposable revokers).
- **Existing text infrastructure**: Reuse the UnicodeText/HarfBuzz/SkiaSharp pipeline from TextBlock on Skia.

### Assumptions

- Only Skia targets are in scope for rendering. Other platforms keep the existing `[NotImplemented]` stubs.
- The existing TextBlock Skia rendering pipeline is stable and suitable for reuse by RichTextBlock.
- WinUI's internal text engine (BlockLayoutEngine, TextFormatter, ParagraphTextSource) will NOT be ported directly. Instead, the ported RichTextBlock API will bridge to Uno's existing UnicodeText pipeline. This boundary is documented with `TODO Uno:` comments.
- RichTextBlock defaults to `TextWrapping.Wrap` (unlike TextBlock which defaults to `NoWrap`), matching WinUI behavior confirmed in the C++ constructor.
- For this initial porting effort, we scope rendering to single-paragraph with basic inlines. Multi-paragraph, hyperlinks, selection, overflow, and TextPointer are deferred.

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Port RichTextBlock API Surface from WinUI C++ (Priority: P1)

As a contributor porting WinUI controls to Uno Platform, I want to faithfully translate the RichTextBlock C++ class declaration, dependency properties, property-changed callbacks, and constructor logic into C# partial files following the WinUI Porting Agent conventions, so that the Uno RichTextBlock has a complete, WinUI-aligned API surface.

**Why this priority**: Without the API surface, nothing else can work. The ported DPs and callbacks are the foundation that the rendering layer builds on. This is also the most mechanical and lowest-risk work.

**Independent Test**: Can be verified by building the Skia solution filter successfully and confirming that all RichTextBlock dependency properties are accessible without `[NotImplemented]` warnings on Skia. Runtime tests can set and get each property and verify default values match WinUI.

**Acceptance Scenarios**:

1. **Given** the WinUI C++ RichTextBlock header and property files, **When** ported to C# following winui-porting-agent conventions, **Then** all files include MUX reference comments, use the correct partial-file layout (`.cs`, `.Properties.cs`, `.h.mux.cs`, `.mux.cs`), and Uno-specific code is wrapped in `#if HAS_UNO`.
2. **Given** the ported RichTextBlock properties file, **When** a developer sets `FontSize`, `FontWeight`, `TextWrapping`, `TextAlignment`, `Padding`, `MaxLines`, `LineHeight`, `CharacterSpacing`, `Foreground`, `TextDecorations`, or `FontFamily` on RichTextBlock, **Then** each property stores and retrieves the value correctly via the DependencyProperty system.
3. **Given** a newly constructed RichTextBlock, **When** its default property values are inspected, **Then** `TextWrapping` is `Wrap`, `LineStackingStrategy` is `MaxHeight`, `TextTrimming` is `None`, `FontSize` is the system default (14.0), and other defaults match the WinUI C++ constructor.
4. **Given** the generated stubs in `Generated/3.0.0.0/.../RichTextBlock.cs`, **When** the porting is complete, **Then** the `__SKIA__` platform is removed from the `#if` guards for all properties implemented in this phase, so the generated stubs no longer activate on Skia.

---

### User Story 2 — Port Document Model Classes (Block, Paragraph, BlockCollection) (Priority: P1)

As a contributor, I want to port the Block, Paragraph, and BlockCollection class logic from WinUI C++ so that the document model behaves consistently with WinUI, including parent chain setup, invalidation propagation, and collection management.

**Why this priority**: The document model is the data structure that RichTextBlock operates on. Properties and rendering both depend on it. Equally critical as the API surface.

**Independent Test**: Can be verified by programmatically constructing a RichTextBlock with Paragraphs and Runs, modifying the collections at runtime, and confirming invalidation propagates to the control.

**Acceptance Scenarios**:

1. **Given** WinUI's CParagraph, CBlock, and CBlockCollection C++ sources, **When** ported to C#, **Then** the ported files follow the partial-file naming conventions and include MUX reference comments.
2. **Given** a RichTextBlock with a Paragraph added to Blocks, **When** a Run is added to the Paragraph's Inlines, **Then** the invalidation chain propagates: Run change -> Inline.InvalidateInlines -> Paragraph.InvalidateInlines -> RichTextBlock.InvalidateMeasure.
3. **Given** a Paragraph's Inlines collection, **When** Inlines are added, removed, or modified at runtime, **Then** the TraversedTree cache is invalidated and the parent RichTextBlock's layout is re-measured.

---

### User Story 3 — Render Single Paragraph with Inline Formatting on Skia (Priority: P1)

As an Uno Platform developer targeting Skia, I want a RichTextBlock with a single Paragraph containing Runs and inline formatting (Bold, Italic, Underline, Span) to render visible, correctly formatted text on screen.

**Why this priority**: This is the visible outcome that validates the entire porting effort. Without rendering, ported properties have no observable effect.

**Independent Test**: Can be fully tested by placing `<RichTextBlock><Paragraph><Run Text="Hello"/><Bold><Run Text=" Bold"/></Bold></Paragraph></RichTextBlock>` in XAML, running on a Skia target, and verifying text appears with correct formatting.

**Acceptance Scenarios**:

1. **Given** a RichTextBlock with `<Paragraph><Run Text="Hello, World!"/></Paragraph>`, **When** rendered on Skia, **Then** the text "Hello, World!" is visible and correctly laid out.
2. **Given** a Paragraph containing `<Run Text="Normal "/><Bold><Run Text="Bold "/></Bold><Italic><Run Text="Italic"/></Italic>`, **When** rendered, **Then** each segment displays with its respective formatting.
3. **Given** a Paragraph with `<Span FontSize="20"><Run Text="Large"/></Span><Run Text=" Small"/>`, **When** rendered, **Then** the spanned text renders at size 20 while "Small" uses the RichTextBlock default.
4. **Given** a RichTextBlock with `FontSize="24"` and `FontWeight="Bold"`, **When** rendered, **Then** all text renders at 24px bold unless overridden by inline elements.
5. **Given** equivalent content in a RichTextBlock and a TextBlock, **When** both are measured, **Then** their DesiredSize values match within 1 device-independent pixel.

---

### User Story 4 — Text Layout Properties (Priority: P2)

As an Uno Platform developer, I want TextWrapping, TextTrimming, TextAlignment, HorizontalTextAlignment, MaxLines, and Padding to work correctly on RichTextBlock so that I can control text layout.

**Why this priority**: Layout control is needed for production-quality UIs but basic rendering can work without these at first.

**Independent Test**: Can be tested by creating RichTextBlocks with various layout property combinations inside fixed-size containers and verifying wrapping, trimming, alignment, and padding behavior.

**Acceptance Scenarios**:

1. **Given** a RichTextBlock with `TextWrapping="Wrap"` inside a narrow container, **When** rendered, **Then** the text wraps to multiple lines.
2. **Given** a RichTextBlock with `TextWrapping="NoWrap"` and `TextTrimming="CharacterEllipsis"`, **When** rendered inside a narrow container, **Then** the text is trimmed with an ellipsis.
3. **Given** a RichTextBlock with `TextAlignment="Center"`, **When** rendered in a wide container, **Then** the text is horizontally centered.
4. **Given** a RichTextBlock with `MaxLines="2"` and long text, **When** rendered, **Then** only two lines of text are visible.
5. **Given** a RichTextBlock with `Padding="10,20,10,20"`, **When** measured, **Then** DesiredSize includes the padding values.
6. **Given** a RichTextBlock with default properties, **When** rendered in a narrow container, **Then** the text wraps (confirming default `TextWrapping.Wrap`).

---

### User Story 5 — Line Height and Character Spacing (Priority: P3)

As an Uno Platform developer, I want to control line height (LineHeight, LineStackingStrategy) and character spacing (CharacterSpacing) to fine-tune text typography.

**Why this priority**: Advanced typography properties used in polished UIs. Basic rendering works without them.

**Independent Test**: Can be tested by setting LineHeight and CharacterSpacing and measuring/comparing output dimensions.

**Acceptance Scenarios**:

1. **Given** a RichTextBlock with `LineHeight="40"` and wrapped text, **When** rendered, **Then** the line spacing matches 40 device-independent pixels.
2. **Given** a RichTextBlock with `CharacterSpacing="100"`, **When** rendered, **Then** the text has wider character spacing and DesiredSize width is larger.
3. **Given** a RichTextBlock with `LineStackingStrategy="BaselineToBaseline"` and `LineHeight="30"`, **When** rendered, **Then** the baseline-to-baseline distance is 30 DIPs.

---

### User Story 6 — Dynamic Property Changes (Priority: P3)

As an Uno Platform developer, I want to change RichTextBlock properties and content at runtime and have the control re-layout and re-render automatically.

**Why this priority**: Runtime reactivity is expected for any XAML control but is less critical than initial rendering correctness.

**Independent Test**: Can be tested by changing properties programmatically after initial render and verifying visual output updates.

**Acceptance Scenarios**:

1. **Given** a rendered RichTextBlock, **When** FontSize is changed at runtime, **Then** the text re-renders at the new size.
2. **Given** a rendered RichTextBlock, **When** a new Run is added to the Paragraph's Inlines, **Then** the new text appears.
3. **Given** a rendered RichTextBlock, **When** a Run's Text property changes, **Then** the displayed text updates.
4. **Given** a rendered RichTextBlock, **When** TextWrapping changes from Wrap to NoWrap, **Then** the layout updates to a single line.

---

### Edge Cases

- What happens when RichTextBlock has no Paragraph children? (Should render empty, not crash)
- What happens when a Paragraph has no Inline children? (Should render empty paragraph, not crash)
- What happens when a Run has empty Text (`Text=""`)? (Should contribute zero width)
- What happens with very long single-word text with `TextWrapping="Wrap"`? (Should character-break if word exceeds container width)
- What happens when the container has zero or very small available width? (Should handle gracefully)
- What happens when Foreground is null or not set? (Should use inherited/default foreground color)
- What happens when multiple Runs have different FontSize values? (Line height should accommodate the tallest run)
- What happens when the Blocks collection is modified at runtime? (Should re-layout)

## Requirements *(mandatory)*

### Functional Requirements

**Layer 1 — API Surface Port (from WinUI C++)**

- **FR-001**: RichTextBlock MUST be ported from the WinUI C++ source following `.github/agents/winui-porting-agent.md` conventions: MUX reference comments, partial-file layout, `#if HAS_UNO` guards, `TODO Uno:` markers for unportable code
- **FR-002**: All RichTextBlock dependency properties MUST be ported to a `RichTextBlock.Properties.cs` file with correct default values matching the WinUI C++ constructor (TextWrapping=Wrap, LineStackingStrategy=MaxHeight, TextTrimming=None, etc.)
- **FR-003**: Property-changed callbacks from the WinUI C++ source MUST be ported, triggering InvalidateMeasure or re-render as appropriate
- **FR-004**: RichTextBlock header fields (state variables, cached references) MUST be ported to a `.h.mux.cs` file
- **FR-005**: The Block/Paragraph/BlockCollection document model classes MUST be ported from WinUI C++ with their invalidation logic intact
- **FR-006**: Generated stubs MUST be updated to remove `__SKIA__` from `#if` guards for all properties implemented in this phase
- **FR-007**: Ported code that references WinUI-internal APIs not available in Uno (BlockLayoutEngine, TextFormatter, etc.) MUST be preserved as `TODO Uno:` comments with the original C++ code

**Layer 2 — Skia Rendering Integration**

- **FR-008**: RichTextBlock MUST render text content from `Paragraph` > `Run` element hierarchy on Skia targets using the existing UnicodeText/HarfBuzz pipeline
- **FR-009**: RichTextBlock MUST support `FontFamily`, `FontSize`, `FontWeight`, `FontStyle`, `FontStretch` properties affecting rendered text
- **FR-010**: RichTextBlock MUST support `Foreground` brush property to control text color
- **FR-011**: RichTextBlock MUST support `TextWrapping` (Wrap, NoWrap, WrapWholeWords) defaulting to `Wrap`
- **FR-012**: RichTextBlock MUST support `TextTrimming` (None, CharacterEllipsis, WordEllipsis, Clip)
- **FR-013**: RichTextBlock MUST support `TextAlignment` and `HorizontalTextAlignment`
- **FR-014**: RichTextBlock MUST support `Padding`
- **FR-015**: RichTextBlock MUST support `MaxLines`
- **FR-016**: RichTextBlock MUST support `LineHeight` and `LineStackingStrategy`
- **FR-017**: RichTextBlock MUST support `CharacterSpacing`
- **FR-018**: RichTextBlock MUST support `TextDecorations` (Underline, Strikethrough)
- **FR-019**: RichTextBlock MUST support inline formatting elements: `Bold`, `Italic`, `Underline`, `Span` with per-inline property overrides
- **FR-020**: RichTextBlock MUST correctly measure and report DesiredSize accounting for text content, font metrics, padding, and layout constraints
- **FR-021**: RichTextBlock MUST re-layout and re-render when any property changes at runtime
- **FR-022**: RichTextBlock MUST re-layout when Blocks or Inlines collections change
- **FR-023**: Inline property inheritance MUST work — Runs without explicit font properties inherit from parent Span/Paragraph/RichTextBlock

### Key Entities

- **RichTextBlock**: The control. Contains a `Blocks` collection. Inherits from `FrameworkElement`. Ported from `CRichTextBlock` in WinUI C++.
- **BlockCollection**: Ordered collection of `Block` elements owned by RichTextBlock. Ported from `CBlockCollection`.
- **Block**: Abstract base for block-level elements. Only concrete subclass is Paragraph. Ported from `CBlock`.
- **Paragraph**: Block-level element containing an `Inlines` collection. Ported from `CParagraph`.
- **InlineCollection**: Ordered collection of `Inline` elements within a Paragraph or Span.
- **Run**: Inline leaf node holding a text string. Ported from `CRun`.
- **Span**: Inline container that holds other inlines and can override font/text properties. Ported from `CSpan`.
- **Bold**: Span subclass that sets FontWeight to Bold.
- **Italic**: Span subclass that sets FontStyle to Italic.
- **Underline**: Span subclass that sets TextDecorations to Underline.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: All ported C# files include MUX reference comments pointing to the source WinUI C++ file, and follow the partial-file naming conventions defined in the WinUI Porting Agent
- **SC-002**: A `<RichTextBlock><Paragraph><Run Text="Hello"/></Paragraph></RichTextBlock>` renders visible text on all Skia targets (Windows, macOS, Linux) with correct font metrics
- **SC-003**: All 23 functional requirements pass automated runtime tests verifying correct measurement, rendering, property defaults, and porting conventions
- **SC-004**: RichTextBlock with mixed inline formatting (Bold, Italic, Underline, Span with overrides) renders each segment correctly in a single paragraph
- **SC-005**: Dynamic property changes and collection modifications cause correct re-layout with no visual artifacts
- **SC-006**: The control handles all edge cases (empty content, zero-width container, null foreground) without crashes
- **SC-007**: Measurement results for equivalent content match TextBlock measurements within 1 device-independent pixel tolerance
- **SC-008**: WinUI C++ code that cannot be directly ported is preserved as `TODO Uno:` comments with the original C++ logic, ensuring no information is lost during porting
