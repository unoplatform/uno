# Phase 1 Data Model: XAML C# Expressions

This document captures the compile-time entities the feature introduces. There are no runtime entities — the feature is entirely a source-generator transformation that lowers to existing Uno compiled-binding and event-handler infrastructure.

## Entities

### 1. `XamlExpressionAttributeValue`

Represents a XAML attribute value that the classifier has identified as carrying a C# expression rather than a literal or a conventional markup extension.

| Field | Type | Description |
|-------|------|-------------|
| `RawText` | `string` | The verbatim attribute value including outer `{` `}` (or CDATA wrapper) |
| `Kind` | `enum ExpressionKind` | `SimpleIdentifier`, `DottedPath`, `Compound`, `EventLambda`, `Explicit`, `ForcedThis`, `ForcedDataType`, `MarkupExtension` |
| `InnerCSharp` | `string` | The C# source text after stripping directive prefixes, alias replacement, and quote transformation |
| `SourceSpan` | `LinePositionSpan` | File + start/end location for diagnostics |
| `IsCData` | `bool` | True when the original form was a CDATA section |

**Validation rules**:
- `InnerCSharp` must be non-empty and non-whitespace after transformation (else `UNO2007`).
- `InnerCSharp` must not contain `;` outside of a string literal (else `UNO2006`).
- For `Kind = EventLambda`, `RawText` must match `^\s*\((?<s>\w+)\s*,\s*(?<e>\w+)\s*\)\s*=>\s*.+$` after brace stripping; if prefixed with `async`, emit `UNO2005`.

**State transitions**: immutable once produced by the classifier.

---

### 2. `ExpressionAst`

The Roslyn-produced syntax tree rooted at the user's expression. We do not wrap Roslyn types — `ExpressionSyntax` and `SyntaxTree` are used directly. The following **derived** views are cached per-attribute:

| View | Shape | Purpose |
|------|-------|---------|
| `RootIdentifier` | `string?` | The leftmost identifier of a member-access chain, if the expression is a single chain |
| `IsPureDottedPath` | `bool` | True when every node is `IdentifierNameSyntax` or `MemberAccessExpressionSyntax` (used by two-way inference) |
| `IsSettable` | `bool` | `IsPureDottedPath && leaf member is writable` (two-way inference target) |
| `ContainsMethodInvocation` | `bool` | Used for the "only non-notifying" heuristic |
| `ReferencedIdentifiers` | `IReadOnlyList<IdentifierReference>` | Every `IdentifierNameSyntax` with its context position |

---

### 3. `IdentifierReference`

| Field | Type |
|-------|------|
| `Name` | `string` |
| `SyntaxNode` | `IdentifierNameSyntax` |
| `ParentChain` | `IReadOnlyList<string>` — for `A.B.C`, reference for `C` has `ParentChain = ["A", "B"]` |
| `IsMethodCallTarget` | `bool` |

---

### 4. `ResolutionScope`

The ordered symbol sources the resolver walks (matches spec FR-007).

| Field | Type | Description |
|-------|------|-------------|
| `PageType` | `INamedTypeSymbol` | The code-behind type of the current XAML page or resource |
| `DataType` | `INamedTypeSymbol?` | The `x:DataType` in scope, if any |
| `KnownMarkupExtensions` | `IReadOnlyDictionary<string, INamedTypeSymbol>` | Registered markup extension names (e.g. `FooExtension` → symbol) |
| `GlobalUsings` | `IReadOnlyList<string>` | Global and `using static` imports in the compilation |
| `Compilation` | `Compilation` | Roslyn compilation (for symbol lookups) |

**Resolution decision function** `ResolutionScope.Resolve(identifier) → ResolutionResult` (decision table in `research.md` §3.1).

---

### 5. `ResolutionResult`

| Field | Type | Description |
|-------|------|-------------|
| `Location` | `enum MemberLocation` | `This`, `DataType`, `Both`, `Neither`, `ForcedThis`, `ForcedDataType`, `StaticType`, `MarkupExtension` |
| `Symbol` | `ISymbol?` | The resolved symbol (null for `MarkupExtension`) |
| `Diagnostic` | `DiagnosticDescriptor?` | When a warning/error should be emitted for this result |

---

### 6. `ExpressionAnalysisResult`

Output of `ExpressionAnalyzer.Analyze` — the bridge between parse/resolve and codegen.

| Field | Type | Description |
|-------|------|-------------|
| `TransformedCSharp` | `string` | The expression with DataType identifiers prefixed (`__source.X`) and page-level identifiers replaced with captures (`__capture_X`) |
| `Handlers` | `IReadOnlyList<BindingHandler>` | INPC subscription tuples for the refresh set |
| `Captures` | `IReadOnlyList<LocalCapture>` | Page-level references captured at load time |
| `LeafPropertyType` | `ITypeSymbol` | Used as `TProperty` in the emitted binding |
| `IsOneShot` | `bool` | True when `Handlers` is empty (expression references only non-notifying sources) |
| `IsSettable` | `bool` | True when the expression is a pure settable dotted path |

---

### 7. `BindingHandler`

| Field | Type | Description |
|-------|------|-------------|
| `Accessor` | `string` | Lambda body producing the intermediate instance (e.g. `__source => __source.User.Address`) |
| `PropertyName` | `string` | The INPC-visible property on that instance (e.g. `"City"`) |

---

### 8. `LocalCapture`

| Field | Type | Description |
|-------|------|-------------|
| `OriginalIdentifier` | `string` | e.g. `TaxRate` or `this.TaxRate` |
| `CaptureVariableName` | `string` | `__capture_TaxRate` |
| `CaptureInitializer` | `string` | C# source for the load-time initializer (e.g. `this.TaxRate`) |
| `Type` | `ITypeSymbol` | Captured value type |

---

### 9. `LoweredExpression`

The final codegen IR consumed by the existing x:Bind emitter (simple paths) or by the new inline event-handler emitter (lambdas).

| Variant | Shape |
|---------|-------|
| `SimplePathBinding` | `{ Path: string, Mode: OneWay\|TwoWay, DataContextSource: This\|DataType }` |
| `CompoundBinding` | `{ HelperMethodName: string, HelperMethodBody: string, Handlers: BindingHandler[], LeafType: ITypeSymbol, Captures: LocalCapture[] }` |
| `EventHandler` | `{ HandlerMethodName: string, HandlerMethodBody: string, EventDelegateType: ITypeSymbol }` |

---

### 10. `Diagnostic` (UNO2xxx)

Produced by the generator at any phase; shape is Roslyn's standard `Diagnostic` with our registered `DiagnosticDescriptor`s (see `contracts/diagnostics.md` for the table).

**Every diagnostic carries**:
- Code (e.g. `UNO2001`)
- Severity
- Source location (file + line + column)
- Named identifier or construct in the message

---

## Relationships

```
XamlObjectDefinition (existing)
    │
    └── XamlMemberDefinition (existing — represents an attribute)
            │
            └── XamlExpressionAttributeValue  [produced by CSharpExpressionClassifier]
                    │
                    ├── ExpressionAst         [produced by CSharpExpressionParser via Roslyn]
                    │
                    ├── ResolutionScope       [derived from the parent XamlFileDefinition + Compilation]
                    │
                    ├── ExpressionAnalysisResult  [produced by ExpressionAnalyzer]
                    │       │
                    │       ├── Handlers → BindingHandler[]
                    │       └── Captures → LocalCapture[]
                    │
                    └── LoweredExpression     [produced by ExpressionLowering]
                            │
                            └── [consumed by existing XamlFileGenerator emitters]
```

## Lifecycle (per XAML file)

1. **Classify** each `XamlMemberDefinition.Value` — produce `XamlExpressionAttributeValue` for those identified as expressions; skip others (literal or conventional markup extension).
2. **Parse** — feed `InnerCSharp` to Roslyn; cache `ExpressionAst`.
3. **Resolve** — build `ResolutionScope` once per page, then call `Resolve(identifier)` for each `RootIdentifier` (bare-identifier case) or per-chain root (dotted-path case).
4. **Analyze** — walk the AST, tagging each identifier, computing `TransformedCSharp`, `Handlers`, `Captures`.
5. **Lower** — produce `LoweredExpression` of the appropriate variant.
6. **Emit** — hand off to the existing `XBindExpressionParser.Rewrite` (for bindings) or to the new inline event-handler emitter (for lambdas).
7. **Diagnostics** — any phase may report; reporting never short-circuits later phases except on unrecoverable parse errors.

## Invariants

- `XamlExpressionAttributeValue.Kind` is stable — later phases do not mutate it.
- `ExpressionAnalysisResult.TransformedCSharp` is a syntactically valid C# expression (otherwise the generator emits `UNO2006` / `UNO2007` and skips lowering).
- `BindingHandler.PropertyName` is always `INotifyPropertyChanged`-raisable on the type of `Accessor`'s return (validated during Analyze; failure surfaces `UNO2010` or downgrades to one-shot).
- `LocalCapture.CaptureInitializer` is read once at load time and never re-executed.
- The generator output when `UnoXamlCSharpExpressionsEnabled=false` is byte-identical to the pre-feature output for every existing XAML input (validated by `Given_OptInBehavior.cs`).
