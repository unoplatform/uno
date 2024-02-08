---
uid: Uno.Features.WSPower
---

# Battery Information

> [!TIP]
> This article covers Uno-specific information for the `Windows.System.Power` namespace. For a full description of the feature and instructions on using it, see [Windows.System.Power Namespace](https://learn.microsoft.com/uwp/api/windows.system.power).

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

For general usage, see [PowerManager Class](https://learn.microsoft.com/uwp/api/windows.system.power.powermanager).

## Platform-specific requirements

### Android

For Android, there is one permission you must configure before using this API in your project. To do that, add the following to `AndroidManifest.xml`:

```xml
<uses-permission android:name="android.permission.BATTERY_STATS" />
```

### Limitations

#### Android

`RemainingChargePercentChanged` event is not updated continuously as there is no API that provides such events. It is triggered by system `Low` and `Ok` battery state broadcasts only. The `RemainingChargePercent` property always returns the up-to-date value. For continuous monitoring you can set up periodic polling.

#### WebAssembly

Before any of the properties/events of `PowerManager` can be accessed, the class needs to be initialized via a platform-specific `InitializeAsync` method:

```csharp
#if HAS_UNO
var isAvailable = await PowerManager.InitializeAsync();
#endif
```

If the returned value is `false`, refrain from using `PowerManager` in your code, as it is not available in the browser the user is using.
