#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Geolocation
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public  partial class Geoposition 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::Windows.Devices.Geolocation.CivicAddress CivicAddress
		{
			get
			{
				throw new global::System.NotImplementedException("The member CivicAddress Geoposition.CivicAddress is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::Windows.Devices.Geolocation.Geocoordinate Coordinate
		{
			get
			{
				throw new global::System.NotImplementedException("The member Geocoordinate Geoposition.Coordinate is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::Windows.Devices.Geolocation.VenueData VenueData
		{
			get
			{
				throw new global::System.NotImplementedException("The member VenueData Geoposition.VenueData is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.Geolocation.Geoposition.Coordinate.get
		// Forced skipping of method Windows.Devices.Geolocation.Geoposition.CivicAddress.get
		// Forced skipping of method Windows.Devices.Geolocation.Geoposition.VenueData.get
	}
}
