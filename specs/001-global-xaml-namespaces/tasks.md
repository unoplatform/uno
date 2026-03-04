# Tasks: Global/Implicit XAML Namespaces

**Input**: Design documents from `/specs/001-global-xaml-namespaces/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

**Tests**: Included per Constitution Principle III (Test-First Quality Gates) - runtime tests are mandatory for all changes.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: MSBuild property plumbing and constants that all stories depend on

- [X] T001 Add `GlobalNamespaceUri` constant to `src/SourceGenerators/Uno.UI.SourceGenerators/XamlGenerator/XamlConstants.cs` — value: `http://schemas.microsoft.com/winfx/2006/xaml/presentation/global`
- [X] T002 [P] Create `src/Uno.Sdk/targets/Uno.ImplicitXamlNamespaces.props` with `UnoEnableImplicitXamlNamespaces` (default true) and `UnoGlobalXamlNamespaceUri` properties
- [X] T003 [P] Add `CompilerVisibleProperty` entries for `UnoEnableImplicitXamlNamespaces` and `UnoGlobalXamlNamespaceUri` in `src/SourceGenerators/Uno.UI.SourceGenerators/Content/Uno.UI.SourceGenerators.props`
- [X] T004 Import `Uno.ImplicitXamlNamespaces.props` from `src/Uno.Sdk/Sdk/Sdk.props`
- [X] T005 Read `UnoEnableImplicitXamlNamespaces` and `UnoGlobalXamlNamespaceUri` MSBuild properties in `src/SourceGenerators/Uno.UI.SourceGenerators/XamlGenerator/XamlCodeGeneration.cs` constructor and pass them through to `XamlFileParser` and `XamlFileGenerator`

**Checkpoint**: MSBuild property flows from project → Uno.Sdk → source generator. No behavioral change yet.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core source generator changes that enable XAML parsing without explicit `xmlns` — MUST be complete before any user story

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [X] T006 Create `src/SourceGenerators/Uno.UI.SourceGenerators/XamlGenerator/GlobalNamespaceResolver.cs` — static class with `GetGlobalClrNamespaces(Compilation, string globalUri)` that scans `XmlnsDefinition` attributes from all referenced assemblies and the current assembly targeting the global URI; also `GetImplicitPrefixes(Compilation)` that scans `XmlnsPrefix` attributes
- [X] T007 Modify `XamlFileParser` constructor in `src/SourceGenerators/Uno.UI.SourceGenerators/XamlGenerator/XamlFileParser.cs` to accept `bool enableImplicitNamespaces` and `(string prefix, string uri)[] implicitPrefixes` parameters
- [X] T008 Modify `RewriteForXBind` method in `src/SourceGenerators/Uno.UI.SourceGenerators/XamlGenerator/XamlFileParser.cs` — when `enableImplicitNamespaces` is true, create `XmlNamespaceManager` pre-populated with default presentation namespace (empty prefix), `x:` XAML namespace, and any `implicitPrefixes`; use `ConformanceLevel.Fragment` and `XmlParserContext` when creating `XmlReader`
- [X] T009 Modify `InitCaches()` in `src/SourceGenerators/Uno.UI.SourceGenerators/XamlGenerator/XamlFileGenerator.Reflection.cs` — when implicit namespaces are enabled and no default xmlns is declared in the file, set `_clrNamespaces` to `PresentationNamespaces` (the implicit default) instead of an empty array
- [X] T010 Add `_globalClrNamespaces` field to `XamlFileGenerator.Reflection.cs` populated from `GlobalNamespaceResolver.GetGlobalClrNamespaces()`; modify `SourceFindTypeByXamlType()` to search `_globalClrNamespaces` after `_clrNamespaces` when the type is not found in the presentation namespace

**Checkpoint**: Source generator can parse XAML without `xmlns` declarations and resolve standard WinUI types. Foundation ready for user story testing.

---

## Phase 3: User Story 1 - Write XAML Without Boilerplate (Priority: P1) 🎯 MVP

**Goal**: XAML files compile and render without explicit `xmlns` and `xmlns:x` declarations on all Uno targets

**Independent Test**: Create a XAML page with only `x:Class`, no `xmlns` declarations, build for Skia, verify it compiles and renders standard WinUI controls

### Tests for User Story 1

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation completes**

- [X] T011 [US1] Create runtime test class `src/Uno.UI.RuntimeTests/Tests/Windows_UI_Xaml/Given_ImplicitXamlNamespaces.cs` with test: `When_Page_Has_No_Xmlns_Declarations` — create a XAML-defined UserControl without explicit `xmlns`, add to visual tree, verify it loads and renders a Button and TextBlock correctly
- [X] T012 [US1] Add runtime test: `When_XName_Works_Without_XmlnsX` — verify `x:Name` works on elements in XAML without `xmlns:x` declaration
- [X] T013 [US1] Add runtime test: `When_XBind_Works_Without_XmlnsX` — verify `x:Bind` expressions resolve correctly in XAML without `xmlns:x` declaration
- [X] T014 [US1] Add runtime test: `When_Explicit_Xmlns_Still_Works` — verify XAML with explicit `xmlns` declarations continues to compile and render identically (backward compatibility)
- [X] T015 [US1] Add runtime test: `When_Feature_Disabled_Xmlns_Required` — verify that when `UnoEnableImplicitXamlNamespaces=false`, XAML without `xmlns` fails to parse (opt-out works)

### Implementation for User Story 1

- [X] T016 [US1] Create test XAML files for runtime tests — UserControls without `xmlns` declarations that use Button, TextBlock, Grid, StackPanel, x:Name, x:Bind, x:Key, and Style resources; register in `src/SamplesApp/UITests.Shared/UITests.Shared.projitems`
- [X] T017 [US1] Wire up the full pipeline: ensure `XamlCodeGeneration.cs` passes the feature flag and implicit prefixes from `GlobalNamespaceResolver` through to `XamlFileParser` and `XamlFileGenerator`, so the foundational changes (Phase 2) are fully connected
- [X] T018 [US1] Verify existing SamplesApp XAML files (which have explicit `xmlns`) still compile without regressions — build `Uno.UI-Skia-only.slnf` and run `dotnet test src/Uno.UI/Uno.UI.Tests.csproj`

**Checkpoint**: XAML files without `xmlns` compile and render on Skia. Existing XAML files unchanged. MVP complete.

---

## Phase 4: User Story 2 - Register App-Specific CLR Namespaces (Priority: P2)

**Goal**: Developers register custom CLR namespaces via `[assembly: XmlnsDefinition]` targeting the global URI; types resolve unprefixed in XAML

**Independent Test**: Create a class in a custom namespace, register it to the global URI, reference it unprefixed in XAML, verify it compiles

### Tests for User Story 2

- [X] T019 [US2] Add runtime test: `When_Custom_Namespace_Registered_To_Global_Uri` — register a test CLR namespace to the global URI via `XmlnsDefinition`, use a type from it unprefixed in XAML, verify it resolves
- [X] T020 [US2] Add runtime test: `When_Multiple_Global_Namespaces_Resolve` — register two CLR namespaces with differently-named types, verify both resolve unprefixed
- [X] T021 [US2] Add runtime test: `When_WinUI_Type_Takes_Precedence_Over_Global` — register a CLR namespace containing a type named `Button`, verify the WinUI `Button` is resolved (not the custom one)

### Implementation for User Story 2

- [X] T022 [US2] Ensure `GlobalNamespaceResolver.GetGlobalClrNamespaces()` correctly discovers `XmlnsDefinition` attributes from the **current assembly** (not just referenced assemblies) in `src/SourceGenerators/Uno.UI.SourceGenerators/XamlGenerator/GlobalNamespaceResolver.cs`
- [X] T023 [US2] Create test helper types and `XmlnsDefinition` attributes in the runtime test project for the test XAML files — custom UserControl types in custom namespaces registered to the global URI
- [X] T024 [US2] Add the global namespace URI to `_knownNamespaces` dictionary in `src/SourceGenerators/Uno.UI.SourceGenerators/XamlGenerator/XamlFileGenerator.cs` with the dynamically discovered CLR namespaces from `GlobalNamespaceResolver`, ensuring presentation namespaces are searched first

**Checkpoint**: Custom CLR namespaces registered to global URI resolve in XAML. WinUI types take precedence.

---

## Phase 5: User Story 3 - Register Third-Party Library Namespaces (Priority: P2)

**Goal**: Third-party library namespaces registered to the global URI via `XmlnsDefinition` (with `AssemblyName`) resolve across assembly boundaries

**Independent Test**: Register a namespace from a referenced assembly to the global URI, use its type unprefixed in XAML

### Tests for User Story 3

- [ ] T025 [US3] Add runtime test: `When_Cross_Assembly_XmlnsDefinition_Resolves` — verify types from a referenced assembly registered to the global URI resolve unprefixed (use an existing Uno assembly type as the cross-assembly reference)
- [ ] T026 [US3] Add runtime test: `When_Library_Ships_Own_XmlnsDefinition` — verify that when a referenced assembly declares its own `XmlnsDefinition` targeting the global URI, its types are automatically discoverable
- [ ] T026b [US3] Add runtime test: `When_XmlnsPrefix_Registered_Prefix_Is_Implicitly_Available` — register a namespace with `[assembly: XmlnsPrefix]` associating a prefix (e.g., `toolkit`), use that prefix in XAML without an explicit `xmlns:toolkit` declaration, verify the prefixed type resolves correctly (covers FR-010)

### Implementation for User Story 3

- [ ] T027 [US3] Verify `GlobalNamespaceResolver.GetGlobalClrNamespaces()` correctly scans `XmlnsDefinition` attributes from **referenced assemblies** (via `Compilation.References` → `IAssemblySymbol.GetAttributes()`) and handles the `AssemblyName` constructor argument in `src/SourceGenerators/Uno.UI.SourceGenerators/XamlGenerator/GlobalNamespaceResolver.cs`
- [ ] T028 [US3] Add test XAML files that reference types from other assemblies without prefixes; register in `src/SamplesApp/UITests.Shared/UITests.Shared.projitems`

**Checkpoint**: Third-party library types resolve unprefixed when their namespace is registered to the global URI.

---

## Phase 6: User Story 4 - Disambiguation with Prefixed Namespaces (Priority: P3)

**Goal**: When type name collisions occur across global namespaces, a clear error is emitted; explicit `xmlns` prefixes resolve ambiguity

**Independent Test**: Register two CLR namespaces with same-named types, verify ambiguity error; add explicit prefix, verify resolution

### Tests for User Story 4

- [ ] T029 [US4] Add runtime test: `When_Ambiguous_Type_Produces_Clear_Error` — register two CLR namespaces both containing a type named `TestCard`, use `<TestCard />` unprefixed, verify a diagnostic error is produced mentioning both namespaces
- [ ] T030 [US4] Add runtime test: `When_Explicit_Prefix_Resolves_Ambiguity` — same setup as above but add explicit `xmlns:custom="using:..."` and use `<custom:TestCard />`, verify it compiles

### Implementation for User Story 4

- [ ] T031 [US4] Add ambiguity detection in `SourceFindTypeByXamlType()` in `src/SourceGenerators/Uno.UI.SourceGenerators/XamlGenerator/XamlFileGenerator.Reflection.cs` — when a type name matches in multiple global CLR namespaces, emit diagnostic `UNO0501` with the conflicting namespace names and disambiguation guidance
- [ ] T032 [US4] Verify that explicit per-file `xmlns` declarations override implicit global registrations (FR-009) — ensure `XamlFileParser.cs` respects file-level declarations when they overlap with implicit ones

**Checkpoint**: Ambiguous types produce clear errors. Explicit prefixes resolve ambiguity.

---

## Phase 7: User Story 5 - Works on WinUI/WinAppSDK via Uno.Sdk (Priority: P1)

**Goal**: Same XAML files (without `xmlns`) compile on WinAppSDK targets via MSBuild pre-processing

**Independent Test**: Build a XAML file without `xmlns` for both Skia and `net10.0-windows10.0.19041.0` targets; both succeed

### Tests for User Story 5

- [ ] T033 [US5] Add unit test in `src/Uno.UI/Uno.UI.Tests.csproj`: `When_WinAppSdk_XamlPreprocessor_Injects_Xmlns` — test the MSBuild task logic that injects `xmlns` declarations into XAML strings, verifying correct output for various input patterns (no xmlns, partial xmlns, existing xmlns preserved)

### Implementation for User Story 5

- [ ] T034 [US5] Create `src/Uno.Sdk/targets/Uno.ImplicitXamlNamespaces.targets` — MSBuild targets that, for WinAppSDK builds only (`Condition="'$(IsWinAppSdk)'=='true'"`), run before `XamlPreCompile` to: (a) copy XAML files to temp directory, (b) inject implicit `xmlns` and `xmlns:x` declarations into root elements, (c) also inject global namespace `xmlns` and any `XmlnsPrefix`-registered prefixes, (d) redirect WinUI compiler to use temp copies
- [ ] T035 [US5] Implement the XAML pre-processing inline MSBuild task or C# task in `src/Uno.Sdk/targets/Uno.ImplicitXamlNamespaces.targets` — string manipulation to find root element opening tag and insert missing `xmlns` attributes; preserve any explicit declarations already present
- [ ] T036 [US5] Import `Uno.ImplicitXamlNamespaces.targets` from `src/Uno.Sdk/Sdk/Sdk.targets`
- [ ] T037 [US5] Add cleanup target that removes temp XAML copies after WinUI compilation completes

**Checkpoint**: Same XAML compiles on both Uno targets and WinAppSDK targets.

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Edge cases, Hot Reload, and final validation

- [ ] T038 Add runtime test: `When_HotReload_Parses_Xaml_Without_Xmlns` — verify that the Hot Reload XAML parsing path (which re-parses XAML at runtime) correctly handles XAML without explicit `xmlns` declarations by invoking the parser with implicit namespace context; additionally, document manual validation steps for live Hot Reload testing (modify a XAML file without `xmlns` during a running Skia app session and confirm the change is applied)
- [ ] T039 Verify `mc:Ignorable` and `d:DesignInstance` patterns work — add runtime test confirming design-time attributes function correctly when `mc:` and `d:` prefixes are declared alongside implicit namespaces
- [ ] T040 Verify `x:Bind` type resolution uses global namespaces — ensure binding expressions like `x:DataType="MyViewModel"` resolve types from globally registered namespaces
- [ ] T041 [P] Run full SamplesApp build and runtime test suite to catch regressions — `dotnet build src/SamplesApp/SamplesApp.Skia.Generic/SamplesApp.Skia.Generic.csproj -c Release -f net10.0` then runtime tests
- [ ] T042 [P] Run unit test suite — `dotnet test src/Uno.UI/Uno.UI.Tests.csproj`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately
- **Foundational (Phase 2)**: Depends on Phase 1 completion — BLOCKS all user stories
- **User Story 1 (Phase 3)**: Depends on Phase 2 — core MVP
- **User Story 2 (Phase 4)**: Depends on Phase 2; enhanced by Phase 3 but independently testable
- **User Story 3 (Phase 5)**: Depends on Phase 2 and T022 from Phase 4 (GlobalNamespaceResolver for current assembly must work before cross-assembly)
- **User Story 4 (Phase 6)**: Depends on Phase 4 (needs global namespace resolution working to test ambiguity)
- **User Story 5 (Phase 7)**: Depends on Phase 1 only (MSBuild-only, independent of source generator)
- **Polish (Phase 8)**: Depends on Phases 3-7 completion

### User Story Dependencies

- **US1 (P1)**: After Phase 2 — no dependencies on other stories
- **US5 (P1)**: After Phase 1 — can run in parallel with Phase 2 and US1 (MSBuild-only work)
- **US2 (P2)**: After Phase 2 — independent of US1, but builds on same GlobalNamespaceResolver
- **US3 (P2)**: After US2 T022 — needs current-assembly resolution working first
- **US4 (P3)**: After US2 — needs global namespace resolution to test ambiguity

### Within Each User Story

- Tests MUST be written and FAIL before implementation
- Foundation code before story-specific code
- Core resolution before edge cases

### Parallel Opportunities

- T002 and T003 (Setup) can run in parallel (different files)
- US1 and US5 can proceed in parallel after their respective prerequisites
- US2 and US3 tests (T019-T021, T025-T026) can be written in parallel
- T041 and T042 (Polish) can run in parallel

---

## Parallel Example: Setup Phase

```bash
# These can run in parallel (different files):
Task T002: "Create Uno.ImplicitXamlNamespaces.props in src/Uno.Sdk/targets/"
Task T003: "Add CompilerVisibleProperty in Uno.UI.SourceGenerators.props"
```

## Parallel Example: User Story 1 + User Story 5

```bash
# US1 (source generator) and US5 (MSBuild) work on completely different subsystems:
# After Phase 1 setup:
Agent A: US5 tasks (T033-T037) — MSBuild pre-processing for WinAppSDK
# After Phase 2 foundational:
Agent B: US1 tasks (T011-T018) — Source generator implicit namespace support
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (T001-T005)
2. Complete Phase 2: Foundational (T006-T010)
3. Complete Phase 3: User Story 1 (T011-T018)
4. **STOP and VALIDATE**: Build SamplesApp, run runtime tests, verify XAML without `xmlns` works on Skia
5. This alone delivers the primary developer experience improvement

### Incremental Delivery

1. Setup + Foundational → MSBuild plumbing and parser ready
2. User Story 1 → XAML without `xmlns` works on Uno targets → **MVP!**
3. User Story 5 → Same XAML also works on WinAppSDK → **Cross-platform parity**
4. User Story 2 → Custom CLR namespaces registered globally → **Full developer workflow**
5. User Story 3 → Third-party libraries registered globally → **Ecosystem support**
6. User Story 4 → Ambiguity errors and disambiguation → **Edge case handling**
7. Polish → Hot Reload, regression testing → **Production ready**

### Parallel Agent Strategy

With multiple agents:

1. All agents complete Setup (Phase 1) together
2. Once Setup is done:
   - Agent A: Foundational (Phase 2) + US1 (Phase 3) — source generator path
   - Agent B: US5 (Phase 7) — MSBuild/WinAppSDK path (independent)
3. After Phase 2:
   - Agent A: US2 (Phase 4) + US3 (Phase 5) + US4 (Phase 6)
   - Agent B: Polish (Phase 8)

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Constitution Principle III requires runtime tests — all stories include test tasks
- The source generator runs at compile time; changes affect ALL platforms simultaneously
- WinAppSDK is the only target needing a separate implementation path (MSBuild pre-processing)
- Commit after each task or logical group using conventional commits
- Stop at any checkpoint to validate story independently
