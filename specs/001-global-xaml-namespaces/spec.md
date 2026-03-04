# Feature Specification: Global/Implicit XAML Namespaces

**Feature Branch**: `001-global-xaml-namespaces`
**Created**: 2026-03-04
**Status**: Draft
**Input**: User description: "Add support for global/implicit XAML namespaces. Investigate the approach in MAUI and have this work in all Uno Platform targets and even in WinUI/WinAppSDK (via the Uno.Sdk)"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Write XAML Without Boilerplate Namespace Declarations (Priority: P1)

As a developer using Uno Platform, I want to write XAML pages without having to repeat `xmlns` declarations for the default WinUI namespace and `xmlns:x` in every file, so that my XAML files are shorter and easier to read.

Today, every XAML file requires at minimum:
```xml
<Page
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    x:Class="MyApp.MainPage">
```

With global/implicit namespaces enabled, the same page becomes:
```xml
<Page x:Class="MyApp.MainPage">
```

The default WinUI presentation namespace and the `x:` XAML namespace are implicitly available without explicit declaration.

**Why this priority**: This is the core value proposition. Eliminating repetitive boilerplate in every XAML file immediately improves developer productivity and XAML readability across all projects.

**Independent Test**: Can be tested by creating a new Uno Platform project, enabling the feature, and verifying that a XAML page compiles and renders correctly without explicit `xmlns` and `xmlns:x` declarations.

**Acceptance Scenarios**:

1. **Given** a new Uno Platform project with global namespaces enabled, **When** a developer creates a XAML page with only `x:Class` and no explicit `xmlns` declarations, **Then** the XAML compiles successfully and the page renders correctly on all targets (WebAssembly, Skia Desktop, Android, iOS, Windows/WinAppSDK).
2. **Given** global namespaces are enabled, **When** a developer uses standard WinUI controls (`Button`, `TextBlock`, `Grid`, `StackPanel`, etc.) without any namespace prefix, **Then** the controls are correctly resolved and rendered.
3. **Given** global namespaces are enabled, **When** a developer uses `x:Name`, `x:Class`, `x:Bind`, `x:Key`, and other `x:` prefixed attributes, **Then** they work correctly without an explicit `xmlns:x` declaration.
4. **Given** a project that has explicitly opted out of global namespaces via the MSBuild property, **When** XAML files omit `xmlns` declarations, **Then** compilation fails as it does today (backward compatibility).

---

### User Story 2 - Register App-Specific CLR Namespaces as Global (Priority: P2)

As a developer, I want to register my own CLR namespaces (view models, converters, custom controls) as globally available in XAML, so that I can reference my types without `xmlns:local` or `xmlns:vm` prefixes in every XAML file.

The developer creates a `GlobalXmlns.cs` file:
```csharp
[assembly: XmlnsDefinition("http://schemas.microsoft.com/winfx/2006/xaml/presentation/global", "MyApp.ViewModels")]
[assembly: XmlnsDefinition("http://schemas.microsoft.com/winfx/2006/xaml/presentation/global", "MyApp.Converters")]
```

Then in XAML, types from these namespaces are available without prefixes:
```xml
<Page x:Class="MyApp.MainPage" x:DataType="MainViewModel">
    <TextBlock Text="{x:Bind Name}" />
</Page>
```

**Why this priority**: This extends the core feature to user-defined types, providing the full "MAUI-like" experience where developers centralize namespace configuration.

**Independent Test**: Can be tested by creating a class in a custom CLR namespace, registering it via `XmlnsDefinition` to the global namespace URI, and referencing the type unprefixed in XAML.

**Acceptance Scenarios**:

1. **Given** a developer has registered `MyApp.ViewModels` to the global namespace URI, **When** they reference `MainViewModel` in XAML without a prefix, **Then** the type is correctly resolved and the XAML compiles.
2. **Given** multiple CLR namespaces are registered to the global namespace URI, **When** two types with different names exist across these namespaces, **Then** both are resolvable without prefixes.
3. **Given** a type name exists in both the default WinUI namespace and a user-registered global namespace, **When** the developer uses the type name unprefixed, **Then** the WinUI type takes precedence (consistent with existing override behavior) and the developer can disambiguate using an explicit `xmlns` prefix.

---

### User Story 3 - Register Third-Party Library Namespaces as Global (Priority: P2)

As a developer using third-party control libraries (e.g., Syncfusion, Telerik, or Uno Toolkit), I want to register those libraries' namespaces as globally available, so that I can use their controls without per-file prefix declarations.

```csharp
[assembly: XmlnsDefinition("http://schemas.microsoft.com/winfx/2006/xaml/presentation/global",
    "Syncfusion.UI.Xaml.Charts", AssemblyName = "Syncfusion.SfChart.WinUI")]
```

**Why this priority**: Third-party library integration is a common pain point with verbose namespace declarations. This has equal value to app-specific namespaces.

**Independent Test**: Can be tested by adding a third-party NuGet, registering its namespace globally, and using its controls unprefixed in XAML.

**Acceptance Scenarios**:

1. **Given** a third-party library's CLR namespace is registered to the global URI with `AssemblyName` specified, **When** the developer uses a type from that library unprefixed, **Then** the type is correctly resolved across the assembly boundary.
2. **Given** a third-party library ships its own `XmlnsDefinition` attributes mapping to the global URI, **When** a developer references the library, **Then** the library's types are automatically available without any developer-side registration.

---

### User Story 4 - Disambiguation with Prefixed Namespaces (Priority: P3)

As a developer, when type name collisions occur between global namespaces, I want to disambiguate by declaring explicit `xmlns` prefixes for specific namespaces, just as I do today.

**Why this priority**: Name collisions are an edge case but must have a well-defined resolution path to avoid blocking developers.

**Independent Test**: Can be tested by registering two CLR namespaces with identically-named types to the global URI, then verifying that explicit `xmlns` prefix disambiguation works.

**Acceptance Scenarios**:

1. **Given** two globally registered namespaces both contain a type called `Card`, **When** the developer uses `<Card />` unprefixed, **Then** the compiler reports a clear, actionable ambiguity error indicating which namespaces conflict.
2. **Given** an ambiguity exists, **When** the developer adds an explicit `xmlns:toolkit="using:Uno.Toolkit"` and uses `<toolkit:Card />`, **Then** the explicit prefix resolves the ambiguity and compilation succeeds.

---

### User Story 5 - Works on WinUI/WinAppSDK via Uno.Sdk (Priority: P1)

As a developer targeting WinUI/WinAppSDK (native Windows), I want global XAML namespaces to also work when building for the Windows target through the Uno.Sdk, so that I have a consistent XAML authoring experience across all platforms.

**Why this priority**: Cross-platform consistency is a core Uno Platform value. If global namespaces only work on Uno targets but not on the Windows/WinAppSDK target, the feature has limited utility for cross-platform projects.

**Independent Test**: Can be tested by building the same XAML (without explicit `xmlns`) for both a Skia target and the `net10.0-windows10.0.19041.0` target and verifying both compile and render correctly.

**Acceptance Scenarios**:

1. **Given** an Uno Platform project targeting Windows via WinAppSDK, **When** global namespaces are enabled and XAML omits default `xmlns` declarations, **Then** the project compiles and runs correctly on Windows.
2. **Given** the same XAML file targeting both Uno (Skia/Wasm/Mobile) and WinAppSDK, **When** built for each target, **Then** the XAML compiles identically on all targets without modification.

---

### Edge Cases

- What happens when a XAML file explicitly declares an `xmlns` that conflicts with a globally registered namespace? The explicit file-level declaration takes precedence.
- What happens when `XmlnsDefinition` attributes from different assemblies register the same CLR namespace to the global URI multiple times? The type should be found once without duplication errors.
- How does IntelliSense/tooling behave? The feature should degrade gracefully - compilation must work even if IDE tooling shows warnings or red squiggles initially.
- What happens with XAML Hot Reload? Modified XAML without explicit namespaces must still be parseable by the Hot Reload pipeline.
- What happens with design-time attributes (`d:DesignInstance`, `mc:Ignorable`)? These standard patterns must continue to work; `mc:` prefix may still require explicit declaration since it is not part of the default WinUI namespace set.
- What happens with `x:Bind` expressions referencing types from global namespaces? Type resolution in binding expressions must also respect globally registered namespaces.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST support implicit/default XAML namespace declarations so that XAML files can omit the standard `xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"` and `xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"` declarations.
- **FR-002**: The feature MUST be enabled by default for all projects using the Uno.Sdk, with an MSBuild property available to opt out. Existing projects upgrading to the new Uno.Sdk version receive the feature automatically. XAML files with explicit `xmlns` declarations continue to work unchanged (the feature is additive, not breaking).
- **FR-003**: The system MUST define a dedicated "global" namespace URI (separate from the existing presentation namespace) that aggregates all user-registered CLR namespaces for unprefixed type resolution. The implicit namespace feature auto-declares both the standard presentation namespace and this global URI as defaults.
- **FR-004**: Developers MUST be able to register additional CLR namespaces to the global namespace URI using `[assembly: XmlnsDefinition(...)]` attributes.
- **FR-005**: The system MUST resolve unprefixed XAML type references by searching all CLR namespaces registered under the global namespace URI.
- **FR-006**: The Uno Platform XAML source generator MUST be updated to support parsing XAML without explicit namespace declarations when the feature is enabled.
- **FR-007**: The feature MUST work across all Uno Platform targets: WebAssembly, Skia (Windows, macOS, Linux), Android, iOS, and native Windows (WinUI/WinAppSDK).
- **FR-008**: When type name collisions occur across globally registered namespaces, the system MUST produce a clear, actionable compiler error indicating the conflicting namespaces.
- **FR-009**: Explicit per-file `xmlns` declarations MUST take precedence over implicit global registrations.
- **FR-010**: The system MUST support the `XmlnsPrefix` attribute to allow developers to define default prefixes for namespaces, enabling prefix usage without per-file declaration.
- **FR-011**: XAML Hot Reload MUST continue to function correctly for XAML files that use implicit namespaces.
- **FR-012**: The feature MUST work for WinUI/WinAppSDK targets when built through the Uno.Sdk via an MSBuild pre-processing step that injects implicit `xmlns` declarations into temporary XAML copies before the WinUI XAML compiler processes them, coordinated with the Uno source generator on non-Windows targets to ensure identical behavior.

### Key Entities

- **Global Namespace URI**: A dedicated XML namespace URI (separate from the standard presentation URI) that serves as the aggregation point for user-registered and third-party CLR namespaces. When the feature is enabled, both the standard presentation namespace and this global URI are implicitly declared, allowing unprefixed resolution of framework types (via presentation URI) and user/library types (via global URI).
- **XmlnsDefinition Attribute**: Assembly-level attribute that maps a CLR namespace to an XML namespace URI. Used by developers to register their namespaces as globally available.
- **XmlnsPrefix Attribute**: Assembly-level attribute that associates a default prefix with an XML namespace URI, enabling implicit prefix availability.
- **Implicit Namespace Set**: The collection of namespace-to-prefix mappings that are automatically injected into the XAML parser context when the feature is enabled. Minimally includes the default presentation namespace (no prefix) and the XAML namespace (`x:` prefix).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A new Uno Platform project with the feature enabled compiles and renders a multi-page XAML application on all supported targets without any `xmlns` declarations in XAML files.
- **SC-002**: Developers can register custom CLR namespaces and use their types unprefixed in XAML, with no more than one line of attribute code per CLR namespace.
- **SC-003**: Existing projects upgrading to the new Uno.Sdk version continue to compile and run correctly - the feature is additive (implicit namespaces supplement, never override, explicit declarations). Projects can opt out if needed.
- **SC-004**: The same XAML source files compile identically on Uno Platform targets and WinUI/WinAppSDK targets (via Uno.Sdk).
- **SC-005**: XAML Hot Reload continues to work correctly for files using implicit namespaces.
- **SC-006**: Type name collision errors provide clear diagnostic messages identifying the conflicting namespaces, enabling developers to resolve ambiguities within minutes.

## Clarifications

### Session 2026-03-04

- Q: How should the Uno.Sdk make global namespaces work on WinAppSDK targets given that WinUI uses Microsoft's closed-source XamlCompiler.exe? → A: MSBuild pre-processing + source generator coordination: inject missing `xmlns` declarations into XAML temp copies before the WinUI compiler runs, AND align this with Uno's source generator for non-Windows targets so both paths produce identical results.
- Q: Should user-registered global types use the existing presentation namespace URI or a separate one? → A: Separate global URI - keeps user-registered types isolated from the framework default namespace, matching MAUI's pattern. The implicit namespace feature auto-declares both the standard presentation namespace AND the global URI, but they remain distinct.
- Q: Should new projects have global namespaces enabled by default? → A: Enabled by default for ALL projects via the Uno.Sdk. Existing projects upgrading to the new Uno.Sdk version get the feature automatically. Projects can opt out via an MSBuild property.

## Assumptions

- The global namespace URI will follow a pattern consistent with existing WinUI XML namespace conventions.
- The `XmlnsDefinition` and `XmlnsPrefix` attributes already exist in the Uno Platform codebase and can be reused without creating new attribute types.
- The Uno Platform XAML source generator is the primary compilation path for XAML on all non-Windows targets, and modifications to it will cover those targets.
- For WinUI/WinAppSDK targets, the Uno.Sdk's MSBuild integration can inject the necessary configuration to make the WinUI XAML compiler behave consistently with Uno's source generator.
- IDE tooling (Visual Studio, VS Code) may initially show warnings or red squiggles for XAML without explicit namespaces; this is acceptable as long as compilation succeeds. Tooling improvements can be addressed separately.
- The MAUI implementation provides a proven technical approach that can be adapted for Uno Platform's source generator pipeline.
