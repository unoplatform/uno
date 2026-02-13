using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using System.Threading.Tasks;
using CoreLocation;
using Foundation;
using Uno.Extensions;
using Windows.UI.Core;
using Uno.UI.Dispatching;

namespace Windows.Devices.Geolocation
{
	public sealed partial class Geolocator
	{
		private CLLocationManager _locationManager;

		partial void PlatformInitialize()
		{
			if (NativeDispatcher.Main.HasThreadAccess)
			{
				_locationManager = new CLLocationManager
				{
					DesiredAccuracy = DesiredAccuracy == PositionAccuracy.Default ? 10 : 1,
				};

				_locationManager.LocationsUpdated += _locationManager_LocationsUpdated;

				_locationManager.StartUpdatingLocation();
			}
			else
			{
				NativeDispatcher.Main.Enqueue(PlatformInitialize, NativeDispatcherPriority.Normal);
			}
		}

		private void _locationManager_LocationsUpdated(object sender, CLLocationsUpdatedEventArgs e)
		{
			BroadcastStatusChanged(PositionStatus.Ready);
			_positionChangedWrapper.Event?.Invoke(this, new PositionChangedEventArgs(ToGeoposition(e.Locations.Last())));
		}

		partial void StartPositionChanged()
		{
			BroadcastStatusChanged(PositionStatus.Initializing);
		}

		internal CLLocationManager LocationManager => _locationManager;

		public IAsyncOperation<Geoposition> GetGeopositionAsync() => GetGeopositionInternalAsync().AsAsyncOperation();

		public Task<Geoposition> GetGeopositionInternalAsync()

		{
			if (CoreDispatcher.Main.HasThreadAccess)
			{
				BroadcastStatusChanged(PositionStatus.Initializing);
				var location = _locationManager.Location;
				if (location == null)
				{
					throw new InvalidOperationException("Could not obtain the location. Please make sure that NSLocationWhenInUseUsageDescription and NSLocationUsageDescription are set in info.plist.");
				}

				BroadcastStatusChanged(PositionStatus.Ready);

				return Task.FromResult(ToGeoposition(location));
			}
			else
			{
				return CoreDispatcher.Main.RunWithResultAsync<Geoposition>(
					priority: CoreDispatcherPriority.Normal,
					task: () => GetGeopositionInternalAsync()
				);
			}
		}

		private static Geoposition ToGeoposition(CLLocation location)
			=> new Geoposition(
				new Geocoordinate(
					altitude: location.Altitude,
					longitude: location.Coordinate.Longitude,
					latitude: location.Coordinate.Latitude,
					accuracy: location.HorizontalAccuracy,
					altitudeAccuracy: location.VerticalAccuracy,
					speed: location.Speed,
					point: new Geopoint(
						new BasicGeoposition
						{
							Altitude = location.Altitude,
							Latitude = location.Coordinate.Latitude,
							Longitude = location.Coordinate.Longitude
						}
					),
					timestamp: (DateTime)location.Timestamp
				)
			);

		public IAsyncOperation<Geoposition> GetGeopositionAsync(TimeSpan maximumAge, TimeSpan timeout)
			=> GetGeopositionAsync();


		private static List<CLLocationManager> _requestManagers = new List<CLLocationManager>();

		public static IAsyncOperation<GeolocationAccessStatus> RequestAccessAsync() => RequestAccessInternalAsync().AsAsyncOperation();

		private static async Task<GeolocationAccessStatus> RequestAccessInternalAsync()

		{
			if (CoreDispatcher.Main.HasThreadAccess)
			{
				var mgr = new CLLocationManager();

				lock (_requestManagers)
				{
					_requestManagers.Add(mgr);
				}

				try
				{
					var accessStatus = default(GeolocationAccessStatus);
					var tsc = new TaskCompletionSource<CLAuthorizationStatus>();

#if __IOS__ || __TVOS__
					// Workaround for a bug in Xamarin.iOS https://github.com/unoplatform/uno/issues/4853
					var @delegate = new CLLocationManagerDelegate();

					mgr.Delegate = @delegate;

					@delegate.AuthorizationChanged += (s, e) =>
#else
					mgr.AuthorizationChanged += (s, e) =>
#endif
					{
						if (e.Status != CLAuthorizationStatus.NotDetermined)
						{
							tsc.TrySetResult(e.Status);
						}
					};

#if __IOS__ || __TVOS__ //required only for iOS
					mgr.RequestWhenInUseAuthorization();
#endif

					if (CLLocationManager.Status != CLAuthorizationStatus.NotDetermined)
					{
						accessStatus = TranslateStatus(CLLocationManager.Status);
					}

					var cLAuthorizationStatus = await tsc.Task;

					accessStatus = TranslateStatus(cLAuthorizationStatus);

					//if geolocation is not well accessible, default geoposition should be recommended
					if (accessStatus != GeolocationAccessStatus.Allowed)
					{
						IsDefaultGeopositionRecommended = true;
					}

					return accessStatus;
				}
				finally
				{
					lock (_requestManagers)
					{
						_requestManagers.Remove(mgr);
					}
				}
			}
			else
			{
				return await CoreDispatcher.Main.RunWithResultAsync<GeolocationAccessStatus>(
					priority: CoreDispatcherPriority.Normal,
					task: () => RequestAccessInternalAsync()
				);
			}
		}

		private static GeolocationAccessStatus TranslateStatus(CLAuthorizationStatus status)
		{
			switch (status)
			{
				// These two constants are set by value based on https://developer.apple.com/library/ios/documentation/CoreLocation/Reference/CLLocationManager_Class/index.html#//apple_ref/c/tdef/CLAuthorizationStatus
				// This is for the compatibility with iOS 8 and the introduction of AuthorizedWhenInUse.
				// This can be replaced with proper enum values when upgrading to iOS 8.0 SDK.
				case (CLAuthorizationStatus)4: // CLAuthorizationStatus.AuthorizedWhenInUse:
				case (CLAuthorizationStatus)3: // CLAuthorizationStatus.AuthorizedAlways:
					return GeolocationAccessStatus.Allowed;

				case CLAuthorizationStatus.NotDetermined:
					return GeolocationAccessStatus.Unspecified;

				default:
				case CLAuthorizationStatus.Restricted:
				case CLAuthorizationStatus.Denied:
					return GeolocationAccessStatus.Denied;
			}
		}

#if __IOS__ || __TVOS__
		private class CLLocationManagerDelegate : NSObject, ICLLocationManagerDelegate
		{
			public event EventHandler<CLAuthorizationChangedEventArgs> AuthorizationChanged;

			[Export("locationManager:didChangeAuthorizationStatus:")]
			public void DidChangeAuthorizationStatus(CLLocationManager manager, CLAuthorizationStatus status)
			{
				AuthorizationChanged?.Invoke(manager, new CLAuthorizationChangedEventArgs(status));
			}
		}
#endif
	}
}
