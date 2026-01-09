---
uid: Uno.Features.Compass
---

# Compass

> [!TIP]
> This article covers Uno-specific information for `Compass`. For a full description of the feature and instructions on using it, see [Compass Class](https://learn.microsoft.com/uwp/api/windows.devices.sensors.Compass).

* The `Windows.Devices.Sensors.Compass` class returns a heading with respect to Magnetic North and, possibly, True North.

## Supported features

| Feature          | Windows | Android | iOS | Web (WASM) | Desktop (macOS) | Desktop (X11) | Desktop (Windows) |
|------------------|---------|---------|-----|------------|-----------------|---------------|-------------------|
| `GetDefault`     | ✔       | ✔       | ✔   | ✔          | ✔               | ✔             | ✔                 |
| `ReadingChanged` | ✔       | ✔       | ✔   | ✔          | ✖               | ✖             | ✖                 |
| `ReportInterval` | ✔       | ✔       | ✔   | ✖          | ✖               | ✖             | ✖                 |

## Using Compass with Uno

* The `GetDefault` method is available on all targets and will return `null` on those which do not support `Compass` or devices that do not have such a sensor.
* Ensure to unsubscribe from the `ReadingChanged` event when you no longer need the readings, so that the sensor is no longer active to avoid unnecessary battery consumption.

## Platform-specific

### Android

If you are planning to use the `HeadingTrueNorth`, your app must declare `android.permission.ACCESS_FINE_LOCATION` permission, otherwise the value will return `null`:

```csharp
[assembly: UsesPermission("android.permission.ACCESS_FINE_LOCATION")]
```

> [!NOTE]
> Android lacks a dedicated API for accessing the compass heading. Uno Platform utilizes the accelerometer and magnetometer sensors to calculate the magnetic north heading, a method endorsed by Google.
>
> In rare cases, you may encounter inconsistent results due to the need for sensor calibration. The process of recalibrating the compass on Android varies across phone models and Android versions. To recalibrate, consult online resources specific to your device. Here are two links that may assist you in recalibrating the compass:
>
> [Google Help Center: Find and improve your location’s accuracy](https://support.google.com/maps/answer/2839911)
> [Stack Exchange Android Enthusiasts: How can I calibrate the compass on my phone?](https://android.stackexchange.com/questions/10145/how-can-i-calibrate-the-compass-on-my-phone)
>
> It's also important to note that concurrently running multiple sensors in your app may impact sensor speed.

### Web (WASM)

The `Magnetometer` sensor is not currently supported by default on any of the popular browsers. However, users can enable this feature on some browsers. For more information on how to do this, please visit the [Mozilla documentation](https://developer.mozilla.org/en-US/docs/Web/API/Magnetometer) on using the Magnetometer API.

## Example

### Capturing sensor readings

```csharp
var compass = Compass.GetDefault();
compass.ReadingChanged += Compass_ReadingChanged;

private async void Compass_ReadingChanged(Compass sender, CompassReadingChangedEventArgs args)
{
    // If you want to update the UI in some way, ensure the Dispatcher is used,
    // as the ReadingChanged event handler does not run on the UI thread.
    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
    {
        OutputTextBlock.Text = $"HeadingMagneticNorth in degrees = {args.Reading.HeadingMagneticNorth}, " +
            $"HeadingTrueNorth in degrees = {args.Reading.HeadingTrueNorth}, "
            $"timestamp = {args.Reading.Timestamp}";
    });
}
```

### Unsubscribing from the readings

```csharp
Compass.ReadingChanged -= Compass_ReadingChanged;
```
