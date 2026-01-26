---
uid: Uno.Features.Barometer
---

# Barometer

> [!TIP]
> This article covers Uno-specific information for `Barometer`. For a full description of the feature and instructions on using it, see [Barometer Class | Microsoft Learn](https://learn.microsoft.com/uwp/api/windows.devices.sensors.barometer).

* The `Windows.Devices.Sensors.Barometer` class allows measuring pressure in hectopascals (hPa).

## Supported features

| Feature          | Windows | Android | iOS | Web (WASM) | Desktop (macOS) | Desktop (X11) | Desktop (Windows) |
|------------------|---------|---------|-----|------------|-----------------|---------------|-------------------|
| `GetDefault`     | ✔       | ✔       | ✔   | ✔          | ✔               | ✔             | ✔                 |
| `ReadingChanged` | ✔       | ✔       | ✔   | ✖          | ✖               | ✖             | ✖                 |
| `ReportInterval` | ✔       | ✔       | ✖   | ✖          | ✖               | ✖             | ✖                 |

## Using Barometer with Uno

* The `GetDefault` method is available on all targets and will return `null` on those which do not support `Barometer` or devices that do not have such a sensor.
* Ensure to unsubscribe from the `ReadingChanged` event when you no longer need the readings, so that the sensor is no longer active to avoid unnecessary battery consumption.

## Example

### Capturing sensor readings

```csharp
var barometer = Barometer.GetDefault();
barometer.ReadingChanged += Barometer_ReadingChanged;

private async void Barometer_ReadingChanged(Barometer sender, BarometerReadingChangedEventArgs args)
{
    // If you want to update the UI in some way, ensure the Dispatcher is used,
    // as the ReadingChanged event handler does not run on the UI thread.
    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
    {
        OutputTextBlock.Text = $"Sensor reading in hPa = {args.Reading.StationPressureInHectopascals}, " +
            $"timestamp = {args.Reading.Timestamp}";
    });
}
```

### Unsubscribing from the readings

```csharp
barometer.ReadingChanged -= Barometer_ReadingChanged;
```
