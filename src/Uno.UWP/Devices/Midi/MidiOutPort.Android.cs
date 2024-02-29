using System;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Android.Content;
using Android.Media.Midi;
using Android.Runtime;

using Uno.Devices.Enumeration.Internal;
using Uno.Devices.Enumeration.Internal.Providers.Midi;
using Uno.Devices.Midi.Internal;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Storage.Streams;

namespace Windows.Devices.Midi
{
	public partial class MidiOutPort : IDisposable
	{
		private readonly MidiManager _midiManager;
		private readonly MidiDeviceInfo _deviceInfo;
		private readonly MidiDeviceInfo.PortInfo _portInfo;

		/// <summary>
		/// This is not a bug, Android uses "input" for output.
		/// </summary>
		private MidiInputPort? _midiPort;
		private MidiDevice? _midiDevice;

		private MidiOutPort(
			string deviceId,
			MidiDeviceInfo deviceInfo,
			MidiDeviceInfo.PortInfo portInfo)
		{
			_midiManager = ContextHelper.Current.GetSystemService(Context.MidiService).JavaCast<MidiManager>()!;
			DeviceId = deviceId ?? throw new ArgumentNullException(nameof(deviceId));
			_deviceInfo = deviceInfo ?? throw new ArgumentNullException(nameof(deviceInfo));
			_portInfo = portInfo ?? throw new ArgumentNullException(nameof(portInfo));
		}

		internal async Task OpenAsync()
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().LogDebug($"Opening the MIDI out port, port number {_portInfo.PortNumber}");
			}
			var completionSource = new TaskCompletionSource<MidiDevice>();
			using (var deviceOpenListener = new MidiDeviceOpenedListener(completionSource))
			{
				_midiManager.OpenDevice(_deviceInfo, deviceOpenListener, null);
				_midiDevice = await completionSource.Task;
				// This is not a bug, Android uses "input" for output.
				_midiPort = _midiDevice.OpenInputPort(_portInfo.PortNumber)!;
			}
		}

		public void SendBufferInternal(IBuffer midiBuffer, TimeSpan timestamp)
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().LogDebug($"Sending MIDI buffer to port {_portInfo.PortNumber}");
			}
			if (_midiPort == null)
			{
				throw new InvalidOperationException("Output port is not initialized.");
			}
			var data = midiBuffer.ToArray();
			_midiPort.Send(data, 0, data.Length, timestamp.Ticks);
		}

		public void Dispose()
		{
			_midiDevice?.Close();
			_midiDevice?.Dispose();
			_portInfo?.Dispose();
			_deviceInfo?.Dispose();
			_midiPort?.Dispose();
			_midiManager?.Dispose();
		}

		private static async Task<IMidiOutPort> FromIdInternalAsync(DeviceIdentifier identifier)
		{
			var provider = new MidiOutDeviceClassProvider();
			var nativeDeviceInfo = provider.GetNativeDeviceInfo(identifier.Id);
			if (nativeDeviceInfo == (null, null))
			{
				throw new InvalidOperationException("Given MIDI out device does not exist");
			}

			var port = new MidiOutPort(identifier.ToString(), nativeDeviceInfo.device, nativeDeviceInfo.port);
			await port.OpenAsync();
			return port;
		}
	}
}
