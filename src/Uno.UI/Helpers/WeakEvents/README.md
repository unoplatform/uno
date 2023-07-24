# Weak events

Note: the base abstract `WeakEventProxy<TSource, TEventHandler>` is based on MAUI's code.

To best explain how this works, let's have an example:

Assume a control (e.g, TextBlock) has a Foreground property of type brush, and the control needs to re-render whenever the brush changes.

## The problem with a typical naive approache

A typical naive approach is to have the control subscribe into Brush's `InvalidateRender` event. The problem is now the brush holds a reference to the control.
If the brush is long-lived, it will prevent the control from being GC'ed.

Can this be solved by unsubscribing the event when the control is Unloaded? No, controls may be created but are never loaded or unloaded. This is not reliable.

## The solution

The idea here is that the control will hold a strong reference to both the brush and the event handler that re-renders the control.
Then, a type derived from WeakEventProxy will be responsible for the subscription (e.g, `WeakBrushChangedProxy`).

When the subscription happens, the brush will hold a reference to the `WeakBrushChangedProxy` rather than the control itself.
`WeakBrushChangedProxy` only has a weak reference to the control and the event handler. So that will allow the control to be GC'ed when it's no longer needed.

But now the brush has a reference to `WeakBrushChangedProxy`, what about the leak possibility here?

This leak is addressed by unsubscribing from the brush event when the control is GC'ed.

To have a full summarized picture:

- The control holds a strong reference to the `WeakEventProxy` and the event handler.
- The event handler holds a reference to the control.
- The brush holds a reference to the `WeakEventProxy`.

Now, let's understand how each component gets GC'ed

- First, the control can be GC'ed, it's referenced only by the event handler, and the event handler is referenced only by the control. So that's a cyclic dependency the GC can handle.
- Second, since the control is GC'ed, the event handler can be GC'ed as well.
- Third, once the control is GC'ed, it will unsubscribe the brush event, so the `WeakEventProxy` is no longer referenced by the brush. Hence, `WeakEventProxy` is now GC'ed as well.

### Important notes

1. The event handler in `WeakEventProxy` must be a `WeakReference`. Why? Because the event handler usually holds a reference to the control itself. So having the event handler as a strong reference will still leak.
2. The event handler has must be stored in a field in the control. Why? Because otherwise it will get GC'ed incorrectly.

So, to use weak events properly, here is a minimal example of what you need to do:

```csharp
public class SomeControl
{
    private WeakBrushChangedProxy _foregroundChangedProxy;
    private Action _foregroundChanged;

    private OnForegroundChanged()
    {
        _foregroundChangedProxy ??= new();
        // Note: If you capture something other than 'this' (e.g, the new brush value), then don't use `??=` and always assign a new handler.
        // Assigning once on first use is an optimization that should only be used when the handler doesn't capture a state that may change.
        _foregroundChanged ??= () => UpdateControlRendering();
        _foregroundChangedProxy.Subscribe(newValue, _foregroundChanged);
    }

    ~SomeControl()
    {
        _foregroundChangedProxy?.Unsubscribe();
    }
}

```

