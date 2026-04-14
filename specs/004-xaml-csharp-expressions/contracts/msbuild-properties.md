# Contract: MSBuild Opt-In

The feature is opt-in at the project level. This document defines the single MSBuild property that gates it, how the generator reads it, and the semantics when the property is off.

## Property

### `UnoXamlCSharpExpressionsEnabled`

- **Type**: `bool` (MSBuild "true"/"false" string)
- **Default**: `false`
- **Where set**: in the consumer's `.csproj` or `Directory.Build.props`
- **Where read**: in the generator, via `context.GetMSBuildPropertyValue("UnoXamlCSharpExpressionsEnabled", "false")`

### Exposure to the generator

Registered as `CompilerVisibleProperty` in the Uno XAML targets file:

```xml
<!-- src/Uno.UI.Xaml/build/Uno.UI.Xaml.targets -->
<ItemGroup>
  <CompilerVisibleProperty Include="UnoXamlCSharpExpressionsEnabled" />
</ItemGroup>
```

This is the established pattern in `Uno.UI.SourceGenerators` (see `UnoPlatformDefaultSymbolsFontFamily`, `_IsUnoUISolution`, `IsUnoHead`).

### Consumption pattern (inside the generator)

```csharp
bool IsEnabled(GeneratorExecutionContext context) =>
    string.Equals(
        context.GetMSBuildPropertyValue("UnoXamlCSharpExpressionsEnabled", "false"),
        "true",
        StringComparison.OrdinalIgnoreCase);
```

Called **once per generator run**, cached for the duration.

## Semantics

### When `UnoXamlCSharpExpressionsEnabled = true`

- The classifier runs for every attribute value.
- Disambiguation directives `{= …}`, `{.Member}`, `{this.Member}` are recognized.
- All resolution, analysis, lowering, and codegen proceed.
- Diagnostics UNO2001–UNO2012, UNO2099 may fire.
- If the compilation target is WinAppSDK, UNO2099 fires for any detected expression (before any other diagnostic).

### When `UnoXamlCSharpExpressionsEnabled = false` (or unset)

- The classifier **does not run**. Attribute value handling is byte-identical to the pre-feature behavior.
- `{= …}`, `{.Member}`, `{this.Member}` appearing in XAML are **not** recognized as feature syntax. The XAML parser will emit a markup-extension parse error on these tokens; the generator re-surfaces that error enhanced with **UNO2020** (to tell the author about the opt-in switch).
- Existing XAML (conventional `{Binding}`, `{StaticResource}`, `{x:Bind}`, custom MarkupExtensions, literals) continues to work unchanged.

### Stability guarantee

Spec FR-013a requires byte-identical generator output when the flag is off, for every existing XAML input. This is validated in `Given_OptInBehavior.cs` by diffing the generator output against a captured golden corpus with the flag off.

## Interaction with other Uno properties

- `_IsUnoUISolution` (internal): no interaction; the feature works the same whether or not we're building Uno itself.
- `IsUnoHead` (internal): no interaction.
- `UnoTargetFrameworkOverride` (dev override): no interaction.

## Release plan

- **First release (preview)**: The property defaults to `false`. Opting in enables the feature and is documented as "preview / may change". Diagnostic messages include the "preview" hint.
- **Post-stabilization release**: The property defaults to `false` still, but the "preview" hint is dropped and the feature is documented as stable. The code numbers are frozen per spec FR-022a.
- **No implicit-enable**: There is never a release in which the feature turns on without the explicit property. This is a hard backward-compatibility guarantee.

## Anti-scenarios (not supported)

- **Per-XAML-file enable**: There is no `<Page UnoXamlCSharpExpressionsEnabled="true">` override. Enable is project-scope.
- **Per-assembly enable via attribute**: Not supported. MSBuild property only.
- **Runtime toggle**: The feature is compile-time only; there is no runtime equivalent.
