<!--
  Sync Impact Report
  ==================================================
  Version change: (none) -> 1.0.0 (MAJOR - initial ratification)

  Modified principles: N/A (initial version)

  Added sections:
    - 7 Core Principles (I through VII)
    - Architecture Boundaries
    - Development Workflow
    - Governance

  Removed sections: N/A (initial version)

  Templates requiring updates:
    - .specify/templates/plan-template.md ✅ (Constitution Check section
      already references constitution gates dynamically)
    - .specify/templates/spec-template.md ✅ (no constitution-specific
      constraints to propagate; user stories and requirements are generic)
    - .specify/templates/tasks-template.md ✅ (task phases and test-first
      ordering align with Principle III)

  Follow-up TODOs: None
  ==================================================
-->

# Uno Platform Core Constitution

## Core Principles

### I. WinUI API Fidelity

All public API surfaces MUST align with the WinUI 3 / WinAppSDK API
contracts. Deviations from WinUI behavior are permitted ONLY when:

- The target platform has a fundamental technical constraint that
  prevents faithful reproduction (documented in code comments).
- No API for such functionality exists in WinUI and it is specifically targeted towards non-WinUI platforms.
- EXCEPTION: Uno.UI.Toolkit can contain custom APIs that are available across all targets (including WinUI).

**Rationale**: Uno Platform's value proposition is write-once WinUI code.
API drift erodes developer trust and breaks existing WinUI applications
ported to Uno.

### II. Cross-Platform Parity

Every feature MUST work consistently across all supported target
platforms: WebAssembly, iOS, Android, Windows (Win32), Linux, macOS,
and native Windows (WinUI). Uno Platform supports Skia and native rendering, where Skia is the primary, and native is legacy - new features may not work 100% on native, but they should not break existing Uno Platform apps using native rendering. Platform-specific behavior MUST be:

- Isolated in files using the platform suffix convention (`.skia.cs`,
  `.wasm.cs`, `.Android.cs`, `.iOS.cs`, `.UIKit.cs`).
- Covered by runtime tests that execute on at least the Skia target.
- Documented when parity is intentionally incomplete.

**Rationale**: Cross-platform consistency is the core promise. Gaps that
are invisible to contributors become user-facing regressions.

### III. Test-First Quality Gates

All changes MUST include runtime tests in
`src/Uno.UI.RuntimeTests/`. The expected
workflow is:

1. Write tests that describe the correct behavior.
2. Verify the tests fail (red).
3. Implement the feature or fix.
4. Verify the tests pass (green).
5. Refactor while keeping tests green.

Build and test MUST pass before any PR is considered mergeable:
- `dotnet build` with the appropriate solution filter.
- Runtime tests via Skia desktop execution (see AGENTS.md for instructions).

**Rationale**: The framework has 141+ controls across 6+ platforms.
Untested changes create compounding risk that is expensive to diagnose
after the fact.

### IV. Performance and Resource Discipline

Changes to hot paths (rendering, layout, input, property system) MUST
NOT introduce measurable regressions in:

- Startup time
- Frame rendering latency
- Memory allocation rate
- Binary size

Contributors SHOULD provide before/after measurements for changes
affecting these areas. Allocations in per-frame code paths MUST be
justified.

**Rationale**: Uno targets resource-constrained environments (mobile,
WebAssembly). Small regressions compound across the control library
and are difficult to attribute after merge.

### V. Generated Code Boundaries

Files under `Generated/` folders are produced by automated tooling
and MUST NOT contain functionality. They may only be edited to change the `#if` directives to mark APIs as implemented. To implement a generated stub:

1. Copy the stub to a non-generated location.
2. Remove the implemented platforms from the `[Uno.NotImplemented]`
   attribute and adjust `#if` accordingly.
3. Provide the platform-specific implementation using file suffixes.

**Rationale**: Manual edits to generated files are silently overwritten,
causing confusion and lost work. Enforcing this boundary eliminates an
entire class of contributor mistakes.

### VI. Backward Compatibility

Breaking changes MUST:
- Use a `feat!` or `fix!` conventional commit.
- Start commit message with `BREAKING CHANGE:`.s
- Include a migration guide or release note entry.
- Be discussed and approved before implementation.

**Rationale**: Downstream applications depend on stable APIs. Undocumented
breaks force upgrade friction that damages ecosystem adoption.

### VII. WinUI Implementation Alignment

When implementing features or fixing bugs that correspond to WinUI
behavior, contributors SHOULD consult the original WinUI C++ sources
to understand the reference implementation. Specifically:

- Ask the user or team lead for the location of WinUI C++ sources.
- Study how WinUI implements the relevant logic before writing the
  Uno equivalent.
- Document any intentional deviations from WinUI behavior with
  rationale in code comments.

This principle is RECOMMENDED (not mandatory) but becomes essential
for complex control behavior, edge-case handling, and state machine
logic where WinUI's implementation details matter.

**Rationale**: Uno aims for behavioral parity with WinUI. Guessing at
behavior instead of reading the source leads to subtle differences
that surface as bugs in production applications.

## Architecture Boundaries

The Uno Platform uses a layered platform abstraction that MUST be
respected in all contributions:

**Rendering Engines**:
- **Skia**: Cross-platform rendering (Desktop Win32, macOS, Linux,
  Skia-based Android/iOS).
- **Native**: Platform-native controls (UIKit, Android Views, DOM
  elements for WebAssembly).

**Platform Base Class Rules**:
- Android native: inherits `ViewGroup` -> `UnoViewGroup` (Java) ->
  `BindableView` -> `UIElement`.
- iOS native: inherits `UIView` -> `BindableUIView` -> `UIElement`.
- WebAssembly native: UIElements map to DOM elements.
- Skia: uses rendering tree.
- `DependencyObject` is an **interface** on Android/iOS (not a base
  class). Source generators provide the implementation.

**Platform-Specific Code Isolation**:
- MUST use file suffix convention (`.Android.cs`, `.iOS.cs`,
  `.skia.cs`, `.wasm.cs`, `.reference.cs`).
- Partial classes are the primary mechanism for platform divergence.
- `ApiExtensibility` pattern for runtime Skia platform specialization.
- `#if` conditional compilation directives in shared code SHOULD be
  avoided in favor of platform-suffixed files.

**Project Organization**:
- Most libraries have 5 variants: Reference, Skia, WebAssembly,
  NetCoreMobile, Tests.
- XAML compilation is handled by source generators
  (`XamlFileGenerator`), not `.xbf` binary format.

## Development Workflow

**Build Validation** (run after every change):

1. `dotnet build` with the matching solution filter and
   `crosstargeting_override.props`.
2. `dotnet test` for affected unit test projects.
3. Runtime tests via headless Skia for UI changes.
4. SamplesApp verification for visual changes.

**SamplesApp Registration** (CRITICAL for new samples):
- All XAML samples MUST be registered in
  `src/SamplesApp/UITests.Shared/UITests.Shared.projitems`.
- Both the XAML page and code-behind MUST be added.
- The `[Sample]` attribute MUST be applied to the code-behind class.

**Conventional Commits** (MANDATORY):
- Format: `<type>[optional scope]: <description>`
- Types: `fix`, `feat`, `docs`, `test`, `chore`, `feat!`
- Imperative mood, under 50 characters.
- Reference issues where applicable.

**Build Safety**:
- Set timeouts to 15+ minutes.
- Favor Skia Desktop solution filter for fastest iteration.
- Delete `obj/`, `bin/`, and restore when encountering stale asset
  errors.

## Governance

This constitution is the authoritative reference for development
principles in the Uno Platform Core repository. It supersedes
conflicting guidance in other documents.

**Amendments**:
- Any change to this constitution MUST be submitted as a pull request
  with documented rationale.
- The PR MUST include a version bump following semantic versioning:
  - MAJOR: Principle removal or redefinition.
  - MINOR: New principle or materially expanded guidance.
  - PATCH: Clarifications, wording, typo fixes.
- At least one core maintainer MUST approve the amendment PR.

**Compliance**:
- All PRs and code reviews MUST verify compliance with these
  principles.
- Complexity or deviations MUST be justified in the PR description
  and tracked in the plan's Complexity Tracking table.

**Runtime Guidance**:
- `AGENTS.md` serves as the authoritative runtime development guide
  for AI agents and developers. It operationalizes these principles
  with concrete commands, paths, and patterns.

**Version**: 1.0.0 | **Ratified**: 2026-02-09 | **Last Amended**: 2026-02-09
