using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using CoreMidi;
using Uno.Devices.Enumeration.Internal;
using Uno.Devices.Enumeration.Internal.Providers.Midi;
using Windows.Storage.Streams;

namespace Windows.Devices.Midi
{
	public partial class MidiInPort
	{
		private MidiEndpoint _endpoint;
		private MidiClient _client;
		private MidiPort _port;

		private MidiInPort(string deviceId, MidiEndpoint endpoint)
		{
			DeviceId = deviceId;
			_endpoint = endpoint;
			_client = new MidiClient(Guid.NewGuid().ToString());
			_port = _client.CreateInputPort(_endpoint.EndpointName);
		}

		partial void StartMessageReceived()
		{
			_port.ConnectSource(_endpoint);
			_port.MessageReceived += NativePortMessageReceived;
		}

		partial void StopMessageReceived()
		{
			_port.MessageReceived -= NativePortMessageReceived;
			_port.Disconnect(_endpoint);
		}

		partial void DisposeNative()
		{

			_port?.Dispose();
			_client?.Dispose();
			_endpoint?.Dispose();
			_port = null;
			_client = null;
			_endpoint = null;
		}

		private static Task<MidiInPort> FromIdInternalAsync(DeviceIdentifier identifier)
		{
			var provider = new MidiInDeviceClassProvider();
			var nativeDeviceInfo = provider.GetNativeEndpoint(identifier.Id);
			if (nativeDeviceInfo == null)
			{
				throw new InvalidOperationException(
					"Given MIDI out device does not exist or is no longer connected");
			}

			return Task.FromResult(new MidiInPort(identifier.ToString(), nativeDeviceInfo));
		}


		private void NativePortMessageReceived(object sender, MidiPacketsEventArgs e)
		{
			foreach (var packet in e.Packets)
			{
				var bytes = new byte[packet.Length];
#pragma warning disable CS0618 // MidiPacket.Bytes is obsolete. We should use MidiPacket.ByteArray once we have https://github.com/xamarin/xamarin-macios/pull/20540.
				Marshal.Copy(packet.Bytes, bytes, 0, packet.Length);
#pragma warning restore CS0618
				OnMessageReceived(bytes, 0, bytes.Length, TimeSpan.FromMilliseconds(packet.TimeStamp));
			}
		}
	}
}
