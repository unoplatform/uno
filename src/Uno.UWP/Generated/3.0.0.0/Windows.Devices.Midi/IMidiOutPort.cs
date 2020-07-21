#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Midi
{
	#if false || false || NET461 || false || __SKIA__ || __NETSTD_REFERENCE__ || false
	[global::Uno.NotImplemented("NET461", "__SKIA__", "__NETSTD_REFERENCE__")]	
	#endif
	public  partial interface IMidiOutPort : global::System.IDisposable
	{
		#if false || false || NET461 || false || __SKIA__ || __NETSTD_REFERENCE__ || false
		[global::Uno.NotImplemented("NET461", "__SKIA__", "__NETSTD_REFERENCE__")]
		string DeviceId
		{
			get;
		}
		#endif
		#if false || false || NET461 || false || __SKIA__ || __NETSTD_REFERENCE__ || false
		[global::Uno.NotImplemented("NET461", "__SKIA__", "__NETSTD_REFERENCE__")]
		void SendMessage( global::Windows.Devices.Midi.IMidiMessage midiMessage);
		#endif
		#if false || false || NET461 || false || __SKIA__ || __NETSTD_REFERENCE__ || false
		[global::Uno.NotImplemented("NET461", "__SKIA__", "__NETSTD_REFERENCE__")]
		void SendBuffer( global::Windows.Storage.Streams.IBuffer midiData);
		#endif
		// Forced skipping of method Windows.Devices.Midi.IMidiOutPort.DeviceId.get
	}
}
