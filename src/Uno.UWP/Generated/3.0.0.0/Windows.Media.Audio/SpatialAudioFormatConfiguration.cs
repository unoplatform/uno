#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Audio
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class SpatialAudioFormatConfiguration 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Audio.MixedRealitySpatialAudioFormatPolicy MixedRealityExclusiveModePolicy
		{
			get
			{
				throw new global::System.NotImplementedException("The member MixedRealitySpatialAudioFormatPolicy SpatialAudioFormatConfiguration.MixedRealityExclusiveModePolicy is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Audio.SpatialAudioFormatConfiguration", "MixedRealitySpatialAudioFormatPolicy SpatialAudioFormatConfiguration.MixedRealityExclusiveModePolicy");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction ReportLicenseChangedAsync( string subtype)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction SpatialAudioFormatConfiguration.ReportLicenseChangedAsync(string subtype) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction ReportConfigurationChangedAsync( string subtype)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction SpatialAudioFormatConfiguration.ReportConfigurationChangedAsync(string subtype) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Media.Audio.SpatialAudioFormatConfiguration.MixedRealityExclusiveModePolicy.get
		// Forced skipping of method Windows.Media.Audio.SpatialAudioFormatConfiguration.MixedRealityExclusiveModePolicy.set
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Media.Audio.SpatialAudioFormatConfiguration GetDefault()
		{
			throw new global::System.NotImplementedException("The member SpatialAudioFormatConfiguration SpatialAudioFormatConfiguration.GetDefault() is not implemented in Uno.");
		}
		#endif
	}
}
