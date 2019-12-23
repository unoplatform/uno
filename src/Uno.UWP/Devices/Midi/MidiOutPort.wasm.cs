#if __WASM__
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Uno.Devices.Enumeration.Internal;
using Uno.Devices.Enumeration.Internal.Providers.Midi;
using Uno.Extensions;
using Uno.Logging;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using static Uno.Foundation.WebAssemblyRuntime;

namespace Windows.Devices.Midi
{
	public partial class MidiOutPort
	{
		private const string JsType = "Windows.Devices.Midi.MidiOutPort";
		private readonly string _deviceId;

		public MidiOutPort(string deviceId)
		{
			_deviceId = deviceId;
		}

		public static IAsyncOperation<IMidiOutPort> FromIdAsync(string deviceId) =>
			FromIdInternalAsync(deviceId).AsAsyncOperation();

		private static async Task<IMidiOutPort> FromIdInternalAsync(string deviceId)
		{
			var obj = new object();
			var parsedIdentifier = DeviceInformation.ParseDeviceId(deviceId);

			if (obj.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Error))
			{
				obj.Log().Error(parsedIdentifier.deviceClassGuid);
			}

			if (!parsedIdentifier.deviceClassGuid.Equals(DeviceClassGuids.MidiOut, StringComparison.InvariantCultureIgnoreCase))
			{
				throw new InvalidOperationException("Given device is not a MIDI out device");
			}

			//TODO: verify the device exists at time of creation
			if (obj.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Error))
			{
				obj.Log().Error("Created device");
			}
			return new MidiOutPort(parsedIdentifier.id);
		}

		public void SendMessage(IMidiMessage midiMessage)
		{
			if (midiMessage is MidiNoteOnMessage noteOn)
			{
				var command = $"{JsType}.sendNoteMessage(\"{Uri.EscapeDataString(_deviceId)}\",{(int)noteOn.Type},{noteOn.Note},{noteOn.Velocity})";
				InvokeJS(command);
			}
			else if (midiMessage is MidiNoteOffMessage noteOff)
			{
				var command = $"{JsType}.sendNoteMessage(\"{Uri.EscapeDataString(_deviceId)}\",{(int)noteOff.Type},{noteOff.Note},{noteOff.Velocity})";
				InvokeJS(command);
			}
			else
			{
				throw new NotSupportedException("This message is not supported yet");
			}
		}
	}
}
#endif
