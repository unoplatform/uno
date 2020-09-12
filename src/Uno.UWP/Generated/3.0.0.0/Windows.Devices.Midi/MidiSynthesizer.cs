#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Midi
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class MidiSynthesizer : global::Windows.Devices.Midi.IMidiOutPort,global::System.IDisposable
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string DeviceId
		{
			get
			{
				throw new global::System.NotImplementedException("The member string MidiSynthesizer.DeviceId is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  double Volume
		{
			get
			{
				throw new global::System.NotImplementedException("The member double MidiSynthesizer.Volume is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Midi.MidiSynthesizer", "double MidiSynthesizer.Volume");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Enumeration.DeviceInformation AudioDevice
		{
			get
			{
				throw new global::System.NotImplementedException("The member DeviceInformation MidiSynthesizer.AudioDevice is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.Midi.MidiSynthesizer.AudioDevice.get
		// Forced skipping of method Windows.Devices.Midi.MidiSynthesizer.Volume.get
		// Forced skipping of method Windows.Devices.Midi.MidiSynthesizer.Volume.set
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void SendMessage( global::Windows.Devices.Midi.IMidiMessage midiMessage)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Midi.MidiSynthesizer", "void MidiSynthesizer.SendMessage(IMidiMessage midiMessage)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void SendBuffer( global::Windows.Storage.Streams.IBuffer midiData)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Midi.MidiSynthesizer", "void MidiSynthesizer.SendBuffer(IBuffer midiData)");
		}
		#endif
		// Forced skipping of method Windows.Devices.Midi.MidiSynthesizer.DeviceId.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Dispose()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Midi.MidiSynthesizer", "void MidiSynthesizer.Dispose()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Devices.Midi.MidiSynthesizer> CreateAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<MidiSynthesizer> MidiSynthesizer.CreateAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Devices.Midi.MidiSynthesizer> CreateAsync( global::Windows.Devices.Enumeration.DeviceInformation audioDevice)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<MidiSynthesizer> MidiSynthesizer.CreateAsync(DeviceInformation audioDevice) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static bool IsSynthesizer( global::Windows.Devices.Enumeration.DeviceInformation midiDevice)
		{
			throw new global::System.NotImplementedException("The member bool MidiSynthesizer.IsSynthesizer(DeviceInformation midiDevice) is not implemented in Uno.");
		}
		#endif
		// Processing: Windows.Devices.Midi.IMidiOutPort
		// Processing: System.IDisposable
	}
}
