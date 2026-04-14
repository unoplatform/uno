# Contract: Resolution Algorithm

Defines how identifiers in a classified XAML C# expression are mapped to symbols (page members, DataType members, static types, or markup extensions). This is the authoritative algorithm; the parser/analyzer conforms to it.

## Inputs

- `ExpressionAst` — Roslyn `SyntaxTree` of the expression
- `ResolutionScope`:
  - `PageType: INamedTypeSymbol` — the code-behind type
  - `DataType: INamedTypeSymbol?` — value of nearest `x:DataType` up the parent chain (respecting `DataTemplate` boundaries)
  - `KnownMarkupExtensions: Dictionary<string, INamedTypeSymbol>`
  - `GlobalUsings: IReadOnlyList<string>`
  - `Compilation: Compilation`

## Resolution decision (per root identifier)

For each **root identifier** (the leftmost identifier of a chain, or the single identifier of a bare expression), the resolver runs this decision table:

| Directive prefix | Page has member? | DataType has member? | `{Name}Extension` exists? | Root matches a static type? | Result | Diagnostic |
|------------------|------------------|----------------------|---------------------------|-----------------------------|--------|------------|
| `{= ...}` or unambiguous syntax | — | — | — | — | **Parse as expression**, proceed per chain below | none |
| `{this.Name}` | — | — | — | — | `ForcedThis` | none |
| `{.Name}` or `{BindingContext.Name}` | — | — | — | — | `ForcedDataType` | none |
| `{prefix:Name}` | — | — | — | — | **MarkupExtension** | none |
| bare `{Name}` | No | No | Yes | — | MarkupExtension | none |
| bare `{Name}` | No | No | No | — | — | **UNO2003** error |
| bare `{Name}` | Yes | No | No | No | `This` | none |
| bare `{Name}` | Yes | No | No | Yes | `This` | **UNO2004** warning |
| bare `{Name}` | No | Yes | No | No | `DataType` | none |
| bare `{Name}` | No | Yes | No | Yes | `DataType` | **UNO2004** warning |
| bare `{Name}` | Yes | Yes | No | — | — | **UNO2002** error |
| bare `{Name}` | — | — | Yes (and matches member) | — | MarkupExtension (wins) | **UNO2001** warning |

## DataType discovery

Walk the parent chain of the current `XamlObjectDefinition`:

1. Stop at the first element that declares `x:DataType="..."`.
2. A `DataTemplate` parent **shadows** ancestors — DataType discovery restarts at the `DataTemplate` boundary.
3. If the current member is `BindingContext={Binding ...}`, skip the immediate parent (matches MAUI behavior: the DataType applies to descendants, not to the `BindingContext` binding itself).
4. If no `x:DataType` found anywhere, emit **UNO2010** info once per XAML file and leave `DataType = null`.

## Per-chain resolution (for dotted paths)

Given `A.B.C`:

1. Resolve `A` using the decision table above → `ResolutionResult` with a `Location` and optional `Symbol`.
2. If `A` resolved to `This` / `ForcedThis` / `DataType` / `ForcedDataType` / `StaticType`:
   - Walk `.B` off the resolved symbol's member set.
   - If `B` doesn't exist on `A`'s type, emit **UNO2003** (unless the chain uses `?.` and a nullable type, in which case the lookup is on the underlying non-nullable type).
   - Repeat for `.C`.
3. If `A` resolved to `MarkupExtension`, the **entire expression** is a markup extension invocation; stop here and hand off to the existing markup-extension code path.

## Type-name collision detection (UNO2004)

For every root identifier that resolves as `This`, `DataType`, or `ForcedX`:

1. Look up the identifier in the compilation's **global using imports** and **`using static`** imports.
2. If a same-named type exists in any of those scopes, emit `UNO2004` warning.
3. Resolution still proceeds (the member wins; the diagnostic is a hint to disambiguate with `{= TypeName.Member}`).

## Analysis of remaining identifiers

Non-root identifiers in a compound expression (method arguments, operands, captures) are handled by `ExpressionAnalyzer`:

- Identifiers whose root resolves to `DataType` or `ForcedDataType` are **prefixed with `__source.`** in the transformed expression and contribute one `BindingHandler` per chain hop that reaches a notification-capable instance.
- Identifiers whose root resolves to `This` or `ForcedThis` are **captured at load time** into `__capture_X` locals and do not contribute to the refresh set.
- Identifiers that resolve to a **static type** are emitted **unchanged** (fully-qualified with `global::` when necessary to avoid ambiguity) and do not contribute to the refresh set.

## Refresh-set computation

For each `MemberAccessExpressionSyntax` chain whose root resolved to the DataType:

1. For each hop `i` in `chain[0..n]`, emit one `BindingHandler`:
   - `Accessor` = `__source => __source.chain[0..i-1]`  (i.e. the parent instance up to this hop)
   - `PropertyName` = `chain[i]`
2. De-duplicate `(Accessor, PropertyName)` pairs by string equality.
3. The resulting list is the refresh set for this expression.

For `User.Address.City`:
```
[
  (s => s,             "User"),
  (s => s.User,        "Address"),
  (s => s.User?.Address, "City"),
]
```

Non-INPC intermediate hops (where `typeof(instance).DoesNotImplementINotifyPropertyChanged`) are still included — the binding back-end gracefully no-ops subscription attempts on non-INPC instances, matching x:Bind semantics.

## Two-way inference

The expression is **settable** (→ two-way binding emitted) iff **all** are true:

1. `ExpressionAst.IsPureDottedPath == true` (every node is identifier or member access; no operators, invocations, interpolations, ternaries).
2. Root resolves to `DataType` or `ForcedDataType`.
3. Every hop in the chain is a property (not a method, not a field).
4. The leaf property has a public setter.
5. Every intermediate instance implements `INotifyPropertyChanged` **or** is a `DependencyObject` (so the change notification path is complete).

Rule 5 matches Uno's existing x:Bind two-way inference rules (spec FR-009).

If the target XAML property's `FrameworkPropertyMetadata.BindsTwoWayByDefault == true` and the expression is **not** settable, emit **UNO2012** info.

## One-shot detection

If `Handlers` is empty after analysis (the expression references only static types, non-INPC instances, or literals), the binding is **one-shot** — evaluated once at load time and never refreshed. Emit **UNO2011** info at generator time.

## Async lambda detection

Classifier-phase regex `^\s*async\s+(\([^)]*\)|\w+|\(\))\s*=>` → emit `UNO2005` error; skip lowering; no binding is generated.

## Event-lambda signature validation

After resolution of the event's `IEventSymbol`:

1. Parse the lambda's parameter list (`(s, e)` → 2 params).
2. Get the event's delegate `IMethodSymbol.Parameters`.
3. If the arities differ, emit **UNO2008** error pointing at the lambda's parameter list.
4. Parameter types are not checked at generator time (the emitted C# is validated by Roslyn on compile).
