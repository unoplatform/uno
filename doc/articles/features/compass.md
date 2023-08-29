---
uid: Uno.Features.Compass
---

# Compass

> [!TIP]
> This article covers Uno-specific information for `Compass`. For a full description of the feature and instructions on using it, consult the UWP documentation: https://docs.microsoft.com/en-us/uwp/api/windows.devices.sensors.Compass

 * The `Windows.Devices.Sensors.Compass` class returns a heading with respect to Magnetic North and, possibly, True North.

## Supported features

| Feature        |  Windows  | Android |  iOS  |  Web (WASM)  | macOS | Linux (Skia)  | Win 7 (Skia) | 
|---------------|-------|-------|-------|-------|-------|-------|-|
| `GetDefault`         | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ |
| `ReadingChanged` | ✔ | ✔ | ✔ | ✖ | ✖ | ✖ | ✖ |
| `ReportInterval`     | ✔ | ✔ | ✖ | ✖ | ✖ | ✖ | ✖ |

## Using Compass with Uno
 
 * The `GetDefault` method is available on all targets and will return `null` on those which do not support `Compass` or devices that do not have such a sensor.
 * Ensure to unsubscribe from the `ReadingChanged` event when you no longer need the readings, so that the sensor is no longer active to avoid unnecessary battery consumption.

## Platform-specific

### Android

If you are planning to use the `HeadingTrueNorth`, your app must declare `android.permission.ACCESS_FINE_LOCATION` permission, otherwise the value will return `null`:

```csharp
[assembly: UsesPermission("android.permission.ACCESS_FINE_LOCATION")]
```

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