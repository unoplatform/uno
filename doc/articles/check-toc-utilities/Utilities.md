---
uid: Uno.Contributing.check-toc.Utilities
---
# Utility Functions for the TOC Checker

The utility functions used by the `check_toc.ps1` script follow SoC principles to improve modularity, maintainability, and testability. These helpers are dynamically imported during script execution via the [`Import-TocCheckerUtils.ps1`](./Import-TocCheckerUtils.ps1) script and encapsulate common logic such as file enumeration, header parsing, and output generation.

> [!TIP]
> Keeping utility functions well-documented and separated from the main script logic helps contributors understand, maintain, and extend the tooling efficiently.

For an overview of how these utilities integrate with the main script, see the [TOC Checker Script Overview](xref:Uno.Contributing.check-toc.Overview).

## Coding Standards

Adhere to the project’s coding standards to ensure consistency, readability across contributions.

## CmdletBinding Usage

Use [`[CmdletBinding()]`](https://learn.microsoft.com/de-de/powershell/module/microsoft.powershell.core/about/about_functions_cmdletbindingattribute?view=powershell-7.5) in all advanced functions. This:

- Enables the use of the `-Verbose` flag for detailed logging
- Integrates better with script pipelines and error handling
- Improves clarity when debugging or reviewing logs

By following these practices, utility scripts remain robust, predictable, and easier to support.

<!-- TODO ## Creating Utilities for Toc Checker -->
<!-- TODO  ### Installing PowerShell 7+ https://github.com/PowerShell/PowerShell -->

## Related Pages

- [TOC Checker Script Overview](xref:Uno.Contributing.check-toc.Overview)
- [The Uno docs website and DocFX](xref:Uno.Contributing.DocFx)

## 🌐 Further Reading

- [!INCLUDE [Clean Architecture Principles Inline](../includes/clean-architecture-principles-inline.md)]
