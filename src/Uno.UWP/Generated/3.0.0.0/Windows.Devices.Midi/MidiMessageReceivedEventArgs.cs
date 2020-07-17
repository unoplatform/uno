#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Midi
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class MidiMessageReceivedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Midi.IMidiMessage Message
		{
			get
			{
				throw new global::System.NotImplementedException("The member IMidiMessage MidiMessageReceivedEventArgs.Message is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.Midi.MidiMessageReceivedEventArgs.Message.get
	}
}
