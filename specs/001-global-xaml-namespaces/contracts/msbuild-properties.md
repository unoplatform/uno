# MSBuild Properties Contract

## New Properties

### UnoEnableImplicitXamlNamespaces

- **Type**: Boolean
- **Default**: `true` (set in Uno.Sdk)
- **Scope**: Project-level
- **Purpose**: Enables/disables implicit XAML namespace declarations
- **Consumed by**: Uno XAML source generator (via `CompilerVisibleProperty`), WinAppSDK pre-processing MSBuild task

```xml
<!-- Opt out (in user's .csproj) -->
<PropertyGroup>
    <UnoEnableImplicitXamlNamespaces>false</UnoEnableImplicitXamlNamespaces>
</PropertyGroup>
```

### UnoGlobalXamlNamespaceUri

- **Type**: String
- **Default**: `http://schemas.microsoft.com/winfx/2006/xaml/presentation/global`
- **Scope**: Internal (not typically overridden by users)
- **Purpose**: The URI used as the global namespace aggregation point
- **Consumed by**: Uno XAML source generator, WinAppSDK pre-processing task

## Property Flow

```
User .csproj
  → Uno.Sdk .props (sets defaults)
    → Uno.UI.SourceGenerators.props (declares CompilerVisibleProperty)
      → Source Generator (reads via context.GetMSBuildPropertyValue)
```

For WinAppSDK:
```
User .csproj
  → Uno.Sdk .props (sets defaults)
    → Uno.Sdk WinAppSDK .targets (runs pre-processing task before XamlPreCompile)
```
