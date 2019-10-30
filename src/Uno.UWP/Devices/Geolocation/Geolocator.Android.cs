#if __ANDROID__
#pragma warning disable 67
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Android.Locations;
using Android.OS;
using Android.Runtime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Foundation.Metadata;
namespace Windows.Devices.Geolocation
{
	public sealed partial class Geolocator : Java.Lang.Object, ILocationListener
	{
		private LocationManager _locationManager;
		private string _locationProvider;

		public Geolocator()
		{
			_locationManager = InitializeLocationProvider(1);

			_locationManager.RequestLocationUpdates(_locationProvider, 0, 0, this);
		}

		public Task<Geoposition> GetGeopositionAsync()
		{
			BroadcastStatus(PositionStatus.Initializing);
			var location = _locationManager.GetLastKnownLocation(_locationProvider);
			BroadcastStatus(PositionStatus.Ready);
			return Task.FromResult(location.ToGeoPosition());
		}

		public Task<Geoposition> GetGeopositionAsync(TimeSpan maximumAge, TimeSpan timeout)
			=> GetGeopositionAsync();

		public static async Task<GeolocationAccessStatus> RequestAccessAsync() => GeolocationAccessStatus.Allowed;

		private LocationManager InitializeLocationProvider(double desiredAccuracy)
		{
			var locationManager = (LocationManager)global::Android.App.Application.Context.GetSystemService(Android.Content.Context.LocationService);

			var criteriaForLocationService = new Criteria
			{
				Accuracy = Accuracy.Coarse
			};

			var acceptableLocationProviders = locationManager.GetProviders(criteriaForLocationService, true);

			if (acceptableLocationProviders.Any())
			{
				_locationProvider = acceptableLocationProviders.First();
			}
			else
			{
				_locationProvider = String.Empty;
			}

			return locationManager;
		}

		partial void StartPositionChanged()
		{
			BroadcastStatus(PositionStatus.Initializing);
		}

		public void OnLocationChanged(Location location)
		{
			BroadcastStatus(PositionStatus.Ready);
			this._positionChanged?.Invoke(this, new PositionChangedEventArgs(location.ToGeoPosition()));
		}

		public void OnProviderDisabled(string provider)
		{
		}

		public void OnProviderEnabled(string provider)
		{
		}

		public void OnStatusChanged(string provider, [GeneratedEnum] Availability status, Bundle extras)
		{
		}		
	}

	static class Extensions
	{
		private const uint Wgs84SpatialReferenceId = 4326;

		public static Geoposition ToGeoPosition(this Location location)
			=> new Geoposition(
				new Geocoordinate(
					latitude: location.Latitude,
					longitude: location.Longitude,
					altitude: location.Altitude,
					timestamp: FromUnixTime(location.Time),
					speed: location.HasSpeed ? location.Speed : 0,
					point: new Geopoint(
						new BasicGeoposition
						{
							Latitude = location.Latitude,
							Longitude = location.Longitude,
							Altitude = location.Altitude,
						},
						AltitudeReferenceSystem.Ellipsoid,
						Wgs84SpatialReferenceId
					),
					accuracy: 0,
					altitudeAccuracy: 0,
					heading: null
				)
			);

		private static DateTimeOffset FromUnixTime(long time)
		{
			var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
			return epoch.AddMilliseconds(time);
		}
	}
}
#endif
