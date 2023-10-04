namespace Uno.Devices.Midi.Internal {
	export class WasmMidiAccess {
		private static midiAccess: WebMidi.MIDIAccess;

		public static request(systemExclusive: boolean): Promise<string> {
			if (navigator.requestMIDIAccess) {
				return navigator.requestMIDIAccess({ sysex: systemExclusive })
					.then(
						(midi: WebMidi.MIDIAccess) => {
							WasmMidiAccess.midiAccess = midi;
							return "true";
						},
						() => "false");
			}
			else {
				return Promise.resolve("false");
			}
		}

		public static getMidi(): WebMidi.MIDIAccess {
			return WasmMidiAccess.midiAccess;
		}
	}
}
