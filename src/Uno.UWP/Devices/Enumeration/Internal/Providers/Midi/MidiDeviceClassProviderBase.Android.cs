#if __ANDROID__
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Android.Content;
using Android.Media.Midi;
using Android.Runtime;
using Android.Service.VR;
using Uno.UI;
using Windows.Devices.Enumeration;

namespace Uno.Devices.Enumeration.Internal.Providers.Midi
{
	internal abstract class MidiDeviceClassProviderBase : IDeviceClassProvider
	{
		private readonly MidiPortType _portType;
		private MidiManager _watchMidiManager;
		private DeviceCallback _deviceCallback;

		public MidiDeviceClassProviderBase(MidiPortType portType) => _portType = portType;

		public bool CanWatch => true;

		public event EventHandler<DeviceInformation> WatchAdded;
		public event EventHandler<DeviceInformation> WatchEnumerationCompleted;
		public event EventHandler<DeviceInformationUpdate> WatchRemoved;
		public event EventHandler<object> WatchStopped;
		public event EventHandler<DeviceInformationUpdate> WatchUpdated;

		public Task<DeviceInformation[]> FindAllAsync()
		{
			using (var midiManager = ContextHelper.Current.GetSystemService(Context.MidiService).JavaCast<MidiManager>())
			{
				return Task.FromResult(
					GetMidiDevices(midiManager).ToArray());
			}
		}

		public void WatchStart()
		{
			if (_deviceCallback != null)
			{
				return;
			}

			_watchMidiManager = _watchMidiManager ?? ContextHelper.Current.GetSystemService(Context.MidiService).JavaCast<MidiManager>();

			var devices = GetMidiDevices(_watchMidiManager).ToArray();
			foreach (var device in devices)
			{
				WatchAdded?.Invoke(this, device);
			}
			OnEnumerationCompleted(devices.LastOrDefault());

			_watchMidiManager.RegisterDeviceCallback(_deviceCallback = new DeviceCallback(this), null);
		}

		public void WatchStop()
		{
			if (_deviceCallback == null)
			{
				return;
			}

			_watchMidiManager.UnregisterDeviceCallback(_deviceCallback);
			_deviceCallback?.Dispose();
			_deviceCallback = null;
			_watchMidiManager.Dispose();
			WatchStopped?.Invoke(this, null);
		}

		private void OnDeviceAdded(MidiDeviceInfo deviceInfo)
		{
			foreach (var port in deviceInfo.GetPorts().Where(p => p.Type == _portType))
			{
				WatchAdded?.Invoke(this, CreateDeviceInformation(deviceInfo, port));
			}
		}

		private void OnDeviceRemoved(MidiDeviceInfo deviceInfo)
		{
			foreach (var port in deviceInfo.GetPorts().Where(p => p.Type == _portType))
			{
				WatchRemoved?.Invoke(this, CreateDeviceInformationUpdate(deviceInfo, port));
			}
		}

		private void OnDeviceUpdated(MidiDeviceStatus status)
		{
			foreach (var port in status.DeviceInfo.GetPorts().Where(p => p.Type == _portType))
			{
				WatchUpdated?.Invoke(this, CreateDeviceInformationUpdate(status.DeviceInfo, port));
			}
		}

		private void OnEnumerationCompleted(DeviceInformation lastDeviceInformation)
		{
			WatchEnumerationCompleted?.Invoke(this, lastDeviceInformation);
		}

		private bool DeviceMatchesType(MidiDeviceInfo info) =>
			_portType == MidiPortType.Input ?
				info.InputPortCount > 0 : info.OutputPortCount > 0;

		private DeviceInformation CreateDeviceInformation(MidiDeviceInfo deviceInfo, MidiDeviceInfo.PortInfo portInfo)
		{
			var name = "";
			if (deviceInfo.Properties.ContainsKey(MidiDeviceInfo.PropertyName))
			{
				name = deviceInfo.Properties.GetString(MidiDeviceInfo.PropertyName);
			}

			var deviceInformation = new DeviceInformation(
				_portType == MidiPortType.Input ? DeviceClassGuids.MidiIn : DeviceClassGuids.MidiOut,
				GetMidiDeviceId(deviceInfo, portInfo))
			{
				Name = name
			};
			return deviceInformation;
		}

		private DeviceInformationUpdate CreateDeviceInformationUpdate(MidiDeviceInfo deviceInfo, MidiDeviceInfo.PortInfo portInfo)
		{
			var deviceInformation = new DeviceInformationUpdate(
				_portType == MidiPortType.Input ? DeviceClassGuids.MidiIn : DeviceClassGuids.MidiOut,
				GetMidiDeviceId(deviceInfo, portInfo));
			return deviceInformation;
		}

		private static string GetMidiDeviceId(MidiDeviceInfo deviceInfo, MidiDeviceInfo.PortInfo portInfo)
		{
			return $"{deviceInfo.Id.ToString()}_{portInfo.PortNumber}";
		}

		private IEnumerable<DeviceInformation> GetMidiDevices(MidiManager midiManager)
		{
			return midiManager
				.GetDevices()
				.Where(d => d.GetPorts().Any(p => p.Type == _portType))
				.SelectMany(d => d.GetPorts().Select(p => (device: d, port: p)))
				.Select(pair => CreateDeviceInformation(pair.device, pair.port));
		}

		private class DeviceCallback : MidiManager.DeviceCallback
		{
			private readonly MidiDeviceClassProviderBase _provider;

			public DeviceCallback(MidiDeviceClassProviderBase provider) =>
				_provider = provider;

			public override void OnDeviceAdded(MidiDeviceInfo device)
			{
				if (_provider.DeviceMatchesType(device))
				{
					_provider.OnDeviceAdded(device);
				}
			}

			public override void OnDeviceRemoved(MidiDeviceInfo device)
			{
				if (_provider.DeviceMatchesType(device))
				{
					_provider.OnDeviceRemoved(device);
				}
			}

			public override void OnDeviceStatusChanged(MidiDeviceStatus status)
			{
				if (_provider.DeviceMatchesType(status.DeviceInfo))
				{
					_provider.OnDeviceUpdated(status);
				}
			}
		}
	}
}
#endif
