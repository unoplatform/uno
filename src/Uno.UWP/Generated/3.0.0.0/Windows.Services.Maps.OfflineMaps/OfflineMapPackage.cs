#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Services.Maps.OfflineMaps
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class OfflineMapPackage 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string DisplayName
		{
			get
			{
				throw new global::System.NotImplementedException("The member string OfflineMapPackage.DisplayName is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string EnclosingRegionName
		{
			get
			{
				throw new global::System.NotImplementedException("The member string OfflineMapPackage.EnclosingRegionName is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  ulong EstimatedSizeInBytes
		{
			get
			{
				throw new global::System.NotImplementedException("The member ulong OfflineMapPackage.EstimatedSizeInBytes is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Services.Maps.OfflineMaps.OfflineMapPackageStatus Status
		{
			get
			{
				throw new global::System.NotImplementedException("The member OfflineMapPackageStatus OfflineMapPackage.Status is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Services.Maps.OfflineMaps.OfflineMapPackage.Status.get
		// Forced skipping of method Windows.Services.Maps.OfflineMaps.OfflineMapPackage.DisplayName.get
		// Forced skipping of method Windows.Services.Maps.OfflineMaps.OfflineMapPackage.EnclosingRegionName.get
		// Forced skipping of method Windows.Services.Maps.OfflineMaps.OfflineMapPackage.EstimatedSizeInBytes.get
		// Forced skipping of method Windows.Services.Maps.OfflineMaps.OfflineMapPackage.StatusChanged.remove
		// Forced skipping of method Windows.Services.Maps.OfflineMaps.OfflineMapPackage.StatusChanged.add
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Services.Maps.OfflineMaps.OfflineMapPackageStartDownloadResult> RequestStartDownloadAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<OfflineMapPackageStartDownloadResult> OfflineMapPackage.RequestStartDownloadAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Services.Maps.OfflineMaps.OfflineMapPackageQueryResult> FindPackagesAsync( global::Windows.Devices.Geolocation.Geopoint queryPoint)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<OfflineMapPackageQueryResult> OfflineMapPackage.FindPackagesAsync(Geopoint queryPoint) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Services.Maps.OfflineMaps.OfflineMapPackageQueryResult> FindPackagesInBoundingBoxAsync( global::Windows.Devices.Geolocation.GeoboundingBox queryBoundingBox)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<OfflineMapPackageQueryResult> OfflineMapPackage.FindPackagesInBoundingBoxAsync(GeoboundingBox queryBoundingBox) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Services.Maps.OfflineMaps.OfflineMapPackageQueryResult> FindPackagesInGeocircleAsync( global::Windows.Devices.Geolocation.Geocircle queryCircle)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<OfflineMapPackageQueryResult> OfflineMapPackage.FindPackagesInGeocircleAsync(Geocircle queryCircle) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Services.Maps.OfflineMaps.OfflineMapPackage, object> StatusChanged
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Services.Maps.OfflineMaps.OfflineMapPackage", "event TypedEventHandler<OfflineMapPackage, object> OfflineMapPackage.StatusChanged");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Services.Maps.OfflineMaps.OfflineMapPackage", "event TypedEventHandler<OfflineMapPackage, object> OfflineMapPackage.StatusChanged");
			}
		}
		#endif
	}
}
