#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.Connectivity
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ConnectivityManager 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Networking.Connectivity.ConnectionSession> AcquireConnectionAsync( global::Windows.Networking.Connectivity.CellularApnContext cellularApnContext)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<ConnectionSession> ConnectivityManager.AcquireConnectionAsync(CellularApnContext cellularApnContext) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static void AddHttpRoutePolicy( global::Windows.Networking.Connectivity.RoutePolicy routePolicy)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.Connectivity.ConnectivityManager", "void ConnectivityManager.AddHttpRoutePolicy(RoutePolicy routePolicy)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static void RemoveHttpRoutePolicy( global::Windows.Networking.Connectivity.RoutePolicy routePolicy)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.Connectivity.ConnectivityManager", "void ConnectivityManager.RemoveHttpRoutePolicy(RoutePolicy routePolicy)");
		}
		#endif
	}
}
