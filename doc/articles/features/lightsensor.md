---
uid: Uno.Features.LightSensor
---

# LightSensor

> [!TIP]
> This article covers Uno-specific information for LightSensor. For a full description of the feature and instructions on using it, see [Use the light sensor](https://learn.microsoft.com/windows/uwp/devices-sensors/use-the-light-sensor).

* The `Windows.Devices.Sensors.LightSensor` class allows measuring the illuminance in LUX.

## Supported features

| Feature          |Windows|Android|iOS|Wasm|macOS|Skia|
|------------------|-------|-------|---|----|-----|----|
| `GetDefault`     |   ✔   |   ✔   | ✖ | ✔ | ✖ | ✖ |
| `ReadingChanged` |   ✔   |   ✔   | ✖ | ✔ | ✖ | ✖ |
| `ReportInterval` |   ✔   |   ✔   | ✖ | ✖ | ✖ | ✖ |

## Using LightSensor with Uno

* The `GetDefault` method is available on all targets and will return `null` on those which do not support `LightSensor` or devices that do not have such a sensor.
* Ensure to unsubscribe from the `ReadingChanged` events when you no longer need the readings, so that the sensor is no longer active to avoid unnecessary battery consumption.

## Examples

### Capturing sensor readings

```csharp
var lightSensor = LightSensor.GetDefault();
lightSensor.ReadingChanged += LightSensor_ReadingChanged;

private async void LightSensor_ReadingChanged(LightSensor sender, LightSensorReadingChangedEventArgs args)
{
    // If you want to update the UI in some way, ensure the Dispatcher is used,
    // as the ReadingChanged event handler does not run on the UI thread.
    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
    {
        OutputTextBlock.Text = $"Sensor reading is " +
            $"IlluminanceInLux = {args.Reading.IlluminanceInLux}, " + 
            $"timestamp = {args.Reading.Timestamp}";
    });
}
```

### Unsubscribing from the readings

```csharp
lightSensor.ReadingChanged -= LightSensor_ReadingChanged;
```
