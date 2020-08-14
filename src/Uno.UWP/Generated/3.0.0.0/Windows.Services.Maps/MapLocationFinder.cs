#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Services.Maps
{
	#if false || false || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class MapLocationFinder 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Services.Maps.MapLocationFinderResult> FindLocationsAtAsync( global::Windows.Devices.Geolocation.Geopoint queryPoint,  global::Windows.Services.Maps.MapLocationDesiredAccuracy accuracy)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<MapLocationFinderResult> MapLocationFinder.FindLocationsAtAsync(Geopoint queryPoint, MapLocationDesiredAccuracy accuracy) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Services.Maps.MapLocationFinderResult> FindLocationsAtAsync( global::Windows.Devices.Geolocation.Geopoint queryPoint)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<MapLocationFinderResult> MapLocationFinder.FindLocationsAtAsync(Geopoint queryPoint) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Services.Maps.MapLocationFinderResult> FindLocationsAsync( string searchText,  global::Windows.Devices.Geolocation.Geopoint referencePoint)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<MapLocationFinderResult> MapLocationFinder.FindLocationsAsync(string searchText, Geopoint referencePoint) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Services.Maps.MapLocationFinderResult> FindLocationsAsync( string searchText,  global::Windows.Devices.Geolocation.Geopoint referencePoint,  uint maxCount)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<MapLocationFinderResult> MapLocationFinder.FindLocationsAsync(string searchText, Geopoint referencePoint, uint maxCount) is not implemented in Uno.");
		}
		#endif
	}
}
