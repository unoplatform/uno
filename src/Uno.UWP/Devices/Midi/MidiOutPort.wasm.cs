#if __WASM__
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Uno.Devices.Enumeration.Internal;
using Uno.Devices.Enumeration.Internal.Providers.Midi;
using Uno.Extensions;
using Uno.Logging;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Storage.Streams;
using static Uno.Foundation.WebAssemblyRuntime;

namespace Windows.Devices.Midi
{
	public partial class MidiOutPort
	{
		private const string JsType = "Windows.Devices.Midi.MidiOutPort";
		private readonly string _wasmId;

		private MidiOutPort(string deviceId, string wasmId)
		{
			DeviceId = deviceId;
			_wasmId = wasmId;
		}

		public void Dispose()
		{
		}

		public void SendBufferInternal(IBuffer midiBuffer, TimeSpan timestamp)
		{
			var data = midiBuffer.ToArray();
			var byteString = string.Join(",", data);
			var command = $"{JsType}.sendBuffer(\"{Uri.EscapeDataString(_wasmId)}\",{timestamp.TotalMilliseconds},{byteString})";
			InvokeJS(command);
		}

		private static async Task<IMidiOutPort> FromIdInternalAsync(DeviceIdentifier identifier)
		{
			return new MidiOutPort(identifier.ToString(), identifier.Id);
		}
	}
}
#endif
