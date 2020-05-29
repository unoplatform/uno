# Windows.UI.Xaml.Application

## Application.Suspending event

This event is invoked when the application is about to be suspended.

### Limitations

**WebAssembly**: The application must not perform asynchronous work during the execution of the handler of Suspending, as the browser window will be closed thereafter.

The handler must invoke the deferral at the end.

```csharp
private void OnSuspending(object sender, SuspendingEventArgs e)
{
    var deferral = e.SuspendingOperation.GetDeferral();
    //TODO: Save application state and stop any background activity
    deferral.Complete();
}
```
