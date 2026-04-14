# Quickstart: XAML C# Expressions

**Status**: design-time; not yet implemented. This document is the on-ramp for contributors who will work on the feature and the spec for how end-users will adopt it once shipped.

## For end-users (once shipped)

### Enable the feature

In your `.csproj`:

```xml
<PropertyGroup>
  <UnoXamlCSharpExpressionsEnabled>true</UnoXamlCSharpExpressionsEnabled>
</PropertyGroup>
```

Default is **off**. When off, Uno's XAML generator emits byte-identical output to the pre-feature version.

### Minimal page

```xml
<Page x:Class="MyApp.MainPage"
      xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
      xmlns:local="using:MyApp"
      x:DataType="local:CustomerViewModel">

  <!-- Simple two-way binding -->
  <TextBox Text="{Name}" />

  <!-- Compound one-way expression -->
  <TextBlock Text="{FirstName + ' ' + LastName}" />

  <!-- Ternary + null-coalescing -->
  <TextBlock Text="{IsVip ? 'Gold' : (Nickname ?? 'Standard')}" />

  <!-- Formatted interpolation -->
  <TextBlock Text="{$'{Balance:C2}'}" />

  <!-- Static call -->
  <TextBlock Text="{Math.Max(A, B)}" />

  <!-- Event lambda -->
  <Button Content="Bump" Click="{(s, e) => Counter++}" />

  <!-- Disambiguation -->
  <TextBlock Text="{= Foo}" />
  <TextBlock Text="{this.PageLevelProperty}" />
  <TextBlock Text="{.DataTypeProperty}" />

  <!-- XML-friendly aliases -->
  <Button IsEnabled="{Count GT 0 AND IsEnabled}" />

  <!-- CDATA fallback -->
  <Button.Visibility><![CDATA[{Count > 0 && IsEnabled ? Visibility.Visible : Visibility.Collapsed}]]></Button.Visibility>
</Page>
```

### View-model shape expected

```csharp
public partial class CustomerViewModel : ObservableObject
{
    [ObservableProperty] private string _name = "";
    [ObservableProperty] private string _firstName = "";
    [ObservableProperty] private string _lastName = "";
    [ObservableProperty] private bool _isVip;
    [ObservableProperty] private string? _nickname;
    [ObservableProperty] private decimal _balance;
    [ObservableProperty] private int _a;
    [ObservableProperty] private int _b;
    [ObservableProperty] private int _count;
    [ObservableProperty] private bool _isEnabled;
    [ObservableProperty] private int _counter;
}
```

### Diagnostics cheatsheet

If the build fails with an `UNO2xxx` code, see `contracts/diagnostics.md`. Most common:

- `UNO2001 / UNO2002 / UNO2003` — identifier resolution ambiguity or member not found. Use `{= Foo}`, `{this.Foo}`, or `{.Foo}` to disambiguate.
- `UNO2005` — async event lambdas are not supported; define a named `async` method in code-behind.
- `UNO2099` — you're building for WinAppSDK. The feature is Uno-only. Exclude the XAML file with `<Page Condition="'$(IsWinAppSDK)' != 'true'">` or rewrite the attribute with a conventional `{Binding}` / code-behind handler.

---

## For contributors

### Layout

```
src/SourceGenerators/Uno.UI.SourceGenerators/XamlGenerator/CSharpExpressions/
  ├── CSharpExpressionClassifier.cs
  ├── CSharpExpressionTokenizer.cs
  ├── CSharpExpressionParser.cs
  ├── MemberResolver.cs
  ├── XDataTypeResolver.cs
  ├── ExpressionAnalyzer.cs
  ├── ExpressionLowering.cs
  ├── OperatorAliases.cs
  ├── QuoteTransform.cs
  └── Diagnostics.cs
```

Hooks into:
- `XamlGenerator/XamlFileGenerator.cs` (~line 3228, before the markup-extension branch)
- `XamlGenerator/XamlFileGenerator.InlineEvent.cs` (extend event-attribute handling to accept lambda expressions)

### TDD workflow (mandatory)

Every FR and diagnostic code gets a **failing test** before the corresponding implementation lands. Use this order when picking up a task:

1. Add the test in the appropriate `Given_*.cs` file (parser, resolver, analyzer, generator snapshot, runtime).
2. Run the test locally — verify **red**.
3. Implement the minimum to turn the test **green**.
4. Refactor; keep tests green.
5. Open PR; CI runs all four test layers + coverage gate.

### Build and test commands

```bash
# Build the generator project
cd src
dotnet build SourceGenerators/Uno.UI.SourceGenerators/Uno.UI.SourceGenerators.csproj

# Run generator unit + snapshot tests
dotnet test SourceGenerators/Uno.UI.SourceGenerators.Tests/Uno.UI.SourceGenerators.Tests.csproj --filter "FullyQualifiedName~CSharpExpressions"

# Run runtime tests (Skia Desktop) via the /runtime-tests skill
#   pass the test class or method name as the skill argument
#   e.g. /runtime-tests Tests_CSharpExpressions
```

### Reference MAUI implementation

- Cloned at `D:\Work\maui`
- Spec: `D:\Work\maui\docs\specs\XamlCSharpExpressions.md`
- PR: https://github.com/dotnet/maui/pull/33693 (squash commit `64d90e3ca4`)
- Key files (for cross-referencing behavior):
  - `src/Controls/src/SourceGen/CSharpExpressionHelpers.cs`
  - `src/Controls/src/SourceGen/MemberResolver.cs`
  - `src/Controls/src/SourceGen/ExpressionAnalyzer.cs`
  - `src/Controls/src/SourceGen/SetPropertyHelpers.cs`
  - `src/Controls/src/SourceGen/Descriptors.cs`

When behavior is unclear, **read the MAUI code first** (Constitution VII equivalent), then decide whether to port faithfully or deviate (documenting deviations in `research.md`).

### Coverage gate

- Parser + generator projects: ≥ 90% line, ≥ 85% branch
- CI blocks merges that regress coverage

### Performance gate

- SamplesApp cold build time regression: ≤ 5%
- Generator output size growth: ≤ 10%
- Measured in CI on a representative XAML corpus; baseline captured on the pre-feature commit

### When in doubt

- Default to MAUI's behavior when the spec is silent.
- Document any deviation in `research.md` §9.2.
- Pair parser / resolver / analyzer changes with their corresponding unit tests in the same PR.
- Ping the XAML generator owners on PR review.
