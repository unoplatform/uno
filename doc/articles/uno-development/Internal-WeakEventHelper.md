---
uid: Uno.Contributing.WeakEventHelper
---

# The WeakEventHelper class

The WeakEventHelper class is an internal method that is designed to provide a
memory-friendly environment for registering to events internally in Uno.

This class is not exposed to the end-user because its patterns do not fit with the
original UWP event-based designs of the API.

## The RegisterEvent method

The bi-directional relation of the weak registration is defined by the fact
that both the source and the target are weak. The source must be kept alive by
another longer-lived reference, and the target is kept alive by the
return disposable.

If the returned disposable is collected, the handler will also be
collected. Conversely, if the provided list is collected
raising the event will produce nothing.

Here's a usage example:

 private List<WeakEventHelper.GenericEventHandler> _sizeChangedHandlers = new List<WeakEventHelper.GenericEventHandler>();

 internal IDisposable RegisterSizeChangedEvent(WindowSizeChangedEventHandler handler)
 {
  return WeakEventHelper.RegisterEvent(
   _sizeChangedHandlers,
   handler,
   (h, s, e) => (h as WindowSizeChangedEventHandler)?.Invoke(s, (WindowSizeChangedEventArgs)e)
  );
 }

The RegisterEvent method is intentionally non-generic to avoid the cost related to AOT performance. The
performance cost is shifted to downcast and upcast checks in the `EventRaiseHandler` handlers.

The returned disposable must be used as follows :

 private SerialDisposable _sizeChangedSubscription = new SerialDisposable();

 ...

 _sizeChangedSubscription.Disposable = null;

 if (Owner != null)
 {
  _sizeChangedSubscription.Disposable = Window.Current.RegisterSizeChangedEvent(OnCurrentWindowSizeChanged);
 }
