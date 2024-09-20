namespace Uno.Devices.Enumeration.Internal.Providers.Midi {
	export class MidiDeviceConnectionWatcher {
		private static dispatchStateChanged: (id: string, name: string, isInput: boolean, isConnected: boolean) => number;

		public static startStateChanged() {
			const midi = Uno.Devices.Midi.Internal.WasmMidiAccess.getMidi();
			midi.addEventListener("statechange", MidiDeviceConnectionWatcher.onStateChanged);
		}

		public static stopStateChanged() {
			const midi = Uno.Devices.Midi.Internal.WasmMidiAccess.getMidi();
			midi.removeEventListener("statechange", MidiDeviceConnectionWatcher.onStateChanged);
		}

		public static onStateChanged(event: WebMidi.MIDIConnectionEvent) {
			if (!MidiDeviceConnectionWatcher.dispatchStateChanged) {
				if ((<any>globalThis).DotnetExports !== undefined) {
					MidiDeviceConnectionWatcher.dispatchStateChanged = (<any>globalThis).DotnetExports.Uno.Uno.Devices.Enumeration.Internal.Providers.Midi.MidiDeviceConnectionWatcher.DispatchStateChanged;
				} else {
					throw `Unable to find dotnet exports`;
				}
			}

			const port = event.port;
			const isInput = port.type == "input";
			const isConnected = port.state == "connected";
			MidiDeviceConnectionWatcher.dispatchStateChanged(port.id, port.name, isInput, isConnected);
		}
	}
}
