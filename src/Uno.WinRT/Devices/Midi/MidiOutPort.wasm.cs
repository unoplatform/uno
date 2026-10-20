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
using Uno.Foundation.Logging;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Storage.Streams;

using NativeMethods = __Windows.Devices.Midi.MidiOutPort.NativeMethods;

namespace Windows.Devices.Midi
{
	public partial class MidiOutPort
	{
		private readonly string _wasmId;

		private MidiOutPort(string deviceId, string wasmId)
		{
			DeviceId = deviceId;
			_wasmId = wasmId;
		}

		public void Dispose()
		{
		}

		internal void SendBufferInternal(IBuffer midiBuffer, TimeSpan timestamp)
		{
			var data = midiBuffer.ToArray();
			NativeMethods.SendBuffer(Uri.EscapeDataString(_wasmId), timestamp.TotalMilliseconds, data);
		}

		private static Task<IMidiOutPort> FromIdInternalAsync(DeviceIdentifier identifier)
		{
			return Task.FromResult<IMidiOutPort>(new MidiOutPort(identifier.ToString(), identifier.Id));
		}
	}
}
