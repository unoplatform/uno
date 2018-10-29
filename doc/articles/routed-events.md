# Uno Support for _Routed Events_

## Implemented Routed Events

| Routed Event          | Android | iOS  | Wasm   |     |
| --------------------- | ----- | ------ | ------ | --- |
| _tapped events_
| `Tapped `             | Yes   | Yes    | Yes    | [Documentation](https://docs.microsoft.com/uwp/api/windows.ui.xaml.uielement.tapped) |
| `DoubleTapped`        | Yes   | Yes    | No     | [Documentation](https://docs.microsoft.com/uwp/api/windows.ui.xaml.uielement.doubletapped) |
| _focus events_
| `GotFocus`            | Yes   | Yes(1) | Yes(1) | [Documentation](https://docs.microsoft.com/uwp/api/windows.ui.xaml.uielement.gotfocus) |
| `LostFocus`           | Yes   | Yes(1) | Yes(1) | [Documentation](https://docs.microsoft.com/uwp/api/windows.ui.xaml.uielement.lostfocus) |
| _keyboard events_
| `KeyDown`             | No(3) | Yes(3) | Yes    | [Documentation](https://docs.microsoft.com/uwp/api/windows.ui.xaml.uielement.keydown) |
| `KeyUp`               | No(3) | Yes(3) | Yes    | [Documentation](https://docs.microsoft.com/uwp/api/windows.ui.xaml.uielement.keyup) |
| _pointerd events_
| `PointerCanceled`     | Yes   | No     | Yes    | [Documentation](https://docs.microsoft.com/uwp/api/windows.ui.xaml.uielement.pointercanceled) |
| `PointerCaptureLost`  | Yes   | No     | Yes    | [Documentation](https://docs.microsoft.com/uwp/api/windows.ui.xaml.uielement.pointercapturelost) |
| `PointerEntered`      | Yes   | No     | Yes    | [Documentation](https://docs.microsoft.com/uwp/api/windows.ui.xaml.uielement.pointerentered) |
| `PointerExited`       | Yes   | No     | Yes    | [Documentation](https://docs.microsoft.com/uwp/api/windows.ui.xaml.uielement.pointerexited) |
| `PointerMoved`        | Yes   | No     | Yes    | [Documentation](https://docs.microsoft.com/uwp/api/windows.ui.xaml.uielement.pointermoved) |
| `PointerPressed`      | Yes   | No     | Yes    | [Documentation](https://docs.microsoft.com/uwp/api/windows.ui.xaml.uielement.pointerpressed) |
| `PointerReleased`     | Yes   | No     | Yes    | [Documentation](https://docs.microsoft.com/uwp/api/windows.ui.xaml.uielement.pointerreleased) |

Notes:

2. **Focus** events::
   * iOS: The concept of _focus_ is emulated because not supported by the platform.
   * Wasm: Current implementation is not totally reliable and doesn't support _lost focus_ most of the time.
3. **Keyboard** events:
   * Android: `KeyDown` & `KeyUp` are not properly implemented for Android.
   * iOS: `KeyDown` & `KeyUp` events are generated from only a `TextBox`. Only characted-related keyboard events are generated.

### Unsupported _Routed Events_

These _routed events_ are not supported right now in Uno:

* `DragEnter`
* `DragLeave`
* `DragOver`
* `Drop`
* `Holding`
* `ManipulationCompleted`
* `ManipulationDelta`
* `ManipulationInertiaStarting`
* `ManipulationStarted`
* `ManipulationStarting`
* `PointerWheelChanged`
* `RightTapped`
