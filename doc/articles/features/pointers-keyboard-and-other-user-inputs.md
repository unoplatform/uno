---
uid: Uno.Features.UserInputs
---

# Uno support for user inputs

## Supported user inputs

User inputs are usually propagated using `RoutedEvents`. See Uno's [routed events documentation](routed-events.md) to better understand their implementation on Uno.

| Routed Event                  | Android | iOS     | Wasm    | macOS | Skia WPF | Skia GtK | Tizen |     |
| ----------------------------- | ------- | ------- | ------- | ----- | -------- | -------- | ----- | --- |
| **_focus events_**
| `GotFocus`                    | Yes     | Yes (1) | Yes (1) | ?     | Yes      | Yes      | Yes   | [Documentation](https://docs.microsoft.com/uwp/api/windows.ui.xaml.uielement.gotfocus) |
| `LostFocus`                   | Yes     | Yes (1) | Yes (1) | ?     | Yes      | Yes      | Yes   | [Documentation](https://docs.microsoft.com/uwp/api/windows.ui.xaml.uielement.lostfocus) |
| **_keyboard events_**
| `KeyDown`           | Hardware Only (2) | Yes (2) | Yes     | Yes   | Yes      | Yes      | Yes   | [Documentation](https://docs.microsoft.com/uwp/api/windows.ui.xaml.uielement.keydown) |
| `KeyUp`             | Hardware Only (2) | Yes (2) | Yes     | Yes   | Yes      | Yes      | Yes   | [Documentation](https://docs.microsoft.com/uwp/api/windows.ui.xaml.uielement.keyup) |
| **_pointer events_**
| `PointerCanceled`             | Yes     | Yes     | Yes     | Yes   | Yes      | Yes      | Yes   | [Documentation](https://docs.microsoft.com/uwp/api/windows.ui.xaml.uielement.pointercanceled) |
| `PointerCaptureLost`          | Yes     | Yes     | Yes     | Yes   | Yes      | Yes      | Yes   | [Documentation](https://docs.microsoft.com/uwp/api/windows.ui.xaml.uielement.pointercapturelost) |
| `PointerEntered`              | Yes     | Yes     | Yes     | Yes   | Yes      | Yes      | Yes   | [Documentation](https://docs.microsoft.com/uwp/api/windows.ui.xaml.uielement.pointerentered) |
| `PointerExited`               | Yes     | Yes     | Yes     | Yes   | Yes      | Yes      | Yes   | [Documentation](https://docs.microsoft.com/uwp/api/windows.ui.xaml.uielement.pointerexited) |
| `PointerMoved`                | Yes     | Yes     | Yes     | Yes   | Yes      | Yes      | Yes   | [Documentation](https://docs.microsoft.com/uwp/api/windows.ui.xaml.uielement.pointermoved) |
| `PointerPressed`              | Yes     | Yes     | Yes     | Yes   | Yes      | Yes      | Yes   | [Documentation](https://docs.microsoft.com/uwp/api/windows.ui.xaml.uielement.pointerpressed) |
| `PointerReleased`             | Yes     | Yes     | Yes     | Yes   | Yes      | Yes      | Yes   | [Documentation](https://docs.microsoft.com/uwp/api/windows.ui.xaml.uielement.pointerreleased) |
| `PointerWheelChanged`         | No      | No      | Yes     | Yes   | Yes      | Yes      | No    | [Documentation](https://docs.microsoft.com/uwp/api/windows.ui.xaml.uielement.pointerwheelchanged) |
| **_manipulation events_**
| `ManipulationStarting`        | Yes     | Yes     | Yes     | Yes   | Yes      | Yes      | Yes   | [Documentation](https://docs.microsoft.com/uwp/api/windows.ui.xaml.uielement.manipulationstarting) |
| `ManipulationStarted`         | Yes     | Yes     | Yes     | Yes   | Yes      | Yes      | Yes   | [Documentation](https://docs.microsoft.com/uwp/api/windows.ui.xaml.uielement.manipulationstarted) |
| `ManipulationDelta`           | Yes     | Yes     | Yes     | Yes   | Yes      | Yes      | Yes   | [Documentation](https://docs.microsoft.com/uwp/api/windows.ui.xaml.uielement.manipulationedelta) |
| `ManipulationInertiaStarting` | Yes     | Yes     | Yes     | Yes   | Yes      | Yes      | Yes   | [Documentation](https://docs.microsoft.com/uwp/api/windows.ui.xaml.uielement.manipulationinertiastarting) |
| `ManipulationCompleted`       | Yes     | Yes     | Yes     | Yes   | Yes      | Yes      | Yes   | [Documentation](https://docs.microsoft.com/uwp/api/windows.ui.xaml.uielement.manipulationcompleted) |
| **_gesture events_**
| `Tapped`                      | Yes     | Yes     | Yes     | Yes   | Yes      | Yes      | Yes   | [Documentation](https://docs.microsoft.com/uwp/api/windows.ui.xaml.uielement.tapped) |
| `DoubleTapped`                | Yes     | Yes     | Yes     | Yes   | Yes      | Yes      | Yes   | [Documentation](https://docs.microsoft.com/uwp/api/windows.ui.xaml.uielement.doubletapped) |
| `RightTapped`                 | Yes     | Yes     | Yes     | Yes   | Yes      | Yes      | Yes   | [Documentation](https://docs.microsoft.com/uwp/api/windows.ui.xaml.uielement.righttapped) |
| `Holding`                     | Yes     | Yes     | Yes     | Yes   | Yes      | Yes      | Yes   | [Documentation](https://docs.microsoft.com/uwp/api/windows.ui.xaml.uielement.holding) |
| **_drag and drop_**
| `DragStarting`                | Yes     | Yes     | Yes     | Yes   | Yes      | Yes      | Yes   | [Documentation](https://docs.microsoft.com/uwp/api/windows.ui.xaml.uielement.dragstarting) |
| `DragEnter`                   | Yes     | Yes     | Yes     | Yes   | Yes      | Yes      | Yes   | [Documentation](https://docs.microsoft.com/uwp/api/windows.ui.xaml.uielement.dragenter) |
| `DragOver`                    | Yes     | Yes     | Yes     | Yes   | Yes      | Yes      | Yes   | [Documentation](https://docs.microsoft.com/uwp/api/windows.ui.xaml.uielement.dragover) |
| `DragLeave`                   | Yes     | Yes     | Yes     | Yes   | Yes      | Yes      | Yes   | [Documentation](https://docs.microsoft.com/uwp/api/windows.ui.xaml.uielement.dragleave) |
| `Drop`                        | Yes     | Yes     | Yes     | Yes   | Yes      | Yes      | Yes   | [Documentation](https://docs.microsoft.com/uwp/api/windows.ui.xaml.uielement.drop) |
| `DropCompleted`               | Yes     | Yes     | Yes     | Yes   | Yes      | Yes      | Yes   | [Documentation](https://docs.microsoft.com/uwp/api/windows.ui.xaml.uielement.dropcompleted) |

Notes:

1. **Focus** events:
   * **iOS**: The concept of _focus_ is emulated because it's not supported by the platform, so this event is
     always bubbling in managed code.
   * **Wasm**: Current implementation is not totally reliable and doesn't support _lost focus_ most of the time.
2. **Keyboard** events:
   * **Android**: `KeyDown` and `KeyUp` events are **generated only from hardware keyboards** (Except for the _Editor Action_ on _soft keyboards_,
     those are translated as `KeyUp` with `KeyCode.Enter`). Some soft keyboard **MAY** generate those events, but your code shouldn't rely
     on that. This is a limitation [in the Android platform](https://developer.android.com/training/keyboard-input/commands) (see note on this link content).
     > Because of those limitations, _Key Events_ are not being implemented as _routed events_ on Android, so `AddHandler` & `RemoveHandler`
     > won't work for keyboard events. **They won't bubble in managed code**.
   * **iOS**: `KeyDown` & `KeyUp` routed events are generated from only a `TextBox`. Only character-related keyboard events are generated.
     They are implemented as _Routed Events_ and they are **always bubbling in managed code**.
   * **Skia**: Keyboard events are supported from `CoreWindow.KeyUp` and `CoreWindow.KeyDown` events, as well as `UIElement.KeyUp` and `UIElement.KeyDown` events for GTK, WPF and Tizen.

## Pointer Events

These events are the base for all other pointing device related events (i.e. Manipulation, Gesture and drag and dop events).
They are directly linked to the native events of each platform:

* `Touches[Began|Moved|Ended|Cancelled]` on iOS
* `dispatchTouchEvent` and `dispatchGenericMotionEvent` on Android
* `pointer[enter|leave|down|up|move|cancel]` on WebAssembly

On Skia however, they are fully managed events.

### Pointers events and the ScrollViewer

Like on WinUI as soon as the system detects that the user wants to scroll, a control gets a `PointerCancelled` and that control won't receive
any other pointer event until the user releases the pointer. That behavior can be prevented by setting the `ManipulationMode`
to something else than `System` on a control nested in the `ScrollViewer`. (cf. [Manipulation events](#manipulation-events))

Be aware that on iOS this will set `DelaysContentTouches` to `false` so it means that it will slightly reduce the performance
of the scrolling (cf. [documentation](https://developer.apple.com/documentation/uikit/uiscrollview/1619398-delayscontenttouches)).

### Known limitations for pointer events

As those events are tightly coupled to the native events, Uno has to make some compromises:

* On iOS, when tapping with a mouse or a pen on Android, or in few other specific cases (like `PointerCaptureLost`),
  multiple managed events are raised from a single native event. These have multiple effects:
  * On UWP if you have a control A and a nested control B, you will get:

    ```output
    B.PointerEnter
    A.PointerEnter
    B.PointerPressed
    A.PointerPressed
    ```

    but with UNO you will get:

    ```output
    B.PointerEnter
    B.PointerPressed
    A.PointerEnter
    A.PointerPressed
    ```

  * If you handle the `PointerEnter` on **B**, the parent control **A** won't get the `PointerEnter` (as expected) nor the  `PointerPressed`.
* On Android with a mouse or a pen, the `PointerEnter` and `PointerExit` are going to be raised without taking clipping in consideration.
  This means that you will get the enter earlier and the exit later than on other platform.
* On Android if you have an element with a `RenderTransform` which overlaps one of its sibling element,
  the element at the top will get the pointer events.
* On WASM, iOS and Android, the `RoutedPointerEventArgs.FrameId` will be reset to 0 after 49 days of running time of the application.
* Unlike on UWP, controls that are under a `Popup` won't receive the unhandled pointer events.
* On non Skia-based platforms, unlike UWP, it's impossible to receive a `PointerReleased` without getting a `PointerPressed` before.
  (For instance if a child control handled the pressed event but not the released event).
  > On WASM as `TextElement` inherits from `UIElement`, it means that unlike UWP `TextBlock` won't raise the
  > `PointerReleased` event when clicking on a `Hyperlink`.
* Unlike UWP, on the `Hyperlink` the `Click` will be raised before the `PointerReleased`.
* The property `PointerPointProperties.PointerUpdateKind` is not set on Android 5.x and lower (API level < 23)
* On Firefox, pressed pointers are reported as fingers. This means you will receive events with `PointerDeviceType == Pen` only for hovering
  (i.e. `Pointer<Enter|Move|Exit>` - note that, as of 2019-11-28, once pressed `PointerMove` will be flagged as "touch")
  and you won't  be able to track the barrel button nor the eraser. (cf. https://bugzilla.mozilla.org/show_bug.cgi?id=1449660)
* On WASM, if you touch the screen with the pen **then** you press the barrel button (still while touching the screen), the pointer events will
  have the `IsRightButtonPressed` set (in addition of the `IsBarrelButtonPressed`). On WinUI and Android you get this flag only if the barrel
  button was pressed at the moment where you touched the screen, otherwise you will have the `IsLeftButtonPressed` and the `IsBarrelButtonPressed`.
* For pen and fingers, the `Holding` event is not raised after a given delay like on WinUI,
  but instead we rely on the fact that we usually get a lot of moves for those kind of pointers,
  so we raise the event only when we get a move that exceed defined thresholds for holding.
* On WASM, Shapes must have a non null Fill to receive pointer events (setting the Stroke is not sufficient).
* On WASM if the user scrolls in diagonal (e.g. with a Touchpad), but you mark as `Handled` pointer events only for vertical scrolling,
  then the events for the horizontal scroll component won't bubble through the parents.

### Pointer capture

The capture of pointer is handled in managed code only. On WebAssembly Uno however still requests the browser to capture the pointer,
but Uno does not rely on native `[got|lost]pointercapture` events.

### iPadOS mouse support

To differentiate between mouse and touch device type for pointer events, include the following in your app's `Info.plist`:

```xml
  <key>UIApplicationSupportsIndirectInputEvents</key>
  <true/>
```

Without this key the current version of iPadOS reports mouse interaction as normal touch.

## Manipulation Events

They are generated from the PointerXXX events (using the `Windows.UI.Input.GestureRecognizer`) and are bubbling in managed only.

## Gesture Events

They are generated from the PointerXXX events (using the `Windows.UI.Input.GestureRecognizer`) and are bubbling in managed only.

Note that `Tapped` and `DoubleTapped` are not linked in any way to a native equivalent, but are fully interpreted in managed code.

In order to match the WinUI behavior, on WASM the default "Context menu" of the browser is disabled (except for the `TextBox`),
no matter if you use / handle the `RightTapped` event or not.
Be aware that on some browser (Firefox), user can still request to get the "Context menu" on right click.

### Disabling browser context menu on `<input>-based` elements

While the browser context menu enabled on `TextBox` and `PasswordBox` by default, it will be disabled when `ContextFlyout` is set on the control.

To manually disable the context menu on a `UIElement` which represents a HTML `<input>`, you can manually set the `context-menu-disabled` CSS class:

```csharp
#if __WASM__

MyInputElement.SetCssClasses("context-menu-disabled");
#endif
```

## Drag and drop

Those events are also 100% managed events, built from the PointerXXX events (using the `Windows.UI.Input.GestureRecognizer`)

### Inter-app drag and drop support

A _drag and drop_ operation can be used to move content within an app, but it can also be used to **copy** / **move** / **link** between apps.
While intra-app _drag and drop_ is supported on all platforms without limitations, inter-app _drag and drop_ requires platform specific support.
The table and sections below describe supported functionality and limitations for each platform.

|          | From uno app to external                         | From external app to uno                        |
| -------- | ------------------------------------------------ | ------------------------                        |
| Android  | No                                               | No                                              |
| iOS      | No                                               | No                                              |
| Wasm     | No                                               | Yes (Text, Link, Image, File, Html, Rtf)        |
| macOS    | Yes (Text, Link, Image, Html, Rtf)               | Yes (Text, Link, Image, File, Html, Rtf)        |
| Skia WPF | Yes (Text, Link, Image, File, Html, Rtf)         | Yes (Text, Link, Image, File, Html, Rtf)        |
| Skia GTK | No                                               | No                                              |

* "Link" may refer to WebLink, ApplicationLink or Uri formats

#### Wasm Limitations

1. When dragging content from external app to uno, you cannot retrieve the content from the `DataPackage` before the `Drop` event.
   This a limitations of web browsers.
   Any attempt to read it before the `Drop` will result into a timeout exception after a hard coded delay of 10 seconds.
2. When dragging some uris from external app to uno, only the first uri will be accessible through the **WebLink** standard format ID.

#### macOS Limitations

1. Dragging a File (StorageItem) from an Uno Platform App to an external destination is not currently supported.
2. When receiving a drop within an Uno Platform App from an external source, key modifiers are not supported

#### Skia Limitations

1. There is no standard type for **WebLink** (nor **ApplicationLink**) on this platform.
   They are copied to the external app as raw **Text**, and converted back as **WebLink** or **ApplicationLink**) from raw text from the external app
   when [`Uri.IsWellFormedUriString(text, UriKind.Absolute)](https://learn.microsoft.com/dotnet/api/system.uri.iswellformeduristring) returns true.
2. The image content seems to not being readable by common apps, only another Uno app is able to read it properly.

### Drag and Drop Data Format Considerations

UWP has the following standard data formats that correspond with a URI/URL:

1. Uri, now deprecated in favor of:
2. WebLink and
3. ApplicationLink

Several platforms such as macOS/iOS/Android do not differentiate between them and only use a single URI/URL class or string.

When applying data to the native clipboard or drag/drop data from a DataPackage, only one URI/URL may be used. Therefore, all URI data formats are merged together into a single URI using the above defined priority. WebLink is considered more specific than ApplicationLink.

When pulling data from the native clipboard or drag/drop data, this single native URI/URL is separated into the equivalent UWP data format (since UWP's direct equivalent standard data format 'Uri' is deprecated). The mapping is as follows:

1. WebLink is used if the given URL/URI has a scheme of "http" or "https"
2. ApplicationLink is used if not #1

For full compatibility, the Uri format within a DataPackage should still be populated regardless of #1 or #2.

### Known issues for drag and drop events

1. If you have 2 nested drop targets (i.e. element flagged with `AllowDrop = true`),
   when the pointer leaves the deepest / top most element but not the parent,
   the parent element will also raise `DragLeave` and immediately after raise `DragEnter`.
1. On UWP, the default UI will include a tooltip which indicates the accepted drop action,
   and a "screenshot" of the dragged element.
   Currently Uno will display only the tooltip.
1. The accepted drop action displayed in the tooltip is not localized.
