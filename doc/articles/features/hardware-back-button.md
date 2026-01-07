---
uid: Uno.Features.HardwareBackButton
---

# Handling hardware back button

Some devices provide a hardware back button to handle navigation within applications. `SystemNavigationManager` provides this functionality for Uno Platform applications.

## Listening to hardware back button

The `BackRequested` event of the `SystemNavigationManager` is triggered whenever the user presses the hardware back button. To subscribe to it, first get an instance of `SystemNavigationManager` via the `GetForCurrentView` method:

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

> [!NOTE]
> On Android 16+ (API level 36+), the behavior differs. See the [Android 16+ behavior](#android-16-behavior) section below for details.

When `Handled` is set to `true`, the OS will not continue processing the request. If not set or set to `false`, the OS will navigate away from the application.

Make sure to unsubscribe from the event when no longer needed.

## Android 16+ behavior

Starting with Android 16 (API level 36), the back navigation behavior has changed due to the [predictive back gesture](https://developer.android.com/guide/navigation/custom-back/predictive-back-gesture) feature. On these versions, the `Handled` property is ignored, and the subscription state of the `BackRequested` event determines whether the app handles back navigation:

- **When subscribed**: The app is assumed to handle back navigation. Back button presses are consumed by the app.
- **When unsubscribed**: The system handles back navigation (e.g., exits the app or navigates back in the task stack).

### Best practices for Android 16+

To ensure proper behavior on Android 16+, subscribe to `BackRequested` only when your app can handle back navigation, and unsubscribe when it cannot. A common pattern is to subscribe on `Loaded` and unsubscribe on `Unloaded`:

```csharp
public MyPage()
{
    InitializeComponent();

    Loaded += OnLoaded;
    Unloaded += OnUnloaded;
}

private void OnLoaded(object sender, RoutedEventArgs e)
{
    SystemNavigationManager.GetForCurrentView().BackRequested += OnBackRequested;
}

private void OnUnloaded(object sender, RoutedEventArgs e)
{
    SystemNavigationManager.GetForCurrentView().BackRequested -= OnBackRequested;
}

private void OnBackRequested(object sender, BackRequestedEventArgs e)
{
    if (this.Frame.CanGoBack)
    {
        this.Frame.GoBack();
        // On Android 16+, the Handled property is ignored.
        // The subscription itself indicates the app handles back navigation.
        e.Handled = true;
    }
}
```

> [!NOTE]
> On Android versions prior to 16, the `Handled` property continues to work as expected. Setting it to `true` prevents the OS from processing the back request, while `false` allows the OS to handle it.
