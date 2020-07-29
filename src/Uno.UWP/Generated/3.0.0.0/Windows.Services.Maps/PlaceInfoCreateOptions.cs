#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Services.Maps
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PlaceInfoCreateOptions 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string DisplayName
		{
			get
			{
				throw new global::System.NotImplementedException("The member string PlaceInfoCreateOptions.DisplayName is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Services.Maps.PlaceInfoCreateOptions", "string PlaceInfoCreateOptions.DisplayName");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string DisplayAddress
		{
			get
			{
				throw new global::System.NotImplementedException("The member string PlaceInfoCreateOptions.DisplayAddress is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Services.Maps.PlaceInfoCreateOptions", "string PlaceInfoCreateOptions.DisplayAddress");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public PlaceInfoCreateOptions() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Services.Maps.PlaceInfoCreateOptions", "PlaceInfoCreateOptions.PlaceInfoCreateOptions()");
		}
		#endif
		// Forced skipping of method Windows.Services.Maps.PlaceInfoCreateOptions.PlaceInfoCreateOptions()
		// Forced skipping of method Windows.Services.Maps.PlaceInfoCreateOptions.DisplayName.set
		// Forced skipping of method Windows.Services.Maps.PlaceInfoCreateOptions.DisplayName.get
		// Forced skipping of method Windows.Services.Maps.PlaceInfoCreateOptions.DisplayAddress.set
		// Forced skipping of method Windows.Services.Maps.PlaceInfoCreateOptions.DisplayAddress.get
	}
}
