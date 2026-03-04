# Source Generator API Contract

## Modified Components

### XamlFileParser

**New constructor parameter**: `bool enableImplicitNamespaces`

**Behavior change in `RewriteForXBind` / XmlReader creation**:
- When `enableImplicitNamespaces == true`:
  - Create `XmlNamespaceManager` with pre-populated namespace mappings
  - Set `ConformanceLevel.Fragment` in `XmlReaderSettings`
  - Pass `XmlParserContext` to `XmlReader.Create()`
- When `enableImplicitNamespaces == false`:
  - Current behavior (no change)

**Implicit namespace mappings injected**:
```
"" (default) → "http://schemas.microsoft.com/winfx/2006/xaml/presentation"
"x"          → "http://schemas.microsoft.com/winfx/2006/xaml"
```

Plus any prefixes discovered from `XmlnsPrefix` attributes in referenced assemblies.

### XamlFileGenerator.Reflection

**New field**: `string[] _globalClrNamespaces` - CLR namespaces registered to the global URI.

**Modified method**: `SourceFindTypeByXamlType(XamlType type)`
- After searching `_knownNamespaces` for the presentation URI, also search `_globalClrNamespaces` for types registered to the global namespace URI.

**Modified method**: `InitCaches()`
- When implicit namespaces enabled and no default xmlns in file, use presentation namespaces as `_clrNamespaces`.
- Populate `_globalClrNamespaces` from discovered `XmlnsDefinition` attributes.

### XamlConstants

**New constant**:
```csharp
public const string GlobalNamespaceUri = "http://schemas.microsoft.com/winfx/2006/xaml/presentation/global";
```

### XamlCodeGeneration

**New MSBuild property read**: `UnoEnableImplicitXamlNamespaces`
**Passed to**: `XamlFileParser` constructor and `XamlFileGenerator` constructor.

## New Components

### GlobalNamespaceResolver (new class)

**Purpose**: Discovers and caches `XmlnsDefinition` and `XmlnsPrefix` attributes from the compilation's referenced assemblies.

**Key methods**:
- `GetClrNamespacesForGlobalUri(Compilation compilation)` → `string[]`
- `GetImplicitPrefixes(Compilation compilation)` → `(string prefix, string uri)[]`

**Caching**: Results cached per compilation to avoid repeated assembly scanning.
