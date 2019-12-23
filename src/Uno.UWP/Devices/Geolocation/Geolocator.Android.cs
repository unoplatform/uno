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
        private static LocationManager _locationManager;

        private uint _reportInterval = 1000; // pkar
        private double _movementThreshold = 0;// pkar
        private uint? _desiredAccuracyInMeters;
        private Criteria _locationCriteria = new Android.Locations.Criteria() {HorizontalAccuracy = Accuracy.Medium };

        private void RemoveUpdates() =>  _locationManager?.RemoveUpdates(mojSluchacz);

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

            _locationManager.RequestLocationUpdates(_reportInterval, (float)_movementThreshold, _locationCriteria, mojSluchacz, Looper.MainLooper);
        }
        
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

        #region MyListener

        public class MyLocListen : Java.Lang.Object, ILocationListener
        {

            public void OnLocationChanged(Location location)
            {
                DateTime date = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                date = date.AddMilliseconds(location.Time);
                if (date.AddMinutes(1) > DateTime.UtcNow)
                { // only from last minute (we don't want to get some obsolete location)
                    mbZmiany = true;
                    moLocat = location;
                }
            }

            public void OnProviderDisabled(string provider)
            {
                throw new global:System.OperationCanceledException();
            }

            public void OnProviderEnabled(string provider)
            {
                //throw new NotImplementedException();
            }

            public void OnStatusChanged(string provider, Availability status, Bundle extras)
            {
                // This method was deprecated in API level 29 (Android 10). This callback will never be invoked.
            }
        }


        MyLocListen mojSluchacz = new MyLocListen();
        #endregion 

        public MyGeolocator()
        {
            _locationManager = (LocationManager)Android.App.Application.Context.GetSystemService(Android.Content.Context.LocationService);

            _reportInterval = 1000;
            _movementThreshold = 0;

            RequestUpdates();
        }

        ~MyGeolocator()
        {
            try
            {
                RemoveUpdates();
            }
            catch
            { }

            try
            {
                _locationManager.Dispose();
            }
            catch
            { }
        }

        public void Destruktor()
        { // this is my own "destructor", called when I want - ~MyGeolocator() is called long time after it should
            try
            {
                RemoveUpdates();
            }
            catch
            { }

            try
            {
                //_locationManager.Dispose();
            }
            catch
            { }

        }

        public Task<Windows.Devices.Geolocation.Geoposition> GetGeopositionAsync()
            => GetGeopositionAsync(TimeSpan.FromHours(2), TimeSpan.FromSeconds(60));


        public async Task<Windows.Devices.Geolocation.Geoposition> GetGeopositionAsync(TimeSpan maximumAge, TimeSpan timeout)
        {
            var providers = _locationManager.GetProviders(_locationCriteria,true);
            int bestAccuracy = 10000;
            Location bestLocation = null;
			
			BroadcastStatus(PositionStatus.Initializing);

            foreach (string locationProvider in providers)
            {
                var location = _locationManager.GetLastKnownLocation(locationProvider);
                if (location != null)
                {
                    // check how old is this fix
                    DateTime date = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                    date = date.AddMilliseconds(location.Time);
                    if (date.AddMilliseconds(maximumAge.TotalMilliseconds) > DateTime.UtcNow)
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
                if (mbZmiany)
                {
                    RemoveUpdates();
					BroadcastStatus(PositionStatus.Ready);
                    return moLocat.ToGeoPosition();
                }
            }

            BroadcastStatus(PositionStatus.Disabled);
            RemoveUpdates();
            throw new TimeoutException();

        }

        public static async Task<Windows.Devices.Geolocation.GeolocationAccessStatus> RequestAccessAsync()
        {

            // below 6.0 (API 23), permission are granted
            if (Android.OS.Build.VERSION.SdkInt < Android.OS.BuildVersionCodes.M)
            {
                return Windows.Devices.Geolocation.GeolocationAccessStatus.Allowed;
            }

            // do we have declared this permission in Manifest?
            // it could be also Coarse, without GPS
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
                return Windows.Devices.Geolocation.GeolocationAccessStatus.Denied;

            // check if permission is granted
            if (Android.Support.V4.Content.ContextCompat.CheckSelfPermission(Uno.UI.ContextHelper.Current, Android.Manifest.Permission.AccessFineLocation)
                    == Android.Content.PM.Permission.Granted)
            {
                return Windows.Devices.Geolocation.GeolocationAccessStatus.Allowed;
            }

            // system dialog asking for permission
			
			// this code would not compile here - but it compile in your own app.
			// to be compiled inside Uno, it has to be splitted into layers
            var tcs = new TaskCompletionSource<Uno.UI.BaseActivity.RequestPermissionsResultWithResultsEventArgs>();

            void handler(object sender, Uno.UI.BaseActivity.RequestPermissionsResultWithResultsEventArgs e)
            {

                if (e.RequestCode == 1)
                {
                    tcs.TrySetResult(e);
                }
            }

            var current = Uno.UI.BaseActivity.Current;

            try
            {
                current.RequestPermissionsResultWithResults += handler;

                ActivityCompat.RequestPermissions(Uno.UI.BaseActivity.Current, new[] { Android.Manifest.Permission.AccessFineLocation }, 1);

                var result = await tcs.Task;
                if(result.GrantResults.Length < 1)
                    return Windows.Devices.Geolocation.GeolocationAccessStatus.Denied;
                if (result.GrantResults[0] == Permission.Granted)
                    return Windows.Devices.Geolocation.GeolocationAccessStatus.Allowed;

            }
            finally
            {
                current.RequestPermissionsResultWithResults -= handler;
            }
        

            return Windows.Devices.Geolocation.GeolocationAccessStatus.Denied;
            
        }


        static bool mbZmiany = false;
        static Location moLocat;

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

            Windows.Devices.Geolocation.BasicGeoposition basicGeoposition; 
            basicGeoposition.Altitude = location.Altitude;
            basicGeoposition.Latitude = location.Latitude;
            basicGeoposition.Longitude = location.Longitude;

            Windows.Devices.Geolocation.Geopoint geopoint = new Windows.Devices.Geolocation.Geopoint(basicGeoposition,
                        Windows.Devices.Geolocation.AltitudeReferenceSystem.Ellipsoid,
                        Wgs84SpatialReferenceId
                    );

            double? locVertAccuracy = null;
            // VerticalAccuracy is since API 26
            if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.O)
            {
                if (location.HasVerticalAccuracy)
                {
                    locVertAccuracy = location.VerticalAccuracyMeters;
                }
            }


            Windows.Devices.Geolocation.Geoposition geopos = new Windows.Devices.Geolocation.Geoposition(
                new Windows.Devices.Geolocation.Geocoordinate(
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



            // this code doesn't set:
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
