#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Services.Maps.OfflineMaps
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class OfflineMapPackageQueryResult 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Services.Maps.OfflineMaps.OfflineMapPackage> Packages
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<OfflineMapPackage> OfflineMapPackageQueryResult.Packages is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Services.Maps.OfflineMaps.OfflineMapPackageQueryStatus Status
		{
			get
			{
				throw new global::System.NotImplementedException("The member OfflineMapPackageQueryStatus OfflineMapPackageQueryResult.Status is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Services.Maps.OfflineMaps.OfflineMapPackageQueryResult.Status.get
		// Forced skipping of method Windows.Services.Maps.OfflineMaps.OfflineMapPackageQueryResult.Packages.get
	}
}
