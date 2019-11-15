# Uno Support for Windows.Devices.Lights

## `Lamp`

The following APIs are implemented on Android and iOS:

* `GetDefaultAsync`
* `IsEnabled`
* `BrightnessLevel`

WASM does not offer an API to control the device flashlight, yet.

### Implementation notes

On iOS, in case the device supports torch, `BrightnessLevel` is fully supported. In case the device has only flash, any non-zero `BrightnessLevel` will result in full brightness of the flashlight.

On Android, flashlight brightness cannot be controlled, hence any non-zero `BrightnessLevel` results in full brightness of the flashlight.

### Usage notes

Make sure to dispose of the `Lamp` instance, as implementation on both iOS and Android uses unmanaged resources and not disposing of them would cause a memory leak. This is in line with UWP, where the `Lamp` needs to be disposed as well.
