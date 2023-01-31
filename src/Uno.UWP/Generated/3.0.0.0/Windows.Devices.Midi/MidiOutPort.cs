#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Midi
{
	#if false || false || false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class MidiOutPort : global::Windows.Devices.Midi.IMidiOutPort,global::System.IDisposable
	{
		#if false || false || NET461 || false || __SKIA__ || __NETSTD_REFERENCE__ || false
		[global::Uno.NotImplemented("NET461", "__SKIA__", "__NETSTD_REFERENCE__")]
		public  string DeviceId
		{
			get
			{
				throw new global::System.NotImplementedException("The member string MidiOutPort.DeviceId is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20MidiOutPort.DeviceId");
			}
		}
		#endif
		#if false || false || NET461 || false || __SKIA__ || __NETSTD_REFERENCE__ || false
		[global::Uno.NotImplemented("NET461", "__SKIA__", "__NETSTD_REFERENCE__")]
		public  void SendMessage( global::Windows.Devices.Midi.IMidiMessage midiMessage)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Midi.MidiOutPort", "void MidiOutPort.SendMessage(IMidiMessage midiMessage)");
		}
		#endif
		#if false || false || NET461 || false || __SKIA__ || __NETSTD_REFERENCE__ || false
		[global::Uno.NotImplemented("NET461", "__SKIA__", "__NETSTD_REFERENCE__")]
		public  void SendBuffer( global::Windows.Storage.Streams.IBuffer midiData)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Midi.MidiOutPort", "void MidiOutPort.SendBuffer(IBuffer midiData)");
		}
		#endif
		// Forced skipping of method Windows.Devices.Midi.MidiOutPort.DeviceId.get
		#if false || false || NET461 || false || __SKIA__ || __NETSTD_REFERENCE__ || false
		[global::Uno.NotImplemented("NET461", "__SKIA__", "__NETSTD_REFERENCE__")]
		public  void Dispose()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Midi.MidiOutPort", "void MidiOutPort.Dispose()");
		}
		#endif
		#if false || false || NET461 || false || __SKIA__ || __NETSTD_REFERENCE__ || false
		[global::Uno.NotImplemented("NET461", "__SKIA__", "__NETSTD_REFERENCE__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Devices.Midi.IMidiOutPort> FromIdAsync( string deviceId)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IMidiOutPort> MidiOutPort.FromIdAsync(string deviceId) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3CIMidiOutPort%3E%20MidiOutPort.FromIdAsync%28string%20deviceId%29");
		}
		#endif
		#if false || false || NET461 || false || __SKIA__ || __NETSTD_REFERENCE__ || false
		[global::Uno.NotImplemented("NET461", "__SKIA__", "__NETSTD_REFERENCE__")]
		public static string GetDeviceSelector()
		{
			throw new global::System.NotImplementedException("The member string MidiOutPort.GetDeviceSelector() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20MidiOutPort.GetDeviceSelector%28%29");
		}
		#endif
		// Processing: Windows.Devices.Midi.IMidiOutPort
		// Processing: System.IDisposable
	}
}
