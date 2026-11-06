---
uid: Uno.Controls.Popup
---

# Popup

## Popups that leave the window

As in WinUI, a popup with `ShouldConstrainToRootBounds="False"` isn't limited to its window: it gets a top-level window of its own, placed by its position in your content. `MenuFlyout`, `ComboBox`, `CommandBarFlyout`, `AutoSuggestBox` and `ToolTip` do this by default, and they're placed against the monitor they open on.

| Platform | Popups can leave the window |
|---|---|
| Windows desktop (Vulkan, OpenGL, software rendering) | Yes |
| Linux (X11; Vulkan, OpenGL, OpenGL ES, software rendering) | Yes. Requires XInput 2.2 and the Shape extension |
| macOS (Metal, software rendering) | Off by default, and not validated yet. Set `FeatureConfiguration.Popup.UseWindowedPopups = true` to try it |
| WebAssembly, Android, iOS | No: they stay inside the window |

To keep every popup inside its window, including those of `ComboBox` and `ToolTip`, set `FeatureConfiguration.Popup.UseWindowedPopups = false` before the first popup opens.

The window never takes the focus or activation from your window: the keyboard stays on your window, and pointer input over the popup goes through your window's input handling. It follows your window when that moves, and closes with it.

Some limits apply to a popup with a window of its own:

- A native element, such as a `WebView2` or a hosted native control, isn't shown inside it.
- `Popup.SystemBackdrop` isn't supported.
- On a monitor with another scale than your window's, its content keeps your window's scale, as in WinUI.
- On X11, transparency needs a running compositor. Without one, the window is cut to the popup's rectangle: there is no drop shadow, and rounded corners show dark pixels.
- On X11, the window is override-redirect, so the window manager doesn't know of it. It follows your window's move, focus and minimize, but raising your window without focusing it, or a window manager's own workspace switch that doesn't unmap your window, can leave it out of step until the next focus change.
- On X11, a press on the popup grabs the pointer until release, as a menu does. The grab is skipped under a managed debugger, because a grab held at a breakpoint locks the desktop. Set the environment variable `UNO_X11_DISABLE_POPUP_GRAB=1` to skip it under a native debugger too.
- On X11, a popup is dismissed by a press in another app's window because that takes the focus from your window. A press on a desktop that takes no focus doesn't dismiss it.
- On macOS, the shadow's gutter takes pointer input where it overlaps another app's window. On X11 and Windows it's passed through where possible.
- Native elements, and drag and drop onto the popup from outside the app, aren't supported in a popup's window on X11.

## Customizing light-dismiss overlay

If you want to show a dimmed overlay underneath the popup, set the `Popup.LightDismissOverlayMode` property to `On`.

If you wish to customize the overlay color, add the following to your top-level `App.Resources`:

```xml
<SolidColorBrush x:Key="PopupLightDismissOverlayBackground"
                 Color="Red" />
```
