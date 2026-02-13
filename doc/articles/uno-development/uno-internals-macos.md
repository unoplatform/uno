---
uid: Uno.Contributing.macOS
---

# How Uno works on macOS

macOS is supported through the Skia rendering backend. The native macOS target (where `UIElement` inherited from `NSView`) has been removed. macOS now uses the same Skia-based rendering pipeline as other desktop platforms (Windows and Linux).

For details on how the Skia backend works, see the [overview article](uno-internals-overview.md).

## The `.iOSmacOS.cs` suffix

Some files in the codebase use the `*.iOSmacOS.cs` suffix. This suffix predates the removal of the native macOS target and is now used for code shared between iOS and Mac Catalyst via Apple UIKit. These files are compiled when targeting `net10.0-ios` or `net10.0-maccatalyst`.
