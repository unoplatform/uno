# Uno Support for _Routed Events_

## Implemented Routed Events

| Routed Event                  | Android | iOS     | Wasm    |     |
| ----------------------------- | ------- | ------- | ------- | --- |
| _tapped events_               
| `Tapped`                      | Yes (1) | Yes (1) | Yes (1) | [Documentation](https://docs.microsoft.com/uwp/api/windows.ui.xaml.uielement.tapped) |
| `DoubleTapped`                | Yes (1) | Yes (1) | Yes (1) | [Documentation](https://docs.microsoft.com/uwp/api/windows.ui.xaml.uielement.doubletapped) |
| _focus events_                
| `GotFocus`                    | Yes     | Yes (2) | Yes (2) | [Documentation](https://docs.microsoft.com/uwp/api/windows.ui.xaml.uielement.gotfocus) |
| `LostFocus`                   | Yes     | Yes (2) | Yes (2) | [Documentation](https://docs.microsoft.com/uwp/api/windows.ui.xaml.uielement.lostfocus) |
| _keyboard events_             
| `KeyDown`           | Hardware Only (3) | Yes (3) | Yes     | [Documentation](https://docs.microsoft.com/uwp/api/windows.ui.xaml.uielement.keydown) |
| `KeyUp`             | Hardware Only (3) | Yes (3) | Yes     | [Documentation](https://docs.microsoft.com/uwp/api/windows.ui.xaml.uielement.keyup) |
| _pointer events_              
| `PointerCanceled`             | Yes     | Yes     | Yes     | [Documentation](https://docs.microsoft.com/uwp/api/windows.ui.xaml.uielement.pointercanceled) |
| `PointerCaptureLost`          | Yes     | Yes     | Yes     | [Documentation](https://docs.microsoft.com/uwp/api/windows.ui.xaml.uielement.pointercapturelost) |
| `PointerEntered`              | Yes     | Yes     | Yes     | [Documentation](https://docs.microsoft.com/uwp/api/windows.ui.xaml.uielement.pointerentered) |
| `PointerExited`               | Yes     | Yes     | Yes     | [Documentation](https://docs.microsoft.com/uwp/api/windows.ui.xaml.uielement.pointerexited) |
| `PointerMoved`                | Yes     | Yes     | Yes     | [Documentation](https://docs.microsoft.com/uwp/api/windows.ui.xaml.uielement.pointermoved) |
| `PointerPressed`              | Yes     | Yes     | Yes     | [Documentation](https://docs.microsoft.com/uwp/api/windows.ui.xaml.uielement.pointerpressed) |
| `PointerReleased`             | Yes     | Yes     | Yes     | [Documentation](https://docs.microsoft.com/uwp/api/windows.ui.xaml.uielement.pointerreleased) |
| `PointerWheelChanged`         | No      | No      | No      | [Documentation](https://docs.microsoft.com/uwp/api/windows.ui.xaml.uielement.pointerwheelchanged) |
| _manipulation events_         
| `ManipulationStarting`        | Yes     | Yes     | Yes     | [Documentation](https://docs.microsoft.com/uwp/api/windows.ui.xaml.uielement.manipulationstarting) |
| `ManipulationStarted`         | Yes     | Yes     | Yes     | [Documentation](https://docs.microsoft.com/uwp/api/windows.ui.xaml.uielement.manipulationstarted) |
| `ManipulationDelta`           | Yes     | Yes     | Yes     | [Documentation](https://docs.microsoft.com/uwp/api/windows.ui.xaml.uielement.manipulationedelta) |
| `ManipulationInertiaStarting` | No      | No      | No      | [Documentation](https://docs.microsoft.com/uwp/api/windows.ui.xaml.uielement.manipulationinertiastarting) |
| `ManipulationCompleted`       | Yes     | Yes     | Yes     | [Documentation](https://docs.microsoft.com/uwp/api/windows.ui.xaml.uielement.manipulationcompleted) |
| _gesture events_         
| `Tapped`                      | Yes     | Yes     | Yes     | [Documentation](https://docs.microsoft.com/uwp/api/windows.ui.xaml.uielement.tapped) |
| `DoubleTapped`                | Yes     | Yes     | Yes     | [Documentation](https://docs.microsoft.com/uwp/api/windows.ui.xaml.uielement.doubletapped) |
| `RightTapped`                 | Yes     | Yes     | Yes     | [Documentation](https://docs.microsoft.com/uwp/api/windows.ui.xaml.uielement.righttapped) |
| `Holding`                     | Yes     | Yes     | Yes     | [Documentation](https://docs.microsoft.com/uwp/api/windows.ui.xaml.uielement.holding) |

Notes:

1. **Gesture** events:
   * Those events are managed only, and will bubble in managed code only. 
2. **Focus** events:
   * **iOS**: The concept of _focus_ is emulated because not supported by the platform, so this event is
     always bubbling in managed code.
   * **Wasm**: Current implementation is not totally reliable and doesn't support _lost focus_ most of the time.
3. **Keyboard** events:
   * **Android**: `KeyDown` and `KeyUp` events are **generated only from hardware keyboards** (Except for the _Editor Action_ on _soft keyboards_,
     those are translated as `KeyUp` with `KeyCode.Enter`). Some soft keyboard **MAY** generate those events, but your code shouldn't rely
     on that. This is a limitation [in the Android platform](https://developer.android.com/training/keyboard-input/commands) (see note on this link content).
     > Because of those limitations, _Key Events_ are not being implemented as _routed events_ on Android, so `AddHandler` & `RemoveHandler`
     > won't work for keyboard events. **They won't bubble in managed code**.
   * **iOS**: `KeyDown` & `KeyUp` routed events are generated from only a `TextBox`. Only character-related keyboard events are generated.
     They are implemented as _Routed Events_ and they are **always bubbling in managed code**.

## Event Bubbling Flow

``` plain
[1]---------------------+
| An event is fired     |
+--------+--------------+
         |
[2]------v--------------+
| Event is dispatched   |
| to corresponding      |                    [12]
| element               |                      ^
+-------yes-------------+                      |
         |                             [11]---no--------------+
         |<---[13]-raise on parent---yes A parent is          |
         |                             | defined?             |
[3]------v--------------+              |                      |
| Any local handlers?   no--------+    +-------^--------------+
+-------yes-------------+         |            |
         |                        |    [10]----+--------------+
[4]------v--------------+         |    | Event is bubbling    |
| Invoke local handlers |         |    | to parent in         <--+
+--------+--------------+         |    | managed code (Uno)   |  |
         |                        |    +-------^--------------+  |
[5]------v--------------+         |            |                 |
| Is the event handled  |         v    [6]-----no-------------+  |
| by local handlers?    no------------>| Event is coming from |  |
+-------yes-------------+              | platform?            |  |
         |                             +------yes-------------+  |
[9]------v--------------+                      |                 |
| Any parent interested |              [7]-----v--------------+  |
| by this event?        yes-+          | Is the event         |  |
+-------no--------------+   |          | bubbling natively?   no-+
         |                  |          +------yes-------------+
[12]-----v--------------+   |                  |
| Processing finished   |   v          [8]-----v--------------+
| for this event.       |  [10]        | Event is returned    |
+-----------------------+              | for native           |
                                       | bubbling in platform |
                                       +----------------------+
```

 1. **An event is fired**: when an event is intercepted from the platform.
 2. **Event dispatcher**: the source element in visual tree receive the event through its event handler.
 3. **Local handlers?**: check if there is any local handlers for the event.
 4. **Invoke handlers**: they are invoked one after the other, taking care of the "IsHandled" status.
 5. **Handled?**: check if any of the local handlers marked the event as _handled_.
 6. **Originating from platform?**: check if the source of the event is from native code.
 7. **Bubbling natively?**: check if the bubbling should be done natively for this event.
    bubbling natively will let controls not implemented in Uno the ability to intercept the event.
 8. **Returned for native bubbling**: let the event bubbling in the platform.
 9. **Any Parent Interested?**: check if any parent control is interested by a "handled" version of this event
    (using `AddEventHandler(<evt>, <handler>, handledEventsToo: true)`).
 10. **Bubbling in managed code**: propagate the event in Uno to parents (for events not bubbling natively
    or `handledEventsToo: true`).
 11. **Parent defined?**: if the element is connected to any parent element.
 12. **Processing finished**: no more handlers is interested by this event. Propagation is stopped.
    Native bubbling is stopped too because the event is fully handled.

## Native bubbling vs Managed bubbling

A special property named `EventsBubblingInManagedCode` is used to determine how properties are propagated through
the platform. This value of this property is inherited in the visual tree.

This value is set to `RoutedEventFlag.None` by default, so all routed events are bubbling natively by default
(except for those which can't bubble natively like `LostFocus` on iOS or `KeyDown` on Android).

Bubbling natively will improve interoperability with native UI elements in the visual tree by letting the
platform propagate the events. But this will cause more interop between managed and unmanaged code.

You can control which events are bubbling in managed code by using the `EventsBubblingInManagedCode`
dependency property. The value of this property is inherited to children. Example:

``` csharp
  // Make sure PointerPressed and PointerReleased are always bubbling in
  // managed code when they are originating from myControl and its children.
  myControl.EventsBubblingInManagedCode =
      RoutedEventFlag.PointerPressed | RoutedEventFlag.PointerReleased;
```

### Limitations of managed bubbling

When using a managed bubbling for a routed event, you'll have a the following limitations:

* Native container controls (scroll viewer, lists...) won't receive the event, so they can stop
  working properly.
* Once an event is converted for _managed_ bubbling, it cannot be switched back to native
  propagations : it will bubble in managed code to the root, no matter how the bubbling mode is
  configured for this event type.

### Limitations of native bubbling

* Registering handler using  `handledEventsToo: true` won't receive events if they are _consumed_
  (marked as _handled_) in platform components. If it's a problem, you must switch to managed bubbling
  for those events. If the event is _consumed_ before reaching the first managed handler, you must
  add a handler in one one the children to give Uno a way to intercept the event and make it bubble
  in managed code.
* Bubbling natively can lead to many back-in-forth between managed and native code, causing a lot
  of interop marshalling.

## Guidelines for performance

1. When you can, prefer _managed_ bubbling over _native_ to reduce crossing the interop boundary
   many times for the same event.
2. Try to reduce the usage of `handledEventsToo: true` when possible.

## Implementation notes

Due to serious differences between various platforms, some compromises were done during the
implementation of RoutedEvents:

### Unsupported Routed Events

These _routed events_ are not implemented yet in Uno:

* `DragEnter`
* `DragLeave`
* `DragOver`
* `Drop`
* `ManipulationInertiaStarting`
* `PointerWheelChanged`

### Property `OriginalSource` might not be accurate on _RoutedEventArgs_

In some cases / events, it's possible that the `OriginalSource` property of the _RoutedEventArgs_ is `null` 
or referencing the element where the event crossed the _native-to-managed_ boundary.

This property is however always accruate for "Pointers", "Manipulation" and "Gesture" events.

### Resetting `Handled` to false won't behave like in UWP

There's a strange behavior in UWP where you can switch back the `event.Handle` to `false` when
intercepted by a handler with `handledEventsToo: true`. In UWP the event will continue to bubble normally.
Trying to do this in Uno can lead to unreliable results.

## Pointer Events

Those events are the base for all others pointing device related events (i.e. Manipulation and Gesture events).
They are directly linked to the native events of each platform:
* `Touches[Began|Moved|Ended|Cancelled]` on iOS
* `dispatchTouchEvent` and `dispatchGenericMotionEvent` on Android
* `pointer[enter|leave|down|up|move|cancel]` on WebAssembly

### Pointers events and the ScrollViewer

Like on WinUI as soon as the system detects that the user wants to scroll, a control gets a `PointerCancelled` and that control won't receive
any other pointer event until the user release the pointer. That behavior can be prevented by setting the `ManipulationMode` 
to something else than `System` on a control nested in the `ScrollViewer`. (cf. [Manipulation events](#Manipulation_Events))

Be aware that on iOS this will set `DelaysContentTouches` to `false` so it means that it will slightly reduce the performance
of the scrolling (cf. [documentation](https://developer.apple.com/documentation/uikit/uiscrollview/1619398-delayscontenttouches)).

### Known limitations for pointer events

As those events are tightly coupled to the native events, Uno has to make some compromises:
* On iOS, when tapping with a mouse or a pen on Android, or in few other specific cases (like `PointerCaptureLost`), 
  multiple managed events are raised from a single native event. These have multiple effects:
	* On UWP if you have a control A and a nested control B, you will get:
		```
		B.PointerEnter
		A.PointerEnter
		B.PointerPressed
		A.PointerPressed
		```
	  but with UNO you will get:
		```
		B.PointerEnter
		B.PointerPressed
		A.PointerEnter
		A.PointerPressed
		```
	* If you handle the `PointerEnter` on **B**, the parent control **A** won't get the `PointerEnter` (as expected) nor the  `PointerPressed`.
* On Android with a mouse or a pen, the `PointerEnter` and `PointerExit` are going to be raised without taking clipping in consideration.
  This means that you will get the enter earlier and the exit later than on other platform.
* On Android if you have an element with a `RenderTransform` which overlaps one of its sibling element, the element at the top will
  get the pointer events.
* On WASM, iOS and Android, the `RoutedPointerEventArgs.FrameId` will be reset to 0 after 49 days of running time of the application.
* Unlike on UWP, controls that are under a `Popup` won't receive the unhandled pointer events.
* Unlike UWP, it's impossible to receive a `PointerReleased` without getting a `PointerPressed` before. (For instance if a child 
  control handled the pressed event but not the released event).
  > On WASM as `TextElement` are `UIElement`, it means that unlike UWP `TextBlock` won't raise the 
  > a `PointerReleased` when clicking on an `Hyperlink`.
* Unlike UWP, on the `Hyperlink` the `Click` will be raised before the `PointerReleased`.
* The property `PointerPointProperties.PointerUpdateKind` is not set on Android 5.x and lower (API level < 23)
* On Firefox, pressed pointers are reported as fingers. This means you will receive events with `PointerDeviceType == Pen` only for hovering 
  (i.e. `Pointer<Enter|Move|Exit>` - note that, as of 2019-11-28, once pressed `PointerMove` will be flagged as "touch") 
  and you won't  be able to track the barrel button nor the eraser. (cf. https://bugzilla.mozilla.org/show_bug.cgi?id=1449660)
* On WASM, if you touch the screen with the pen **then** you press the barrel button (still while touching the screen), the pointer events will
  have the `IsRightButtonPressed` set (in addition of the `IsBarrelButtonPressed`). On WinUI and Android you get this flag only if the barrel 
  button was pressed at the moment where you touched the screen, otherwise you will have the `IsLeftButtonPressed` and the `IsBarrelButtonPressed`.
* The `Holding` event is not raised after a given delay like on WinUI, but instead we rely on the fact that we usually 
  get a lot of moves for pens and fingers, so we raise the event only when we get a move that exceed defined thresholds for holding.
* On WASM, Shapes must have a non null Fill to receive pointer events (setting the Stroke is not sufficient).

### Pointer capture

The capture of pointer is handled in managed code only. On WebAssembly Uno however still requests the browser to capture the pointer,
but Uno does not rely on native `[got|lost]pointercapture` events.

## Manipulation Events

They are generated from the PointerXXX events (using the `Windows.UI.Input.GestureRecognizer`) and are bubbling in managed only.

Currently there is no intertia support, so the `IsInertial` will always be `false` and the `UIElement.ManipulationInertiaStarting` event 
will never be fired. The `Velocities` properties of event args are not implemented neither.

## Gesture Events

They are generated from the PointerXXX events (using the `Windows.UI.Input.GestureRecognizer`) and are bubbling in managed only.

Note that `Tapped` and `DoubleTapped` are not linked in any way to a native equivalent, but are fully interpreted in managed code.

In order to match the WinUI behavior, on WASM the default "Context menu" of the browser is disabled (except for the `TextBox`), 
no matter if you use / handle the `RightTapped` event or not.
Be aware that on some browser (Firefox), user can still request to get the "Context menu" on right click.
