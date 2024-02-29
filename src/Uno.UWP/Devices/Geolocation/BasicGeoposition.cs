using System;

namespace Windows.Devices.Geolocation
{
	public partial struct BasicGeoposition : IEquatable<BasicGeoposition>
	{
		public double Altitude;
		public double Latitude;
		public double Longitude;

		// NOTE: Equality implementation should be modified if a new field/property is added.

		#region Equality Members
		public override bool Equals(object? obj) => obj is BasicGeoposition geoposition && Equals(geoposition);
		public bool Equals(BasicGeoposition other) => Altitude == other.Altitude && Latitude == other.Latitude && Longitude == other.Longitude;

		public override int GetHashCode()
		{
			var hashCode = 841894690;
			hashCode = hashCode * -1521134295 + Altitude.GetHashCode();
			hashCode = hashCode * -1521134295 + Latitude.GetHashCode();
			hashCode = hashCode * -1521134295 + Longitude.GetHashCode();
			return hashCode;
		}

		public static bool operator ==(BasicGeoposition left, BasicGeoposition right) => left.Equals(right);
		public static bool operator !=(BasicGeoposition left, BasicGeoposition right) => !left.Equals(right);
		#endregion
	}
}
