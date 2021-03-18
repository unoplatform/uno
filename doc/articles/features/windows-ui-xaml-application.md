# Windows.UI.Xaml.Application

## Application.Suspending event

This event is invoked when the application is about to be suspended.

### Limitations

#### WebAssembly

The application cannot perform asynchronous work during the execution of the handler of Suspending, as the browser window will be closed thereafter the execution of a [beforeunload](https://developer.mozilla.org/en-US/docs/Web/API/Window/beforeunload_event) handler.

The handler must invoke the deferral at the end.

```csharp
private void OnSuspending(object sender, SuspendingEventArgs e)
{
    var deferral = e.SuspendingOperation.GetDeferral();
    //TODO: Save application state
    deferral.Complete();
}
```

If the application wants to make a network call, the [navigation.sendBeacon](https://developer.mozilla.org/en-US/docs/Web/API/Navigator/sendBeacon) can be used past the closing of the current browser page.
