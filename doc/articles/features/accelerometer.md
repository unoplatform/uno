---
uid: Uno.Features.Accelerometer
---

# Accelerometer

> [!TIP]
> This article covers Uno-specific information for Accelerometer. For a full description of the feature and instructions on using it, see [Use the accelerometer](https://learn.microsoft.com/windows/uwp/devices-sensors/use-the-accelerometer).

* The `Windows.Devices.Sensors.Accelerometer` class allows measuring the linear acceleration of the device along three X, Y, and Z-axis.

## Supported features

| Feature          | Windows | Android | iOS | Web (WASM) | Desktop (macOS) | Desktop (X11) | Desktop (Windows) |
|------------------|---------|---------|-----|------------|-----------------|---------------|-------------------|
| `GetDefault`     | ✔       | ✔       | ✔   | ✔          | ✔               | ✔             | ✔                 |
| `ReadingChanged` | ✔       | ✔       | ✔   | ✔          | ✖               | ✖             | ✖                 |
| `Shaken`         | ✔       | ✔       | ✔   | ✔          | ✖               | ✖             | ✖                 |
| `ReportInterval` | ✔       | ✔       | ✔   | ✔          | ✖               | ✖             | ✖                 |

## Using Accelerometer with Uno

* The `GetDefault` method is available on all targets and will return `null` on those that do not support `Accelerometer` or devices that do not have such a sensor.
* Ensure to unsubscribe from the `ReadingChanged` and `Shaken` events when you no longer need the readings, so that the sensor is no longer active to avoid unnecessary battery consumption.
* While iOS features a built-in shake gesture recognition, this is not available on Android and WASM. We use a commonly used implementation that tries to approximate the shake motion to detect it for these platforms. Both Android and WASM use the same implementation. If this is not sufficient for your use case, you can implement Shake detection utilizing the `ReadingChanged` event readings.
* On Android, when both `ReadingChanged` and `Shaken` events are attached and the user sets the `ReportInterval` to a high value, the `ReadingChanged` event may be raised more often than requested. This is because for multiple subscribers to the same sensor, the system may raise the sensor events with the frequency of the one with the lowest requested report delay. This is, however, in line with the behavior of the WinUI Accelerometer, and you can filter the events as necessary for your use case.
* `ReportInterval` property on WASM is not supported directly, and we use an approximation in the form of raising the `ReadingChanged` event only when enough time has passed since the last report. The event is actually raised a bit more often to make sure the gap caused by the filter is not too large, but this is in line with the behavior of the WinUI Accelerometer.

## Examples

### Capturing sensor readings

```csharp
var accelerometer = Accelerometer.GetDefault();
accelerometer.ReadingChanged += Accelerometer_ReadingChanged;

private async void Accelerometer_ReadingChanged(Accelerometer sender, AccelerometerReadingChangedEventArgs args)
{
    // If you want to update the UI in some way, ensure the Dispatcher is used,
    // as the ReadingChanged event handler does not run on the UI thread.
    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
    {
        OutputTextBlock.Text = $"Sensor reading is " +
            $"x = {args.Reading.AccelerationX}, y = {args.Reading.AccelerationY}, z = {args.Reading.AccelerationZ}, " + 
            $"timestamp = {args.Reading.Timestamp}";
    });
}
```

### Detecting shake

```csharp
var accelerometer = Accelerometer.GetDefault();
accelerometer.Shaken += Accelerometer_ReadingChanged;

private async void Accelerometer_Shaken(Accelerometer sender, AccelerometerShakenEventArgs args)
{
    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => ShakenTimestamp = args.Timestamp.ToString("R"));
}
```

### Unsubscribing from the readings

```csharp
accelerometer.ReadingChanged -= Accelerometer_ReadingChanged;
accelerometer.Shaken -= Accelerometer_ReadingChanged;
```

## See Accelerometer in action

* To see this API in action, visit the [example application for non-UI APIs](https://aka.platform.uno/demo-unexpected-apis).
