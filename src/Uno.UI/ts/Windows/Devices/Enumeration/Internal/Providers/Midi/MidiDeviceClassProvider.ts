namespace Uno.Devices.Enumeration.Internal.Providers.Midi {
	export class MidiDeviceClassProvider {		
		public static findDevices(findInputDevices: boolean): string {
			var result = "";
			const midi = Uno.Devices.Midi.Internal.WasmMidiAccess.getMidi();
			if (findInputDevices) {
				midi.inputs.forEach((input: WebMidi.MIDIInput, key: string) => {
					const inputId = input.id;
					const name = input.name;
					const encodedMetadata = encodeURIComponent(inputId) + '#' + encodeURIComponent(name);
					result += encodedMetadata + '&';
				});
			}
			else {
				midi.outputs.forEach((output: WebMidi.MIDIOutput, key: string) => {
					const outputId = output.id;
					const name = output.name;
					const encodedMetadata = encodeURIComponent(outputId) + '#' + encodeURIComponent(name);
					result += encodedMetadata + '&';
				});
			}
			return result;
		}
	}
}
