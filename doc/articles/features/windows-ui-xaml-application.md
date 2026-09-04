---
uid: Uno.Features.WinUIApplication
---

# Application

> [!TIP]
> This article covers Uno-specific information for `Microsoft.UI.Xaml.Application`. For a full description of the feature and instructions on using it, see [Application Class](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.application).

* The `Microsoft.UI.Xaml.Application` class enables an application to manage the lifetime of the application and to query the application's state.

## Lifecycle events

`Suspending`, `Resuming`, `EnteredBackground` and `LeavingBackground` are not members of
`Microsoft.UI.Xaml.Application`, matching the Windows App SDK. Subscribe to the events of the same
names on `Windows.ApplicationModel.Core.CoreApplication` instead:

```csharp
using Windows.ApplicationModel.Core;

CoreApplication.Suspending += OnSuspending;

private void OnSuspending(object? sender, SuspendingEventArgs e)
{
    var deferral = e.SuspendingOperation.GetDeferral();
    //TODO: Save application state
    deferral.Complete();
}
```

Which targets report suspension follows the platform:

|Target|Reports suspension|
|-|-|
|**Android**, **iOS**, **tvOS**|Yes — the OS genuinely backgrounds and may terminate the app.|
|**Skia Desktop** (Windows, macOS, Linux)|No. Desktop apps have no suspended state, as on the Windows App SDK. Save state from `Window.Closed`.|
|**WebAssembly**|No.|

### Limitations

#### Android, iOS and tvOS

The handler cannot perform asynchronous work: the deferral must be completed before the handler
returns, because the OS gives the app only a short, non-extendable window before it is suspended.
