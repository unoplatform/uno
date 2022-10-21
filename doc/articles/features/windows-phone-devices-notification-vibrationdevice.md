# Uno Support for Windows.Phone.Devices.Notification APIs

## `VibrationDevice`

### Limitations

**iOS**
- The `Cancel` method is not supported.
- The parameter of the `Vibrate(TimeSpan)` method is not taken into account - iOS supports only a default vibration duration.

**UWP**
- The `VibrationDevice`-class is only available in the UWP-head if the `Windows.Mobile.Extenstions for the UWP` is added to the project.
