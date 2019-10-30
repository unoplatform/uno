#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Geolocation
{
	#if false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class Geopoint : global::Windows.Devices.Geolocation.IGeoshape
	{
		// Skipping already declared property Position
		#if __ANDROID__ || __IOS__ || NET461 || false || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Devices.Geolocation.AltitudeReferenceSystem AltitudeReferenceSystem
		{
			get
			{
				throw new global::System.NotImplementedException("The member AltitudeReferenceSystem Geopoint.AltitudeReferenceSystem is not implemented in Uno.");
			}
		}
		#endif
		// Skipping already declared property GeoshapeType
		#if __ANDROID__ || __IOS__ || NET461 || false || __MACOS__
		[global::Uno.NotImplemented]
		public  uint SpatialReferenceId
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint Geopoint.SpatialReferenceId is not implemented in Uno.");
			}
		}
		#endif
		// Skipping already declared method Windows.Devices.Geolocation.Geopoint.Geopoint(Windows.Devices.Geolocation.BasicGeoposition)
		// Forced skipping of method Windows.Devices.Geolocation.Geopoint.Geopoint(Windows.Devices.Geolocation.BasicGeoposition)
		#if __ANDROID__ || __IOS__ || NET461 || false || __MACOS__
		[global::Uno.NotImplemented]
		public Geopoint( global::Windows.Devices.Geolocation.BasicGeoposition position,  global::Windows.Devices.Geolocation.AltitudeReferenceSystem altitudeReferenceSystem) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Geolocation.Geopoint", "Geopoint.Geopoint(BasicGeoposition position, AltitudeReferenceSystem altitudeReferenceSystem)");
		}
		#endif
		// Forced skipping of method Windows.Devices.Geolocation.Geopoint.Geopoint(Windows.Devices.Geolocation.BasicGeoposition, Windows.Devices.Geolocation.AltitudeReferenceSystem)
		#if __ANDROID__ || __IOS__ || NET461 || false || __MACOS__
		[global::Uno.NotImplemented]
		public Geopoint( global::Windows.Devices.Geolocation.BasicGeoposition position,  global::Windows.Devices.Geolocation.AltitudeReferenceSystem altitudeReferenceSystem,  uint spatialReferenceId) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Geolocation.Geopoint", "Geopoint.Geopoint(BasicGeoposition position, AltitudeReferenceSystem altitudeReferenceSystem, uint spatialReferenceId)");
		}
		#endif
		// Forced skipping of method Windows.Devices.Geolocation.Geopoint.Geopoint(Windows.Devices.Geolocation.BasicGeoposition, Windows.Devices.Geolocation.AltitudeReferenceSystem, uint)
		// Forced skipping of method Windows.Devices.Geolocation.Geopoint.Position.get
		// Forced skipping of method Windows.Devices.Geolocation.Geopoint.GeoshapeType.get
		// Forced skipping of method Windows.Devices.Geolocation.Geopoint.SpatialReferenceId.get
		// Forced skipping of method Windows.Devices.Geolocation.Geopoint.AltitudeReferenceSystem.get
		// Processing: Windows.Devices.Geolocation.IGeoshape
	}
}
