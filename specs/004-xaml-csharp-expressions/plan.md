# Implementation Plan: XAML C# Expressions

**Branch**: `dev/mazi/xamlexpressions` | **Date**: 2026-04-14 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/004-xaml-csharp-expressions/spec.md`

## Summary

Add opt-in support for inline C# expressions inside XAML attribute values (e.g. `Text="{FirstName + ' ' + LastName}"`, `Click="{(s,e) => Counter++}"`, `{= expr}`, `{.Member}`, `{this.Member}`) to the Uno Platform XAML source generator. The feature **reuses Uno's existing compiled-bindings (`x:Bind`) back-end**: simple paths lower to synthesized compiled bindings that honor Uno's two-way inference rules; compound expressions lower to one-way compiled bindings whose refresh set is the union of notification-capable identifiers; event lambdas lower to synthesized handler methods bound via the existing inline-event machinery. The feature is **Uno-only** — WinAppSDK builds surface `UNO2099` and never silently reinterpret XAML. Implementation follows a classify → rewrite → lower pipeline that mirrors MAUI's `ExpandMarkupsVisitor` / `SetPropertyHelpers` / `ExpressionAnalyzer` (PR [dotnet/maui#33693](https://github.com/dotnet/maui/pull/33693)) but emits Uno's `Binding`/compiled-binding shapes instead of MAUI's `TypedBinding`. Test-first discipline is enforced: every functional requirement has a failing test before implementation, and the feature ships with parser unit tests, resolver unit tests, generator snapshot tests, diagnostic tests, and runtime tests on Skia Desktop and WebAssembly.

## Technical Context

**Language/Version**: C# (net10.0 multi-target), .NET Roslyn source generators (Microsoft.CodeAnalysis 4.x via `Uno.UI.SourceGenerators`)
**Primary Dependencies**: `Uno.UI.SourceGenerators` (existing XAML + x:Bind generator), `Uno.Xaml` parser (already surfaces `{}`-escaped and CDATA forms via `ValueNode`), `Microsoft.CodeAnalysis.CSharp` (used only for parsing the C# sub-expression into a `SyntaxTree` during symbol/handler analysis)
**Storage**: N/A (compile-time feature; no runtime state beyond what `x:Bind` already emits)
**Testing**: MSTest for parser/resolver/generator unit tests in `Uno.UI.SourceGenerators.Tests` (same harness the XAML generator already uses); runtime tests in `Uno.UI.RuntimeTests` on Skia Desktop and WebAssembly (MSTest + Uno test helpers)
**Target Platform**: Uno Platform non-WinAppSDK heads — Skia Desktop (Win32/macOS/Linux, Skia Android, Skia iOS), WebAssembly, native iOS, native Android, native tvOS. **WinAppSDK is out of scope** (hard diagnostic, no codegen)
**Project Type**: Single project — a new subsystem inside `src/SourceGenerators/Uno.UI.SourceGenerators/` plus parallel tests in `src/SourceGenerators/Uno.UI.SourceGenerators.Tests/` and runtime tests in `src/Uno.UI.RuntimeTests/`
**Performance Goals**: Cold build of `SamplesApp` must not regress by more than 5% when the feature is enabled; parser must handle attribute values in O(n) with a single pass (no backtracking) before handing off to Roslyn; generated output size growth on a representative corpus ≤ 10%
**Constraints**: Must not alter behavior of any attribute value when opt-in flag is off (byte-for-byte generator output stability); must not conflict with existing markup extensions, `{x:Bind}`, or custom `MarkupExtension` types; must emit stable `UNO2xxx` diagnostic codes; must not introduce any per-frame allocation in generated bindings beyond what `x:Bind` already allocates
**Scale/Scope**: Target support for the expression grammar matrix in spec (FR-001..FR-022): property/member access, method invocation, arithmetic/comparison/boolean/null-coalescing/ternary operators, string interpolation with format specifiers, single-quoted strings, operator aliases (`AND`/`OR`/`LT`/`GT`/`LTE`/`GTE`), event lambdas of arity 2, CDATA sections, disambiguation directives (`{=}`, `{.M}`, `{this.M}`, `{prefix:Name}`). ~2500-3500 LOC across parser + resolver + analyzer + codegen + diagnostics, ≥ 100 new tests.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Gate | Status |
|-----------|------|--------|
| I. WinUI API Fidelity | No public API surface is added. The feature is a compile-time syntactic sugar that lowers to existing `Binding` / compiled-binding shapes that already have WinUI-equivalent runtime semantics. WinAppSDK builds are explicitly rejected with `UNO2099` so no WinUI contract is altered for Windows consumers. | ✅ PASS |
| II. Cross-Platform Parity | Generated code path is platform-agnostic (it lowers to the same compiled bindings that run identically on Skia/Wasm/iOS/Android). Parity is enforced by runtime tests on **both Skia Desktop and WebAssembly** (spec SC-007). Native iOS/Android ride the same generated output. | ✅ PASS |
| III. Test-First Quality Gates | Enforced by spec FR-023..FR-026 and reinforced in this plan: every functional requirement gets a failing unit test **in the same PR** as the implementation; runtime tests in `src/Uno.UI.RuntimeTests/` cover every User Story acceptance scenario; coverage gate ≥ 90% line / 85% branch. | ✅ PASS |
| IV. Performance & Resource Discipline | Parser is single-pass, allocation-light. Generated compiled bindings reuse the `x:Bind` back-end — no new per-frame allocations. Cold-build regression budget ≤ 5% (SC-006) is enforced via build-time measurement on SamplesApp. | ✅ PASS |
| V. Generated Code Boundaries | No edits to `Generated/` folders. All source-generator changes live under `src/SourceGenerators/Uno.UI.SourceGenerators/` (non-generated). | ✅ PASS |
| VI. Backward Compatibility | Feature is **opt-in** via `UnoXamlCSharpExpressionsEnabled` (spec FR-013a). Default off. When off, the generator produces byte-identical output to the previous version for all existing XAML (validated by a regression test that diff-compares generator output against a golden corpus with the flag off). No breaking changes. | ✅ PASS |
| VII. WinUI Implementation Alignment | There is no WinUI C++ reference for this feature (WinUI does not support inline C# expressions). Instead, this plan documents alignment with **MAUI's reference implementation** in [dotnet/maui#33693](https://github.com/dotnet/maui/pull/33693) (cloned at `D:\Work\maui`) — the only production implementation of this syntax family. Deviations from MAUI are documented in `research.md` with rationale (e.g., we reuse Uno's `x:Bind` back-end rather than MAUI's `TypedBinding`). | ✅ PASS (alternative authoritative source) |

**Gate result: PASS.** No violations. No entries required in Complexity Tracking.

## Project Structure

### Documentation (this feature)

```text
specs/004-xaml-csharp-expressions/
├── plan.md              # This file
├── spec.md              # Feature specification (already authored)
├── research.md          # Phase 0: MAUI implementation investigation + unknowns resolved
├── data-model.md        # Phase 1: Expression AST, Resolution scope, Generated binding types
├── quickstart.md        # Phase 1: Developer onboarding + minimal end-to-end example
├── contracts/
│   ├── expression-grammar.md      # The accepted C# sub-grammar, tokens, aliases, literal rules
│   ├── resolution-algorithm.md    # Identifier resolution order, disambiguation directives
│   ├── diagnostics.md             # UNO2xxx codes (full list, severity, message template, triggers)
│   ├── msbuild-properties.md      # Opt-in property + AnalyzerConfigOptions wiring
│   └── generated-binding-shapes.md # Exact shape of emitted C# for simple paths, compound, events
└── checklists/
    └── requirements.md  # (pre-existing)
```

### Source Code (repository root)

The feature is confined to three source-code regions — no changes to `Uno.UI` core, no changes to `Uno.UWP`, no platform-suffixed files, no new runtime types:

```text
src/
├── SourceGenerators/
│   ├── Uno.UI.SourceGenerators/
│   │   └── XamlGenerator/
│   │       ├── CSharpExpressions/                       # NEW subsystem (parser + resolver + analyzer + codegen)
│   │       │   ├── CSharpExpressionClassifier.cs        # lexical detection: is this attribute a C# expression?
│   │       │   ├── CSharpExpressionTokenizer.cs         # single-pass tokenizer (handles aliases, single-quoted strings, interpolation)
│   │       │   ├── CSharpExpressionParser.cs            # produces Expression AST via Roslyn SyntaxTree (script mode)
│   │       │   ├── MemberResolver.cs                    # this vs DataType vs statics vs markup-extension
│   │       │   ├── XDataTypeResolver.cs                 # walks parent XamlObjectDefinition to find x:DataType
│   │       │   ├── ExpressionAnalyzer.cs                # refresh-set + captures + transformed C# source
│   │       │   ├── ExpressionLowering.cs                # emits compiled-binding shapes (simple/compound/event)
│   │       │   ├── OperatorAliases.cs                   # AND/OR/LT/GT/LTE/GTE replacement
│   │       │   ├── QuoteTransform.cs                    # single-quoted → double-quoted string + char-literal re-detection
│   │       │   └── Diagnostics.cs                       # UNO2000-series DiagnosticDescriptors
│   │       ├── XamlFileGenerator.cs                     # HOOK: classify before markup-extension branch (~line 3228)
│   │       ├── XamlFileGenerator.InlineEvent.cs         # HOOK: accept lambda expressions alongside named methods
│   │       └── Utils/
│   │           └── XBindExpressionParser.cs             # REUSE: path/refresh-set extraction (no change required)
│   └── Uno.UI.SourceGenerators.Tests/
│       └── XamlCodeGeneratorTests/
│           ├── CSharpExpressions/                       # NEW
│           │   ├── Given_ExpressionClassifier.cs        # unit: detection cases
│           │   ├── Given_ExpressionTokenizer.cs         # unit: tokens, aliases, quotes, interpolation
│           │   ├── Given_ExpressionParser.cs            # unit: AST shape for each supported form
│           │   ├── Given_MemberResolver.cs              # unit: resolution order, disambiguation directives
│           │   ├── Given_ExpressionAnalyzer.cs          # unit: refresh-set, captures, transformed source
│           │   ├── Given_ExpressionLowering.cs          # snapshot: emitted C# for each expression kind
│           │   ├── Given_ExpressionDiagnostics.cs       # negative: each UNO2xxx code fires correctly
│           │   └── Given_OptInBehavior.cs               # regression: flag-off == byte-identical legacy output
│           └── ...existing generator tests unchanged
└── Uno.UI.RuntimeTests/
    └── Tests_CSharpExpressions/                         # NEW
        ├── Given_SimpleBinding.cs                       # US1 acceptance scenarios
        ├── Given_CompoundExpression.cs                  # US1 compound + refresh set
        ├── Given_EventLambda.cs                         # US2 acceptance scenarios
        ├── Given_Disambiguation.cs                      # US3 directives
        ├── Given_StaticAccess.cs                        # US4 Math.Max, DateTime.Now, etc.
        ├── Given_AliasesAndCdata.cs                     # US5 AND/OR/LT/GT + CDATA
        ├── Pages/                                       # XAML sample pages backing the runtime tests
        │   ├── SimpleBindingPage.xaml(.cs)
        │   ├── CompoundExpressionPage.xaml(.cs)
        │   ├── EventLambdaPage.xaml(.cs)
        │   ├── DisambiguationPage.xaml(.cs)
        │   ├── StaticAccessPage.xaml(.cs)
        │   └── AliasesAndCdataPage.xaml(.cs)
        └── ViewModels/
            └── ExpressionsTestViewModel.cs              # INotifyPropertyChanged VM shared across runtime tests

# MSBuild wiring
src/Uno.UI.Xaml/build/Uno.UI.Xaml.targets                # ADD: CompilerVisibleProperty UnoXamlCSharpExpressionsEnabled
```

**Structure Decision**: Single-project layout. The feature is a new **subsystem** inside the existing XAML source generator (`src/SourceGenerators/Uno.UI.SourceGenerators/XamlGenerator/CSharpExpressions/`) with parallel unit-test and runtime-test trees. No platform-suffixed files are needed because all outputs of the feature are standard C# that flows through Uno's existing compiled-binding pipeline, which is itself platform-agnostic. WinAppSDK exclusion is enforced at a single point (the existing `PlatformHelper.IsValidPlatform` path, extended with a specific diagnostic for the XCS case when the flag is opted-in on WinAppSDK).

## Complexity Tracking

No Constitution Check violations. No entries.
