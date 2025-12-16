#if !HAS_UNO_WINUI
#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using Uno;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Windows.Devices.Geolocation;

namespace Microsoft.UI.Xaml.Controls.Maps
{
#if !__APPLE_UIKIT__ && !__ANDROID__
	[NotImplemented]
#endif
	public partial class MapControl : Controls.Control, IUnoMapControl
	{
		public MapControl()
		{
			Children = new DependencyObjectCollection(this);

			Layers = new List<MapLayer>();

			DefaultStyleKey = typeof(MapControl);
		}

		private protected override void OnLoaded()
		{
			base.OnLoaded();

#if __APPLE_UIKIT__ || __ANDROID__
			if (Template == null && this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().LogWarning($"No template available for MapControl, the control will not display. Check that Uno MapControl support is configured correctly (see https://platform.uno/docs/articles/controls/map-control-support.html ).");
			}
#else
			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().LogWarning($"MapControl is not supported on this target platform.");
			}
#endif
		}

		public MapStyle Style
		{
			get => (MapStyle)this.GetValue(StyleProperty);
			set => this.SetValue(StyleProperty, value);
		}

		public double DesiredPitch
		{
			get => (double)this.GetValue(DesiredPitchProperty);
			set => this.SetValue(DesiredPitchProperty, value);
		}

		public MapColorScheme ColorScheme
		{
			get => (MapColorScheme)this.GetValue(ColorSchemeProperty);
			set => this.SetValue(ColorSchemeProperty, value);
		}

		public bool PedestrianFeaturesVisible
		{
			get => (bool)this.GetValue(PedestrianFeaturesVisibleProperty);
			set => this.SetValue(PedestrianFeaturesVisibleProperty, value);
		}

		public Geopoint Center
		{
			get => (Geopoint)this.GetValue(CenterProperty);
			set => this.SetValue(CenterProperty, value);
		}

		public bool LandmarksVisible
		{
			get => (bool)this.GetValue(LandmarksVisibleProperty);
			set => this.SetValue(LandmarksVisibleProperty, value);
		}

		public double Heading
		{
			get => (double)this.GetValue(HeadingProperty);
			set => this.SetValue(HeadingProperty, value);
		}

		public MapWatermarkMode WatermarkMode
		{
			get => (MapWatermarkMode)this.GetValue(WatermarkModeProperty);
			set => this.SetValue(WatermarkModeProperty, value);
		}

		public string MapServiceToken
		{
			get => (string)this.GetValue(MapServiceTokenProperty) ?? "";
			set => this.SetValue(MapServiceTokenProperty, value);
		}

		public global::Windows.Foundation.Point TransformOrigin
		{
			get => (global::Windows.Foundation.Point)this.GetValue(TransformOriginProperty);
			set => this.SetValue(TransformOriginProperty, value);
		}

		public bool TrafficFlowVisible
		{
			get => (bool)this.GetValue(TrafficFlowVisibleProperty);
			set => this.SetValue(TrafficFlowVisibleProperty, value);
		}

		public double ZoomLevel
		{
			get => (double)this.GetValue(ZoomLevelProperty);
			set => this.SetValue(ZoomLevelProperty, value);
		}

		public IList<DependencyObject> Children
		{
			get => (IList<DependencyObject>)this.GetValue(ChildrenProperty);
			set => this.SetValue(ChildrenProperty, value);
		}

		public MapLoadingStatus LoadingStatus => (MapLoadingStatus)this.GetValue(LoadingStatusProperty);
		public IList<MapElement> MapElements => (IList<MapElement>)this.GetValue(MapElementsProperty);
		public double MaxZoomLevel => throw new global::System.NotImplementedException("The member double MapControl.MaxZoomLevel is not implemented in Uno.");
		public double MinZoomLevel => throw new global::System.NotImplementedException("The member double MapControl.MinZoomLevel is not implemented in Uno.");
		public double Pitch => (double)this.GetValue(PitchProperty);
		public IList<MapRouteView> Routes => (IList<MapRouteView>)this.GetValue(RoutesProperty);
		public IList<MapTileSource> TileSources => (IList<MapTileSource>)this.GetValue(TileSourcesProperty);
		public MapCustomExperience CustomExperience
		{
			get => throw new global::System.NotImplementedException("The member MapCustomExperience MapControl.CustomExperience is not implemented in Uno.");
			set => global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.UI.Xaml.Controls.Maps.MapControl", "MapCustomExperience MapControl.CustomExperience");
		}
		public bool BusinessLandmarksVisible
		{
			get => (bool)this.GetValue(BusinessLandmarksVisibleProperty);
			set => this.SetValue(BusinessLandmarksVisibleProperty, value);
		}
		public MapInteractionMode ZoomInteractionMode
		{
			get => (MapInteractionMode)this.GetValue(ZoomInteractionModeProperty);
			set => this.SetValue(ZoomInteractionModeProperty, value);
		}
		public bool TransitFeaturesVisible
		{
			get => (bool)this.GetValue(TransitFeaturesVisibleProperty);
			set => this.SetValue(TransitFeaturesVisibleProperty, value);
		}
		public MapPanInteractionMode PanInteractionMode
		{
			get => (MapPanInteractionMode)this.GetValue(PanInteractionModeProperty);
			set => this.SetValue(PanInteractionModeProperty, value);
		}
		public MapInteractionMode TiltInteractionMode
		{
			get => (MapInteractionMode)this.GetValue(TiltInteractionModeProperty);
			set => this.SetValue(TiltInteractionModeProperty, value);
		}
		public MapScene Scene
		{
			get => (MapScene)this.GetValue(SceneProperty);
			set => this.SetValue(SceneProperty, value);
		}
		public MapInteractionMode RotateInteractionMode
		{
			get => (MapInteractionMode)this.GetValue(RotateInteractionModeProperty);
			set => this.SetValue(RotateInteractionModeProperty, value);
		}
		public bool Is3DSupported => (bool)this.GetValue(Is3DSupportedProperty);
		public bool IsStreetsideSupported => (bool)this.GetValue(IsStreetsideSupportedProperty);
		public MapCamera TargetCamera => throw new global::System.NotImplementedException("The member MapCamera MapControl.TargetCamera is not implemented in Uno.");
		public MapCamera ActualCamera => throw new global::System.NotImplementedException("The member MapCamera MapControl.ActualCamera is not implemented in Uno.");
		public bool TransitFeaturesEnabled
		{
			get => (bool)this.GetValue(TransitFeaturesEnabledProperty);
			set => this.SetValue(TransitFeaturesEnabledProperty, value);
		}
		public bool BusinessLandmarksEnabled
		{
			get => (bool)this.GetValue(BusinessLandmarksEnabledProperty);
			set => this.SetValue(BusinessLandmarksEnabledProperty, value);
		}
		public Thickness ViewPadding
		{
			get => (Thickness)this.GetValue(ViewPaddingProperty);
			set => this.SetValue(ViewPaddingProperty, value);
		}
		public MapStyleSheet StyleSheet
		{
			get => (MapStyleSheet)this.GetValue(StyleSheetProperty);
			set => this.SetValue(StyleSheetProperty, value);
		}
		public MapProjection MapProjection
		{
			get => (MapProjection)this.GetValue(MapProjectionProperty);
			set => this.SetValue(MapProjectionProperty, value);
		}
		public IList<MapLayer> Layers
		{
			get => (IList<MapLayer>)this.GetValue(LayersProperty);
			set => this.SetValue(LayersProperty, value);
		}
		public string Region
		{
			get => (string)this.GetValue(RegionProperty) ?? "";
			set => this.SetValue(RegionProperty, value);
		}
		public static DependencyProperty CenterProperty { get; } =
		Microsoft.UI.Xaml.DependencyProperty.Register(
			"Center", typeof(Geopoint),
			typeof(MapControl),
			new FrameworkPropertyMetadata(new Geopoint(new BasicGeoposition())));
		public static DependencyProperty ChildrenProperty { get; } =
		Microsoft.UI.Xaml.DependencyProperty.Register(
			"Children", typeof(IList<DependencyObject>),
			typeof(MapControl),
			new FrameworkPropertyMetadata(default(IList<DependencyObject>)));
		public static DependencyProperty ColorSchemeProperty { get; } =
		Microsoft.UI.Xaml.DependencyProperty.Register(
			"ColorScheme", typeof(MapColorScheme),
			typeof(MapControl),
			new FrameworkPropertyMetadata(default(MapColorScheme)));
		public static DependencyProperty DesiredPitchProperty { get; } =
		Microsoft.UI.Xaml.DependencyProperty.Register(
			"DesiredPitch", typeof(double),
			typeof(MapControl),
			new FrameworkPropertyMetadata(default(double)));
		public static DependencyProperty HeadingProperty { get; } =
		Microsoft.UI.Xaml.DependencyProperty.Register(
			"Heading", typeof(double),
			typeof(MapControl),
			new FrameworkPropertyMetadata(default(double)));
		public static DependencyProperty LandmarksVisibleProperty { get; } =
		Microsoft.UI.Xaml.DependencyProperty.Register(
			"LandmarksVisible", typeof(bool),
			typeof(MapControl),
			new FrameworkPropertyMetadata(default(bool)));
		public static DependencyProperty LocationProperty
		{
			[DynamicDependency(nameof(GetLocation))]
			[DynamicDependency(nameof(SetLocation))]
			get;
		} = Microsoft.UI.Xaml.DependencyProperty.RegisterAttached(
			"Location", typeof(Geopoint),
			typeof(MapControl),
			new FrameworkPropertyMetadata(default(Geopoint)));
		public static DependencyProperty MapElementsProperty { get; } =
		Microsoft.UI.Xaml.DependencyProperty.Register(
			"MapElements", typeof(IList<MapElement>),
			typeof(MapControl),
			new FrameworkPropertyMetadata(default(IList<MapElement>)));
		public static DependencyProperty MapServiceTokenProperty { get; } =
		Microsoft.UI.Xaml.DependencyProperty.Register(
			"MapServiceToken", typeof(string),
			typeof(MapControl),
			new FrameworkPropertyMetadata(default(string)));
		public static DependencyProperty NormalizedAnchorPointProperty
		{
			[DynamicDependency(nameof(GetNormalizedAnchorPoint))]
			[DynamicDependency(nameof(SetNormalizedAnchorPoint))]
			get;
		} = Microsoft.UI.Xaml.DependencyProperty.RegisterAttached(
			"NormalizedAnchorPoint", typeof(global::Windows.Foundation.Point),
			typeof(MapControl),
			new FrameworkPropertyMetadata(default(global::Windows.Foundation.Point)));
		public static DependencyProperty PedestrianFeaturesVisibleProperty { get; } =
		Microsoft.UI.Xaml.DependencyProperty.Register(
			"PedestrianFeaturesVisible", typeof(bool),
			typeof(MapControl),
			new FrameworkPropertyMetadata(default(bool)));
		public static DependencyProperty PitchProperty { get; } =
		Microsoft.UI.Xaml.DependencyProperty.Register(
			"Pitch", typeof(double),
			typeof(MapControl),
			new FrameworkPropertyMetadata(default(double)));
		public static DependencyProperty RoutesProperty { get; } =
		Microsoft.UI.Xaml.DependencyProperty.Register(
			"Routes", typeof(IList<MapRouteView>),
			typeof(MapControl),
			new FrameworkPropertyMetadata(default(IList<MapRouteView>)));
		public static DependencyProperty StyleProperty { get; } =
		Microsoft.UI.Xaml.DependencyProperty.Register(
			"Style", typeof(MapStyle),
			typeof(MapControl),
			new FrameworkPropertyMetadata(default(MapStyle)));
		public static DependencyProperty TileSourcesProperty { get; } =
		Microsoft.UI.Xaml.DependencyProperty.Register(
			"TileSources", typeof(IList<MapTileSource>),
			typeof(MapControl),
			new FrameworkPropertyMetadata(default(IList<MapTileSource>)));
		public static DependencyProperty TrafficFlowVisibleProperty { get; } =
		Microsoft.UI.Xaml.DependencyProperty.Register(
			"TrafficFlowVisible", typeof(bool),
			typeof(MapControl),
			new FrameworkPropertyMetadata(default(bool)));
		public static DependencyProperty TransformOriginProperty { get; } =
		Microsoft.UI.Xaml.DependencyProperty.Register(
			"TransformOrigin", typeof(global::Windows.Foundation.Point),
			typeof(MapControl),
			new FrameworkPropertyMetadata(default(global::Windows.Foundation.Point)));
		public static DependencyProperty WatermarkModeProperty { get; } =
		Microsoft.UI.Xaml.DependencyProperty.Register(
			"WatermarkMode", typeof(MapWatermarkMode),
			typeof(MapControl),
			new FrameworkPropertyMetadata(default(MapWatermarkMode)));
		public static DependencyProperty ZoomLevelProperty { get; } =
		Microsoft.UI.Xaml.DependencyProperty.Register(
			"ZoomLevel", typeof(double),
			typeof(MapControl),
			new FrameworkPropertyMetadata(default(double)));
		public static DependencyProperty LoadingStatusProperty { get; } =
		Microsoft.UI.Xaml.DependencyProperty.Register(
			"LoadingStatus", typeof(MapLoadingStatus),
			typeof(MapControl),
			new FrameworkPropertyMetadata(default(MapLoadingStatus)));
		public static DependencyProperty BusinessLandmarksVisibleProperty { get; } =
		Microsoft.UI.Xaml.DependencyProperty.Register(
			"BusinessLandmarksVisible", typeof(bool),
			typeof(MapControl),
			new FrameworkPropertyMetadata(default(bool)));
		public static DependencyProperty Is3DSupportedProperty { get; } =
		Microsoft.UI.Xaml.DependencyProperty.Register(
			"Is3DSupported", typeof(bool),
			typeof(MapControl),
			new FrameworkPropertyMetadata(default(bool)));
		public static DependencyProperty PanInteractionModeProperty { get; } =
		Microsoft.UI.Xaml.DependencyProperty.Register(
			"PanInteractionMode", typeof(MapPanInteractionMode),
			typeof(MapControl),
			new FrameworkPropertyMetadata(default(MapPanInteractionMode)));
		public static DependencyProperty RotateInteractionModeProperty { get; } =
		Microsoft.UI.Xaml.DependencyProperty.Register(
			"RotateInteractionMode", typeof(MapInteractionMode),
			typeof(MapControl),
			new FrameworkPropertyMetadata(default(MapInteractionMode)));
		public static DependencyProperty TiltInteractionModeProperty { get; } =
		Microsoft.UI.Xaml.DependencyProperty.Register(
			"TiltInteractionMode", typeof(MapInteractionMode),
			typeof(MapControl),
			new FrameworkPropertyMetadata(default(MapInteractionMode)));
		public static DependencyProperty TransitFeaturesVisibleProperty { get; } =
		Microsoft.UI.Xaml.DependencyProperty.Register(
			"TransitFeaturesVisible", typeof(bool),
			typeof(MapControl),
			new FrameworkPropertyMetadata(default(bool)));
		public static DependencyProperty ZoomInteractionModeProperty { get; } =
		Microsoft.UI.Xaml.DependencyProperty.Register(
			"ZoomInteractionMode", typeof(MapInteractionMode),
			typeof(MapControl),
			new FrameworkPropertyMetadata(default(MapInteractionMode)));
		public static DependencyProperty IsStreetsideSupportedProperty { get; } =
		Microsoft.UI.Xaml.DependencyProperty.Register(
			"IsStreetsideSupported", typeof(bool),
			typeof(MapControl),
			new FrameworkPropertyMetadata(default(bool)));
		public static DependencyProperty SceneProperty { get; } =
		Microsoft.UI.Xaml.DependencyProperty.Register(
			"Scene", typeof(MapScene),
			typeof(MapControl),
			new FrameworkPropertyMetadata(default(MapScene)));
		public static DependencyProperty BusinessLandmarksEnabledProperty { get; } =
		Microsoft.UI.Xaml.DependencyProperty.Register(
			"BusinessLandmarksEnabled", typeof(bool),
			typeof(MapControl),
			new FrameworkPropertyMetadata(default(bool)));
		public static DependencyProperty TransitFeaturesEnabledProperty { get; } =
		Microsoft.UI.Xaml.DependencyProperty.Register(
			"TransitFeaturesEnabled", typeof(bool),
			typeof(MapControl),
			new FrameworkPropertyMetadata(default(bool)));
		public static DependencyProperty MapProjectionProperty { get; } =
		Microsoft.UI.Xaml.DependencyProperty.Register(
			"MapProjection", typeof(MapProjection),
			typeof(MapControl),
			new FrameworkPropertyMetadata(default(MapProjection)));
		public static DependencyProperty StyleSheetProperty { get; } =
		Microsoft.UI.Xaml.DependencyProperty.Register(
			"StyleSheet", typeof(MapStyleSheet),
			typeof(MapControl),
			new FrameworkPropertyMetadata(default(MapStyleSheet)));
		public static DependencyProperty ViewPaddingProperty { get; } =
		Microsoft.UI.Xaml.DependencyProperty.Register(
			"ViewPadding", typeof(Thickness),
			typeof(MapControl),
			new FrameworkPropertyMetadata(default(Thickness)));
		public static DependencyProperty LayersProperty { get; } =
		Microsoft.UI.Xaml.DependencyProperty.Register(
			"Layers", typeof(IList<MapLayer>),
			typeof(MapControl),
			new FrameworkPropertyMetadata(default(IList<MapLayer>)));
		public static DependencyProperty RegionProperty { get; } =
		Microsoft.UI.Xaml.DependencyProperty.Register(
			"Region", typeof(string),
			typeof(MapControl),
			new FrameworkPropertyMetadata(default(string)));
		public IReadOnlyList<MapElement> FindMapElementsAtOffset(global::Windows.Foundation.Point offset)
		{
			throw new global::System.NotImplementedException("The member IReadOnlyList<MapElement> MapControl.FindMapElementsAtOffset(Point offset) is not implemented in Uno.");
		}
		public void GetLocationFromOffset(global::Windows.Foundation.Point offset, out Geopoint location)
		{
			throw new global::System.NotImplementedException("The member void MapControl.GetLocationFromOffset(Point offset, out Geopoint location) is not implemented in Uno.");
		}
		public void GetOffsetFromLocation(Geopoint location, out global::Windows.Foundation.Point offset)
		{
			throw new global::System.NotImplementedException("The member void MapControl.GetOffsetFromLocation(Geopoint location, out Point offset) is not implemented in Uno.");
		}
		public void IsLocationInView(Geopoint location, out bool isInView)
		{
			throw new global::System.NotImplementedException("The member void MapControl.IsLocationInView(Geopoint location, out bool isInView) is not implemented in Uno.");
		}
		public global::Windows.Foundation.IAsyncOperation<bool> TrySetViewBoundsAsync(GeoboundingBox bounds, Thickness? margin, MapAnimationKind animation)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> MapControl.TrySetViewBoundsAsync(GeoboundingBox bounds, Thickness? margin, MapAnimationKind animation) is not implemented in Uno.");
		}
		public global::Windows.Foundation.IAsyncOperation<bool> TrySetViewAsync(Geopoint center)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> MapControl.TrySetViewAsync(Geopoint center) is not implemented in Uno.");
		}
		public global::Windows.Foundation.IAsyncOperation<bool> TrySetViewAsync(Geopoint center, double? zoomLevel)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> MapControl.TrySetViewAsync(Geopoint center, double? zoomLevel) is not implemented in Uno.");
		}
		public global::Windows.Foundation.IAsyncOperation<bool> TrySetViewAsync(Geopoint center, double? zoomLevel, double? heading, double? desiredPitch)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> MapControl.TrySetViewAsync(Geopoint center, double? zoomLevel, double? heading, double? desiredPitch) is not implemented in Uno.");
		}
		public global::Windows.Foundation.IAsyncOperation<bool> TrySetViewAsync(Geopoint center, double? zoomLevel, double? heading, double? desiredPitch, MapAnimationKind animation)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> MapControl.TrySetViewAsync(Geopoint center, double? zoomLevel, double? heading, double? desiredPitch, MapAnimationKind animation) is not implemented in Uno.");
		}
		public void StartContinuousRotate(double rateInDegreesPerSecond)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.UI.Xaml.Controls.Maps.MapControl", "void MapControl.StartContinuousRotate(double rateInDegreesPerSecond)");
		}
		public void StopContinuousRotate()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.UI.Xaml.Controls.Maps.MapControl", "void MapControl.StopContinuousRotate()");
		}
		public void StartContinuousTilt(double rateInDegreesPerSecond)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.UI.Xaml.Controls.Maps.MapControl", "void MapControl.StartContinuousTilt(double rateInDegreesPerSecond)");
		}
		public void StopContinuousTilt()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.UI.Xaml.Controls.Maps.MapControl", "void MapControl.StopContinuousTilt()");
		}
		public void StartContinuousZoom(double rateOfChangePerSecond)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.UI.Xaml.Controls.Maps.MapControl", "void MapControl.StartContinuousZoom(double rateOfChangePerSecond)");
		}
		public void StopContinuousZoom()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.UI.Xaml.Controls.Maps.MapControl", "void MapControl.StopContinuousZoom()");
		}
		public global::Windows.Foundation.IAsyncOperation<bool> TryRotateAsync(double degrees)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> MapControl.TryRotateAsync(double degrees) is not implemented in Uno.");
		}
		public global::Windows.Foundation.IAsyncOperation<bool> TryRotateToAsync(double angleInDegrees)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> MapControl.TryRotateToAsync(double angleInDegrees) is not implemented in Uno.");
		}
		public global::Windows.Foundation.IAsyncOperation<bool> TryTiltAsync(double degrees)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> MapControl.TryTiltAsync(double degrees) is not implemented in Uno.");
		}
		public global::Windows.Foundation.IAsyncOperation<bool> TryTiltToAsync(double angleInDegrees)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> MapControl.TryTiltToAsync(double angleInDegrees) is not implemented in Uno.");
		}
		public global::Windows.Foundation.IAsyncOperation<bool> TryZoomInAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> MapControl.TryZoomInAsync() is not implemented in Uno.");
		}
		public global::Windows.Foundation.IAsyncOperation<bool> TryZoomOutAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> MapControl.TryZoomOutAsync() is not implemented in Uno.");
		}
		public global::Windows.Foundation.IAsyncOperation<bool> TryZoomToAsync(double zoomLevel)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> MapControl.TryZoomToAsync(double zoomLevel) is not implemented in Uno.");
		}
		public global::Windows.Foundation.IAsyncOperation<bool> TrySetSceneAsync(MapScene scene)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> MapControl.TrySetSceneAsync(MapScene scene) is not implemented in Uno.");
		}
		public global::Windows.Foundation.IAsyncOperation<bool> TrySetSceneAsync(MapScene scene, MapAnimationKind animationKind)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> MapControl.TrySetSceneAsync(MapScene scene, MapAnimationKind animationKind) is not implemented in Uno.");
		}
		public Geopath GetVisibleRegion(MapVisibleRegionKind region)
		{
			throw new global::System.NotImplementedException("The member Geopath MapControl.GetVisibleRegion(MapVisibleRegionKind region) is not implemented in Uno.");
		}

		public IReadOnlyList<MapElement> FindMapElementsAtOffset(global::Windows.Foundation.Point offset, double radius)
		{
			throw new global::System.NotImplementedException("The member IReadOnlyList<MapElement> MapControl.FindMapElementsAtOffset(Point offset, double radius) is not implemented in Uno.");
		}
		public void GetLocationFromOffset(global::Windows.Foundation.Point offset, AltitudeReferenceSystem desiredReferenceSystem, out Geopoint location)
		{
			throw new global::System.NotImplementedException("The member void MapControl.GetLocationFromOffset(Point offset, AltitudeReferenceSystem desiredReferenceSystem, out Geopoint location) is not implemented in Uno.");
		}
		public void StartContinuousPan(double horizontalPixelsPerSecond, double verticalPixelsPerSecond)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.UI.Xaml.Controls.Maps.MapControl", "void MapControl.StartContinuousPan(double horizontalPixelsPerSecond, double verticalPixelsPerSecond)");
		}
		public void StopContinuousPan()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.UI.Xaml.Controls.Maps.MapControl", "void MapControl.StopContinuousPan()");
		}
		public global::Windows.Foundation.IAsyncOperation<bool> TryPanAsync(double horizontalPixels, double verticalPixels)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> MapControl.TryPanAsync(double horizontalPixels, double verticalPixels) is not implemented in Uno.");
		}
		public global::Windows.Foundation.IAsyncOperation<bool> TryPanToAsync(Geopoint location)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> MapControl.TryPanToAsync(Geopoint location) is not implemented in Uno.");
		}
		public bool TryGetLocationFromOffset(global::Windows.Foundation.Point offset, out Geopoint location)
		{
			throw new global::System.NotImplementedException("The member bool MapControl.TryGetLocationFromOffset(Point offset, out Geopoint location) is not implemented in Uno.");
		}
		public bool TryGetLocationFromOffset(global::Windows.Foundation.Point offset, AltitudeReferenceSystem desiredReferenceSystem, out Geopoint location)
		{
			throw new global::System.NotImplementedException("The member bool MapControl.TryGetLocationFromOffset(Point offset, AltitudeReferenceSystem desiredReferenceSystem, out Geopoint location) is not implemented in Uno.");
		}

		public static Geopoint GetLocation(DependencyObject element)
		{
			return (Geopoint)element.GetValue(LocationProperty);
		}
		public static void SetLocation(DependencyObject element, Geopoint value)
		{
			element.SetValue(LocationProperty, value);
		}
		public static global::Windows.Foundation.Point GetNormalizedAnchorPoint(DependencyObject element)
		{
			return (global::Windows.Foundation.Point)element.GetValue(NormalizedAnchorPointProperty);
		}
		public static void SetNormalizedAnchorPoint(DependencyObject element, global::Windows.Foundation.Point value)
		{
			element.SetValue(NormalizedAnchorPointProperty, value);
		}
		public event global::Windows.Foundation.TypedEventHandler<MapControl, object> CenterChanged;
		public event global::Windows.Foundation.TypedEventHandler<MapControl, object> HeadingChanged
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.UI.Xaml.Controls.Maps.MapControl", "event TypedEventHandler<MapControl, object> MapControl.HeadingChanged");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.UI.Xaml.Controls.Maps.MapControl", "event TypedEventHandler<MapControl, object> MapControl.HeadingChanged");
			}
		}
		public event global::Windows.Foundation.TypedEventHandler<MapControl, object> LoadingStatusChanged
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.UI.Xaml.Controls.Maps.MapControl", "event TypedEventHandler<MapControl, object> MapControl.LoadingStatusChanged");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.UI.Xaml.Controls.Maps.MapControl", "event TypedEventHandler<MapControl, object> MapControl.LoadingStatusChanged");
			}
		}
		public event global::Windows.Foundation.TypedEventHandler<MapControl, MapInputEventArgs> MapDoubleTapped
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.UI.Xaml.Controls.Maps.MapControl", "event TypedEventHandler<MapControl, MapInputEventArgs> MapControl.MapDoubleTapped");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.UI.Xaml.Controls.Maps.MapControl", "event TypedEventHandler<MapControl, MapInputEventArgs> MapControl.MapDoubleTapped");
			}
		}
		public event global::Windows.Foundation.TypedEventHandler<MapControl, MapInputEventArgs> MapHolding
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.UI.Xaml.Controls.Maps.MapControl", "event TypedEventHandler<MapControl, MapInputEventArgs> MapControl.MapHolding");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.UI.Xaml.Controls.Maps.MapControl", "event TypedEventHandler<MapControl, MapInputEventArgs> MapControl.MapHolding");
			}
		}
		public event global::Windows.Foundation.TypedEventHandler<MapControl, MapInputEventArgs> MapTapped
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.UI.Xaml.Controls.Maps.MapControl", "event TypedEventHandler<MapControl, MapInputEventArgs> MapControl.MapTapped");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.UI.Xaml.Controls.Maps.MapControl", "event TypedEventHandler<MapControl, MapInputEventArgs> MapControl.MapTapped");
			}
		}
		public event global::Windows.Foundation.TypedEventHandler<MapControl, object> PitchChanged
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.UI.Xaml.Controls.Maps.MapControl", "event TypedEventHandler<MapControl, object> MapControl.PitchChanged");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.UI.Xaml.Controls.Maps.MapControl", "event TypedEventHandler<MapControl, object> MapControl.PitchChanged");
			}
		}
		public event global::Windows.Foundation.TypedEventHandler<MapControl, object> TransformOriginChanged
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.UI.Xaml.Controls.Maps.MapControl", "event TypedEventHandler<MapControl, object> MapControl.TransformOriginChanged");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.UI.Xaml.Controls.Maps.MapControl", "event TypedEventHandler<MapControl, object> MapControl.TransformOriginChanged");
			}
		}
		public event global::Windows.Foundation.TypedEventHandler<MapControl, object> ZoomLevelChanged;
		public event global::Windows.Foundation.TypedEventHandler<MapControl, MapActualCameraChangedEventArgs> ActualCameraChanged
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.UI.Xaml.Controls.Maps.MapControl", "event TypedEventHandler<MapControl, MapActualCameraChangedEventArgs> MapControl.ActualCameraChanged");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.UI.Xaml.Controls.Maps.MapControl", "event TypedEventHandler<MapControl, MapActualCameraChangedEventArgs> MapControl.ActualCameraChanged");
			}
		}
		public event global::Windows.Foundation.TypedEventHandler<MapControl, MapActualCameraChangingEventArgs> ActualCameraChanging
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.UI.Xaml.Controls.Maps.MapControl", "event TypedEventHandler<MapControl, MapActualCameraChangingEventArgs> MapControl.ActualCameraChanging");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.UI.Xaml.Controls.Maps.MapControl", "event TypedEventHandler<MapControl, MapActualCameraChangingEventArgs> MapControl.ActualCameraChanging");
			}
		}
		public event global::Windows.Foundation.TypedEventHandler<MapControl, MapCustomExperienceChangedEventArgs> CustomExperienceChanged
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.UI.Xaml.Controls.Maps.MapControl", "event TypedEventHandler<MapControl, MapCustomExperienceChangedEventArgs> MapControl.CustomExperienceChanged");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.UI.Xaml.Controls.Maps.MapControl", "event TypedEventHandler<MapControl, MapCustomExperienceChangedEventArgs> MapControl.CustomExperienceChanged");
			}
		}
		public event global::Windows.Foundation.TypedEventHandler<MapControl, MapElementClickEventArgs> MapElementClick
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.UI.Xaml.Controls.Maps.MapControl", "event TypedEventHandler<MapControl, MapElementClickEventArgs> MapControl.MapElementClick");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.UI.Xaml.Controls.Maps.MapControl", "event TypedEventHandler<MapControl, MapElementClickEventArgs> MapControl.MapElementClick");
			}
		}
		public event global::Windows.Foundation.TypedEventHandler<MapControl, MapElementPointerEnteredEventArgs> MapElementPointerEntered
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.UI.Xaml.Controls.Maps.MapControl", "event TypedEventHandler<MapControl, MapElementPointerEnteredEventArgs> MapControl.MapElementPointerEntered");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.UI.Xaml.Controls.Maps.MapControl", "event TypedEventHandler<MapControl, MapElementPointerEnteredEventArgs> MapControl.MapElementPointerEntered");
			}
		}
		public event global::Windows.Foundation.TypedEventHandler<MapControl, MapElementPointerExitedEventArgs> MapElementPointerExited
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.UI.Xaml.Controls.Maps.MapControl", "event TypedEventHandler<MapControl, MapElementPointerExitedEventArgs> MapControl.MapElementPointerExited");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.UI.Xaml.Controls.Maps.MapControl", "event TypedEventHandler<MapControl, MapElementPointerExitedEventArgs> MapControl.MapElementPointerExited");
			}
		}
		public event global::Windows.Foundation.TypedEventHandler<MapControl, MapTargetCameraChangedEventArgs> TargetCameraChanged
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.UI.Xaml.Controls.Maps.MapControl", "event TypedEventHandler<MapControl, MapTargetCameraChangedEventArgs> MapControl.TargetCameraChanged");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.UI.Xaml.Controls.Maps.MapControl", "event TypedEventHandler<MapControl, MapTargetCameraChangedEventArgs> MapControl.TargetCameraChanged");
			}
		}
		public event global::Windows.Foundation.TypedEventHandler<MapControl, MapRightTappedEventArgs> MapRightTapped
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.UI.Xaml.Controls.Maps.MapControl", "event TypedEventHandler<MapControl, MapRightTappedEventArgs> MapControl.MapRightTapped");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.UI.Xaml.Controls.Maps.MapControl", "event TypedEventHandler<MapControl, MapRightTappedEventArgs> MapControl.MapRightTapped");
			}
		}
		public event global::Windows.Foundation.TypedEventHandler<MapControl, MapContextRequestedEventArgs> MapContextRequested
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.UI.Xaml.Controls.Maps.MapControl", "event TypedEventHandler<MapControl, MapContextRequestedEventArgs> MapControl.MapContextRequested");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.UI.Xaml.Controls.Maps.MapControl", "event TypedEventHandler<MapControl, MapContextRequestedEventArgs> MapControl.MapContextRequested");
			}
		}

		void IUnoMapControl.RaiseCenterChanged(object sender, object p)
			=> CenterChanged?.Invoke(this, p);

		void IUnoMapControl.RaiseZoomLevelChanged(object sender, object p)
			=> ZoomLevelChanged?.Invoke(this, p);
	}
}
#endif
