---
uid: Uno.Features.RoutedEvents
---

# Uno Platform Support for Routed Events

Per the WinUI contract, Uno Platform provides support for [routed events](https://learn.microsoft.com/windows/uwp/xaml-platform/events-and-routed-events-overview), events which are 'bubbled' from a child object to each of its successive parents in the XAML object tree. In most cases, you can expect routed events to behave the same way on Windows and non-Windows platforms.

This article covers some of the finer technical details of Uno Platform's routed events implementation that may be relevant in advanced scenarios, eg those involving custom native (non-WinUI) views in your visual tree.

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

## Differences from UWP/WinUI 3

Due to serious differences between various platforms, some compromises were done during the
implementation of RoutedEvents:

### Property `OriginalSource` might not be accurate on _RoutedEventArgs_

In some cases / events, it's possible that the `OriginalSource` property of the _RoutedEventArgs_ is `null`
or referencing the element where the event crossed the _native-to-managed_ boundary.

This property is however always accurate for "Pointers", "Manipulation", "Gesture", and "Drag and drop" events.

### Resetting `Handled` to false won't behave like in UWP

There's a strange behavior in UWP where you can switch back the `event.Handle` to `false` when
intercepted by a handler with `handledEventsToo: true`. In UWP, the event will continue to bubble normally.
Trying to do this in Uno can lead to unreliable results.
