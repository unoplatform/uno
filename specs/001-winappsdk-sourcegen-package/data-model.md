# Data Model: WinAppSDK Source Generator Package

**Date**: 2026-03-13
**Feature**: 001-winappsdk-sourcegen-package

## Entities

### XamlClassInfo (record struct)

Represents parsed `x:Class` information from a XAML file.

| Field                  | Type     | Description                                                              |
|------------------------|----------|--------------------------------------------------------------------------|
| FullClassName          | string   | Fully-qualified class name (e.g., `MyApp.MainPage`)                      |
| Namespace              | string   | Namespace portion (e.g., `MyApp`)                                        |
| ClassName              | string   | Class name portion (e.g., `MainPage`)                                    |
| RootElementName        | string   | XAML root element local name (e.g., `Page`)                              |
| RootElementNamespace   | string   | XAML root element XML namespace URI                                      |
| BaseTypeFullName       | string   | Resolved fully-qualified base type (e.g., `Microsoft.UI.Xaml.Controls.Page`) |

### DiagnosticDescriptor (UNOB0001)

| Property            | Value                                                              |
|---------------------|--------------------------------------------------------------------|
| ID                  | `UNOB0001`                                                         |
| Title               | `Invalid x:Class Value`                                            |
| MessageFormat       | `{0}`                                                              |
| Category            | `XAML`                                                             |
| Severity            | Warning                                                            |
| Description         | The x:Class attribute value is malformed and must include a namespace |

### MSBuild Properties

| Property                     | Scope       | Default | Description                                              |
|------------------------------|-------------|---------|----------------------------------------------------------|
| UnoGenerateCodeBehind        | Project     | true    | Global enable/disable for code-behind generation          |
| UnoGenerateCodeBehind        | Per-file    | (none)  | Per-file override via item metadata; takes precedence     |

## Relationships

- One XAML file (`Page` or `ApplicationDefinition`) → zero or one `XamlClassInfo` (via parser)
- One `XamlClassInfo` → one generated `.codebehind.g.cs` source file (via emitter)
- `XamlClassInfo` existence depends on: `x:Class` attribute present AND class not already in compilation
