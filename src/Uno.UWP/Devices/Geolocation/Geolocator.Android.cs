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
using System.Threading;

using Android.Content.PM;
using Android.Support.V4.App;
using Android.Support.V4.Content;
using Uno.Extensions;


namespace Windows.Devices.Geolocation
{
	public sealed partial class Geolocator : Java.Lang.Object, ILocationListener
	{
		private LocationManager _locationManager;
		private string _locationProvider;

		private uint _reportInterval = 1000; 
		private double _movementThreshold = 0;
		public uint ReportInterval
		{ 
			get => _reportInterval;
			set
			{
				_reportInterval = value;
				_locationManager.RemoveUpdates(this);
				_locationManager.RequestLocationUpdates(_locationProvider, _reportInterval, (float)_movementThreshold, this);
			}
		}

		public double MovementThreshold
		{ 
			get => _movementThreshold;
			set
			{
				_movementThreshold = value;
				_locationManager.RemoveUpdates(this);
				_locationManager.RequestLocationUpdates(_locationProvider, _reportInterval, (float)_movementThreshold, this);
			}
		}


		public Geolocator()
		{
			_locationManager = InitializeLocationProvider(1);
			_locationManager.RequestLocationUpdates(_locationProvider, _reportInterval, (float)_movementThreshold, this);
		}

		~Geolocator()
		{
			_locationManager.RemoveUpdates(this);
			try
			{
				_locationManager.Dispose();
			}
			catch
			{ }
		}


		public Task<Geoposition> GetGeopositionAsync()
			=> GetGeopositionAsync(TimeSpan.FromHours(2), TimeSpan.FromSeconds(60));

		public async Task<Geoposition> GetGeopositionAsync(TimeSpan maximumAge, TimeSpan timeout)
		{
			
			BroadcastStatus(PositionStatus.Initializing);
			var location = _locationManager.GetLastKnownLocation(_locationProvider);
			if (location != null)
			{
				// check how old is this fix
				DateTime date = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
				date.AddMilliseconds(location.Time);
				if (date.AddMilliseconds(maximumAge.TotalMilliseconds) > DateTime.UtcNow)
				{
					BroadcastStatus(PositionStatus.Ready);
					return location.ToGeoPosition();
				}
			}

			// if there is no fix, wait for it...
			for (int i = (int)(timeout.TotalMilliseconds / 250.0); i > 0; i--)
			{
				await Task.Delay(250);	// but it seems that it doesn't wait?
				location = _locationManager.GetLastKnownLocation(_locationProvider);
				if (location != null)
				{
					// check how old is this fix - should not be required?
					DateTime date = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
					date.AddMilliseconds(location.Time);
					if (date.AddMilliseconds(maximumAge.TotalMilliseconds) > DateTime.UtcNow)
					{
						BroadcastStatus(PositionStatus.Ready);
						return location.ToGeoPosition();
					}
				}
			}

			BroadcastStatus(PositionStatus.Disabled);
			throw new TimeoutException();

		}

		public static async Task<GeolocationAccessStatus> RequestAccessAsync()
		{
			// below 6.0 (API 23), permission are granted
			if (Android.OS.Build.VERSION.SdkInt < Android.OS.BuildVersionCodes.M)
			{
				return GeolocationAccessStatus.Allowed;
			}

            // do we have declared this permission in Manifest?
            Android.Content.Context context = Android.App.Application.Context;
            Android.Content.PM.PackageInfo packageInfo = 
                context.PackageManager.GetPackageInfo(context.PackageName, PackageInfoFlags.Permissions);
            var requestedPermissions = packageInfo?.RequestedPermissions;
            if(requestedPermissions is null)
                return Windows.Devices.Geolocation.GeolocationAccessStatus.Denied;
            
            bool bInManifest = false;
            foreach (string oPerm in requestedPermissions)
            {
                if (oPerm.Equals(Android.Manifest.Permission.AccessFineLocation, StringComparison.OrdinalIgnoreCase))
                    bInManifest = true;
            }

            if(!bInManifest)
				// return Denied, but maybe we should throw exception?
                return Windows.Devices.Geolocation.GeolocationAccessStatus.Denied;


			// check if permission is granted
			if (Android.Support.V4.Content.ContextCompat.CheckSelfPermission(Uno.UI.ContextHelper.Current, Android.Manifest.Permission.AccessFineLocation)
					== Android.Content.PM.Permission.Granted)
			{
				return GeolocationAccessStatus.Allowed;
			}


			// this method doesnt work!
			// CancellationTokenSource cts = new CancellationTokenSource();
			//bool granted = await Windows.Extensions.PermissionsHelper.CheckFineLocationPermission(cts.Token);
			//cts.Dispose();
			//if (granted)
			//	return GeolocationAccessStatus.Allowed;

			// check these files:
			// src\Uno.UI\Extensions\PermissionsHelper.Android.cs
			// src\Uno.UWP\Extensions\PermissionsHelper.cs

			return GeolocationAccessStatus.Denied;

		}

		private LocationManager InitializeLocationProvider(double desiredAccuracy)
		{
			var locationManager = (LocationManager)global::Android.App.Application.Context.GetSystemService(Android.Content.Context.LocationService);

			var criteriaForLocationService = new Criteria
			{
				Accuracy = Accuracy.Fine 
			};

			_locationProvider = locationManager.GetBestProvider(criteriaForLocationService, true);

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
		{
			double? geoheading = null;
			if (location.HasBearing)
			{
				geoheading = location.Bearing;
			}

			// pkar
			PositionSource posSource = PositionSource.Unknown ;
			switch(location.Provider)
			{
				case Android.Locations.LocationManager.NetworkProvider:
					posSource = PositionSource.Cellular;	// cell, wifi
					break;
				case Android.Locations.LocationManager.PassiveProvider:
					posSource = PositionSource.Unknown;  // inni
					break;
				case Android.Locations.LocationManager.GpsProvider:
					posSource = PositionSource.Satellite ;
					break;
			}

			BasicGeoposition basicGeoposition; 
			basicGeoposition.Altitude = location.Altitude;
			basicGeoposition.Latitude = location.Latitude;
			basicGeoposition.Longitude = location.Longitude;

			Geopoint geopoint = new Geopoint(basicGeoposition,
						AltitudeReferenceSystem.Ellipsoid,
						Wgs84SpatialReferenceId
					);

			double? locVertAccuracy=null;
			// HasVerticalAccuracy and VerticalAccuracyMeters are marked as "to be added", but where? Mono? Android SDK?
			// if(location.HasVerticalAccuracy )
			// {
			// 	locVertAccuracy = location.VerticalAccuracyMeters;
			// }

			Geoposition geopos = new Geoposition(
				new Geocoordinate(
					latitude: location.Latitude,
					longitude: location.Longitude,
					altitude: location.Altitude,
					timestamp: FromUnixTime(location.Time),
					speed: location.HasSpeed ? location.Speed : 0,
					point: geopoint,
					accuracy: location.HasAccuracy ? location.Accuracy : 0,
					altitudeAccuracy: locVertAccuracy,
					heading: geoheading,
					positionSource: posSource
				)
			);



			// it doesn't set these fields:
			// heading (double?) ,
			// position source, [enum]
			// position sourcetimestamp (DateTimeOffset?),
			//satelitedata


			return geopos;
		}
			

		private static DateTimeOffset FromUnixTime(long time)
		{
			var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
			return epoch.AddMilliseconds(time);
		}
	}
}
#endif
