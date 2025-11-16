---
uid: Uno.Contributing.WeakEventHelper
---

# The WeakEventHelper class

The WeakEventHelper class is an internal method that is designed to provide a
memory-friendly environment for registering to events internally in Uno.

This class is not exposed to the end-user because its patterns do not fit with the
original WinUI event-based designs of the API.

## The RegisterEvent method

The bi-directional relation of the weak registration is defined by the fact
that both the source and the target are weak. The source must be kept alive by
another longer-lived reference, and the target is kept alive by the
return disposable.

If the provided handler is collected, the registration will
be collected as well. The returned disposable is not tracked, which means that it will
not remove the registration when collected, unless the provided handler is a lambda. In
this case, the lambda's lifetime is tied to the returned disposable.

The WeakEventCollection automatically manages its internal registration list using GC events.

Here's a usage example:

 ```csharp
 private WeakEventHelper.WeakEventCollection? _sizeChangedHandlers;

 internal IDisposable RegisterSizeChangedEvent(WindowSizeChangedEventHandler handler)
 {
  return WeakEventHelper.RegisterEvent(
   _sizeChangedHandlers ??= new(),
   handler,
   (h, s, e) => (h as WindowSizeChangedEventHandler)?.Invoke(s, (WindowSizeChangedEventArgs)e)
  );
 }

 internal void RaiseEvent()
 {
    _sizeChangedHandlers?.Invoke(this, new WindowSizeChangedEventArgs());
 }
 ```

The RegisterEvent method is intentionally non-generic to avoid the cost related to AOT performance. The
performance cost is shifted to downcast and upcast checks in the `EventRaiseHandler` handlers.

The returned disposable must be used as follows :

 ```csharp
 private IDisposable? _sizeChangedSubscription;

 ...

 _sizeChangedSubscription?.Dispose();

 _sizeChangedSubscription = Window.Current.RegisterSizeChangedEvent(OnCurrentWindowSizeChanged);
 ```
