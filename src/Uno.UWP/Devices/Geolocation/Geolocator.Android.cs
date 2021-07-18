#if __ANDROID__
#pragma warning disable 67
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Android.Locations;
using Android.OS;
using Android.Runtime;
using Uno.Extensions;
using Windows.ApplicationModel.Core;
using Windows.Extensions;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Foundation.Metadata;
using Windows.UI.Core;

// empty change - forcing build restart

namespace Windows.Devices.Geolocation
{
	public sealed partial class Geolocator : Java.Lang.Object, ILocationListener
	{
		//using ConcurrentDictionary as concurrent HashSet (https://stackoverflow.com/questions/18922985/concurrent-hashsett-in-net-framework), byte is throwaway
		private static ConcurrentDictionary<Geolocator, byte> _positionChangedSubscriptions = new ConcurrentDictionary<Geolocator, byte>();

		private LocationManager _locationManager;
		private string _locationProvider;

		public Geolocator()
		{
		}

		private void TryInitialize()
		{
			if (_locationManager == null)
			{
				_locationManager = InitializeLocationProvider(1);
				_locationManager.RequestLocationUpdates(_locationProvider, 0, 0, this);
				CoreApplication.Resuming -= CoreApplication_Resuming;
			}
		}

		public Task<Geoposition> GetGeopositionAsync()
		{
			if (CoreDispatcher.Main.HasThreadAccess)
			{
				TryInitialize();

				BroadcastStatus(PositionStatus.Initializing);
				var location = _locationManager.GetLastKnownLocation(_locationProvider);
				BroadcastStatus(PositionStatus.Ready);
				return Task.FromResult(location.ToGeoPosition());
			}
			else
			{
				return CoreDispatcher.Main.RunWithResultAsync(
					priority: CoreDispatcherPriority.Normal,
					task: () => GetGeopositionAsync()
				);
			}
		}


		public static async Task<GeolocationAccessStatus> RequestAccessAsync()
		{
			var status = GeolocationAccessStatus.Allowed;

			if (!await PermissionsHelper.CheckFineLocationPermission(CancellationToken.None))
			{
				status = await PermissionsHelper.TryGetFineLocationPermission(CancellationToken.None)
					? GeolocationAccessStatus.Allowed
					: GeolocationAccessStatus.Denied;

				BroadcastStatus(PositionStatus.Initializing);
				if (status == GeolocationAccessStatus.Allowed)
				{
					BroadcastStatus(PositionStatus.Ready);

					// If geolocators subscribed to PositionChanged before the location permission was granted,
					// make sure to initialize these geolocators now so they can start broadcasting.
					foreach (var subscriber in _positionChangedSubscriptions)
					{
						subscriber.Key.TryInitialize();
					}
				}
				else
				{
					BroadcastStatus(PositionStatus.Disabled);

					foreach (var subscriber in _positionChangedSubscriptions)
					{
						subscriber.Key.WaitForPermissionFromBackground();
					}
				}
			}

			return status;
		}

		public void WaitForPermissionFromBackground()
		{
			CoreApplication.Resuming -= CoreApplication_Resuming;
			CoreApplication.Resuming += CoreApplication_Resuming;
		}

		private void CoreApplication_Resuming(object sender, object e)
		{
			CoreDispatcher.Main.RunAsync(
				priority: CoreDispatcherPriority.Normal,
				handler: InitializeIfPermissionIsGranted
			);
		}

		private async void InitializeIfPermissionIsGranted()
		{
			// If the user has granted the location permission while the app was in background, Initialize
			if (await PermissionsHelper.CheckFineLocationPermission(CancellationToken.None))
			{
				TryInitialize();
				CoreApplication.Resuming -= CoreApplication_Resuming;
			}
		}

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
			_positionChangedSubscriptions.TryAdd(this, 0);
		}

		partial void StopPositionChanged()
		{
			_positionChangedSubscriptions.TryRemove(this, out var _);
		}

		public async Task<Windows.Devices.Geolocation.Geoposition> GetGeopositionAsync(TimeSpan maximumAge, TimeSpan timeout)
		{

			_locationManager = (LocationManager)Android.App.Application.Context.GetSystemService(Android.Content.Context.LocationService);

			_reportInterval = 1000;
			_movementThreshold = 0;

			RequestUpdates();

			var providers = _locationManager.GetProviders(_locationCriteria, true);
			int bestAccuracy = 10000;
			Location bestLocation = null;

			BroadcastStatus(PositionStatus.Initializing);

			var startDate = DateTimeOffset.UtcNow;
			foreach (string locationProvider in providers)
			{
				var location = _locationManager.GetLastKnownLocation(locationProvider);
				if (location != null)
				{
					// check how old is this fix
					var date = DateTimeOffset.FromUnixTimeMilliseconds(location.Time);
					if (date + maximumAge > startDate)
					{   // can be used, but is it best accuracy?
						if (location.HasAccuracy)
						{
							if (location.Accuracy < bestAccuracy)
							{
								bestAccuracy = (int)location.Accuracy;
								bestLocation = location;
							}
						}
						else
							bestLocation = location;
					}
				}
			}

			if (bestLocation != null)
			{
				RemoveUpdates();
				BroadcastStatus(PositionStatus.Ready);
				return bestLocation.ToGeoPosition();
			}

			// wait for fix
			for (int i = (int)(timeout.TotalMilliseconds / 250.0); i > 0; i--)
			{
				await Task.Delay(250);
				if (_locChanged)
				{
					RemoveUpdates();
					BroadcastStatus(PositionStatus.Ready);
					return _location.ToGeoPosition();
				}
			}

			BroadcastStatus(PositionStatus.Disabled);
			RemoveUpdates();
			throw new TimeoutException("Timeout in GetGeopositionAsync(TimeSpan,TimeSpan)");

		}

		~Geolocator()
        {
			RemoveUpdates();
			_locationManager?.Dispose();
        }

		private void RemoveUpdates()
		{
			if (this is null)
			{ // when caled from destructor
				return;
			}
			_locationManager?.RemoveUpdates(this);
		}

		private void RequestUpdates()
		{
			if (_desiredAccuracyInMeters.HasValue)
			{
				if (_desiredAccuracyInMeters.Value < 100)
				{
					_locationCriteria.HorizontalAccuracy = Accuracy.High;
				}
				else
					if (_desiredAccuracyInMeters.Value < 500)
				{
					_locationCriteria.HorizontalAccuracy = Accuracy.Medium;
				}
				else
				{
					_locationCriteria.HorizontalAccuracy = Accuracy.Low;
				}

			}
			else
			{
				_locationCriteria.HorizontalAccuracy = Accuracy.Medium;
			}

			_locationManager?.RequestLocationUpdates(_reportInterval, (float)_movementThreshold, _locationCriteria, this, Looper.MainLooper);
		}

		private uint _reportInterval = 1000;
		private double _movementThreshold = 0;

		static bool _locChanged = false;
		static Location _location;

		private Criteria _locationCriteria = new Android.Locations.Criteria() { HorizontalAccuracy = Accuracy.Medium };


		public uint ReportInterval
		{
			get => _reportInterval;
			set
			{
				_reportInterval = value;
				RemoveUpdates();
				RequestUpdates();
			}
		}

		public double MovementThreshold
		{
			get => _movementThreshold;
			set
			{
				_movementThreshold = value;
				RemoveUpdates();
				RequestUpdates();
			}
		}

		public uint? DesiredAccuracyInMeters
		{
			get => _desiredAccuracyInMeters;
			set
			{
				_desiredAccuracyInMeters = value;
				RemoveUpdates();
				RequestUpdates();
			}
		}

#region Android ILocationListener
		public void OnLocationChanged(Location location)
		{
			DateTimeOffset date = DateTimeOffset.FromUnixTimeMilliseconds(location.Time);
			if (date.AddMinutes(1) > DateTimeOffset.UtcNow)
			{// only from last minute (we don't want to get some obsolete location)
				_locChanged = true;
				_location = location;

				BroadcastStatus(PositionStatus.Ready);
				this._positionChanged?.Invoke(this, new PositionChangedEventArgs(location.ToGeoPosition()));
			}
		}

		public void OnProviderDisabled(string provider)
		{
		}

		public void OnProviderEnabled(string provider)
		{
		}

		public void OnStatusChanged(string provider, [GeneratedEnum] Availability status, Bundle extras)
		{
			// This method was deprecated in API level 29 (Android 10). This callback will never be invoked.
		}

#endregion
	}

	static class Extensions
	{
		private const uint Wgs84SpatialReferenceId = 4326;

		public static Windows.Devices.Geolocation.Geoposition ToGeoPosition(this Location location)
		{
			double? geoheading = null;
			if (location.HasBearing)
			{
				geoheading = location.Bearing;
			}

			Windows.Devices.Geolocation.PositionSource posSource;
			switch (location.Provider)
			{
				case Android.Locations.LocationManager.NetworkProvider:
					posSource = Windows.Devices.Geolocation.PositionSource.Cellular;    // cell, wifi
					break;
				case Android.Locations.LocationManager.PassiveProvider:
					posSource = Windows.Devices.Geolocation.PositionSource.Unknown;  // other apps
					break;
				case Android.Locations.LocationManager.GpsProvider:
					posSource = Windows.Devices.Geolocation.PositionSource.Satellite;
					break;
				default:
					// ex.: "fused" - all merged, also e.g. Google Play
					posSource = Windows.Devices.Geolocation.PositionSource.Unknown;
					break;
			}


			double? locVertAccuracy = null;
			// VerticalAccuracy is since API 26
			if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.O)
			{
				if (location.HasVerticalAccuracy)
				{
					locVertAccuracy = location.VerticalAccuracyMeters;
				}
			}


			return new Windows.Devices.Geolocation.Geoposition(
				new Geocoordinate(
					latitude: location.Latitude,
					longitude: location.Longitude,
					altitude: location.Altitude,
					timestamp: DateTimeOffset.FromUnixTimeMilliseconds(location.Time),
					speed: location.HasSpeed ? location.Speed : 0,
					point: new Geopoint(
							new BasicGeoposition
							{
								Latitude = location.Latitude,
								Longitude = location.Longitude,
								Altitude = location.Altitude
							},
							AltitudeReferenceSystem.Ellipsoid,
							Wgs84SpatialReferenceId
						),

					accuracy: location.HasAccuracy ? location.Accuracy : 0,
					altitudeAccuracy: locVertAccuracy,
					heading: geoheading,
					positionSource: posSource
				)
			);

		}

	}
}
#endif
