namespace Uno.Devices.Enumeration.Internal.Providers.Midi {
	export class MidiDeviceClassProvider {		
		public static findDevices(findInputDevices: boolean): string {
			var result = "";
			var midi = Uno.Devices.Midi.Internal.WasmMidiAccess.getMidi();
			if (findInputDevices) {
				midi.inputs.forEach((input: WebMidi.MIDIInput, key: string) => {
					var inputId = input.id;
					var name = input.name;
					var encodedMetadata = encodeURIComponent(inputId) + '#' + encodeURIComponent(name);
					result += encodedMetadata + '&';
				});
			}
			else {
				midi.outputs.forEach((output: WebMidi.MIDIOutput, key: string) => {
					var inputId = output.id;
					var name = output.name;
					var encodedMetadata = encodeURIComponent(inputId) + '#' + encodeURIComponent(name);
					result += encodedMetadata + '&';
				});
			}
			return result;
		}
	}
}
