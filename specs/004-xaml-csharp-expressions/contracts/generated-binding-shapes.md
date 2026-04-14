# Contract: Generated Binding Shapes

The exact shape of C# emitted by the generator for each expression kind. These are the **lowered output** that is then compiled as part of the XAML-generated partial class. All shapes reuse existing Uno compiled-binding / inline-event infrastructure — no new runtime types are introduced.

## 1. Simple settable dotted path

### Input

```xml
<TextBox x:DataType="local:Customer" Text="{FirstName}" />
```

### Output (generated C# inside the page's XAML partial)

Equivalent to the emit for `{x:Bind FirstName, Mode=TwoWay}`. The `LoweredExpression.SimplePathBinding` variant is handed to the existing `XBindExpressionParser.Rewrite` + `BuildXBindEvalFunction` emitter. The resulting code is the same `Binding`-setup call the x:Bind emitter produces today; no changes to the emitter.

Key property: **byte-for-byte identical** to the output of a hand-written `{x:Bind FirstName, Mode=TwoWay}` against the same DataType, with the same refresh-set subscription.

## 2. Simple read-only dotted path (leaf is read-only)

### Input

```xml
<TextBlock x:DataType="local:Customer" Text="{FullName}" />
```

### Output

Equivalent to `{x:Bind FullName, Mode=OneWay}`. Same emitter; differs from §1 only in the `Mode` parameter.

## 3. Compound expression (one-way, refreshed by INPC)

### Input

```xml
<TextBlock x:DataType="local:Order" Text="{Price * Quantity}" />
```

### Output (sketch — exact token count may vary)

```csharp
// Emitted once per unique expression per page:
private decimal __xcs_Expr_001(global::MyApp.Order __source)
    => __source.Price * __source.Quantity;

// At the call site of the TextBlock setup (equivalent to x:Bind lowering):
__that.SetBinding(
    global::Microsoft.UI.Xaml.Controls.TextBlock.TextProperty,
    new global::Microsoft.UI.Xaml.Data.Binding
    {
        Path = null,
        Mode = global::Microsoft.UI.Xaml.Data.BindingMode.OneWay,
        CompiledSource = ... /* x:Bind-equivalent CompiledBindingSource with getter = __xcs_Expr_001(__source) */,
        Properties = new[] { "Price", "Quantity" }  // refresh set
    });
```

The exact emitter output follows Uno's existing compiled-binding back-end for `x:Bind Foo()` expressions — the generator synthesizes a method on the page and references it the same way `x:Bind`-with-function-call does today.

## 4. Compound expression with captures (page-level identifier mixed with DataType identifier)

### Input

```xml
<TextBlock x:DataType="local:Order"
           Text="{Price * this.TaxRate}" />
```

### Output (sketch)

```csharp
private decimal __xcs_Expr_002(
    global::MyApp.Order __source,
    decimal __capture_TaxRate)
    => __source.Price * __capture_TaxRate;

// At setup:
var __capture_TaxRate = this.TaxRate;   // load-time capture
__that.SetBinding(
    TextBlock.TextProperty,
    new Binding {
        Mode = BindingMode.OneWay,
        CompiledSource = ... /* calls __xcs_Expr_002(__source, __capture_TaxRate) */,
        Properties = new[] { "Price" }   // refresh set: only notifying DataType members
    });
```

- `TaxRate` on the page is read **once** at binding setup (load time) and its value snapshotted into `__capture_TaxRate`.
- `TaxRate` changes do **not** trigger rebinding.
- This matches spec FR-013 semantics.

## 5. One-shot (only non-notifying sources)

### Input

```xml
<TextBlock Text="{DateTime.Now.ToString('t')}" />
```

### Output

Evaluated once at load; no refresh path.

```csharp
__that.Text = global::System.DateTime.Now.ToString("t");
```

Direct property assignment — no `Binding` setup at all. `UNO2011` info diagnostic fires at generator time.

## 6. Compound with string interpolation

### Input

```xml
<TextBlock x:DataType="local:Order" Text="{$'{Price:C2} × {Quantity}'}" />
```

### Output

```csharp
private string __xcs_Expr_003(global::MyApp.Order __source)
    => $"{__source.Price:C2} × {__source.Quantity}";

// ... Binding setup with Properties = ["Price", "Quantity"]
```

String interpolation preserves the format specifier and culture-aware evaluation (spec FR-012).

## 7. Null-coalescing and ternary

### Input

```xml
<TextBlock x:DataType="local:Customer" Text="{IsVip ? 'Gold' : (Nickname ?? 'Anonymous')}" />
```

### Output

```csharp
private string __xcs_Expr_004(global::MyApp.Customer __source)
    => __source.IsVip ? "Gold" : (__source.Nickname ?? "Anonymous");

// ... Binding setup with Properties = ["IsVip", "Nickname"]
```

## 8. Static method invocation

### Input

```xml
<TextBlock x:DataType="local:MyVm" Text="{Math.Max(A, B)}" />
```

### Output

```csharp
private int __xcs_Expr_005(global::MyApp.MyVm __source)
    => global::System.Math.Max(__source.A, __source.B);

// ... Binding setup with Properties = ["A", "B"]
```

`Math` is a static type resolved via `global using System` — does NOT contribute to the refresh set. The `global::` qualifier prevents collisions.

## 9. Event lambda (no parameters referenced)

### Input

```xml
<Button x:DataType="local:MyVm" Click="{(s, e) => Counter++}" />
```

### Output

```csharp
private void __xcs_EventHandler_001(object s, global::Microsoft.UI.Xaml.RoutedEventArgs e)
{
    // 'Counter' resolves on DataType (via BindingContext) → goes through __source.
    // For event lambdas, 'Counter' on DataType needs an explicit BindingContext fetch:
    var __source = (global::MyApp.MyVm)this.BindingContext;
    __source.Counter++;
}

// At setup (reusing existing inline-event emitter):
__that.Click += __xcs_EventHandler_001;
```

The existing `XamlFileGenerator.GenerateInlineEvent` handles the `+=` wiring; we add a branch that, instead of resolving a named method on the page, emits the synthesized `__xcs_EventHandler_NNN` method and binds that.

## 10. Event lambda referencing `sender`/`args`

### Input

```xml
<TextBox TextChanged="{(s, e) => Search(((TextBox)s).Text)}" />
```

### Output

```csharp
private void __xcs_EventHandler_002(object s, global::Microsoft.UI.Xaml.Controls.TextChangedEventArgs e)
{
    var __source = (global::MyApp.MyVm)this.BindingContext;
    __source.Search(((global::Microsoft.UI.Xaml.Controls.TextBox)s).Text);
}
```

Parameter names are taken verbatim from the user's lambda.

## 11. Forced-this vs forced-datatype

### Input

```xml
<TextBlock Text="{this.WindowTitle}" />
<TextBlock Text="{.FirstName}" />
```

### `{this.WindowTitle}` Output

Read-once capture (page-level identifier has no automatic INPC hookup; authors who want reactivity should use `x:Bind` against the page):

```csharp
__that.Text = this.WindowTitle;
```

Unless `WindowTitle` is itself a DependencyProperty with property-change notification, in which case the binding uses a page-source compiled binding equivalent to `{x:Bind WindowTitle}` (rare case; the emitter decides based on the resolved symbol).

### `{.FirstName}` Output

Equivalent to `{x:Bind FirstName, Mode=OneWay}` against the DataType:

```csharp
__that.SetBinding(TextBlock.TextProperty,
    new Binding { Mode = BindingMode.OneWay, /* x:Bind-equivalent compiled source */ });
```

## 12. CDATA

### Input

```xml
<Button>
  <Button.Visibility>
    <![CDATA[{Count > 0 && IsEnabled ? Visibility.Visible : Visibility.Collapsed}]]>
  </Button.Visibility>
</Button>
```

### Output

Same as §3 — CDATA is just a syntactic wrapper; after unwrapping, the expression goes through the identical pipeline.

## 13. Operator aliases

### Input

```xml
<Button IsEnabled="{Count GT 0 AND IsEnabled}" />
```

### Output

After alias replacement (`GT → >`, `AND → &&`):

```csharp
private bool __xcs_Expr_006(global::MyApp.MyVm __source)
    => __source.Count > 0 && __source.IsEnabled;

// ... Binding setup with Properties = ["Count", "IsEnabled"]
```

## Helper-method naming convention

- Binding helpers: `__xcs_Expr_{NNN}` (counter per page, zero-padded to 3)
- Event handlers: `__xcs_EventHandler_{NNN}`
- Captured locals: `__capture_{IdentifierName}` (or `{IdentifierName}_{disambiguator}` on collision)
- Source parameter name: `__source` (matches MAUI convention; matches existing x:Bind generator where applicable)

All helper methods are emitted as **private methods** on the page's XAML partial class.

## Invariants

- **No new runtime types.** Every shape above compiles against existing Uno public APIs.
- **Generated C# is idempotent.** Re-running the generator with the same input produces byte-identical output.
- **No conflicts with existing x:Bind.** The emitter produces the same binding-setup code whether the input was `{x:Bind Foo}` or a lowered simple-path expression `{Foo}`.
- **No boxing in hot paths.** `__source` is passed as a strongly-typed parameter, not `object`.

## Testing

Each shape has at least one **generator snapshot test** in `Given_ExpressionLowering.cs` that compares the emitted text against a captured golden file. Each shape also has at least one **runtime test** in `Tests_CSharpExpressions/` that loads the generated page and asserts behavior.
