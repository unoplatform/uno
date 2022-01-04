#if __ANDROID__
#pragma warning disable 67

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Android.Locations;
using Android.OS;
using Android.Runtime;
using Uno.Extensions;
using Windows.ApplicationModel.Core;
using Windows.Extensions;
using Windows.UI.Core;

namespace Windows.Devices.Geolocation
{
	public sealed partial class Geolocator : Java.Lang.Object, ILocationListener
	{
		// Only locations not older than this const can be used as current
		// (used only in GetGeopositionAsync with parameters, parameterless call can return older location)
		private const int MaxLocationAgeInSeconds = 60;

		// Using ConcurrentDictionary as concurrent HashSet (https://stackoverflow.com/questions/18922985/concurrent-hashsett-in-net-framework), byte is throwaway.
		private static readonly ConcurrentDictionary<Geolocator, byte> _positionChangedSubscriptions = new ConcurrentDictionary<Geolocator, byte>();

		private readonly Criteria _locationCriteria = new() { HorizontalAccuracy = Accuracy.Medium };

		private LocationManager _locationManager;
		private string _locationProvider;

		private double _movementThreshold = 0;
		private uint _reportInterval = 1000;

		static bool _locChanged = false;
		static Location _location;

		partial void PlatformDestruct()
		{
			RemoveUpdates();
			_locationManager?.Dispose();
		}

		/// <summary>
		/// The distance of movement, in meters, relative to the coordinate from the last PositionChanged event,
		/// that is required for the Geolocator to raise a PositionChanged event. The default value is 0.
		/// </summary>
		public double MovementThreshold
		{
			get => _movementThreshold;
			set
			{
				_movementThreshold = value;
				RestartUpdates();
			}
		}

		/// <summary>
		/// The requested minimum time interval between location updates, in milliseconds.
		/// If your application requires updates infrequently, set this value so that
		/// location services can conserve power by calculating location only when needed.
		/// The default value is 1000.
		/// </summary>
		public uint ReportInterval
		{
			get => _reportInterval;
			set
			{
				_reportInterval = value;
				RestartUpdates();
			}
		}

		/// <summary>
		/// Requests permission to access location data.
		/// </summary>
		/// <returns>A GeolocationAccessStatus that indicates if permission to location data has been granted.</returns>
		public static async Task<GeolocationAccessStatus> RequestAccessAsync()
		{
			var status = GeolocationAccessStatus.Allowed;

			if (!await PermissionsHelper.CheckFineLocationPermission(CancellationToken.None))
			{
				status = await PermissionsHelper.TryGetFineLocationPermission(CancellationToken.None)
					? GeolocationAccessStatus.Allowed
					: GeolocationAccessStatus.Denied;

				BroadcastStatusChanged(PositionStatus.Initializing);
				if (status == GeolocationAccessStatus.Allowed)
				{
					BroadcastStatusChanged(PositionStatus.Ready);

					// If geolocators subscribed to PositionChanged before the location permission was granted,
					// make sure to initialize these geolocators now so they can start broadcasting.
					foreach (var subscriber in _positionChangedSubscriptions)
					{
						subscriber.Key.TryInitialize();
					}
				}
				else
				{
					BroadcastStatusChanged(PositionStatus.Disabled);

					foreach (var subscriber in _positionChangedSubscriptions)
					{
						subscriber.Key.WaitForPermissionFromBackground();
					}
				}
			}

			return status;
		}

		/// <summary>
		/// Starts an asynchronous operation to retrieve the current location of the device.
		/// </summary>
		/// <returns>An asynchronous operation that, upon completion, returns a Geoposition marking the found location.</returns>
		public Task<Geoposition> GetGeopositionAsync()
		{
			if (CoreDispatcher.Main.HasThreadAccess)
			{
				TryInitialize();

				BroadcastStatusChanged(PositionStatus.Initializing);
				var location = _locationManager.GetLastKnownLocation(_locationProvider);
				BroadcastStatusChanged(PositionStatus.Ready);
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

		/// <summary>
		/// Starts an asynchronous operation to retrieve the current location of the device.
		/// </summary>
		/// <param name="maximumAge">The maximum acceptable age of cached location data. A TimeSpan is a time period expressed in 100-nanosecond units.</param>
		/// <param name="timeout">The timeout. A TimeSpan is a time period expressed in 100-nanosecond units.</param>
		/// <returns>An asynchronous operation that, upon completion, returns a Geoposition marking the found location.</returns>
		/// <exception cref="TimeoutException">Thrown when the request times out.</exception>
		public async Task<Geoposition> GetGeopositionAsync(TimeSpan maximumAge, TimeSpan timeout)
		{
			_locationManager = (LocationManager)Android.App.Application.Context.GetSystemService(Android.Content.Context.LocationService);

			_reportInterval = 1000;
			_movementThreshold = 0;

			RequestUpdates();

			BroadcastStatusChanged(PositionStatus.Initializing);

			Location bestLocation = TryGetCachedGeoposition(maximumAge);
			if (bestLocation != null)
			{
				RemoveUpdates();
				BroadcastStatusChanged(PositionStatus.Ready);
				return bestLocation.ToGeoPosition();
			}

			// wait for fix
			if (await TryWaitForGetGeopositionAsync(timeout))
			{
				// success
				RemoveUpdates();
				BroadcastStatusChanged(PositionStatus.Ready);
				return _location.ToGeoPosition();
			}

			// timeout
			BroadcastStatusChanged(PositionStatus.Disabled);
			RemoveUpdates();
			throw new TimeoutException("Timeout in GetGeopositionAsync(TimeSpan,TimeSpan)");
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

		internal void WaitForPermissionFromBackground()
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
			var locationManager = (LocationManager)Android.App.Application.Context.GetSystemService(Android.Content.Context.LocationService);

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

		partial void StartPositionChanged() => _positionChangedSubscriptions.TryAdd(this, 0);

		partial void StopPositionChanged() => _positionChangedSubscriptions.TryRemove(this, out var _);

		private async Task<bool> TryWaitForGetGeopositionAsync(TimeSpan timeout)
		{
			var stopwatch = Stopwatch.StartNew();

			while (stopwatch.Elapsed < timeout)
			{
				await Task.Delay(250);
				if (_locChanged)
				{
					stopwatch.Stop();
					return true;
				}
			}
			stopwatch.Stop();
			return false;
		}

		private Location TryGetCachedGeoposition(TimeSpan maximumAge)
		{
			var providers = _locationManager.GetProviders(_locationCriteria, true);
			int bestAccuracy = 10000;
			Location bestLocation = null;

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
						{
							bestLocation = location;
						}
					}
				}
			}

			if (bestLocation != null)
			{
				return bestLocation;
			}

			return null;
		}

		private void RestartUpdates()
		{
			RemoveUpdates();
			RequestUpdates();
		}

		private void RemoveUpdates()
		{
			if (this is null)
			{
				// when caled from destructor
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

			var providers = _locationManager.GetProviders(_locationCriteria, true);

			foreach (var provider in providers)
			{
				_locationManager?.RequestLocationUpdates(provider, _reportInterval, (float)_movementThreshold, this, Looper.MainLooper);
			}
		}

		partial void OnDesiredAccuracyInMetersChanged()
		{
			// Reset request for updates from Android - with new desired accuracy
			RestartUpdates();
		}

		public void OnLocationChanged(Location location)
		{
			DateTimeOffset date = DateTimeOffset.FromUnixTimeMilliseconds(location.Time);
			if (date.AddSeconds(MaxLocationAgeInSeconds) > DateTimeOffset.UtcNow)
			{// only from last minute (we don't want to get some obsolete location)
				_locChanged = true;
				_location = location;

				BroadcastStatusChanged(PositionStatus.Ready);
				_positionChangedWrapper.Event?.Invoke(this, new PositionChangedEventArgs(location.ToGeoPosition()));
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
	}

	static class Extensions
	{
		private const uint Wgs84SpatialReferenceId = 4326;

		public static Geoposition ToGeoPosition(this Location location)
		{
			double? geoheading = null;
			if (location.HasBearing)
			{
				geoheading = location.Bearing;
			}

			PositionSource posSource;
			switch (location.Provider)
			{
				case LocationManager.NetworkProvider:
					posSource = PositionSource.Cellular;    // cell, wifi
					break;
				case LocationManager.PassiveProvider:
					posSource = PositionSource.Unknown;  // other apps
					break;
				case LocationManager.GpsProvider:
					posSource = PositionSource.Satellite;
					break;
				default:
					// ex.: "fused" - all merged, also e.g. Google Play
					posSource = PositionSource.Unknown;
					break;
			}

			double? locVertAccuracy = null;
			// VerticalAccuracy is since API 26
			if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
			{
				if (location.HasVerticalAccuracy)
				{
					locVertAccuracy = location.VerticalAccuracyMeters;
				}
			}

			return new Geoposition(
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
