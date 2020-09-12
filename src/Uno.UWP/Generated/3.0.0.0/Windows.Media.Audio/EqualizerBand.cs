#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Audio
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class EqualizerBand 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  double Gain
		{
			get
			{
				throw new global::System.NotImplementedException("The member double EqualizerBand.Gain is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Audio.EqualizerBand", "double EqualizerBand.Gain");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  double FrequencyCenter
		{
			get
			{
				throw new global::System.NotImplementedException("The member double EqualizerBand.FrequencyCenter is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Audio.EqualizerBand", "double EqualizerBand.FrequencyCenter");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  double Bandwidth
		{
			get
			{
				throw new global::System.NotImplementedException("The member double EqualizerBand.Bandwidth is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Audio.EqualizerBand", "double EqualizerBand.Bandwidth");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.Audio.EqualizerBand.Bandwidth.get
		// Forced skipping of method Windows.Media.Audio.EqualizerBand.Bandwidth.set
		// Forced skipping of method Windows.Media.Audio.EqualizerBand.FrequencyCenter.get
		// Forced skipping of method Windows.Media.Audio.EqualizerBand.FrequencyCenter.set
		// Forced skipping of method Windows.Media.Audio.EqualizerBand.Gain.get
		// Forced skipping of method Windows.Media.Audio.EqualizerBand.Gain.set
	}
}
