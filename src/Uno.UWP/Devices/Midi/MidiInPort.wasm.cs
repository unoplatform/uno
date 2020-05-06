using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno.Devices.Enumeration.Internal;

namespace Windows.Devices.Midi
{
    public partial class MidiInPort
    {
		private const string JsType = "Windows.Devices.Midi.MidiInPort";
		private readonly string _wasmId;

		private MidiInPort(string deviceId, string wasmId)
		{
			DeviceId = deviceId;
			_wasmId = wasmId;
		}

		public void Dispose()
		{
		}

		private static async Task<MidiInPort> FromIdInternalAsync(DeviceIdentifier identifier) =>
			new MidiInPort(identifier.ToString(), identifier.Id);
	}
}
