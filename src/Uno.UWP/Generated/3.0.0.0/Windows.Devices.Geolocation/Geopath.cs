#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Geolocation
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class Geopath : global::Windows.Devices.Geolocation.IGeoshape
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Devices.Geolocation.BasicGeoposition> Positions
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<BasicGeoposition> Geopath.Positions is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Geolocation.AltitudeReferenceSystem AltitudeReferenceSystem
		{
			get
			{
				throw new global::System.NotImplementedException("The member AltitudeReferenceSystem Geopath.AltitudeReferenceSystem is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Geolocation.GeoshapeType GeoshapeType
		{
			get
			{
				throw new global::System.NotImplementedException("The member GeoshapeType Geopath.GeoshapeType is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint SpatialReferenceId
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint Geopath.SpatialReferenceId is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public Geopath( global::System.Collections.Generic.IEnumerable<global::Windows.Devices.Geolocation.BasicGeoposition> positions) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Geolocation.Geopath", "Geopath.Geopath(IEnumerable<BasicGeoposition> positions)");
		}
		#endif
		// Forced skipping of method Windows.Devices.Geolocation.Geopath.Geopath(System.Collections.Generic.IEnumerable<Windows.Devices.Geolocation.BasicGeoposition>)
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public Geopath( global::System.Collections.Generic.IEnumerable<global::Windows.Devices.Geolocation.BasicGeoposition> positions,  global::Windows.Devices.Geolocation.AltitudeReferenceSystem altitudeReferenceSystem) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Geolocation.Geopath", "Geopath.Geopath(IEnumerable<BasicGeoposition> positions, AltitudeReferenceSystem altitudeReferenceSystem)");
		}
		#endif
		// Forced skipping of method Windows.Devices.Geolocation.Geopath.Geopath(System.Collections.Generic.IEnumerable<Windows.Devices.Geolocation.BasicGeoposition>, Windows.Devices.Geolocation.AltitudeReferenceSystem)
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public Geopath( global::System.Collections.Generic.IEnumerable<global::Windows.Devices.Geolocation.BasicGeoposition> positions,  global::Windows.Devices.Geolocation.AltitudeReferenceSystem altitudeReferenceSystem,  uint spatialReferenceId) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Geolocation.Geopath", "Geopath.Geopath(IEnumerable<BasicGeoposition> positions, AltitudeReferenceSystem altitudeReferenceSystem, uint spatialReferenceId)");
		}
		#endif
		// Forced skipping of method Windows.Devices.Geolocation.Geopath.Geopath(System.Collections.Generic.IEnumerable<Windows.Devices.Geolocation.BasicGeoposition>, Windows.Devices.Geolocation.AltitudeReferenceSystem, uint)
		// Forced skipping of method Windows.Devices.Geolocation.Geopath.Positions.get
		// Forced skipping of method Windows.Devices.Geolocation.Geopath.GeoshapeType.get
		// Forced skipping of method Windows.Devices.Geolocation.Geopath.SpatialReferenceId.get
		// Forced skipping of method Windows.Devices.Geolocation.Geopath.AltitudeReferenceSystem.get
		// Processing: Windows.Devices.Geolocation.IGeoshape
	}
}
