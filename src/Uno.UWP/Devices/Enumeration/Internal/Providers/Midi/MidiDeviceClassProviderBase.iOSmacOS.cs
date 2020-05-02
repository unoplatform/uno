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
			_client = new MidiClient("Watch");

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

		//internal (MidiDeviceInfo device, MidiDeviceInfo.PortInfo port) GetNativeDeviceInfo(string midiDeviceId)
		//{
		//	var parsed = ParseMidiDeviceId(midiDeviceId);
		//	using (var midiManager = ContextHelper.Current.GetSystemService(Context.MidiService).JavaCast<MidiManager>())
		//	{
		//		return midiManager
		//			.GetDevices()
		//			.Where(d => d.Id == parsed.id)
		//			.SelectMany(d =>
		//				d.GetPorts()
		//					.Where(p =>
		//						p.Type == _portType &&
		//						p.PortNumber == parsed.portNumber)
		//					.Select(p => (device: d, port: p)))
		//			.FirstOrDefault();
		//	}
		//}

		private void OnDeviceAdded(MidiDeviceInfo deviceInfo)
		{
			//foreach (var port in deviceInfo.GetPorts().Where(p => p.Type == _portType))
			//{
			//	WatchAdded?.Invoke(this, CreateDeviceInformation(deviceInfo, port));
			//}
		}

		private void OnDeviceRemoved(MidiDeviceInfo deviceInfo)
		{
			//foreach (var port in deviceInfo.GetPorts().Where(p => p.Type == _portType))
			//{
			//	WatchRemoved?.Invoke(this, CreateDeviceInformationUpdate(deviceInfo, port));
			//}
		}

		private void OnDeviceUpdated(MidiDeviceStatus status)
		{
			//foreach (var port in status.DeviceInfo.GetPorts().Where(p => p.Type == _portType))
			//{
			//	WatchUpdated?.Invoke(this, CreateDeviceInformationUpdate(status.DeviceInfo, port));
			//}
		}

		private void OnEnumerationCompleted(DeviceInformation lastDeviceInformation)
		{
			WatchEnumerationCompleted?.Invoke(this, lastDeviceInformation);
		}

		private bool DeviceMatchesType(MidiDevice info) =>
			_isInput ?
				info.InputPortCount > 0 : info.OutputPortCount > 0;

		private DeviceInformation CreateDeviceInformation(MidiDevice device)
		{
			
		}


		//private static (int id, int portNumber) ParseMidiDeviceId(string id)
		//{
		//	var parts = id.Split("_");
		//	var intId = int.Parse(parts[0]);
		//	var portNumber = int.Parse(parts[1]);
		//	return (intId, portNumber);
		//}

		//private static string GetMidiDeviceId(MidiDeviceInfo deviceInfo, MidiDeviceInfo.PortInfo portInfo)
		//{
		//	return $"{deviceInfo.Id.ToString()}_{portInfo.PortNumber}";
		//}

		private IEnumerable<DeviceInformation> GetMidiDevices()
		{
			for (int i = 0; i < MidiInfo.DeviceCount; i++)
			{
				var device = MidiInfo.GetDevice(i);
				foreach (var deviceInfo in ReadDeviceInformation(device))
				{
					yield return deviceInfo;
				}
			}
			for (int i = 0; i < MidiInfo.ExternalDeviceCount; i++)
			{
				var device = MidiInfo.GetExternalDevice(i);
				foreach (var deviceInfo in ReadDeviceInformation(device))
				{
					yield return deviceInfo;
				}
			}
		}

		private IEnumerable<DeviceInformation> ReadDeviceInformation(MidiDevice device)
		{
			var name = "";
			for (int i = 0; i < device.EntityCount; i++)
			{
				var entity = device.GetEntity(i);
				if (_isInput)
				{
					for (int inputId = 0; inputId < entity.Sources; inputId++)
					{
						var source = entity.GetSource(inputId);

						source
					}
				}
				else
				{
					for (int inputId = 0; inputId < entity.Destinations; inputId++)
					{
						var source = entity.GetSource(inputId);

						source
					}
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
