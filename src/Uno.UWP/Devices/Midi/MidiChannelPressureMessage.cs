using System;
using Uno.Devices.Midi.Internal;
using Windows.Storage.Streams;

namespace Windows.Devices.Midi
{
	/// <summary>
	/// Represents a MIDI message that specifies the channel pressure.
	/// </summary>
	public partial class MidiChannelPressureMessage : IMidiMessage
	{
		/// <summary>
		/// Creates a new MidiChannelPressureMessage object.
		/// </summary>
		/// <param name="channel">The channel from 0-15 that this message applies to.</param>
		/// <param name="pressure">The pressure from 0-127.</param>
		public MidiChannelPressureMessage(byte channel, byte pressure)
		{
			MidiMessageValidators.VerifyRange(channel, 15, nameof(channel));
			MidiMessageValidators.VerifyRange(pressure, 127, nameof(pressure));

			Channel = channel;
			Pressure = pressure;
			RawData = new InMemoryBuffer(new byte[]
			{
				(byte)((byte)Type | Channel),
				Pressure
			});
		}

		/// <summary>
		/// Gets the type of this MIDI message.
		/// </summary>
		public MidiMessageType Type => MidiMessageType.ChannelPressure;

		/// <summary>
		/// Gets the channel from 0-15 that this message applies to.
		/// </summary>
		public byte Channel { get; }

		/// <summary>
		/// Gets the pressure from 0-127.
		/// </summary>
		public byte Pressure { get; }

		/// <summary>
		/// Gets the array of bytes associated with the MIDI message, including status byte.
		/// </summary>
		public IBuffer RawData { get; }

		/// <summary>
		/// Gets the duration from when the MidiInPort was created to the time the message was received.
		/// For messages being sent to a MidiOutPort, this value has no meaning.
		/// </summary>
		public TimeSpan Timestamp { get; } = TimeSpan.Zero;
	}
}
