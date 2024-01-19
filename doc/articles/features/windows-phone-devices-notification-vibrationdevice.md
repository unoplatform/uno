---
uid: Uno.Features.WPDNotification
---

# Vibration

> [!TIP]
> This article covers Uno-specific information for `Windows.Phone.Devices.Notification` namespace. For a full description of the feature and instructions on using it, see [Windows.Phone.Devices.Notification Namespace](https://learn.microsoft.com/uwp/api/windows.phone.devices.notification).

- The `Windows.Phone.Devices.Notification.VibrationDevice` allows for managing the vibration device on the phone.

## `VibrationDevice` class

### Limitations

#### iOS

- The `Cancel` method is not supported.
- The parameter of the `Vibrate(TimeSpan)` method is not taken into account - iOS supports only a default vibration duration.

#### UWP

- The `VibrationDevice` class is only available in the UWP-head if the `Windows.Mobile.Extenstions for the UWP` is added to the project.
