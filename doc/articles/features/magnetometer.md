---
uid: Uno.Features.Magnetometer
---

# Magnetometer

> [!TIP]
> This article covers Uno-specific information for Magnetometer. For a full description of the feature and instructions on using it, see [Magnetometer Class](https://learn.microsoft.com/uwp/api/windows.devices.sensors.magnetometer).

* The `Windows.Devices.Sensors.Magnetometer` class allows measuring magnetic force affecting the device.

## Supported features

| Feature          | Windows | Android | iOS | Web (WASM) | Desktop (macOS) | Desktop (X11) | Desktop (Windows) |
|------------------|---------|---------|-----|------------|-----------------|---------------|-------------------|
| `GetDefault`     | ✔       | ✔       | ✔   | ✔          | ✔               | ✔             | ✔                 |
| `ReadingChanged` | ✔       | ✔       | ✔   | ✔          | ✖               | ✖             | ✖                 |
| `ReportInterval` | ✔       | ✔       | ✔   | ✔          | ✖               | ✖             | ✖                 |

## Using Magnetometer with Uno

* The `GetDefault` method is available on all targets and will return `null` on those which do not support `Magnetometer` or devices which do not have such sensor.
* Ensure to unsubscribe from the `ReadingChanged` event when you no longer need the readings, so that the sensor is no longer active to avoid unnecessary battery consumption.
* `ReportInterval` property on WASM is currently not supported directly. Uno uses an approximation in the form of raising the `ReadingChanged` event, only when enough time has passed since the last report. The event is raised a bit more often to make sure the gap caused by the filter is not too large, but this is in-line with the behavior of Windows' `Magnetometer`.
* `DirectionalAccuracy` is not reported on iOS, so it will always return `Unknown`.

## Example

### Capturing sensor readings

```csharp
var magnetometer = Magnetometer.GetDefault();
magnetometer.ReadingChanged += Magnetometer_ReadingChanged;

private async void Magnetometer_ReadingChanged(Magnetometer sender, MagnetometerReadingChangedEventArgs args)
{
    // If you want to update the UI in some way, ensure the Dispatcher is used,
    // as the ReadingChanged event handler does not run on the UI thread.
    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
    {
        MagneticFieldX = args.Reading.MagneticFieldX;
        MagneticFieldY = args.Reading.MagneticFieldY;
        MagneticFieldZ = args.Reading.MagneticFieldZ;
        DirectionalAccuracy = args.Reading.DirectionalAccuracy;
        Timestamp = args.Reading.Timestamp.ToString("R");
    });
}
```

### Unsubscribing from the readings

```csharp
magnetometer.ReadingChanged -= Magnetometer_ReadingChanged;
```
