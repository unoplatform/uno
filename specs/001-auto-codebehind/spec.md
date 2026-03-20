# Feature Specification: Auto-Generate Code-Behind for XAML Files

**Feature Branch**: `001-auto-codebehind`
**Created**: 2026-03-05
**Status**: Draft
**Input**: User description: "Add the ability to auto-generate code-behind for .xaml files which don't have it yet. If user has a MainPage.xaml file and no accompanying MainPage.xaml.cs, it would source generate the plain code-behind with just the constructor and InitializeComponent call inside. If user provides a code-behind file manually, it would not generate anything. It should only work for files which do not have a code-behind file. This should use the x:Class to identify it. In addition, it should be possible to explicitly disable/enable this behavior in general (in .csproj) and even specifically for concrete xaml files. The feature should work for both Uno Platform targets and WinUI (probably via Uno.Sdk). Note that existing Source Generators likely only run against Uno Platform targets."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Zero-Boilerplate XAML Page Creation (Priority: P1)

A developer creates a new XAML page (e.g., `MainPage.xaml`) with an `x:Class="MyApp.MainPage"` attribute but does not create a corresponding `MainPage.xaml.cs` file. At build time, the system automatically source-generates a code-behind file containing the partial class declaration with a constructor that calls `InitializeComponent()`. The developer's XAML page compiles and works without any manual code-behind authoring.

**Why this priority**: This is the core value proposition - eliminating boilerplate for simple XAML pages that only need `InitializeComponent()`. It directly reduces friction for new pages, especially in tutorials, prototypes, and MVUX/declarative patterns where code-behind is empty.

**Independent Test**: Can be fully tested by creating a XAML file with `x:Class` and no `.xaml.cs`, building the project, and verifying the generated code-behind exists with the correct constructor and `InitializeComponent()` call.

**Acceptance Scenarios**:

1. **Given** a project with `MainPage.xaml` containing `x:Class="MyApp.MainPage"` and no `MainPage.xaml.cs`, **When** the project is built, **Then** a source-generated partial class `MyApp.MainPage` is produced with a parameterless constructor calling `this.InitializeComponent()`.
2. **Given** a project with `MainPage.xaml` containing `x:Class="MyApp.MainPage"` and a developer-authored `MainPage.xaml.cs`, **When** the project is built, **Then** no code-behind is auto-generated for that file.
3. **Given** a project with `Settings.xaml` containing `x:Class="MyApp.Views.Settings"` (nested namespace) and no code-behind, **When** the project is built, **Then** the generated class uses the correct namespace `MyApp.Views` and class name `Settings`.

---

### User Story 2 - Project-Level Opt-In/Opt-Out Control (Priority: P2)

A developer or team wants to control whether auto code-behind generation is enabled or disabled across the entire project. They set an MSBuild property in their `.csproj` file to toggle the behavior globally.

**Why this priority**: Teams need project-level governance over this feature. Some teams may prefer explicit code-behind files for consistency, while others want the convenience of auto-generation.

**Independent Test**: Can be tested by setting the MSBuild property to `false`, creating a XAML file without code-behind, building, and verifying no code-behind is generated (build should fail or warn due to missing class).

**Acceptance Scenarios**:

1. **Given** a project with `<UnoGenerateCodeBehind>true</UnoGenerateCodeBehind>` in the `.csproj`, **When** XAML files without code-behind exist, **Then** code-behind is auto-generated for those files.
2. **Given** a project with `<UnoGenerateCodeBehind>false</UnoGenerateCodeBehind>` in the `.csproj`, **When** XAML files without code-behind exist, **Then** no code-behind is auto-generated.
3. **Given** a project with no `UnoGenerateCodeBehind` property set, **When** XAML files without code-behind exist, **Then** code-behind is auto-generated (feature is enabled by default).

---

### User Story 3 - Per-File Opt-Out/Opt-In (Priority: P2)

A developer wants to exclude specific XAML files from auto code-behind generation (or include specific files when the feature is globally disabled). They annotate the XAML file's MSBuild item with metadata to override the project-level setting.

**Why this priority**: Fine-grained control is essential for mixed scenarios - e.g., a project that generally wants auto code-behind but has a few XAML files (like ResourceDictionaries or complex pages) where manual code-behind is intentionally omitted.

**Independent Test**: Can be tested by adding MSBuild metadata to a specific XAML file entry and verifying it overrides the project-level setting.

**Acceptance Scenarios**:

1. **Given** auto code-behind is globally enabled and a XAML file has `<Page Include="Special.xaml"><UnoGenerateCodeBehind>false</UnoGenerateCodeBehind></Page>`, **When** the project is built, **Then** no code-behind is generated for `Special.xaml`.
2. **Given** auto code-behind is globally disabled and a XAML file has `<Page Include="Simple.xaml"><UnoGenerateCodeBehind>true</UnoGenerateCodeBehind></Page>`, **When** the project is built, **Then** code-behind is generated for `Simple.xaml`.

---

### User Story 4 - Cross-Platform Consistency (Priority: P1)

A developer using Uno Platform targets (Skia, WebAssembly, Mobile) and/or WinUI (Windows) expects auto code-behind generation to work consistently across all target platforms. The feature works on WinUI targets through Uno.Sdk integration, not just on Uno Platform targets where existing source generators already run.

**Why this priority**: The feature must work across all supported platforms to be useful. A feature that only works on some targets would cause confusing build failures.

**Independent Test**: Can be tested by building the same project for multiple target frameworks and verifying code-behind is generated consistently.

**Acceptance Scenarios**:

1. **Given** a multi-targeted Uno Platform project, **When** built for Skia (`net10.0`), **Then** auto code-behind is generated.
2. **Given** the same project, **When** built for WinUI (`net10.0-windows10.0.19041.0`), **Then** auto code-behind is generated.
3. **Given** the same project, **When** built for WebAssembly, Android, or iOS, **Then** auto code-behind is generated consistently.

---

### Edge Cases

- What happens when a XAML file has no `x:Class` attribute (e.g., a standalone `ResourceDictionary`)? No code-behind should be generated.
- What happens when a XAML file has `x:Class` but the class already exists in a different file (not named as conventional code-behind)? The system should detect the class exists in the compilation and not generate a duplicate.
- What happens when a developer-authored code-behind exists but is empty or has a different constructor signature? No auto-generation should occur - any existing code-behind file takes precedence.
- What happens when the `x:Class` value is malformed (missing namespace, invalid characters)? The system should report a diagnostic and skip generation for that file.
- What happens with `ApplicationDefinition` (App.xaml) files that have no code-behind? The same rules apply - auto-generate if no code-behind exists and `x:Class` is present.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST auto-generate a code-behind partial class for any XAML file that has an `x:Class` attribute but no corresponding class in the compilation.
- **FR-002**: The generated code-behind MUST contain a parameterless constructor that calls `this.InitializeComponent()`.
- **FR-003**: The generated code-behind MUST use the namespace and class name extracted from the `x:Class` attribute value.
- **FR-004**: The generated class MUST inherit from the base type specified as the XAML root element (e.g., `Page`, `UserControl`, `Window`, `ResourceDictionary`).
- **FR-005**: The system MUST NOT generate code-behind for XAML files where the `x:Class` type already exists in the compilation (i.e., the developer has provided a code-behind file or the class is defined elsewhere).
- **FR-006**: The system MUST NOT generate code-behind for XAML files that lack an `x:Class` attribute.
- **FR-007**: The system MUST support a project-level MSBuild property (`UnoGenerateCodeBehind`) to enable or disable the feature globally. The default value MUST be `true` (enabled).
- **FR-008**: The system MUST support per-file MSBuild item metadata (`UnoGenerateCodeBehind`) on `Page`, `UnoPage`, `ApplicationDefinition`, and `UnoApplicationDefinition` items to override the project-level setting.
- **FR-009**: Per-file metadata MUST take precedence over the project-level property.
- **FR-010**: The feature MUST work on all Uno Platform target frameworks (Skia, WebAssembly, Android, iOS, macOS, tvOS).
- **FR-011**: The feature MUST work on WinUI target frameworks (`net10.0-windows10.0.*`) via Uno.Sdk integration.
- **FR-012**: The generated source MUST be produced by a Roslyn source generator (IIncrementalGenerator) to integrate with the standard build pipeline and IDE experience.
- **FR-013**: The generated class MUST be declared as `partial` to allow the XAML source generator to add `InitializeComponent()` and named fields to the same class.

### Key Entities

- **XAML File**: A `.xaml` file included in the project as a `Page`, `UnoPage`, `ApplicationDefinition`, or `UnoApplicationDefinition` MSBuild item. Contains an optional `x:Class` attribute identifying the code-behind class.
- **Code-Behind Class**: A partial C# class corresponding to the `x:Class` value. Can be developer-authored or auto-generated. Contains at minimum a constructor calling `InitializeComponent()`.
- **Project Configuration**: MSBuild properties and item metadata controlling the auto-generation behavior at project and file level.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Developers can create new XAML pages without writing any code-behind file, and the project builds successfully on all target platforms.
- **SC-002**: Projects with a mix of auto-generated and manually-authored code-behind files build without conflicts or duplicate class definitions.
- **SC-003**: The auto-generation can be toggled on/off at the project level via a single MSBuild property, with changes taking effect on the next build.
- **SC-004**: Individual XAML files can be excluded from or included in auto-generation via per-file metadata, overriding the project-level setting.
- **SC-005**: The feature works identically across Uno Platform targets and WinUI Windows targets, producing the same compilation result.
- **SC-006**: No behavioral change occurs for existing projects that already have code-behind files for all their XAML pages (full backward compatibility).

## Assumptions

- The Roslyn source generator infrastructure is available for all target platforms via the existing `Uno.UI.SourceGenerators` project and Uno.Sdk.
- The existing `XamlCodeGeneration` pipeline already parses `x:Class` and can be extended to detect missing code-behind classes.
- For WinUI targets, a source generator can be delivered through Uno.Sdk (which is already consumed by WinUI projects using Uno Platform).
- The `InitializeComponent()` method will be generated by the existing XAML source generator in a separate partial class file, as it does today.
- ResourceDictionary XAML files without `x:Class` are intentionally excluded and do not need code-behind.
