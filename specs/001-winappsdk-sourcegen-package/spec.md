# Feature Specification: WinAppSDK Source Generator Package

**Feature Branch**: `001-winappsdk-sourcegen-package`
**Created**: 2026-03-13
**Status**: Draft
**Input**: User description: "Create a new package Uno.UI.SourceGenerators.WinAppSDK that would contain the WinAppSDK-specific generators, separated from Uno.UI.SourceGenerators."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Code-behind generation works on WinAppSDK projects without interference (Priority: P1)

As a developer using Uno Platform with WinAppSDK targets, I want code-behind files to be automatically generated for XAML pages that lack explicit `.xaml.cs` files, using a dedicated WinAppSDK generator package. Currently, the build system must remove all Uno.UI.SourceGenerators analyzers (which include many generators irrelevant to WinAppSDK) and then re-add the same DLL in a restricted "code-behind-only" mode. A dedicated package eliminates this fragile remove-and-re-add dance.

**Why this priority**: This is the core reason for the new package — ensuring WinAppSDK projects get code-behind generation from a clean, isolated package rather than a multi-purpose generator assembly running in a gated mode.

**Independent Test**: Can be fully tested by building a WinAppSDK project with XAML pages that have no explicit code-behind files and verifying that partial classes with constructors calling `InitializeComponent()` are generated.

**Acceptance Scenarios**:

1. **Given** a WinAppSDK project with a XAML page containing `x:Class` and no corresponding `.xaml.cs` file, **When** the project is built, **Then** the new package generates a minimal partial class with a constructor calling `InitializeComponent()`.
2. **Given** a WinAppSDK project with a XAML page that already has an explicit `.xaml.cs` file, **When** the project is built, **Then** no code-behind file is generated for that page.
3. **Given** a WinAppSDK project, **When** the project is built, **Then** no other Uno-specific source generators (XAML parser, DependencyObject generator, etc.) run against the project.

---

### User Story 2 - Existing Uno Platform targets remain unaffected (Priority: P1)

As a developer using Uno Platform targeting Skia, WebAssembly, or mobile, I want my build to continue working exactly as before. The new WinAppSDK package must not be referenced or loaded for Uno Platform targets.

**Why this priority**: Regression prevention is equally critical — the change must not break existing Uno Platform builds.

**Independent Test**: Can be tested by building an Uno Platform project targeting Skia or WebAssembly and verifying the code-behind generation continues to work through the existing integrated XAML code generation pipeline.

**Acceptance Scenarios**:

1. **Given** an Uno Platform project targeting Skia, **When** the project is built, **Then** code-behind generation is handled by the existing integrated XAML code generation pipeline, not the new WinAppSDK package.
2. **Given** an Uno Platform project, **When** the project is built, **Then** the new WinAppSDK source generator package is not loaded as an analyzer.

---

### User Story 3 - Per-file and project-level code-behind configuration on WinAppSDK (Priority: P2)

As a developer using WinAppSDK, I want to control code-behind generation at both the project level and per-file level, just as I can on Uno Platform targets. I should be able to disable generation globally via a project property or opt specific XAML files in or out via item metadata.

**Why this priority**: Configuration parity with Uno Platform targets ensures a consistent developer experience, but the feature works with defaults even without explicit configuration.

**Independent Test**: Can be tested by setting `UnoGenerateCodeBehind` to `false` at the project level and verifying no code-behind is generated, then enabling it per-file on a specific XAML page and verifying generation resumes for that file only.

**Acceptance Scenarios**:

1. **Given** a WinAppSDK project with `UnoGenerateCodeBehind` set to `false`, **When** the project is built, **Then** no code-behind files are generated for any XAML page.
2. **Given** a WinAppSDK project with `UnoGenerateCodeBehind` set to `false` globally but a specific Page item has `UnoGenerateCodeBehind` metadata set to `true`, **When** the project is built, **Then** a code-behind file is generated only for that specific page.

---

### Edge Cases

- What happens when the new WinAppSDK package and the full Uno.UI.SourceGenerators are both accidentally referenced in the same project? The system should not produce duplicate generated files or build errors.
- What happens when a XAML file has a malformed `x:Class` attribute (e.g., missing namespace)? The generator should emit a diagnostic warning and skip that file.
- What happens when a project migrates from the old approach (gated mode in Uno.UI.SourceGenerators) to the new dedicated package? The transition should be seamless with no build breaks.
- What happens when a custom root element type is used (not Page, UserControl, Window, etc.)? The generator should still produce correct code-behind using the namespace from the XAML xmlns declaration.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST provide a dedicated, separately-packaged source generator for WinAppSDK targets that generates code-behind files independently from the Uno.UI.SourceGenerators package.
- **FR-002**: The WinAppSDK source generator MUST generate a minimal partial class with a parameterless constructor calling `InitializeComponent()` for every XAML file with an `x:Class` attribute that does not have a corresponding code-behind source file.
- **FR-003**: The WinAppSDK source generator MUST NOT generate code-behind for XAML files that already have an explicit code-behind file in the compilation.
- **FR-004**: The system MUST support the `UnoGenerateCodeBehind` MSBuild property at the project level to globally enable or disable code-behind generation (default: enabled).
- **FR-005**: The system MUST support per-file `UnoGenerateCodeBehind` item metadata on Page and ApplicationDefinition items, with per-file settings taking precedence over the project-level property.
- **FR-006**: The WinAppSDK source generator MUST correctly map standard WinUI root elements (Page, UserControl, Window, ContentDialog, Application, ResourceDictionary, SwapChainPanel) to their fully-qualified base types.
- **FR-007**: The WinAppSDK source generator MUST handle custom namespace prefixes (`using:` and `clr-namespace:`) to resolve non-standard root element types.
- **FR-008**: The system MUST emit a diagnostic warning when an `x:Class` attribute is malformed (e.g., missing namespace separator).
- **FR-009**: No Uno-specific source generators other than the code-behind generator MUST run on WinAppSDK target builds.
- **FR-010**: The existing Uno Platform source generation pipeline MUST remain unchanged and unaffected by this packaging change.
- **FR-011**: The WinAppSDK package MUST be automatically included for WinAppSDK target projects through the existing Uno SDK infrastructure — developers should not need to manually reference it.

### Key Entities

- **WinAppSDK Source Generator Package**: A standalone NuGet package containing only the code-behind source generator, intended exclusively for WinAppSDK target builds.
- **Code-Behind Generator**: The incremental source generator that produces partial class files for XAML pages lacking explicit code-behind files.
- **XAML Class Info**: Metadata extracted from XAML files (full class name, namespace, root element type) used to generate the code-behind output.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: WinAppSDK projects build successfully with code-behind generation working identically to the current behavior — all existing tests pass without modification.
- **SC-002**: Uno Platform target builds (Skia, WebAssembly, mobile) continue to work identically — no regressions in existing functionality.
- **SC-003**: The WinAppSDK build no longer requires the remove-and-re-add analyzer workaround — the dedicated package is referenced directly.
- **SC-004**: The WinAppSDK generator package contains only the code-behind generator — no other Uno source generators are included.
- **SC-005**: All existing unit tests for the code-behind generator continue to pass against the new package.

## Assumptions

- The code-behind generator logic (parser, emitter, class info) can be shared or extracted between the Uno.UI.SourceGenerators and the new WinAppSDK package without duplication, or acceptable duplication is tolerated for isolation.
- The Uno SDK build infrastructure (`Uno.Sdk/targets/`) can be modified to reference the new package for WinAppSDK targets instead of using the gated analyzer approach.
- The code-behind generator is currently the only generator needed on WinAppSDK targets. If additional WinAppSDK-specific generators are needed in the future, they can be added to this new package.
- The NuGet packaging infrastructure supports creating and distributing the new package alongside existing Uno packages.
