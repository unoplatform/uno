#if __ANDROID__
using System;
using System.Threading.Tasks;
using Android.Media.Midi;
using Uno.Devices.Enumeration.Internal;
using Uno.Devices.Enumeration.Internal.Providers.Midi;
using Windows.Devices.Enumeration;
using Windows.Foundation;

namespace Windows.Devices.Midi
{
	public partial class MidiOutPort : IDisposable
	{
		private MidiDeviceInfo _deviceInfo = null;
		private MidiDeviceInfo.PortInfo _portInfo = null;

		private MidiOutPort(
			MidiDeviceInfo deviceInfo,
			MidiDeviceInfo.PortInfo portInfo)
		{
			_deviceInfo = deviceInfo;
			_portInfo = portInfo;
		}

		public string DeviceId { get; private set; }

		public static IAsyncOperation<IMidiOutPort> FromIdAsync(string deviceId) =>
			FromIdInternalAsync(deviceId).AsAsyncOperation();


		public void Dispose()
		{
			_portInfo?.Dispose();
			_deviceInfo?.Dispose();
		}

		private static async Task<IMidiOutPort> FromIdInternalAsync(string deviceId)
		{
			var parsedIdentifier = DeviceInformation.ParseDeviceId(deviceId);
			if (!parsedIdentifier.deviceClassGuid.Equals(DeviceClassGuids.MidiOut, StringComparison.InvariantCultureIgnoreCase))
			{
				throw new InvalidOperationException("Given device is not a MIDI out device");
			}

			var provider = new MidiOutDeviceClassProvider();
			var nativeDeviceInfo = provider.GetNativeDeviceInfo(parsedIdentifier.id);
			if (nativeDeviceInfo == (null, null))
			{
				throw new InvalidOperationException("Given MIDI out device does not exist");
			}

			return new MidiOutPort(nativeDeviceInfo.device, nativeDeviceInfo.port);
		}
	}
}
#endif
