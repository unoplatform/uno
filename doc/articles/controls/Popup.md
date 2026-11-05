---
uid: Uno.Controls.Popup
---

# Popup

## Popups that leave the window

As in WinUI, a popup with `ShouldConstrainToRootBounds="False"` isn't limited to its window: it gets a top-level window of its own, placed by its position in your content. `MenuFlyout`, `ComboBox`, `CommandBarFlyout`, `AutoSuggestBox` and `ToolTip` do this by default, and they're placed against the monitor they open on.

| Platform | Popups can leave the window |
|---|---|
| Windows desktop (Vulkan, OpenGL, software rendering) | Yes |
| Linux (X11), macOS | Not yet: they stay inside the window |
| WebAssembly, Android, iOS | No: they stay inside the window |

The window never takes the focus or activation from your window: the keyboard stays on your window, and pointer input over the popup goes through your window's input handling. It follows your window when that moves, and closes with it.

Some limits apply to a popup with a window of its own:

- A native element, such as a `WebView2` or a hosted native control, isn't shown inside it.
- `Popup.SystemBackdrop` isn't supported.
- On a monitor with another scale than your window's, its content keeps your window's scale, as in WinUI.

## Customizing light-dismiss overlay

If you want to show a dimmed overlay underneath the popup, set the `Popup.LightDismissOverlayMode` property to `On`.

If you wish to customize the overlay color, add the following to your top-level `App.Resources`:

```xml
<SolidColorBrush x:Key="PopupLightDismissOverlayBackground"
                 Color="Red" />
```
