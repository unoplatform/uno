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
