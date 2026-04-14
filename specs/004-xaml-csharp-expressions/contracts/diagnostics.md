# Contract: Diagnostics (UNO2xxx)

The complete list of diagnostics this feature introduces. Codes are **stable across versions** once published (spec FR-022a).

## Code ranges

| Range | Purpose |
|-------|---------|
| `UNO2000–UNO2019` | Resolution, parsing, and diagnostic behavior |
| `UNO2020–UNO2029` | Opt-in / configuration errors |
| `UNO2099` | Platform exclusion (WinAppSDK) |

## Descriptors

Each descriptor is registered in `Diagnostics.cs` as `DiagnosticDescriptor` with category `"XAML.CSharpExpressions"`.

### UNO2001 — AmbiguousExpressionOrMarkup

- **Severity**: Warning
- **Title**: "Expression conflicts with a registered markup extension"
- **Message**: `"Identifier '{0}' matches both a markup extension '{0}Extension' and a member in scope. The markup extension wins; use {= {0}} to force the C# expression or use {prefix:{0}} to force the markup extension and suppress this warning."`
- **Triggered by**: bare `{Foo}` when both `FooExtension` (or `Foo` as a markup extension type) and a `Foo` member exist in scope.
- **MAUI equivalent**: MAUIX2007

### UNO2002 — AmbiguousMemberExpression

- **Severity**: Error
- **Title**: "Member expression is ambiguous"
- **Message**: `"Identifier '{0}' exists on both the page and the DataType. Use 'this.{0}' to reference the page member or '.{0}' (or 'BindingContext.{0}') to reference the DataType member."`
- **Triggered by**: bare `{Foo}` when both `PageType` and `DataType` declare `Foo`.
- **MAUI equivalent**: MAUIX2008

### UNO2003 — MemberNotFound

- **Severity**: Error
- **Title**: "Member not found"
- **Message**: `"'{0}' not found on page type '{1}' or DataType '{2}'."` (when DataType is known; message omits the DataType clause otherwise)
- **Triggered by**: simple identifier expression that resolves nowhere.
- **MAUI equivalent**: MAUIX2009

### UNO2004 — AmbiguousMemberWithStaticType

- **Severity**: Warning
- **Title**: "Member conflicts with a static type name"
- **Message**: `"Identifier '{0}' collides with type '{1}' imported via {2}. The member wins; use '{0}.Member' form or a different member name to disambiguate."`
- **Triggered by**: root identifier resolves as a member but a same-named type is in scope via `global using` or `using static`.
- **MAUI equivalent**: MAUIX2011

### UNO2005 — AsyncLambdaNotSupported

- **Severity**: Error
- **Title**: "Async event lambdas are not supported"
- **Message**: `"Async lambda expressions are not supported as XAML event handlers. Define a named async method in code-behind and reference it with Click=\"OnClick\" instead."`
- **Triggered by**: event-attribute expression matching `^\s*async\s+(\([^)]*\)|\w+|\(\))\s*=>`.
- **MAUI equivalent**: MAUIX2013

### UNO2006 — InvalidExpressionSyntax

- **Severity**: Error
- **Title**: "Invalid C# expression syntax"
- **Message**: `"The expression '{0}' is not a valid single C# expression: {1}."` (`{1}` is the Roslyn parse error or "multiple statements not allowed")
- **Triggered by**:
  - Multi-statement body (semicolon at top level outside a lambda block)
  - Statement forms (`throw`, `return`, `yield`, control-flow)
  - Any Roslyn script-mode parse diagnostic
- **Uno-new**.

### UNO2007 — EmptyExpression

- **Severity**: Error
- **Title**: "Expression is empty"
- **Message**: `"Empty expressions are not allowed. Remove the attribute or provide a valid expression between the braces."`
- **Triggered by**: `{   }`, `{= }`, `{=   }`, `<![CDATA[{  }]]>`.
  - **Note**: `{}` in attribute syntax is the XAML literal-escape (a single opening brace in the value). It is **not** a diagnostic — the existing XAML parser already interprets `{}foo` as literal `{foo`; we preserve that.
- **Uno-new**.

### UNO2008 — EventLambdaArityMismatch

- **Severity**: Error
- **Title**: "Event lambda has wrong number of parameters"
- **Message**: `"Event '{0}' expects {1} parameter(s) but the lambda has {2}. Use '(sender, args) => ...' matching the event's delegate signature."`
- **Triggered by**: event lambda whose parameter count differs from the event's delegate `Invoke` method arity.
- **Uno-new**.

### UNO2009 — NestedMarkupExtensionInExpression

- **Severity**: Error
- **Title**: "Markup extension nested inside a C# expression"
- **Message**: `"Markup extension '{0}' cannot appear inside a C# expression. Split into a separate attribute or rewrite the expression."`
- **Triggered by**: classifier detects a `{prefix:Name}` token as a sub-expression of a larger `{= ...}` expression.
- **Uno-new**.

### UNO2010 — XDataTypeMissing

- **Severity**: Info
- **Title**: "x:DataType is missing; DataType-based resolution is disabled"
- **Message**: `"The XAML file '{0}' contains C# expressions but does not declare x:DataType. DataType-relative identifiers will not resolve; use 'this.Member' for page-level references."`
- **Triggered by**: any expression in a XAML file with no `x:DataType` on any ancestor.
- Emitted **once per XAML file** (deduped by file path).
- **Uno-new**.

### UNO2011 — OneShotNonNotifyingSources

- **Severity**: Info
- **Title**: "Expression has no notifying sources; binding is one-shot"
- **Message**: `"Expression '{0}' references only non-notifying sources (static members or non-INPC instances). The binding will be evaluated once at load and will not refresh."`
- **Triggered by**: `ExpressionAnalysisResult.Handlers.Count == 0` after analysis.
- **Uno-new**.

### UNO2012 — ExpressionNotSettableDowngrade

- **Severity**: Info
- **Title**: "Compound expression downgrades two-way default binding to one-way"
- **Message**: `"Property '{0}' has a two-way default binding mode, but the expression is not a simple settable path. Binding is emitted as one-way."`
- **Triggered by**: target DP has `FrameworkPropertyMetadata.BindsTwoWayByDefault == true` and `ExpressionAst.IsSettable == false`.
- **MAUI equivalent**: MAUIX2010.

### UNO2020 — OptInDirectiveWhenDisabled

- **Severity**: Error
- **Title**: "C# expressions are not enabled in this project"
- **Message**: `"The attribute '{0}' uses C# expression syntax but UnoXamlCSharpExpressionsEnabled is not set to true. Set <UnoXamlCSharpExpressionsEnabled>true</UnoXamlCSharpExpressionsEnabled> in the csproj or use a conventional {Binding} expression."`
- **Triggered by**: `{= ...}`, `{.Member}`, `{this.Member}` appearing in a XAML file when the opt-in is off. (These forms are unambiguously the new feature; a clear diagnostic is better than silent fall-through to the markup-extension parser which would emit a confusing error.)
- **Uno-new**.

### UNO2099 — CSharpExpressionsNotSupportedOnWinAppSDK

- **Severity**: Error
- **Title**: "XAML C# expressions are not supported on WinAppSDK"
- **Message**: `"The XAML file '{0}' uses C# expression syntax (attribute '{1}') but the active target is WinAppSDK. This feature is Uno-only. Exclude this XAML from the WinAppSDK target with <Page Condition=\"'$(IsWinAppSDK)' != 'true'\"/> or rewrite the attribute using a conventional {Binding}, {x:Bind}, or named event handler."`
- **Triggered by**: `PlatformHelper.IsWinAppSDK(context) && UnoXamlCSharpExpressionsEnabled == true && classifier identified at least one expression`.
- **Fires per XAML file** (not per attribute), pointing at the first offending attribute.
- **Uno-new** (satisfies spec FR-015).

## Location format

Every diagnostic carries:
- **File path**: absolute or repo-relative
- **Line number** (1-based)
- **Column number** (1-based, at the opening `{` of the offending attribute value)
- **Length**: end of attribute value

These are provided via Roslyn's `Location.Create(sourceTree, textSpan)` using the XAML file's `AdditionalText` tracked by the generator. For XAML files that don't have a tracked `AdditionalText`, we fall back to `Location.Create(filePath, textSpan, linePositionSpan)`.

## Stability

Once a code is published in a release, it MUST NOT:
- Change meaning
- Change severity (downgrades/upgrades must use a new code and mark the old one obsolete in release notes)
- Be removed without an at-least-one-release deprecation window

New codes may be added. Gaps in the numeric sequence are allowed (don't renumber).

## Testing

Every descriptor has at least one dedicated test in `Given_ExpressionDiagnostics.cs` that:
1. Feeds a minimal reproducing XAML snippet through the generator
2. Asserts the descriptor is present in the diagnostic list
3. Asserts the location points to the expected line/column
4. Asserts no unrelated diagnostics fire

Covered by spec FR-025 and SC-003.
