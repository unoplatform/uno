namespace Windows.Devices.Midi {
	export class MidiOutPort {
		public static sendBuffer(encodedDeviceId: string, timestamp: number, ...args: number[]) {
			const midi = Uno.Devices.Midi.Internal.WasmMidiAccess.getMidi();
			const deviceId = decodeURIComponent(encodedDeviceId);
			const output = midi.outputs.get(deviceId);
			output.send(args, timestamp);
		}
	}
}
