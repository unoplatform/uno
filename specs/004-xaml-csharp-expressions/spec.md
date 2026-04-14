# Feature Specification: XAML C# Expressions

**Feature Branch**: `dev/mazi/xamlexpressions`
**Created**: 2026-04-14
**Status**: Draft
**Input**: User description: "Spec out support for XAML C# expressions analogous to what MAUI supports - take heavy inspiration from their spec at https://github.com/dotnet/maui/blob/main/docs/specs/XamlCSharpExpressions.md. In addition make sure that the implementation uses TDD and it has heavy test coverage. This feature will only be supported on non-WinAppSDK targets of Uno Platform."

## Summary

This feature lets Uno Platform XAML authors write C# expressions directly inside XAML attribute values (for example `Text="{FirstName + ' ' + LastName}"` or `Click="{(s, e) => Counter++}"`) instead of requiring code-behind helpers, converters, or hand-written bindings for common cases. Expressions are resolved at build time by the XAML source generator against the page's declared data type, local members, and static types, producing equivalent bindings or event handlers automatically. The feature targets the Uno Platform source generator path only and is **explicitly scoped out on WinAppSDK builds** of the same codebase so that multi-targeted projects still compile unchanged on Windows.

## Clarifications

### Session 2026-04-14

- Q: How is the feature activated in a project? → A: Opt-in via an MSBuild project property, default off; existing XAML behavior is preserved byte-for-byte unless the author opts in.
- Q: How should the new syntax relate to the existing `x:Bind` implementation? → A: Deferred to the planning phase; decision will be made after investigating the MAUI implementation. Current leaning: lower inline expressions to synthesized `x:Bind` (and synthesized event handlers for lambdas) and reuse the compiled-binding back-end end-to-end (Option A), but this is not yet committed.
- Q: What qualifies as a "simple path" for two-way inference? → A: Arbitrary-depth dotted paths, matching the existing Uno `x:Bind` two-way inference rules (every hop readable/writable, intermediate instances notification-capable).
- Q: What prefix should the feature's diagnostic codes use? → A: `UNO` followed by 4 digits (e.g., `UNO2001`); generic Uno prefix, no XAML marker.
- Q: How does the generated binding refresh when some sources in the expression don't notify? → A: Refresh set is the union of notification-capable sources only; non-notifying references (statics, non-INPC hops, `DateTime.Now`) are captured at load time and re-read on each refresh fired by a notifying source, but do not themselves trigger refreshes.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Inline property and expression bindings without code-behind (Priority: P1)

A XAML author wants to render computed or derived view-model state (full name, formatted price, visibility based on a boolean) without authoring a `Converter`, a helper property on the view model, or a manual `{Binding}` with `Path=`. They type the C# expression directly into the attribute value and the XAML generator produces the equivalent binding, including string interpolation, ternary, null-coalescing, arithmetic, and comparison operators.

**Why this priority**: This is the core reason the feature exists. It removes the most common source of converter/helper boilerplate in MVVM XAML codebases and is the entry point every other story builds on. Without it, the rest of the feature is not usable.

**Independent Test**: Ship only this story and an author can replace `{Binding FirstName, Converter=...}` patterns with inline expressions such as `{FirstName + ' ' + LastName}`, `{IsVip ? 'Gold' : 'Standard'}`, `{Price * Quantity}`, and `{$'{Balance:C2}'}`. Verify by building a sample page with an `x:DataType`, running it on a non-WinAppSDK target, and observing the rendered text updates when the bound properties change.

**Acceptance Scenarios**:

1. **Given** a page with `x:DataType="local:Customer"` and a `Customer` VM exposing `FirstName` and `LastName`, **When** the author writes `Text="{FirstName + ' ' + LastName}"`, **Then** the TextBlock displays the concatenated name and updates when either property raises `PropertyChanged`.
2. **Given** a numeric property `Price` and a method `GetTaxRate()` on the DataType, **When** the author writes `Text="{$'{Price * GetTaxRate():C2}'}"`, **Then** the TextBlock displays the culture-aware formatted total and recomputes when `Price` changes.
3. **Given** a boolean VM property `IsVip`, **When** the author writes `Text="{IsVip ? 'Gold member' : 'Standard'}"`, **Then** the text reflects the current value and toggles when `IsVip` changes.
4. **Given** a nullable property `Nickname`, **When** the author writes `Text="{Nickname ?? 'Anonymous'}"`, **Then** the TextBlock shows the fallback string when `Nickname` is null and the actual value otherwise.

---

### User Story 2 - Inline event handler lambdas (Priority: P1)

A XAML author wants to react to events (for example `Click`, `Tapped`, `TextChanged`) with a short lambda instead of defining a named method in code-behind. They write `Click="{(s, e) => Counter++}"` and the generator emits a compliant event handler wired to the event.

**Why this priority**: Event-handler boilerplate is the second largest source of code-behind noise. Shipping it alongside P1 gives authors a complete "no-code-behind for the easy cases" story, which is the public positioning of the feature.

**Independent Test**: Authors can attach event handlers for standard routed/CLR events using a lambda directly in the XAML attribute. Verify by running the built sample, invoking the event (click, tap, key-press), and observing the VM state change and UI refresh.

**Acceptance Scenarios**:

1. **Given** a Button on a page with a `Counter` property on the DataType, **When** the author writes `Click="{(s, e) => Counter++}"` and the user clicks the button, **Then** `Counter` increments and any bindings that depend on it update.
2. **Given** a TextBox and a VM with a `Search(string)` method, **When** the author writes `TextChanged="{(s, e) => Search(((TextBox)s).Text)}"`, **Then** the method is invoked on each change with the current text.
3. **Given** a lambda that uses an `async` modifier (`{async (s, e) => await ...}`), **When** the project is built, **Then** the author receives a clear, actionable build error identifying the unsupported construct and pointing at the attribute.

---

### User Story 3 - Disambiguation from markup extensions and scoped identifiers (Priority: P2)

A XAML author wants precise control over whether `{Foo}` means "invoke the `FooExtension` markup extension", "bind to the `Foo` property on the DataType", or "call the `Foo` property on the page itself". They use directives (`{= ...}`, `{.Foo}`, `{this.Foo}`, `{prefix:Foo}`) to disambiguate without ambiguity warnings.

**Why this priority**: Without this, expressions silently collide with existing markup extensions in large codebases and break compatibility. It is required for the feature to be safe to ship, but it is not the first thing a user discovers.

**Independent Test**: An author can add a project that contains both a `FooExtension` markup extension and a `Foo` property on a page, write `{= Foo}` and `{local:Foo}` side by side, and see each resolve to the intended target with no warning. Verify by inspecting the built output behavior and the generator diagnostics.

**Acceptance Scenarios**:

1. **Given** both `FooExtension` and a `Foo` property exist in scope, **When** the author writes `{Foo}`, **Then** the generator emits a named warning diagnostic describing the ambiguity and pointing at both candidates.
2. **Given** the same scope, **When** the author writes `{= Foo}`, **Then** the generator resolves `Foo` as a C# expression with no warning.
3. **Given** the same scope, **When** the author writes `{local:Foo}`, **Then** the generator invokes the markup extension with no warning.
4. **Given** a page with a `Title` property and a DataType with a `Title` property, **When** the author writes `{this.Title}` vs `{.Title}`, **Then** each resolves to the page-level and DataType-level member respectively.

---

### User Story 4 - Static type access and method invocation (Priority: P2)

A XAML author wants to call static members (`Math.Max`, `DateTime.Now`, `string.Format`) directly from XAML without registering them as resources or creating helper properties.

**Why this priority**: Common enough to remove significant boilerplate, but an author can still work around it using local wrapper properties, so it is not required for MVP.

**Independent Test**: Build a sample that uses `Text="{Math.Max(A, B)}"` against a DataType exposing `A` and `B`, observe the rendered value, then change `A` or `B` via the VM and observe the refresh.

**Acceptance Scenarios**:

1. **Given** DataType properties `A` and `B`, **When** the author writes `Text="{Math.Max(A, B)}"`, **Then** the TextBlock shows the larger value and refreshes when either changes.
2. **Given** a page with the correct global or file-scoped using for `System`, **When** the author writes `Text="{DateTime.Now.ToString('t')}"`, **Then** the TextBlock renders the current time at load and, since no notifying source is referenced, the expression is one-shot (it will not refresh on its own).
3. **Given** a custom static helper class `Formatting` in the project, **When** the author writes `Text="{local:Formatting.Currency(Price)}"` or an equivalent fully-qualified form, **Then** the generator resolves the static method without error.

---

### User Story 5 - XML-friendly operator aliases and CDATA fallback (Priority: P3)

A XAML author needs to write expressions that would otherwise require escaping XML special characters (`<`, `>`, `&`). They use word-based aliases (`AND`, `OR`, `LT`, `GT`, `LTE`, `GTE`) or wrap the expression in a CDATA section.

**Why this priority**: Necessary for edge cases but only a minority of expressions require it; most authors will use `?:`, `??`, and `+` which do not need escaping.

**Independent Test**: Write a page that uses `{Count GT 0 AND IsEnabled}` and an equivalent CDATA-wrapped form with `>` and `&&`, confirm both compile and produce the same runtime behavior.

**Acceptance Scenarios**:

1. **Given** a VM with `Count` and `IsEnabled`, **When** the author writes `IsHitTestVisible="{Count GT 0 AND IsEnabled}"`, **Then** the control's hit-test visibility matches the boolean result and updates when inputs change.
2. **Given** the same VM, **When** the author writes the attribute value as a CDATA section containing `Count > 0 && IsEnabled`, **Then** the generator accepts it and produces identical behavior.
3. **Given** an alias that is not surrounded by whitespace (e.g., `CountGT0`), **When** parsed, **Then** the generator does NOT treat it as the `GT` alias (alias parsing is whitespace-bounded).

---

### User Story 6 - WinAppSDK compatibility fallback for multi-targeted projects (Priority: P1)

A project that uses Uno to target Windows (via WinAppSDK), WebAssembly, Skia Desktop, iOS, and Android must still build and run on all targets even when XAML C# expressions are used in shared XAML. On WinAppSDK the expression syntax must produce either a hard, actionable build error or an equivalent supported construct — it must never silently change behavior or crash at runtime.

**Why this priority**: Uno's value proposition depends on a single XAML codebase working across targets. Shipping a syntax that breaks the WinAppSDK build without a clear diagnostic would block adoption in any project that multi-targets Windows.

**Independent Test**: Take a sample page that uses the new expression syntax, build it against a WinAppSDK target, and confirm either a clear diagnostic or a documented alternative. Re-build on a non-WinAppSDK target and confirm the expression runs normally.

**Acceptance Scenarios**:

1. **Given** a shared XAML file containing `Text="{FirstName + ' ' + LastName}"`, **When** the project is built with a WinAppSDK target active, **Then** the build surfaces a clear diagnostic that names the feature, the unsupported target, and points at the exact attribute, with documentation on the recommended workaround.
2. **Given** the same XAML file, **When** built with any non-WinAppSDK target (Skia Desktop, WebAssembly, iOS, Android), **Then** the build succeeds and the behavior matches Story 1.
3. **Given** a project that has both WinAppSDK and non-WinAppSDK target frameworks, **When** the author wants the same page to work on all targets, **Then** the documented workaround (for example, a conditional XAML include or a fallback binding) is discoverable from the diagnostic itself.

---

### Edge Cases

- **Ambiguous identifier** between markup extension and member: a named warning diagnostic must fire and the author must be pointed at `{= ...}` or `{prefix:Name}` disambiguation.
- **Identifier exists on both page and DataType**: a named error diagnostic must fire; `{this.Foo}` vs `{.Foo}` are the disambiguation forms.
- **Member not found** on DataType, page, or in-scope static types: a named error diagnostic with the exact attribute, file, and line.
- **Two-way target bound to a compound expression**: a named info diagnostic stating the binding silently degrades to one-way because no setter can be inferred.
- **Expression targets a non-bindable property** (e.g., not a DependencyProperty, no notification): resolves as a one-shot assignment at load with a named info diagnostic.
- **Event lambda with wrong arity or wrong parameter types** for the event signature: error diagnostic showing expected vs actual signature.
- **`async` lambda event handler**: error diagnostic explicitly calling out the unsupported construct.
- **Expression containing a string with embedded double quote**: author must switch to CDATA or use `&quot;`; this is documented and validated.
- **Multi-line or multi-statement body**: error diagnostic; only single expressions are supported.
- **Empty expression** (`{}`, `{   }`): error diagnostic.
- **Nested markup extensions inside an expression**: error diagnostic unless the inner form is already a supported C# literal or identifier.
- **Compiled bindings (`x:Bind`) coexistence**: the feature must not break any existing `x:Bind` expression; ambiguity with `x:Bind` tokens is decided in favor of `x:Bind` when the attribute starts with `x:Bind`.
- **DataType not declared** (`x:DataType` missing): member lookup against the DataType is skipped; only page-level and static resolution occur, and an info diagnostic is emitted the first time per file.
- **Nullable reference types**: expression evaluation under `#nullable enable` must not introduce spurious warnings in generated code.
- **Globalization**: string interpolation with format specifiers must honor the current culture at evaluation time, matching standard binding behavior.

## Requirements *(mandatory)*

### Functional Requirements

**Authoring syntax**

- **FR-001**: The XAML parser MUST accept a C# expression in any XAML attribute value when the value is delimited by `{` and `}` and does not match a registered markup extension by name, or when preceded by the `{= ...}` escape directive.
- **FR-002**: The system MUST support these expression forms: property access chains, method invocation with argument lists, arithmetic operators (`+`, `-`, `*`, `/`, `%`), comparison operators (`==`, `!=`, `<`, `<=`, `>`, `>=`), boolean operators (`&&`, `||`, `!`), the null-coalescing operator (`??`), the conditional ternary (`a ? b : c`), string interpolation (`$'...'`), character and string literals with single quotes, numeric literals, and parenthesized sub-expressions.
- **FR-003**: The system MUST accept event handler lambdas of the form `(sender, args) => <expression>` in event attributes.
- **FR-004**: The system MUST provide XML-friendly aliases for the most-escaped operators: `AND`, `OR`, `LT`, `GT`, `LTE`, `GTE`, case-insensitive, recognized only when surrounded by whitespace.
- **FR-005**: The system MUST accept CDATA sections as an alternative that allows the author to use raw `<`, `>`, `&`, and double-quoted strings without escaping.
- **FR-006**: The system MUST treat single-quoted string literals in expressions as C# strings and translate them verbatim in generated code.

**Resolution and semantics**

- **FR-007**: The system MUST resolve identifiers in this order for simple expressions: (1) registered markup extension, (2) DataType member (properties and methods from `x:DataType`), (3) local page/view member, (4) in-scope static member.
- **FR-008**: The system MUST provide the following disambiguation directives: `{= expr}` to force C# expression resolution, `{.Member}` to force DataType resolution, `{this.Member}` to force page-level resolution, and `{prefix:Name}` to force markup extension invocation.
- **FR-009**: For a simple identifier or a dotted path of arbitrary depth, the system MUST generate a two-way binding when the target property is settable and every hop in the path resolves to a readable/writable member on a notification-capable instance (matching Uno's existing `x:Bind` two-way inference rules); otherwise it MUST generate a one-way binding.
- **FR-010**: For compound expressions (any expression containing operators, method calls, or interpolations), the system MUST generate a one-way binding and MUST emit an info-level diagnostic when the target would otherwise be two-way.
- **FR-011**: For event lambdas, the system MUST generate an event handler that matches the event's delegate signature; the generator must validate the lambda parameter arity and types against that signature.
- **FR-012**: String interpolation with format specifiers (`:C2`, `:N0`, etc.) MUST honor the current culture at evaluation time, matching standard `Binding.StringFormat` behavior.
- **FR-013**: The system MUST re-evaluate compound expressions whenever any referenced source property raises a change notification. The refresh set equals the union of notification-capable sources appearing anywhere in the expression. Non-notifying references (static members, non-INPC instances, fields) MUST be captured at load time and re-read on every refresh triggered by a notifying source, but MUST NOT themselves trigger a refresh; if an expression references only non-notifying sources, the binding is one-shot.

**Platform scope**

- **FR-013a**: The feature MUST be opt-in at the project level via a dedicated MSBuild property, default off. When the property is not set or set to false, the XAML generator MUST behave exactly as it did before this feature shipped for every attribute value in the project (no silent reinterpretation of `{Identifier}` as a C# expression).
- **FR-013b**: When the opt-in property is on, the resolution order in FR-007 and the disambiguation directives in FR-008 apply; when it is off, those rules MUST NOT fire and `{= ...}` and other new directives MUST be rejected with a clear diagnostic pointing at the opt-in switch.
- **FR-014**: The feature MUST function on all non-WinAppSDK Uno targets: Skia Desktop (Win32, macOS, Linux, Skia Android, Skia iOS), WebAssembly, native iOS, native Android, and tvOS.
- **FR-015**: When the active target is WinAppSDK, the build MUST surface a clear, named diagnostic that identifies the feature, the unsupported target, and the exact source location. The build MUST NOT silently strip, rewrite, or ignore the expression.
- **FR-016**: The feature MUST NOT change behavior for any XAML attribute that does not use the new syntax. All existing markup extensions (`{Binding}`, `{StaticResource}`, `{ThemeResource}`, `{x:Bind}`, `{TemplateBinding}`, and user-defined extensions) MUST continue to work identically.

**Diagnostics**

- **FR-017**: The system MUST emit a named warning when an identifier could resolve to either a registered markup extension or a member in scope.
- **FR-018**: The system MUST emit a named error when an identifier matches members on both the page and the DataType with no disambiguation directive.
- **FR-019**: The system MUST emit a named error when an identifier cannot be resolved anywhere in the resolution chain.
- **FR-020**: The system MUST emit a named error when an `async` lambda is used in an event attribute.
- **FR-021**: The system MUST emit a named error when a multi-statement block, control-flow statement, or empty expression appears in an attribute value.
- **FR-022**: Every diagnostic MUST include file path, line, column, and a short message that names the offending identifier or construct.
- **FR-022a**: Every diagnostic introduced by this feature MUST use the code format `UNO` followed by 4 digits (for example `UNO2001`). Specific code numbers are assigned during planning and MUST remain stable across versions once published.

**Testing discipline (TDD)**

- **FR-023**: Development MUST be test-driven: every functional requirement (FR-001 through FR-022) MUST have at least one failing test authored before the corresponding implementation change is merged, and that test must appear in the same pull request as the implementation.
- **FR-024**: The test suite MUST include unit tests for the expression parser, unit tests for the resolution/binding generator, and runtime tests that exercise the generated bindings and event handlers on at least Skia Desktop and WebAssembly.
- **FR-025**: The test suite MUST include negative/diagnostic tests asserting each named diagnostic fires with the correct code, severity, and source location for a minimal reproducing XAML snippet.
- **FR-026**: The feature MUST reach at least 90% line coverage and 85% branch coverage across the parser and generator projects it introduces or modifies; coverage MUST be measured and reported in CI.

### Key Entities

- **Expression**: the C# source text inside a `{ ... }` attribute value (or a CDATA section on an attribute) that the generator parses; has a kind (simple identifier, dotted path, compound expression, event lambda), a resolved target, and a set of referenced source members.
- **Resolution scope**: the ordered set of symbol sources the generator walks (markup extensions in scope, DataType members, page/view members, in-scope static types) when binding identifiers in an expression.
- **Generated binding**: the compile-time artifact that backs a simple or compound expression; carries its mode (one-way / two-way), its refresh set (properties that trigger re-evaluation), and its target property.
- **Generated event handler**: the compile-time artifact that backs an event lambda; carries the event's delegate signature and the synthesized method body.
- **Diagnostic**: a build-time report (code, severity, message, source location) produced by the parser or resolver; each named diagnostic in this spec must be stable across versions.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A XAML author can replace a common "converter or VM-helper-property" pattern (full name, formatted price, visibility from boolean) with a single inline expression and see the page render identically on the first build, with no code-behind changes, in under two minutes of edit time.
- **SC-002**: On a reference sample page using each supported expression form (property access, method call, ternary, null-coalescing, string interpolation, event lambda, static member), all scenarios render correctly and update on source-property change across Skia Desktop, WebAssembly, iOS, and Android.
- **SC-003**: Every diagnostic named in this specification (FR-017 through FR-021) fires with the correct code, severity, and source location for a minimal reproducing XAML snippet, and no other diagnostics fire on valid inputs.
- **SC-004**: Building a shared XAML file that uses the new syntax on a WinAppSDK target surfaces a clear, named diagnostic naming the feature and pointing at the exact attribute; the same file builds and runs on every non-WinAppSDK target.
- **SC-005**: Code coverage on the parser and generator projects reaches at least 90% line and 85% branch, tracked in CI, and does not regress on subsequent PRs.
- **SC-006**: Adding the feature does not increase cold build time of the reference SamplesApp by more than 5% and does not increase generated XAML output size by more than 10% on a repository-representative XAML corpus.
- **SC-007**: Runtime tests exercising the generated bindings and event handlers pass on Skia Desktop and WebAssembly in CI, with at least one test per acceptance scenario in User Stories 1–6.
- **SC-008**: No existing passing runtime test regresses as a result of enabling this feature; the baseline runtime-test pass rate on the target platforms is preserved within noise.

## Assumptions

- Expression resolution integrates with the existing Uno XAML source generator (`Uno.UI.SourceGenerators`) and runs alongside the compiled-bindings/`x:Bind` infrastructure, not as a runtime XAML loader feature.
- Two-way inference for simple paths uses the same rules the Uno compiled-binding generator already applies for `{x:Bind Foo, Mode=TwoWay}`-equivalent paths.
- `x:DataType` is the authoritative source of truth for DataType member resolution; without it, DataType-based resolution is disabled but the feature still works against page members and static types.
- Single-quoted strings in expressions are idiomatic (XAML attributes use double quotes); the generator rewrites them to double-quoted C# at code-emit time.
- Operator aliases (`AND`, `OR`, `LT`, ...) are whitespace-delimited tokens and must not be embedded inside identifiers.
- Diagnostic codes use the `UNO` + 4-digit format (see FR-022a); concrete numbers are allocated during planning and treated as a stable contract thereafter.
- WinAppSDK is out of scope by product decision for this release; a supported WinAppSDK implementation is deferred to a later spec.
- Test coverage targets (90% line / 85% branch) are measured by the existing Uno coverage tooling used in CI.
