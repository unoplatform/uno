using System;
using System.Threading.Tasks;
using Uno.Devices.Enumeration.Internal;
using Uno.Devices.Enumeration.Internal.Providers.Midi;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using CoreMidi;
using MidiInfo = CoreMidi.Midi;
using System.Net;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Storage.Streams;

namespace Windows.Devices.Midi
{
	public partial class MidiOutPort : IDisposable
	{
		private MidiEndpoint _endpoint;
		private MidiClient _client;
		private MidiPort _port;

		private MidiOutPort(string deviceId, MidiEndpoint endpoint)
		{
			DeviceId = deviceId;
			_endpoint = endpoint;
			_client = new MidiClient(Guid.NewGuid().ToString());
		}

		internal void Open()
		{
			_port = _client.CreateOutputPort(_endpoint.EndpointName);
		}

		public void Dispose()
		{
			_port?.Disconnect(_endpoint);
			_port?.Dispose();
			_client?.Dispose();
			_endpoint?.Dispose();
			_port = null;
			_client = null;
			_endpoint = null;
		}

		private static Task<IMidiOutPort> FromIdInternalAsync(DeviceIdentifier identifier)
		{
			var provider = new MidiOutDeviceClassProvider();
			var nativeDeviceInfo = provider.GetNativeEndpoint(identifier.Id);
			if (nativeDeviceInfo == null)
			{
				throw new InvalidOperationException(
					"Given MIDI out device does not exist or is no longer connected");
			}

			var port = new MidiOutPort(identifier.ToString(), nativeDeviceInfo);
			port.Open();
			return Task.FromResult<IMidiOutPort>(port);
		}

		private void SendBufferInternal(IBuffer midiData, TimeSpan timestamp)
		{
			if (midiData is null)
			{
				throw new ArgumentNullException(nameof(midiData));
			}

			if (_port == null)
			{
				throw new InvalidOperationException("Output port is not initialized.");
			}

			var data = midiData.ToArray();

			var packet = new MidiPacket(0, data, 0, data.Length);
			var packets = new MidiPacket[] { packet };

#pragma warning disable BI1234
			_port.Send(_endpoint, packets);
#pragma warning restore BI1234
		}
	}
}
