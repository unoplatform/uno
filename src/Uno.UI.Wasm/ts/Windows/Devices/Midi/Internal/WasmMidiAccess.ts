namespace Uno.Devices.Midi.Internal {
	export class WasmMidiAccess {


		private static midiAccess: WebMidi.MIDIAccess;

		private static dispatchRequest: (hasAccess: boolean) => number;

		private static initialize() {			
			if (!this.dispatchRequest) {
				this.dispatchRequest = (<any>Module).mono_bind_static_method("[Uno] Uno.Devices.Midi.Internal.WasmMidiAccess:DispatchRequest");
			}
		}

		public static request() {
			WasmMidiAccess.initialize();
			if (navigator.requestMIDIAccess) {
				console.log('This browser supports WebMIDI!');
				navigator.requestMIDIAccess()
					.then(
						(midi: WebMidi.MIDIAccess) => {
							WasmMidiAccess.midiAccess = midi;
							return WasmMidiAccess.dispatchRequest(true);
						},
						() => WasmMidiAccess.dispatchRequest(false));
			}
			else {
				console.log('WebMIDI is not supported in this browser.');
				WasmMidiAccess.dispatchRequest(false);
			}
		}

		public static getMidi(): WebMidi.MIDIAccess {
			return this.midiAccess;
		}
	}
}
