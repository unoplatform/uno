#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Storage
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class SystemGPSProperties 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string LatitudeDecimal
		{
			get
			{
				throw new global::System.NotImplementedException("The member string SystemGPSProperties.LatitudeDecimal is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string LongitudeDecimal
		{
			get
			{
				throw new global::System.NotImplementedException("The member string SystemGPSProperties.LongitudeDecimal is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Storage.SystemGPSProperties.LatitudeDecimal.get
		// Forced skipping of method Windows.Storage.SystemGPSProperties.LongitudeDecimal.get
	}
}
