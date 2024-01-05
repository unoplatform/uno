#if !IS_UNIT_TESTS && !__SKIA__ && !__NETSTD_REFERENCE__
using System;

using Uno.Devices.Enumeration.Internal;
using Uno.Devices.Midi.Internal;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Windows.Foundation;

namespace Windows.Devices.Midi
{
	/// <summary>
	/// Represents a port used to receive MIDI messages from a MIDI device.
	/// </summary>
	public sealed partial class MidiInPort : IDisposable
	{
		private readonly static string MidiInAqsFilter =
			"System.Devices.InterfaceClassGuid:=\"{" + DeviceClassGuids.MidiIn + "}\" AND System.Devices.InterfaceEnabled:=System.StructuredQueryType.Boolean#True";

		private readonly object _syncLock = new object();
		private readonly MidiMessageParser _parser = new MidiMessageParser();

		private TypedEventHandler<MidiInPort, MidiMessageReceivedEventArgs>? _messageReceived;

		/// <summary>
		/// Gets the id of the device that was used to initialize the MidiInPort.
		/// </summary>
		public event TypedEventHandler<MidiInPort, MidiMessageReceivedEventArgs> MessageReceived
		{
			add
			{
				lock (_syncLock)
				{
					var firstSubscriber = _messageReceived == null;
					_messageReceived += value;
					if (firstSubscriber)
					{
						StartMessageReceived();
					}
				}
			}
			remove
			{
				lock (_syncLock)
				{
					_messageReceived -= value;
					if (_messageReceived == null)
					{
						StopMessageReceived();
					}
				}
			}
		}

		/// <summary>
		/// Gets the id of the device that was used to initialize the MidiInPort.
		/// </summary>
		public string DeviceId { get; private set; }

		/// <summary>
		/// Creates a MidiInPort object for the specified device.
		/// </summary>
		/// <param name="deviceId">The device ID, which can be obtained by enumerating the devices on the system.</param>
		/// <returns>The asynchronous operation. Upon completion, IAsyncOperation.GetResults returns a MidiInPort object.</returns>
		public static IAsyncOperation<MidiInPort> FromIdAsync(string deviceId)
		{
			var deviceIdentifier = ValidateAndParseDeviceId(deviceId);
			return FromIdInternalAsync(deviceIdentifier).AsAsyncOperation();
		}

		/// <summary>
		/// Gets a query string that can be used to enumerate all MidiInPort objects on the system.
		/// </summary>
		/// <returns>The query string used to enumerate the MidiInPort objects on the system.</returns>
		public static string GetDeviceSelector() => MidiInAqsFilter;

		public void Dispose()
		{
			_messageReceived = null;
			DisposeNative();
		}

		partial void StartMessageReceived();

		partial void StopMessageReceived();

		partial void DisposeNative();

		private static DeviceIdentifier ValidateAndParseDeviceId(string deviceId)
		{
			if (deviceId is null)
			{
				throw new ArgumentNullException(nameof(deviceId));
			}

			if (!DeviceIdentifier.TryParse(deviceId, out var deviceIdentifier))
			{
				throw new ArgumentException("Device identifier is not valid", nameof(deviceId));
			}

			if (deviceIdentifier.DeviceClass != DeviceClassGuids.MidiIn)
			{
				throw new InvalidOperationException("Given device is not a MIDI in device");
			}

			return deviceIdentifier;
		}

		private void OnMessageReceived(byte[] message, int startingOffset, int length, TimeSpan timestamp)
		{
			if (message.Length == 0)
			{
				// ignore empty message
				return;
			}

			try
			{
				// parse message
				var parsedMessages = _parser.Parse(message, startingOffset, length, timestamp);
				foreach (var parsedMessage in parsedMessages)
				{
					var eventArgs = new MidiMessageReceivedEventArgs(parsedMessage);
					_messageReceived?.Invoke(this, eventArgs);
				}
			}
			catch (Exception ex)
			{
				this.Log().LogError("MIDI Message could not be parsed", ex);
			}
		}
	}
}
#endif
