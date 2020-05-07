using System;
using System.Collections.Generic;
using Uno.Devices.Enumeration.Internal;
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

		/// <summary>
		/// Gets the id of the device that was used to initialize the MidiInPort.
		/// </summary>
		public string DeviceId { get; private set; }

		/// <summary>
		/// Gets the id of the device that was used to initialize the MidiInPort.
		/// </summary>
		public event TypedEventHandler<MidiInPort, MidiMessageReceivedEventArgs> MessageReceived;

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

		private void OnMessageReceived(byte[] message, TimeSpan timestamp)
		{
			if (message.Length == 0)
			{
				//ignore empty message
				return;
			}

			//read message type
			
		}
	}
}
