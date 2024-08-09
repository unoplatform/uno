#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Gms.Maps;
using Android.Gms.Maps.Model;
using Android.Views;
using Uno.Disposables;
using Uno.Extensions;
using Uno.UI;
using Windows.Devices.Geolocation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Foundation.Metadata;
using Windows.UI.Xaml.Markup;
using Uno.Foundation.Logging;

namespace Windows.UI.Xaml.Controls.Maps.Presenters
{
	public sealed partial class MapPresenter : Control
	{
		private Grid? _mapGrid, _layerGrid;

		private MapReadyCallback _callback;
		private GoogleMapView _internalMapView;
		private GoogleMap? _map;
		private MapLifeCycleCallBacks _callbacks;
		private Android.App.Application _application;
		private MapControl? _owner;

		partial void InitializePartial()
		{
			_internalMapView = new GoogleMapView(ContextHelper.Current, new GoogleMapOptions());

			MapsInitializer.Initialize(ContextHelper.Current);

			_internalMapView.GetMapAsync(_callback = new MapReadyCallback(OnMapReady));

			_internalMapView.OnCreate(null); // This otherwise the map does not appear

			Loaded += (s, e) => OnControlLoaded();
			Unloaded += (s, e) => OnControlUnloaded();
		}

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();
			UpdateOwnerSubscriptions();

			_mapGrid = GetTemplateChild("MapGrid") as Grid;
			_layerGrid = GetTemplateChild("LayerGrid") as Grid;

			if (_mapGrid == null)
			{
				throw new InvalidOperationException("Unable to find [MapGrid] template part");
			}

			if (_layerGrid == null)
			{
				throw new InvalidOperationException("Unable to find [LayerGrid] template part");
			}

			_mapGrid.Children.Add(_internalMapView);
		}

		private IDisposable UpdateOwnerSubscriptions(MapControl owner)
		{
			OnCenterChanged();
			var disposables = new CompositeDisposable();
			owner.RegisterDisposablePropertyChangedCallback(MapControl.CenterProperty, (s, e) => OnCenterChanged()).DisposeWith(disposables);
			owner.RegisterDisposablePropertyChangedCallback(MapControl.ZoomLevelProperty, (s, e) => OnCenterChanged()).DisposeWith(disposables);
			return disposables;
		}

		private void OnMapReady(GoogleMap map)
		{
			_map = map;
			_map.MyLocationEnabled = false;

			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug("GoogleMap instance is ready");
			}

			OnCenterChanged();
		}

		private void OnCenterChanged()
		{
			if (_map != null && _owner != null)
			{
				var builder = new CameraPosition.Builder(_map.CameraPosition)
						.Target(_owner.Center.ToLatLng());

				builder.Zoom((float)_owner.ZoomLevel);

				var cameraUpdate = CameraUpdateFactory.NewCameraPosition(builder.Build());
				_map.AnimateCamera(cameraUpdate);
			}
		}

		private void OnControlLoaded()
		{
			_internalMapView.OnResume(); // This otherwise the map stay empty

			HandleActivityLifeCycle();

			_internalMapView.TouchOccurred += MapTouchOccurred;
		}

		private void OnControlUnloaded()
		{
			// These line is required for the control to 
			// stop actively monitoring the user's location.
			_internalMapView.OnPause();

			_application.UnregisterActivityLifecycleCallbacks(_callbacks);

			if (_internalMapView != null)
			{
				_internalMapView.TouchOccurred -= MapTouchOccurred;
			}
		}

		private void MapTouchOccurred(object? sender, MotionEvent e)
		{

		}

		/// <summary>
		/// Register to the LifeCycleCallbacks and properly call the OnResume and OnPause methods when needed.
		/// This will release the GPS while the application is in the background
		/// </summary>
		private void HandleActivityLifeCycle()
		{
			_callbacks = new MapLifeCycleCallBacks(onPause: _internalMapView.OnPause, onResume: _internalMapView.OnResume);

			_application = (Context?.ApplicationContext as Android.App.Application)!;
			if (_application != null)
			{
				_application.RegisterActivityLifecycleCallbacks(_callbacks);
			}
			else
			{
				this.Log().Error("ApplicationContext is invalid, could not RegisterActivityLifecycleCallbacks to release GPS when application is paused.");
			}
		}

		private class MapLifeCycleCallBacks : Java.Lang.Object, global::Android.App.Application.IActivityLifecycleCallbacks
		{
			private readonly Action _onPause;
			private readonly Action _onResume;

			public MapLifeCycleCallBacks(Action onPause, Action onResume)
			{
				_onResume = onResume;
				_onPause = onPause;
			}

			public void OnActivityResumed(Activity activity)
			{
				_onResume();
			}

			public void OnActivityPaused(Activity activity)
			{
				_onPause();
			}

			public void OnActivityCreated(Activity activity, global::Android.OS.Bundle? savedInstanceState)
			{
			}

			public void OnActivityDestroyed(Activity activity)
			{
			}

			public void OnActivitySaveInstanceState(Activity activity, global::Android.OS.Bundle outState)
			{
			}

			public void OnActivityStarted(Activity activity)
			{
			}

			public void OnActivityStopped(Activity activity)
			{
			}
		}

		private class MapReadyCallback : Java.Lang.Object, IOnMapReadyCallback
		{
			private readonly Action<GoogleMap> _mapAvailable;

			public MapReadyCallback(Action<GoogleMap> mapAvailable)
			{
				_mapAvailable = mapAvailable;
			}

			public void OnMapReady(GoogleMap googleMap)
			{
				_mapAvailable(googleMap);
			}
		}

		private class MapCancellableCallback : Java.Lang.Object, GoogleMap.ICancelableCallback
		{
			private readonly TaskCompletionSource<bool> _source = new TaskCompletionSource<bool>();
			private readonly IDisposable _cancelSubscription;

			public MapCancellableCallback(CancellationToken ct)
			{
				_cancelSubscription = ct.Register(() => _source.TrySetCanceled());
			}

			void GoogleMap.ICancelableCallback.OnCancel()
			{
				_source.TrySetCanceled();
			}

			void GoogleMap.ICancelableCallback.OnFinish()
			{
				_source.TrySetResult(true);
			}

			public TaskAwaiter<bool> GetAwaiter()
			{
				return _source.Task.GetAwaiter();
			}

			protected override void Dispose(bool disposing)
			{
				_cancelSubscription.Dispose();

				base.Dispose(disposing);
			}
		}
	}

	static partial class CoordinateExtensions
	{
		public static LatLng ToLatLng(this Geopoint c)
		{
			return new LatLng(c.Position.Latitude, c.Position.Longitude);
		}

		public static Geoposition ToGeoposition(LatLng l)
		{
			return new Geoposition(
				new Geocoordinate(
					latitude: l.Latitude,
					longitude: l.Longitude,
					point: new Geopoint(
						new BasicGeoposition
						{
							Latitude = l.Latitude,
							Longitude = l.Longitude
						}
					),
					accuracy: 0,
					timestamp: DateTime.Now
				)
			);
		}
	}
}
