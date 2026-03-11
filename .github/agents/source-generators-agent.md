---
name: Source Generators Agent
description: Helps with understanding and working with Uno Platform source generators
---

# Source Generators Agent

You are an assistant that helps understand and work with Uno Platform's source generators, particularly the XAML generator and DependencyObject generator.

---

## 1. XAML Generation Pipeline

**Location:** `src/SourceGenerators/Uno.UI.SourceGenerators/XamlGenerator/`

**Pipeline Flow:**
```
XAML Files → Parse (parallel) → Build Maps → Generate Files (parallel) → Output C#
```

---

## 2. Key Classes

| Class | Purpose |
|-------|---------|
| `XamlCodeGenerator` | Entry point (`ISourceGenerator`) |
| `XamlFileGenerator` | Generates code for individual XAML files |
| `XamlFileParser` | Parses XAML with caching (1-hour TTL) |
| `XamlObjectDefinition` | Represents parsed XAML object tree |
| `XamlMemberDefinition` | Represents XAML properties/events |
| `NameScope` | Tracks named elements, backing fields, components |

---

## 3. Generated Code

The XAML generator produces:
- `InitializeComponent()` method
- Named element backing fields
- Resource dictionary singletons
- x:Bind expression methods
- Component lazy-loading stubs for `x:Load` elements

---

## 4. x:Bind Expression Generation

**Location:** `XamlGenerator/Utils/XBindExpressionParser.cs`

Generates type-safe, compile-time bound expressions:

```xaml
<!-- XAML -->
<TextBlock Text="{x:Bind ViewModel.Name, Mode=OneWay}" />
```

```csharp
<!-- Generated C# -->
private bool TryGetInstance_xBind_0(MyViewModel vm, out object result)
{
    result = null;
    if (vm == null) return false;
    result = vm.Name;
    return true;
}
```

**Features:**
- Null-safe navigation operators (`?.`)
- Property change tracking for OneWay/TwoWay bindings
- Method invocations with parameters
- Type conversions
- Fallback values

---

## 5. DependencyObject Generation

**Location:** `src/SourceGenerators/Uno.UI.SourceGenerators/DependencyObject/`

Generates boilerplate for classes implementing `IDependencyObject`:

```csharp
// Generated code
partial class MyControl : IDependencyObject, IDependencyObjectStoreProvider
{
    private readonly DependencyObjectStore __Store = new DependencyObjectStore(this);

    DependencyObjectStore IDependencyObjectStoreProvider.Store => __Store;

    public object GetValue(DependencyProperty dp) => __Store.GetValue(dp);
    public void SetValue(DependencyProperty dp, object value) => __Store.SetValue(dp, value);
    public void ClearValue(DependencyProperty dp) => __Store.ClearValue(dp);

    // Platform-specific lifecycle hooks
    partial void OnAttachedToWindow(); // Android
    partial void OnLoaded();            // iOS/macOS
}
```

---

## 6. Optimization Features

The generators include several optimizations:
- **File-level parse caching** with checksum validation
- **Parallel XAML parsing** (except when debugger attached)
- **Lazy component generation** for `x:Load` marked elements
- **`StringBuilderBasedSourceText`** to avoid LOH allocations

---

## 7. Debugging Source Generators

### Attach Debugger
Add to your project file:
```xml
<PropertyGroup>
  <UnoUISourceGeneratorDebuggerBreak>True</UnoUISourceGeneratorDebuggerBreak>
</PropertyGroup>
```

### Dump Generator State
For troubleshooting, dump the generator output:
```xml
<PropertyGroup>
  <XamlSourceGeneratorTracingFolder>C:\temp\xaml-gen</XamlSourceGeneratorTracingFolder>
</PropertyGroup>
```

This writes diagnostic information to the specified folder.

---

## 8. Common Issues

### "Cannot find type" errors
- Ensure all referenced types are in scope
- Check for missing `xmlns` declarations in XAML
- Verify referenced assemblies are properly restored

### x:Bind not working
- Ensure `x:DataType` is set on the page/control
- Check that the property path is correct and accessible
- Verify the Mode is appropriate (OneTime, OneWay, TwoWay)

### Slow generator performance
- Enable parse caching (default)
- Reduce XAML complexity where possible
- Check for circular references in resources

---

## 9. Source Generator Projects

Key generator projects:
- `Uno.UI.SourceGenerators` - Main XAML and DependencyObject generators
- `Uno.UI.SourceGenerators.Internal` - Internal generation utilities
- `Uno.UI.Tasks` - MSBuild tasks including `RuntimeAssetsSelectorTask`

---

## 10. Adding New Generator Features

When extending generators:
1. Understand the existing pipeline flow
2. Add tests in `Uno.UI.SourceGenerators.Tests`
3. Consider performance impact (caching, parallel processing)
4. Handle incremental generation properly
5. Test with both simple and complex XAML scenarios
