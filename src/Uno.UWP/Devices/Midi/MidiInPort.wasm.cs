using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
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

		private readonly static ConcurrentDictionary<string, MidiInPort> _instanceSubscriptions =
			new ConcurrentDictionary<string, MidiInPort>();

		private MidiInPort(string deviceId, string wasmId)
		{
			DeviceId = deviceId;
			_wasmId = wasmId;
		}

		public static int DispatchMessage(string serializedMessage)
		{
			var splitMessage = serializedMessage.Split(':');
			var timestamp = TimeSpan.FromMilliseconds(double.Parse(splitMessage[0], CultureInfo.InvariantCulture));
			var message = new byte[splitMessage.Length - 1];
			for (int i = 1; i < splitMessage.Length; i++)
			{
				message[i - 1] = byte.Parse(splitMessage[i]);
			}

			OnMessageReceived(message, 0, message.Length, TimeSpan.FromMilliseconds(timestamp));

			return 0;
		}

		public void Dispose()
		{
		}

		private static async Task<MidiInPort> FromIdInternalAsync(DeviceIdentifier identifier)
		{
			return new MidiInPort(identifier.ToString(), identifier.Id);
		}
	}
}
