---
uid: Uno.Features.WSPower
---

# Uno Support for Windows.System.Power APIs

## `PowerManager`

### Limitations

**Android**
- `RemainingDischargeTime` and `RemainingDischargeTimeChanged` events not supported (platform does not offer appropriate APIs)
- `RemainingChargePercentChanged` event is not updated continuously as there is no API that provides such events. It is triggered by system `Low` and `Ok` battery state broadcasts only. The `RemainingChargePercent` property always returns the up-to-date value. For continuous monitoring you can set up periodic polling.

**iOS**
- `RemainingDischargeTime` and `RemainingDischargeTimeChanged` events not supported (platform does not offer appropriate APIs)

**WASM**

Before any of the properties/events of `PowerManager` can be accessed, the class needs to be initialized via a platform-specific `InitializeAsync` method:

```csharp
#if __WASM__
var isAvailable = await PowerManager.InitializeAsync();
#endif
```

If the returned value is `false`, refrain from using `PowerManager` in your code, as it is not available in the browser the user is using.

**Other platforms**
- Not implemented
