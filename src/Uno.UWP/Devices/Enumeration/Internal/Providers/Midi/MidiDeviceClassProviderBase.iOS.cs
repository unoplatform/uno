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
		private readonly bool _isInput;

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
			_client.ObjectAdded += MidiObjectAdded;
			_client.ObjectRemoved += MidiObjectRemoved;
			_client.PropertyChanged += MidiObjectChanged;
		}

		private void MidiObjectAdded(object sender, ObjectAddedOrRemovedEventArgs e)
		{
			if (e.Child is MidiEndpoint endpoint)
			{
				OnDeviceAdded(endpoint);
			}
		}

		private void MidiObjectRemoved(object sender, ObjectAddedOrRemovedEventArgs e)
		{
			if (e.Child is MidiEndpoint endpoint)
			{
				OnDeviceRemoved(endpoint);
			}
		}

		private void MidiObjectChanged(object sender, ObjectPropertyChangedEventArgs e)
		{
			if (e.MidiObject is MidiEndpoint endpoint)
			{
				OnDeviceUpdated(endpoint);
			}
		}

		public void WatchStop()
		{
			if (_client == null)
			{
				return;
			}

			_client.ObjectAdded -= MidiObjectAdded;
			_client.ObjectRemoved -= MidiObjectRemoved;
			_client.PropertyChanged -= MidiObjectChanged;
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

		private void OnDeviceAdded(MidiEndpoint endpoint)
		{
			var newDevice = CreateDeviceInformation(endpoint);
			WatchAdded?.Invoke(this, newDevice);
		}

		private void OnDeviceRemoved(MidiEndpoint endpoint)
		{
			var update = CreateDeviceInformationUpdate(endpoint);
			WatchRemoved?.Invoke(this, update);
		}

		private void OnDeviceUpdated(MidiEndpoint endpoint)
		{
			var update = CreateDeviceInformationUpdate(endpoint);
			WatchUpdated?.Invoke(this, update);
		}

		private void OnEnumerationCompleted(DeviceInformation lastDeviceInformation)
		{
			WatchEnumerationCompleted?.Invoke(this, lastDeviceInformation);
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

		private DeviceInformationUpdate CreateDeviceInformationUpdate(MidiEndpoint device)
		{
			var deviceIdentifier = new DeviceIdentifier(
				GetMidiDeviceId(device),
				_isInput ? DeviceClassGuids.MidiIn : DeviceClassGuids.MidiOut);
			return new DeviceInformationUpdate(deviceIdentifier);
		}

		private static string ParseMidiDeviceId(string id) => id;

		private static string GetMidiDeviceId(MidiEndpoint endpoint) => endpoint.EndpointName;
	}
}
