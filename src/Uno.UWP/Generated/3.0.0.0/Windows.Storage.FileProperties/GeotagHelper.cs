#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Storage.FileProperties
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class GeotagHelper 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Devices.Geolocation.Geopoint> GetGeotagAsync( global::Windows.Storage.IStorageFile file)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<Geopoint> GeotagHelper.GetGeotagAsync(IStorageFile file) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncAction SetGeotagFromGeolocatorAsync( global::Windows.Storage.IStorageFile file,  global::Windows.Devices.Geolocation.Geolocator geolocator)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction GeotagHelper.SetGeotagFromGeolocatorAsync(IStorageFile file, Geolocator geolocator) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncAction SetGeotagAsync( global::Windows.Storage.IStorageFile file,  global::Windows.Devices.Geolocation.Geopoint geopoint)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction GeotagHelper.SetGeotagAsync(IStorageFile file, Geopoint geopoint) is not implemented in Uno.");
		}
		#endif
	}
}
