---
uid: Uno.Features.HardwareBackButton
---

# Handling hardware back button

Some devices provide a hardware back button to handle navigation within applications. `SystemNavigationManager` provides this functionality for Uno Platform applications.

## Listening to hardware back button

The `BackRequested` event of the `SystemNavigationManager` is triggered whenever the user presses the hardware back button or performs a back gesture. To subscribe to it, first get an instance of `SystemNavigationManager` via the `GetForCurrentView` method:

```csharp
var manager = SystemNavigationManager.GetForCurrentView();
manager.BackRequested += OnBackRequested;
```

The event handler should check whether the application can handle the back button press (e.g. if it is possible to navigate back within the app's UI), and in such case perform the in-app navigation and mark the event args as `Handled`:

```csharp
private void OnBackRequested(object sender, BackRequestedEventArgs e)
{
    if (this.Frame.CanGoBack)
    {
        this.Frame.GoBack();
        e.Handled = true; // Indicates that the back request has been handled
    }
}
```

When `Handled` is set to `true`, the OS will not continue processing the request. If not set or set to `false`, the OS will navigate away from the application.

Make sure to unsubscribe from the event when no longer needed.

## Android predictive back gesture support

Starting with Android 13 (API 33) and becoming more prominent in Android 14 (API 34) and Android 15 (API 35), Android introduced support for predictive back gestures. Uno Platform supports this feature by using the modern `OnBackPressedCallback` API instead of the deprecated `OnBackPressed` method.

This means:

- When you subscribe to the `BackRequested` event, Uno Platform automatically registers an `OnBackPressedCallback` with the Android activity.
- The callback is enabled only when there are subscribers to the `BackRequested` event or when `AppViewBackButtonVisibility` is set to `Visible`.
- If your handler does not set `Handled = true`, the system's default back behavior will occur.

No changes to your existing code are required - the implementation automatically handles the transition to the new Android back handling mechanism.
