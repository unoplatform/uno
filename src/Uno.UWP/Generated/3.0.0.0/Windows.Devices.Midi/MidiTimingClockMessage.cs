#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Midi
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class MidiTimingClockMessage : global::Windows.Devices.Midi.IMidiMessage
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Storage.Streams.IBuffer RawData
		{
			get
			{
				throw new global::System.NotImplementedException("The member IBuffer MidiTimingClockMessage.RawData is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.TimeSpan Timestamp
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimeSpan MidiTimingClockMessage.Timestamp is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Midi.MidiMessageType Type
		{
			get
			{
				throw new global::System.NotImplementedException("The member MidiMessageType MidiTimingClockMessage.Type is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public MidiTimingClockMessage() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Midi.MidiTimingClockMessage", "MidiTimingClockMessage.MidiTimingClockMessage()");
		}
		#endif
		// Forced skipping of method Windows.Devices.Midi.MidiTimingClockMessage.MidiTimingClockMessage()
		// Forced skipping of method Windows.Devices.Midi.MidiTimingClockMessage.Timestamp.get
		// Forced skipping of method Windows.Devices.Midi.MidiTimingClockMessage.RawData.get
		// Forced skipping of method Windows.Devices.Midi.MidiTimingClockMessage.Type.get
		// Processing: Windows.Devices.Midi.IMidiMessage
	}
}
