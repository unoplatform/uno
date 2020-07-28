#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Geolocation
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IGeoshape 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Devices.Geolocation.AltitudeReferenceSystem AltitudeReferenceSystem
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Devices.Geolocation.GeoshapeType GeoshapeType
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		uint SpatialReferenceId
		{
			get;
		}
		#endif
		// Forced skipping of method Windows.Devices.Geolocation.IGeoshape.GeoshapeType.get
		// Forced skipping of method Windows.Devices.Geolocation.IGeoshape.SpatialReferenceId.get
		// Forced skipping of method Windows.Devices.Geolocation.IGeoshape.AltitudeReferenceSystem.get
	}
}
