#if __IOS__ || __MACOS__
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using CoreMidi;
using MidiInfo = CoreMidi.Midi;

namespace Uno.Devices.Enumeration.Internal.Providers.Midi
{
	internal abstract class MidiDeviceClassProviderBase : IDeviceClassProvider
	{
		// https://github.com/xamarin/ios-samples/tree/master/CoreMidiSample
		private readonly bool _isInput = false;

		private MidiClient _client;

		public MidiDeviceClassProviderBase(bool isInput) => _isInput = isInput;

		public bool CanWatch => true;

		public event EventHandler<DeviceInformation> WatchAdded;
		public event EventHandler<DeviceInformation> WatchEnumerationCompleted;
		public event EventHandler<DeviceInformationUpdate> WatchRemoved;
		public event EventHandler<object> WatchStopped;
		public event EventHandler<DeviceInformationUpdate> WatchUpdated;

		public Task<DeviceInformation[]> FindAllAsync() =>
			Task.FromResult(GetMidiDevices().ToArray());

		public void WatchStart()
		{
			MidiInfo.Restart();
			_client = new MidiClient(Guid.NewGuid().ToString());

			var devices = GetMidiDevices().ToArray();
			foreach (var device in devices)
			{
				WatchAdded?.Invoke(this, device);
			}
			OnEnumerationCompleted(devices.LastOrDefault());
			_client.ObjectAdded += ClientChanged;
			_client.ObjectRemoved += ClientChanged;
		}

		private void ClientChanged(object sender, ObjectAddedOrRemovedEventArgs e)
		{

		}

		public void WatchStop()
		{
			if (_client == null)
			{
				return;
			}

			_client.ObjectAdded -= ClientChanged;
			_client.ObjectRemoved -= ClientChanged;
			_client = null;
			_client.Dispose();
			WatchStopped?.Invoke(this, null);
		}

		internal MidiEndpoint GetNativeEndpoint(string midiDeviceId)
		{
			var parsed = ParseMidiDeviceId(midiDeviceId);
			if (_isInput)
			{
				for (int inputId = 0; inputId < MidiInfo.SourceCount; inputId++)
				{
					var source = MidiEndpoint.GetSource(inputId);

					if (source.EndpointName == parsed)
					{
						return source;
					}
				}
			}
			else
			{
				for (int outputId = 0; outputId < MidiInfo.DestinationCount; outputId++)
				{
					var destination = MidiEndpoint.GetDestination(outputId);

					if (destination.EndpointName == parsed)
					{
						return destination;
					}
				}
			}
			return null;
		}

		//private void OnDeviceAdded(MidiDeviceInfo deviceInfo)
		//{
		//	//foreach (var port in deviceInfo.GetPorts().Where(p => p.Type == _portType))
		//	//{
		//	//	WatchAdded?.Invoke(this, CreateDeviceInformation(deviceInfo, port));
		//	//}
		//}

		private void OnDeviceRemoved()//MidiDeviceInfo deviceInfo)
		{
			//foreach (var port in deviceInfo.GetPorts().Where(p => p.Type == _portType))
			//{
			WatchRemoved?.Invoke(this, null);
			//}
		}

		private void OnDeviceUpdated()//MidiDeviceStatus status)
		{
			//foreach (var port in status.DeviceInfo.GetPorts().Where(p => p.Type == _portType))
			//{
			WatchUpdated?.Invoke(this, null);
			//}
		}

		private void OnEnumerationCompleted(DeviceInformation lastDeviceInformation)
		{
			WatchEnumerationCompleted?.Invoke(this, lastDeviceInformation);
		}

		private DeviceInformation CreateDeviceInformation(MidiEndpoint endpoint)
		{
			var deviceIdentifier = new DeviceIdentifier(
					GetMidiDeviceId(endpoint),
					_isInput ? DeviceClassGuids.MidiIn : DeviceClassGuids.MidiOut);
			var deviceInformation = new DeviceInformation(deviceIdentifier)
			{
				Name = endpoint.DisplayName
			};
			return deviceInformation;
		}

		private static string ParseMidiDeviceId(string id)
		{
			return id;
		}

		private static string GetMidiDeviceId(MidiEndpoint endpoint)
		{
			return endpoint.EndpointName;
		}

		private IEnumerable<DeviceInformation> GetMidiDevices()
		{
			if (_isInput)
			{
				for (int inputId = 0; inputId < MidiInfo.SourceCount; inputId++)
				{
					var source = MidiEndpoint.GetSource(inputId);
					yield return CreateDeviceInformation(source);
				}
			}
			else
			{
				for (int outputId = 0; outputId < MidiInfo.DestinationCount; outputId++)
				{
					var destination = MidiEndpoint.GetDestination(outputId);
					yield return CreateDeviceInformation(destination);
				}
			}
		}

		//private DeviceInformationUpdate CreateDeviceInformationUpdate(MidiDevice device)
		//{
		//	var deviceInformation = new DeviceInformationUpdate(
		//		_isInput ? DeviceClassGuids.MidiIn : DeviceClassGuids.MidiOut,
		//		device.DeviceID);
		//	return deviceInformation;
		//}
	}
}
#endif
