#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.MediaProperties
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class MediaRatio 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint Numerator
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint MediaRatio.Numerator is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.MediaProperties.MediaRatio", "uint MediaRatio.Numerator");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint Denominator
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint MediaRatio.Denominator is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.MediaProperties.MediaRatio", "uint MediaRatio.Denominator");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.MediaProperties.MediaRatio.Numerator.set
		// Forced skipping of method Windows.Media.MediaProperties.MediaRatio.Numerator.get
		// Forced skipping of method Windows.Media.MediaProperties.MediaRatio.Denominator.set
		// Forced skipping of method Windows.Media.MediaProperties.MediaRatio.Denominator.get
	}
}
