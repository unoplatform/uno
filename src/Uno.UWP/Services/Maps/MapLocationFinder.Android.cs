using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Android.Content;
using Android.Locations;
using Java.Util;
using Uno.Extensions;
using Uno.UI;
using Windows.Devices.Geolocation;

namespace Windows.Services.Maps
{
	public static partial class MapLocationFinder
	{
		private const int MaxResults = int.MaxValue;
		private static readonly Geocoder _geocoder;

		static MapLocationFinder()
		{
			var context = ContextHelper.Current; // TODO: Inject Context instance?

			//Get the locale directly instead of using ApplicationLanguages.Language.ToLanguageTag() and converting to Locale.
			//ToLanguageTag() causes a bug in Xamarin, fixed in Xamarin 4. NoSuchMethodError Java exception is thrown
#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable CA1422 // Validate platform compatibility
			var locale = context.Resources!.Configuration!.Locale ?? Locale.Default;
#pragma warning restore CS0618 // Type or member is obsolete
#pragma warning restore CA1422 // Validate platform compatibility

			_geocoder = new Geocoder(context, locale);
		}

		public static async Task<MapLocationFinderResult> FindLocationsAtAsync(CancellationToken ct, Geopoint queryPoint)
		{
			if (!Geocoder.IsPresent)
			{
				return new MapLocationFinderResult(
					locations: new List<MapLocation>().AsReadOnly(),
					status: MapLocationFinderStatus.UnknownError
				);
			}

			var addresses = await _geocoder.GetFromLocationAsync(
				queryPoint.Position.Latitude,
				queryPoint.Position.Longitude,
				maxResults: MaxResults
			);

			var locations = addresses!.Select(loc =>
			{
				var point = new Geopoint(new BasicGeoposition
				{
					Latitude = loc.Latitude,
					Longitude = loc.Longitude
				});

				// Known differences:
				// ==================
				// - Android seems to provide 2 types of address:
				//      - Precise addresses with street, streetNumber and postCode (no district)
				//      - Less precise addresses with district (no street, streetNumber or postCode)
				// - Precise addresses usually come first in the list
				// - It might be possible to merge a precise address with a less precise one in order to get the district information (which Windows does provide)
				// - Other known differences are listed below
				var address = new MapAddress(
					buildingFloor: string.Empty, // not supported
					buildingName: string.Empty, // not supported
					buildingRoom: string.Empty, // not supported
					buildingWing: string.Empty, // not supported
					formattedAddress: string.Join(", ", Enumerable.Range(0, loc.MaxAddressLineIndex).Select(loc.GetAddressLine)), // differs from Windows
					continent: string.Empty, // not supported
					country: loc.CountryName!,
					countryCode: loc.CountryCode!, // differs from Windows (i.e., "CA" instead of "CAN")
					district: loc.SubLocality!, // seems to be null if a specific address is found (streetNumber, street and postCode)
					neighborhood: loc.SubLocality!, // haven't seen a non-null value yet (on both Windows and Android)
					postCode: loc.PostalCode!,
					region: loc.AdminArea!,
					regionCode: string.Empty, // haven't seen a non-null value yet (on both Windows and Android)
					street: loc.Thoroughfare!,
					streetNumber: loc.SubThoroughfare!, // usually is a range (i.e., "706-212" instead of "706")
					town: loc.Locality!
				);

				return new MapLocation(point, address);
			})
			.ToList()
			.AsReadOnly();

			var status = MapLocationFinderStatus.Success; // TODO

			return new MapLocationFinderResult(locations, status);
		}
	}
}
