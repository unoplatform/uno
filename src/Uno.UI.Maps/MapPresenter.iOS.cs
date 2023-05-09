#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CoreLocation;
using Foundation;
using MapKit;
using UIKit;
using Uno.Disposables;
using Uno.Extensions;
using Windows.Devices.Geolocation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Foundation.Metadata;
using Windows.UI.Xaml.Markup;
using AnnotationAlias = MapKit.IMKAnnotation;
using OverlayAlias = MapKit.IMKOverlay;

namespace Windows.UI.Xaml.Controls.Maps.Presenters
{
	public sealed partial class MapPresenter : Control
	{
		private Grid? _mapGrid, _layerGrid;

		private MKMapView _internalMapView;
		private readonly Dictionary<OverlayAlias, MKOverlayRenderer> _overlayRenderers = new Dictionary<OverlayAlias, MKOverlayRenderer>();
		private readonly SerialDisposable _elementsDisposable = new SerialDisposable();
		private readonly Dictionary<DependencyObject, MapControlAnnotation> _elements = new Dictionary<DependencyObject, MapControlAnnotation>();
		private bool _changingCenter;

		private MapControl? _owner;

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

			_internalMapView = new MKMapView();

			_mapGrid.Children.Add(_internalMapView);
			_internalMapView.ShowsUserLocation = false;

			_internalMapView.RegionChanged += OnRegionChanged;

			OnChildrenCollectionChanged(this, null);

			SetUpOverlayRenderer();

			_internalMapView.GetViewForAnnotation = OnGetViewForAnnotation!;
		}

		private IDisposable UpdateOwnerSubscriptions(MapControl owner)
		{
			var disposables = new CompositeDisposable();
			owner.RegisterDisposablePropertyChangedCallback(MapControl.CenterProperty, OnCenterChanged).DisposeWith(disposables);
			owner.RegisterDisposablePropertyChangedCallback(MapControl.ZoomLevelProperty, OnZoomLevelChanged).DisposeWith(disposables);

			((DependencyObjectCollection)owner.Children).VectorChanged += OnChildrenCollectionChanged;
			Disposable.Create(() => ((DependencyObjectCollection)owner.Children).VectorChanged -= OnChildrenCollectionChanged).DisposeWith(disposables);
			return disposables;
		}

		private void OnRegionChanged(object? sender, MKMapViewChangeEventArgs e)
		{
			if (!_changingCenter)
			{
				_changingCenter = true;

				try
				{
					if (_owner != null)
					{
						_owner.Center = _internalMapView.Region.Center.ToGeopoint();
					}
					UpdateZoomLevel();
				}
				finally
				{
					_changingCenter = false;
				}
			}
		}

		private double Log2(double value) => (Math.Log(value) / Math.Log(2));

		private void UpdateZoomLevel()
		{
			if (_owner != null)
			{
				_owner.ZoomLevel = 21 - Log2(_internalMapView.Region.Span.LongitudeDelta *
					MapHelper.MercatorRadius * Math.PI / (180.0 * _internalMapView.Bounds.Size.Width));
			}
		}

		private void OnCenterChanged(DependencyObject sender, object _)
		{
			if (!_changingCenter)
			{
				_changingCenter = true;

				try
				{
					if (_owner is IUnoMapControl unoMap)
					{
						unoMap.RaiseCenterChanged(this, null);
					}

					RefreshPosition();
				}
				finally
				{
					_changingCenter = false;
				}
			}
		}

		private void OnZoomLevelChanged(DependencyObject sender, object _)
		{
			if (!_changingCenter)
			{
				_changingCenter = true;

				try
				{
					if (_owner is IUnoMapControl unoMap)
					{
						unoMap.RaiseZoomLevelChanged(this, null);
					}

					RefreshPosition();
				}
				finally
				{
					_changingCenter = false;
				}
			}
		}

		private void OnChildrenCollectionChanged(object sender, IVectorChangedEventArgs? e)
		{
			if (_internalMapView != null && _owner != null)
			{
				var mapItemsControl = _owner.Children.OfType<MapItemsControl>();
				var allItems = mapItemsControl.SelectMany(c => c.Items.OfType<UIElement>()).ToArray();

				var removed = _elements.Keys.Except().ToArray();

				foreach (var child in allItems)
				{
					MapControlAnnotation? annotation;

					if (!_elements.TryGetValue(child, out annotation))
					{
						_internalMapView.AddAnnotation(new MapControlAnnotation(child));
					}
				}

				if (removed.Length != 0)
				{
					_internalMapView.RemoveAnnotations(removed.Select(d => _elements[d]).ToArray());
				}
			}
		}

		private void RefreshPosition()
		{
			if (_owner != null)
			{
				var region = MapHelper.CreateRegion(_owner.Center, _owner.ZoomLevel, Bounds.Size);

				_internalMapView.SetRegion(region, true);
			}
		}

		private void SetUpOverlayRenderer()
		{
			// We do not support GetViewForOverlay.
			_internalMapView.OverlayRenderer = OnGetOverlayRenderer;
		}

		private MKOverlayRenderer OnGetOverlayRenderer(MKMapView mapView, IMKOverlay overlay)
		{
			return _overlayRenderers.GetValueOrDefault(overlay)!;
		}

		private MKAnnotationView? OnGetViewForAnnotation(MKMapView mapView, AnnotationAlias annotation)
		{
			if (annotation is MapControlAnnotation mapAnnotation)
			{
				var annotationView = mapView.DequeueReusableAnnotation(MapControlAnnotation.AnnotationId);

				if (annotationView == null)
				{
					annotationView = new MKAnnotationView(mapAnnotation, MapControlAnnotation.AnnotationId);
					annotationView.Add(mapAnnotation.Content);
				}

				if (mapAnnotation.Content is FrameworkElement element)
				{
					element.Measure(new Size((float)Bounds.Width, (float)Bounds.Height));
					var size = element.DesiredSize;
					element.Arrange(new Rect(0, 0, size.Width, size.Height));

					// Set the frame size, or the pin will not be selectable.
					annotationView.Frame = new CoreGraphics.CGRect(0, 0, size.Width, size.Height);
					mapAnnotation.Content.Frame = new CoreGraphics.CGRect(0, 0, size.Width, size.Height);
				}

				return annotationView;
			}

			return null;
		}

		private class MapControlAnnotation : MKAnnotation
		{
			public const string AnnotationId = "Uno.Pushpin";
			private CLLocationCoordinate2D _coordinate;

			public MapControlAnnotation(UIElement child)
			{
				Content = child;

				Content.RegisterDisposablePropertyChangedCallback(MapControl.LocationProperty, OnCoordinateChanged);
			}

			private void OnCoordinateChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
			{
				WillChangeValue("coordinate");
				_coordinate = GetAttachedLocation();
				DidChangeValue("coordinate");
			}

			public override void SetCoordinate(CLLocationCoordinate2D value)
			{
				_coordinate = value;
				MapControl.SetLocation(Content, _coordinate.ToGeopoint());
			}

			public override string Title
			{
				get
				{
					return "Test";
				}
			}

			public override CLLocationCoordinate2D Coordinate
			{
				get { return _coordinate; }
			}

			private CLLocationCoordinate2D GetAttachedLocation()
			{
				return MapControl.GetLocation(Content)?.ToLocation() ?? new CLLocationCoordinate2D();
			}

			public UIElement Content { get; set; }
		}
	}
}
