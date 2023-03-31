---
uid: Uno.Features.WDEnumeration
---

# Uno Support for Windows.Devices.Enumeration

Device enumeration is partially supported in Uno Platform. It currently supports enumerating the following device classes:

- MIDI input devices
- MIDI output devices

To enumerate devices of a given class, please use the respective `GetDeviceSelector()` method, for example: `MidiInPort.GetDeviceSelector()`.

## For contributors

To add support for a new device class, several steps are required:

1. Add the device class GUID to `Uno.Devices.Enumeration.Internal.DeviceClassGuids`. You can discover the GUID by reading the `GetDeviceSelector` methods output.
2. Add provider implementations to `Uno.UWP\Devices\Enumeration\Internal\Providers`. The providers must implement `IDeviceClassProvider`.
3. Add the provider classes to `DeviceInformation.providers` partial classes in `Uno.UWP\Devices\Enumeration`.
