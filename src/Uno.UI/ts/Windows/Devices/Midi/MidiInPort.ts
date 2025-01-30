namespace Windows.Devices.Midi {
	export class MidiInPort {
		private static dispatchMessage:
			(instanceId: string, serializedMessage: string, timestamp: number) => number;

		private static instanceMap: { [managedId: string]: MidiInPort } = {};

		private managedId: string;
		private inputPort: WebMidi.MIDIInput;

		private constructor(managedId: string, inputPort: WebMidi.MIDIInput) {
			this.managedId = managedId;
			this.inputPort = inputPort;
		}

		public static createPort(managedId: string, encodedDeviceId: string) {
			const midi = Uno.Devices.Midi.Internal.WasmMidiAccess.getMidi();
			const deviceId = decodeURIComponent(encodedDeviceId);
			const input = midi.inputs.get(deviceId);
			MidiInPort.instanceMap[managedId] = new MidiInPort(managedId, input);
		}

		public static removePort(managedId: string) {
			MidiInPort.stopMessageListener(managedId);
			delete MidiInPort.instanceMap[managedId];
		}

		public static startMessageListener(managedId: string) {
			if (!MidiInPort.dispatchMessage) {
				if ((<any>globalThis).DotnetExports !== undefined) {
					MidiInPort.dispatchMessage = (<any>globalThis).DotnetExports.Uno.Windows.Devices.Midi.MidiInPort.DispatchMessage;
				} else {
					throw `Unable to find dotnet exports`;
				}
			}

			const instance = MidiInPort.instanceMap[managedId];
			instance.inputPort.addEventListener("midimessage", instance.messageReceived);
		}

		public static stopMessageListener(managedId: string) {
			const instance = MidiInPort.instanceMap[managedId];
			instance.inputPort.removeEventListener("midimessage", instance.messageReceived);
		}

		private messageReceived = (event: WebMidi.MIDIMessageEvent) => {
			var serializedMessage = event.data[0].toString();
			for (var i = 1; i < event.data.length; i++) {
				serializedMessage += ':' + event.data[i];
			}
			MidiInPort.dispatchMessage(this.managedId, serializedMessage, event.timeStamp);
		}
	}
}
