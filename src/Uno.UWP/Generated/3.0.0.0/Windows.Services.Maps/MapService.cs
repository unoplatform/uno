#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Services.Maps
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class MapService 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static string ServiceToken
		{
			get
			{
				throw new global::System.NotImplementedException("The member string MapService.ServiceToken is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Services.Maps.MapService", "string MapService.ServiceToken");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static string WorldViewRegionCode
		{
			get
			{
				throw new global::System.NotImplementedException("The member string MapService.WorldViewRegionCode is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static string DataAttributions
		{
			get
			{
				throw new global::System.NotImplementedException("The member string MapService.DataAttributions is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Services.Maps.MapServiceDataUsagePreference DataUsagePreference
		{
			get
			{
				throw new global::System.NotImplementedException("The member MapServiceDataUsagePreference MapService.DataUsagePreference is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Services.Maps.MapService", "MapServiceDataUsagePreference MapService.DataUsagePreference");
			}
		}
		#endif
		// Forced skipping of method Windows.Services.Maps.MapService.DataUsagePreference.set
		// Forced skipping of method Windows.Services.Maps.MapService.DataUsagePreference.get
		// Forced skipping of method Windows.Services.Maps.MapService.DataAttributions.get
		// Forced skipping of method Windows.Services.Maps.MapService.WorldViewRegionCode.get
		// Forced skipping of method Windows.Services.Maps.MapService.ServiceToken.set
		// Forced skipping of method Windows.Services.Maps.MapService.ServiceToken.get
	}
}
