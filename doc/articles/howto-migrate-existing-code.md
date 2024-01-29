---
uid: Uno.Development.MigrateExistingCode
---

# Migrating single-platform WinUI code to Uno

Uno Platform takes WinUI code and makes it run on almost any target platform. If you have an existing application or library that was written to target WinUI, you'll find guidance here for converting it to a cross-platform codebase, using WinUI to target Windows 10 and Uno Platform to target other platforms. Depending on the APIs your code uses, it may run with minimal modifications, or you may need to add [platform-specific code](platform-specific-csharp.md) to cover missing functionality.

The articles in this guide cover:

- Initial [checklist](migrating-before-you-start.md)
- Steps for [migrating an application](migrating-apps.md)
- Steps for [migrating a class library](migrating-libraries.md)
- [General guidance](migrating-guidance.md) for converting WinUI-only code to Uno-compatible code

See also the [guide to working with cross-targeted class libraries](cross-targeted-libraries.md).
