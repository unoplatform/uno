# RichTextBlock deep-core port — validation against WinUI 2.4.0

| | |
|---|---|
| **Branch** | `dev/mazi/richtextblock-winui` |
| **Scope** | `d083fba2df5..HEAD` — 93 commits, 149 changed `Uno.UI` sources, 131 files audited in depth |
| **Reference** | `winui3/release/2.4.0` = `e8442d07a` (tip of `winui3/release/2.0-stable`), sources under the `src/` prefix |
| **Method** | 9 parallel cluster audits reading both sides member-for-member, 1 provenance-header audit, 1 feature-gap sweep, then an adversarial refutation pass over every critical/major finding |
| **Supersedes** | `port-review.md` |

> **This document supersedes `specs/richtextblock-winui-port/port-review.md`.** That earlier pass derived its
> verdicts from commit `4a1c6184c`, a `main`-branch commit from 2026-06-18 that is contained in **no release tag**
> (`git merge-base --is-ancestor 4a1c6184c winui3/release/2.4.0` → not an ancestor). Its reference baseline was
> therefore wrong. Every verdict below was re-derived from `winui3/release/2.4.0`. `port-review.md` is retained
> unmodified for history but should not be cited.

---

## 0. Remediation status

Updated as items are addressed. Verdicts below are unchanged; this section only tracks what has been *done*.

| # | Item | Status | Commit | Evidence |
|---|---|:--:|---|---|
| — | `.skia` suffix removal on this port's files | ✅ done | `fe4fb8dd15c` | 91 files; `Uno.UI` is Skia-only so the suffix selects nothing. Test files keep theirs — the WinAppSDK head compiles zero `.skia.cs` |
| — | MUX header repin (§6.1–6.3) | ✅ done | `0dd5d8b6528` | 112 files now pin `tag winui3/release/2.4.0, commit e8442d07a`; 102 branch-wide + 4 per-file exceptions + 6 previously header-less ports |
| V1 | `RichTextBlockOverflow` cycle check | ✅ fixed | `f4c3c7afb2d` (fix) · `9a23ac9ae15` (test) | Runtime: with the fix reverted the test run **never terminates** (killed at 120 s, no results file). With it, 8/8 green |
| V2 | Overflow slice built from raw `Margin` | ✅ fixed | `63235e9a264` (fix) · `b3c1c9ed2f8` (test) | Runtime: reverted → slice starts row 32 (not 8) and ends row 87 of a 131 px box (not 129); 1000 painted px vs 1887 |
| V3 | `TransformPositionToPage` container↔flat space | ✅ fixed | `b5445cfc970` (fix) · `a84ca17dc15` (test) | Runtime: reverted → the last four container positions of a two-run paragraph all resolve to an empty rect; a point→pointer→rect round trip returns `X 0` for a probe at `x=120` |
| V4 | `TextHighlightMerge` pipeline unreachable | ✅ fixed | `4db6af52a12` (fix) · `9796bd3f10a` (test) | Runtime: reverted → the second highlighter's colour is **absent from the render entirely** |
| V5–V8 | Minor findings | ⬜ open | — | See §3 |
| P1–P3 | Feature backlog (18 items) | ⬜ open | — | See §7 |

### Notes recorded while remediating

**`TextBlockAutomationPeer.cs` keeps its `1.8.4` tag deliberately.** §6.2 proposed repinning it; the audit grades
it 40 % / structural ("deliberately keeps the pre-existing minimal `DirectUI.TextAdapter`; out of Stage 10 scope").
Repinning would assert a provenance the file does not have, so it was left alone.

**Correction to §3 (V4): `TextBlock` is _not_ unaffected.** The finding stated that `TextBlock` renders through
`UnicodeText.Draw` and so escapes the highlighter collapse. On this branch `TextBlock.cs:1655` calls
`ParsedText.Draw`, the same path, so it was subject to the same one-highlighter-one-range limit and is fixed
by the same change. Four `Given_TextBlock` tests fail around this area; three (`When_Inlines_Transitively_Change`
and two clipboard copy tests) were confirmed failing at the pre-fix baseline, and the fourth
(`When_IsTextSelectionEnabled_SurrogatePair_Copy`) passed 3/3 on re-run — clipboard contention, not a regression.

**`.skia.cs` suffixes are now no-ops inside `Uno.UI`** (found while fixing V1). `Uno.UI` has a single csproj whose
`UnoRuntimeIdentifier` is unconditionally `Skia`, so `__SKIA__` is always defined there — verified with
`dotnet build -getProperty:DefineConstants`. That makes 94 `.skia.cs` + 21 `.crossruntime.cs` suffixes and every
`#if __SKIA__` in `Uno.UI` redundant, and leaves 6 files (5 `.wasm.cs`, 1 `.Android.cs`) that nothing compiles.
The suffix is **still load-bearing in `Uno.UI.RuntimeTests`**, which has two heads: the `.Windows` (WinAppSDK
parity) head compiles **zero** `.skia.cs` files and never defines `__SKIA__`. A cleanup sweep of `Uno.UI` is a
separate change, out of scope for this branch.

---

## 1. Executive summary

**Is the port accurate?** Yes, to an unusually high degree. Across 131 audited files the mean completeness is
**94%**, and 77 files are exact, member-order-preserving 1:1 ports. The parts most at risk of silent
simplification — the `ITextContainer` reserved-position arithmetic (`BlockCollection`/`InlineCollection`/
`CRun`/`CSpan`/`CParagraph`), the `PageNode` break-continuation state machine, `ParagraphNode`'s line-formatting
loop and RTL bounds math, `PageHostedObjectRun`'s embedded-element reparenting, and the ~1500-line
`TextRangeAdapter` UIA surface — are faithful down to comments and variable names. The intentional architectural
divergence ("Strategy B": a whole-paragraph Skia `ParsedText` parse replacing WinUI's per-line LineServices loop)
was traced through its callers and found behaviour-preserving for everything except line trimming.

Eight parity findings survived adversarial verification: **4 major**, **4 minor**. One originally-major finding
was **refuted** outright (appendix). The major four cluster tightly: two are `RichTextBlockOverflow` chain
robustness (a dropped `ValidateNextLink` cycle check that hangs the UI thread; overflow render-slice geometry
rebuilt from the raw DP `Margin` instead of the collapsed margin/content render size the master already uses),
one is a container-space/flat-space impedance mismatch in `RichTextBlockView.TransformPositionToPage`, and one is
that the faithfully ported `TextHighlightMerge`/`TextHighlightRenderer` pipeline is dead code, so
`RichTextBlock` renders only the first highlighter and drops the selection highlight whenever an app
highlighter is also active. A further 17 minor/intended-deferral findings were left **unverified** (section 5).

**Is header provenance right?** No — systematically. 107 of the 149 changed files carry a `// MUX Reference`
header; **102 of them cite `tag winui3/release/1.8.2, commit 4a1c6184c`, a tag/commit pair that does not exist**
(`1.8.2` is `b1db15715`; `4a1c6184c` belongs to no tag). Five more cite `1.8.4` or `1.4.2` with no commit at all,
neither being an ancestor of `2.4.0`. Re-verifying content against the correct tag found **no resulting drift** —
the code matches `2.4.0`, only the citation is wrong. Every cited C++ filename (131 unique) was confirmed to
exist at the tag. Six genuine ports carry no header at all; two files are missing the MIT/MUX copyright block.
Section 6 is a directly applicable fix-list.

**What can't an app developer do?** Three clusters dominate. (1) **`TextTrimming` renders no ellipsis** —
the orchestration is ported but `SkiaTextLine.Collapse` is a no-op, so text hard-clips while `IsTextTrimmed`
still reports `true`. (2) **`RichTextBlockOverflow` is display-only and partly wrong** — no pointer/keyboard/focus
handling at all, no selection or highlighter painting, three public members still throwing, and a paragraph split
across a page break repaints from line 0 rather than the continuation line. (3) **Keyboard and touch
accessibility on hyperlinks and selection** — a focused `Hyperlink` cannot be activated by Enter/Space and draws
no focus rect; touch grippers are no-ops. Beyond those, `TextReadingOrder`, `OpticalMarginAlignment`,
`IsColorFontEnabled` and all `Typography` attached properties are inert. 18 gaps are backlogged in section 7.

---

## 2. Port accuracy by cluster

| Cluster | Files | Mean completeness | Accuracy distribution |
|---|---|---|---|
| A — RichTextServices contract | 22 | 99% | 19 faithful-1:1 · 3 minor |
| B — RichTextServices run types + Skia bridge | 12 | 90% | 6 faithful-1:1 · 4 structural · 2 not-a-port |
| C — BlockLayout node tree | 13 | 98% | 10 faithful-1:1 · 3 minor |
| D — Drawing contexts, text sources, element-model partials | 12 | 97% | 4 faithful-1:1 · 5 minor · 3 not-a-port |
| E — ITextView / text views + UIA text pattern | 14 | 89% | 6 faithful-1:1 · 6 minor · 2 structural |
| F — Element model + ITextContainer | 15 | 96% | 10 faithful-1:1 · 5 minor |
| G — Element-model leaves + TextPointer + TextSchema | 16 | 95% | 11 faithful-1:1 · 4 minor · 1 not-a-port |
| H — Selection + text highlighting | 15 | 97% | 8 faithful-1:1 · 7 minor |
| I — RichTextBlock / RichTextBlockOverflow controls | 12 | 90% | 3 faithful-1:1 · 8 minor · 1 not-a-port |
| **Total** | **131** | **94%** | **77 faithful-1:1 · 41 minor · 6 structural · 7 not-a-port** |

The distribution is skewed by design: the low-completeness outliers are almost all documented deferrals rather
than defects. `TextCollapsingCharacters` (15%) and `TextBlockAutomationPeer` (40%) are the only files below 70%,
and both are explicitly out of the plan's current stage scope. Clusters E and I score lowest because they carry
the port's genuinely net-new surface — the UIA text pattern and the overflow chain — where WinUI's C++ leans on
subsystems (LineServices, D2D, `CTextElementCollection` navigation) the port deliberately does not replicate.

### Cluster A — RichTextServices contract

| File | C++ source | % | Accuracy | Verdict |
|---|---|---|---|---|
| `RichTextServices/CharacterHit.cs` | `CharacterHit.h` | 100 | faithful-1:1 | Exact record-struct mirror. |
| `RichTextServices/DirectionalControl.cs` | `DirectionalControl.h` | 100 | faithful-1:1 | Enum order identical. |
| `RichTextServices/ElementType.cs` | `ElementType.h` | 100 | faithful-1:1 | Explicit numeric values preserved. |
| `RichTextServices/LayoutNodeType.cs` | `LayoutNodeType.h` | 100 | faithful-1:1 | Exact. |
| `RichTextServices/ObjectRunMetrics.cs` | `ObjectRunMetrics.h` | 100 | faithful-1:1 | Field order and types match. |
| `RichTextServices/TextBounds.cs` | `TextBounds.h` | 100 | faithful-1:1 | Exact. |
| `RichTextServices/TextBreak.cs` | `TextBreak.h`/`.cpp` | 100 | faithful-1:1 | Reference-equality base; refcounting → GC. |
| `RichTextServices/TextLineBreak.cs` | `TextLineBreak.h` | 100 | faithful-1:1 | Empty marker subclass, as in C++. |
| `RichTextServices/TextRunType.cs` | `TextRunType.h` | 100 | faithful-1:1 | Exact. |
| `RichTextServices/TextFormatting.cs` | `common/TextFormatting.cpp` | 95 | minor | `GetScaledFontSize` formula exact; generation-counter cache dropped (property system inherits). |
| `RichTextServices/TextRunCache.cs` | `TextRunCache.h`/`.cpp` | 100 | faithful-1:1 | Abstract `Clear()` exact; LineServices factory TODO-marked. |
| `RichTextServices/Result.skia.cs` | `Result.h` | 100 | faithful-1:1 | All 8 codes match; IFC macros → direct branching. |
| `RichTextServices/ITextContainer.skia.cs` | `inc/TextContainer.h` | 100 | faithful-1:1 | Member order and out-param order exact. |
| `RichTextServices/ILinkedTextContainer.skia.cs` | `ILinkedTextContainer.h` | 100 | faithful-1:1 | All 8 members, same order. |
| `RichTextServices/IEmbeddedElementHost.skia.cs` | `Inc/EmbeddedElementHost.h` | 100 | faithful-1:1 | Exact. |
| `RichTextServices/TextSource.skia.cs` | `TextSource.h` | 100 | faithful-1:1 | Exact; out-param → return value. |
| `RichTextServices/TextRun.skia.cs` | `TextRun.h` | 100 | faithful-1:1 | Field order and access modifiers exact. |
| `RichTextServices/TextRunProperties.skia.cs` | `TextRunProperties.h`/`.cpp` | 90 | minor | `EqualsForShaping` omits `InheritedProperties`/Typography (documented deferral). |
| `RichTextServices/TextParagraphProperties.skia.cs` | `TextParagraphProperties.h`/`.cpp` | 100 | faithful-1:1 | Flags, ctor order, `4 * FontSize` tab formula exact. |
| `RichTextServices/TextDrawingContext.skia.cs` | `TextDrawingContext.h` | 95 | minor | `DrawGlyphs` DWrite overload dropped (Skia shapes upstream). |
| `RichTextServices/TextFormatter.skia.cs` | `TextFormatter.h` | 100 | faithful-1:1 | `FormatLine` exact; factory → Skia bridge. |
| `RichTextServices/TextLine.skia.cs` | `TextLine.h` | 100 | faithful-1:1 | All 12 members, 14 fields, 15 accessors match. |

This is the cleanest cluster in the port. Every HRESULT/out-param collapse is legitimate per the porting rules,
and the only dropped members are LineServices/DWrite-only surface the plan already excludes.

### Cluster B — RichTextServices run types + Skia line-formatting bridge

| File | C++ source | % | Accuracy | Verdict |
|---|---|---|---|---|
| `RichTextServices/EndOfLineRun.skia.cs` | `EndOfLineRun.h`/`.cpp` | 100 | faithful-1:1 | Exact. |
| `RichTextServices/EndOfParagraphRun.skia.cs` | `EndOfParagraphRun.h`/`.cpp` | 100 | faithful-1:1 | Exact incl. assert and comment. |
| `RichTextServices/HiddenRun.skia.cs` | `HiddenRun.h`/`.cpp` | 100 | faithful-1:1 | Exact. |
| `RichTextServices/ObjectRun.skia.cs` | `ObjectRun.h`/`.cpp` | 100 | faithful-1:1 | All 5 members in order. |
| `RichTextServices/TextCharactersRun.skia.cs` | `TextCharactersRun.h`/`.cpp` | 100 | faithful-1:1 | `Split` semantics and assert exact. |
| `RichTextServices/TextCollapsingSymbol.skia.cs` | `TextCollapsingSymbol.h` | 100 | faithful-1:1 | Abstract contract exact. |
| `RichTextServices/TextRunCache.BlockLayout.skia.cs` | `TextRunCache.cpp` | 100 | structural | No-op cache by design; `SkiaTextFormatter` owns caching. Invalidation traced safe. |
| `RichTextServices/Skia/ISkiaParagraphSource.skia.cs` | — (Uno-native) | 100 | not-a-port | Whole-paragraph input seam `ParsedText.ParseText` needs. |
| `RichTextServices/Skia/SkiaTextFormatter.skia.cs` | `TextFormatter.h`, `LsTextFormatter.cpp` | 90 | structural | `FormatLine` role preserved; parse cache keyed by `TextSource` identity (verified non-stale). |
| `RichTextServices/Skia/SkiaTextLine.skia.cs` | `TextLine.h`, `LsTextLine.cpp` | 80 | structural | Metrics/arrange/hit-test/caret faithful; `Draw` + `Collapse` stubbed (Stage 6). |
| `RichTextServices/Skia/SkiaTextLineBreak.skia.cs` | `TextLineBreak.h`, `LsTextLineBreak.*` | 100 | structural | Opaque continuation token contract preserved. |
| `RichTextServices/Skia/TextCollapsingCharacters.skia.cs` | `TextCollapsingCharacters.h`/`.cpp` | 15 | not-a-port | Empty shell; `Width`/`Draw` throw. Plan risk R2 / Stage 6. |

Two plausible-looking defects were chased and disproved here. The single-entry parse cache cannot go stale
because `PageNode` rebuilds `ParagraphNode` (and its `readonly` `ParagraphTextSource`) whenever content is dirty.
`GetNext/PreviousCaretCharacterHit`'s hardcoded ±1 step is correct in `ParsedText`'s coordinate space, which
already bakes leading/trailing-edge resolution into the returned glyph index — it is not a surrogate bug.

### Cluster C — BlockLayout node tree

| File | C++ source | % | Accuracy | Verdict |
|---|---|---|---|---|
| `BlockLayout/BlockLayoutEngine.skia.cs` | `BlockLayoutEngine.h`/`.cpp` | 100 | faithful-1:1 | Exact. |
| `BlockLayout/BlockLayoutHelpers.skia.cs` | `BlockLayoutHelpers.h`/`.cpp` | 80 | minor | Member order exact; font-context/optical-margin/reading-order/color-font stubbed to WinUI defaults with TODO markers; `IsCloseReal` epsilon differs. |
| `BlockLayout/BlockNode.skia.cs` | `BlockNode.h`/`.cpp` | 100 | faithful-1:1 | Bypass state machine and invalidation propagation exact. |
| `BlockLayout/BlockNodeBreak.skia.cs` | `BlockNodeBreak.h`/`.cpp` | 100 | faithful-1:1 | Exact. |
| `BlockLayout/ContainerNode.skia.cs` | `ContainerNode.h`/`.cpp` | 100 | faithful-1:1 | Delegation and bounds translation exact. |
| `BlockLayout/PageNode.skia.cs` | `PageNode.h`/`.cpp` | 98 | minor | 4-case child-reuse algorithm byte-for-byte; `RemoveClippedEmbeddedUIElements` substitutes a documented containment check. |
| `BlockLayout/PageNodeBreak.skia.cs` | `PageNodeBreak.h`/`.cpp` | 100 | faithful-1:1 | Exact; `as` instead of `static_cast` is strictly safer. |
| `BlockLayout/ParagraphNode.skia.cs` | `ParagraphNode.h`/`.cpp` | 97 | minor | Line loop, `ApplyLineStackingStrategy`, RTL rect flip, gravity derivation all exact; only downstream ellipsis width is stubbed. |
| `BlockLayout/ParagraphNodeBreak.skia.cs` | `ParagraphNodeBreak.h`/`.cpp` | 100 | faithful-1:1 | Index-only `Equals`, per the C++ comment. |
| `BlockLayout/RichTextBlockBreak.skia.cs` | `RichTextBlockBreak.h`/`.cpp` | 100 | faithful-1:1 | Exact. |
| `BlockLayout/TextGravity.skia.cs` | `metadata/inc/EnumDefs.h` | 100 | faithful-1:1 | Flags and comments verbatim. |
| `BlockLayout/LineMetrics.skia.cs` | `LineMetrics.h` | 100 | faithful-1:1 | struct → class, documented; fields 1:1. |
| `BlockLayout/LineMetricsCache.skia.cs` | `LineMetricsCache.h`/`.cpp` | 100 | faithful-1:1 | Exact; file location differs from the plan's proposed path (harmless). |

### Cluster D — Drawing contexts, text sources, element-model layout partials

| File | C++ source | % | Accuracy | Verdict |
|---|---|---|---|---|
| `BlockLayout/DrawingContext.skia.cs` | `DrawingContext.h`/`.cpp` | 100 | faithful-1:1 | Matrix → offset (only ever a translation); Stage 8 highlight API omitted from the contract. |
| `BlockLayout/ContainerDrawingContext.skia.cs` | `ContainerDrawingContext.h`/`.cpp` | 100 | minor | Cache walks are no-ops — observationally equivalent since every leaf context is also a no-op. |
| `BlockLayout/ParagraphDrawingContext.skia.cs` | `ParagraphDrawingContext.h`/`.cpp` | 100 | minor | `GetTextDrawingContext()` always null by design; sole caller special-cases it. |
| `BlockLayout/ParagraphTextSource.skia.cs` | `ParagraphTextSource.h`/`.cpp` | 85 | minor | `IsInSurrogateCRLF` and host plumbing exact; `GetTextRun` deferred to Stage 4. Line-stacking fallback cross-checked against `GetLineStackingInfo` and matches. |
| `BlockLayout/PageHostedObjectRun.skia.cs` | `PageHostedObjectRun.h`/`.cpp` | 100 | faithful-1:1 | Reparent/measure-skip branching exact, comments verbatim. |
| `BlockLayout/TextBlockViewHelpers.skia.cs` | `components/text/TextBlockViewHelpers.*` | 100 | faithful-1:1 | All four algorithms and both placeholder constants exact. |
| `BlockLayout/ElementModelStubs.skia.cs` | — (placeholder) | 100 | not-a-port | Marker types so Stage-4 signatures compile. |
| `InlineUIContainer.BlockLayout.skia.cs` | `TextLayout/InlineUIContainer.cpp` | 75 | minor | Host attach/detach primitives exact; `EnterImpl`/`LeaveImpl`/`Shutdown` have no analogue (open question below). |
| `Paragraph.BlockLayout.skia.cs` | `inc/BlockTextElement.h` | 100 | faithful-1:1 | Trivial accessor port. |
| `TextElement.BlockLayout.skia.cs` | `inc/TextElement.h` | 100 | not-a-port | Scoped `KnownPropertyIndex` placeholder; only member actually referenced. |
| `BlockCollection.BlockLayout.skia.cs` | `inc/BlockTextElement.h`, `inc/TextElement.h` | 100 | minor | Collection surface maps cleanly; `ElementEdge` values exact (a comment misattributes its C++ home). |
| `InlineCollection.BlockLayout.skia.cs` | — | 100 | not-a-port | Empty organizational partial. |

**Open question (not a finding).** WinUI's `CInlineUIContainer::LeaveImpl`/`Shutdown` detach the embedded
`UIElement` on live-tree exit, independently of the `Child` property changing. In this port attach/detach is
driven only by the `Child` setter and `PageHostedObjectRun.Format`'s per-pass host reconciliation. Whether a
container removed from its collection without a subsequent `RichTextBlock` layout pass leaks its host reference
could not be settled by reading alone and is not reported as a defect.

### Cluster E — ITextView / text views + UIA text pattern

| File | C++ source | % | Accuracy | Verdict |
|---|---|---|---|---|
| `Controls/Text/Core/ITextView.skia.cs` | `inc/TextView.h` | 100 | faithful-1:1 | Exact interface mirror. |
| `Controls/Text/Core/ITextViewHost.skia.cs` | — (Uno-native) | 100 | faithful-1:1 | Documented seam replacing WinUI's down-cast. |
| `Controls/Text/Core/LinkedRichTextBlockView.skia.cs` | `LinkedRichTextBlockView.h`/`.cpp` | 100 | faithful-1:1 | Chain-walk loops and E_NOTIMPL asserts exact. |
| `Controls/Text/Core/PlainTextPosition.skia.cs` | `PlainTextPosition.h`/`.cpp` | 100 | faithful-1:1 | All members in order, CR/LF surrogate cases included. |
| `Controls/Text/Core/RichTextBlockView.skia.cs` | `RichTextBlockView.h`/`.cpp` | 85 | structural | Most methods bridge container↔flat space correctly; `TransformPositionToPage` does not (finding V3). |
| `Controls/Text/Core/TextBlockView.skia.cs` | `TextBlockView.h`/`.cpp` | 75 | minor | `TextMode::Normal` path faithful; DWriteLayout path deliberately omitted; currently dormant. |
| `Controls/Text/Core/TextBoxHelpers.skia.cs` | `TextBoxHelpers.h`/`.cpp` | 100 | faithful-1:1 | Read-only subset exact; editable-container helpers correctly excluded. |
| `Controls/Text/Core/TextPosition.skia.cs` | `TextPosition.h`/`.cpp` | 100 | faithful-1:1 | Exact, incl. WinUI's own `IsAfterLineBreak` stub. |
| `Automation/Peers/Text/TextAdapter.skia.cs` | `common/textadapter.cpp` | 90 | minor | Type-switch logic faithful; `GetPageNode` resolves only the overflow. |
| `Automation/Peers/Text/TextRangeAdapter.skia.cs` | `common/textrangeadapter.cpp` | 85 | minor | ~30 operations faithful incl. subtle fallthroughs; two honest TODO stubs. |
| `Automation/Peers/RichTextBlockAutomationPeer.cs` | `RichTextBlockAutomationPeer_Partial.cpp` | 95 | minor | Logic matches; `GetChildrenCore` starts from an empty list rather than the base result. |
| `Automation/Peers/RichTextBlockOverflowAutomationPeer.cs` | `RichTextBlockOverflowAutomationPeer_Partial.cpp` | 95 | minor | Slicing logic matches; same base-list note. |
| `Automation/Peers/TextBlockAutomationPeer.cs` | `TextBlockAutomationPeer_Partial.cpp` | 40 | structural | Deliberately keeps the pre-existing minimal `DirectUI.TextAdapter`; out of Stage 10 scope. |
| `Automation/Peers/HyperlinkAutomationPeer.cs` | `HyperlinkAutomationPeer_Partial.cpp` | 80 | minor | Property passthrough exact; two pre-existing geometry stubs (finding V5). |

### Cluster F — Element model + ITextContainer

| File | C++ source | % | Accuracy | Verdict |
|---|---|---|---|---|
| `Documents/Block.TextContainer.skia.cs` | `inc/BlockTextElement.h` | 100 | faithful-1:1 | Stub overrides exact. |
| `Documents/Block.cs` | `BlockTextElement.h`, `paragraph.cpp` | 90 | minor | All 5 DPs with WinUI defaults; `ValidateMargin` negative rejection absent. |
| `Documents/BlockCollection.ITextContainer.skia.cs` | `common/BlockCollection.cpp` | 100 | faithful-1:1 | Incl. the two-offset clamp. |
| `Documents/BlockCollection.TextContainer.skia.cs` | `common/BlockCollection.cpp` | 100 | faithful-1:1 | Line-for-line, incl. empty-collection branch and loop guards. |
| `Documents/BlockCollection.TextContainer2.skia.cs` | — (Uno-native) | 100 | faithful-1:1 | Trivial accessor for selection-manager construction. |
| `Documents/BlockCollection.cs` | `BlockTextElement.h` | 100 | faithful-1:1 | `MarkDirty` hook wired to `ResetLengths`. |
| `Documents/Inline.TextContainer.skia.cs` | `inc/inline.h`, `TextBlock/Inline.cpp` | 100 | faithful-1:1 | Exact. |
| `Documents/Inline.cs` | `TextElement_Partial.cpp` (pattern) | 95 | minor | Invalidation recursion matches the net effect of the C++ dispatch chain. |
| `Documents/InlineCollection.ITextContainer.skia.cs` | `TextBlock/InlineCollection.cpp` | 100 | faithful-1:1 | Forwarding surface exact. |
| `Documents/InlineCollection.TextContainer.skia.cs` | `TextBlock/InlineCollection.cpp` | 100 | faithful-1:1 | EOP-dummy-run, reserved-boundary and "-1" nesting arithmetic all exact. |
| `Documents/InlineCollection.cs` | `InlineCollection.cpp`, `TextSchema.cpp` | 70 | minor | Collection routing sound; `ValidateInline` never calls the ported `TextSchema`. |
| `Documents/TextElement.TextContainer.skia.cs` | `RichTextArea/TextElement.cpp` | 90 | minor | Offset/containment exact; `GetFlowDirection()` hardcodes LTR for non-`Run`. |
| `Documents/TextElement.cs` | `inc/TextElement.h`, `TextElement.cpp` | 100 | faithful-1:1 | All inheriting DPs match metadata; parent walk restructured but equivalent. |
| `Documents/TextElementCollection.cs` | `RichTextArea/TextElementCollection.cpp` | 100 | minor | Faithful port with zero callers — dead code. |
| `Documents/InheritedProperties.cs` | `InheritedProperties.h`, `Typography.cpp` | 100 | faithful-1:1 | All 39 Typography fields, same order, same defaults. |

### Cluster G — Element-model leaves + TextPointer + TextSchema

| File | C++ source | % | Accuracy | Verdict |
|---|---|---|---|---|
| `Documents/Run.TextContainer.skia.cs` | `TextBlock/CRun.cpp` | 85 | faithful-1:1 | Position math exact; `SetText` flags folded into the DP, `IsRightToLeft` is a WinUI stub anyway. |
| `Documents/Run.cs` | — (Uno-native) | 100 | not-a-port | HarfBuzz shaping; the DWrite/LineServices analogue the port does not replicate. |
| `Documents/Span.TextContainer.skia.cs` | `TextBlock/Inline.cpp` | 100 | faithful-1:1 | Incl. DEBUG assert and empty-inlines path. |
| `Documents/Span.cs` | `Span_Partial.cpp` | 90 | minor | Peer walk exact; `OnDisconnectVisualChildren` absent (likely GC-covered, unconfirmed). |
| `Documents/Paragraph.cs` | `Paragraph_Partial.cpp` | 90 | minor | Peer walk exact; same `OnDisconnectVisualChildren` note. |
| `Documents/Paragraph.TextContainer.skia.cs` | `RichTextArea/paragraph.cpp` | 85 | faithful-1:1 | Position math exact; `CompressInlinesWhitespace` handled at the XAML-parse layer instead. |
| `Documents/LineBreak.TextContainer.skia.cs` | `TextBlock/LineBreak.cpp` | 100 | faithful-1:1 | U+2028 and nesting split exact. |
| `Documents/InlineUIContainer.Properties.cs` | `TextLayout/InlineUIContainer.cpp` | 90 | faithful-1:1 | Three-step detach/set/attach sequence exact. |
| `Documents/InlineUIContainer.TextContainer.skia.cs` | `TextLayout/InlineUIContainer.cpp` | 100 | faithful-1:1 | Exact. |
| `Documents/TextPointer.cs` | `TextPointer_Partial.cpp`, `TextPointerWrapper.h` | 100 | faithful-1:1 | Public surface 1:1. |
| `Documents/TextPointer.skia.cs` | `RichTextArea/TextPointerWrapper.cpp` | 95 | faithful-1:1 | GC replaces the weak-ref liveness check, documented. |
| `Documents/TextElement.TextPointers.cs` | `TextElement_Partial.cpp` | 100 | faithful-1:1 | Four edge properties exact. |
| `Documents/TextElement.TextPointers.skia.cs` | `RichTextArea/TextElement.cpp` | 90 | minor | Edge→gravity switch exact; detached case returns null instead of throwing. |
| `Documents/TextSchema.cs` | `RichTextArea/TextSchema.cpp` | 100 | faithful-1:1 | Complete and correct — but never called (finding V6). |
| `Documents/TextNestingType.skia.cs` | `metadata/inc/EnumDefs.h` | 100 | faithful-1:1 | Exact. |
| `Documents/Hyperlink.mux.cs` | `inc/Hyperlink.h`, `HyperLink_Partial.cpp` | 90 | minor | `IsFocusable` shape matches; `IsActive()` pair omitted (disclosed in-code). |

### Cluster H — Selection + text highlighting

| File | C++ source | % | Accuracy | Verdict |
|---|---|---|---|---|
| `Controls/TextBlock/IJupiterTextSelection.skia.cs` | `Inc/ITextSelection.h` | 100 | faithful-1:1 | 1:1 incl. doc comments. |
| `Controls/TextBlock/SelectionWordBreaker.skia.cs` | `SelectionWordBreaker.h`/`.cpp` | 90 | minor | Break state machines exact; ICU segmenter replaced by a whitespace fallback (TODO-marked). |
| `Controls/TextBlock/TextSelection.skia.cs` | `TextSelection.h`/`.cpp` | 100 | faithful-1:1 | Every member in order, incl. the two stubbed-in-C++ methods. |
| `Controls/TextBlock/TextSelectionManager.skia.cs` | `common/TextSelectionManager.cpp` | 85 | minor | Core pointer/keyboard/copy slice near line-for-line, bug-for-bug on `GetSelectionBoundingRect`. |
| `Controls/TextBlock/TextSelectionManager.Gripper.skia.cs` | `TextSelectionManager.cpp` (gripper) | 100 | minor | Deliberately reduced: forwards to the existing presenter, per plan. |
| `Controls/TextBlock/TextSelectionManager.h.skia.cs` | `Inc/TextSelectionManager.h` | 90 | minor | Field set mirrors the header incl. parity-only fields. |
| `Controls/TextBlock/TextSelectionSettings.skia.cs` | `TextSelectionSettings.h`/`.cpp` | 100 | faithful-1:1 | All 14 constants identical. |
| `Controls/TextBlock/TextBlock.TextContainer.skia.cs` | `TextBlock/TextBlock.cpp` | 100 | minor | Resolved-language triad collapsed to one string (reasonable bridging). |
| `Controls/TextBlock/TextBlock.BlockLayout.skia.cs` | `TextBlock/TextBlock.cpp` | 100 | minor | Dual baseline path collapsed to `ParsedText.FirstLineBaseline`, per plan. |
| `Controls/TextBlock/TextBlock.cs` | `TextBlock.cpp`, `TextSelectionManager.cpp` | 85 | minor | Keeps its own pipeline; two cross-referenced methods match. Unaffected by the highlighter bug. |
| `Documents/HighlightRegion.cs` | `components/text/HighlightRegion.h` | 100 | faithful-1:1 | Exact. |
| `Documents/TextHighlightMerge.cs` | `TextHighlightMerge.h`/`.cpp` | 100 | faithful-1:1 | Overlap resolution exact incl. diagrams; never instantiated. |
| `Documents/TextHighlightRenderer.skia.cs` | `TextHighlightRenderer.h`/`.cpp` | 100 | faithful-1:1 | Algorithm exact; zero callers (finding V4). |
| `Documents/TextHighlighterCollection.cs` | `TextHighlighterCollection.h`/`.cpp` | 100 | faithful-1:1 | Faithful; never instantiated. |
| `Documents/TextRangeCollection.cs` | `TextRangeCollection.h`/`.cpp` | 100 | faithful-1:1 | Faithful; dead code. |

### Cluster I — RichTextBlock / RichTextBlockOverflow controls

| File | C++ source | % | Accuracy | Verdict |
|---|---|---|---|---|
| `RichTextBlock/RichTextBlock.cs` | `RichTextBlock_Partial.cpp`, `RichTextBlock.cpp` | 90 | minor | Pointer routing, hyperlink bookkeeping, `GetPlainText`, `GetFocusableChildren` faithful. |
| `RichTextBlock/RichTextBlock.skia.cs` | `RichTextBlock.cpp` | 85 | minor | Measure/arrange/layout-rounding sequence faithful; invalidation collapsed to one tier. |
| `RichTextBlock/RichTextBlock.crossruntime.cs` | — (Uno-native) | 100 | not-a-port | Single `IsViewHit` override. |
| `RichTextBlock/RichTextBlock.Properties.cs` | `RichTextBlock.h`, `SetValue` | 80 | minor | DPs 1:1 on defaults/metadata; no per-property invalidation tiering, no negative-value validation. |
| `RichTextBlock/RichTextBlock.BlockLayout.skia.cs` | `RichTextBlock.cpp` | 100 | faithful-1:1 | Exact. |
| `RichTextBlock/RichTextBlock.Overflow.skia.cs` | `RichTextBlock.cpp` | 90 | minor | Chain linking faithful; the heavier invalidate helper is dead code. |
| `RichTextBlock/RichTextBlock.Selection.skia.cs` | — (Uno bridge) | 90 | minor | Owner glue correct; gripper/high-contrast members TODO-stubbed. |
| `RichTextBlock/RichTextBlock.TextPointers.cs` | `RichTextBlock.cpp`, `_Partial.cpp` | 100 | faithful-1:1 | Gravity edge cases exact. |
| `RichTextBlock/RichTextBlockOverflow.skia.cs` | `RichTextBlockOverflow.cpp` | 80 | minor | Linked layout sequence faithful; render-slice geometry wrong (V2); content edges internal-only. |
| `RichTextBlock/RichTextBlockOverflow.mux.skia.cs` | `RichTextBlockOverflow.cpp` | 80 | minor | Chain walkers faithful; `ValidateNextLink` missing (V1). |
| `RichTextBlock/RichTextBlockOverflow.Properties.cs` | `RichTextBlockOverflow.h`, `SetValue` | 85 | minor | DPs 1:1; `IsTabStop`/`TabIndex` rejection and `MaxLines` validation absent. |
| `RichTextBlock/RichTextBlockOverflow.BlockLayout.skia.cs` | `RichTextBlockOverflow.cpp` | 100 | faithful-1:1 | Exact. |

---

## 3. Verified parity findings

Ranked by corrected severity. Each survived an adversarial pass that attempted to refute it; the refutation
verdict recorded under each entry is why it stood.

### Major

| # | Finding | File | Confidence |
|---|---|---|---|
| V1 | `OverflowContentTarget` cycle detection dropped — a cyclic chain hangs the UI thread | `Controls/RichTextBlock/RichTextBlockOverflow.mux.skia.cs:17` | high |
| V2 | Overflow render-slice positioning uses raw `Margin` instead of collapsed-margin/content-render-size | `Controls/RichTextBlock/RichTextBlockOverflow.skia.cs:414` | high |
| V3 | `TransformPositionToPage` skips the container→flat position conversion its siblings perform | `Controls/Text/Core/RichTextBlockView.skia.cs:469` | medium |
| V4 | Ported highlight-merge pipeline is dead code; live renderer shows one highlighter, one range | `Documents/TextHighlightRenderer.skia.cs:155` | high |

**V1 — dropped cycle check.** *C++:* `CRichTextBlockOverflow::SetValue` (`RichTextBlockOverflow.cpp:216-240`)
special-cases `OverflowContentTarget` and requires `ValidateNextLink` (`:1312-1329`) to pass before writing
`m_pOverflowTarget`, else `E_INVALIDARG`; that helper walks `GetPrevious()` from `this` and rejects an assignment
that would close a loop. *C#:* `ValidateNextLink` appears nowhere in the repo;
`OnOverflowContentTargetChangedPartial` detaches the old link and attaches the new one unconditionally.
*Impact:* the DP value is written *before* the callback runs, so `o.OverflowContentTarget = o` immediately reaches
`ResetAllOverflowMasters`, whose `for (var pOverflow = pFirst; pOverflow is not null; pOverflow =
pOverflow.OverflowContentTarget)` loop never terminates — the UI thread hangs. Four sibling walkers
(`InvalidateAllOverflowContent`/`ContentMeasure`/`ContentArrange`/`Render`) have the same shape and are reachable
from any `RichTextBlock` property change. *Refutation verdict:* not refuted, high confidence — the whole
`Properties.cs` file is `+146/-0` versus master, so this is net-new code, not inherited debt, and nothing on the
branch contains the word "cycle".

**V2 — overflow slice geometry.** *In-repo contract:* the master's own
`RichTextBlock.PopulateLayoutsFromTree` (`RichTextBlock.skia.cs:241-243`) reads
`paragraphNode.GetCollapsedMargin()` and `GetContentRenderSize()`, with a comment explaining that block margins
collapse and that the arranged content box is wider than the measured width for right-aligned, centred or
justified paragraphs. *C#:* `RichTextBlockOverflow.PopulateLayoutsFromTree` (`:414-418`), whose own comment
claims it mirrors the master, instead reads `var margin = para.Margin;` and subtracts it from
`GetDesiredSize()`. The two spaces provably differ — `BlockNode` sets `m_margin.Top = suppressTopMargin ? 0 :
max(margin.Top, mcsIn)` and `m_margin.Bottom = 0` always, and `GetDesiredSize` adds those *collapsed* margins.
*Impact:* `Draw()` translates by `layout.Margin.Left` and clips to `layout.Size`, so overflowed text is
misaligned or wrongly clipped wherever margins collapse or a paragraph is not left-aligned. *Refutation verdict:*
not refuted, and strengthened — this is precisely the bug fixed for the master in commit `8319f19f9ee`
("Position blocks from the layout geometry"), whose file list touched only `RichTextBlock.skia.cs` and
`BlockNode.skia.cs`; the overflow copy was left on the old pattern. Classified as an internal-consistency
rendering defect rather than a C++ mismatch — WinUI renders the overflow through the node's own drawing context
and never rebuilds a box from the DP.

**V3 — position-space mismatch.** *C++:* `RichTextBlockView::TransformPositionToPage` subtracts
`m_pPageNode->GetStartPosition()` and range-checks against `GetContentLength()` — valid because `CPageNode`
measures in the container's own logical-position space. *C#:* the method is a faithful 1:1 port, but Uno's
`PageNode` measures a **flat `ParsedText` character space** (`ParagraphNode.skia.cs:576` accumulates
`lineMetrics.Length`), while `ITextView` callers pass container offsets. The same file establishes that contract
in two other places — `IsAtInsertionPosition` converts container→flat via `GetCharacterIndex`, and
`PixelPositionToTextPosition` converts flat→container — and overrides `GetContentLength()` with a container-space
computation. `TransformPositionToPage` does neither, so it range-checks a container offset against a flat length
and hands the raw offset to `PageNode.GetTextBounds`, which asserts `start + length <= m_length` in flat space.
*Impact:* `TextPointer.GetCharacterRect`, `ContainsPosition`, `GetUIScopeForPosition` and UIA/gripper rect queries
return wrong or empty results for tail positions. *Refutation verdict:* mechanism survived but two claims were
corrected — the divergence is **not** "2 per Span with plain content coinciding" (`Run.GetPositionCount` is
`GetTextLength() + 2`, so even a single plain `Run` diverges), and selection *rendering* is unaffected because
`RichTextBlock` draws flat-index highlighters through `ParsedText.Draw`. Downgraded critical → major; medium
confidence because the file already carries a `TODO Uno (9b render)` acknowledging the same impedance for bounds
queries, so part of this may be an accepted staged gap.

**V4 — dead highlight-merge pipeline.** *C++:* `TextSelectionManager::HWRender`/`D2DRender`
(`TextSelectionManager.cpp` ~1540/1625) call `TextHighlightRenderer::HWRenderCollection` with the full highlighter
collection plus the selection region, and `IterateMergedHighlighters` composites them through
`TextHighlightMerge`. *C#:* `HWRenderCollection`, `IterateMergedHighlighters` and `HWHighlightRect` have **zero
callers** repo-wide, and `TextHighlightMerge` is referenced only from inside the dead method.
`RichTextBlock.GetParagraphHighlighters` (`RichTextBlock.skia.cs:322-378`) builds a flat, unmerged list with the
selection appended last and passes it to `ParsedText.Draw`, which does
`var highlighter = highlighters.FirstOrDefault(); var selection = highlighter?.Ranges?.FirstOrDefault();`.
*Impact:* with any app-set `TextHighlighters` entry overlapping a paragraph, the selection highlight becomes
**entirely invisible**; two independent highlighters show only the first. `TextBlock` is unaffected — its live
renderer is `UnicodeText.Draw`, which aggregates all highlighters via a `RangeSlicer`. *Refutation verdict:* not
refuted; two corrections. The multi-range sub-claim collapses into the multi-highlighter one because
`GetParagraphHighlighters` already flattens each `TextRange` into its own highlighter. And the dead-renderer half
is a *documented* deviation (`TextSelectionManager.h.skia.cs:32-35` says rendering funnels through the
ParsedText pipeline) — though that note is itself inaccurate, since `TextHighlightMerge` is not in the live path.
Downgraded critical → major: it needs app highlighters plus selection to manifest; default rendering and plain
selection are correct.

### Minor

| # | Finding | File | Confidence |
|---|---|---|---|
| V5 | `GetClickablePointCore` returns the origin although the text-view infrastructure now exists | `Automation/Peers/HyperlinkAutomationPeer.cs:179` | medium |
| V6 | `TextSchema` nesting validation is fully implemented but never invoked | `Documents/TextSchema.cs:107` | high |
| V7 | Overflow `ContentStart`/`ContentEnd` stay `NotImplemented` despite working internal logic | `Controls/RichTextBlock/RichTextBlockOverflow.skia.cs:123` | medium |
| V8 | Property-changed callbacks collapse WinUI's 4-tier invalidation into one always-heaviest path | `Controls/RichTextBlock/RichTextBlock.Properties.cs:241` | high |

**V5.** *C++:* `HyperlinkAutomationPeer_Partial.cpp` `GetClickablePointCore` (~299) computes the point via
`CTextAdapter::GetTextView` + `hyperlink->GetTextContentStart/End()` + `ITextView::TextRangeToTextBounds`.
*C#:* both `GetClickablePointCore` and `GetBoundingRectangleCore` `return default;` with comments claiming the
infrastructure is unavailable, although `TextAdapter.GetTextView`, `Hyperlink.GetContentStart/End` and
`RichTextBlockView.TextRangeToTextBounds` now exist and are used in the same cluster. *Refutation verdict:* not
refuted but heavily downgraded (major → minor) on three grounds. The file is `+7/-1` on this branch — both stubs
pre-date it. The `GetBoundingRectangleCore` half is genuinely blocked: its Uno equivalent,
`RichTextBlockView.GetBoundsCollectionForElement`, still throws (`TODO Uno (Stage 7)`). And the headline impact
("UIA navigation to hyperlinks is broken") is wrong: in both C++ and the C# port, `RangeFromLink` **discards** the
hit-tested position and builds the range from the hyperlink's own content offsets, taking only `gravity` from the
hit-test. What remains is real but small — a `(0,0)` clickable point and a gravity derived from the origin.

**V6.** *C++:* `CTextElementCollection::AppendImpl`/`InsertImpl` (`TextElementCollection.cpp:43,69`) call
`ValidateTextElement`; `CInlineCollection::ValidateTextElement` (`InlineCollection.cpp:624-634`) calls
`CTextSchema::InlineCollectionSupportsElement`, and `HyperlinkSupportsElement` (`TextSchema.cpp:95-138`) accepts
only `Run` and non-`Hyperlink` `Span`. *C#:* `TextSchema.cs` is a faithful port with **zero call sites**; the live
gate is `InlineCollection.ValidateInline`, which only rejects an `InlineUIContainer` under a `TextBlock`-owned
collection. *Impact:* nesting a `Hyperlink`, `InlineUIContainer` or `LineBreak` inside a `Hyperlink`'s `Inlines`
silently succeeds where WinUI throws. *Refutation verdict:* not refuted, high confidence, but downgraded
major → minor: the `TextBlock` half already matches WinUI (`TextBlockSupportsElement` likewise rejects only
`InlineUIContainer`), so the entire delta is the Hyperlink-content constraint, and the "downstream code was not
written for this shape" argument is speculative — no concrete failure was demonstrated.

**V7.** The generated stub file still declares `ContentStart`, `ContentEnd` and `GetPositionFromPoint` as
`[Uno.NotImplemented("__SKIA__")]` throwing members, while `RichTextBlockOverflow.skia.cs` supplies correct
`internal GetContentStart()`/`GetContentEnd()` ports that are already consumed by `TextAdapter`.
*Refutation verdict:* not refuted, but one third of it is wrong and must be corrected — **there is no backing
logic for the overflow's `GetPositionFromPoint`**; that member is genuinely unported, so throwing is the expected
state. The residual defect is two complete-but-`internal` properties, consistent with the repo's documented
"expose a new port as internal until validated" gating. Downgraded major → minor.

**V8.** *C++:* `CRichTextBlock::SetValue` dispatches to four distinct methods — `InvalidateContent()` for
font/shaping DPs, `InvalidateContentMeasure()` for layout DPs, `InvalidateContentArrange()` for
`IsColorFontEnabled`, and `case RichTextBlock_Foreground: InvalidateRender();` for the brush. *C#:* every DP
callback calls `InvalidateRichTextBlock()` → `InvalidateContent()` → `_pageNode?.InvalidateContent()` (setting
`m_isContentDirty` on every `BlockNode`, forcing a full re-shape) plus `InvalidateOverflowChainContentMeasure()`.
The `Foreground` path does call the cheap partial, but then runs the heavy path too. *Impact:* a `Foreground`
animation (a routine VisualState pattern) re-shapes the whole content and re-measures the overflow chain.
*Refutation verdict:* facts confirmed on both sides — including that `RichTextBlockOverflow.mux.skia.cs` *does*
implement all three tiers, so the omission is asymmetric rather than a naming convention. Downgraded to minor
because it is pure over-invalidation: the rendered result is identical, only cost differs, and the prior review
classified the analogous overflow case the same way.

---

## 4. Cross-cutting observations

Three patterns recur across the verified findings and are worth treating as themes rather than isolated bugs.

**Ported-but-unwired code.** `TextSchema`, `TextHighlightMerge`, `TextHighlighterCollection`,
`TextRangeCollection`, `TextHighlightRenderer`, `TextElementCollection.MarkDirty` and
`RichTextBlock.InvalidateOverflowChainContent(bool)` are all faithful ports with zero callers. Each is
individually harmless; collectively they mean a "faithful-1:1" verdict on a file does not imply the behaviour
reaches the user. Any follow-up should either wire these up or delete them, because two parallel implementations
of the same concern will drift.

**Master/overflow asymmetry.** The master `RichTextBlock` received fixes (layout geometry in `8319f19f9ee`, the
public `TextPointer` surface) and the overflow received the four-tier invalidation the master lacks. V1, V2, V7
and V8 are all instances of one side having what the other does not. A mechanical diff of the two
`PopulateLayoutsFromTree` implementations, and of the two `SetValue` validation paths, would likely surface more.

**Container space vs flat space.** `ParsedText` indexes glyphs; `ITextContainer` indexes reserved positions
(2 extra per element). The port converts at most call sites and documents the contract, but V3 shows the
conversion is not applied uniformly. A single named helper pair (`ToFlat`/`ToContainer`) with every crossing
routed through it would make the remaining sites auditable.

---

## 5. Unverified findings

> **These findings were NOT put through the adversarial verification pass.** They fell below the
> critical/major tier that the refutation pass was scoped to (minor and intended-deferral only), so each rests on
> a single auditor's reading of both sides. Treat the evidence as reported but the severity and impact as
> provisional. No critical or major finding was left unverified.

| # | Severity | Finding | File:line |
|---|---|---|---|
| U1 | intended-deferral | `EqualsForShaping` omits the `InheritedProperties`/Typography comparison | `RichTextServices/TextRunProperties.skia.cs:106` |
| U2 | intended-deferral | `TextTrimming` Character/WordEllipsis never truncates or shows an ellipsis | `RichTextServices/Skia/SkiaTextLine.skia.cs:227` |
| U3 | intended-deferral | `TextLine.Draw` always throws `NotSupportedException` (currently dead code) | `RichTextServices/Skia/SkiaTextLine.skia.cs:223` |
| U4 | minor | `IsCloseReal` uses a fixed absolute epsilon instead of WinUI's magnitude-relative one | `BlockLayout/BlockLayoutHelpers.skia.cs:957` |
| U5 | intended-deferral | `TraverseTextElementTreeForFormat` always reports no format boundary | `Automation/Peers/Text/TextRangeAdapter.skia.cs:1402` |
| U6 | intended-deferral | Hyperlink children never surfaced via `GetChildren`/`RangeIsInLink` | `Automation/Peers/Text/TextRangeAdapter.skia.cs:257` |
| U7 | intended-deferral | `GetPageNode` resolves only `RichTextBlockOverflow`, not the master | `Automation/Peers/Text/TextAdapter.skia.cs:488` |
| U8 | intended-deferral | `TextBlock`'s UIA Text pattern bypasses this port's `TextAdapter` entirely | `Automation/Peers/TextBlockAutomationPeer.cs:32` |
| U9 | minor | Hyperlink content validation (`TextSchema`) never invoked from the collection gate | `Documents/InlineCollection.cs:238` |
| U10 | minor | Non-`Run` `TextElement`s never inherit `FlowDirection` into resolved formatting | `Documents/TextElement.TextContainer.skia.cs:177` |
| U11 | minor | `MUX Reference` header cites a commit not in the stated tag (7+ files) | `Documents/BlockCollection.ITextContainer.skia.cs:3` |
| U12 | minor | Ported `CTextElementCollection::MarkDirty` dispatch is dead code | `Documents/TextElementCollection.cs:31` |
| U13 | minor | `Block.Margin` accepts negative `Thickness` values WinUI rejects | `Documents/Block.cs:109` |
| U14 | minor | `GetTextPointer` returns null instead of throwing for a detached `TextElement` | `Documents/TextElement.TextPointers.skia.cs:48` |
| U15 | minor | `OnHolding`/`OnRightTapped` substitute the wrong flyout-state predicate | `Controls/TextBlock/TextSelectionManager.skia.cs:437` |
| U16 | intended-deferral | CJK/Thai/Korean word-break segmentation falls back to space-delimited logic | `Controls/TextBlock/SelectionWordBreaker.skia.cs:650` |
| U17 | minor | Negative `MaxLines`/`LineHeight` silently accepted instead of rejected | `Controls/RichTextBlock/RichTextBlock.Properties.cs:185` |

U2 deserves a note despite its tier: it is the user-visible half of the trimming gap and is reachable on every
Arrange pass, not gated behind dead code. It fails safe (hard clip, no exception) and is planned work (plan risk
R2 / Stage 6), which is why it is filed as a deferral — but it is the single most likely finding here to be
reported as a bug by an app developer. U9 and V6 are two views of the same unwired `TextSchema`: U9 reports the
missing call from the collection, V6 reports the orphaned implementation; both are retained as filed. U11
overlaps section 6 and is fixed by the same edit.

---

## 6. MUX Reference header fix-list

### 6.1 The branch-wide correction

`winui3/release/1.8.2` resolves to `b1db15715bfead9fe8ad2e7f78b0172589225e69` (2025-09-23).
`4a1c6184ca277b8db00424e1157ec41acc9933fa` is a `main`-branch commit (2026-06-18) contained in **no release tag**.
The pairing is therefore invalid wherever it appears. The correct clause is:

```
before:  tag winui3/release/1.8.2, commit 4a1c6184c
after:   tag winui3/release/2.4.0, commit e8442d07a
```

**102 files** carry it (100 on a single header line, 2 on the trailing line of a wrapped multi-line header:
`Controls/RichTextBlock/RichTextBlock.TextPointers.cs` and `Documents/TextElement.TextPointers.cs`). Because the
substring is identical in every case, one pass fixes them all:

```bash
cd D:/Work/uno-worktrees/richtextblock-winui
grep -rl "tag winui3/release/1.8.2, commit 4a1c6184c" src/Uno.UI --include=*.cs \
  | xargs sed -i 's|tag winui3/release/1\.8\.2, commit 4a1c6184c|tag winui3/release/2.4.0, commit e8442d07a|g'
```

Content was re-verified against `2.4.0` across all 102 — **no drift resulted from the wrong citation**, and all
131 unique cited C++ basenames exist at the tag and resolve to a unique path. This is a metadata fix only.

### 6.2 Per-file exceptions

| File | Issue | Current | Proposed |
|---|---|---|---|
| `Automation/Peers/HyperlinkAutomationPeer.cs` | tag `1.8.4`, no commit | `// MUX Reference HyperlinkAutomationPeer_Partial.cpp, tag winui3/release/1.8.4` | `// MUX Reference HyperlinkAutomationPeer_Partial.cpp, tag winui3/release/2.4.0, commit e8442d07a` |
| `Automation/Peers/RichTextBlockAutomationPeer.cs` | tag `1.8.4`, no commit | `// MUX Reference RichTextBlockAutomationPeer_Partial.cpp, tag winui3/release/1.8.4` | `…, tag winui3/release/2.4.0, commit e8442d07a` |
| `Automation/Peers/RichTextBlockOverflowAutomationPeer.cs` | tag `1.8.4`, no commit | `// MUX Reference RichTextBlockOverflowAutomationPeer_Partial.cpp, tag winui3/release/1.8.4` | `…, tag winui3/release/2.4.0, commit e8442d07a` |
| `Automation/Peers/TextBlockAutomationPeer.cs` | tag `1.8.4`, no commit | `// MUX Reference TextBlockAutomationPeer_Partial.cpp, tag winui3/release/1.8.4` | `…, tag winui3/release/2.4.0, commit e8442d07a` |
| `Controls/RichTextBlock/RichTextBlock.cs` | tag `1.4.2`, no commit; line indented inside the namespace | `\t// MUX Reference RichTextBlock_Partial.cpp, tag winui3/release/1.4.2` | `// MUX Reference RichTextBlock_Partial.cpp, tag winui3/release/2.4.0, commit e8442d07a` — move to file top |
| `Controls/Text/Core/TextBoxHelpers.skia.cs` | wrong C++ file in an inline citation | `// MUX Reference EnumDefs.h (TagConversion). Controls whether GetCharacter substitutes a` | `// MUX Reference TextBoxHelpers.h (TagConversion). Controls whether GetCharacter substitutes a` |
| `Automation/Peers/HyperlinkAutomationPeer.cs` | missing MIT/MUX copyright block | file starts at the BOM, then the MUX line | prepend `// Copyright (c) Microsoft Corporation. All rights reserved.` and `// Licensed under the MIT License. See LICENSE in the project root for license information.` |
| `Controls/RichTextBlock/RichTextBlock.cs` | missing MIT/MUX copyright block | file starts `#nullable enable` | prepend the same two lines |

`1.8.4` and `1.4.2` are separate release lines, neither an ancestor of `2.4.0` (verified via `merge-base`), so
these are not merely stale — they point at content that may differ. The `TagConversion` misattribution was
confirmed at the tag: the enum is declared in `src/dxaml/xcp/core/text/Inc/TextBoxHelpers.h`; `EnumDefs.h` has no
such symbol.

### 6.3 Files with no header

42 of the 149 changed files carry no top-of-file header. Classification:

| Classification | Count | Action |
|---|---|---|
| Generated stubs (`Generated/**`) | 7 | none — Phase-6 compliant, verified attribute/`#if`-only |
| Genuinely Uno-native | 9 | none — each self-declares its seam/placeholder role |
| Pre-existing, out of scope | 20 | none — added 2018–2025, only lightly touched here |
| **Genuine ports missing a header** | **6** | **add the proposed line below** |

| File needing a header | Proposed line |
|---|---|
| `Controls/RichTextBlock/RichTextBlock.Properties.cs` | `// MUX Reference RichTextBlock_Partial.cpp, tag winui3/release/2.4.0, commit e8442d07a` |
| `Controls/RichTextBlock/RichTextBlock.skia.cs` | `// MUX Reference RichTextBlock.cpp, tag winui3/release/2.4.0, commit e8442d07a` |
| `Documents/Block.cs` | `// MUX Reference BlockTextElement.h (CBlock), tag winui3/release/2.4.0, commit e8442d07a` |
| `Documents/BlockCollection.BlockLayout.skia.cs` | `// MUX Reference TextPointerWrapper.h (CTextPointerWrapper::ElementEdge), tag winui3/release/2.4.0, commit e8442d07a` |
| `Documents/Paragraph.BlockLayout.skia.cs` | `// MUX Reference BlockTextElement.h (CParagraph::GetInlineCollection), tag winui3/release/2.4.0, commit e8442d07a` |
| `Documents/InlineCollection.BlockLayout.skia.cs` | `// MUX Reference InlineCollection.cpp, tag winui3/release/2.4.0, commit e8442d07a` |

The nine Uno-native files are `RichTextBlock.Selection.skia.cs`, `RichTextBlock.crossruntime.cs`,
`Controls/Text/Core/ITextViewHost.skia.cs`, `Documents/BlockCollection.TextContainer2.skia.cs`,
`BlockLayout/ElementModelStubs.skia.cs`, `RichTextServices/Skia/ISkiaParagraphSource.skia.cs`,
`RichTextServices/Skia/SkiaTextLineBreak.skia.cs`, `RichTextServices/TextRunCache.BlockLayout.skia.cs` and
`Documents/TextElement.BlockLayout.skia.cs`. `InlineCollection.BlockLayout.skia.cs` is the lowest-priority of the
six, carrying no ported code — only a pointer comment. Two cosmetic notes:
`BlockCollection.TextContainer2.skia.cs`'s numeric suffix clashes with the existing
`BlockCollection.TextContainer.skia.cs` / `BlockCollection.ITextContainer.skia.cs` partials and should be renamed
or folded; and pre-existing `Documents/Hyperlink.mux.cs` opens with a bare `// Hyperlink.h, Hyperlink.cpp` line
that reads like a malformed header — out of scope here, worth a follow-up.

---

## 7. Missing features backlog

Priorities are the sweep's own. "Status" uses: *silently-no-op* (API accepted, nothing happens),
*absent-from-surface* (no code path at all), *throws-at-runtime*, *partially-implemented*,
*not-implemented-attribute* (still a generated `[Uno.NotImplemented]` stub).

### P1

| Feature | WinUI source | Status | Impact | Effort |
|---|---|---|---|---|
| `TextTrimming` ellipsis never rendered | `LsTextLine::Collapse` :337 / `Draw` :266; `BlockLayoutHelpers::CreateCollapsingSymbol` :178; `ParagraphNode::CollapseLine` :981 | silently-no-op | Text hard-clips with no "…" while `IsTextTrimmed` reports `true`; same for `MaxLines` trimming | large |
| `RichTextBlockOverflow` has no input handling | `RichTextBlockOverflow.cpp` OnPointer* :1731-1835, OnGotFocus :1974, OnTapped :2038, OnKey* :2137, HitTestLink :501 | absent-from-surface | Every column after the first is inert: no selection, no link clicks, no cursor, no context menu, no Tab | large |
| Overflow paints no selection highlight or `TextHighlighters` | `RichTextBlockOverflow.cpp` HWRenderSelection :2193, UpdateSelectionAndHighlightRegions :1534 | not-implemented-attribute | `SelectAll()` highlights only the master; highlighting stops at the column boundary | medium |
| Mid-paragraph overflow renders the wrong lines | `ParagraphNode::DrawCore` :742; `LsTextLine::Draw` :266 | partially-implemented | A paragraph split across a break repaints from line 0 — the reader sees duplicate text and loses the middle | medium |
| Focused `Hyperlink` cannot be keyboard-activated, no focus rect | `CHyperlink::KeyDown/KeyUpEventListener` :379/:357 → `Navigate` :557; `RichTextBlock::HWRenderFocusRects` :2696 | absent-from-surface | Hyperlinked rich text is keyboard-inaccessible — a WCAG-level problem | medium |
| Overflow `ContentStart`/`ContentEnd`/`GetPositionFromPoint` throw | `RichTextBlockOverflow_Partial.cpp` :94/:105/:133 | throws-at-runtime | Pagination readers and column hit-testing crash; the only remaining throwing members on the family | small |

The trimming gap and the mid-paragraph overflow gap need the **same missing primitive**: the ability to draw a
sub-range of a paragraph's `RenderLine`s. `IParsedText.Draw` takes no first-line/line-count parameter today.
Building that once unblocks both. The overflow `ContentStart`/`ContentEnd` fix is the cheapest item on the whole
list — the implementations exist and are already consumed internally; only public forwarders and stub removal are
needed. `GetPositionFromPoint` is the one member of that trio that genuinely needs new code, though the
overflow's `RichTextBlockView` is already built in `SetupLinkedBlockLayout`.

### P2

| Feature | WinUI source | Status | Impact | Effort |
|---|---|---|---|---|
| Selection drag does not cross into a linked overflow | `TextSelectionManager::IsPointOverLinkedView` :2287 | partially-implemented | A passage spanning a column break cannot be selected by dragging, though `Select`/`SelectAll` build it correctly | medium |
| Touch-selection grippers are no-ops | `TextSelectionManager` ShowGrippers :2825, UpdateGripperPositions :3265 | silently-no-op | No touch text selection at all; mouse and keyboard only | large |
| `TextReadingOrder` / bidi detection inert | `BlockLayoutHelpers::GetTextReadingOrder` :1021 | silently-no-op | RTL content always laid out LTR; `TextAlignment.DetectFromContent` always resolves Left | large |
| UIA `RangeFromChild` / `GetChildren` empty on the master | `CTextAdapter::GetPageNode`; `CTextRangeAdapter::GetChildren` | partially-implemented | Embedded controls inside rich text are invisible to assistive tech unless hosted in an overflow column | small |
| No built-in Copy / Select all context menu | `RichTextBlockCommandHandler::Invoke`; `RichTextBlock_Partial.cpp` OnContextMenuOpeningHandler :305 | absent-from-surface | Right-click/long-press offers no way to copy; apps must author their own flyout | medium |

Explicit `FlowDirection` on a `Run` or on the control *is* honoured — only content-driven detection and full
Unicode bidi reordering are missing, and that is a pre-existing Uno text-engine limitation rather than a
regression of this port. The UIA item is mechanical: `RichTextBlock` already holds `_pageNode`, it is simply not
exposed on the master's arm of `GetPageNode`'s switch. The context-menu gap is shared with `TextBlock`, so a
single default `TextCommandBarFlyout` would serve both.

### P3

| Feature | WinUI source | Status | Impact | Effort |
|---|---|---|---|---|
| `OpticalMarginAlignment` ignored | `BlockLayoutHelpers::GetOpticalMarginAlignment` :957 | not-implemented-attribute | `TrimSideBearings` has no effect; display type does not optically align | medium |
| `IsColorFontEnabled` cannot be turned off | `BlockLayoutHelpers::GetIsColorFontEnabled` :886 | not-implemented-attribute | Emoji always render in colour; monochrome designs cannot be expressed | small |
| `Typography` attached properties entirely inert | `Typography` runtimeclass, controls IDL :620-756 | not-implemented-attribute | No OpenType feature can be requested from XAML — ligatures, small caps, fractions, stylistic sets, all write-only | large |
| Selection brushes never switch to high-contrast system colours | `RichTextBlock::SetBackPlateConfiguration` :1463/:2913 | partially-implemented | Selection highlight can fall below the theme's guaranteed contrast (text rendering itself does honour HC) | small |
| Caret browsing (F7) not implemented | `TextSelectionManager` ShowCaretElement :3459, CaretOnKeyDown :3749 | silently-no-op | Arrow-key caret navigation announced as available but does nothing | large |
| `RichTextBlock` renders nothing on non-Skia targets | `CRichTextBlock` Measure/Arrange/HWRenderContent | absent-from-surface | Native Android/iOS and legacy WASM-DOM heads show an empty control | large |
| Paragraph default metrics ignore OS text scale | `BlockLayoutHelpers::GetParagraphProperties` → `GetScaledFontSize` | partially-implemented | Glyphs scale but default line height does not, so spacing can be too tight at high text scale | small |

The non-Skia item is informational, not schedulable work: `AGENTS.md` puts native targets in maintenance-only,
and on `master` `RichTextBlock` was already a 46-line stub that rendered nothing anywhere — so this is not a
regression. It belongs in the release notes so the Skia-only scope is stated rather than discovered. Note also
that `IsColorFontEnabled` is *inverted* relative to actual behaviour: the helper returns `false` while the Skia
engine renders colour glyphs unconditionally.

### Confirmed non-gaps

The sweep explicitly re-checked and cleared, contrary to the older `gap-closure-plan.md`: `BaselineOffset`,
`TextLineBounds`, `CharacterSpacing`, `TextAlignment.Justify` and `DetectFromContent`, `InlineUIContainer`
measure/arrange/reparenting/hit-testing, the `TextPointer` surface on `RichTextBlock` and `TextElement`,
`TextHighlighters` on the master, mouse and keyboard selection, Ctrl+A / Ctrl+C, `SelectionFlyout`,
`ContextMenuOpening`, and per-`Inline` text-scale-factor. `FindText`/`FindAttribute` returning not-supported is
**parity**, not a gap — `CTextRangeAdapter` does not implement them either. One stale comment should be deleted:
`TextRangeAdapter.skia.cs:230` claims `InlineUIContainer.GetChild` throws; it is implemented, and the surrounding
`catch` is dead.

### Not settled by reading

Two questions need runtime evidence, not more source reading. Whether the mid-paragraph overflow mis-slice is
visible in every configuration — the existing render test asserts only red-pixels-present-inside /
absent-below, which a line-0 repaint also satisfies; a screenshot comparing the master's last line against the
overflow's first line would settle it. And whether the `#if !__WASM__` legacy-DOM branches in `RichTextBlock.cs`
are still reachable in current heads. No builds or tests were run for this validation.

---

## 8. Appendix: refuted claims

One finding from the cluster audits was put to the adversarial pass and **refuted**. It is recorded here so the
analysis is not lost and the same hypothesis is not chased again.

**R1 — "Ellipsis collapsing-character width is hard-coded to 0"** — `BlockLayout/BlockLayoutHelpers.skia.cs:847`,
originally filed *major*, **corrected to intended-deferral**, high confidence.

*Original claim.* `BlockLayoutHelpers.cpp:913` `ComputeCollapsingCharacterWidth` computes the real advance
(`AdvanceWidth * fontSize / DesignUnitsPerEm`) via `GetGlyphIndices`/`GetDesignGlyphMetrics`; the C# sets
`eAdvance = 0.0f` unconditionally. The claimed impact was that `CreateCollapsingSymbol` builds an ellipsis symbol
of advertised width 0, so trimmed lines would under-reserve space and overlap the ellipsis with preceding text.

*Refuter's reasoning.* The C++ facts and the C# reading both check out, but **the stated impact is provably
false: nothing consumes that width.** `TextCollapsingCharacters.skia.cs` is a no-op shell whose `Width` and
`Draw` both `throw new NotSupportedException("TODO Uno (Stage 6): line collapsing / ellipsis is not yet
ported")`, and `SkiaTextLine.Collapse` is `=> this` with `HasCollapsed => false`. The symbol produced by
`CreateCollapsingSymbol` is therefore constructed and immediately discarded; no ellipsis is ever laid out, so
nothing can under-reserve or overlap. This is one leaf of the documented deferral R2 in `port-plan.md:492`
("Collapse/text-trimming + ellipsis model (no Uno equivalent) … Net-new; isolate in Stage 3"), also flagged at
plan lines 169 and 444(c). Filing the zero-width constant as a standalone major defect mis-scopes a known
in-plan deferral whose real surface is `TextLine.Collapse` — captured above as U2 and as the P1 trimming backlog
item.
