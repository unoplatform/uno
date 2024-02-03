---
uid: Uno.Features.WDHaptics
---

# Haptics

> [!TIP]
> This article covers Uno-specific information for the `Windows.Devices.Haptics` namespace. For a full description of the feature and instructions on using it, see [Windows.Devices.Haptics Namespace](https://learn.microsoft.com/uwp/api/windows.devices.haptics).

* The `Windows.Devices.Haptics` namespace provides classes for accessing and managing vibration devices and haptic feedback.

## `VibrationDevice` class

### Platform-specific requirements

The `GetDefaultAsync` method is implemented on all platforms and returns `null` for the unsupported platforms (WPF, GTK).

#### Android

For Android, there is one permission you must configure before using this API in your project. To do that, add the following to `AndroidManifest.xml`:

```xml
<uses-permission android:name="android.permission.VIBRATE" />
```

#### Tizen

For Tizen, the `http://tizen.org/privilege/haptic` privilege needs to be declared in `tizen-manifest.xml` file.

## `SimpleHapticsController`

The `SupportedFeedback` property returns the list of supported feedback types for the given platform. In most cases this includes `Click` and `Press`.

The following code snippet illustrates the usage of `VibrationDevice` and `SimpleHapticsController`:

```csharp
var result = await VibrationDevice.RequestAccessAsync();
if (result == VibrationAccessStatus.Allowed)
{
    var vibrationDevice = await VibrationDevice.GetDefaultAsync();
    if (vibrationDevice != null)
    {
        var simpleHapticsController = vibrationDevice.SimpleHapticsController;
        var feedbackType = simpleHapticsController.SupportedFeedback.FirstOrDefault(
            feedback => feedback.Waveform == KnownSimpleHapticsControllerWaveforms.Press);
        if (feedbackType != null)
        {
            simpleHapticsController.SendHapticFeedback(feedbackType);
        }
    }
}
```
