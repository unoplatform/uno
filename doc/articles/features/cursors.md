---
uid: Uno.Features.Cursors
---

# Using pointer cursors

You can change the pointer cursor when the pointer hovers certain elements in your application at runtime on WebAssembly, macOS, or Skia Desktop by subclassing a `UIElement` and setting its protected `ProtectedCursor` property. Alternatively, a workaround with reflection is possible and avoids the need to create custom `UIElement`s:

```csharp
public static void ChangeCursor(this UIElement uiElement, InputCursor cursor)
{
  typeof(UIElement).GetProperty(nameof(ProtectedCursor), BindingFlags.NonPublic | BindingFlags.Instance).SetValue(element, cursor);
}
```

Note that you must perform this action on the UI thread. For more details on how to use `ProtectedCursor`, follow this [discussion](https://github.com/microsoft/WindowsAppSDK/discussions/1816).

Note that the legacy `CoreWindow.PointerCursor` API is no longer supported.

## Changing the default cursor for button-based controls on WASM

To provide a web-like feel of Uno Platform WebAssembly apps, we use the "hand" cursor for "interactive" controls (currently including controls derived from `ButtonBase` and `ToggleSwitch` control). This makes the experience feel familiar to users. If you want your application to feel more like a native application, you can set the `FeatureConfiguration.Cursors.UseHandForInteraction` to `false`:

```csharp
Uno.UI.FeatureConfiguration.Cursors.UseHandForInteraction = false;
```

Make sure to set this property early in the application lifecycle, before the window content is first set, for example at the beginning of the `Main` method in `Program.cs`, which is located in the WASM project.
