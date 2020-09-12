#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Geolocation
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class GeoboundingBox : global::Windows.Devices.Geolocation.IGeoshape
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Geolocation.BasicGeoposition Center
		{
			get
			{
				throw new global::System.NotImplementedException("The member BasicGeoposition GeoboundingBox.Center is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  double MaxAltitude
		{
			get
			{
				throw new global::System.NotImplementedException("The member double GeoboundingBox.MaxAltitude is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  double MinAltitude
		{
			get
			{
				throw new global::System.NotImplementedException("The member double GeoboundingBox.MinAltitude is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Geolocation.BasicGeoposition NorthwestCorner
		{
			get
			{
				throw new global::System.NotImplementedException("The member BasicGeoposition GeoboundingBox.NorthwestCorner is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Geolocation.BasicGeoposition SoutheastCorner
		{
			get
			{
				throw new global::System.NotImplementedException("The member BasicGeoposition GeoboundingBox.SoutheastCorner is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Geolocation.AltitudeReferenceSystem AltitudeReferenceSystem
		{
			get
			{
				throw new global::System.NotImplementedException("The member AltitudeReferenceSystem GeoboundingBox.AltitudeReferenceSystem is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Geolocation.GeoshapeType GeoshapeType
		{
			get
			{
				throw new global::System.NotImplementedException("The member GeoshapeType GeoboundingBox.GeoshapeType is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint SpatialReferenceId
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint GeoboundingBox.SpatialReferenceId is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public GeoboundingBox( global::Windows.Devices.Geolocation.BasicGeoposition northwestCorner,  global::Windows.Devices.Geolocation.BasicGeoposition southeastCorner) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Geolocation.GeoboundingBox", "GeoboundingBox.GeoboundingBox(BasicGeoposition northwestCorner, BasicGeoposition southeastCorner)");
		}
		#endif
		// Forced skipping of method Windows.Devices.Geolocation.GeoboundingBox.GeoboundingBox(Windows.Devices.Geolocation.BasicGeoposition, Windows.Devices.Geolocation.BasicGeoposition)
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public GeoboundingBox( global::Windows.Devices.Geolocation.BasicGeoposition northwestCorner,  global::Windows.Devices.Geolocation.BasicGeoposition southeastCorner,  global::Windows.Devices.Geolocation.AltitudeReferenceSystem altitudeReferenceSystem) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Geolocation.GeoboundingBox", "GeoboundingBox.GeoboundingBox(BasicGeoposition northwestCorner, BasicGeoposition southeastCorner, AltitudeReferenceSystem altitudeReferenceSystem)");
		}
		#endif
		// Forced skipping of method Windows.Devices.Geolocation.GeoboundingBox.GeoboundingBox(Windows.Devices.Geolocation.BasicGeoposition, Windows.Devices.Geolocation.BasicGeoposition, Windows.Devices.Geolocation.AltitudeReferenceSystem)
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public GeoboundingBox( global::Windows.Devices.Geolocation.BasicGeoposition northwestCorner,  global::Windows.Devices.Geolocation.BasicGeoposition southeastCorner,  global::Windows.Devices.Geolocation.AltitudeReferenceSystem altitudeReferenceSystem,  uint spatialReferenceId) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Geolocation.GeoboundingBox", "GeoboundingBox.GeoboundingBox(BasicGeoposition northwestCorner, BasicGeoposition southeastCorner, AltitudeReferenceSystem altitudeReferenceSystem, uint spatialReferenceId)");
		}
		#endif
		// Forced skipping of method Windows.Devices.Geolocation.GeoboundingBox.GeoboundingBox(Windows.Devices.Geolocation.BasicGeoposition, Windows.Devices.Geolocation.BasicGeoposition, Windows.Devices.Geolocation.AltitudeReferenceSystem, uint)
		// Forced skipping of method Windows.Devices.Geolocation.GeoboundingBox.NorthwestCorner.get
		// Forced skipping of method Windows.Devices.Geolocation.GeoboundingBox.SoutheastCorner.get
		// Forced skipping of method Windows.Devices.Geolocation.GeoboundingBox.Center.get
		// Forced skipping of method Windows.Devices.Geolocation.GeoboundingBox.MinAltitude.get
		// Forced skipping of method Windows.Devices.Geolocation.GeoboundingBox.MaxAltitude.get
		// Forced skipping of method Windows.Devices.Geolocation.GeoboundingBox.GeoshapeType.get
		// Forced skipping of method Windows.Devices.Geolocation.GeoboundingBox.SpatialReferenceId.get
		// Forced skipping of method Windows.Devices.Geolocation.GeoboundingBox.AltitudeReferenceSystem.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Devices.Geolocation.GeoboundingBox TryCompute( global::System.Collections.Generic.IEnumerable<global::Windows.Devices.Geolocation.BasicGeoposition> positions)
		{
			throw new global::System.NotImplementedException("The member GeoboundingBox GeoboundingBox.TryCompute(IEnumerable<BasicGeoposition> positions) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Devices.Geolocation.GeoboundingBox TryCompute( global::System.Collections.Generic.IEnumerable<global::Windows.Devices.Geolocation.BasicGeoposition> positions,  global::Windows.Devices.Geolocation.AltitudeReferenceSystem altitudeRefSystem)
		{
			throw new global::System.NotImplementedException("The member GeoboundingBox GeoboundingBox.TryCompute(IEnumerable<BasicGeoposition> positions, AltitudeReferenceSystem altitudeRefSystem) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Devices.Geolocation.GeoboundingBox TryCompute( global::System.Collections.Generic.IEnumerable<global::Windows.Devices.Geolocation.BasicGeoposition> positions,  global::Windows.Devices.Geolocation.AltitudeReferenceSystem altitudeRefSystem,  uint spatialReferenceId)
		{
			throw new global::System.NotImplementedException("The member GeoboundingBox GeoboundingBox.TryCompute(IEnumerable<BasicGeoposition> positions, AltitudeReferenceSystem altitudeRefSystem, uint spatialReferenceId) is not implemented in Uno.");
		}
		#endif
		// Processing: Windows.Devices.Geolocation.IGeoshape
	}
}
