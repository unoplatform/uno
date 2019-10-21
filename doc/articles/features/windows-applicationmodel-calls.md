# Uno Support for Windows.ApplicationModel.Calls

## `PhoneCallManager`

### Limitations

**Android**
- `RequestStoreAsync` method is not implemented
- Second parameter of `ShowPhoneCallUI` is ignored (`displayName` of the dialer cannot be set)

**iOS**
- `RequestStoreAsync` and `ShowPhoneCallSettingsUI` methods are not implemented
- Second parameter of `ShowPhoneCallUI` is ignored (`displayName` of the dialer cannot be set)

**WASM**
- Only implements the `ShowPhoneCallUI` method. Similarly to the other platforms the `displayName` parameter is not used
