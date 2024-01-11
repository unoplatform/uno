---
uid: Uno.Features.ProximitySensor
---

# Proximity sensor

> [!TIP]
> This article covers Uno-specific information for `ProximitySensor`. For a full description of the feature and instructions on using it, consult the UWP documentation: https://docs.microsoft.com/en-us/uwp/api/windows.devices.sensors.ProximitySensor

* The `Windows.Devices.Sensors.ProximitySensor` class allows measuring distance of an object in millimeters.

## Using `ProximitySensor` with Uno

* The sensor is currently only available on Android.
* To retrieve the available sensors, `DeviceInformation.FindAllAsync` method is used.

## Example

### Capturing sensor readings

```csharp
var selector = ProximitySensor.GetDeviceSelector();
var devices = await DeviceInformation.FindAllAsync(selector);
var device = devices.FirstOrDefault();
if (device is not null)
{
    var proximitySensor = ProximitySensor.FromId(device.Id);
    proximitySensor.ReadingChanged += ProximitySensor_ReadingChanged;
}

// ..

private async void ProximitySensor_ReadingChanged(ProximitySensor sender, ProximitySensorReadingChangedEventArgs args)
{
    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
    {
        uint? distanceInMillimeters = args.Reading.DistanceInMillimeters;
        bool isDetected = args.Reading.IsDetected;
    });
}
```

### Unsubscribing from the readings

```csharp
proximitySensor.ReadingChanged -= ProximitySensor_ReadingChanged;
```
