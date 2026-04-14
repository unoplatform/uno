# Phase 0 Research: XAML C# Expressions

**Feature**: XAML C# Expressions for Uno Platform (Uno-only, non-WinAppSDK)
**Date**: 2026-04-14
**Primary reference**: .NET MAUI implementation in [dotnet/maui#33693](https://github.com/dotnet/maui/pull/33693) and follow-ups ([#34137](https://github.com/dotnet/maui/pull/34137), [#33994](https://github.com/dotnet/maui/pull/33994)), plus the GA-promotion work on branch `feature/remove-preview-feature-gate-csharp-expressions` (candidate for the upcoming PR referenced by the task). MAUI sources cloned at `D:\Work\maui`. Design doc at `D:\Work\maui\docs\specs\XamlCSharpExpressions.md`.

## Scope of Research

The spec left the following questions explicitly open for planning:

1. **How should the new syntax relate to the existing `x:Bind` implementation?** (spec Clarifications §2026-04-14 Q2 — "deferred to planning")
2. What is the exact C# sub-grammar to accept, and what parsing technology to use (bespoke vs. Roslyn)?
3. How are the identifier resolution order, disambiguation directives, and diagnostics implemented at the code level in MAUI, and which of those choices transplant to Uno's existing generator topology?
4. How is the refresh set computed for compound expressions, and how does that compose with Uno's `x:Bind` INPC infrastructure?
5. What is the exact shape of the emitted C# (for simple paths, compound expressions, and event lambdas)?
6. What diagnostics must be introduced, and what UNO2xxx code numbers should we allocate?
7. How is WinAppSDK excluded, and exactly when does the hard diagnostic fire?
8. What is the test topology — unit tests vs. generator snapshot tests vs. runtime tests — and how do we reach the 90%/85% coverage bar?

Each is resolved below.

---

## 1. Relationship to `x:Bind`

**Decision**: Lower all inline expressions to **synthesized compiled bindings** that re-use Uno's existing `x:Bind` back-end end-to-end (the "Option A" leaning in the spec is committed). Event lambdas lower to synthesized handler methods registered through the existing inline-event machinery in `XamlFileGenerator.GenerateInlineEvent`.

**Rationale**:
- **Parity with spec FR-009 and FR-013**: The spec explicitly requires two-way inference to match Uno's existing `x:Bind` rules and requires the refresh set to be the union of notification-capable sources. Both are already implemented in `XBindExpressionParser.cs`. Re-using it avoids re-implementing a parallel compiled-binding back-end and guarantees behavioral parity.
- **MAUI precedent**: MAUI's implementation (`SetPropertyHelpers.SetExpressionBinding` at `src/Controls/src/SourceGen/SetPropertyHelpers.cs`) emits a `TypedBinding<TSource,TProperty>` — their equivalent of compiled bindings — rather than a parallel runtime path. Uno's `x:Bind` is the same class of back-end; mapping 1:1 is natural.
- **No runtime footprint**: The generated code is indistinguishable from what an author would write with `{x:Bind ...}` today. No new runtime types are introduced.
- **Two-way setter inference**: Uno's `x:Bind` already infers two-way setters for dotted paths whose every hop is writable; MAUI re-implements this in `ExpressionAnalyzer.IsSettable`. We delegate entirely to Uno's existing logic.

**Alternatives considered**:
- **A parallel binding infrastructure** (like a new `Uno.UI.Xaml.Data.ExpressionBinding` class). Rejected because it duplicates ~1,500 LOC of x:Bind back-end (TypedBindingBase, path-based INPC subscription, weak handler management) and creates two code paths that must be kept in sync.
- **Interpreted expressions at runtime** (like Reflection-based DataBinding). Rejected because it defeats the performance goal (spec SC-006), adds AOT hostility on iOS, and diverges from the compile-time-only philosophy of `x:Bind`.

**Consequence**: The lowering phase translates an `ExpressionAst` node into an equivalent internal representation that the **existing x:Bind emitter** accepts. This requires one small extension: the x:Bind emitter currently expects the input to already be a valid C# expression whose identifiers resolve against `DataType` (or the page, for `x:Bind`'s "target-relative" mode). We keep that contract — the Lowering phase rewrites identifiers into the canonical form (prefixing DataType members with the binding-context source, capturing page-level references as closure locals) **before** handing off to `XBindExpressionParser.Rewrite`.

---

## 2. Expression Grammar and Parsing Technology

**Decision**: Use a **two-phase approach** that mirrors MAUI:
1. A **lexical classifier** (regex + small hand-written tokenizer) that decides "is this attribute value a C# expression?" (and separates off disambiguation directives `{=}`, `{.M}`, `{this.M}`).
2. After the classifier accepts the value, a **Roslyn `SyntaxTree` parse in script mode** (`CSharpSyntaxTree.ParseText(code, CSharpParseOptions(kind: SourceCodeKind.Script))`) produces the canonical AST that Resolution and Analysis walk.

**Rationale**:
- **MAUI uses the same split** (`CSharpExpressionHelpers.cs` for classification + `ExpressionAnalyzer.cs` for Roslyn parsing). The split works because "is this a C# expression?" has to be answered *before* we know if it's syntactically valid — classic parser-dispatch problem — and because the grammar we accept is a subset of C# that Roslyn already parses correctly.
- **No bespoke parser**: Owning a C# parser is a maintenance tax we don't want. Roslyn is already a compile-time dependency of the generator.
- **Script mode** permits top-level expressions (no enclosing method) and matches MAUI's choice.

**Supported grammar (accepted forms)**:

| Form | Example |
|------|---------|
| Identifier / member access | `Name`, `User.DisplayName` |
| Null-conditional | `User?.DisplayName` |
| Method invocation | `GetText()`, `Math.Max(A, B)`, `string.Format('x:{0}', y)` |
| Arithmetic operators | `+ - * / %` |
| Comparison operators | `== != < <= > >=` |
| Boolean operators | `&& ||`, unary `!` |
| Null-coalesce | `A ?? B` |
| Ternary | `Cond ? A : B` |
| Parentheses | `(A + B) * C` |
| String literals (single-quoted) | `'hello'`, `'it\'s'` |
| Character literals | inferred contextually (see §2.1 below) |
| Numeric literals | `42`, `3.14`, `1_000`, `0x1F`, `0b101` |
| String interpolation | `$'{Name}: {Count:D3}'` |
| Event lambdas | `(s, e) => Counter++` |
| Operator aliases (whitespace-bounded, case-insensitive) | `AND OR LT GT LTE GTE` |
| CDATA wrappers | `<![CDATA[{Count > 0 && IsEnabled}]]>` |
| Disambiguation directives | `{= expr}`, `{.Member}`, `{this.Member}`, `{prefix:Name}` |

### 2.1 Single-quoted strings and char literals

XAML attributes use `"` as delimiter, so C# string literals inside them use `'`. We transform `'foo'` → `"foo"` byte-for-byte, handling escapes (`\'`, `\"`, `\\`, `\n`, `\t`, `\0`, `\u00XX`). Char-literal disambiguation is **context-sensitive**: a single-character `'x'` passed to a method whose parameter type is `char` is emitted as a C# char literal rather than a string. We port MAUI's `TransformQuotesWithSemantics` approach, which walks `ArgumentSyntax` in the Roslyn tree and checks the `IMethodSymbol` parameter type.

### 2.2 Operator aliases

Whitespace-bounded, case-insensitive replacement happens **outside string literals only**. Order matters: `LTE/GTE` before `LT/GT` to avoid partial replacement. We port `TransformOperatorAliases` from MAUI's `CSharpExpressionHelpers.cs` verbatim (pure function, no dependencies on MAUI types).

### 2.3 Things we explicitly reject at parse time

- Multi-statement bodies (anything with a `;`): emit `UNO2006`.
- Empty / whitespace-only expression (`{}`, `{   }`, `{=}`): emit `UNO2007`.
- `async` lambdas: emit `UNO2005`.
- Lambdas of non-`(s, e)` arity: rely on C# compiler error from the emitted code plus an optional `UNO2008` when we can detect statically (arity mismatch against the event's delegate type).
- Nested markup extensions inside an expression: emit `UNO2009`.

**Alternatives considered**:
- Bespoke recursive-descent parser (reject — see above).
- ANTLR grammar (reject — new build dependency).

---

## 3. Identifier Resolution Order

**Decision**: Adopt MAUI's `MemberResolver.Resolve` algorithm, adapted to Uno's XAML type system. The resolution order spec FR-007 lists is implemented as:

```
classify(expression)
  ├── explicit {= expr}        → parse as C# expression; resolve per §3.1
  ├── {prefix:Name}            → markup extension invocation (existing path)
  ├── {.Member}                → force DataType resolution (no ambiguity)
  ├── {this.Member}            → force page-level resolution (no ambiguity)
  ├── unambiguously C# syntax  → parse as C# expression; resolve per §3.1
  │     (starts with '(', '!', 'new', '$"', "$'", 'typeof(', 'nameof(', 'default(', 'sizeof(',
  │      '.Letter', 'this.', 'BindingContext.', or matches method-call/member-access pattern)
  └── bare identifier          → resolve per §3.1 with markup-extension fallback
```

### 3.1 MemberResolver decisions for a bare root identifier `Foo`

| Resolves on page? | Resolves on DataType? | Is `FooExtension` a markup type? | Outcome |
|-------------------|----------------------|----------------------------------|---------|
| No | No | Yes | Markup extension wins |
| No | No | No | `UNO2003` error (not found) |
| Yes | No | No | Page-level; lower to captured local |
| No | Yes | No | DataType; lower to compiled-binding path |
| Yes | Yes | No | `UNO2002` error (use `this.Foo` or `.Foo`) |
| Yes or No | Yes or No | Yes | `UNO2001` warning (ambiguous); markup extension wins |
| — | — | "is also a global-using type name" | `UNO2004` warning (ambiguous with static type) |

**Rationale**:
- Matches MAUI exactly (MAUIX2007→UNO2001, MAUIX2008→UNO2002, MAUIX2009→UNO2003, MAUIX2011→UNO2004).
- The tie-breaker "markup wins" preserves backward compatibility with every existing XAML codebase.
- `{= Foo}` and `{prefix:Foo}` give the author a deterministic escape hatch.

### 3.2 DataType discovery

Walk the parent chain of the current `XamlObjectDefinition`. Stop at the first `x:DataType` attribute. Respect `DataTemplate` boundaries (MAUI's `XDataTypeResolver` treats a `DataTemplate` parent as shadowing). If no `x:DataType` is found, DataType-based resolution is **disabled** and only page + statics + markup extensions remain — an `UNO2010` info diagnostic fires once per XAML file.

**Alternatives considered**:
- Implicitly infer DataType from the binding-context source expression. Rejected: matches neither MAUI nor `x:Bind` conventions and leads to surprising resolution when authors change the `DataContext`.

---

## 4. Refresh-Set Computation for Compound Expressions

**Decision**: Adopt MAUI's `ExpressionAnalyzer.ExtractHandlers` algorithm. Walk the Roslyn `SyntaxTree` and for every `MemberAccessExpressionSyntax` / `IdentifierNameSyntax` that resolves on the DataType and is reached through a notification-capable chain, emit one **handler tuple** of the form `(accessor: Func<TSource, object>, propertyName: string)` for each hop in the chain. De-duplicate by `(accessorShape, propertyName)` string.

For `User.Address.City`:
```
handlers = [
    (s => s, "User"),
    (s => s.User, "Address"),
    (s => s.User?.Address, "City"),
]
```

**Non-notifying references** (static members, non-INPC instances, fields): captured into local variables at load time, substituted with `__capture_X` in the lowered expression, and do **not** produce handler tuples. Matches spec FR-013.

**One-shot degradation**: If the expression references *only* non-notifying sources (e.g., `Text="{DateTime.Now.ToString('t')}"`), the binding degrades to a one-shot assignment at load. `UNO2011` info fires.

### 4.1 Two-way inference for compound expressions

Only a **pure dotted path** on a settable leaf qualifies for two-way (matches MAUI's `IsSettable` and the existing Uno x:Bind logic). Anything with an operator, method call, or interpolation auto-degrades to one-way; if the target property's default binding mode is TwoWay (a known set e.g. `TextBox.Text`, `Slider.Value`, `DatePicker.Date`, etc.), an `UNO2012` info fires (matches MAUIX2010).

---

## 5. Generated C# Shape

**Decision**: Three distinct shapes, all lowering to existing Uno infrastructure. Exact IR is documented in `contracts/generated-binding-shapes.md`; the summary:

### 5.1 Simple path `{Name}` (DataType-bound, settable)

Lower to a synthesized `{x:Bind Name, Mode=TwoWay}` equivalent. In generator IR: call `XBindExpressionParser.Rewrite("Name", contextSymbol, ...)` and emit via the existing `BuildXBindEvalFunction` emitter. No new runtime types, no new emitter.

### 5.2 Compound `{Price * Quantity}`

Lower to a synthesized `{x:Bind ExpressionMethod(), Mode=OneWay}` with `Properties` = `["Price", "Quantity"]`. The `ExpressionMethod()` is emitted as a private method on the page:
```csharp
private decimal __xcs_Expr_001() => __source.Price * __source.Quantity;
```
…and the x:Bind back-end wires up the INPC subscriptions for `Price` and `Quantity` on the DataType instance.

### 5.3 Event lambda `Click="{(s, e) => Counter++}"`

Lower to a synthesized handler method on the page:
```csharp
private void __xcs_EventHandler_001(object s, RoutedEventArgs e) { Counter++; }
```
…plus the existing `GenerateInlineEvent` wiring that binds `Click += __xcs_EventHandler_001;` at load. `Counter++` identifier-resolves against the page (`this`) or DataType per §3.1, with the same capture/rewrite rules.

**Rationale**: Three shapes, three existing emitters. No new back-end surface.

---

## 6. Diagnostic Codes (UNO2xxx range)

**Decision**: Allocate the `UNO2000` block for this feature. Uno's existing XAML diagnostics are in the `UXAML0xxx` range; to satisfy spec FR-022a's "UNO + 4 digits, generic prefix", we introduce the **`UNO2xxx`** namespace and document it in `contracts/diagnostics.md`. Codes:

| Code | Severity | Name | Trigger | MAUI equivalent |
|------|----------|------|---------|-----------------|
| UNO2001 | Warning | AmbiguousExpressionOrMarkup | Bare `{Foo}` matches both `FooExtension` and a member | MAUIX2007 |
| UNO2002 | Error | AmbiguousMemberExpression | `{Foo}` resolves on both `this` and DataType | MAUIX2008 |
| UNO2003 | Error | MemberNotFound | Simple identifier resolves nowhere | MAUIX2009 |
| UNO2004 | Warning | AmbiguousMemberWithStaticType | Identifier collides with a global-using type | MAUIX2011 |
| UNO2005 | Error | AsyncLambdaNotSupported | Event lambda starts with `async` | MAUIX2013 |
| UNO2006 | Error | MultiStatementExpression | `;` in expression body | (Uno-new) |
| UNO2007 | Error | EmptyExpression | `{}`, `{   }`, `{= }` | (Uno-new) |
| UNO2008 | Error | EventLambdaArityMismatch | Lambda `(a) =>` on a 2-arg event | (Uno-new) |
| UNO2009 | Error | NestedMarkupExtensionInExpression | `{Binding Foo}` inside a C# expression | (Uno-new) |
| UNO2010 | Info | XDataTypeMissing | DataType-relative resolution attempted without `x:DataType` | (Uno-new) |
| UNO2011 | Info | OneShotNonNotifyingSources | Expression references only non-notifying sources | (Uno-new) |
| UNO2012 | Info | ExpressionNotSettableDowngrade | Compound expression on TwoWay-default target | MAUIX2010 |
| UNO2020 | Error | OptInDirectiveWhenDisabled | `{= ...}` used without `UnoXamlCSharpExpressionsEnabled=true` | (Uno-new) |
| UNO2099 | Error | CSharpExpressionsNotSupportedOnWinAppSDK | WinAppSDK target active with opt-in on | (Uno-new, FR-015) |

**Rationale**: The numeric ranges are reserved in 10-code blocks (2000s = feature category for C# expressions). UNO2099 is the WinAppSDK block. We avoid conflicting with Uno's existing `UXAML0xxx` series.

**Stability commitment**: Once published, codes are stable across versions (spec FR-022a).

---

## 7. WinAppSDK Exclusion

**Decision**: Detection lives in the **existing** `PlatformHelper.IsValidPlatform` (which already short-circuits the whole XAML generator for WinAppSDK outputs). For the XCS feature we add a single additional check: if `UnoXamlCSharpExpressionsEnabled=true` **and** the active compilation is WinAppSDK (`OutputKind.WindowsRuntimeApplication` / `WindowsRuntimeMetadata`), emit `UNO2099` for every XAML file that contains a detected expression, pointing at the first offending attribute. The Uno XAML generator already does not run at all on WinAppSDK (that's handled by the WinUI XAML compiler), so the diagnostic path is the only concern.

**Rationale**: Matches spec FR-015 — the build must not silently strip or rewrite on WinAppSDK; it must fail loudly with a clear diagnostic that identifies the feature and the attribute.

**MSBuild opt-in**: `UnoXamlCSharpExpressionsEnabled` (default `false`). Registered as a `CompilerVisibleProperty` in `src/Uno.UI.Xaml/build/Uno.UI.Xaml.targets`. Read via `context.GetMSBuildPropertyValue("UnoXamlCSharpExpressionsEnabled", "false")` inside the generator — this is the established pattern (see `UnoPlatformDefaultSymbolsFontFamily`, `_IsUnoUISolution` in `XamlFileGenerator.cs`).

---

## 8. Test Topology and Coverage Strategy

**Decision**: Four test levels, all TDD — every FR has a failing test **before** the corresponding implementation lands.

### 8.1 Parser / classifier / tokenizer unit tests (MSTest)

Location: `src/SourceGenerators/Uno.UI.SourceGenerators.Tests/XamlCodeGeneratorTests/CSharpExpressions/Given_ExpressionTokenizer.cs`, `Given_ExpressionParser.cs`, `Given_ExpressionClassifier.cs`.

Coverage targets:
- Every disambiguation directive form
- Every operator (arithmetic, comparison, boolean, null-coalesce, ternary)
- Every literal form (numeric, string, interpolation, char inferred)
- Every alias (AND/OR/LT/GT/LTE/GTE, case permutations, whitespace-boundary negative cases)
- CDATA entry/exit
- Negative cases for `UNO2005` (async), `UNO2006` (multi-statement), `UNO2007` (empty), `UNO2009` (nested extension)

Rough count: **≥ 40 tests**.

### 8.2 Resolver unit tests (MSTest)

Location: `Given_MemberResolver.cs`, `Given_XDataTypeResolver.cs`.

Coverage targets: every row of the §3.1 decision table × every directive form. Test fixtures include mock `ITypeSymbol`s with deliberate collisions (page ∩ DataType ∩ static).

Rough count: **≥ 25 tests**.

### 8.3 Generator snapshot tests (MSTest + existing XAML generator test harness)

Location: `Given_ExpressionLowering.cs`, `Given_ExpressionDiagnostics.cs`, `Given_OptInBehavior.cs`.

Pattern: existing Uno XAML generator tests use **raw-string comparison** of generated C#. We follow that pattern — each test feeds a small XAML snippet through the generator and asserts on the emitted C# for the expression region. Diagnostic tests assert on the produced `DiagnosticDescriptor` IDs and locations.

Rough count: **≥ 30 tests** (one per FR × representative inputs + one per UNO2xxx code + regression snapshot for flag-off = byte-identical).

### 8.4 Runtime tests (Uno.UI.RuntimeTests, Skia Desktop + WebAssembly)

Location: `src/Uno.UI.RuntimeTests/Tests_CSharpExpressions/`.

Pattern: standard Uno runtime-test harness (`/runtime-tests` skill for execution). One test per acceptance scenario in User Stories 1–6. Each test instantiates a XAML page that uses the feature, loads it into `WindowHelper.WindowContent`, exercises the behavior (property change, button click, etc.), and asserts on the rendered value or VM state.

Rough count: **≥ 25 tests** (matches spec SC-007 "at least one test per acceptance scenario in US1–6").

### 8.5 Coverage instrumentation

Spec SC-005 requires ≥ 90% line and ≥ 85% branch coverage on the parser + generator projects. We measure via the CI coverage tooling already in place for `Uno.UI.SourceGenerators.Tests`. Coverage is reported per-PR; regression blocks merge.

### 8.6 Performance baseline

Spec SC-006 budget: ≤ 5% cold-build regression on SamplesApp, ≤ 10% generator output size growth. We capture a baseline on a pre-feature commit, re-measure after each significant milestone (parser done, resolver done, codegen done, all-wired), and fail the PR if either budget is exceeded.

---

## 9. MAUI Implementation Map (reference)

For maintainers porting individual pieces, the MAUI source-of-truth files are:

| Concern | MAUI file | Uno target file |
|---------|-----------|-----------------|
| Classification + tokenization + aliases + quotes | `src/Controls/src/SourceGen/CSharpExpressionHelpers.cs` (893 LOC) | `CSharpExpressionClassifier.cs` + `CSharpExpressionTokenizer.cs` + `OperatorAliases.cs` + `QuoteTransform.cs` |
| Symbol resolution | `src/Controls/src/SourceGen/MemberResolver.cs` (251 LOC) | `MemberResolver.cs` |
| x:DataType discovery | `src/Controls/src/SourceGen/XDataTypeResolver.cs` (130 LOC) | `XDataTypeResolver.cs` |
| Handler + capture extraction | `src/Controls/src/SourceGen/ExpressionAnalyzer.cs` (675 LOC) | `ExpressionAnalyzer.cs` |
| Pipeline integration | `src/Controls/src/SourceGen/Visitors/ExpandMarkupsVisitor.cs` + `SetPropertyHelpers.cs` | `XamlFileGenerator.cs` hook (~line 3228) + new `ExpressionLowering.cs` |
| Diagnostics | `src/Controls/src/SourceGen/Descriptors.cs` + `AnalyzerReleases.Unshipped.md` | `Diagnostics.cs` in `CSharpExpressions/` |
| Opt-in wiring | `Microsoft.Maui.Controls.Common.targets` (`EnablePreviewFeatures`) | `Uno.UI.Xaml.targets` (`UnoXamlCSharpExpressionsEnabled`) |
| Unit tests | `src/Controls/tests/SourceGen.UnitTests/CSharpExpressionDiagnosticsTests.cs` + `MemberResolverTests.cs` | `Given_ExpressionDiagnostics.cs` + `Given_MemberResolver.cs` |
| Runtime tests | `src/Controls/tests/Xaml.UnitTests/CSharpExpressions.sgen.xaml(.cs)` + Issue pages | `Tests_CSharpExpressions/` under `Uno.UI.RuntimeTests` |

### 9.1 Known MAUI gaps we should close in Uno

- **No diagnostic for wrong-arity event lambdas**: MAUI relies on downstream Roslyn errors. We add `UNO2008` (detectable statically at generator time).
- **No diagnostic for multi-statement / empty expressions**: MAUI relies on Roslyn parse errors. We add `UNO2006`, `UNO2007` for clear messages.
- **No diagnostic for nested markup extensions**: we add `UNO2009`.
- **Hard-coded TwoWay-default set of 16 BPs**: MAUI matches hard-coded; we instead consult each target `DependencyProperty`'s `FrameworkPropertyMetadata.BindsTwoWayByDefault` so the set is self-maintaining.
- **No disambiguation for root-identifier conflicts with static types**: MAUI emits MAUIX2011 but only for `global using` type names. We extend to `using static` imports as well.

### 9.2 MAUI choices we intentionally do NOT port

- **`TypedBinding<TSource,TProperty>`**: Uno has its own compiled-binding back-end (`x:Bind`). We lower to that instead.
- **`IsEscaped` flag on `ValueNode`**: Uno's XAML parser already represents the `{}` literal-escape; we verify this and extend only if needed.
- **Experimental / preview gate (`EnablePreviewFeatures`)**: the MAUI in-flight GA branch removes this. Uno ships with a **named** opt-in (`UnoXamlCSharpExpressionsEnabled`) from day one; no preview-feature coupling.

---

## 10. Open Questions (resolved)

All **NEEDS CLARIFICATION** items from the Technical Context are now answered:

- Expression AST technology → Roslyn `CSharpSyntaxTree` (script mode) (§2).
- Parser/back-end split → classifier (regex) + Roslyn (§2).
- Relation to x:Bind → lower to synthesized x:Bind (§1).
- Diagnostic namespace → `UNO2xxx` (§6).
- Opt-in property name → `UnoXamlCSharpExpressionsEnabled` (§7).
- WinAppSDK behavior → hard `UNO2099` when opt-in is on (§7).
- Test topology → 4 layers, TDD, ≥ 120 total tests (§8).

No outstanding NEEDS CLARIFICATION. Phase 1 can proceed.
