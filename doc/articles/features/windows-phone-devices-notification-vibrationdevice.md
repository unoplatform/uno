---
uid: Uno.Features.WPDNotification
---

# Vibration

> [!TIP]
> This article covers Uno-specific information for the `Windows.Phone.Devices.Notification` namespace. For a full description of the feature and instructions on using it, see [Windows.Phone.Devices.Notification Namespace](https://learn.microsoft.com/uwp/api/windows.phone.devices.notification).

- The `Windows.Phone.Devices.Notification.VibrationDevice` allows for managing the vibration device on the phone.

## `VibrationDevice` class

### Platform-specific requirements

#### Android

For Android, there is one permission you must configure before using this API in your project. To do that, add the following to `AndroidManifest.xml`:

```xml
<uses-permission android:name="android.permission.VIBRATE" />
```

### Limitations

#### iOS

- The `Cancel` method is not supported.
- The parameter of the `Vibrate(TimeSpan)` method is not taken into account - iOS supports only a default vibration duration.

#### WinUI

- The `Windows.Phone.Devices.Notification` namespace is no longer supported in WinUI.
- Developers should use the [Windows.Devices.Haptics](xref:Uno.Features.WDHaptics) namespace for vibration functionality.
