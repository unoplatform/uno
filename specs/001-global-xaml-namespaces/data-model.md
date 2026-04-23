# Data Model: Global/Implicit XAML Namespaces

**Feature**: `001-global-xaml-namespaces`

## Entities

### ImplicitNamespaceConfiguration

Represents the feature configuration passed from MSBuild to the source generator.

- **IsEnabled** (bool): Whether implicit XAML namespaces are active. Default: `true`.
- **GlobalNamespaceUri** (string): The URI for the global namespace. Value: `http://schemas.microsoft.com/winfx/2006/xaml/presentation/global`.
- **ImplicitPrefixes** (list): Pre-configured prefix-to-URI mappings injected into the parser.
  - Default prefix (empty) → Presentation URI
  - `x:` → XAML URI (`http://schemas.microsoft.com/winfx/2006/xaml`)

### GlobalNamespaceRegistration

Represents a CLR namespace registered to the global namespace URI via `XmlnsDefinition`.

- **ClrNamespace** (string): The .NET namespace (e.g., `MyApp.ViewModels`).
- **AssemblyName** (string, optional): The assembly containing the namespace. Null for current assembly.
- **SourceUri** (string): Always equals the global namespace URI.
- **Source** (enum): `FrameworkDefault` | `UserRegistered` | `ThirdPartyLibrary`.

### ImplicitPrefixRegistration

Represents a prefix registered via `XmlnsPrefix` for implicit availability.

- **Prefix** (string): The XML prefix (e.g., `toolkit`).
- **XmlNamespace** (string): The XML namespace URI the prefix maps to.

## Relationships

- **ImplicitNamespaceConfiguration** contains 0..N **GlobalNamespaceRegistrations** (discovered from `XmlnsDefinition` attributes at compile time).
- **ImplicitNamespaceConfiguration** contains 0..N **ImplicitPrefixRegistrations** (discovered from `XmlnsPrefix` attributes).
- Framework `PresentationNamespaces` are always included (not via global URI, but via the standard presentation URI).

## State Transitions

This feature is stateless at runtime - all configuration is resolved at compile time. The "state" is the resolved set of namespace mappings that the parser uses.

```
MSBuild Property Set → Source Generator Reads Property →
  XmlnsDefinition Attributes Scanned → Namespace Mappings Built →
    XmlReader Configured → XAML Parsed Successfully
```

## Type Resolution Order (Data Flow)

When resolving an unprefixed type name `Foo`:

1. Search `PresentationNamespaces` (framework types: `Microsoft.UI.Xaml.Controls.Foo`, etc.)
2. Search global namespace CLR namespaces (user/library types registered to global URI)
3. If found in exactly one namespace → resolved
4. If found in multiple → compiler error with disambiguation guidance
5. If not found → standard error (type not found)
