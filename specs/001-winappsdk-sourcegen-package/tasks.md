# Tasks: WinAppSDK Source Generator Package

**Input**: Design documents from `/specs/001-winappsdk-sourcegen-package/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/msbuild-contract.md, quickstart.md

**Tests**: Not explicitly requested in feature specification. Test infrastructure updates are included where needed for existing test migration.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup

**Purpose**: Create the new project skeleton and directory structure

- [x] T001 Create new project file at `src/SourceGenerators/Uno.UI.SourceGenerators.WinAppSDK/Uno.UI.SourceGenerators.WinAppSDK.csproj` targeting `netstandard2.0` with `Microsoft.CodeAnalysis.CSharp` (v4.0.1) dependency, `EnforceExtendedAnalyzerRules`, and analyzer packaging properties (`IncludeBuildOutput=false`, pack DLL into `analyzers/dotnet/cs/`)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Create all source files in the new project — these MUST be complete before any user story integration work can begin

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [x] T002 [P] Create `src/SourceGenerators/Uno.UI.SourceGenerators.WinAppSDK/XamlClassInfo.cs` — copy the `XamlClassInfo` record struct from `src/SourceGenerators/Uno.UI.SourceGenerators/XamlGenerator/XamlClassInfo.cs`, update namespace to `Uno.UI.SourceGenerators.WinAppSDK`
- [x] T003 [P] Create `src/SourceGenerators/Uno.UI.SourceGenerators.WinAppSDK/XamlCodeBehindDiagnostics.cs` — define static class with `UNOB0001` `DiagnosticDescriptor` (Warning, category "XAML", title "Invalid x:Class Value", message format `{0}`), per research Decision 3
- [x] T004 [P] Create `src/SourceGenerators/Uno.UI.SourceGenerators.WinAppSDK/XamlCodeBehindParser.cs` — copy from `src/SourceGenerators/Uno.UI.SourceGenerators/XamlGenerator/XamlCodeBehindParser.cs`, update namespace and change `XamlCodeGenerationDiagnostics.InvalidXClassRule` reference to use local `XamlCodeBehindDiagnostics`
- [x] T005 [P] Create `src/SourceGenerators/Uno.UI.SourceGenerators.WinAppSDK/XamlCodeBehindEmitter.cs` — copy from `src/SourceGenerators/Uno.UI.SourceGenerators/XamlGenerator/XamlCodeBehindEmitter.cs`, update namespace
- [x] T006 Create `src/SourceGenerators/Uno.UI.SourceGenerators.WinAppSDK/XamlCodeBehindGenerator.cs` — copy from `src/SourceGenerators/Uno.UI.SourceGenerators/XamlGenerator/XamlCodeBehindGenerator.cs`, update namespace and ensure references to parser, emitter, diagnostics, and class info resolve to the local types (depends on T002–T005)

**Checkpoint**: New project builds successfully — `dotnet build src/SourceGenerators/Uno.UI.SourceGenerators.WinAppSDK/Uno.UI.SourceGenerators.WinAppSDK.csproj`

---

## Phase 3: User Story 1 — Code-behind generation works on WinAppSDK projects without interference (Priority: P1) 🎯 MVP

**Goal**: WinAppSDK projects get code-behind generation from the new dedicated package, replacing the fragile remove-and-re-add workaround

**Independent Test**: Build a WinAppSDK project with XAML pages lacking explicit `.xaml.cs` files and verify partial classes with `InitializeComponent()` constructors are generated. Verify no other Uno source generators run.

### Implementation for User Story 1

- [x] T007 [US1] Create MSBuild props file at `src/SourceGenerators/Uno.UI.SourceGenerators.WinAppSDK/Content/Uno.UI.SourceGenerators.WinAppSDK.props` — define `CompilerVisibleProperty` for `UnoGenerateCodeBehind`, `CompilerVisibleItemMetadata` for `AdditionalFiles` (`SourceItemGroup` and `UnoGenerateCodeBehind`), and `_InjectAdditionalFilesForWinAppSDK` target that injects `Page` and `ApplicationDefinition` items into `AdditionalFiles`, per contracts/msbuild-contract.md
- [x] T008 [P] [US1] Create NuGet package spec at `build/nuget/Uno.UI.SourceGenerators.WinAppSDK.nuspec` — package the DLL into `analyzers/dotnet/cs/`, props file into `buildTransitive/`, follow existing Uno versioning (`$version$`), no dependencies on `Uno.Xaml` or telemetry, per research Decision 6
- [x] T009 [US1] Update `src/Uno.Sdk/targets/Uno.UI.SourceGenerators.WinAppSdk.props` — replace the current gated analyzer approach with a direct `PackageReference` to `Uno.UI.SourceGenerators.WinAppSDK`, per research Decision 5
- [x] T010 [US1] Simplify `build/nuget/uno.winui.winappsdk.targets` — remove the `_AddCodeBehindGeneratorForWinUI` and `_InjectAdditionalFilesForWinUI` targets, keep `_RemoveRoslynUnoSourceGenerationWinUI` (still removes `Uno.UI.SourceGenerators` analyzer), per contracts/msbuild-contract.md "Removed Contracts" table

**Checkpoint**: New package is wired into the build — WinAppSDK targets reference the new package and the remove-and-re-add dance is eliminated

---

## Phase 4: User Story 2 — Existing Uno Platform targets remain unaffected (Priority: P1)

**Goal**: Skia, WebAssembly, and mobile builds continue working identically — the new package is not loaded for Uno Platform targets, and the old integrated code-behind pipeline is unaffected

**Independent Test**: Build an Uno Platform project targeting Skia or WebAssembly and verify code-behind generation works through the existing integrated XAML code generation pipeline

### Implementation for User Story 2

- [x] T011 [P] [US2] Delete standalone code-behind source files from old project: `src/SourceGenerators/Uno.UI.SourceGenerators/XamlGenerator/XamlCodeBehindGenerator.cs`, `XamlCodeBehindParser.cs`, `XamlCodeBehindEmitter.cs`, and `XamlClassInfo.cs`, per research Decision 2
- [x] T012 [P] [US2] Remove `UnoCodeBehindGeneratorOnly` property handling from `src/SourceGenerators/Uno.UI.SourceGenerators/Content/Uno.UI.SourceGenerators.props` — remove the CompilerVisibleProperty entry and any conditional logic gated on this property
- [x] T013 [US2] Review and update `build/nuget/Uno.WinUI.nuspec` — verify no references to the removed code-behind files or `UnoCodeBehindGeneratorOnly` property remain
- [x] T014 [US2] Update test project: add project reference to `Uno.UI.SourceGenerators.WinAppSDK` in `src/SourceGenerators/Uno.UI.SourceGenerators.Tests/Uno.UI.SourceGenerators.Tests.csproj` and update `XamlCodeBehindGeneratorTests.cs` to reference the generator type from the new assembly namespace

**Checkpoint**: Existing Uno Platform builds work identically — `dotnet build src/Uno.UI-UnitTests-only.slnf --no-restore` succeeds, `dotnet test src/SourceGenerators/Uno.UI.SourceGenerators.Tests/ --filter "FullyQualifiedName~XamlCodeBehind"` passes

---

## Phase 5: User Story 3 — Per-file and project-level code-behind configuration on WinAppSDK (Priority: P2)

**Goal**: Developers can control code-behind generation via `UnoGenerateCodeBehind` at both project level and per-file item metadata, with per-file taking precedence

**Independent Test**: Set `UnoGenerateCodeBehind` to `false` at project level and verify no code-behind is generated, then enable it per-file on one XAML page and verify generation resumes for that file only

### Implementation for User Story 3

- [x] T015 [US3] Verify that the props file created in T007 exposes `UnoGenerateCodeBehind` as a `CompilerVisibleProperty` and per-file `UnoGenerateCodeBehind` as `CompilerVisibleItemMetadata` on `AdditionalFiles` — ensure the generator code copied in T006 reads both `build_property.UnoGenerateCodeBehind` and `build_metadata.AdditionalFiles.UnoGenerateCodeBehind` and applies per-file precedence over project-level setting
- [x] T016 [US3] Verify that `UnoGenerateCodeBehind` defaults to `true` when not explicitly set — confirm the generator treats missing/empty property value as enabled, per data-model.md MSBuild Properties table

**Checkpoint**: Configuration parity confirmed — project-level disable prevents all generation, per-file override re-enables for specific files

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Final validation and cleanup across all stories

- [x] T017 [P] Run full build verification per quickstart.md: `dotnet build src/SourceGenerators/Uno.UI.SourceGenerators.WinAppSDK/Uno.UI.SourceGenerators.WinAppSDK.csproj`, then `dotnet test src/SourceGenerators/Uno.UI.SourceGenerators.Tests/ --filter "FullyQualifiedName~XamlCodeBehind"`, then `dotnet build src/Uno.UI-UnitTests-only.slnf --no-restore` and `dotnet test src/Uno.UI/Uno.UI.Tests.csproj --no-build`
- [x] T018 Verify the new nuspec produces a valid package structure matching contracts/msbuild-contract.md: DLL in `analyzers/dotnet/cs/`, props in `buildTransitive/`, no unexpected dependencies
- [x] T019 Verify edge case: ensure that if both `Uno.UI.SourceGenerators` and `Uno.UI.SourceGenerators.WinAppSDK` are accidentally referenced, no duplicate generated files or build errors occur (the old project no longer contains the standalone code-behind generator after T011)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately
- **Foundational (Phase 2)**: Depends on Phase 1 — BLOCKS all user stories
- **US1 (Phase 3)**: Depends on Phase 2 completion — BLOCKS US2 (cannot safely remove from old project until new package is wired in)
- **US2 (Phase 4)**: Depends on Phase 3 (US1) completion — removal is safe only after new package replaces old
- **US3 (Phase 5)**: Depends on Phase 3 (US1) completion — configuration lives in the props/generator created in US1
- **Polish (Phase 6)**: Depends on all user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational (Phase 2) — no dependencies on other stories
- **User Story 2 (P1)**: Depends on US1 — must not remove from old project until new package is in place
- **User Story 3 (P2)**: Depends on US1 — configuration verification requires the props and generator to exist

### Within Each User Story

- US1: Props file (T007) before SDK targets update (T009); nuspec (T008) is parallel with props
- US2: File deletion (T011) and props cleanup (T012) are parallel; nuspec review (T013) and test update (T014) follow
- US3: Verification tasks (T015, T016) are sequential — confirm props then confirm defaults

### Parallel Opportunities

- Phase 2: All source file creation tasks (T002–T005) can run in parallel
- Phase 3: T008 (nuspec) can run in parallel with T007 (props)
- Phase 4: T011 (delete files) and T012 (clean props) can run in parallel
- Phase 6: T017 (build verification) and T018 (nuspec verification) can run in parallel

---

## Parallel Example: Phase 2 (Foundational)

```
# Launch all independent source file copies together:
Task T002: "Create XamlClassInfo.cs in new project"
Task T003: "Create XamlCodeBehindDiagnostics.cs in new project"
Task T004: "Create XamlCodeBehindParser.cs in new project"
Task T005: "Create XamlCodeBehindEmitter.cs in new project"

# Then sequentially:
Task T006: "Create XamlCodeBehindGenerator.cs (depends on T002-T005)"
```

## Parallel Example: Phase 4 (User Story 2)

```
# Launch independent cleanup tasks together:
Task T011: "Delete code-behind files from old project"
Task T012: "Remove UnoCodeBehindGeneratorOnly from old props"

# Then sequentially:
Task T013: "Review Uno.WinUI.nuspec"
Task T014: "Update test project references"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (T001)
2. Complete Phase 2: Foundational (T002–T006)
3. Complete Phase 3: User Story 1 (T007–T010)
4. **STOP and VALIDATE**: Build the new project and verify code-behind generation works for WinAppSDK targets
5. This is a functional MVP — the new package works, even though the old code-behind files still exist in Uno.UI.SourceGenerators

### Incremental Delivery

1. Setup + Foundational → New project builds successfully
2. Add User Story 1 → New package wired into build → **MVP!**
3. Add User Story 2 → Old project cleaned up, tests migrated → Full isolation achieved
4. Add User Story 3 → Configuration parity verified → Feature complete
5. Polish → Full validation across all build configurations

### Sequential Execution (Recommended for Single Developer)

This feature has a natural sequential flow due to US2 depending on US1:

1. Phase 1 → Phase 2 → Phase 3 (US1) → Phase 4 (US2) → Phase 5 (US3) → Phase 6

---

## Notes

- [P] tasks = different files, no dependencies on incomplete tasks
- [Story] label maps task to specific user story for traceability
- US1 and US2 are both P1 but US2 depends on US1 — implement US1 first
- Research decisions (research.md) inform implementation choices in each task
- The total new code is ~340 lines across 5 source files — this is a packaging/refactoring feature, not new functionality
- Commit after each phase or logical group of tasks
- Stop at any checkpoint to validate independently
