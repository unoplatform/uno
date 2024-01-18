---
uid: Uno.Features.Cursors
---

# Using pointer cursors

You can change the pointer cursor in your application at runtime on WebAssembly, macOS, GTK and WPF by setting the `CoreWindow.PointerCursor` property:

```csharp
var handCursorType = Windows.UI.Core.CoreCursorType.Hand;
var cursor = new Windows.UI.Core.CoreCursor(handCursorType, 0);
CoreWindow.GetForCurrentThread().PointerCursor = cursor;
```

Note that you must perform this action on the UI thread.

## Changing the default cursor for button-based controls on WASM

To provide a web-like feel of Uno Platform WebAssembly apps, we use the "hand" cursor for "interactive" controls (currently including controls derived from `ButtonBase` and `ToggleSwitch` control). This makes the experience feel familiar to users. If you want your application to feel more like a native application, you can set the `FeatureConfiguration.Cursors.UseHandForInteraction` to `false`:

```csharp
Uno.UI.FeatureConfiguration.Cursors.UseHandForInteraction = false;
```

Make sure to set this property early in the application lifecycle, before the window content is first set, for example at the beginning of the `Main` method in `Program.cs`, which is located in the WASM project.
