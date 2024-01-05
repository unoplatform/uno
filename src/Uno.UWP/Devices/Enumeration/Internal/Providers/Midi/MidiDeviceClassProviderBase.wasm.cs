using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;

using Uno.Devices.Midi.Internal;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Windows.Devices.Enumeration;



namespace Uno.Devices.Enumeration.Internal.Providers.Midi
{
	internal abstract partial class MidiDeviceClassProviderBase : IDeviceClassProvider
	{
		private readonly bool _isInput;

		public MidiDeviceClassProviderBase(bool isInput) => _isInput = isInput;

		public event EventHandler<DeviceInformation>? WatchAdded;

		public event EventHandler<DeviceInformation?>? WatchEnumerationCompleted;

		public event EventHandler<DeviceInformationUpdate>? WatchRemoved;

		public event EventHandler<object?>? WatchStopped;

#pragma warning disable CS0067 // Device update watching is not supported on WASM
		public event EventHandler<DeviceInformationUpdate>? WatchUpdated;
#pragma warning restore CS0067

		public bool CanWatch => true;

		public async Task<DeviceInformation[]> FindAllAsync()
		{
			if (!await WasmMidiAccess.RequestAsync())
			{
				throw new AccessViolationException("Can't access Web MIDI API");
			}

			return GetMidiDevices().ToArray();
		}

		public void WatchStart()
		{
			Task.Run(async () =>
			{
				try
				{
					if (!await WasmMidiAccess.RequestAsync())
					{
						throw new AccessViolationException("Can't access Web MIDI API");
					}

					var devices = GetMidiDevices().ToArray();
					foreach (var device in devices)
					{
						WatchAdded?.Invoke(this, device);
					}

					OnEnumerationCompleted(devices.LastOrDefault());
					StartStateChanged();
				}
				catch (Exception ex)
				{
					if (this.Log().IsEnabled(LogLevel.Error))
					{
						this.Log().LogError("Exception occurred trying to start MIDI watch.", ex);
					}
					throw;
				}
			});
		}

		private void StartStateChanged() => MidiDeviceConnectionWatcher.AddObserver(this);

		private void StopStateChanged() => MidiDeviceConnectionWatcher.RemoveObserver(this);

		public void WatchStop()
		{
			StopStateChanged();
			WatchStopped?.Invoke(this, null);
		}

		internal void OnDeviceAdded(string id, string name)
		{
			var deviceInformation = CreateDeviceInformation(id, name);
			WatchAdded?.Invoke(this, deviceInformation);
		}

		internal void OnDeviceRemoved(string id)
		{
			var deviceInformationUpdate = CreateDeviceInformationUpdate(id);
			WatchRemoved?.Invoke(this, deviceInformationUpdate);
		}

		private void OnEnumerationCompleted(DeviceInformation? lastDeviceInformation)
		{
			WatchEnumerationCompleted?.Invoke(this, lastDeviceInformation);
		}

		private IEnumerable<DeviceInformation> GetMidiDevices()
		{
			var result = NativeMethods.FindDevices(_isInput);

			var devices = result.Split('&', StringSplitOptions.RemoveEmptyEntries);
			foreach (var device in devices)
			{
				var deviceMetadata = device.Split('#', StringSplitOptions.RemoveEmptyEntries);
				var id = Uri.UnescapeDataString(deviceMetadata[0]);
				var name = Uri.UnescapeDataString(deviceMetadata[1]);
				yield return CreateDeviceInformation(id, name);
			}
		}

		private DeviceInformation CreateDeviceInformation(string id, string name)
		{
			var identifier = new DeviceIdentifier(
					id,
					_isInput ? DeviceClassGuids.MidiIn : DeviceClassGuids.MidiOut);
			return new DeviceInformation(identifier)
			{
				Name = name
			};
		}

		private DeviceInformationUpdate CreateDeviceInformationUpdate(string id)
		{
			var deviceIdentifier = new DeviceIdentifier(
				id,
				_isInput ? DeviceClassGuids.MidiIn : DeviceClassGuids.MidiOut);
			return new DeviceInformationUpdate(deviceIdentifier);
		}

		internal static partial class NativeMethods
		{
			[JSImport($"globalThis.Uno.Devices.Enumeration.Internal.Providers.Midi.MidiDeviceClassProvider.findDevices")]
			internal static partial string FindDevices(bool isInput);
		}
	}
}
