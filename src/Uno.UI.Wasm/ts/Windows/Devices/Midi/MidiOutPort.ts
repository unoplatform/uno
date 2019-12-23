namespace Windows.Devices.Midi {
	export class MidiOutPort {
		public static sendDefault(encodedDeviceId: string) {
			var midi = Uno.Devices.Midi.Internal.WasmMidiAccess.getMidi();
			var deviceId = decodeURIComponent(encodedDeviceId);
			var noteOnMessage = [0x90, 60, 0x7f];    // note on, middle C, full velocity
			var output = midi.outputs.get(deviceId);
			output.send(noteOnMessage);  //omitting the timestamp means send immediately.
			output.send([0x80, 60, 0x40], window.performance.now() + 1000.0); // Inlined array creation- note off, middle C,  
			// release velocity = 64, timestamp = now + 1000ms.
		}

		public static sendNoteMessage(encodedDeviceId: string, messageType: number, noteNumber: number, velocity: number) {
			var midi = Uno.Devices.Midi.Internal.WasmMidiAccess.getMidi();
			var deviceId = decodeURIComponent(encodedDeviceId);
			var noteOnMessage = [messageType, noteNumber, velocity];
			var output = midi.outputs.get(deviceId);
			output.send(noteOnMessage);
		}
	}
}
