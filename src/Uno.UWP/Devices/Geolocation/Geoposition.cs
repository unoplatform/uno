#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Geolocation
{
	public partial class Geoposition
	{
		public Geoposition(Geocoordinate coordinate, CivicAddress? civicAddress = null, VenueData? venueData = null)
		{
			CivicAddress = civicAddress;
			Coordinate = coordinate;
			VenueData = venueData;
		}

		public CivicAddress? CivicAddress { get; }

		public Geocoordinate Coordinate { get; }

		public VenueData? VenueData { get; }
	}
}
