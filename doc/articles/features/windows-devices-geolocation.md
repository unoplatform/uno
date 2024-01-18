---
uid: Uno.Features.WDGeolocation
---

# Geolocation

> [!TIP]
> This article covers Uno-specific information for Geolocation. For a full description of the feature and instructions on using it, consult the UWP documentation: https://learn.microsoft.com/en-us/uwp/api/windows.devices.geolocation

* The `Windows.Devices.Geolocation.Geolocator` provides APIs for getting the current location or tracking the device's location over time. Location information may come from estimating a position from beacons like Wi-Fi access points and cell towers, from the device's IP address, or may come from other sources such as a GNSS or GPS device.

## `Geolocator`

The following `Geolocator` APIs are implemented on Android, iOS, macOS and WASM:

* `RequestAccessAsync`
* `GetGeolocationAsync`
* `PositionChanged`
* `StatusChanged`
* `DefaultGeoposition`
* `IsDefaultGeopositionRecommended`
* `LocationStatus`
* `DesiredAccuracy`
* `DesiredAccuracyInMeters`

### Implementation notes

`StatusChanged` event is delivered to all `Geolocator` instances which have subscribers as a "broadcast". This is unusual, but in line with the UWP implementation.

#### WASM

On WebAssembly, `RequestAccessAsync` requires querying for position info, which triggers the user permission dialog. For the developer, it should behave the same way as in UWP. We recommend showing the user instructions on how to enable geolocation if it is `Denied` via a dialog message for example, if you require the feature for the functionality of your app.

`GetGeolocationAsync` creates a single request for position. The parameterless version of the method works a bit differently from UWP - the default timeout period is 60 seconds there, but in reality, the code never waits for so long even if user requests higher accuracy. Hence, the default timeout in Uno Platform implementation is currently set to 10 seconds.

WebAssembly does not allow setting explicit meter-based accuracy for Geolocation and has only a standard and high level of accuracy. The high accuracy option is used when `DesiredAccuracy` is `High` or `DesiredAccuracyInMeters` is lower than 50. The `GetGeolocationAsync` implementation utilizes the set timeout period to try to improve the accuracy of the positioning. If better accuracy is not achieved, the last available result is used.
