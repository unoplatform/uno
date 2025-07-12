---
uid: Uno.Contributing.Tests.PowerShell-Pester-Tests
---

# Testing PowerShell Scripts with Pester

## Introduction

Some Parts of Uno.UI like the [check-toc.ps1](xref:Uno.Contributing.docfx) or the [import_external_docs_test.ps1](../../../import_external_docs_test.ps1) used for Contribution and general Uno Documentation deployment, are written in PowerShell. To make sure, they are working properly, the recommended and most common used Testing Framework is [Pester](https://pester.dev/docs/quick-start).

<!-- TODO: ### Pester List of the Commonly used Commands  -->
<!-- TODO: ### Mocking File Content to test with Pester (like uid:) -->

## Showing Pester Test Result in CI

Pester integrates seamlessly with CI pipelines, allowing you to automate the validation of your PowerShell scripts. By including Pester tests in your CI configuration, you can ensure that changes to the script any dependant Modules or Functions do not introduce regressions. Most CI systems, such as GitHub Actions or Azure Pipelines, support running Pester tests and can display detailed test results in their interfaces.

For more information, refer to the [Pester Test Results Documentation](https://pester.dev/docs/usage/test-results).

## Related Pages

- [The Uno docs website and DocFX](xref:Uno.Contributing.docfx)
- [`Installing Pester 5+`](https://pester.dev/docs/introduction/installation)
