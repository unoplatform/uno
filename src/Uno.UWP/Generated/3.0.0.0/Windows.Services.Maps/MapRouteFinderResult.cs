#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Services.Maps
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class MapRouteFinderResult 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Services.Maps.MapRoute Route
		{
			get
			{
				throw new global::System.NotImplementedException("The member MapRoute MapRouteFinderResult.Route is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Services.Maps.MapRouteFinderStatus Status
		{
			get
			{
				throw new global::System.NotImplementedException("The member MapRouteFinderStatus MapRouteFinderResult.Status is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Services.Maps.MapRoute> AlternateRoutes
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<MapRoute> MapRouteFinderResult.AlternateRoutes is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Services.Maps.MapRouteFinderResult.Route.get
		// Forced skipping of method Windows.Services.Maps.MapRouteFinderResult.Status.get
		// Forced skipping of method Windows.Services.Maps.MapRouteFinderResult.AlternateRoutes.get
	}
}
