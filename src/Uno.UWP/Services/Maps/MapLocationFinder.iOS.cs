using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CoreLocation;
using Uno.UI;
using Windows.Devices.Geolocation;

namespace Windows.Services.Maps
{
	public static partial class MapLocationFinder
	{
		private static readonly CLGeocoder _geocoder;

		static MapLocationFinder()
		{
			_geocoder = new CLGeocoder();
		}

		public static async Task<MapLocationFinderResult> FindLocationsAtAsync(CancellationToken ct, Geopoint queryPoint)
		{
			var locations = await _geocoder.ReverseGeocodeLocationAsync(new CLLocation(queryPoint.Position.Latitude, queryPoint.Position.Longitude));

			var mapLocations = locations.Select(loc =>
			{
				var point = new Geopoint(
					new BasicGeoposition
					{
						Latitude = loc.Location!.Coordinate.Latitude,
						Longitude = loc.Location.Coordinate.Longitude,
						Altitude = loc.Location.Altitude
					}
				);

				var address = new MapAddress(
					buildingFloor: string.Empty, // not supported
					buildingRoom: string.Empty, // not supported
					buildingWing: string.Empty, // not supported
					buildingName: string.Empty, // not supported
					formattedAddress: string.Empty, // TODO
					continent: null, // not supported
					country: loc.Country,
					countryCode: loc.IsoCountryCode,
					district: loc.SubAdministrativeArea, // TODO: Verify
					neighborhood: loc.SubLocality, // TODO: Verify
					postCode: loc.PostalCode,
					region: loc.AdministrativeArea, // TODO: Verify
					regionCode: null, // TODO: Verify
					street: loc.Thoroughfare, // TODO: Verify
					streetNumber: loc.SubThoroughfare,
					town: loc.Locality
				);

				return new MapLocation(point, address);
			})
			.ToList()
			.AsReadOnly();

			var status = MapLocationFinderStatus.Success; // TODO

			return new MapLocationFinderResult(
				locations: mapLocations,
				status: status
			);
		}
	}
}
