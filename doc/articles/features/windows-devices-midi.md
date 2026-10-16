---
uid: Uno.Features.WDMidi
---

# MIDI

MIDI device input and output are supported across iOS, Android, macOS, and WebAssembly.

To see how to implement MIDI support in your application, you can follow the sample project provided in the [UWP samples repository](https://github.com/microsoft/Windows-universal-samples/tree/master/Samples/MIDI). This shows MIDI device enumeration, various forms of input as well as output.

In the case of **WebAssembly**, an additional security check is required to be able to receive/send system-exclusive MIDI messages. If you require this functionality, please set the `WinRTFeatureConfiguration.Midi.RequestSystemExclusiveAccess` flag early in the application lifecycle.

```csharp
#if __WASM__
WinRTFeatureConfiguration.Midi.RequestSystemExclusiveAccess = true;
#endif
```

On **WebAssembly**, the browser asks the user for MIDI access the first time devices are enumerated or a port is opened. To ask at a moment of your choosing instead, for example in response to a button click, call `Uno.Devices.Midi.WasmMidiAccess.RequestAsync()`, which returns whether access was granted:

```csharp
#if __WASM__
var granted = await Uno.Devices.Midi.WasmMidiAccess.RequestAsync();
#endif
```
