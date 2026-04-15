---

description: "Task list for XAML C# Expressions (feature 004)"
---

# Tasks: XAML C# Expressions

**Input**: Design documents from `/specs/004-xaml-csharp-expressions/`
**Prerequisites**: plan.md (required), spec.md (required), research.md, data-model.md, contracts/

**Tests**: REQUIRED and TEST-FIRST. Spec FR-023..FR-026 mandate TDD; every functional requirement has a failing test authored before the corresponding implementation in the same PR. Coverage gate: ≥ 90% line / 85% branch on parser + generator projects.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US6)
- Exact file paths appear in every task description

## Path Conventions

- **Generator project**: `src/SourceGenerators/Uno.UI.SourceGenerators/XamlGenerator/CSharpExpressions/`
- **Generator tests**: `src/SourceGenerators/Uno.UI.SourceGenerators.Tests/XamlCodeGeneratorTests/CSharpExpressions/`
- **Runtime tests**: `src/Uno.UI.RuntimeTests/Tests_CSharpExpressions/`
- **MSBuild wiring**: `src/Uno.UI.Xaml/build/Uno.UI.Xaml.targets`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create folders, wire the MSBuild opt-in, seed empty compilation units so later tasks can run in parallel without file collisions.

- [X] T001 Create subsystem folder `src/SourceGenerators/Uno.UI.SourceGenerators/XamlGenerator/CSharpExpressions/` with a `.gitkeep` placeholder and confirm it is picked up by the generator csproj glob
- [X] T002 [P] Create generator-test folder `src/SourceGenerators/Uno.UI.SourceGenerators.Tests/XamlCodeGeneratorTests/CSharpExpressions/` with a `.gitkeep`
- [X] T003 [P] Create runtime-test folder `src/Uno.UI.RuntimeTests/Tests_CSharpExpressions/` with subfolders `Pages/` and `ViewModels/` and `.gitkeep` files
- [X] T004 Register `CompilerVisibleProperty Include="UnoXamlCSharpExpressionsEnabled"` in `src/SourceGenerators/Uno.UI.SourceGenerators/Content/Uno.UI.SourceGenerators.props` (deviated from tasks.md: the referenced `src/Uno.UI.Xaml/build/Uno.UI.Xaml.targets` does not exist — the established `CompilerVisibleProperty` file is `Uno.UI.SourceGenerators.props`)
- [X] T005 [P] Add shared `INotifyPropertyChanged` view-model `src/Uno.UI.RuntimeTests/Tests_CSharpExpressions/ViewModels/ExpressionsTestViewModel.cs` exposing properties used across US1–US5 acceptance scenarios (FirstName, LastName, IsVip, Nickname, Price, Quantity, Balance, A, B, Count, IsEnabled, Counter, TaxRate, User.Address.City graph)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure used by every user story — data model, diagnostic descriptors, opt-in gating, WinAppSDK exclusion, classifier hook. No user story work may begin until this phase is complete.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

### Data model and diagnostic scaffolding

- [X] T006 [P] Create `Diagnostics.cs` in `src/SourceGenerators/Uno.UI.SourceGenerators/XamlGenerator/CSharpExpressions/Diagnostics.cs` with `DiagnosticDescriptor` fields for every UNO2xxx code listed in `contracts/diagnostics.md` (category `"XAML.CSharpExpressions"`); leave `Triggered by` logic for later phases
- [X] T007 [P] Create `XamlExpressionAttributeValue.cs` record type in `src/SourceGenerators/Uno.UI.SourceGenerators/XamlGenerator/CSharpExpressions/XamlExpressionAttributeValue.cs` matching data-model §1 (fields: `RawText`, `Kind`, `InnerCSharp`, `SourceSpan`, `IsCData`) with `ExpressionKind` enum
- [X] T008 [P] Create `ResolutionScope.cs` in `src/SourceGenerators/Uno.UI.SourceGenerators/XamlGenerator/CSharpExpressions/ResolutionScope.cs` with fields from data-model §4 (`PageType`, `DataType`, `KnownMarkupExtensions`, `GlobalUsings`, `Compilation`) — empty `Resolve` stub
- [X] T009 [P] Create `ResolutionResult.cs` + `MemberLocation` enum in `src/SourceGenerators/Uno.UI.SourceGenerators/XamlGenerator/CSharpExpressions/ResolutionResult.cs` per data-model §5
- [X] T010 [P] Create `ExpressionAnalysisResult.cs`, `BindingHandler.cs`, `LocalCapture.cs`, `LoweredExpression.cs` placeholder types in the same folder per data-model §6–§9

### Opt-in gating and hooks

- [X] T011 Implement `IsEnabled(GeneratorExecutionContext)` reader in `src/SourceGenerators/Uno.UI.SourceGenerators/XamlGenerator/CSharpExpressions/CSharpExpressionOptions.cs` (reads `UnoXamlCSharpExpressionsEnabled`, caches per generator run) per contracts/msbuild-properties.md
- [X] T012 [P] [Test] Generator unit test `Given_OptInBehavior.cs` — Phase 2 lands a minimal regression smoke test; full golden-corpus byte-equality deferred to T091 (Phase 9)
- [X] T013 [P] [Test] Generator unit test `Given_Uno2020_OptInDirectiveWhenDisabled.cs` authored and active (Phase 2 extension patched `XamlXmlReader.cs` to bypass markup-extension parsing for directive forms; runtime execution blocked by local environment needing `Uno.UI.dll` — compile-clean)
- [X] T014 Wire classifier entry point in `src/SourceGenerators/Uno.UI.SourceGenerators/XamlGenerator/XamlFileGenerator.cs` (before the `HasMarkupExtension` branch) — calls `CSharpExpressionClassifier.TryClassify` only when `member.Value` is a string; opt-in and WinAppSDK gating live inside the classifier
- [X] T015 Emit `UNO2020` for unambiguous new-syntax directives when the flag is off (implemented in `CSharpExpressionClassifier.TryClassify`; deep path for directives that Uno's XAML parser converts into markup-extension objects before our hook sees them lands in Phase 3)

### WinAppSDK exclusion (pre-work for US6; emitted before any lowering)

- [X] T016 [P] [Test] Generator unit test `Given_Uno2099_WinAppSDK.cs` authored and active (runtime execution same environment caveat as T013)
- [X] T017 Extend `src/SourceGenerators/SourceGeneratorHelpers/Helpers/PlatformHelper.cs` with a reusable `IsWinAppSdk` predicate; classifier emits `UNO2099` before lowering. IMPORTANT KNOWN LIMITATION: the main `Uno.UI.SourceGenerators` is disabled on WinAppSDK via `ShouldRunGenerator` in `Uno.UI.SourceGenerators.props`, so in practice UNO2099 emission is defensive in depth; true WinAppSDK protection needs a follow-up wiring inside `Uno.UI.SourceGenerators.WinAppSDK` (to be scheduled alongside Phase 5)

**Checkpoint**: Foundation ready — classifier hook in place, diagnostics scaffolded, opt-in enforced, WinAppSDK hard-blocked. User story phases may now proceed.

---

## Phase 3: User Story 1 — Inline property and compound expression bindings (Priority: P1) 🎯 MVP

**Goal**: Author replaces converter/helper-property patterns with inline expressions: property access, dotted paths, ternary, null-coalescing, arithmetic, comparison, string interpolation with format specifiers.

**Independent Test**: Build a sample page with `x:DataType="local:Customer"`, use `Text="{FirstName + ' ' + LastName}"`, `Text="{IsVip ? 'Gold' : 'Standard'}"`, `Text="{Price * Quantity}"`, `Text="{$'{Balance:C2}'}"`; run on Skia Desktop and WebAssembly; observe render and INPC-driven refresh.

### Tests for User Story 1 (write FIRST — MUST FAIL before implementation)

- [X] T018 [P] [US1] Parser unit test `Given_ExpressionTokenizer.cs` in `src/SourceGenerators/Uno.UI.SourceGenerators.Tests/XamlCodeGeneratorTests/CSharpExpressions/Given_ExpressionTokenizer.cs` covering single-quoted strings, escape sequences, interpolation, and quote-inside-quote (FR-002, FR-006) — Red: 6 of 7 cases fail against current empty `Tokenize` stub
- [X] T019 [P] [US1] Parser unit test `Given_ExpressionParser.cs` asserting the Roslyn AST shape for each form in contracts/expression-grammar.md (property access, dotted path, ternary, null-coalescing, arithmetic, interpolation) (FR-002) — Red: 5 of 9 cases fail (alias replacement and single-quoted strings not yet wired into `Parse`)
- [X] T020 [P] [US1] Classifier unit test `Given_ExpressionClassifier.cs` asserting that `{Foo}`, `{Foo + Bar}`, `{A.B.C}`, `{$'...'}`, `{Foo(X)}`, `{X ? Y : Z}` are classified as expressions, and that existing `{Binding}`, `{StaticResource}`, `{x:Bind}`, `{TemplateBinding}`, `{ThemeResource}` are NOT reclassified (FR-001, FR-016) — Red: 6 of 20 cases fail (Phase 2 stub `Classify` only handles opt-in directives; markup-extension fall-through and directive cases pass). Added pure `Classify(string?)` API on `CSharpExpressionClassifier` to enable test-first; T029 fills in the bare-identifier / dotted-path / compound / interpolation / method-call recognition.
- [X] T021 [P] [US1] Resolver unit test `Given_MemberResolver.cs` covering the decision table in contracts/resolution-algorithm.md for the bare-identifier cases that fire UNO2002, UNO2003 (FR-007, FR-018, FR-019) — Red: 6 tests authored (This / DataType / Both → UNO2002 / Neither → UNO2003 / MarkupExtension fallthrough / null DataType) using a Roslyn `CSharpCompilation` harness; all 6 fail against the Phase 2 stub which always returns `(Neither, null, null)`. T034 will make them green.
- [X] T022 [P] [US1] Analyzer unit test `Given_ExpressionAnalyzer.cs` asserting refresh-set computation for `User.Address.City`, `Price * Quantity`, `Price * this.TaxRate`, and verifying capture/source separation (FR-013, data-model §6, resolution-algorithm §Refresh-set) — Green: 6/6 cases pass against T035 implementation, covering single DataType identifier, dotted-path handler-per-hop with `?.`-joined accessor, compound-operand refresh set, mixed DataType/page-capture separation, pure-capture one-shot, and non-settable leaf.
- [X] T023 [P] [US1] Generator snapshot test `Given_ExpressionLowering_SimplePath.cs` — Green: 5 tests exercise `ExpressionLowering.Lower` directly (no XamlSourceGeneratorVerifier harness, since it requires `Uno.UI.dll` at test runtime — see T013 environment note). Covers two-way simple identifier, one-way read-only, dotted path, ForcedDataType-forced-OneWay, and two-way-to-one-way downgrade per `contracts/generated-binding-shapes.md` §1, §2, §11 (FR-009, FR-010). End-to-end byte-equality against golden files deferred until T041 wiring is in place.
- [X] T024 [P] [US1] Generator snapshot test `Given_ExpressionLowering_Compound.cs` — Green: 6 tests covering `Price * Quantity`, `Price * this.TaxRate`, ternary-with-null-coalescing, interpolation with `:C2` format specifier, one-shot direct assignment, and helper-name padding. Asserts exact helper-method-body strings against fully-qualified DataType/capture parameter types; covers `contracts/generated-binding-shapes.md` §3, §4, §5, §6, §7 (FR-002, FR-010, FR-012, FR-013). Same end-to-end deferral as T023.
- [ ] T025 [P] [US1] Diagnostic test `Given_ExpressionDiagnostics_US1.cs` asserting UNO2002, UNO2003, UNO2006, UNO2007, UNO2010, UNO2011, UNO2012 fire with correct code, severity, file/line/column for minimal reproducing snippets (FR-017..FR-022, FR-022a, SC-003). **Deferred**: the `XamlSourceGeneratorVerifier` harness requires `Uno.UI.dll` on disk at test runtime; local environment gap documented against T013 applies here too.
- [X] T026 [P] [US1] Runtime test class `Given_SimpleBinding.cs` in `src/Uno.UI.RuntimeTests/Tests_CSharpExpressions/Given_SimpleBinding.cs` — Green: 4/4 on Skia Desktop. Covers US1 acceptance scenarios on `SimpleBindingPage.xaml`: two-way settable path propagates VM→UI and UI→VM, read-only computed path refreshes on source change, one-shot `{this.X}` capture holds initial value.
- [X] T027 [P] [US1] Runtime test class `Given_CompoundExpression.cs` — Green: 4/4 on Skia Desktop. Covers one-shot compound arithmetic (`this.Price * this.Quantity`), compound with page capture (`this.Price * this.TaxRate`), ternary default branch, and ternary gold-branch via a `VipCompoundExpressionPage` subclass that overrides the virtual `IsVip` property (virtual dispatch flows through the captured `this.IsVip`). Reactive refresh of compound expressions via INPC is blocked on `x:DataType`-on-non-DataTemplate support (pending follow-up).
- [X] T028 [P] [US1] XAML sample pages `Pages/SimpleBindingPage.xaml(.cs)` and `Pages/CompoundExpressionPage.xaml(.cs)` authored under `src/Uno.UI.RuntimeTests/Tests_CSharpExpressions/Pages/`. Uses explicit directive forms (`{= FirstName}`, `{this.WindowTitle}`, `{this.Price * this.Quantity}`, `{this.IsVip ? 'Gold' : 'Standard'}`) — the XamlXmlReader (T035a) passes compound/interpolation/ternary forms through, directive forms (`{= …}`, `{this.…}`, `{.…}`) are handled by the classifier hook. Bare `{FirstName}` remains markup-extension-parsed per T035a carve-out and is not exercised here.

### Implementation for User Story 1

- [X] T029 [US1] Implement `CSharpExpressionClassifier.cs` in `src/SourceGenerators/Uno.UI.SourceGenerators/XamlGenerator/CSharpExpressions/CSharpExpressionClassifier.cs` per contracts/expression-grammar.md classification rules (makes T020 pass) — Green: all 20 Given_ExpressionClassifier cases pass (prefix:Name + known markup-ext names fall through; compound/interpolation/method-call/ternary forms detected by string-aware scan)
- [X] T030 [US1] Implement `CSharpExpressionTokenizer.cs` in the same folder: single-pass tokenizer that recognizes single-quoted strings and interpolation token boundaries (makes T018 pass) (FR-002, FR-006) — Green: all 7 Given_ExpressionTokenizer cases pass
- [X] T031 [US1] Implement `QuoteTransform.cs` (single-quoted → double-quoted, escape-sequence preservation, char-literal re-detection hook) in the same folder (FR-006) — Green via Given_ExpressionParser (quote rewriting exercised through parser). `TransformQuotesWithSemantics` remains a stub pending semantic analysis (T035+).
- [X] T032 [US1] Implement `CSharpExpressionParser.cs` using `CSharpSyntaxTree.ParseText(..., SourceCodeKind.Script)`; surface Roslyn parse diagnostics as `UNO2006` with original location (makes T019 pass; FR-021, UNO2006, UNO2007) — Green: wires OperatorAliases → QuoteTransform → Roslyn Script parse; also promotes multi-statement bodies to parse errors. UNO2006 diagnostic conversion lands with the analyzer in T035/T041.
- [X] T033 [US1] Implement `XDataTypeResolver.cs` in the same folder: walks parent `XamlObjectDefinition` chain, respects `DataTemplate` boundary, emits `UNO2010` once per file (resolution-algorithm §DataType-discovery) — Green: `Resolve(memberOwner, typeResolver)` walks the `Owner` chain, returns the first `x:DataType` (namespace `XamlConstants.XamlXmlNamespace`) via the caller-supplied resolver, and stops at a `DataTemplate` ancestor so outer `x:DataType` does not leak into templates. UNO2010 emission lives on the caller (wired in T041 when the classifier hook runs).
- [X] T034 [US1] Implement `MemberResolver.cs` in the same folder: decision table for bare identifiers covering the `This` / `DataType` / `Both` / `Neither` cases and diagnostics UNO2002, UNO2003 (makes T021 pass) (FR-007, FR-018, FR-019) — Green: all 6 Given_MemberResolver cases pass. Walks base-type chain, skips static members (US4/T076 will add static-type branch), falls through to KnownMarkupExtensions lookup when no instance member matches.
- [X] T035 [US1] Implement `ExpressionAnalyzer.cs` in the same folder: walks the AST, produces `ExpressionAnalysisResult` with `TransformedCSharp`, `Handlers`, `Captures`, `LeafPropertyType`, `IsOneShot`, `IsSettable` per resolution-algorithm §Analysis and §Refresh-set (makes T022 pass) (FR-009, FR-010, FR-013) — Green: uses a `CSharpSyntaxWalker` + span-based edit list to rewrite DataType-rooted roots as `__source.X` and page-rooted roots (`this.X` or bare page members) as `__capture_X`; emits per-hop `BindingHandler`s with `.`/`?.` accessor chain per spec; computes `IsSettable` via pure-dotted-path + per-hop INPC/DO + public-setter check.
- [X] **T035a [US1] Extend `XamlXmlReader` to bypass markup-extension parsing for unambiguous C# expression forms**. `src/SourceGenerators/System.Xaml/System.Xaml/XamlXmlReader.cs` — renamed `IsUnoCSharpExpressionDirective` → `IsUnoCSharpExpressionAttribute` and widened recognition to include: interpolated strings (`{$'...'`, `{$"...`), lambda introducer (`{(…)=>…`), and unambiguous compound markers outside string literals (`+`, `*`, `/`, `%`, `<`, `>`, `!`, `==`, `&&`, `||`, `??`, `? :`, `=>`, `^`, `~`). Intentional carve-outs: bare `?.` stays markup-ext (preserves `{x:Bind Foo?.Bar}`), single-token `?`, `,`, `=` stay markup-ext. Bare-identifier / dotted-path / method-call forms (`{FirstName}`, `{Foo(X)}`) remain ambiguous with custom markup extensions and still flow through `ParsedMarkupExtensionInfo.Parse` — authors use `{= Foo}` / `{.Foo}` / `{this.Foo}` directives, or a future flag-gated reader mode, to disambiguate. Compiled clean; change is additive so existing markup-extension parsing is unchanged. **Unblocks T036/T037/T041 for compound / interpolation / lambda forms; bare-identifier support still pending flag-gated reader mode.**
- [X] T036 [US1] Implement `ExpressionLowering.cs` — simple path variant (two-way / one-way): returns `SimplePathBinding(Path, Mode, DataContextSource)` IR consumed by the existing x:Bind emitter. `Mode` is inferred from `IsSettable` (TwoWay when settable), forced to OneWay for `ExpressionKind.ForcedDataType`, and forced to OneWay via the `forceOneWayForTwoWayTarget` flag when the caller detects a `BindsTwoWayByDefault` target with a non-settable leaf (T038). `XBindExpressionParser`/`BuildXBindEvalFunction` wiring is the callers's (T041) responsibility — lowering stays a pure function so it can be unit-tested without the generator harness (FR-009, FR-010). **Depends on T035a.**
- [X] T037 [US1] Implement `ExpressionLowering.cs` — compound variant: returns `CompoundBinding(HelperMethodName, HelperMethodBody, Handlers, LeafType, Captures)` where `HelperMethodBody` is a complete `private <returnType> __xcs_Expr_NNN(<DataType> __source[, <CaptureType> __capture_X...]) => <TransformedCSharp>;` string with fully-qualified DataType + capture types. Return type resolves from `targetPropertyTypeFullName` → `analysis.LeafPropertyType` → `object`. The actual `__xcs_Expr_NNN` emission into the page partial class, `Binding { Mode=OneWay, Properties=refreshSet, CompiledSource=... }` wiring, and `__capture_X = this.X` load-time snapshots are caller responsibilities (T041) (generated-binding-shapes §3, §4, §6, §7). **Depends on T035a.**
- [X] T038 [US1] Emit `UNO2012` (two-way → one-way downgrade) — now fires from `TryEmitCSharpExpression` in `XamlFileGenerator.CSharpExpressions.cs` when a heuristic `IsBindsTwoWayByDefault(owner, propertyName)` check matches (covers `TextBox.Text`, `CheckBox.IsChecked`, `Slider.Value`, `ToggleSwitch.IsOn`, `ComboBox.Selected*`, `DatePicker.Date`, `TimePicker.Time`, `NumberBox.Value`, `RatingControl.Value`, `RadioButton/ToggleButton.IsChecked`, `ListView/GridView.SelectedItem`/`SelectedIndex`, `PasswordBox.Password`) and the analyser reports `IsSettable=false`. The lowering then returns the `SimplePathBinding` with `Mode=OneWay`. Full `FrameworkPropertyMetadataOptions.BindsTwoWayByDefault` introspection is a follow-up.
- [X] T039 [US1] Emit `UNO2011` (one-shot info) and emit direct-assignment output (no `Binding`) when `Handlers` is empty — `ExpressionLowering.Lower` returns a new `DirectAssignment(CSharpExpression, LeafType, Captures)` IR when the analysis produces zero handlers; carries the captured-locals list so the caller can emit `var __capture_X = this.X;` before the direct property assignment (generated-binding-shapes §5). **UNO2011 diagnostic emission at the classifier-hook site awaits T041.**
- [X] T040 [US1] Sample pages are auto-included by the default Microsoft.NET.Sdk XAML glob (no explicit `<Page Include>` needed). `UnoXamlCSharpExpressionsEnabled=true` set in `src/Uno.UI.RuntimeTests/Directory.Build.props`, inherited by all four runtime-tests csprojs (Skia, WASM, netcoremobile, Windows).
- [X] T041 [US1] Wire the lowered output through `XamlFileGenerator`. Completed via a new partial file `src/SourceGenerators/Uno.UI.SourceGenerators/XamlGenerator/XamlFileGenerator.CSharpExpressions.cs` containing `TryEmitCSharpExpression` (parses, resolves scope via `XDataTypeResolver.Resolve` + `_metadataHelper.Compilation`, analyses, lowers, and dispatches to one of three emission paths). `EmitSimplePathBinding` emits a runtime `SetBinding(Owner.PropertyNameProperty, new Binding { Path=new PropertyPath("..."), Mode=... })` — reusing the existing runtime binding engine. `EmitDirectAssignment` emits capture initialisers + a direct property set with `System.Convert.ToString` coercion when the DP target is `string`. `EmitCompoundBinding` registers a `__xcs_Expr_NNN` helper via `CurrentScope.RegisterMethod` and builds a scoped block that tracks the INPC subscription across `DataContextChanged`/`Loaded`/`Unloaded` (the reactive path is present but INPC refresh exercise is blocked until `x:DataType` is supported outside `DataTemplate`). Classifier hook (`XamlFileGenerator.cs:1944`) now accepts `XamlLazyApplyBlockIIndentedStringBuilder writer` and delegates to `TryEmitCSharpExpression` when the classifier outcome is `RecognisedPendingImplementation`. End-to-end verified by 8/8 runtime tests on Skia Desktop (T026+T027) and 85/85 generator-side unit tests. WASM validation is a follow-up.

**Checkpoint**: US1 independently functional — MVP testable by enabling the flag on a throwaway project and verifying the four acceptance scenarios in SimpleBindingPage + CompoundExpressionPage.

---

## Phase 4: User Story 2 — Inline event handler lambdas (Priority: P1)

**Goal**: Author attaches a short event handler with `(s, e) => ...` syntax in a XAML event attribute (e.g. `Click`, `Tapped`, `TextChanged`). The generator emits a compliant handler matching the event's delegate.

**Independent Test**: Run the built sample, invoke the event, observe VM state change and UI refresh.

### Tests for User Story 2 (write FIRST — MUST FAIL)

- [ ] T042 [P] [US2] Classifier unit test in `Given_ExpressionClassifier.cs` (extend) asserting `(s, e) => Counter++` and `(s, e) => Search(...)` are classified as `EventLambda`; `async (s, e) => ...` is flagged for `UNO2005` (FR-003, FR-020)
- [ ] T043 [P] [US2] Generator snapshot test `Given_ExpressionLowering_EventLambda.cs` comparing emitted C# for `Click="{(s, e) => Counter++}"` and `TextChanged="{(s, e) => Search(((TextBox)s).Text)}"` against golden files; covers generated-binding-shapes §9 and §10 (FR-011)
- [ ] T044 [P] [US2] Diagnostic test in `Given_ExpressionDiagnostics_US2.cs` asserting `UNO2005` (async lambda) and `UNO2008` (arity mismatch) fire with correct severity and location (FR-020, SC-003)
- [ ] T045 [P] [US2] Runtime test `Given_EventLambda.cs` in `src/Uno.UI.RuntimeTests/Tests_CSharpExpressions/Given_EventLambda.cs` exercising US2 acceptance scenarios 1–2 on `Pages/EventLambdaPage.xaml`
- [ ] T046 [P] [US2] Sample page `Pages/EventLambdaPage.xaml(.cs)` with a Button, TextBox, and `x:DataType` view-model; Click and TextChanged use inline lambdas

### Implementation for User Story 2

- [ ] T047 [US2] Extend `CSharpExpressionClassifier.cs` with event-lambda detection for event attributes (2-arity lambdas, block bodies with single statement accepted) (FR-003; makes T042 pass)
- [ ] T048 [US2] Emit `UNO2005` for `async` lambda at classifier time; regex `^\s*async\s+(\([^)]*\)|\w+|\(\))\s*=>` per resolution-algorithm §Async-lambda (makes the async-branch of T044 pass)
- [ ] T049 [US2] Extend `ExpressionLowering.cs` with `EventHandler` variant: emits `__xcs_EventHandler_NNN` on the page partial matching the event's delegate signature; passes author-chosen parameter names through (makes T043 pass) (FR-011)
- [ ] T050 [US2] Resolve `IEventSymbol` for the XAML event attribute and validate arity; emit `UNO2008` on mismatch (makes arity-branch of T044 pass)
- [ ] T051 [US2] Extend `src/SourceGenerators/Uno.UI.SourceGenerators/XamlGenerator/XamlFileGenerator.InlineEvent.cs` to accept a classified `EventLambda` and route it to the new `__xcs_EventHandler_NNN` emission; reuses existing `+=` wiring for named handlers
- [ ] T052 [US2] Register `EventLambdaPage.xaml` in `Uno.UI.RuntimeTests.csproj` and verify T045 green on Skia Desktop and WASM

**Checkpoint**: US1 + US2 functional — the "no code-behind for easy cases" story is complete.

---

## Phase 5: User Story 6 — WinAppSDK compatibility fallback (Priority: P1)

**Goal**: Shared XAML with new syntax produces a clear `UNO2099` on WinAppSDK targets and runs normally on every non-WinAppSDK target; never silently changes behavior or crashes.

**Independent Test**: Take a shared XAML file with inline expressions; build on WinAppSDK (expect `UNO2099` pointing at the attribute); build on Skia Desktop / WASM (expect success and runtime behavior from US1/US2).

### Tests for User Story 6 (write FIRST — MUST FAIL)

- [ ] T053 [P] [US6] Generator unit test `Given_WinAppSDKExclusion.cs` in `src/SourceGenerators/Uno.UI.SourceGenerators.Tests/XamlCodeGeneratorTests/CSharpExpressions/Given_WinAppSDKExclusion.cs` asserting that with WinAppSDK simulated the generator emits `UNO2099` for the first offending attribute and **no** lowering occurs (FR-014, FR-015, SC-004)
- [ ] T054 [P] [US6] Generator unit test extending `Given_WinAppSDKExclusion.cs` verifying the diagnostic message names the feature, the XAML file, and the attribute — and carries a file/line/column location
- [ ] T055 [P] [US6] Regression test extending `Given_OptInBehavior.cs` verifying that when the flag is off on a WinAppSDK build, `UNO2099` does NOT fire (feature is gated by opt-in)

### Implementation for User Story 6

- [ ] T056 [US6] Strengthen `PlatformHelper.IsWinAppSDK` and `UNO2099` emission (seeded in T017): ensure diagnostic is emitted before lowering, per-file, and the classifier short-circuits for the rest of the file after emission (makes T053, T054 pass) (FR-014, FR-015)
- [ ] T057 [US6] Verify via T055 that the feature stays fully inert on WinAppSDK when the opt-in is off (FR-013a, FR-016)
- [ ] T058 [US6] Add documentation stub in the `UNO2099` message pointing at the `<Page Condition="'$(IsWinAppSDK)' != 'true'"/>` workaround and at `contracts/diagnostics.md` (per acceptance scenario 3)

**Checkpoint**: US1 + US2 + US6 shippable — MVP is safe for multi-targeted projects.

---

## Phase 6: User Story 3 — Disambiguation directives (Priority: P2)

**Goal**: `{= expr}`, `{.Member}`, `{this.Member}`, `{prefix:Name}` resolve unambiguously; bare `{Foo}` with a markup-extension collision emits `UNO2001`.

**Independent Test**: Project has both a `FooExtension` and a `Foo` member; `{Foo}` warns (UNO2001); `{= Foo}` resolves as expression; `{local:Foo}` resolves as markup extension.

### Tests for User Story 3 (write FIRST — MUST FAIL)

- [ ] T059 [P] [US3] Classifier unit test extending `Given_ExpressionClassifier.cs` covering `{= Foo}`, `{.Member}`, `{this.Member}`, `{BindingContext.Foo}` prefix stripping (FR-008)
- [ ] T060 [P] [US3] Resolver unit test extending `Given_MemberResolver.cs` for UNO2001 (ambiguous markup-extension vs member), including the `{prefix:Name}` suppression path
- [ ] T061 [P] [US3] Diagnostic test in `Given_ExpressionDiagnostics_US3.cs` asserting UNO2001 (warning) and UNO2009 (nested markup extension in expression) fire correctly
- [ ] T062 [P] [US3] Generator snapshot test extending `Given_ExpressionLowering_SimplePath.cs` for `{this.WindowTitle}` (capture / page-source) and `{.FirstName}` (forced-DataType) per generated-binding-shapes §11
- [ ] T063 [P] [US3] Runtime test `Given_Disambiguation.cs` exercising US3 acceptance scenarios 1–4 on `Pages/DisambiguationPage.xaml`
- [ ] T064 [P] [US3] Sample page `Pages/DisambiguationPage.xaml(.cs)` with a `FooExtension` markup extension and `Foo` property on both page and DataType

### Implementation for User Story 3

- [ ] T065 [US3] Extend `CSharpExpressionClassifier.cs` to recognize directive prefixes and mark `Kind` accordingly per data-model §1 and contracts/expression-grammar.md §Disambiguation (makes T059 pass) (FR-008)
- [ ] T066 [US3] Extend `MemberResolver.cs` with `ForcedThis`, `ForcedDataType`, `MarkupExtension` branches and UNO2001 emission (makes T060, T061 pass) (FR-017)
- [ ] T067 [US3] Extend `ExpressionLowering.cs` so `{this.Foo}` emits the capture/page-source variant and `{.Foo}` emits the DataType-bound variant per generated-binding-shapes §11 (makes T062 pass)
- [ ] T068 [US3] Detect nested markup extension sub-expressions inside `{= …}` and emit UNO2009 (makes UNO2009 branch of T061 pass)
- [ ] T069 [US3] Register `DisambiguationPage.xaml` and verify T063 green on Skia Desktop and WASM

**Checkpoint**: Large-codebase ambiguity is safe — the feature can be rolled out alongside existing markup extensions without silent collisions.

---

## Phase 7: User Story 4 — Static type access and method invocation (Priority: P2)

**Goal**: Author calls static members (`Math.Max`, `DateTime.Now`, `string.Format`, local `Formatting.Currency(Price)`) directly from XAML.

**Independent Test**: `Text="{Math.Max(A, B)}"` with a DataType exposing `A` and `B`; observe value and refresh on change.

### Tests for User Story 4 (write FIRST — MUST FAIL)

- [ ] T070 [P] [US4] Resolver unit test extending `Given_MemberResolver.cs` with static-type resolution via global usings and `using static`; covers UNO2004 (member/type name collision) (FR-007)
- [ ] T071 [P] [US4] Analyzer unit test extending `Given_ExpressionAnalyzer.cs` verifying static references are emitted unchanged and do NOT contribute to the refresh set (resolution-algorithm §Analysis)
- [ ] T072 [P] [US4] Generator snapshot test `Given_ExpressionLowering_StaticAccess.cs` for `{Math.Max(A, B)}`, `{DateTime.Now.ToString('t')}`, and `{local:Formatting.Currency(Price)}` per generated-binding-shapes §8 and §5
- [ ] T073 [P] [US4] Diagnostic test extending `Given_ExpressionDiagnostics_US3.cs` (or new file) asserting UNO2004 fires correctly when a member and a same-named type coexist
- [ ] T074 [P] [US4] Runtime test `Given_StaticAccess.cs` exercising US4 acceptance scenarios 1–3 on `Pages/StaticAccessPage.xaml`
- [ ] T075 [P] [US4] Sample page `Pages/StaticAccessPage.xaml(.cs)` + local `Formatting` static class

### Implementation for User Story 4

- [ ] T076 [US4] Extend `MemberResolver.cs` with static-type lookup via `ResolutionScope.GlobalUsings` and compilation symbol search; emit UNO2004 on collision (makes T070, T073 pass) (FR-007)
- [ ] T077 [US4] Extend `ExpressionAnalyzer.cs` so identifiers resolving to a static type are emitted with `global::` qualification and excluded from the refresh set (makes T071 pass)
- [ ] T078 [US4] Extend `ExpressionLowering.cs` to accept `Formatting` static types and emit the one-shot direct-assignment form when the refresh set is empty (makes T072 pass; also covered by T039)
- [ ] T079 [US4] Register `StaticAccessPage.xaml` and verify T074 green on Skia Desktop and WASM

**Checkpoint**: Common static helpers (`Math`, `DateTime`, `string`, user-defined) work from XAML without resource wrappers.

---

## Phase 8: User Story 5 — XML-friendly operator aliases and CDATA (Priority: P3)

**Goal**: Author writes `{Count GT 0 AND IsEnabled}` or wraps expressions in `<![CDATA[ … ]]>` to avoid escaping `<`, `>`, `&`.

**Independent Test**: One page with the alias form and one with the equivalent CDATA form produce identical runtime behavior.

### Tests for User Story 5 (write FIRST — MUST FAIL)

- [X] T080 [P] [US5] Parser unit test `Given_OperatorAliases.cs` in `src/SourceGenerators/Uno.UI.SourceGenerators.Tests/XamlCodeGeneratorTests/CSharpExpressions/Given_OperatorAliases.cs` asserting `AND→&&`, `OR→||`, `LT→<`, `GT→>`, `LTE→<=`, `GTE→>=` with whitespace-bound and case-insensitive rules; `CountGT0` NOT replaced; `LTE`/`GTE` take precedence over `LT`/`GT`; replacement skips inside strings (FR-004; expression-grammar.md §Aliases) — Green against the early-pulled `OperatorAliases.Replace` (T085); covers basic alias replacement, case variations, identifier-substring suppression, LTE/GTE precedence, single-quoted and interpolated string literal preservation, interpolation-hole replacement, mixed forms, and null/empty inputs.
- [ ] T081 [P] [US5] Classifier test extending `Given_ExpressionClassifier.cs` asserting CDATA-wrapped attribute values on element-syntax properties are recognized and `IsCData=true` is set (FR-005)
- [ ] T082 [P] [US5] Generator snapshot test extending `Given_ExpressionLowering_Compound.cs` for the `{Count GT 0 AND IsEnabled}` form per generated-binding-shapes §13
- [ ] T083 [P] [US5] Runtime test `Given_AliasesAndCdata.cs` exercising US5 acceptance scenarios 1–3 on `Pages/AliasesAndCdataPage.xaml`
- [ ] T084 [P] [US5] Sample page `Pages/AliasesAndCdataPage.xaml(.cs)` with both alias form and CDATA form

### Implementation for User Story 5

- [X] T085 [US5] Implement `OperatorAliases.cs` in `src/SourceGenerators/Uno.UI.SourceGenerators/XamlGenerator/CSharpExpressions/OperatorAliases.cs` with whitespace-bounded, case-insensitive, string-literal-aware replacement; `LTE`/`GTE` before `LT`/`GT` (makes T080 pass) (FR-004) — Landed early (pulled forward from Phase 8) because `Given_ExpressionParser.When_OperatorAlias_IsReplacedBeforeParse` required a working Replace. Implementation: char-scan with single-quoted/interpolated-string awareness, identifier-word matching (so `CountGT0` is preserved), case-insensitive alias map. T080 (dedicated Given_OperatorAliases tests) still to be authored under Phase 8.
- [ ] T086 [US5] Wire `OperatorAliases.Replace` into the tokenizer/classifier pipeline between "strip directive prefix" and "transform quotes" per expression-grammar.md §Parsing-order (makes T082 pass)
- [ ] T087 [US5] Extend `CSharpExpressionClassifier.cs` and the XAML-file visitor to accept CDATA-wrapped expression values on element-syntax properties; set `IsCData=true` on the produced `XamlExpressionAttributeValue` (makes T081 pass) (FR-005)
- [ ] T088 [US5] Register `AliasesAndCdataPage.xaml` and verify T083 green on Skia Desktop and WASM

**Checkpoint**: Edge-case expressions that would otherwise require XML escaping work end-to-end.

---

## Phase 9: Polish & Cross-Cutting Concerns

**Purpose**: Quality gates, performance, documentation, and safety nets that span every user story.

- [ ] T089 [P] Coverage measurement configuration: ensure `Uno.UI.SourceGenerators.Tests` publishes coverage for `Uno.UI.SourceGenerators` and the CI pipeline gates merges at ≥ 90% line / ≥ 85% branch per spec FR-026 and SC-005 (update `build/ci/` or equivalent workflow — confirm with the DevOps owner file set in CI)
- [ ] T090 [P] Performance measurement: capture SamplesApp cold-build time and generator output size on the pre-feature commit and add a CI check that fails if the feature adds more than 5% / 10% respectively per SC-006
- [ ] T091 [P] Expand `Given_OptInBehavior.cs` golden corpus to cover every existing markup-extension case in `src/Uno.UI.Tests` so flag-off byte-equality is broadly validated (FR-013a, FR-016)
- [ ] T092 [P] Document the feature in `doc/articles/features/` (new file `xaml-csharp-expressions.md`) with the quickstart content, diagnostics list, and WinAppSDK caveat — link from the Uno features index
- [ ] T093 [P] Run `dotnet xstyler -d src/Uno.UI.RuntimeTests/Tests_CSharpExpressions -r` to format all sample XAML per repo convention
- [ ] T094 Validate quickstart.md end-to-end on a fresh single-project reproducer: enable the flag, paste the minimal page, run on Skia Desktop and WASM, confirm every displayed form updates on VM mutation
- [ ] T095 Nullability sweep: verify generated `__xcs_Expr_*` and `__xcs_EventHandler_*` methods emit under `#nullable enable` with no spurious warnings (spec Edge Cases §Nullable)
- [ ] T096 Globalization test: extend runtime tests with a culture-switch case asserting that `{$'{Balance:C2}'}` re-renders with the new culture's currency format on `CultureInfo.CurrentCulture` change (FR-012)

---

## Dependencies & Execution Order

### Phase dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately.
- **Foundational (Phase 2)**: Depends on Setup; BLOCKS every user story.
- **US1 (Phase 3, P1)**: Depends on Foundational. Must land before US2/US3/US4/US5 (their lowering reuses US1's simple/compound emitters and analyzer).
- **US2 (Phase 4, P1)**: Depends on Foundational; independent of US1 in code but shares the classifier — US1 first reduces merge friction.
- **US6 (Phase 5, P1)**: Seeded in Foundational (T016/T017); completed in Phase 5. Independent of US1/US2/US3/US4/US5.
- **US3 (Phase 6, P2)**: Depends on US1 (resolver + classifier + lowering). Independent of US2.
- **US4 (Phase 7, P2)**: Depends on US1 + US3 (static resolver extends the resolver foundation).
- **US5 (Phase 8, P3)**: Depends on US1 (aliases and CDATA feed the same lowering). Independent of US2/US3/US4.
- **Polish (Phase 9)**: Depends on every desired user story landing.

### Within each user story

- Tests **MUST** be written and **MUST FAIL** before the corresponding implementation task is merged (spec FR-023). Each PR keeps its test and implementation together.
- Data-model types and classifier hooks before resolver/analyzer.
- Analyzer before lowering.
- Lowering before runtime wiring (projitems + runtime test).
- Runtime test before checking the phase off.

### Parallel opportunities

- All Phase 1 Setup tasks marked `[P]` are independent.
- All Phase 2 descriptor/data-model scaffolding tasks marked `[P]` run in parallel (T006–T010, T012, T013, T016).
- Within each user story, all tests marked `[P]` run in parallel before any implementation begins.
- Different user stories can be worked in parallel by different developers once Foundational is green, subject to the dependency chain above.
- Phase 9 Polish tasks marked `[P]` are independent.

---

## Parallel Example: User Story 1

```bash
# All US1 test tasks can run in parallel (they touch different files):
Task: "Parser unit test Given_ExpressionTokenizer.cs"
Task: "Parser unit test Given_ExpressionParser.cs"
Task: "Classifier unit test Given_ExpressionClassifier.cs"
Task: "Resolver unit test Given_MemberResolver.cs"
Task: "Analyzer unit test Given_ExpressionAnalyzer.cs"
Task: "Generator snapshot test Given_ExpressionLowering_SimplePath.cs"
Task: "Generator snapshot test Given_ExpressionLowering_Compound.cs"
Task: "Diagnostic test Given_ExpressionDiagnostics_US1.cs"
Task: "Runtime test Given_SimpleBinding.cs"
Task: "Runtime test Given_CompoundExpression.cs"
Task: "Sample pages SimpleBindingPage.xaml and CompoundExpressionPage.xaml"

# After tests are red, implementation tasks can be split across developers:
Developer A: Tokenizer + QuoteTransform + Parser (T030, T031, T032)
Developer B: Classifier + XDataTypeResolver + MemberResolver (T029, T033, T034)
Developer C: ExpressionAnalyzer + ExpressionLowering (T035, T036, T037)
```

---

## Implementation Strategy

### MVP First (US1 + US2 + US6)

The P1 stories together form the minimum shippable increment: "no code-behind for easy cases" with multi-target safety.

1. Complete Phase 1 Setup.
2. Complete Phase 2 Foundational.
3. Complete Phase 3 US1 → test independently.
4. Complete Phase 4 US2 → test independently.
5. Complete Phase 5 US6 → test independently (WinAppSDK build error + non-WinAppSDK success).
6. **STOP and VALIDATE**: run all runtime tests on Skia Desktop and WASM; run a WinAppSDK build on the same shared XAML.
7. Demo / internal release behind the opt-in flag as preview.

### Incremental Delivery

1. Setup + Foundational → foundation ready.
2. US1 → MVP (simple + compound bindings).
3. US2 → MVP extended (event lambdas).
4. US6 → MVP safe for multi-targeted projects.
5. US3 → disambiguation directives.
6. US4 → static access.
7. US5 → operator aliases and CDATA.
8. Polish phase — coverage gate, perf gate, docs, culture test.

### Parallel Team Strategy

With three developers, once Foundational is green:

- Developer A: US1 (parser + resolver + analyzer + simple/compound lowering).
- Developer B: US2 (event lambdas) + US6 (WinAppSDK exclusion).
- Developer C: US3 (disambiguation) after US1 stabilizes; pairs on US4 and US5 later.

---

## Notes

- `[P]` = different files, no dependencies.
- `[Story]` label maps each task to its user story for traceability.
- Each user story is independently testable; MVP stops after US1 + US2 + US6.
- Every test MUST fail before the matching implementation task is merged (FR-023).
- Commit after each task or logical group; use conventional commit types (`feat`, `test`, `docs`, `chore`).
- Runtime tests run through the `/runtime-tests` skill; pass the test class or method name as the skill argument.
- When porting behavior from MAUI (research.md), read the MAUI source at `D:\Work\maui\src\Controls\src\SourceGen\` first and document any deviation in `research.md` §9.2.
