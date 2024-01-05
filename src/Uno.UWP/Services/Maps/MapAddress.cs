using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.Services.Maps
{
	public sealed partial class MapAddress
	{
		internal MapAddress(
			string? buildingFloor,
			string? buildingName,
			string? buildingRoom,
			string? buildingWing,
			string? formattedAddress,
			string? continent,
			string? country,
			string? countryCode,
			string? district,
			string? neighborhood,
			string? postCode,
			string? region,
			string? regionCode,
			string? street,
			string? streetNumber,
			string? town
		)
		{
			FormattedAddress = formattedAddress ?? string.Empty;
			BuildingFloor = buildingFloor ?? string.Empty;
			BuildingName = buildingName ?? string.Empty;
			BuildingRoom = buildingRoom ?? string.Empty;
			BuildingWing = buildingWing ?? string.Empty;
			Continent = continent ?? string.Empty;
			Country = country ?? string.Empty;
			CountryCode = countryCode ?? string.Empty;
			District = district ?? string.Empty;
			Neighborhood = neighborhood ?? string.Empty;
			PostCode = postCode ?? string.Empty;
			Region = region ?? string.Empty;
			RegionCode = regionCode ?? string.Empty;
			Street = street ?? string.Empty;
			StreetNumber = streetNumber ?? string.Empty;
			Town = town ?? string.Empty;
		}

		public string FormattedAddress { get; }
		public string BuildingFloor { get; }
		public string BuildingName { get; }
		public string BuildingRoom { get; }
		public string BuildingWing { get; }
		public string Continent { get; }
		public string Country { get; }
		public string CountryCode { get; }
		public string District { get; }
		public string Neighborhood { get; }
		public string PostCode { get; }
		public string Region { get; }
		public string RegionCode { get; }
		public string Street { get; }
		public string StreetNumber { get; }
		public string Town { get; }
	}
}
