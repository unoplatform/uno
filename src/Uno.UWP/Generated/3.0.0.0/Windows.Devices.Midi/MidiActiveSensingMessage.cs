#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Midi
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class MidiActiveSensingMessage : global::Windows.Devices.Midi.IMidiMessage
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Storage.Streams.IBuffer RawData
		{
			get
			{
				throw new global::System.NotImplementedException("The member IBuffer MidiActiveSensingMessage.RawData is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.TimeSpan Timestamp
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimeSpan MidiActiveSensingMessage.Timestamp is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Midi.MidiMessageType Type
		{
			get
			{
				throw new global::System.NotImplementedException("The member MidiMessageType MidiActiveSensingMessage.Type is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public MidiActiveSensingMessage() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Midi.MidiActiveSensingMessage", "MidiActiveSensingMessage.MidiActiveSensingMessage()");
		}
		#endif
		// Forced skipping of method Windows.Devices.Midi.MidiActiveSensingMessage.MidiActiveSensingMessage()
		// Forced skipping of method Windows.Devices.Midi.MidiActiveSensingMessage.Timestamp.get
		// Forced skipping of method Windows.Devices.Midi.MidiActiveSensingMessage.RawData.get
		// Forced skipping of method Windows.Devices.Midi.MidiActiveSensingMessage.Type.get
		// Processing: Windows.Devices.Midi.IMidiMessage
	}
}
