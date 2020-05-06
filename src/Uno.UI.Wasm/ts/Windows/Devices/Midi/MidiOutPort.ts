namespace Windows.Devices.Midi {
	export class MidiOutPort {
		public static sendBuffer(encodedDeviceId: string, timestamp: number, ...args: number[]) {
			var midi = Uno.Devices.Midi.Internal.WasmMidiAccess.getMidi();
			var deviceId = decodeURIComponent(encodedDeviceId);
			var output = midi.outputs.get(deviceId);
			output.send(args, timestamp);
		}
	}
}
