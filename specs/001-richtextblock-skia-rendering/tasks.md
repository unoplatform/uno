# Tasks: RichTextBlock Skia Rendering (Phase 1)

**Input**: Design documents from `/specs/001-richtextblock-skia-rendering/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, quickstart.md

**Tests**: Included. Constitution Principle III (Test-First Quality Gates) mandates runtime tests for all UI changes.

**Organization**: Tasks are grouped by user story. Foundational infrastructure is separated because all rendering components must work together before any story can be tested.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup

**Purpose**: Verify build environment is ready for development

- [X] T001 Verify Skia build works by running `dotnet build src/Uno.UI-Skia-only.slnf` and confirming it succeeds
- [X] T002 Verify unit tests pass by running `dotnet test src/Uno.UI/Uno.UI.Tests.csproj`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Fix the invalidation chain and Block base class so content changes propagate from Paragraph/Inline up to RichTextBlock. These changes are required before ANY rendering can work.

**CRITICAL**: No user story work can begin until this phase is complete.

### Invalidation Chain Fix

- [X] T003 [P] Add `case Paragraph` to `Inline.InvalidateInlines` switch in `src/Uno.UI/UI/Xaml/Documents/Inline.cs` — when parent is Paragraph, call `paragraph.InvalidateInlines(updateText)` (see research.md R3 fix point 1)
- [X] T004 [P] Add `case Paragraph` to `InlineCollection.OnCollectionChanged` switch in `src/Uno.UI/UI/Xaml/Documents/InlineCollection.cs` — when `_collection.GetParent()` is Paragraph, call `paragraph.InvalidateInlines(true)` (see research.md R3 fix point 2)
- [X] T005 [P] Add `internal void InvalidateInlines(bool updateText)` method to `src/Uno.UI/UI/Xaml/Documents/Paragraph.cs` — walk up parent chain: if parent is `RichTextBlock`, call `richTextBlock.InvalidateForContentChange()` and invalidate the paragraph's own InlineCollection.TraversedTree (see research.md R3 fix point 3)

### Block Base Class Fix

- [ ] T006 Create hand-written `src/Uno.UI/UI/Xaml/Documents/Block.cs` with a `protected Block()` constructor (no `TryRaiseNotImplemented` call) and remove `__SKIA__` from the constructor guard in `src/Uno.UI/Generated/3.0.0.0/Microsoft.UI.Xaml.Documents/Block.cs` so Paragraph creation doesn't log spurious NotImplemented warnings on Skia (see research.md R6)

### RichTextBlock Constructor

- [ ] T007 Update `src/Uno.UI/UI/Xaml/Controls/RichTextBlock/RichTextBlock.cs` — remove `[global::Uno.NotImplemented]` from class and constructor, remove `TryRaiseNotImplemented` call from constructor, add `Blocks.SetParent(this)` to establish DP parent chain, subscribe to `Blocks.VectorChanged` for collection change notifications that call `InvalidateForContentChange()` (see research.md R7 and plan.md D3/D4)

**Checkpoint**: Invalidation chain complete — content and collection changes can now propagate from Paragraph/Inline up to RichTextBlock.

---

## Phase 3: User Story 1 — Display Basic Rich Text (Priority: P1) MVP

**Goal**: A `<RichTextBlock><Paragraph><Run Text="Hello, World!"/></Paragraph></RichTextBlock>` renders visible text on Skia targets with correct font metrics and non-zero DesiredSize.

**Independent Test**: Place a RichTextBlock with a single Paragraph/Run in XAML, run on Skia target, verify text appears and DesiredSize is correct.

### Implementation for User Story 1

- [ ] T008 [P] [US1] Create `src/Uno.UI/UI/Xaml/Controls/RichTextBlock/RichTextBlock.Properties.cs` with ALL Phase 1 DependencyProperty registrations. Include: FontFamily, FontSize (default 14.0), FontWeight (default Normal), FontStyle (default Normal), FontStretch (default Normal), Foreground (default null), TextWrapping (default **Wrap** — different from TextBlock!), TextTrimming (default None), TextAlignment (default Left), HorizontalTextAlignment (default Left), Padding (default 0), MaxLines (default 0), LineHeight (default 0), LineStackingStrategy (default MaxHeight), CharacterSpacing (default 0), TextDecorations (default None), IsTextTrimmed (read-only). Use `FrameworkPropertyMetadataOptions.Inherits | AffectsMeasure` for font properties. Include `propertyChangedCallback` for each property that calls `InvalidateTextBlock()`. Follow TextBlock.cs DP registration patterns exactly. (see data-model.md DependencyProperties tables for all defaults and options)
- [ ] T009 [P] [US1] Create `src/Uno.UI/UI/Xaml/Controls/RichTextBlock/RichTextBlockTextVisual.skia.cs` — a `ContainerVisual` subclass that holds a `WeakReference<RichTextBlock>` and delegates `Paint()` to `RichTextBlock.Draw()`. Model exactly on `src/Uno.UI/UI/Xaml/Controls/TextBlock/TextVisual.skia.cs`. Override `CanPaint()` to return true. (see research.md R5)
- [ ] T010 [US1] Create `src/Uno.UI/UI/Xaml/Controls/RichTextBlock/RichTextBlock.skia.cs` with: (1) `CreateElementVisual()` override returning `new RichTextBlockTextVisual(Compositor.GetSharedCompositor(), this)`, (2) `IParsedText ParsedText` property initialized to `ParsedText.Empty`, (3) `MeasureOverride` that subtracts Padding, gets first Paragraph from Blocks, collects `paragraph.Inlines.TraversedTree.leafTree`, calls `GetDefaultFontDetails()` from `FontDetailsCache`, creates `new UnicodeText(...)` passing leafInlines + all layout properties, adds Padding back, applies layout rounding (copy from TextBlock.skia.cs), returns desiredSize, (4) `ArrangeOverride` that re-parses text with final size, invalidates render, calls `UpdateIsTextTrimmed()`, (5) `Draw(in Visual.PaintingSession)` that translates by Padding, calls `ParsedText.Draw(session, null, Enumerable.Empty<TextHighlighter>())`, restores canvas, (6) `GetDefaultFontDetails()` method using `FontDetailsCache.GetFont()` (copy from TextBlock.skia.cs), (7) `InvalidateInlineAndRequireRepaint()` method, (8) `InvalidateForContentChange()` that invalidates measure + render, (9) `internal void InvalidateTextBlock()` called by DP callbacks, (10) implement `UnicodeText.IFontCacheUpdateListener` interface. Handle empty Blocks gracefully (return zero-size with padding). (see data-model.md Rendering Pipeline and TextBlock.skia.cs as reference)
- [ ] T011 [US1] Update generated stub `src/Uno.UI/Generated/3.0.0.0/Microsoft.UI.Xaml.Controls/RichTextBlock.cs` — for EVERY property and DP registration that was hand-written in T008, remove `__SKIA__` from the `#if` guard so the generated version compiles out on Skia and the hand-written version is used instead. Properties to update: FontFamily, FontSize, FontWeight, FontStyle, FontStretch, Foreground, TextWrapping, TextTrimming, TextAlignment, HorizontalTextAlignment, Padding, MaxLines, LineHeight, LineStackingStrategy, CharacterSpacing, TextDecorations, IsTextTrimmed, and ALL their corresponding static DependencyProperty fields. Keep the `#if` guards for non-Skia platforms intact.

### Tests for User Story 1

- [ ] T012 [US1] Create `src/Uno.UI.RuntimeTests/Tests/Microsoft_UI_Xaml_Controls/Given_RichTextBlock.cs` with runtime tests: (1) `When_SingleRun_Then_TextRendered` — create RichTextBlock with `<Paragraph><Run Text="Hello, World!"/></Paragraph>`, add to WindowHelper.WindowContent, await WaitForLoaded, assert DesiredSize.Width > 0 and DesiredSize.Height > 0, (2) `When_SingleRun_Then_DesiredSizeMatchesTextBlock` — create equivalent TextBlock and RichTextBlock with same text/font, compare DesiredSize within 1 DIP tolerance, (3) `When_EmptyBlocks_Then_NoException` — create RichTextBlock with no Paragraphs, add to visual tree, await WaitForLoaded, assert no exception and DesiredSize.Height equals padding only. Use `[TestClass]`, `[TestMethod]`, `[RunsOnUIThread]` attributes and `#if __SKIA__` guard. Follow naming convention `Given_RichTextBlock`. (see spec.md US1 acceptance scenarios and .github/agents/runtime-tests-agent.md)

**Checkpoint**: RichTextBlock renders basic text on Skia. MVP is functional.

---

## Phase 4: User Story 2 — Apply Font Properties (Priority: P1)

**Goal**: FontFamily, FontSize, FontWeight, FontStyle, and FontStretch properties on RichTextBlock correctly affect rendered text, and Runs inherit these properties.

**Independent Test**: Set various font properties on RichTextBlock and verify DesiredSize changes appropriately (e.g., FontSize=24 produces larger DesiredSize than FontSize=12).

### Tests for User Story 2

- [ ] T013 [US2] Add runtime tests to `src/Uno.UI.RuntimeTests/Tests/Microsoft_UI_Xaml_Controls/Given_RichTextBlock.cs`: (1) `When_FontSize24_Then_LargerDesiredSize` — create two RichTextBlocks with same text, one FontSize=14 and one FontSize=24, assert the 24px version has larger DesiredSize, (2) `When_FontWeightBold_Then_WiderDesiredSize` — compare DesiredSize.Width of Normal vs Bold weight, (3) `When_FontStyleItalic_Then_DesiredSizeNonZero` — verify italic text renders with non-zero size, (4) `When_RunInheritsRichTextBlockFontSize` — create RichTextBlock with FontSize=20 and a Run with no explicit FontSize, verify the Run's effective FontSize is 20 by comparing DesiredSize to an explicit FontSize=20 Run. (see spec.md US2 acceptance scenarios)

**Checkpoint**: Font properties verified working with property inheritance.

---

## Phase 5: User Story 3 — Inline Formatting Within a Paragraph (Priority: P1)

**Goal**: Bold, Italic, Underline, and Span inline formatting elements apply their respective formatting to text segments within a single Paragraph.

**Independent Test**: Create a Paragraph with mixed inline elements (Normal + Bold + Italic + Underline + Span with FontSize override) and verify each segment has distinct formatting by comparing measurements.

### Tests for User Story 3

- [ ] T014 [US3] Add runtime tests to `src/Uno.UI.RuntimeTests/Tests/Microsoft_UI_Xaml_Controls/Given_RichTextBlock.cs`: (1) `When_BoldInline_Then_TextIsBold` — create RichTextBlock with `<Bold><Run Text="Bold"/></Bold>`, verify DesiredSize.Width is wider than same text without Bold (bold glyphs are wider), (2) `When_SpanWithFontSizeOverride_Then_OverrideApplied` — create `<Span FontSize="30"><Run Text="Big"/></Span>` alongside `<Run Text="Small"/>` at default size, verify overall DesiredSize.Height accommodates the larger font, (3) `When_NestedBoldInsideItalic_Then_BothApplied` — create `<Italic><Bold><Run Text="BoldItalic"/></Bold></Italic>`, verify DesiredSize is non-zero (both formats applied), (4) `When_UnderlineInline_Then_TextRendered` — create `<Underline><Run Text="Underlined"/></Underline>`, verify DesiredSize is non-zero. (see spec.md US3 acceptance scenarios)

**Checkpoint**: Mixed inline formatting verified working in single Paragraph.

---

## Phase 6: User Story 4 — Text Layout Properties (Priority: P2)

**Goal**: TextWrapping, TextTrimming, TextAlignment, HorizontalTextAlignment, MaxLines, and Padding work correctly on RichTextBlock.

**Independent Test**: Create RichTextBlocks with various layout property combinations inside fixed-size containers and verify wrapping, trimming, alignment, and padding by comparing DesiredSize.

### Tests for User Story 4

- [ ] T015 [US4] Add runtime tests to `src/Uno.UI.RuntimeTests/Tests/Microsoft_UI_Xaml_Controls/Given_RichTextBlock.cs`: (1) `When_TextWrappingWrap_Then_TextWraps` — create RichTextBlock with long text inside a narrow (100px) container with TextWrapping=Wrap, verify DesiredSize.Height > single-line height, (2) `When_TextWrappingDefault_Then_DefaultIsWrap` — create RichTextBlock with no explicit TextWrapping inside narrow container, verify text wraps (confirming default is Wrap), (3) `When_MaxLines2_Then_OnlyTwoLines` — create RichTextBlock with long text and MaxLines=2, verify DesiredSize.Height matches approximately 2 lines, (4) `When_PaddingSet_Then_DesiredSizeIncludesPadding` — create RichTextBlock with Padding="10,20,10,20", verify DesiredSize includes padding (compare with and without padding), (5) `When_TextAlignmentCenter_Then_NoException` — create RichTextBlock with TextAlignment=Center, verify renders without exception and DesiredSize is non-zero, (6) `When_TextTrimmingCharacterEllipsis_Then_NoException` — create RichTextBlock with TextTrimming=CharacterEllipsis and TextWrapping=NoWrap in narrow container, verify no exception. (see spec.md US4 acceptance scenarios)

**Checkpoint**: Layout properties verified working.

---

## Phase 7: User Story 5 — Line Height and Character Spacing (Priority: P3)

**Goal**: LineHeight, LineStackingStrategy, and CharacterSpacing properties control text typography on RichTextBlock.

**Independent Test**: Set LineHeight and CharacterSpacing and compare DesiredSize with and without these properties to verify they take effect.

### Tests for User Story 5

- [ ] T016 [US5] Add runtime tests to `src/Uno.UI.RuntimeTests/Tests/Microsoft_UI_Xaml_Controls/Given_RichTextBlock.cs`: (1) `When_LineHeight40_Then_LargerHeight` — create two RichTextBlocks with same multi-line wrapping text, one with LineHeight=40 and one with default, verify the LineHeight=40 version has larger DesiredSize.Height, (2) `When_CharacterSpacing100_Then_WiderText` — create two RichTextBlocks with same text, one with CharacterSpacing=100 and one with default, verify CharacterSpacing=100 has wider DesiredSize.Width, (3) `When_LineStackingStrategyBaselineToBaseline_Then_NoException` — create RichTextBlock with LineStackingStrategy=BaselineToBaseline and LineHeight=30, verify renders without exception and DesiredSize is non-zero. (see spec.md US5 acceptance scenarios)

**Checkpoint**: Typography properties verified working.

---

## Phase 8: User Story 6 — Dynamic Property Changes (Priority: P3)

**Goal**: Changing RichTextBlock properties and content at runtime causes automatic re-layout and re-render.

**Independent Test**: Change properties programmatically after initial render and verify DesiredSize updates accordingly.

### Tests for User Story 6

- [ ] T017 [US6] Add runtime tests to `src/Uno.UI.RuntimeTests/Tests/Microsoft_UI_Xaml_Controls/Given_RichTextBlock.cs`: (1) `When_FontSizeChangedAtRuntime_Then_DesiredSizeUpdates` — create RichTextBlock with FontSize=14, await WaitForLoaded, record DesiredSize, change FontSize to 28, await WaitForIdle, assert new DesiredSize is larger, (2) `When_RunAddedAtRuntime_Then_LayoutUpdates` — create RichTextBlock with one Run, await WaitForLoaded, record DesiredSize.Width, add another Run to Paragraph.Inlines, await WaitForIdle, assert DesiredSize.Width increased, (3) `When_RunTextChangedAtRuntime_Then_TextUpdates` — create RichTextBlock with Run Text="Short", await WaitForLoaded, record width, change Run.Text to "Much longer text content", await WaitForIdle, assert width increased, (4) `When_TextWrappingChangedAtRuntime_Then_LayoutUpdates` — create RichTextBlock with wrapping text in narrow container, await WaitForLoaded, record height (multi-line), change TextWrapping to NoWrap, await WaitForIdle, assert height decreased (single line). (see spec.md US6 acceptance scenarios)

**Checkpoint**: Dynamic property and content changes verified working.

---

## Phase 9: Polish & Cross-Cutting Concerns

**Purpose**: Edge case tests, SamplesApp registration, and build validation

- [ ] T018 [P] Add edge case runtime tests to `src/Uno.UI.RuntimeTests/Tests/Microsoft_UI_Xaml_Controls/Given_RichTextBlock.cs`: (1) `When_EmptyParagraph_Then_NoException` — Paragraph with no Inlines, (2) `When_EmptyRunText_Then_NoException` — Run with Text="", (3) `When_NullForeground_Then_UsesDefault` — RichTextBlock with no explicit Foreground, (4) `When_MultipleDifferentFontSizeRuns_Then_LineAccommodatesTallest` — two Runs in same Paragraph with FontSize=10 and FontSize=30, verify DesiredSize.Height accommodates the larger one, (5) `When_BlocksCollectionModified_Then_RelayoutOccurs` — add a new Paragraph at runtime, verify no crash. (see spec.md Edge Cases)
- [ ] T019 [P] Create SamplesApp sample `src/SamplesApp/UITests.Shared/Microsoft_UI_Xaml_Controls/RichTextBlockTests/RichTextBlock_BasicRendering.xaml` and code-behind `.xaml.cs` — demonstrate basic rendering, font properties, inline formatting (Bold/Italic/Underline/Span), layout properties. Apply `[Uno.UI.Samples.Controls.Sample]` attribute to code-behind class. (see AGENTS.md SamplesApp guidance)
- [ ] T020 Register SamplesApp sample in `src/SamplesApp/UITests.Shared/UITests.Shared.projitems` — add both `<Page>` entry for XAML and `<Compile>` entry for code-behind with `<DependentUpon>` (see AGENTS.md SamplesApp Registration section for exact XML format)
- [ ] T021 Run full Skia build: `dotnet build src/Uno.UI-Skia-only.slnf` and verify no errors or new warnings related to RichTextBlock
- [ ] T022 Run unit tests: `dotnet test src/Uno.UI/Uno.UI.Tests.csproj` and verify no regressions
- [ ] T023 Run runtime tests headlessly: build `src/SamplesApp/SamplesApp.Skia.Generic/SamplesApp.Skia.Generic.csproj -c Release -f net10.0`, then run `dotnet SamplesApp.Skia.Generic.dll --runtime-tests=test-results.xml` and verify all Given_RichTextBlock tests pass

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately
- **Foundational (Phase 2)**: Depends on Setup — BLOCKS all user stories
- **US1 (Phase 3)**: Depends on Foundational — this is the MVP
- **US2 (Phase 4)**: Depends on US1 (needs rendering working to test font properties)
- **US3 (Phase 5)**: Depends on US1 (needs rendering working to test inline formatting)
- **US4 (Phase 6)**: Depends on US1 (needs rendering working to test layout properties)
- **US5 (Phase 7)**: Depends on US1 (needs rendering working to test typography)
- **US6 (Phase 8)**: Depends on US1 (needs rendering working to test dynamic changes)
- **Polish (Phase 9)**: Depends on all user stories being complete

### User Story Dependencies

- **US1 (P1)**: Depends on Phase 2 only — independent MVP
- **US2 (P1)**: Depends on US1 — font property testing requires working renderer
- **US3 (P1)**: Depends on US1 — inline formatting testing requires working renderer
- **US4 (P2)**: Depends on US1 — layout testing requires working renderer
- **US5 (P3)**: Depends on US1 — typography testing requires working renderer
- **US6 (P3)**: Depends on US1 — dynamic change testing requires working renderer

Note: US2, US3, US4, US5, US6 can all run in parallel after US1 completes (they test different property groups).

### Within Each User Story

- All implementation code is in Phase 2 (foundational) and Phase 3 (US1)
- US2-US6 phases are primarily test tasks that validate property groups
- Tests should be written and verified to pass after implementation is complete

### Parallel Opportunities

**Phase 2 (Foundational)**:
- T003, T004, T005 can run in parallel (different files: Inline.cs, InlineCollection.cs, Paragraph.cs)
- T006 is independent (Block.cs + generated Block.cs)

**Phase 3 (US1)**:
- T008, T009 can run in parallel (different files: Properties.cs, TextVisual.skia.cs)
- T010 depends on T008 and T009
- T011 depends on T008

**After US1 completes**:
- US2, US3, US4, US5, US6 test phases can all run in parallel (all add tests to the same file but test different property groups)

**Phase 9 (Polish)**:
- T018, T019 can run in parallel

---

## Parallel Example: Phase 2 (Foundational)

```bash
# Launch all invalidation chain fixes in parallel (different files):
Task: "Add Paragraph case to Inline.InvalidateInlines in src/Uno.UI/UI/Xaml/Documents/Inline.cs"
Task: "Add Paragraph case to InlineCollection.OnCollectionChanged in src/Uno.UI/UI/Xaml/Documents/InlineCollection.cs"
Task: "Add InvalidateInlines method to src/Uno.UI/UI/Xaml/Documents/Paragraph.cs"
```

## Parallel Example: Phase 3 (US1)

```bash
# Launch property registration and visual in parallel (different files):
Task: "Create RichTextBlock.Properties.cs with all DependencyProperties"
Task: "Create RichTextBlockTextVisual.skia.cs composition visual"

# Then sequentially:
Task: "Create RichTextBlock.skia.cs rendering implementation"
Task: "Update generated stub to remove __SKIA__ guards"
```

## Parallel Example: After US1 MVP

```bash
# All test phases can run in parallel:
Task: "US2 font property tests"
Task: "US3 inline formatting tests"
Task: "US4 layout property tests"
Task: "US5 typography tests"
Task: "US6 dynamic change tests"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (verify build)
2. Complete Phase 2: Foundational (invalidation chain + Block fix + constructor)
3. Complete Phase 3: User Story 1 (full rendering implementation + basic tests)
4. **STOP and VALIDATE**: Run `dotnet build` + runtime tests for Given_RichTextBlock
5. A `<RichTextBlock><Paragraph><Run Text="Hello"/></Paragraph></RichTextBlock>` now renders on Skia

### Incremental Delivery

1. Setup + Foundational + US1 → **MVP: Basic text renders** (Phases 1-3)
2. Add US2 tests → **Font properties verified** (Phase 4)
3. Add US3 tests → **Inline formatting verified** (Phase 5)
4. Add US4 tests → **Layout properties verified** (Phase 6)
5. Add US5 + US6 tests → **Typography + dynamic changes verified** (Phases 7-8)
6. Polish → **SamplesApp + edge cases + full build validation** (Phase 9)

Each increment is independently verifiable and adds confidence without breaking previous work.

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- All DependencyProperties are registered in a single Properties.cs file (T008) to avoid same-file conflicts
- The rendering implementation (T010) is a single task because MeasureOverride/ArrangeOverride/Draw are tightly coupled
- US2-US6 phases are primarily test-only phases because the implementation is shared infrastructure
- Generated stub updates (T011) must match Properties.cs (T008) exactly to avoid duplicate DP registrations
- Runtime tests use `#if __SKIA__` guard since RichTextBlock rendering is Skia-only in Phase 1
