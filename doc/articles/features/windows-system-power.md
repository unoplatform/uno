---
uid: Uno.Features.WSPower
---

# Battery Information

> [!TIP]
> This article covers Uno-specific information for `Windows.System.Power` namespace. For a full description of the feature and instructions on using it, consult the UWP documentation: https://learn.microsoft.com/en-us/uwp/api/windows.system.power

* You can use the `Windows.System.Power.PowerManager` class to query the battery and charging status of the device and subscribe to the related events when these change.

## `PowerManager`

**Legend**

* ✔  Supported
* ✖ Not supported

| Picker         | UWP/WinUI   | WebAssembly | Android | iOS   | macOS | WPF | GTK |
|----------------|-------|-------------|---------|-------|-------|-----|-----|
| `BatteryStatus` | ✔   | ✔  | ✔     | ✔    |✖ ️   | ✖ | ✖ ️  |
| `EnergySaverStatus` | ✔   |  ✖ | ✔     | ✔    |✖ ️   | ✖ | ✖ ️  |
| `PowerSupplyStatus` | ✔   | ✔  | ✔     | ✔   |✖ ️   | ✖ | ✖ ️  |
| `RemainingChargePercent` | ✔   | ✔ | ✔     | ✔   |✖ ️   | ✖ | ✖ ️  |
| `RemainingDischargeTime` | ✔   | ✔ |  ✖    | ✖ |✖ ️   | ✖ | ✖ ️  |
| `BatteryStatusChanged` | ✔   | ✔  | ✔     | ✔   |✖ ️   | ✖ | ✖ ️  |
| `EnergySaverStatusChanged` | ✔   |  ✖ | ✔     | ✔    |✖ ️   | ✖ | ✖ ️  |
| `PowerSupplyStatusChanged` | ✔   | ✔  | ✔     | ✔   |✖ ️   | ✖ | ✖ ️  |
| `RemainingChargePercentChanged` | ✔   | ✔| ✔     | ✔   |✖ ️   | ✖ | ✖ ️  |
| `RemainingDischargeTimeChanged` | ✔   | ✔     |  ✖     |  ✖  |✖ ️   | ✖ | ✖ ️  |

### Usage

For general usage you can follow the documentation provided by [Microsoft](https://learn.microsoft.com/en-us/uwp/api/windows.system.power.powermanager).

### Limitations

#### Android

`RemainingChargePercentChanged` event is not updated continuously as there is no API that provides such events. It is triggered by system `Low` and `Ok` battery state broadcasts only. The `RemainingChargePercent` property always returns the up-to-date value. For continuous monitoring you can set up periodic polling.

#### WebAssembly

Before any of the properties/events of `PowerManager` can be accessed, the class needs to be initialized via a platform-specific `InitializeAsync` method:

```csharp
#if __HAS_UNO__
var isAvailable = await PowerManager.InitializeAsync();
#endif
```

If the returned value is `false`, refrain from using `PowerManager` in your code, as it is not available in the browser the user is using.
