#pragma warning disable 67
#nullable enable

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Android.Locations;
using Android.OS;
using Android.Runtime;
using Uno.Disposables;
using Uno.Extensions;
using Windows.ApplicationModel.Core;
using Windows.Extensions;
using Windows.Foundation;
using Windows.UI.Core;

namespace Windows.Devices.Geolocation;

public sealed partial class Geolocator
{
	private sealed class LocationListener : Java.Lang.Object, ILocationListener
	{
		private readonly Geolocator _owner;

		public LocationListener(Geolocator owner)
		{
			_owner = owner;
		}

		public void OnLocationChanged(Location location)
		{
			DateTimeOffset date = DateTimeOffset.FromUnixTimeMilliseconds(location.Time);
			if (date.AddSeconds(MaxLocationAgeInSeconds) > DateTimeOffset.UtcNow)
			{// only from last minute (we don't want to get some obsolete location)
				_locationChanged = true;
				_location = location;

				BroadcastStatusChanged(PositionStatus.Ready);
				_owner._positionChangedWrapper.Event?.Invoke(_owner, new PositionChangedEventArgs(location.ToGeoPosition()));
			}
		}

		public void OnProviderDisabled(string provider)
		{
		}

		public void OnProviderEnabled(string provider)
		{
		}

		public void OnStatusChanged(string? provider, [GeneratedEnum] Availability status, Bundle? extras)
		{
			// This method was deprecated in API level 29 (Android 10). This callback will never be invoked.
		}
	}

	private LocationListener _locationListener;

	// Only locations not older than this const can be used as current
	// (used only in GetGeopositionAsync with parameters, parameterless call can return older location)
	private const int MaxLocationAgeInSeconds = 60;

	// Using ConcurrentDictionary as concurrent HashSet (https://stackoverflow.com/questions/18922985/concurrent-hashsett-in-net-framework), byte is throwaway.
	private static readonly ConcurrentDictionary<Geolocator, byte> _positionChangedSubscriptions = new ConcurrentDictionary<Geolocator, byte>();

	private readonly Criteria _locationCriteria = new() { HorizontalAccuracy = Accuracy.Medium };

	private static bool _locationChanged;
	private static Location? _location;

	private LocationManager? _locationManager;
	private string? _locationProvider;

	private readonly SerialDisposable _resumingSubscription = new SerialDisposable();

	private double _movementThreshold;
	private uint _reportInterval = 1000;

	partial void PlatformInitialize()
	{
		_locationListener = new(this);
	}

	partial void PlatformDestruct()
	{
		RemoveUpdates();
		_resumingSubscription.Disposable = null;
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
	public static IAsyncOperation<GeolocationAccessStatus> RequestAccessAsync()
	{
		return RequestAccessCore().AsAsyncOperation();
	}

	private static async Task<GeolocationAccessStatus> RequestAccessCore()
	{
		if (!IsLocationEnabled())
		{
			return GeolocationAccessStatus.Denied;
		}

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

	public static bool IsLocationEnabled()
	{
		var locationManager = (LocationManager?)Android.App.Application.Context.GetSystemService(Android.Content.Context.LocationService);

		return locationManager?.IsLocationEnabled ?? false;
	}

	/// <summary>
	/// Starts an asynchronous operation to retrieve the current location of the device.
	/// </summary>
	/// <returns>An asynchronous operation that, upon completion, returns a Geoposition marking the found location.</returns>
	public IAsyncOperation<Geoposition?> GetGeopositionAsync()
	{
		return GetGeopositionCore().AsAsyncOperation();
	}

	private Task<Geoposition?> GetGeopositionCore()
	{
		// on UWP, "This method times out after 60 seconds, except when in Connected Standby. During Connected Standby, Geolocator objects can be instantiated but the Geolocator object will not find any sensors to aggregate and calls to GetGeopositionAsync will time out after 7 seconds."
		// so we have discrepancy here.
		if (CoreDispatcher.Main.HasThreadAccess)
		{
			TryInitialize();

			if (string.IsNullOrWhiteSpace(_locationProvider) || _locationManager is null)
			{
				return Task.FromResult<Geoposition?>(null);
			}

			BroadcastStatusChanged(PositionStatus.Initializing);
			var location = _locationManager.GetLastKnownLocation(_locationProvider);
			BroadcastStatusChanged(PositionStatus.Ready);
			return Task.FromResult(location?.ToGeoPosition());
		}
		else
		{
			return CoreDispatcher.Main.RunWithResultAsync(
				priority: CoreDispatcherPriority.Normal,
				task: () => GetGeopositionCore()
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
	public IAsyncOperation<Geoposition?> GetGeopositionAsync(TimeSpan maximumAge, TimeSpan timeout)
	{
		return GetGeopositionCore(maximumAge, timeout).AsAsyncOperation();
	}

	private async Task<Geoposition?> GetGeopositionCore(TimeSpan maximumAge, TimeSpan timeout)
	{
		_locationManager = (LocationManager?)Android.App.Application.Context.GetSystemService(Android.Content.Context.LocationService);

		_reportInterval = 1000;
		_movementThreshold = 0;

		RestartUpdates();

		BroadcastStatusChanged(PositionStatus.Initializing);

		var bestLocation = TryGetCachedGeoposition(maximumAge);
		if (bestLocation != null)
		{
			if (_positionChangedWrapper.Event == null)
			{
				RemoveUpdates();
			}
			BroadcastStatusChanged(PositionStatus.Ready);
			return bestLocation.ToGeoPosition();
		}

		// wait for fix
		if (await TryWaitForGetGeopositionAsync(timeout, DateTime.Now - maximumAge))
		{
			// success
			if (_positionChangedWrapper.Event == null)
			{
				RemoveUpdates();
			}
			BroadcastStatusChanged(PositionStatus.Ready);
			return _location?.ToGeoPosition();
		}

		// timeout
		BroadcastStatusChanged(PositionStatus.Disabled);
		if (_positionChangedWrapper.Event == null)
		{
			RemoveUpdates();
		}
		throw new TimeoutException("Timeout in GetGeopositionAsync(TimeSpan,TimeSpan)");
	}

	private void TryInitialize()
	{
		if (_locationManager == null)
		{
			_locationManager = InitializeLocationProvider();
			if (_locationManager is null || string.IsNullOrWhiteSpace(_locationProvider))
			{
				return;
			}
			_locationManager.RequestLocationUpdates(_locationProvider, 0, 0, _locationListener);
			_resumingSubscription.Disposable = null;
		}
	}

	internal void WaitForPermissionFromBackground()
	{
		_resumingSubscription.Disposable = null;
		CoreApplication.Resuming += CoreApplication_Resuming;
		_resumingSubscription.Disposable = Disposable.Create(() => CoreApplication.Resuming -= CoreApplication_Resuming);
	}

	private void CoreApplication_Resuming(object? sender, object? e)
	{
		_ = CoreDispatcher.Main.RunAsync(
			priority: CoreDispatcherPriority.Normal,
			agileCallback: InitializeIfPermissionIsGranted
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

	private LocationManager? InitializeLocationProvider()
	{
		_locationProvider = null;
		var locationManager = (LocationManager?)Android.App.Application.Context.GetSystemService(Android.Content.Context.LocationService);

		var criteriaForLocationService = new Criteria
		{
			Accuracy = Accuracy.Coarse
		};

		if (locationManager?.GetProviders(criteriaForLocationService, true) is { } acceptableLocationProviders)
		{
			_locationProvider = acceptableLocationProviders.FirstOrDefault();
		}

		return locationManager;
	}

	partial void StartPositionChanged()
	{
		_positionChangedSubscriptions.TryAdd(this, 0);
		TryInitialize();
		RestartUpdates();
	}

	partial void StopPositionChanged()
	{
		_positionChangedSubscriptions.TryRemove(this, out _);
		RemoveUpdates();
		BroadcastStatusChanged(PositionStatus.Disabled);
	}

	private async Task<bool> TryWaitForGetGeopositionAsync(TimeSpan timeout, DateTime earliestDate)
	{
		var stopwatch = Stopwatch.StartNew();

		while (stopwatch.Elapsed < timeout)
		{
			await Task.Delay(250);
			if (_locationChanged)
			{
				// check if we get current (not obsolete) location
				if (_location is not null && _location.Time >= earliestDate.ToUnixTimeMilliseconds())
				{
					stopwatch.Stop();
					return true;
				}
			}
		}
		stopwatch.Stop();
		return false;
	}

	private Location? TryGetCachedGeoposition(TimeSpan maximumAge)
	{
		if (_locationManager is null)
		{
			return null;
		}

		var providers = _locationManager.GetProviders(_locationCriteria, true);
		int bestAccuracy = 10000;
		Location? bestLocation = null;

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

		if (_locationManager is not null
			// RemoveUpdates may be invoked from the finalizer and _locationListener may already have been collected.
			&& _locationListener.Handle != IntPtr.Zero)
		{
			_locationManager.RemoveUpdates(_locationListener);
		}
	}

	private void RequestUpdates()
	{
		if (_locationManager is null)
		{
			return;
		}

		_locationCriteria.HorizontalAccuracy = GetAndroidAccuracy(_desiredAccuracyInMeters);
		var providers = _locationManager.GetProviders(_locationCriteria, true);

		foreach (var provider in providers)
		{
			_locationManager.RequestLocationUpdates(provider, _reportInterval, (float)_movementThreshold, _locationListener, Looper.MainLooper);
		}
	}

	private Accuracy GetAndroidAccuracy(uint? desiredAccuracyInMeters) =>
		_desiredAccuracyInMeters switch
		{
			< 100 => Accuracy.High,
			< 500 => Accuracy.Medium,
			{ } => Accuracy.Low,
			_ => Accuracy.Medium
		};

	partial void OnDesiredAccuracyInMetersChanged()
	{
		// Reset request for updates from Android - with new desired accuracy
		RestartUpdates();
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
				posSource = PositionSource.Cellular; // Cell, Wi-Fi
				break;
			case LocationManager.PassiveProvider:
				posSource = PositionSource.Unknown; // Other apps
				break;
			case LocationManager.GpsProvider:
				posSource = PositionSource.Satellite;
				break;
			default:
				// Ex.: "fused" - all merged, also e.g. Google Play
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
