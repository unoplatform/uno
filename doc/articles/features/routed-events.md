# Uno Support for _Routed Events_

## Implemented Routed Events

| Routed Event          | Android | iOS     | Wasm    |     |
| --------------------- | ------- | ------- | ------- | --- |
| _tapped events_
| `Tapped`              | Yes     | Yes (1) | Yes (1) | [Documentation](https://docs.microsoft.com/uwp/api/windows.ui.xaml.uielement.tapped) |
| `DoubleTapped`        | Yes     | Yes (1) | No (1)  | [Documentation](https://docs.microsoft.com/uwp/api/windows.ui.xaml.uielement.doubletapped) |
| _focus events_
| `GotFocus`            | Yes     | Yes (2) | Yes (2) | [Documentation](https://docs.microsoft.com/uwp/api/windows.ui.xaml.uielement.gotfocus) |
| `LostFocus`           | Yes     | Yes (2) | Yes (2) | [Documentation](https://docs.microsoft.com/uwp/api/windows.ui.xaml.uielement.lostfocus) |
| _keyboard events_
| `KeyDown`   | Hardware Only (3) | Yes (3) | Yes     | [Documentation](https://docs.microsoft.com/uwp/api/windows.ui.xaml.uielement.keydown) |
| `KeyUp`     | Hardware Only (3) | Yes (3) | Yes     | [Documentation](https://docs.microsoft.com/uwp/api/windows.ui.xaml.uielement.keyup) |
| _pointerd events_
| `PointerCanceled`     | Yes     | No      | Yes     | [Documentation](https://docs.microsoft.com/uwp/api/windows.ui.xaml.uielement.pointercanceled) |
| `PointerCaptureLost`  | Yes     | No      | Yes     | [Documentation](https://docs.microsoft.com/uwp/api/windows.ui.xaml.uielement.pointercapturelost) |
| `PointerEntered`      | Yes     | No      | Yes     | [Documentation](https://docs.microsoft.com/uwp/api/windows.ui.xaml.uielement.pointerentered) |
| `PointerExited`       | Yes     | No      | Yes     | [Documentation](https://docs.microsoft.com/uwp/api/windows.ui.xaml.uielement.pointerexited) |
| `PointerMoved`        | Yes     | No      | Yes     | [Documentation](https://docs.microsoft.com/uwp/api/windows.ui.xaml.uielement.pointermoved) |
| `PointerPressed`      | Yes     | No      | Yes     | [Documentation](https://docs.microsoft.com/uwp/api/windows.ui.xaml.uielement.pointerpressed) |
| `PointerReleased`     | Yes     | No      | Yes     | [Documentation](https://docs.microsoft.com/uwp/api/windows.ui.xaml.uielement.pointerreleased) |

Notes:

1. **Tapped** & **DoubleTapped** events:
   * **iOS**: Those events are the result of a _Gesture Recognizer_ on the platform, so they'll never bubble natively.
   * **Wasm**: Those are connected to `click` and `dblclick` events in HTML. They will always be reported as coming from a mouse pointer.
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
   * **iOS**: `KeyDown` & `KeyUp` routed events are generated from only a `TextBox`. Only characted-related keyboard events are generated.
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
 3. **Local handlers?**: check if there is any local handlders for the event.
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
    Native bubbling is stopped too, because the event is fully handled.

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

1. When you can, prefer _managed_ bubbling over _native_ to redure crossing the interop boundary
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
* `Holding`
* `ManipulationCompleted`
* `ManipulationDelta`
* `ManipulationInertiaStarting`
* `ManipulationStarted`
* `ManipulationStarting`
* `PointerWheelChanged`
* `RightTapped`

### Property `OriginalSource` not accurate on _RoutedEventArgs_

In the current implementation the `OriginalSource` property on the _RoutedEventArgs_ will often be null
or referencing the element where the event crossed the _native-to-managed_ boundary.

### Reseting `Handled` to false won't behave like in UWP

There's a strange behavior in UWP where you can switch back the `event.Handle` to `false` when
intercepted by a handler with `handledEventsToo: true`. In UWP the event will continue to bubble normally.
Trying to do this in Uno can lead to unreliable results.
