---
uid: Uno.Features.Cursors
---

# Using pointer cursors

You can change the pointer cursor when the pointer hovers certain elements in your application at runtime on WebAssembly, macOS, Skia Desktop, or Skia iOS (iPadOS, with a connected mouse or trackpad) by making a subclass of the `UIElement` of interest and setting its protected `ProtectedCursor` property, for example by adding a method in the the subclass.

```csharp
public void ChangeCursor(InputCursor cursor)
{
    this.ProtectedCursor = cursor;
}
```

Note that you must perform this action on the UI thread. For more details on how to use `ProtectedCursor`, follow this [discussion](https://github.com/microsoft/WindowsAppSDK/discussions/1816).

Note that the legacy `CoreWindow.PointerCursor` API is no longer supported.

## Cursors on Skia iOS (iPadOS)

On Skia iOS, cursors require a connected mouse or trackpad and iPadOS 13.4 or later. iPadOS uses an adaptive, content-aware pointer rather than a fixed catalog of cursor images, so the mapping is approximate:

- `Arrow` (and any cursor without a native equivalent) shows the default system pointer.
- `IBeam` shows the text beam.
- `SizeWestEast` and `SizeNorthSouth` show an axis-aligned beam (the closest native equivalent; iPadOS has no dedicated resize cursor).
- A hidden cursor (`ProtectedCursor = null` on a disposed `InputCursor`) hides the pointer.

There is no `Wait`/busy pointer on iPadOS; show an in-content progress indicator instead. Mouse, hover, button and right-click support additionally require the `UIApplicationSupportsIndirectInputEvents` key set to `true` in the iOS app's `Info.plist` (already included in the Uno Platform app templates).

## Changing the default cursor for button-based controls on WASM

To provide a web-like feel of Uno Platform WebAssembly apps, we use the "hand" cursor for "interactive" controls (currently including controls derived from `ButtonBase` and `ToggleSwitch` control). This makes the experience feel familiar to users. If you want your application to feel more like a native application, you can set the `FeatureConfiguration.Cursors.UseHandForInteraction` to `false`:

```csharp
Uno.UI.FeatureConfiguration.Cursors.UseHandForInteraction = false;
```

Make sure to set this property early in the application lifecycle, before the window content is first set, for example at the beginning of the `Main` method in `Program.cs`, which is located in the WASM project.
