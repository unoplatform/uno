#if !NET461 && !__SKIA__ && !__NETSTD_REFERENCE__
using System;
using Uno.Devices.Enumeration.Internal;
using Windows.Foundation;
using Windows.Storage.Streams;

namespace Windows.Devices.Midi
{
	/// <summary>
	/// Represents a port used to send MIDI messages to a MIDI device.
	/// </summary>
	public sealed partial class MidiOutPort : IDisposable
	{
		private readonly static string MidiOutAqsFilter =
			"System.Devices.InterfaceClassGuid:=\"{" + DeviceClassGuids.MidiOut + "}\" AND System.Devices.InterfaceEnabled:=System.StructuredQueryType.Boolean#True";

		/// <summary>
		/// Gets the id of the device that was used to initialize the MidiOutPort.
		/// </summary>
		public string DeviceId { get; private set; }

		/// <summary>
		/// Gets a query string that can be used to enumerate all MidiOutPort objects on the system.
		/// </summary>
		/// <returns>The query string used to enumerate the MidiOutPort objects on the system.</returns>
		public static string GetDeviceSelector() => MidiOutAqsFilter;

		/// <summary>
		/// Creates a MidiOutPort object for the specified device.
		/// </summary>
		/// <param name="deviceId">The device ID, which can be obtained by enumerating the devices on the system</param>
		/// <returns>The asynchronous operation. Upon completion, IAsyncOperation.GetResults returns a MidiOutPort object.</returns>
		public static IAsyncOperation<IMidiOutPort> FromIdAsync(string deviceId)
		{
			var deviceIdentifier = ValidateAndParseDeviceId(deviceId);
			return FromIdInternalAsync(deviceIdentifier).AsAsyncOperation();
		}

		/// <summary>
		/// Send the data in the specified MIDI message to the device associated with this MidiOutPort.
		/// </summary>
		/// <param name="midiMessage">The MIDI message to send to the device.</param>
		public void SendMessage(IMidiMessage midiMessage)
		{
			if (midiMessage is null)
			{
				throw new ArgumentNullException(nameof(midiMessage));
			}

			SendBufferInternal(midiMessage.RawData, midiMessage.Timestamp);
		}

		/// <summary>
		///	Sends the contents of the buffer through the MIDI out port.
		/// </summary>
		/// <param name="midiData">The data to send to the device.</param>
		public void SendBuffer(IBuffer midiData)
		{
			if (midiData is null)
			{
				throw new ArgumentNullException(nameof(midiData));
			}

			SendBufferInternal(midiData, TimeSpan.Zero);
		}

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

			if (deviceIdentifier.DeviceClass != DeviceClassGuids.MidiOut)
			{
				throw new InvalidOperationException("Given device is not a MIDI out device");
			}

			return deviceIdentifier;
		}
	}
}
#endif
