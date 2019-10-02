# Uno Support for Windows.System.Power APIs

## `PowerManager`

### Limitations

**Android**
- `RemainingDischargeTime` and `RemainingDischargeTimeChanged` events not supported (platform does not offer appropriate APIs)
- `RemainingChargePercentChanged` event is not updated continuously as there is no API that provides such events. It is triggered by system `Low` and `Ok` battery state broadcasts only. The `RemainingChargePercent` property always returns the up-to-date value. For continuous monitoring you can set up periodic polling.

**iOS**
- `RemainingDischargeTime` and `RemainingDischargeTimeChanged` events not supported (platform does not offer appropriate APIs)

**Other platforms**
- Not implemented