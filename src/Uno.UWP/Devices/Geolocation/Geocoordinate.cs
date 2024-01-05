#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
using System;

namespace Windows.Devices.Geolocation
{
	public partial class Geocoordinate
	{
		public Geocoordinate(
			double latitude,
			double longitude,
			double accuracy,
			DateTimeOffset timestamp,
			Geopoint point,
			double? altitude = null,
			double? altitudeAccuracy = null,
			double? heading = null,
			double? speed = null,
			PositionSource positionSource = PositionSource.Default,
			GeocoordinateSatelliteData? satelliteData = null,
			DateTimeOffset? positionSourceTimestamp = null
		)
		{
			Latitude = latitude;
			Longitude = longitude;
			Accuracy = accuracy;
			Timestamp = timestamp;
			Point = point;
			Altitude = altitude;
			AltitudeAccuracy = altitudeAccuracy;
			Heading = heading;
			Speed = speed;
			PositionSource = positionSource;
			SatelliteData = satelliteData;
			PositionSourceTimestamp = positionSourceTimestamp;
		}

		public double Accuracy { get; }

		public double? Altitude { get; }

		public double? AltitudeAccuracy { get; }

		public double? Heading { get; }

		public double Latitude { get; }

		public double Longitude { get; }

		public double? Speed { get; }

		public global::System.DateTimeOffset Timestamp { get; }

		public global::Windows.Devices.Geolocation.Geopoint Point { get; }

		public global::Windows.Devices.Geolocation.PositionSource PositionSource { get; }

		public global::Windows.Devices.Geolocation.GeocoordinateSatelliteData? SatelliteData { get; }

		public global::System.DateTimeOffset? PositionSourceTimestamp { get; }
	}
}
