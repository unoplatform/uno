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
- **Consumed by**: Uno XAML source generator only (the WinAppSDK pre-processing task does not use this property — it only injects `xmlns` and `xmlns:x`)

## Property Flow

```
User .csproj
  → Uno.Sdk .props (sets defaults)
    → Uno.UI.SourceGenerators.props (declares CompilerVisibleProperty)
      → Source Generator (reads via context.GetMSBuildPropertyValue)
```

For WinAppSDK (limited to implicit `xmlns` and `xmlns:x` only):
```
User .csproj
  → Uno.Sdk .props (sets defaults)
    → Uno.Sdk WinAppSDK .targets (runs pre-processing task before MarkupCompilePass1,
                                   injects default xmlns and xmlns:x declarations)
```

> **Note**: Custom `XmlnsDefinition` registrations and `XmlnsPrefix`-based implicit prefixes are not supported on WinAppSDK targets. The MSBuild pre-processing task only handles the two base namespace declarations.
