---
uid: Uno.Features.Gyrometer
---

# Gyrometer

> [!TIP]
> This article covers Uno-specific information for Gyrometer. For a full description of the feature and instructions on using it, see [Gyrometer Class](https://learn.microsoft.com/uwp/api/windows.devices.sensors.gyrometer).

* The `Windows.Devices.Sensors.Gyrometer` class allows measuring angular velocity applied on the device.

## Supported features

| Feature          | Windows | Android | iOS | Web (WASM) | Desktop (macOS) | Desktop (X11) | Desktop (Windows) |
|------------------|---------|---------|-----|------------|-----------------|---------------|-------------------|
| `GetDefault`     | ✔       | ✔       | ✔   | ✔          | ✔               | ✔             | ✔                 |
| `ReadingChanged` | ✔       | ✔       | ✔   | ✔          | ✖               | ✖             | ✖                 |
| `ReportInterval` | ✔       | ✔       | ✔   | ✔          | ✖               | ✖             | ✖                 |

## Using Gyrometer with Uno

* The `GetDefault` method is available on all targets and will return `null` on those which do not support `Gyrometer` or devices that do not have such a sensor.
* Ensure to unsubscribe from the `ReadingChanged` event when you no longer need the readings, so that the sensor is no longer active to avoid unnecessary battery consumption.
* `ReportInterval` property on WASM is currently not supported directly. Uno uses an approximation in the form of raising the `ReadingChanged` event, only when enough time has passed since the last report. The event is raised a bit more often to make sure the gap caused by the filter is not too large, but this is in line with the behavior of Windows' `Gyrometer`.

## Example

### Capturing sensor readings

```csharp
var gyrometer = Gyrometer.GetDefault();
gyrometer.ReadingChanged += Gyrometer_ReadingChanged;

private async void Gyrometer_ReadingChanged(Gyrometer sender, GyrometerReadingChangedEventArgs args)
{
    // If you want to update the UI in some way, ensure the Dispatcher is used,
    // as the ReadingChanged event handler does not run on the UI thread.
    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
    {
        AngularVelocityX = args.Reading.AngularVelocityX;
        AngularVelocityY = args.Reading.AngularVelocityY;
        AngularVelocityZ = args.Reading.AngularVelocityZ;
        Timestamp = args.Reading.Timestamp.ToString("R");
    });
}
```

### Unsubscribing from the readings

```csharp
gyrometer.ReadingChanged -= Gyrometer_ReadingChanged;
```
