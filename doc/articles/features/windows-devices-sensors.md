# Uno Support for Windows.Devices.Sensors

## `Accelerometer`

The following APIs are implemented on Android, iOS and WASM:

* `ReadingChanged`
* `Shaken`
* `ReportInterval`

### Implementation notes

While iOS features a built-in shake gesture recognition, this is not available on Android and WASM. For these platforms we use a commonly used implementation which tries to approximate the shake motion to detect it. Both Android and WASM use the same implementation. In case this is not sufficient for your use case, you can implement Shake detection above the `ReadingChanged` event.

`ReportInterval` property on WASM is not supported directly and we use an approximation in the form of raising the `ReadingChanged` event only when enough time has passed since the last report. The event is actually raised a bit more often to make sure the gap caused by the filter is not too large, but this is in-line with the behavior of UWP Accelerometer.

On Android, when both `ReadingChanged` and `Shaken` events are attached and the user sets the `ReportInterval` to a high value, the `ReadingChanged` event may be raised more often than requested. This is because for multiple subscribers to the same sensor the system may raise the sensor events with the frequency of the one with lower requested report delay. This is, however, again in line with the behavior of UWP Accelerometer and you can filter the events as necessary for your use case.

## `Barometer`

### Implementation notes

#### Android

* Only `ReadingChanged` event and `ReportInterval` is supported, system does not support retrieval of a single reading.

#### iOS

* Only `ReadingChanged` event is supported, system does not support retrieval of a single reading. `ReportInterval` is not supported.

#### WASM

* No barometer API is currently available in JS

## `Gyrometer`

### Implementation notes

`ReportInterval` property on WASM is currently not supported directly. Uno uses an approximation in the form of raising the `ReadingChanged` event, only when enough time has passed since the last report. The event is raised a bit more often to make sure the gap caused by the filter is not too large, but this is in-line with the behavior of Windows' `Gyrometer`.

## `Magnetometer`

### Implementation notes

`ReportInterval` property on WASM is currently not supported directly. Uno uses an approximation in the form of raising the `ReadingChanged` event only when enough time has passed since the last report. The event is raised a bit more often to make sure the gap caused by the filter is not too large, but this is in-line with the behavior of UWP `Magnetometer`.

`DirectionalAccuracy` is not reported on iOS, so it will always return `Unknown`.

## `Pedometer`

### Implementation notes

#### Android

On Android, the first reading returns the cumulative number of steps since the device was first booted up. The sensor may not correctly respect the requested reporting interval, so the implementation does this manually to make sure the `ReadingChanged` events are triggered only after the `ReportInterval` elapses.

Since Android 10, your application must declare the permission to use the step counter sensor by adding the following `uses-feature` declaration to the `AndroidManifest.xml` in your project:

```
<uses-permission android:name="android.permission.ACTIVITY_RECOGNITION" />
```

#### iOS

The first reading on iOS returns the cumulative number of steps from 24 hours back to current moment. Unfortunately, in case the tracking was not enabled before, this will likely return 0 steps. Once the tracking is enabled, `ReadingChanged` will be triggered and step count will be updated appropriately.

Make sure to add the following capability declaration to your `Info.plist` file, otherwise the API will crash at runtime.

```
<key>NSMotionUsageDescription</key>
<string>Some reason why your app wants to track motion.</string>
```
