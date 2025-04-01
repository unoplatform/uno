---
uid: Uno.Contributing.Tests.PowerShell-Pester-Tests
---

# Testing PowerShell Scripts with Pester

## Introduction

Some Parts of Uno.UI like the [Toc Checker](xref:Uno.Contributing.check-toc) are written in PowerShell. To make sure, they are working properly, the recommended and most common used Testing Framework is [Pester](https://pester.dev/docs/quick-start).

## Showing Pester Test Result in CI

Pester integrates seamlessly with CI pipelines, allowing you to automate the validation of your PowerShell scripts. By including Pester tests in your CI configuration, you can ensure that changes to the `check_toc.ps1` script do not introduce regressions. Most CI systems, such as GitHub Actions or Azure Pipelines, support running Pester tests and can display detailed test results in their interfaces.

For more information, refer to the [Pester Test Results Documentation](https://pester.dev/docs/usage/test-results).

## Related Pages

- [The Uno docs website and DocFX](xref:Uno.Contributing.docfx)
- [Validating the Table of Contents](xref:Uno.Contributing.check-toc)
