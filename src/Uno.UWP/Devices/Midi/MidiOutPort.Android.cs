#if __ANDROID__
using System;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Android.Content;
using Android.Media.Midi;
using Android.Runtime;
using Uno.Devices.Enumeration.Internal;
using Uno.Devices.Enumeration.Internal.Providers.Midi;
using Uno.Devices.Midi.Internal;
using Uno.UI;
using Windows.Devices.Enumeration;
using Windows.Foundation;

namespace Windows.Devices.Midi
{
	public partial class MidiOutPort : IDisposable
	{
		private readonly MidiManager _midiManager;
		private readonly MidiDeviceInfo _deviceInfo = null;
		private readonly MidiDeviceInfo.PortInfo _portInfo = null;		

		/// <summary>
		/// This is not a bug, Android uses "input" for output.
		/// </summary>
		private MidiInputPort _midiPort = null;
		private MidiDevice _midiDevice = null;

		private MidiOutPort(
			MidiDeviceInfo deviceInfo,
			MidiDeviceInfo.PortInfo portInfo)
		{
			_midiManager = ContextHelper.Current.GetSystemService(Context.MidiService).JavaCast<MidiManager>();
			_deviceInfo = deviceInfo;
			_portInfo = portInfo;
		}

		internal async Task OpenAsync()
		{
			var completionSource = new TaskCompletionSource<MidiDevice>();
			using (var deviceOpenListener = new MidiDeviceOpenedListener(completionSource))
			{
				_midiManager.OpenDevice(_deviceInfo, deviceOpenListener, null);
				_midiDevice = await completionSource.Task;
				// This is not a bug, Android uses "input" for output.
				_midiPort = _midiDevice.OpenInputPort(_portInfo.PortNumber);
			}
		}

		public string DeviceId { get; private set; }

		public static IAsyncOperation<IMidiOutPort> FromIdAsync(string deviceId) =>
			FromIdInternalAsync(deviceId).AsAsyncOperation();

		public void SendMessage(IMidiMessage midiMessage)
		{
			if (_midiPort == null)
			{
				throw new InvalidOperationException("Output port is not initialized.");
			}
			var data = midiMessage.RawData.ToArray();
			_midiPort.Send(data, 0, data.Length);
		}

		public void Dispose()
		{
			_portInfo?.Dispose();
			_deviceInfo?.Dispose();
			_midiPort?.Dispose();
			_midiManager?.Dispose();
		}

		private static async Task<IMidiOutPort> FromIdInternalAsync(string deviceId)
		{
			var deviceIdentifier = ValidateAndParseDeviceId(deviceId);

			var provider = new MidiOutDeviceClassProvider();
			var nativeDeviceInfo = provider.GetNativeDeviceInfo(deviceIdentifier.Id);
			if (nativeDeviceInfo == (null, null))
			{
				throw new InvalidOperationException("Given MIDI out device does not exist");
			}

			var port = new MidiOutPort(nativeDeviceInfo.device, nativeDeviceInfo.port);
			await port.OpenAsync();
			return port;
		}
	}
}
#endif
