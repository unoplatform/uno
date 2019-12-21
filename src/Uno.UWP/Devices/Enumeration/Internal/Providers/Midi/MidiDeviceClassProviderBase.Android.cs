#if __ANDROID__
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Android.Content;
using Android.Media.Midi;
using Android.Runtime;
using Uno.UI;
using Windows.Devices.Enumeration;

namespace Uno.Devices.Enumeration.Internal.Providers.Midi
{
	internal abstract class MidiDeviceClassProviderBase : IDeviceClassProvider
	{
		private readonly MidiPortType _portType;
		private MidiManager _midiManager;
		private DeviceCallback _deviceCallback;

		public MidiDeviceClassProviderBase(MidiPortType portType) => _portType = portType;

		public bool CanWatch => true;

		public event EventHandler<DeviceInformation> WatchAdded;
		public event EventHandler<DeviceInformation> WatchEnumerationCompleted;
		public event EventHandler<DeviceInformationUpdate> WatchRemoved;
		public event EventHandler<object> WatchStopped;
		public event EventHandler<DeviceInformationUpdate> WatchUpdated;
		
		public Task<DeviceInformation[]> FindAllAsync() =>
			Task.FromResult(
				GetMidiDevices()
					.Select(CreateDeviceInformation)
					.ToArray());

		public void WatchStart()
		{
			if (_deviceCallback == null)
			{
				return;
			}

			_midiManager = _midiManager ?? ContextHelper.Current.GetSystemService(Context.MidiService).JavaCast<MidiManager>();
			_midiManager.RegisterDeviceCallback(_deviceCallback = new DeviceCallback(this), null);
		}

		public void WatchStop()
		{
			if (_deviceCallback != null)
			{
				return;
			}

			_midiManager.UnregisterDeviceCallback(_deviceCallback);
			_deviceCallback?.Dispose();
			_deviceCallback = null;
			_midiManager.Dispose();
			WatchStopped?.Invoke(this, null);
		}

		private bool DeviceMatchesType(MidiDeviceInfo info) =>
			_portType == MidiPortType.Input ?
				info.InputPortCount > 0 : info.OutputPortCount > 0;

		private static DeviceInformation CreateDeviceInformation(MidiDeviceInfo info)
		{

			throw new NotImplementedException();
		}

		private static DeviceInformationUpdate CreateDeviceInformationUpdate(MidiDeviceInfo info, bool isRemoved) => throw new NotImplementedException();

		private IEnumerable<MidiDeviceInfo> GetMidiDevices()
		{
			return _midiManager
				.GetDevices()
				.Where(d => d.GetPorts()
					.Any(p => p.Type == _portType));
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
					_provider.WatchAdded?.Invoke(_provider, CreateDeviceInformation(device));
				}
			}

			public override void OnDeviceRemoved(MidiDeviceInfo device)
			{
				if (_provider.DeviceMatchesType(device))
				{
					_provider.WatchRemoved?.Invoke(_provider, CreateDeviceInformationUpdate(device, true));
				}
			}

			public override void OnDeviceStatusChanged(MidiDeviceStatus status)
			{
				if (_provider.DeviceMatchesType(status.DeviceInfo))
				{
					_provider.WatchUpdated?.Invoke(_provider, CreateDeviceInformationUpdate(status.DeviceInfo, false));
				}
			}
		}
	}
}
#endif
