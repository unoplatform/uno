#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Midi
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class MidiNoteOffMessage : global::Windows.Devices.Midi.IMidiMessage
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Storage.Streams.IBuffer RawData
		{
			get
			{
				throw new global::System.NotImplementedException("The member IBuffer MidiNoteOffMessage.RawData is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.TimeSpan Timestamp
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimeSpan MidiNoteOffMessage.Timestamp is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Midi.MidiMessageType Type
		{
			get
			{
				throw new global::System.NotImplementedException("The member MidiMessageType MidiNoteOffMessage.Type is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  byte Channel
		{
			get
			{
				throw new global::System.NotImplementedException("The member byte MidiNoteOffMessage.Channel is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  byte Note
		{
			get
			{
				throw new global::System.NotImplementedException("The member byte MidiNoteOffMessage.Note is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  byte Velocity
		{
			get
			{
				throw new global::System.NotImplementedException("The member byte MidiNoteOffMessage.Velocity is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public MidiNoteOffMessage( byte channel,  byte note,  byte velocity) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Midi.MidiNoteOffMessage", "MidiNoteOffMessage.MidiNoteOffMessage(byte channel, byte note, byte velocity)");
		}
		#endif
		// Forced skipping of method Windows.Devices.Midi.MidiNoteOffMessage.MidiNoteOffMessage(byte, byte, byte)
		// Forced skipping of method Windows.Devices.Midi.MidiNoteOffMessage.Channel.get
		// Forced skipping of method Windows.Devices.Midi.MidiNoteOffMessage.Note.get
		// Forced skipping of method Windows.Devices.Midi.MidiNoteOffMessage.Velocity.get
		// Forced skipping of method Windows.Devices.Midi.MidiNoteOffMessage.Timestamp.get
		// Forced skipping of method Windows.Devices.Midi.MidiNoteOffMessage.RawData.get
		// Forced skipping of method Windows.Devices.Midi.MidiNoteOffMessage.Type.get
		// Processing: Windows.Devices.Midi.IMidiMessage
	}
}
