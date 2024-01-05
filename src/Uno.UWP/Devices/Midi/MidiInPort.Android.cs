using System;
using System.Threading.Tasks;
using Android.Content;
using Android.Media.Midi;
using Java.Interop;
using Uno.Devices.Enumeration.Internal;
using Uno.Devices.Enumeration.Internal.Providers.Midi;
using Uno.Devices.Midi.Internal;
using Uno.UI;

namespace Windows.Devices.Midi
{
	public partial class MidiInPort
	{
		private readonly MidiManager _midiManager;
		private readonly MidiDeviceInfo _deviceInfo;
		private readonly MidiDeviceInfo.PortInfo _portInfo;

		/// <summary>
		/// This is not a bug, Android uses "output" for input.
		/// </summary>
		private MidiOutputPort? _midiPort;
		private MidiDevice? _midiDevice;
		private MessageReceiver? _messageReceiver;

		private MidiInPort(
			string deviceId,
			MidiDeviceInfo deviceInfo,
			MidiDeviceInfo.PortInfo portInfo)
		{
			_midiManager = ContextHelper.Current.GetSystemService(Context.MidiService).JavaCast<MidiManager>()!;
			DeviceId = deviceId ?? throw new ArgumentNullException(nameof(deviceId));
			_deviceInfo = deviceInfo ?? throw new ArgumentNullException(nameof(deviceInfo));
			_portInfo = portInfo ?? throw new ArgumentNullException(nameof(portInfo));
		}

		partial void StartMessageReceived()
		{
			_messageReceiver = new MessageReceiver(this);
			_midiPort!.Connect(_messageReceiver);
		}

		partial void StopMessageReceived()
		{
			_midiPort!.Disconnect(_messageReceiver);
			_messageReceiver!.Dispose();
			_messageReceiver = null;
		}

		internal async Task InitializeAsync()
		{
			var completionSource = new TaskCompletionSource<MidiDevice>();
			using (var deviceOpenListener = new MidiDeviceOpenedListener(completionSource))
			{
				_midiManager.OpenDevice(_deviceInfo, deviceOpenListener, null);
				_midiDevice = await completionSource.Task;
				// This is not a bug, Android uses "output" for input.
				_midiPort = _midiDevice.OpenOutputPort(_portInfo.PortNumber);
			}
		}

		partial void DisposeNative()
		{
			if (_messageReceiver != null)
			{
				_midiPort?.Disconnect(_messageReceiver);
				_messageReceiver.Dispose();
				_messageReceiver = null;
			}
			_portInfo?.Dispose();
			_deviceInfo?.Dispose();
			_midiPort?.Dispose();
			_midiManager?.Dispose();
		}

		private static async Task<MidiInPort> FromIdInternalAsync(DeviceIdentifier identifier)
		{
			var provider = new MidiInDeviceClassProvider();
			var nativeDeviceInfo = provider.GetNativeDeviceInfo(identifier.Id);
			if (nativeDeviceInfo == (null, null))
			{
				throw new InvalidOperationException("Given MIDI out device does not exist");
			}

			var port = new MidiInPort(identifier.ToString(), nativeDeviceInfo.device, nativeDeviceInfo.port);
			await port.InitializeAsync();
			return port;
		}

		private class MessageReceiver : MidiReceiver
		{
			private readonly MidiInPort _midiInPort;

			internal MessageReceiver(MidiInPort midiInPort)
			{
				_midiInPort = midiInPort;
			}

			public override void OnSend(byte[]? msg, int offset, int count, long timestamp)
			{
				_midiInPort.OnMessageReceived(msg, offset, count, TimeSpan.FromMilliseconds(timestamp));
			}
		}
	}
}
