---
uid: Uno.Features.StepCounter
---

# Step counter (Pedometer)

> [!TIP]
> This article covers Uno-specific information for Pedometer. For a full description of the feature and instructions on using it, see [Pedometer Class](https://learn.microsoft.com/uwp/api/windows.devices.sensors.pedometer).

* The `Windows.Devices.Sensors.Pedometer` class allows counting steps taken with the device.

## Supported features

| Feature           | Windows | Android | iOS | Web (WASM) | Desktop (macOS) | Desktop (X11) | Desktop (Windows) |
|-------------------|---------|---------|-----|------------|-----------------|---------------|-------------------|
| `GetDefaultAsync` | ✔       | ✔       | ✔   | ✔          | ✔               | ✔             | ✔                 |
| `ReadingChanged`  | ✔       | ✔       | ✔   | ✖          | ✖               | ✖             | ✖                 |
| `ReportInterval`  | ✔       | ✔       | ✔   | ✖          | ✖               | ✖             | ✖                 |

## Using Pedometer with Uno

* The `GetDefault` method is available on all targets and will return `null` on those which do not support `Pedometer` or devices that do not have such a sensor.
* Ensure to unsubscribe from the `ReadingChanged` event when you no longer need the readings, so that the sensor is no longer active to avoid unnecessary battery consumption.
* `ReportInterval` property on WASM is currently not supported directly. Uno uses an approximation in the form of raising the `ReadingChanged` event, only when enough time has passed since the last report. The event is raised a bit more often to make sure the gap caused by the filter is not too large, but this is in line with the behavior of Windows' `Pedometer`.
* `DirectionalAccuracy` is not reported on iOS, so it will always return `Unknown`.

## Platform-specific requirements

### [**Android**](#tab/Android)

On Android, the first reading returns the cumulative number of steps since the device was first booted up. The sensor may not correctly respect the requested reporting interval, so the implementation does this manually to make sure the `ReadingChanged` events are triggered only after the `ReportInterval` elapses.

Since Android 10, your application must declare the permission to use the step counter sensor by adding the following `uses-feature` declaration to the `AndroidManifest.xml` in your project:

```xml
<uses-permission android:name="android.permission.ACTIVITY_RECOGNITION" />
```

### [**iOS**](#tab/iOS)

The first reading on iOS returns the cumulative number of steps from 24 hours back to the current moment. Unfortunately, in case the tracking was not enabled before, this will likely return 0 steps. Once the tracking is enabled, `ReadingChanged` will be triggered and the step count will be updated appropriately.

Make sure to add the following capability declaration to your `Info.plist` file, otherwise, the API will crash at runtime.

```xml
<key>NSMotionUsageDescription</key>
<string>Some reason why your app wants to track motion.</string>
```

---

## Example

### Capturing sensor readings

```csharp
var pedometer = await Pedometer.GetDefaultAsync();
pedometer.ReportInterval = 10000;
pedometer.ReadingChanged += Pedometer_ReadingChanged;

private async void Pedometer_ReadingChanged(Pedometer sender, PedometerReadingChangedEventArgs args)
{
    // If you want to update the UI in some way, ensure the Dispatcher is used,
    // as the ReadingChanged event handler does not run on the UI thread.
    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
    {
        CumulativeSteps = args.Reading.CumulativeSteps;
        CumulativeStepsDurationInSeconds = args.Reading.CumulativeStepsDuration.TotalSeconds;
        Timestamp = args.Reading.Timestamp.ToString("R");
    });
}
```
