#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Midi
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class MidiPolyphonicKeyPressureMessage : global::Windows.Devices.Midi.IMidiMessage
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Storage.Streams.IBuffer RawData
		{
			get
			{
				throw new global::System.NotImplementedException("The member IBuffer MidiPolyphonicKeyPressureMessage.RawData is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.TimeSpan Timestamp
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimeSpan MidiPolyphonicKeyPressureMessage.Timestamp is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Midi.MidiMessageType Type
		{
			get
			{
				throw new global::System.NotImplementedException("The member MidiMessageType MidiPolyphonicKeyPressureMessage.Type is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  byte Channel
		{
			get
			{
				throw new global::System.NotImplementedException("The member byte MidiPolyphonicKeyPressureMessage.Channel is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  byte Note
		{
			get
			{
				throw new global::System.NotImplementedException("The member byte MidiPolyphonicKeyPressureMessage.Note is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  byte Pressure
		{
			get
			{
				throw new global::System.NotImplementedException("The member byte MidiPolyphonicKeyPressureMessage.Pressure is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public MidiPolyphonicKeyPressureMessage( byte channel,  byte note,  byte pressure) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Midi.MidiPolyphonicKeyPressureMessage", "MidiPolyphonicKeyPressureMessage.MidiPolyphonicKeyPressureMessage(byte channel, byte note, byte pressure)");
		}
		#endif
		// Forced skipping of method Windows.Devices.Midi.MidiPolyphonicKeyPressureMessage.MidiPolyphonicKeyPressureMessage(byte, byte, byte)
		// Forced skipping of method Windows.Devices.Midi.MidiPolyphonicKeyPressureMessage.Channel.get
		// Forced skipping of method Windows.Devices.Midi.MidiPolyphonicKeyPressureMessage.Note.get
		// Forced skipping of method Windows.Devices.Midi.MidiPolyphonicKeyPressureMessage.Pressure.get
		// Forced skipping of method Windows.Devices.Midi.MidiPolyphonicKeyPressureMessage.Timestamp.get
		// Forced skipping of method Windows.Devices.Midi.MidiPolyphonicKeyPressureMessage.RawData.get
		// Forced skipping of method Windows.Devices.Midi.MidiPolyphonicKeyPressureMessage.Type.get
		// Processing: Windows.Devices.Midi.IMidiMessage
	}
}
