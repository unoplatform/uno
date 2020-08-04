#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Geolocation
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class GeocoordinateSatelliteData 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  double? HorizontalDilutionOfPrecision
		{
			get
			{
				throw new global::System.NotImplementedException("The member double? GeocoordinateSatelliteData.HorizontalDilutionOfPrecision is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  double? PositionDilutionOfPrecision
		{
			get
			{
				throw new global::System.NotImplementedException("The member double? GeocoordinateSatelliteData.PositionDilutionOfPrecision is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  double? VerticalDilutionOfPrecision
		{
			get
			{
				throw new global::System.NotImplementedException("The member double? GeocoordinateSatelliteData.VerticalDilutionOfPrecision is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.Geolocation.GeocoordinateSatelliteData.PositionDilutionOfPrecision.get
		// Forced skipping of method Windows.Devices.Geolocation.GeocoordinateSatelliteData.HorizontalDilutionOfPrecision.get
		// Forced skipping of method Windows.Devices.Geolocation.GeocoordinateSatelliteData.VerticalDilutionOfPrecision.get
	}
}
