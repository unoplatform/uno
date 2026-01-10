---
uid: Uno.Features.OrientationSensor
---

# Orientation sensor

> [!TIP]
> This article covers Uno-specific information for SimpleOrientationSensor. For a full description of the feature and instructions on using it, see [SimpleOrientationSensor Class](https://learn.microsoft.com/uwp/api/windows.devices.sensors.simpleorientationsensor).

* The `Windows.Devices.Sensors.SimpleOrientationSensor` class allows you to determine the general orientation of the device.

## Supported features

| Feature              | Windows | Android | iOS | Web (WASM) | Desktop (macOS) | Desktop (X11) | Desktop (Windows) |
|----------------------|---------|---------|-----|------------|-----------------|---------------|-------------------|
| `GetDefault`         | ✔       | ✔       | ✔   | ✔          | ✔               | ✔             | ✔                 |
| `OrientationChanged` | ✔       | ✔       | ✔   | ✔          | ✖               | ✖             | ✖                 |

> [!IMPORTANT]
> The `OrientationChanged` event is not supported by iOS simulators.

## Using SimpleOrientationSensor with Uno

* The `GetDefault` method is available on all targets and will return `null` on those that do not support `SimpleOrientationSensor` or devices that do not have such a sensor.
* Ensure to unsubscribe from the `OrientationChanged` event when you no longer need the readings so that the sensor is no longer active to avoid unnecessary battery consumption.

## Example

### Capturing sensor readings

```csharp
var simpleOrientationSensor = SimpleOrientationSensor.GetDefault();
simpleOrientationSensor.OrientationChanged += SimpleOrientationSensor_OrientationChanged;

private async void SimpleOrientationSensor_OrientationChanged(object sender, SimpleOrientationSensorOrientationChangedEventArgs args)
{
    // If you want to update the UI in some way, ensure the Dispatcher is used,
    // as the OrientationChanged event handler does not run on the UI thread.
    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
    {
        switch (orientation)
        {
            case SimpleOrientation.NotRotated:
                tb.Text = "Not Rotated";
                break;
            case SimpleOrientation.Rotated90DegreesCounterclockwise:
                tb.Text = "Rotated 90 Degrees Counterclockwise";
                break;
            case SimpleOrientation.Rotated180DegreesCounterclockwise:
                tb.Text = "Rotated 180 Degrees Counterclockwise";
                break;
            case SimpleOrientation.Rotated270DegreesCounterclockwise:
                tb.Text = "Rotated 270 Degrees Counterclockwise";
                break;
            case SimpleOrientation.Faceup:
                tb.Text = "Faceup";
                break;
            case SimpleOrientation.Facedown:
                tb.Text = "Facedown";
                break;
            default:
                tb.Text = "Unknown orientation";
                break;
        }
    });
}
```

### Unsubscribing from the readings

```csharp
simpleOrientationSensor.OrientationChanged -= SimpleOrientationSensor_OrientationChanged;
```
