# Uno Support for Windows.Devices.Sensors

## `Barometer`

### Limitations

**Android**
- Only `ReadingChanged` event is supported, system does not support retrieval of a single reading

**iOS**
- Only `ReadingChanged` event is supported, system does not support retrieval of a single reading

**WASM**
- No barometer API is currently available in JS