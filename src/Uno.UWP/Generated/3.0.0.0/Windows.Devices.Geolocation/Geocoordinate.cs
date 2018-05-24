#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Geolocation
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public  partial class Geocoordinate 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  double Accuracy
		{
			get
			{
				throw new global::System.NotImplementedException("The member double Geocoordinate.Accuracy is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  double? Altitude
		{
			get
			{
				throw new global::System.NotImplementedException("The member double? Geocoordinate.Altitude is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  double? AltitudeAccuracy
		{
			get
			{
				throw new global::System.NotImplementedException("The member double? Geocoordinate.AltitudeAccuracy is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  double? Heading
		{
			get
			{
				throw new global::System.NotImplementedException("The member double? Geocoordinate.Heading is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  double Latitude
		{
			get
			{
				throw new global::System.NotImplementedException("The member double Geocoordinate.Latitude is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  double Longitude
		{
			get
			{
				throw new global::System.NotImplementedException("The member double Geocoordinate.Longitude is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  double? Speed
		{
			get
			{
				throw new global::System.NotImplementedException("The member double? Geocoordinate.Speed is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::System.DateTimeOffset Timestamp
		{
			get
			{
				throw new global::System.NotImplementedException("The member DateTimeOffset Geocoordinate.Timestamp is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::Windows.Devices.Geolocation.Geopoint Point
		{
			get
			{
				throw new global::System.NotImplementedException("The member Geopoint Geocoordinate.Point is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::Windows.Devices.Geolocation.PositionSource PositionSource
		{
			get
			{
				throw new global::System.NotImplementedException("The member PositionSource Geocoordinate.PositionSource is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::Windows.Devices.Geolocation.GeocoordinateSatelliteData SatelliteData
		{
			get
			{
				throw new global::System.NotImplementedException("The member GeocoordinateSatelliteData Geocoordinate.SatelliteData is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::System.DateTimeOffset? PositionSourceTimestamp
		{
			get
			{
				throw new global::System.NotImplementedException("The member DateTimeOffset? Geocoordinate.PositionSourceTimestamp is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.Geolocation.Geocoordinate.Latitude.get
		// Forced skipping of method Windows.Devices.Geolocation.Geocoordinate.Longitude.get
		// Forced skipping of method Windows.Devices.Geolocation.Geocoordinate.Altitude.get
		// Forced skipping of method Windows.Devices.Geolocation.Geocoordinate.Accuracy.get
		// Forced skipping of method Windows.Devices.Geolocation.Geocoordinate.AltitudeAccuracy.get
		// Forced skipping of method Windows.Devices.Geolocation.Geocoordinate.Heading.get
		// Forced skipping of method Windows.Devices.Geolocation.Geocoordinate.Speed.get
		// Forced skipping of method Windows.Devices.Geolocation.Geocoordinate.Timestamp.get
		// Forced skipping of method Windows.Devices.Geolocation.Geocoordinate.PositionSource.get
		// Forced skipping of method Windows.Devices.Geolocation.Geocoordinate.SatelliteData.get
		// Forced skipping of method Windows.Devices.Geolocation.Geocoordinate.Point.get
		// Forced skipping of method Windows.Devices.Geolocation.Geocoordinate.PositionSourceTimestamp.get
	}
}
