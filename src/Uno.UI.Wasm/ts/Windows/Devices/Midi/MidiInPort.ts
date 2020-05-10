namespace Windows.Devices.Midi {
	export class MidiInPort {
		private static dispatchMessage: (serializedMessage: string) => number;

		private static instanceMap: Array;

		public static startMessageListener(encodedDeviceId: string) {
			if (window.DeviceMotionEvent) {
				this.dispatchMessage = (<any>Module).mono_bind_static_method("[Uno] Windows.Devices.Midi.MidiInPort:DispatchMessage");
			}

			var midi = Uno.Devices.Midi.Internal.WasmMidiAccess.getMidi();
			var deviceId = decodeURIComponent(encodedDeviceId);
			var input = midi.inputs.get(deviceId);
			input.addEventListener("onmidimessage", MidiInPort.messageReceived);
		}

		public static stopMessageListener(encodedDeviceId: string) {
			var midi = Uno.Devices.Midi.Internal.WasmMidiAccess.getMidi();
			var deviceId = decodeURIComponent(encodedDeviceId);
			var input = midi.inputs.get(deviceId);
			input.removeEventListener("onmidimessage", MidiInPort.messageReceived);
		}

		private static messageReceived(event: WebMidi.MIDIMessageEvent) {			
			var serializedMessage = event.timeStamp.toString();
			for (var i = 0; i < event.data.length; i++) {
				serializedMessage += ':' + event.data[i];
			}
			MidiInPort.dispatchMessage(serializedMessage);
		}
	}
}
