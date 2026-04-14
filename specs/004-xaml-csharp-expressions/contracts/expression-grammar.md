# Contract: Expression Grammar

The C# sub-grammar the generator accepts inside XAML attribute values. This is the authoritative list; anything outside it is either a conventional markup extension or a diagnostic.

## Classification

An attribute value is treated as a **C# expression** if and only if:

1. The feature is opted in (`UnoXamlCSharpExpressionsEnabled=true`), AND
2. The value is delimited by `{` and `}` (or wrapped in `<![CDATA[ {...} ]]>`), AND
3. At least one of the following is true after stripping outer braces:
   - Starts with `= ` (**explicit directive**; always an expression)
   - Starts with `this.` (**forced-this directive**)
   - Starts with `.` followed by a letter or `_` (**forced-datatype directive**)
   - Matches the **unambiguous-start pattern** — regex `^\s*(\(|!|new\b|typeof\(|nameof\(|default\(|sizeof\(|\$"|\$'|BindingContext\.)`
   - Matches the **method-call pattern** — regex `^\s*\w+\s*\(`
   - Matches the **member-access pattern** — regex `^\s*\w+\s*\.\s*\w+` AND the prefix is NOT a known XML namespace alias resolving to a markup extension namespace
   - Matches the **binary-operator pattern** — the raw text contains one of `+ - * / % == != < > <= >= && || ?? ?:` or any of the whitespace-bounded aliases `AND OR LT GT LTE GTE`
4. AND the expression is **not** a registered markup extension invocation of the form `{prefix:TypeName arg1=val1, arg2=val2}` — the markup extension path always wins when it matches first (preserves backward compatibility).

Rule 4 is the tie-break. It resolves the overlap with existing `{Binding Foo}`, `{StaticResource Baz}`, `{x:Bind Qux}`, etc.

## Accepted forms

### Literals

| Form | Example | Notes |
|------|---------|-------|
| Integer | `42`, `1_000`, `0x1F`, `0b1010` | Standard C# integer literal syntax |
| Floating-point | `3.14`, `1.5e10`, `2.0f`, `1.2m` | `f`/`d`/`m` suffixes accepted |
| Boolean | `true`, `false` | |
| Null | `null` | |
| String (single-quoted) | `'hello'`, `'it\'s'` | Translated to double-quoted C# string; escape sequences `\'`, `\"`, `\\`, `\n`, `\t`, `\0`, `\u00XX` accepted |
| Character (contextual) | `'x'` as a `char` arg | Single-character string literal is re-emitted as C# `char` literal only when the surrounding method call's parameter type is `char` |
| Interpolated string | `$'{Name}: {Count:D3}'`, `$"..."` | Format specifiers honored (culture-aware at runtime) |

### Identifiers and member access

| Form | Example |
|------|---------|
| Simple identifier | `Name` |
| Dotted path | `User.Address.City` |
| Null-conditional | `User?.DisplayName`, `User?.Address?.City` |
| `this.` prefix | `this.PageLevelProperty` |
| `.` prefix | `.DataTypeProperty` |
| `BindingContext.` | `BindingContext.Foo` (equivalent to `.Foo` when DataType doesn't declare a `BindingContext` member) |

### Method invocation

| Form | Example |
|------|---------|
| Instance method | `GetText()`, `User.Name.ToUpper()` |
| Static method | `Math.Max(A, B)`, `string.Format('{0:C2}', Price)` |
| Null-conditional invocation | `Callback?.Invoke()` |
| Arguments can be any accepted expression | `Math.Max(A + 1, B * 2)` |

### Operators

| Category | Operators |
|----------|-----------|
| Arithmetic | `+` `-` `*` `/` `%` |
| Comparison | `==` `!=` `<` `<=` `>` `>=` |
| Logical (boolean) | `&&` `\|\|` `!` (unary) |
| Null-coalescing | `??` |
| Conditional | `? :` (ternary) |
| Unary | `-` (numeric negation) `!` (boolean) |
| Parentheses | `(` `)` |

Increment/decrement `++` `--` are accepted **only inside event lambda bodies**.

### Operator aliases (whitespace-bounded, case-insensitive)

| Alias | Canonical |
|-------|-----------|
| `AND` | `&&` |
| `OR` | `\|\|` |
| `LT` | `<` |
| `LTE` | `<=` |
| `GT` | `>` |
| `GTE` | `>=` |

Replacement rules:
1. Applied **outside of string literals** only (single-quoted and interpolated strings are skipped token-by-token).
2. Each alias must be **surrounded by whitespace** (or expression boundary). `CountGT0` is NOT a match for `GT`.
3. `LTE` and `GTE` are replaced **before** `LT` and `GT` so the longer form wins.
4. Case-insensitive: `and`, `And`, `AND`, `aNd` all equivalent.

### Event lambdas

| Form | Example |
|------|---------|
| Two-argument lambda | `(s, e) => Counter++` |
| Two-argument with statement block body | `(s, e) => { Counter++; Log('click'); }` — **single-statement body only**, block wrapper accepted |
| Parameter names are chosen by the author | `(sender, args) => sender.Foo()` |

**Rejected**:
- `async (s, e) => ...` — `UNO2005`
- `() => ...` or `x => ...` — not emitted as an expression; falls through to named-handler resolution
- Multi-statement lambda bodies (multiple `;` at top level of the block) — `UNO2006`

### Disambiguation directives

| Directive | Meaning |
|-----------|---------|
| `{= expr}` | Force C# expression resolution. Every directive strip is exact: two leading chars (`{=`) and one trailing char (`}`) |
| `{.Member}` | Force DataType resolution |
| `{this.Member}` | Force page-level resolution |
| `{prefix:Name ...}` | Force markup extension invocation |

### CDATA fallback

```xml
<Button.Visibility><![CDATA[{Count > 0 && IsEnabled}]]></Button.Visibility>
```

- Only allowed in **element syntax** for a property (not in attribute syntax, which is already an XML string).
- The content must begin with `{` and end with `}`.
- Raw `<`, `>`, `&`, and double-quoted strings are permitted inside.
- The `IsCData` flag on the produced `XamlExpressionAttributeValue` is `true`.

## Rejected forms (hard diagnostics)

| Form | Code | Notes |
|------|------|-------|
| Empty body: `{}`, `{   }`, `{= }`, `<![CDATA[{ }]]>` | `UNO2007` | `{}` in attribute syntax is the XAML literal-escape and must be preserved (not reinterpreted as empty expression) |
| Multi-statement body (`A; B`) | `UNO2006` | Only single expressions (and single-statement lambda bodies) are supported |
| Nested markup extension: `{A + {Binding X}}` | `UNO2009` | Compound expressions may not contain markup-extension calls; use compiled bindings instead |
| `async` lambda | `UNO2005` | |
| `goto`, `yield`, `throw`, `return`, control-flow statements | `UNO2006` | |

## Notes on parsing order

1. **Classify** the value (above rules).
2. Strip directive prefix if any (`{=`, `{`, `{.`, `{this.`).
3. Unwrap CDATA if present.
4. Replace operator aliases.
5. Transform quotes (`'…'` → `"…"`).
6. Feed result to Roslyn `CSharpSyntaxTree.ParseText(code, CSharpParseOptions(kind: SourceCodeKind.Script))`.
7. If Roslyn reports a parse diagnostic, surface it as `UNO2006` (generic "invalid C# expression") with the original location.
8. Post-parse, re-run `TransformQuotesWithSemantics` to re-detect char-literal contexts using the resolved method symbols.
