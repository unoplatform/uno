---
uid: Uno.Features.AppleTv
---

# Support for Apple TV (tvOS)

Uno Platform apps run on Apple TV through the `net10.0-tvos` target framework. Because Uno Platform draws the whole UI itself, the native tvOS focus engine has no views to move between. Instead, the Siri Remote is translated into the same gamepad and remote input that WinUI uses on Xbox, so focus, activation and back navigation go through the regular Uno Platform focus and keyboard pipeline.

Remote support is always on for tvOS heads; there is nothing to opt into.

## Remote control support

To navigate the focus with the remote, make sure `XYFocusKeyboardNavigation` is `Enabled` on your pages, exactly as for [Android TV](xref:Uno.Features.AndroidTv). The same XAML works on both:

```xml
<Page xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
      XYFocusKeyboardNavigation="Enabled">
    <!-- Your app's UI elements go here -->
</Page>
```

The Siri Remote is mapped as follows:

| Siri Remote input | `VirtualKey` raised | Default behavior |
|---|---|---|
| Swipe up/down/left/right on the touch surface | `GamepadDPadUp`/`Down`/`Left`/`Right` | Moves focus one step in that direction |
| Click on the edge of the clickpad (arrow press) | `GamepadDPadUp`/`Down`/`Left`/`Right` | Moves focus one step in that direction |
| Click the center of the clickpad (Select) | `GamepadA` | Activates the focused control, like `Space` |
| Menu / Back | `GamepadB` | Closes the topmost light-dismiss popup or flyout, like `Escape`; otherwise raises `SystemNavigationManager.BackRequested` |
| Play/Pause | `179` (media Play/Pause) | None, handle it in `KeyDown` |

As on Xbox, `KeyRoutedEventArgs.OriginalKey` carries the gamepad key while `KeyRoutedEventArgs.Key` carries the equivalent keyboard key (`GamepadA` is `Space`, `GamepadB` is `Escape`, the D-pad keys are the arrow keys), so controls that already support the keyboard work with the remote.

### Back navigation

A Menu press that no element handles in `KeyDown` raises `SystemNavigationManager.BackRequested`. When that is not handled either, the press is passed on to tvOS, which returns to the Apple TV home screen. This follows the Apple guideline that Menu on the root screen of an app exits it:

```csharp
SystemNavigationManager.GetForCurrentView().BackRequested += (s, e) =>
{
    if (rootFrame.CanGoBack)
    {
        rootFrame.GoBack();
        e.Handled = true; // Leave it unhandled on the root page so Menu exits the app.
    }
};
```

### Initial focus

Like an Xbox app, a tvOS app treats the remote as the last used input device from startup. The first focusable element of the app gets keyboard focus, with its focus visual, without the user having to press anything first.

### Control defaults

Controls that adapt to Xbox in WinUI treat tvOS the same way:

- `NavigationView` hides its back button when `IsBackButtonVisible` is `Auto`, as Menu already goes back. Set it to `Visible` to show it anyway.
- `SplitView`, `AppBar` and `CommandBar` show their light-dismiss overlay when `LightDismissOverlayMode` is `Auto`.

### Touch surface

The touch surface of the Siri Remote is not a pointer: its touches are not raised as pointer events. Swipes move the focus, and clicks are reported as the presses above.

## Limitations

- Holding a direction does not repeat the focus move.
- A hardware keyboard connected to the Apple TV only works as far as tvOS reports its keys as remote presses.
- Gamepads connected to the Apple TV can be read through `Windows.Gaming.Input.Gamepad`, but they do not move the focus.
