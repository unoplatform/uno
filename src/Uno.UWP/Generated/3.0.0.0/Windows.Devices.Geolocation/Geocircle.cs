#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Geolocation
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class Geocircle : global::Windows.Devices.Geolocation.IGeoshape
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Geolocation.BasicGeoposition Center
		{
			get
			{
				throw new global::System.NotImplementedException("The member BasicGeoposition Geocircle.Center is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  double Radius
		{
			get
			{
				throw new global::System.NotImplementedException("The member double Geocircle.Radius is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Geolocation.AltitudeReferenceSystem AltitudeReferenceSystem
		{
			get
			{
				throw new global::System.NotImplementedException("The member AltitudeReferenceSystem Geocircle.AltitudeReferenceSystem is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Geolocation.GeoshapeType GeoshapeType
		{
			get
			{
				throw new global::System.NotImplementedException("The member GeoshapeType Geocircle.GeoshapeType is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint SpatialReferenceId
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint Geocircle.SpatialReferenceId is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public Geocircle( global::Windows.Devices.Geolocation.BasicGeoposition position,  double radius) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Geolocation.Geocircle", "Geocircle.Geocircle(BasicGeoposition position, double radius)");
		}
		#endif
		// Forced skipping of method Windows.Devices.Geolocation.Geocircle.Geocircle(Windows.Devices.Geolocation.BasicGeoposition, double)
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public Geocircle( global::Windows.Devices.Geolocation.BasicGeoposition position,  double radius,  global::Windows.Devices.Geolocation.AltitudeReferenceSystem altitudeReferenceSystem) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Geolocation.Geocircle", "Geocircle.Geocircle(BasicGeoposition position, double radius, AltitudeReferenceSystem altitudeReferenceSystem)");
		}
		#endif
		// Forced skipping of method Windows.Devices.Geolocation.Geocircle.Geocircle(Windows.Devices.Geolocation.BasicGeoposition, double, Windows.Devices.Geolocation.AltitudeReferenceSystem)
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public Geocircle( global::Windows.Devices.Geolocation.BasicGeoposition position,  double radius,  global::Windows.Devices.Geolocation.AltitudeReferenceSystem altitudeReferenceSystem,  uint spatialReferenceId) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Geolocation.Geocircle", "Geocircle.Geocircle(BasicGeoposition position, double radius, AltitudeReferenceSystem altitudeReferenceSystem, uint spatialReferenceId)");
		}
		#endif
		// Forced skipping of method Windows.Devices.Geolocation.Geocircle.Geocircle(Windows.Devices.Geolocation.BasicGeoposition, double, Windows.Devices.Geolocation.AltitudeReferenceSystem, uint)
		// Forced skipping of method Windows.Devices.Geolocation.Geocircle.Center.get
		// Forced skipping of method Windows.Devices.Geolocation.Geocircle.Radius.get
		// Forced skipping of method Windows.Devices.Geolocation.Geocircle.GeoshapeType.get
		// Forced skipping of method Windows.Devices.Geolocation.Geocircle.SpatialReferenceId.get
		// Forced skipping of method Windows.Devices.Geolocation.Geocircle.AltitudeReferenceSystem.get
		// Processing: Windows.Devices.Geolocation.IGeoshape
	}
}
