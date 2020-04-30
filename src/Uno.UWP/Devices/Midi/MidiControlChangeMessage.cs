using System;
using Uno.Devices.Midi.Internal;
using Windows.Storage.Streams;

namespace Windows.Devices.Midi
{
	/// <summary>
	/// Represents a MIDI message that specifies a control change.
	/// </summary>
	public partial class MidiControlChangeMessage : IMidiMessage
	{
		/// <summary>
		/// Creates a new MidiControlChangeMessage object.
		/// </summary>
		/// <param name="channel">The channel from 0-15 that this message applies to.</param>
		/// <param name="controller">The controller from 0-127 to receive this message.</param>
		/// <param name="controlValue">The value from 0-127 to apply to the controller.</param>
		public MidiControlChangeMessage(byte channel, byte controller, byte controlValue)
		{
			MidiMessageValidators.VerifyRange(channel, 15, nameof(channel));
			MidiMessageValidators.VerifyRange(controller, 127, nameof(controller));
			MidiMessageValidators.VerifyRange(controlValue, 127, nameof(controlValue));

			Channel = channel;
			Controller = controller;
			ControlValue = controlValue;
			RawData = new InMemoryBuffer(new byte[]
			{
				(byte)((byte)Type | Channel),
				Controller,
				ControlValue
			});
		}

		/// <summary>
		/// Gets the type of this MIDI message.
		/// </summary>
		public MidiMessageType Type => MidiMessageType.ControlChange;

		/// <summary>
		/// Gets the channel from 0-15 that this message applies to.
		/// </summary>
		public byte Channel { get; }

		/// <summary>
		/// Gets the value from 0-127 to apply to the controller.
		/// </summary>
		public byte ControlValue { get; }

		/// <summary>
		/// Gets controller from 0-127 to receive this message.
		/// </summary>
		public byte Controller { get; }

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
