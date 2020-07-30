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
				MidiDeviceConnectionWatcher.dispatchStateChanged =
					(<any>Module).mono_bind_static_method(
						"[Uno] Uno.Devices.Enumeration.Internal.Providers.Midi.MidiDeviceConnectionWatcher:DispatchStateChanged");
			}

			const port = event.port;
			const isInput = port.type == "input";
			const isConnected = port.state == "connected";
			MidiDeviceConnectionWatcher.dispatchStateChanged(port.id, port.name, isInput, isConnected);
		}
	}
}
