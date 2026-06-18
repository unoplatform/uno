---
uid: Uno.Development.MigratingToUno7
---

# Migrating to Uno Platform 7.0

Uno Platform 7.0 makes [Skia](xref:uno.features.renderer.skia) the single rendering backend on every target. The legacy **native renderer** (native Android Views, native iOS/UIKit, and the WebAssembly DOM renderer) is removed, and the NuGet packages that only existed to ship it are no longer produced.

This does **not** drop platform support: Android, iOS, macOS, Windows, Linux, and WebAssembly all remain supported — they now all render with Skia.

## Native renderer removal

The `NativeRenderer` Uno Feature and the renderer-selection logic are gone. Skia is always used, so:

- Remove `NativeRenderer` from `<UnoFeatures>` if it is present.
- `SkiaRenderer` is now implied on every head. It is kept as a no-op for back-compat, so you can leave it in `<UnoFeatures>` or remove it — either way Skia is used.

If your project was created before Uno Platform 6.0 and still selects a renderer, follow the [Uno 6.0 migration guide](xref:Uno.Development.MigratingToUno6) first to move to the Uno.SDK single-project model.

## Removal of native-only NuGet packages

The following package no longer ships, because it only carried the WebAssembly DOM renderer:

| Removed package | Replacement |
|-----------------|-------------|
| `Uno.WinUI.WebAssembly` | `Uno.WinUI.Runtime.Skia.WebAssembly.Browser` (the Skia browser head) |

When using the `Uno.SDK`, the Skia browser head is referenced implicitly — there is nothing to add. If you have an explicit reference in a WebAssembly head, remove it:

```diff
- <PackageReference Include="Uno.WinUI.WebAssembly" Version="..." />
```

`Uno.WinUI` no longer carries the native `net*-android`, `net*-ios`, and `net*-tvos` rendering assemblies either. Skia-on-Android/iOS consumes the cross-platform `Uno.WinUI` assembly, so no package change is required on those heads.

> [!NOTE]
> Referencing `Uno.WinUI.WebAssembly` (or the older `Uno.WinUI.Runtime.WebAssembly`) alongside the Skia browser head raises the `UNOB0017` build diagnostic. Removing the explicit reference resolves it.

## Native image loading (Android)

The native renderer used `Uno.UniversalImageLoader` for image loading on Android. Skia handles image loading internally, so this package is no longer injected. If you initialized it manually, remove that code:

```diff
- ConfigureUniversalImageLoader();
```

See the [Uno 6.0 migration guide](xref:Uno.Development.MigratingToUno6#optional-use-of-skia-rendering-for-ios-android-and-webassembly) for the full Android/iOS/WebAssembly Skia bootstrapping steps.
