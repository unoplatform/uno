#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Audio
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class SpatialAudioDeviceConfiguration 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string ActiveSpatialAudioFormat
		{
			get
			{
				throw new global::System.NotImplementedException("The member string SpatialAudioDeviceConfiguration.ActiveSpatialAudioFormat is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string DefaultSpatialAudioFormat
		{
			get
			{
				throw new global::System.NotImplementedException("The member string SpatialAudioDeviceConfiguration.DefaultSpatialAudioFormat is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string DeviceId
		{
			get
			{
				throw new global::System.NotImplementedException("The member string SpatialAudioDeviceConfiguration.DeviceId is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsSpatialAudioSupported
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool SpatialAudioDeviceConfiguration.IsSpatialAudioSupported is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.Audio.SpatialAudioDeviceConfiguration.DeviceId.get
		// Forced skipping of method Windows.Media.Audio.SpatialAudioDeviceConfiguration.IsSpatialAudioSupported.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsSpatialAudioFormatSupported( string subtype)
		{
			throw new global::System.NotImplementedException("The member bool SpatialAudioDeviceConfiguration.IsSpatialAudioFormatSupported(string subtype) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Media.Audio.SpatialAudioDeviceConfiguration.ActiveSpatialAudioFormat.get
		// Forced skipping of method Windows.Media.Audio.SpatialAudioDeviceConfiguration.DefaultSpatialAudioFormat.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Media.Audio.SetDefaultSpatialAudioFormatResult> SetDefaultSpatialAudioFormatAsync( string subtype)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<SetDefaultSpatialAudioFormatResult> SpatialAudioDeviceConfiguration.SetDefaultSpatialAudioFormatAsync(string subtype) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Media.Audio.SpatialAudioDeviceConfiguration.ConfigurationChanged.add
		// Forced skipping of method Windows.Media.Audio.SpatialAudioDeviceConfiguration.ConfigurationChanged.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Media.Audio.SpatialAudioDeviceConfiguration GetForDeviceId( string deviceId)
		{
			throw new global::System.NotImplementedException("The member SpatialAudioDeviceConfiguration SpatialAudioDeviceConfiguration.GetForDeviceId(string deviceId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Media.Audio.SpatialAudioDeviceConfiguration, object> ConfigurationChanged
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Audio.SpatialAudioDeviceConfiguration", "event TypedEventHandler<SpatialAudioDeviceConfiguration, object> SpatialAudioDeviceConfiguration.ConfigurationChanged");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Audio.SpatialAudioDeviceConfiguration", "event TypedEventHandler<SpatialAudioDeviceConfiguration, object> SpatialAudioDeviceConfiguration.ConfigurationChanged");
			}
		}
		#endif
	}
}
