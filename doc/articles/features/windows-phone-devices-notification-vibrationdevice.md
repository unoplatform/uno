# Uno Support for Windows.Phone.Devices.Notification APIs

## `VibrationDevice`

### Limitations

**iOS**
- The `Cancel` method is not supported.
- The parameter of the `Vibrate(TimeSpan)` method is not taken into account - iOS supports only a default vibration duration.