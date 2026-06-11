---
uid: Uno.Contributing.macOS
---

# How Uno works on macOS

macOS is supported through the Skia rendering backend. The native macOS target (where `UIElement` inherited from `NSView`) has been removed. macOS now uses the same Skia-based rendering pipeline as other desktop platforms (Windows and Linux).

For details on how the Skia backend works, see the [overview article](uno-internals-overview.md).

## The `.iOSmacOS.cs` suffix

Some files in the codebase use the `*.iOSmacOS.cs` suffix. This suffix predates the removal of the native macOS and Mac Catalyst targets and is now retained for historical reasons. These files are compiled when targeting `net10.0-ios`.
