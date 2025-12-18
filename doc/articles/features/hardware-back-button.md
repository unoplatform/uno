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

### Important: Setting AppViewBackButtonVisibility

To properly support Android's predictive back gesture, you **must** set `AppViewBackButtonVisibility` to `Visible` when your app can handle back navigation:

```csharp
var manager = SystemNavigationManager.GetForCurrentView();

// Subscribe to back requests
manager.BackRequested += OnBackRequested;

// Proactively indicate when your app can handle back navigation
// This enables Android's predictive back gesture animation
manager.AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
```

The predictive back gesture requires the app to proactively declare whether it can handle back navigation **before** the user starts the gesture. When `AppViewBackButtonVisibility` is:
- **Visible**: The back gesture shows an in-app animation and triggers your `BackRequested` handler
- **Collapsed**: The back gesture shows the "going home" animation and exits the app

### Updating visibility dynamically

You should update `AppViewBackButtonVisibility` whenever your app's navigation state changes:

```csharp
private void OnNavigated(object sender, NavigationEventArgs e)
{
    var manager = SystemNavigationManager.GetForCurrentView();
    manager.AppViewBackButtonVisibility = 
        Frame.CanGoBack 
            ? AppViewBackButtonVisibility.Visible 
            : AppViewBackButtonVisibility.Collapsed;
}
```

This ensures the predictive back gesture correctly animates based on whether your app will handle the back action.
