#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Devices
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class DefaultAudioRenderDeviceChangedEventArgs : global::Windows.Media.Devices.IDefaultAudioDeviceChangedEventArgs
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Id
		{
			get
			{
				throw new global::System.NotImplementedException("The member string DefaultAudioRenderDeviceChangedEventArgs.Id is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Devices.AudioDeviceRole Role
		{
			get
			{
				throw new global::System.NotImplementedException("The member AudioDeviceRole DefaultAudioRenderDeviceChangedEventArgs.Role is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.Devices.DefaultAudioRenderDeviceChangedEventArgs.Id.get
		// Forced skipping of method Windows.Media.Devices.DefaultAudioRenderDeviceChangedEventArgs.Role.get
		// Processing: Windows.Media.Devices.IDefaultAudioDeviceChangedEventArgs
	}
}
