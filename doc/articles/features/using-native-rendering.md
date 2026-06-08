---
uid: uno.features.renderer.native
---

# Native rendering (removed in Uno Platform 7.0)

> [!IMPORTANT]
> The native UI rendering backends (native Android Views, native iOS/tvOS/macCatalyst
> UIKit, and the native WebAssembly DOM renderer) were **removed in Uno Platform 7.0**.
> **Skia is now the single rendering engine on every target.**

There is no longer a choice of renderer: every Uno Platform target renders the WinUI
visual tree through Skia. Each `UIElement` is a managed object backed by a
`Composition.Visual`, drawn into a single Skia surface — there is no parallel native view
tree, and no per-element DOM on WebAssembly.

To embed native platform controls alongside the Skia-rendered tree, see
[Incorporating native views to the Uno visual tree](xref:Uno.Development.NativeViews).

If you are upgrading an app that relied on native rendering, follow
[Migrating to Uno Platform 7.0](xref:Uno.Development.MigratingToUno7).
