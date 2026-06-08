# Tasks: Auto-Generate Code-Behind for XAML Files

**Input**: Design documents from `/specs/001-auto-codebehind/`
**Prerequisites**: plan.md (required), spec.md (required), research.md, data-model.md, contracts/msbuild-contract.md

**Tests**: Included per Constitution Principle III (Test-First Quality Gates). Tests are written BEFORE implementation within each user story phase.

**Organization**: Tasks grouped by user story. US1 and US4 are both P1; US2 and US3 are both P2. Test-first ordering within each phase.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3, US4)
- Include exact file paths in descriptions

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: MSBuild property/metadata plumbing so the source generators can read project configuration

- [X] T001 Add `CompilerVisibleProperty` for `UnoGenerateCodeBehind` and `CompilerVisibleItemMetadata` for `UnoGenerateCodeBehind` on all four item types (`AdditionalFiles` with `SourceItemGroup` of `Page`, `UnoPage`, `ApplicationDefinition`, `UnoApplicationDefinition`) in `src/SourceGenerators/Uno.UI.SourceGenerators/Content/Uno.UI.SourceGenerators.props`
- [X] T002 Create MSBuild targets file for WinUI support at `src/Uno.Sdk/targets/Uno.UI.SourceGenerators.WinAppSdk.props` that re-adds the analyzer DLL (path: `$(MSBuildThisFileDirectory)../analyzers/dotnet/cs/Uno.UI.SourceGenerators.dll` or equivalent resolved from Uno.Sdk package layout) on WinUI builds when `UnoGenerateCodeBehind` is not `false`, and sets `UnoCodeBehindGeneratorOnly=true`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Shared utilities and data structures used by both the integrated path (Uno) and standalone path (WinUI)

**CRITICAL**: Both US1 and US4 depend on these shared components

- [X] T003 [P] Create `XamlClassInfo` record/struct (FullClassName, Namespace, ClassName, RootElementName, RootElementNamespace, BaseTypeFullName) in `src/SourceGenerators/Uno.UI.SourceGenerators/XamlGenerator/XamlClassInfo.cs`
- [X] T004 Create XAML `x:Class` and root element parser using `XDocument` in `src/SourceGenerators/Uno.UI.SourceGenerators/XamlGenerator/XamlCodeBehindParser.cs`. This parser is primarily used by the standalone WinUI path (US4). The integrated Uno path (US1) reuses existing `XamlCodeGeneration` type resolution infrastructure. Include: (a) `x:Class` extraction returning `XamlClassInfo?` (null when no `x:Class`), (b) root element type map (Page, UserControl, Window, Application, ResourceDictionary, ContentDialog, SwapChainPanel to fully-qualified WinUI types), (c) `UnoGenerateCodeBehind` config reading helper that resolves per-file metadata vs project-level property precedence (FR-009)
- [X] T005 [P] Create code-behind source text emitter method that takes `XamlClassInfo` and returns formatted C# source string matching the template in data-model.md in `src/SourceGenerators/Uno.UI.SourceGenerators/XamlGenerator/XamlCodeBehindEmitter.cs`

**Checkpoint**: Foundation ready - shared parsing and emitting infrastructure complete

---

## Phase 3: User Story 1 - Zero-Boilerplate XAML Page Creation (Priority: P1) MVP

**Goal**: XAML files with `x:Class` and no developer-authored code-behind get auto-generated code-behind at build time on Uno Platform targets

**Independent Test**: Create a XAML file with `x:Class` and no `.xaml.cs`, build the project for Skia, verify generated code-behind exists with correct constructor and `InitializeComponent()` call

### Tests for User Story 1 (Write FIRST - must FAIL before implementation)

- [X] T006 [P] [US1] Create test XAML page(s) without code-behind for runtime test validation (e.g., `AutoCodeBehindTestPage.xaml` with `x:Class` and no `.xaml.cs`) in `src/Uno.UI.RuntimeTests/Tests/Windows_UI_Xaml/`
- [X] T007 [US1] Create runtime test class `Given_AutoCodeBehind` verifying auto-generated pages can be instantiated, added to visual tree, and render correctly in `src/Uno.UI.RuntimeTests/Tests/Windows_UI_Xaml/Given_AutoCodeBehind.cs`

> **RED**: Tests should fail at this point (no code-behind generation exists yet). Verify failure before proceeding.

### Implementation for User Story 1

- [X] T008 [US1] Modify `XamlCodeGeneration.Generate()` to detect XAML files where `FindClassSymbol()` returns null, check `UnoGenerateCodeBehind` property and per-file `UnoGenerateCodeBehind` metadata, and emit code-behind source using shared emitter. Note: for root element type resolution, reuse existing `GetType(topLevelControl.Type)` from the `XamlCodeGeneration` pipeline (do NOT use the `XDocument`-based parser from T004 - that is for the WinUI standalone path only) in `src/SourceGenerators/Uno.UI.SourceGenerators/XamlGenerator/XamlCodeGeneration.cs`
- [X] T009 [US1] Add internal flag/set in `XamlCodeGeneration` to track which XAML files have auto-generated code-behind, so `XamlFileGenerator` can query it in `src/SourceGenerators/Uno.UI.SourceGenerators/XamlGenerator/XamlCodeGeneration.cs`
- [X] T010 [US1] Modify `XamlFileGenerator.TryGenerateWarningForInconsistentBaseType` to suppress `#warning` when code-behind is auto-generated in `src/SourceGenerators/Uno.UI.SourceGenerators/XamlGenerator/XamlFileGenerator.cs`
- [X] T011 [US1] Modify `XamlFileGenerator` x:Bind resolution paths (`GetXBindPropertyPathType`, `GetTargetType`, `XBindExpressionParser.Rewrite`) to use XAML-derived base type when code-behind is auto-generated. Implementation: use the base type `INamedTypeSymbol` resolved by `XamlCodeGeneration` pipeline and pass it as a substitute for `_xClassName.Symbol` in affected code paths in `src/SourceGenerators/Uno.UI.SourceGenerators/XamlGenerator/XamlFileGenerator.cs`
- [X] T012 [US1] Handle edge case: XAML files with no `x:Class` attribute are skipped (FR-006) - verify existing `XamlCodeGeneration` pipeline already filters these, add guard if not in `src/SourceGenerators/Uno.UI.SourceGenerators/XamlGenerator/XamlCodeGeneration.cs`
- [X] T013 [US1] Handle edge case: malformed `x:Class` (no namespace separator) emits diagnostic `UXAML0004` and skips generation. First verify `UXAML0004` is not already in use in `src/SourceGenerators/` (search existing diagnostics). In `src/SourceGenerators/Uno.UI.SourceGenerators/XamlGenerator/XamlCodeGeneration.cs`

> **GREEN**: Runtime tests from T007 should now pass. Verify before proceeding.

**Checkpoint**: User Story 1 functional - XAML pages without code-behind compile on Uno Platform targets

---

## Phase 4: User Story 4 - Cross-Platform Consistency (Priority: P1)

**Goal**: Auto code-behind generation works on WinUI targets (`net10.0-windows10.0.*`) via a standalone `IIncrementalGenerator`, producing the same result as the Uno integrated path

**Independent Test**: Build same project for WinUI target and verify code-behind is generated for XAML files without developer-authored code-behind

### Tests for User Story 4 (Write FIRST - must FAIL before implementation)

- [X] T014 [P] [US4] Create test class `XamlCodeBehindGeneratorTests` with test infrastructure (in-memory compilation, XAML as AdditionalText) in `src/SourceGenerators/Uno.UI.SourceGenerators.Tests/XamlCodeBehindGeneratorTests/XamlCodeBehindGeneratorTests.cs`
- [X] T015 [P] [US4] Test: XAML with `x:Class` and no code-behind generates correct partial class with constructor in `src/SourceGenerators/Uno.UI.SourceGenerators.Tests/XamlCodeBehindGeneratorTests/XamlCodeBehindGeneratorTests.cs`
- [X] T016 [P] [US4] Test: XAML with `x:Class` and existing code-behind class in compilation produces no generated output in `src/SourceGenerators/Uno.UI.SourceGenerators.Tests/XamlCodeBehindGeneratorTests/XamlCodeBehindGeneratorTests.cs`
- [X] T017 [P] [US4] Test: XAML with `x:Class` where class exists in non-conventional file (not `.xaml.cs`) produces no generated output in `src/SourceGenerators/Uno.UI.SourceGenerators.Tests/XamlCodeBehindGeneratorTests/XamlCodeBehindGeneratorTests.cs`
- [X] T018 [P] [US4] Test: XAML with no `x:Class` attribute produces no generated output in `src/SourceGenerators/Uno.UI.SourceGenerators.Tests/XamlCodeBehindGeneratorTests/XamlCodeBehindGeneratorTests.cs`
- [X] T019 [P] [US4] Test: Malformed `x:Class` (no namespace) emits `UXAML0004` diagnostic in `src/SourceGenerators/Uno.UI.SourceGenerators.Tests/XamlCodeBehindGeneratorTests/XamlCodeBehindGeneratorTests.cs`
- [X] T020 [P] [US4] Test: Correct base type resolution for Page, UserControl, Window, ContentDialog root elements in `src/SourceGenerators/Uno.UI.SourceGenerators.Tests/XamlCodeBehindGeneratorTests/XamlCodeBehindGeneratorTests.cs`

> **RED**: Tests should fail at this point (standalone generator does not exist yet). Verify failure before proceeding.

### Implementation for User Story 4

- [X] T021 [US4] Create `XamlCodeBehindGenerator` class implementing `IIncrementalGenerator` in `src/SourceGenerators/Uno.UI.SourceGenerators/XamlGenerator/XamlCodeBehindGenerator.cs`
- [X] T022 [US4] Implement `Initialize` method: register provider pipeline that filters `AdditionalFiles` by `SourceItemGroup` metadata (`Page`, `UnoPage`, `ApplicationDefinition`, `UnoApplicationDefinition`), reads `build_property.UnoCodeBehindGeneratorOnly` to gate activation (only runs when property is `true`, i.e., WinUI builds) in `src/SourceGenerators/Uno.UI.SourceGenerators/XamlGenerator/XamlCodeBehindGenerator.cs`
- [X] T023 [US4] Implement incremental pipeline: parse XAML with shared `XamlCodeBehindParser` (from T004), check `Compilation.GetTypeByMetadataName()` for existing class, read `UnoGenerateCodeBehind` property and per-file `UnoGenerateCodeBehind` metadata, emit source via shared emitter in `src/SourceGenerators/Uno.UI.SourceGenerators/XamlGenerator/XamlCodeBehindGenerator.cs`
- [X] T024 [US4] Verify `Uno.UI.SourceGenerators.WinAppSdk.props` (T002) correctly re-adds analyzer DLL and sets gating property on WinUI builds in `src/Uno.Sdk/targets/Uno.UI.SourceGenerators.WinAppSdk.props`

> **GREEN**: Unit tests from T014-T020 should now pass. Verify before proceeding.

**Checkpoint**: Cross-platform complete - code-behind generation works on both Uno and WinUI targets

---

## Phase 5: User Story 2 + User Story 3 - Configuration Controls (Priority: P2)

**Goal**: Project-level `UnoGenerateCodeBehind` property and per-file `UnoGenerateCodeBehind` metadata correctly enable/disable generation with proper precedence (per-file overrides project-level)

**Independent Test (US2)**: Set `UnoGenerateCodeBehind=false` in `.csproj`, create XAML file without code-behind, build and verify no code-behind is generated

**Independent Test (US3)**: With `UnoGenerateCodeBehind=true`, add `<UnoGenerateCodeBehind>false</UnoGenerateCodeBehind>` to a specific XAML file, verify it is skipped. Conversely, with global `false`, set per-file `true` and verify generation occurs.

### Tests for User Story 2 + 3 (Write FIRST - must FAIL before hardening)

- [X] T025 [P] [US2] Test: `UnoGenerateCodeBehind=false` suppresses generation in `src/SourceGenerators/Uno.UI.SourceGenerators.Tests/XamlCodeBehindGeneratorTests/XamlCodeBehindGeneratorTests.cs`
- [X] T026 [P] [US3] Test: Per-file `UnoGenerateCodeBehind=false` overrides global `true` in `src/SourceGenerators/Uno.UI.SourceGenerators.Tests/XamlCodeBehindGeneratorTests/XamlCodeBehindGeneratorTests.cs`
- [X] T027 [P] [US3] Test: Per-file `UnoGenerateCodeBehind=true` overrides global `false` in `src/SourceGenerators/Uno.UI.SourceGenerators.Tests/XamlCodeBehindGeneratorTests/XamlCodeBehindGeneratorTests.cs`

> **RED/GREEN**: Some tests may already pass if config logic was correctly wired in T008/T023. Verify and fix any failures.

### Implementation for User Story 2 + 3

> Note: The core property/metadata reading logic is built in Phase 2 (T004) and wired into both generators in T008 and T023. This phase validates and hardens the configuration behavior.

- [X] T028 [US2] [US3] Verify and harden integrated path (XamlCodeGeneration) correctly reads `build_property.UnoGenerateCodeBehind` and respects `true`/`false`/absent (default `true`) in `src/SourceGenerators/Uno.UI.SourceGenerators/XamlGenerator/XamlCodeGeneration.cs`
- [X] T029 [US2] [US3] Verify and harden integrated path reads per-file `build_metadata.AdditionalFiles.UnoGenerateCodeBehind` and applies precedence (per-file wins over project-level) in `src/SourceGenerators/Uno.UI.SourceGenerators/XamlGenerator/XamlCodeGeneration.cs`
- [X] T030 [US2] [US3] Verify and harden standalone path (XamlCodeBehindGenerator) reads the same properties with identical behavior in `src/SourceGenerators/Uno.UI.SourceGenerators/XamlGenerator/XamlCodeBehindGenerator.cs`

> **GREEN**: All config tests T025-T027 must pass. Verify before proceeding.

**Checkpoint**: Full configuration control working - project-level and per-file toggles function correctly on all targets

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Final validation, edge case hardening, and cross-path parity

- [X] T031 Verify `ApplicationDefinition` (App.xaml) files without code-behind are handled correctly (same rules apply per edge cases in spec) across both generator paths
- [X] T032 Verify backward compatibility: existing projects with code-behind for all XAML files see zero behavioral change (SC-006) - no new generated files, no warnings
- [X] T033 Validate cross-path output parity (SC-005): compare generated source text from integrated path vs standalone path for the same XAML input and verify identical output
- [ ] T034 Run quickstart.md validation: create a minimal project with a XAML-only page, build for Skia, verify build succeeds and page renders
- [X] T035 Verify incremental generation: rebuild without changes produces no re-generation (performance goal from plan.md)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Phase 1 (T001 must be done for property reading)
- **US1 (Phase 3)**: Depends on Phase 2 (needs shared emitter). Tests first, then implementation.
- **US4 (Phase 4)**: Depends on Phase 2 (needs shared parser/emitter). Can run in parallel with Phase 3. Tests first, then implementation.
- **US2+US3 (Phase 5)**: Depends on Phase 3 and Phase 4 (validates config in both paths). Tests first, then hardening.
- **Polish (Phase 6)**: Depends on all previous phases

### User Story Dependencies

- **US1 (P1)**: Depends on Foundational only. Core MVP.
- **US4 (P1)**: Depends on Foundational only. Can proceed in parallel with US1 (different files).
- **US2+US3 (P2)**: Configuration logic is built during US1/US4, this phase validates and hardens it.

### Within Each User Story (Test-First Order per Constitution III)

1. Write tests that describe correct behavior (RED)
2. Verify tests fail
3. Implement the feature
4. Verify tests pass (GREEN)
5. Edge cases and hardening

### Parallel Opportunities

- T003 and T005 can run in parallel with T004 (different files)
- Phase 3 (US1) and Phase 4 (US4) can run in parallel (different files: XamlCodeGeneration.cs vs XamlCodeBehindGenerator.cs)
- All unit tests within a phase marked [P] can run in parallel (same file but independent test methods)
- T006 and T007 (US1 tests) can run in parallel

---

## Parallel Example: Foundational Phase

```bash
# Launch foundational tasks in parallel (different files):
Task T003: "Create XamlClassInfo record in XamlClassInfo.cs"
Task T005: "Create code-behind emitter in XamlCodeBehindEmitter.cs"
# T004 creates XamlCodeBehindParser.cs (single file, runs alone or parallel with T003/T005)
```

## Parallel Example: US1 + US4

```bash
# After Foundational phase, launch both P1 stories in parallel:
# Developer A: US1 (integrated path)
Task T006-T013: "Tests then XamlCodeGeneration.cs and XamlFileGenerator.cs"

# Developer B: US4 (standalone path)
Task T014-T024: "Tests then XamlCodeBehindGenerator.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (MSBuild props)
2. Complete Phase 2: Foundational (parser, emitter, config helper)
3. Complete Phase 3: User Story 1 - tests first (RED), then implementation (GREEN)
4. **STOP and VALIDATE**: Build a Skia project with a XAML-only page, verify it compiles and runs
5. Demo the zero-boilerplate experience

### Incremental Delivery

1. Setup + Foundational -> Infrastructure ready
2. Add US1 (Uno targets) -> Tests RED -> Implement -> Tests GREEN -> MVP!
3. Add US4 (WinUI targets) -> Tests RED -> Implement -> Tests GREEN -> Cross-platform!
4. Add US2+US3 (config controls) -> Tests RED/GREEN -> Harden -> Full feature
5. Polish -> Production ready

### Task Count Summary

| Phase | Tasks | Parallelizable |
|-------|-------|----------------|
| Phase 1: Setup | 2 | 0 |
| Phase 2: Foundational | 3 | 2 |
| Phase 3: US1 (tests + impl) | 8 | 1 |
| Phase 4: US4 (tests + impl) | 11 | 7 |
| Phase 5: US2+US3 (tests + impl) | 6 | 3 |
| Phase 6: Polish | 5 | 0 |
| **Total** | **35** | **13** |

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- US1 and US4 are both P1 but target different code paths (Uno vs WinUI) - they can proceed in parallel
- US2 and US3 are combined in Phase 5 because they share the same configuration mechanism
- The integrated path (US1) is the most complex due to XamlFileGenerator modifications for Symbol-null handling
- The standalone path (US4) is simpler but requires MSBuild targets coordination
- All source generator code must target `netstandard2.0`
- The `XDocument`-based parser (T004) is primarily for the WinUI standalone path; the integrated Uno path reuses existing `XamlCodeGeneration` type resolution
- Tests follow RED-GREEN ordering per Constitution Principle III within every user story phase
