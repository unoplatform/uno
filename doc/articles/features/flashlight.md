---
uid: Uno.Features.Flashlight
---

# Flashlight

> [!TIP]
> This article covers Uno-specific information for Flashlight. For a full description of the feature and instructions on using it, consult the UWP documentation: https://docs.microsoft.com/en-us/uwp/api/windows.devices.lights.lamp

* `Lamp` API allows you to turn the phone's camera flashlight on and off

## Supported features

| Feature        |  Windows  | Android |  iOS  |  Web (WASM)  | macOS | Linux (Skia)  | Win 7 (Skia) |
|---------------|-------|-------|-------|-------|-------|-------|-|
| `GetDefaultAsync` | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ |
| `IsEnabled`     | ✔ | ✔ | ✔ | ✖ | ✖ | ✖ | ✖ |
| `BrightnessLevel`     | ✔ | ✔ | ✔ | ✖ | ✖ | ✖ | ✖ |

<!-- Add any additional information on platform-specific limitations and constraints -->

## Using `Lamp` with Uno

* The `GetDefaultAsync` method is implemented on all targets and will return `null` where `Lamp` is not supported.
* Make sure to call `Lamp.Dispose()` after use, as implementation on both iOS and Android uses unmanaged resources, and not disposing of them could cause a memory leak. This is in line with UWP, where the `Lamp` needs to be disposed of as well.
* On **iOS**, in case the device supports the torch, `BrightnessLevel` is fully supported. In case the device has only flash, any non-zero `BrightnessLevel` will result in the full brightness of the flashlight.
* On **Android**, flashlight brightness cannot be controlled, hence any non-zero `BrightnessLevel` results in the full brightness of the flashlight.

## Example

```csharp
if (await Lamp.GetDefaultAsync() is Lamp lamp)
{
    lamp.IsEnabled = true; // Turn on the flashlight.

    lamp.BrightnessLevel = 0.5; // Set brightness of 50 %.

    lamp.IsEnabled = false; // Turn off the flashlight.

    lamp.Dispose(); // Stop using flashlight and clean up resources.
}
```
