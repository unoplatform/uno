using CoreLocation;

namespace Windows.Devices.Geolocation
{
	public static class GeolocatorHelper
	{
		/// <summary>
		/// Return an instance of CLLocationManager to set properties
		/// </summary>
		public static CLLocationManager GetLocationManager(this Geolocator instance)
		   => instance.LocationManager;
	}
}
