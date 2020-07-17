#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Midi
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class MidiPitchBendChangeMessage : global::Windows.Devices.Midi.IMidiMessage
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Storage.Streams.IBuffer RawData
		{
			get
			{
				throw new global::System.NotImplementedException("The member IBuffer MidiPitchBendChangeMessage.RawData is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.TimeSpan Timestamp
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimeSpan MidiPitchBendChangeMessage.Timestamp is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Midi.MidiMessageType Type
		{
			get
			{
				throw new global::System.NotImplementedException("The member MidiMessageType MidiPitchBendChangeMessage.Type is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  ushort Bend
		{
			get
			{
				throw new global::System.NotImplementedException("The member ushort MidiPitchBendChangeMessage.Bend is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  byte Channel
		{
			get
			{
				throw new global::System.NotImplementedException("The member byte MidiPitchBendChangeMessage.Channel is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public MidiPitchBendChangeMessage( byte channel,  ushort bend) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Midi.MidiPitchBendChangeMessage", "MidiPitchBendChangeMessage.MidiPitchBendChangeMessage(byte channel, ushort bend)");
		}
		#endif
		// Forced skipping of method Windows.Devices.Midi.MidiPitchBendChangeMessage.MidiPitchBendChangeMessage(byte, ushort)
		// Forced skipping of method Windows.Devices.Midi.MidiPitchBendChangeMessage.Channel.get
		// Forced skipping of method Windows.Devices.Midi.MidiPitchBendChangeMessage.Bend.get
		// Forced skipping of method Windows.Devices.Midi.MidiPitchBendChangeMessage.Timestamp.get
		// Forced skipping of method Windows.Devices.Midi.MidiPitchBendChangeMessage.RawData.get
		// Forced skipping of method Windows.Devices.Midi.MidiPitchBendChangeMessage.Type.get
		// Processing: Windows.Devices.Midi.IMidiMessage
	}
}
