#pragma warning disable CS0618 // obsolete members

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Android.Content;
using Android.Media.Midi;
using Android.Runtime;
using Android.Service.VR;

using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI;
using Windows.Devices.Enumeration;

namespace Uno.Devices.Enumeration.Internal.Providers.Midi
{
	internal abstract class MidiDeviceClassProviderBase : IDeviceClassProvider
	{
		private readonly MidiPortType _portType;
		private MidiManager? _watchMidiManager;
		private DeviceCallback? _deviceCallback;

		public MidiDeviceClassProviderBase(MidiPortType portType) => _portType = portType;

		public event EventHandler<DeviceInformation>? WatchAdded;
		public event EventHandler<DeviceInformation?>? WatchEnumerationCompleted;
		public event EventHandler<DeviceInformationUpdate>? WatchRemoved;
		public event EventHandler<DeviceInformationUpdate>? WatchUpdated;
		public event EventHandler<object?>? WatchStopped;

		public bool CanWatch => true;

		public Task<DeviceInformation[]> FindAllAsync()
		{
			return Task.FromResult(GetMidiDevices().ToArray());
		}

		public void WatchStart()
		{
			if (_deviceCallback != null)
			{
				return;
			}

			var devices = GetMidiDevices().ToArray();
			foreach (var device in devices)
			{
				WatchAdded?.Invoke(this, device);
			}
			OnEnumerationCompleted(devices.LastOrDefault());

#pragma warning disable CA1422 // Validate platform compatibility
			MidiManager.RegisterDeviceCallback(_deviceCallback = new DeviceCallback(this), null);
#pragma warning restore CA1422 // Validate platform compatibility
		}

		public void WatchStop()
		{
			if (_deviceCallback == null)
			{
				return;
			}

			MidiManager?.UnregisterDeviceCallback(_deviceCallback);
			_deviceCallback?.Dispose();
			_deviceCallback = null;
			_watchMidiManager = null;
			WatchStopped?.Invoke(this, null);
		}

		private MidiManager MidiManager
			=> _watchMidiManager ??= ContextHelper.Current.GetSystemService(Context.MidiService)!.JavaCast<MidiManager>();

		internal (MidiDeviceInfo device, MidiDeviceInfo.PortInfo port) GetNativeDeviceInfo(string midiDeviceId)
		{
			var parsed = ParseMidiDeviceId(midiDeviceId);
			using (var midiManager = ContextHelper.Current.GetSystemService(Context.MidiService).JavaCast<MidiManager>())
			{
#pragma warning disable CA1422 // Validate platform compatibility
				return midiManager!
					.GetDevices()!
					.Where(d => d.Id == parsed.id)
					.SelectMany(d =>
						d.GetPorts()!
							.Where(p =>
								p.Type == _portType &&
								p.PortNumber == parsed.portNumber)
							.Select(p => (device: d, port: p)))
					.FirstOrDefault();
#pragma warning restore CA1422 // Validate platform compatibility
			}
		}

		private bool DeviceMatchesType(MidiDeviceInfo info) =>
			_portType == MidiPortType.Input ?
				info.InputPortCount > 0 : info.OutputPortCount > 0;

		private static (int id, int portNumber) ParseMidiDeviceId(string id)
		{
			var parts = id.Split("_");
			var intId = int.Parse(parts[0], CultureInfo.InvariantCulture);
			var portNumber = int.Parse(parts[1], CultureInfo.InvariantCulture);
			return (intId, portNumber);
		}

		private static string GetMidiDeviceId(MidiDeviceInfo deviceInfo, MidiDeviceInfo.PortInfo portInfo) =>
			$"{deviceInfo.Id}_{portInfo.PortNumber}";

		private IEnumerable<DeviceInformation> GetMidiDevices()
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().LogDebug("Retrieving MIDI devices");
			}

#pragma warning disable CA1422 // Validate platform compatibility
			return MidiManager
				.GetDevices()!
				.SelectMany(d => FilterMatchingPorts(d.GetPorts()!).Select(p => (device: d, port: p)))
				.Select(pair => CreateDeviceInformation(pair.device, pair.port));
#pragma warning restore CA1422 // Validate platform compatibility
		}

		private IEnumerable<MidiDeviceInfo.PortInfo> FilterMatchingPorts(IEnumerable<MidiDeviceInfo.PortInfo> port)
		{
			return port.Where(p => p.Type == _portType);
		}

		private void OnEnumerationCompleted(DeviceInformation? lastDeviceInformation) =>
			WatchEnumerationCompleted?.Invoke(this, lastDeviceInformation);

		private void OnDeviceAdded(MidiDeviceInfo deviceInfo)
		{
			foreach (var port in deviceInfo.GetPorts()!.Where(p => p.Type == _portType))
			{
				WatchAdded?.Invoke(this, CreateDeviceInformation(deviceInfo, port));
			}
		}

		private void OnDeviceRemoved(MidiDeviceInfo deviceInfo)
		{
			foreach (var port in deviceInfo.GetPorts()!.Where(p => p.Type == _portType))
			{
				WatchRemoved?.Invoke(this, CreateDeviceInformationUpdate(deviceInfo, port));
			}
		}

		private void OnDeviceUpdated(MidiDeviceStatus status)
		{
			foreach (var port in status.DeviceInfo!.GetPorts()!.Where(p => p.Type == _portType))
			{
				WatchUpdated?.Invoke(this, CreateDeviceInformationUpdate(status.DeviceInfo, port));
			}
		}

		private DeviceInformation CreateDeviceInformation(MidiDeviceInfo deviceInfo, MidiDeviceInfo.PortInfo portInfo)
		{
			var name = "";
			if (deviceInfo.Properties!.ContainsKey(MidiDeviceInfo.PropertyName))
			{
				name = deviceInfo.Properties.GetString(MidiDeviceInfo.PropertyName);
			}

			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().LogDebug($"Creating device info for {name}");
			}

			var deviceIdentifier = new DeviceIdentifier(
				GetMidiDeviceId(deviceInfo, portInfo),
				_portType == MidiPortType.Input ? DeviceClassGuids.MidiIn : DeviceClassGuids.MidiOut);

			var properties = new Dictionary<string, object>();
			foreach (var key in deviceInfo.Properties.KeySet()!)
			{
				var value = deviceInfo.Properties.Get(key);
				properties.Add(key, value!);
			}

			var deviceInformation = new DeviceInformation(deviceIdentifier, properties)
			{
				Name = name,
			};

			return deviceInformation;
		}

		private DeviceInformationUpdate CreateDeviceInformationUpdate(MidiDeviceInfo deviceInfo, MidiDeviceInfo.PortInfo portInfo)
		{
			var deviceIdentifier = new DeviceIdentifier(
				GetMidiDeviceId(deviceInfo, portInfo),
				_portType == MidiPortType.Input ? DeviceClassGuids.MidiIn : DeviceClassGuids.MidiOut);
			var deviceInformation = new DeviceInformationUpdate(deviceIdentifier);
			return deviceInformation;
		}

		private class DeviceCallback : MidiManager.DeviceCallback
		{
			private readonly MidiDeviceClassProviderBase _provider;

			public DeviceCallback(MidiDeviceClassProviderBase provider) =>
				_provider = provider;

			public override void OnDeviceAdded(MidiDeviceInfo? device)
			{
				if (_provider.DeviceMatchesType(device!))
				{
					_provider.OnDeviceAdded(device!);
				}
			}

			public override void OnDeviceRemoved(MidiDeviceInfo? device)
			{
				if (_provider.DeviceMatchesType(device!))
				{
					_provider.OnDeviceRemoved(device!);
				}
			}

			public override void OnDeviceStatusChanged(MidiDeviceStatus? status)
			{
				if (_provider.DeviceMatchesType(status!.DeviceInfo!))
				{
					_provider.OnDeviceUpdated(status);
				}
			}
		}
	}
}
