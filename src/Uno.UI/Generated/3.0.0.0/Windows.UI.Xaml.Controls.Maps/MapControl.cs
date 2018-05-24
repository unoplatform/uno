#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls.Maps
{
	#if false || false || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public  partial class MapControl : global::Windows.UI.Xaml.Controls.Control
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Controls.Maps.MapStyle Style
		{
			get
			{
				return (global::Windows.UI.Xaml.Controls.Maps.MapStyle)this.GetValue(StyleProperty);
			}
			set
			{
				this.SetValue(StyleProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  double DesiredPitch
		{
			get
			{
				return (double)this.GetValue(DesiredPitchProperty);
			}
			set
			{
				this.SetValue(DesiredPitchProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Controls.Maps.MapColorScheme ColorScheme
		{
			get
			{
				return (global::Windows.UI.Xaml.Controls.Maps.MapColorScheme)this.GetValue(ColorSchemeProperty);
			}
			set
			{
				this.SetValue(ColorSchemeProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  bool PedestrianFeaturesVisible
		{
			get
			{
				return (bool)this.GetValue(PedestrianFeaturesVisibleProperty);
			}
			set
			{
				this.SetValue(PedestrianFeaturesVisibleProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::Windows.Devices.Geolocation.Geopoint Center
		{
			get
			{
				return (global::Windows.Devices.Geolocation.Geopoint)this.GetValue(CenterProperty);
			}
			set
			{
				this.SetValue(CenterProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  bool LandmarksVisible
		{
			get
			{
				return (bool)this.GetValue(LandmarksVisibleProperty);
			}
			set
			{
				this.SetValue(LandmarksVisibleProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  double Heading
		{
			get
			{
				return (double)this.GetValue(HeadingProperty);
			}
			set
			{
				this.SetValue(HeadingProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Controls.Maps.MapWatermarkMode WatermarkMode
		{
			get
			{
				return (global::Windows.UI.Xaml.Controls.Maps.MapWatermarkMode)this.GetValue(WatermarkModeProperty);
			}
			set
			{
				this.SetValue(WatermarkModeProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  string MapServiceToken
		{
			get
			{
				return (string)this.GetValue(MapServiceTokenProperty);
			}
			set
			{
				this.SetValue(MapServiceTokenProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.Point TransformOrigin
		{
			get
			{
				return (global::Windows.Foundation.Point)this.GetValue(TransformOriginProperty);
			}
			set
			{
				this.SetValue(TransformOriginProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  bool TrafficFlowVisible
		{
			get
			{
				return (bool)this.GetValue(TrafficFlowVisibleProperty);
			}
			set
			{
				this.SetValue(TrafficFlowVisibleProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  double ZoomLevel
		{
			get
			{
				return (double)this.GetValue(ZoomLevelProperty);
			}
			set
			{
				this.SetValue(ZoomLevelProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::System.Collections.Generic.IList<global::Windows.UI.Xaml.DependencyObject> Children
		{
			get
			{
				return (global::System.Collections.Generic.IList<global::Windows.UI.Xaml.DependencyObject>)this.GetValue(ChildrenProperty);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Controls.Maps.MapLoadingStatus LoadingStatus
		{
			get
			{
				return (global::Windows.UI.Xaml.Controls.Maps.MapLoadingStatus)this.GetValue(LoadingStatusProperty);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::System.Collections.Generic.IList<global::Windows.UI.Xaml.Controls.Maps.MapElement> MapElements
		{
			get
			{
				return (global::System.Collections.Generic.IList<global::Windows.UI.Xaml.Controls.Maps.MapElement>)this.GetValue(MapElementsProperty);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  double MaxZoomLevel
		{
			get
			{
				throw new global::System.NotImplementedException("The member double MapControl.MaxZoomLevel is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  double MinZoomLevel
		{
			get
			{
				throw new global::System.NotImplementedException("The member double MapControl.MinZoomLevel is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  double Pitch
		{
			get
			{
				return (double)this.GetValue(PitchProperty);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::System.Collections.Generic.IList<global::Windows.UI.Xaml.Controls.Maps.MapRouteView> Routes
		{
			get
			{
				return (global::System.Collections.Generic.IList<global::Windows.UI.Xaml.Controls.Maps.MapRouteView>)this.GetValue(RoutesProperty);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::System.Collections.Generic.IList<global::Windows.UI.Xaml.Controls.Maps.MapTileSource> TileSources
		{
			get
			{
				return (global::System.Collections.Generic.IList<global::Windows.UI.Xaml.Controls.Maps.MapTileSource>)this.GetValue(TileSourcesProperty);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Controls.Maps.MapCustomExperience CustomExperience
		{
			get
			{
				throw new global::System.NotImplementedException("The member MapCustomExperience MapControl.CustomExperience is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Maps.MapControl", "MapCustomExperience MapControl.CustomExperience");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  bool BusinessLandmarksVisible
		{
			get
			{
				return (bool)this.GetValue(BusinessLandmarksVisibleProperty);
			}
			set
			{
				this.SetValue(BusinessLandmarksVisibleProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Controls.Maps.MapInteractionMode ZoomInteractionMode
		{
			get
			{
				return (global::Windows.UI.Xaml.Controls.Maps.MapInteractionMode)this.GetValue(ZoomInteractionModeProperty);
			}
			set
			{
				this.SetValue(ZoomInteractionModeProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  bool TransitFeaturesVisible
		{
			get
			{
				return (bool)this.GetValue(TransitFeaturesVisibleProperty);
			}
			set
			{
				this.SetValue(TransitFeaturesVisibleProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Controls.Maps.MapPanInteractionMode PanInteractionMode
		{
			get
			{
				return (global::Windows.UI.Xaml.Controls.Maps.MapPanInteractionMode)this.GetValue(PanInteractionModeProperty);
			}
			set
			{
				this.SetValue(PanInteractionModeProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Controls.Maps.MapInteractionMode TiltInteractionMode
		{
			get
			{
				return (global::Windows.UI.Xaml.Controls.Maps.MapInteractionMode)this.GetValue(TiltInteractionModeProperty);
			}
			set
			{
				this.SetValue(TiltInteractionModeProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Controls.Maps.MapScene Scene
		{
			get
			{
				return (global::Windows.UI.Xaml.Controls.Maps.MapScene)this.GetValue(SceneProperty);
			}
			set
			{
				this.SetValue(SceneProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Controls.Maps.MapInteractionMode RotateInteractionMode
		{
			get
			{
				return (global::Windows.UI.Xaml.Controls.Maps.MapInteractionMode)this.GetValue(RotateInteractionModeProperty);
			}
			set
			{
				this.SetValue(RotateInteractionModeProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  bool Is3DSupported
		{
			get
			{
				return (bool)this.GetValue(Is3DSupportedProperty);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  bool IsStreetsideSupported
		{
			get
			{
				return (bool)this.GetValue(IsStreetsideSupportedProperty);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Controls.Maps.MapCamera TargetCamera
		{
			get
			{
				throw new global::System.NotImplementedException("The member MapCamera MapControl.TargetCamera is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Controls.Maps.MapCamera ActualCamera
		{
			get
			{
				throw new global::System.NotImplementedException("The member MapCamera MapControl.ActualCamera is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  bool TransitFeaturesEnabled
		{
			get
			{
				return (bool)this.GetValue(TransitFeaturesEnabledProperty);
			}
			set
			{
				this.SetValue(TransitFeaturesEnabledProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  bool BusinessLandmarksEnabled
		{
			get
			{
				return (bool)this.GetValue(BusinessLandmarksEnabledProperty);
			}
			set
			{
				this.SetValue(BusinessLandmarksEnabledProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Thickness ViewPadding
		{
			get
			{
				return (global::Windows.UI.Xaml.Thickness)this.GetValue(ViewPaddingProperty);
			}
			set
			{
				this.SetValue(ViewPaddingProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Controls.Maps.MapStyleSheet StyleSheet
		{
			get
			{
				return (global::Windows.UI.Xaml.Controls.Maps.MapStyleSheet)this.GetValue(StyleSheetProperty);
			}
			set
			{
				this.SetValue(StyleSheetProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Controls.Maps.MapProjection MapProjection
		{
			get
			{
				return (global::Windows.UI.Xaml.Controls.Maps.MapProjection)this.GetValue(MapProjectionProperty);
			}
			set
			{
				this.SetValue(MapProjectionProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::System.Collections.Generic.IList<global::Windows.UI.Xaml.Controls.Maps.MapLayer> Layers
		{
			get
			{
				return (global::System.Collections.Generic.IList<global::Windows.UI.Xaml.Controls.Maps.MapLayer>)this.GetValue(LayersProperty);
			}
			set
			{
				this.SetValue(LayersProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  string Region
		{
			get
			{
				return (string)this.GetValue(RegionProperty);
			}
			set
			{
				this.SetValue(RegionProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty CenterProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"Center", typeof(global::Windows.Devices.Geolocation.Geopoint), 
			typeof(global::Windows.UI.Xaml.Controls.Maps.MapControl), 
			new FrameworkPropertyMetadata(default(global::Windows.Devices.Geolocation.Geopoint)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty ChildrenProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"Children", typeof(global::System.Collections.Generic.IList<global::Windows.UI.Xaml.DependencyObject>), 
			typeof(global::Windows.UI.Xaml.Controls.Maps.MapControl), 
			new FrameworkPropertyMetadata(default(global::System.Collections.Generic.IList<global::Windows.UI.Xaml.DependencyObject>)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty ColorSchemeProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"ColorScheme", typeof(global::Windows.UI.Xaml.Controls.Maps.MapColorScheme), 
			typeof(global::Windows.UI.Xaml.Controls.Maps.MapControl), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Controls.Maps.MapColorScheme)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty DesiredPitchProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"DesiredPitch", typeof(double), 
			typeof(global::Windows.UI.Xaml.Controls.Maps.MapControl), 
			new FrameworkPropertyMetadata(default(double)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty HeadingProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"Heading", typeof(double), 
			typeof(global::Windows.UI.Xaml.Controls.Maps.MapControl), 
			new FrameworkPropertyMetadata(default(double)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty LandmarksVisibleProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"LandmarksVisible", typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.Maps.MapControl), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty LocationProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.RegisterAttached(
			"Location", typeof(global::Windows.Devices.Geolocation.Geopoint), 
			typeof(global::Windows.UI.Xaml.Controls.Maps.MapControl), 
			new FrameworkPropertyMetadata(default(global::Windows.Devices.Geolocation.Geopoint)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty MapElementsProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"MapElements", typeof(global::System.Collections.Generic.IList<global::Windows.UI.Xaml.Controls.Maps.MapElement>), 
			typeof(global::Windows.UI.Xaml.Controls.Maps.MapControl), 
			new FrameworkPropertyMetadata(default(global::System.Collections.Generic.IList<global::Windows.UI.Xaml.Controls.Maps.MapElement>)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty MapServiceTokenProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"MapServiceToken", typeof(string), 
			typeof(global::Windows.UI.Xaml.Controls.Maps.MapControl), 
			new FrameworkPropertyMetadata(default(string)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty NormalizedAnchorPointProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.RegisterAttached(
			"NormalizedAnchorPoint", typeof(global::Windows.Foundation.Point), 
			typeof(global::Windows.UI.Xaml.Controls.Maps.MapControl), 
			new FrameworkPropertyMetadata(default(global::Windows.Foundation.Point)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty PedestrianFeaturesVisibleProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"PedestrianFeaturesVisible", typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.Maps.MapControl), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty PitchProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"Pitch", typeof(double), 
			typeof(global::Windows.UI.Xaml.Controls.Maps.MapControl), 
			new FrameworkPropertyMetadata(default(double)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty RoutesProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"Routes", typeof(global::System.Collections.Generic.IList<global::Windows.UI.Xaml.Controls.Maps.MapRouteView>), 
			typeof(global::Windows.UI.Xaml.Controls.Maps.MapControl), 
			new FrameworkPropertyMetadata(default(global::System.Collections.Generic.IList<global::Windows.UI.Xaml.Controls.Maps.MapRouteView>)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty StyleProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"Style", typeof(global::Windows.UI.Xaml.Controls.Maps.MapStyle), 
			typeof(global::Windows.UI.Xaml.Controls.Maps.MapControl), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Controls.Maps.MapStyle)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty TileSourcesProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"TileSources", typeof(global::System.Collections.Generic.IList<global::Windows.UI.Xaml.Controls.Maps.MapTileSource>), 
			typeof(global::Windows.UI.Xaml.Controls.Maps.MapControl), 
			new FrameworkPropertyMetadata(default(global::System.Collections.Generic.IList<global::Windows.UI.Xaml.Controls.Maps.MapTileSource>)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty TrafficFlowVisibleProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"TrafficFlowVisible", typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.Maps.MapControl), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty TransformOriginProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"TransformOrigin", typeof(global::Windows.Foundation.Point), 
			typeof(global::Windows.UI.Xaml.Controls.Maps.MapControl), 
			new FrameworkPropertyMetadata(default(global::Windows.Foundation.Point)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty WatermarkModeProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"WatermarkMode", typeof(global::Windows.UI.Xaml.Controls.Maps.MapWatermarkMode), 
			typeof(global::Windows.UI.Xaml.Controls.Maps.MapControl), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Controls.Maps.MapWatermarkMode)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty ZoomLevelProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"ZoomLevel", typeof(double), 
			typeof(global::Windows.UI.Xaml.Controls.Maps.MapControl), 
			new FrameworkPropertyMetadata(default(double)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty LoadingStatusProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"LoadingStatus", typeof(global::Windows.UI.Xaml.Controls.Maps.MapLoadingStatus), 
			typeof(global::Windows.UI.Xaml.Controls.Maps.MapControl), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Controls.Maps.MapLoadingStatus)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty BusinessLandmarksVisibleProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"BusinessLandmarksVisible", typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.Maps.MapControl), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty Is3DSupportedProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"Is3DSupported", typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.Maps.MapControl), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty PanInteractionModeProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"PanInteractionMode", typeof(global::Windows.UI.Xaml.Controls.Maps.MapPanInteractionMode), 
			typeof(global::Windows.UI.Xaml.Controls.Maps.MapControl), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Controls.Maps.MapPanInteractionMode)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty RotateInteractionModeProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"RotateInteractionMode", typeof(global::Windows.UI.Xaml.Controls.Maps.MapInteractionMode), 
			typeof(global::Windows.UI.Xaml.Controls.Maps.MapControl), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Controls.Maps.MapInteractionMode)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty TiltInteractionModeProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"TiltInteractionMode", typeof(global::Windows.UI.Xaml.Controls.Maps.MapInteractionMode), 
			typeof(global::Windows.UI.Xaml.Controls.Maps.MapControl), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Controls.Maps.MapInteractionMode)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty TransitFeaturesVisibleProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"TransitFeaturesVisible", typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.Maps.MapControl), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty ZoomInteractionModeProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"ZoomInteractionMode", typeof(global::Windows.UI.Xaml.Controls.Maps.MapInteractionMode), 
			typeof(global::Windows.UI.Xaml.Controls.Maps.MapControl), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Controls.Maps.MapInteractionMode)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty IsStreetsideSupportedProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"IsStreetsideSupported", typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.Maps.MapControl), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty SceneProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"Scene", typeof(global::Windows.UI.Xaml.Controls.Maps.MapScene), 
			typeof(global::Windows.UI.Xaml.Controls.Maps.MapControl), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Controls.Maps.MapScene)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty BusinessLandmarksEnabledProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"BusinessLandmarksEnabled", typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.Maps.MapControl), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty TransitFeaturesEnabledProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"TransitFeaturesEnabled", typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.Maps.MapControl), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty MapProjectionProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"MapProjection", typeof(global::Windows.UI.Xaml.Controls.Maps.MapProjection), 
			typeof(global::Windows.UI.Xaml.Controls.Maps.MapControl), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Controls.Maps.MapProjection)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty StyleSheetProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"StyleSheet", typeof(global::Windows.UI.Xaml.Controls.Maps.MapStyleSheet), 
			typeof(global::Windows.UI.Xaml.Controls.Maps.MapControl), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Controls.Maps.MapStyleSheet)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty ViewPaddingProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"ViewPadding", typeof(global::Windows.UI.Xaml.Thickness), 
			typeof(global::Windows.UI.Xaml.Controls.Maps.MapControl), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Thickness)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty LayersProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"Layers", typeof(global::System.Collections.Generic.IList<global::Windows.UI.Xaml.Controls.Maps.MapLayer>), 
			typeof(global::Windows.UI.Xaml.Controls.Maps.MapControl), 
			new FrameworkPropertyMetadata(default(global::System.Collections.Generic.IList<global::Windows.UI.Xaml.Controls.Maps.MapLayer>)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty RegionProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"Region", typeof(string), 
			typeof(global::Windows.UI.Xaml.Controls.Maps.MapControl), 
			new FrameworkPropertyMetadata(default(string)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public MapControl() : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Maps.MapControl", "MapControl.MapControl()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.MapControl()
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.Center.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.Center.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.Children.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.ColorScheme.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.ColorScheme.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.DesiredPitch.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.DesiredPitch.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.Heading.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.Heading.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.LandmarksVisible.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.LandmarksVisible.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.LoadingStatus.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.MapServiceToken.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.MapServiceToken.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.MaxZoomLevel.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.MinZoomLevel.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.PedestrianFeaturesVisible.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.PedestrianFeaturesVisible.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.Pitch.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.Style.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.Style.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.TrafficFlowVisible.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.TrafficFlowVisible.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.TransformOrigin.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.TransformOrigin.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.WatermarkMode.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.WatermarkMode.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.ZoomLevel.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.ZoomLevel.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.MapElements.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.Routes.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.TileSources.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.CenterChanged.add
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.CenterChanged.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.HeadingChanged.add
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.HeadingChanged.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.LoadingStatusChanged.add
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.LoadingStatusChanged.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.MapDoubleTapped.add
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.MapDoubleTapped.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.MapHolding.add
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.MapHolding.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.MapTapped.add
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.MapTapped.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.PitchChanged.add
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.PitchChanged.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.TransformOriginChanged.add
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.TransformOriginChanged.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.ZoomLevelChanged.add
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.ZoomLevelChanged.remove
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.UI.Xaml.Controls.Maps.MapElement> FindMapElementsAtOffset( global::Windows.Foundation.Point offset)
		{
			throw new global::System.NotImplementedException("The member IReadOnlyList<MapElement> MapControl.FindMapElementsAtOffset(Point offset) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  void GetLocationFromOffset( global::Windows.Foundation.Point offset, out global::Windows.Devices.Geolocation.Geopoint location)
		{
			throw new global::System.NotImplementedException("The member void MapControl.GetLocationFromOffset(Point offset, out Geopoint location) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  void GetOffsetFromLocation( global::Windows.Devices.Geolocation.Geopoint location, out global::Windows.Foundation.Point offset)
		{
			throw new global::System.NotImplementedException("The member void MapControl.GetOffsetFromLocation(Geopoint location, out Point offset) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  void IsLocationInView( global::Windows.Devices.Geolocation.Geopoint location, out bool isInView)
		{
			throw new global::System.NotImplementedException("The member void MapControl.IsLocationInView(Geopoint location, out bool isInView) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncOperation<bool> TrySetViewBoundsAsync( global::Windows.Devices.Geolocation.GeoboundingBox bounds,  global::Windows.UI.Xaml.Thickness? margin,  global::Windows.UI.Xaml.Controls.Maps.MapAnimationKind animation)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> MapControl.TrySetViewBoundsAsync(GeoboundingBox bounds, Thickness? margin, MapAnimationKind animation) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncOperation<bool> TrySetViewAsync( global::Windows.Devices.Geolocation.Geopoint center)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> MapControl.TrySetViewAsync(Geopoint center) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncOperation<bool> TrySetViewAsync( global::Windows.Devices.Geolocation.Geopoint center,  double? zoomLevel)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> MapControl.TrySetViewAsync(Geopoint center, double? zoomLevel) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncOperation<bool> TrySetViewAsync( global::Windows.Devices.Geolocation.Geopoint center,  double? zoomLevel,  double? heading,  double? desiredPitch)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> MapControl.TrySetViewAsync(Geopoint center, double? zoomLevel, double? heading, double? desiredPitch) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncOperation<bool> TrySetViewAsync( global::Windows.Devices.Geolocation.Geopoint center,  double? zoomLevel,  double? heading,  double? desiredPitch,  global::Windows.UI.Xaml.Controls.Maps.MapAnimationKind animation)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> MapControl.TrySetViewAsync(Geopoint center, double? zoomLevel, double? heading, double? desiredPitch, MapAnimationKind animation) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.BusinessLandmarksVisible.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.BusinessLandmarksVisible.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.TransitFeaturesVisible.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.TransitFeaturesVisible.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.PanInteractionMode.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.PanInteractionMode.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.RotateInteractionMode.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.RotateInteractionMode.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.TiltInteractionMode.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.TiltInteractionMode.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.ZoomInteractionMode.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.ZoomInteractionMode.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.Is3DSupported.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.IsStreetsideSupported.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.Scene.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.Scene.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.ActualCamera.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.TargetCamera.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.CustomExperience.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.CustomExperience.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.MapElementClick.add
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.MapElementClick.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.MapElementPointerEntered.add
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.MapElementPointerEntered.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.MapElementPointerExited.add
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.MapElementPointerExited.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.ActualCameraChanged.add
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.ActualCameraChanged.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.ActualCameraChanging.add
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.ActualCameraChanging.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.TargetCameraChanged.add
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.TargetCameraChanged.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.CustomExperienceChanged.add
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.CustomExperienceChanged.remove
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  void StartContinuousRotate( double rateInDegreesPerSecond)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Maps.MapControl", "void MapControl.StartContinuousRotate(double rateInDegreesPerSecond)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  void StopContinuousRotate()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Maps.MapControl", "void MapControl.StopContinuousRotate()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  void StartContinuousTilt( double rateInDegreesPerSecond)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Maps.MapControl", "void MapControl.StartContinuousTilt(double rateInDegreesPerSecond)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  void StopContinuousTilt()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Maps.MapControl", "void MapControl.StopContinuousTilt()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  void StartContinuousZoom( double rateOfChangePerSecond)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Maps.MapControl", "void MapControl.StartContinuousZoom(double rateOfChangePerSecond)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  void StopContinuousZoom()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Maps.MapControl", "void MapControl.StopContinuousZoom()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncOperation<bool> TryRotateAsync( double degrees)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> MapControl.TryRotateAsync(double degrees) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncOperation<bool> TryRotateToAsync( double angleInDegrees)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> MapControl.TryRotateToAsync(double angleInDegrees) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncOperation<bool> TryTiltAsync( double degrees)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> MapControl.TryTiltAsync(double degrees) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncOperation<bool> TryTiltToAsync( double angleInDegrees)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> MapControl.TryTiltToAsync(double angleInDegrees) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncOperation<bool> TryZoomInAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> MapControl.TryZoomInAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncOperation<bool> TryZoomOutAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> MapControl.TryZoomOutAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncOperation<bool> TryZoomToAsync( double zoomLevel)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> MapControl.TryZoomToAsync(double zoomLevel) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncOperation<bool> TrySetSceneAsync( global::Windows.UI.Xaml.Controls.Maps.MapScene scene)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> MapControl.TrySetSceneAsync(MapScene scene) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncOperation<bool> TrySetSceneAsync( global::Windows.UI.Xaml.Controls.Maps.MapScene scene,  global::Windows.UI.Xaml.Controls.Maps.MapAnimationKind animationKind)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> MapControl.TrySetSceneAsync(MapScene scene, MapAnimationKind animationKind) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.MapRightTapped.add
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.MapRightTapped.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.BusinessLandmarksEnabled.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.BusinessLandmarksEnabled.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.TransitFeaturesEnabled.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.TransitFeaturesEnabled.set
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::Windows.Devices.Geolocation.Geopath GetVisibleRegion( global::Windows.UI.Xaml.Controls.Maps.MapVisibleRegionKind region)
		{
			throw new global::System.NotImplementedException("The member Geopath MapControl.GetVisibleRegion(MapVisibleRegionKind region) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.MapProjection.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.MapProjection.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.StyleSheet.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.StyleSheet.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.ViewPadding.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.ViewPadding.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.MapContextRequested.add
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.MapContextRequested.remove
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.UI.Xaml.Controls.Maps.MapElement> FindMapElementsAtOffset( global::Windows.Foundation.Point offset,  double radius)
		{
			throw new global::System.NotImplementedException("The member IReadOnlyList<MapElement> MapControl.FindMapElementsAtOffset(Point offset, double radius) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  void GetLocationFromOffset( global::Windows.Foundation.Point offset,  global::Windows.Devices.Geolocation.AltitudeReferenceSystem desiredReferenceSystem, out global::Windows.Devices.Geolocation.Geopoint location)
		{
			throw new global::System.NotImplementedException("The member void MapControl.GetLocationFromOffset(Point offset, AltitudeReferenceSystem desiredReferenceSystem, out Geopoint location) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  void StartContinuousPan( double horizontalPixelsPerSecond,  double verticalPixelsPerSecond)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Maps.MapControl", "void MapControl.StartContinuousPan(double horizontalPixelsPerSecond, double verticalPixelsPerSecond)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  void StopContinuousPan()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Maps.MapControl", "void MapControl.StopContinuousPan()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncOperation<bool> TryPanAsync( double horizontalPixels,  double verticalPixels)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> MapControl.TryPanAsync(double horizontalPixels, double verticalPixels) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncOperation<bool> TryPanToAsync( global::Windows.Devices.Geolocation.Geopoint location)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> MapControl.TryPanToAsync(Geopoint location) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.Layers.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.Layers.set
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  bool TryGetLocationFromOffset( global::Windows.Foundation.Point offset, out global::Windows.Devices.Geolocation.Geopoint location)
		{
			throw new global::System.NotImplementedException("The member bool MapControl.TryGetLocationFromOffset(Point offset, out Geopoint location) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  bool TryGetLocationFromOffset( global::Windows.Foundation.Point offset,  global::Windows.Devices.Geolocation.AltitudeReferenceSystem desiredReferenceSystem, out global::Windows.Devices.Geolocation.Geopoint location)
		{
			throw new global::System.NotImplementedException("The member bool MapControl.TryGetLocationFromOffset(Point offset, AltitudeReferenceSystem desiredReferenceSystem, out Geopoint location) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.Region.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.Region.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.RegionProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.LayersProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.MapProjectionProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.StyleSheetProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.ViewPaddingProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.BusinessLandmarksEnabledProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.TransitFeaturesEnabledProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.BusinessLandmarksVisibleProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.TransitFeaturesVisibleProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.PanInteractionModeProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.RotateInteractionModeProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.TiltInteractionModeProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.ZoomInteractionModeProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.Is3DSupportedProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.IsStreetsideSupportedProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.SceneProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.CenterProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.ChildrenProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.ColorSchemeProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.DesiredPitchProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.HeadingProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.LandmarksVisibleProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.LoadingStatusProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.MapServiceTokenProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.PedestrianFeaturesVisibleProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.PitchProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.StyleProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.TrafficFlowVisibleProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.TransformOriginProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.WatermarkModeProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.ZoomLevelProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.MapElementsProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.RoutesProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.TileSourcesProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.LocationProperty.get
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.Devices.Geolocation.Geopoint GetLocation( global::Windows.UI.Xaml.DependencyObject element)
		{
			return (global::Windows.Devices.Geolocation.Geopoint)element.GetValue(LocationProperty);
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static void SetLocation( global::Windows.UI.Xaml.DependencyObject element,  global::Windows.Devices.Geolocation.Geopoint value)
		{
			element.SetValue(LocationProperty, value);
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapControl.NormalizedAnchorPointProperty.get
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.Foundation.Point GetNormalizedAnchorPoint( global::Windows.UI.Xaml.DependencyObject element)
		{
			return (global::Windows.Foundation.Point)element.GetValue(NormalizedAnchorPointProperty);
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static void SetNormalizedAnchorPoint( global::Windows.UI.Xaml.DependencyObject element,  global::Windows.Foundation.Point value)
		{
			element.SetValue(NormalizedAnchorPointProperty, value);
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Xaml.Controls.Maps.MapControl, object> CenterChanged
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Maps.MapControl", "event TypedEventHandler<MapControl, object> MapControl.CenterChanged");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Maps.MapControl", "event TypedEventHandler<MapControl, object> MapControl.CenterChanged");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Xaml.Controls.Maps.MapControl, object> HeadingChanged
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Maps.MapControl", "event TypedEventHandler<MapControl, object> MapControl.HeadingChanged");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Maps.MapControl", "event TypedEventHandler<MapControl, object> MapControl.HeadingChanged");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Xaml.Controls.Maps.MapControl, object> LoadingStatusChanged
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Maps.MapControl", "event TypedEventHandler<MapControl, object> MapControl.LoadingStatusChanged");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Maps.MapControl", "event TypedEventHandler<MapControl, object> MapControl.LoadingStatusChanged");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Xaml.Controls.Maps.MapControl, global::Windows.UI.Xaml.Controls.Maps.MapInputEventArgs> MapDoubleTapped
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Maps.MapControl", "event TypedEventHandler<MapControl, MapInputEventArgs> MapControl.MapDoubleTapped");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Maps.MapControl", "event TypedEventHandler<MapControl, MapInputEventArgs> MapControl.MapDoubleTapped");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Xaml.Controls.Maps.MapControl, global::Windows.UI.Xaml.Controls.Maps.MapInputEventArgs> MapHolding
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Maps.MapControl", "event TypedEventHandler<MapControl, MapInputEventArgs> MapControl.MapHolding");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Maps.MapControl", "event TypedEventHandler<MapControl, MapInputEventArgs> MapControl.MapHolding");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Xaml.Controls.Maps.MapControl, global::Windows.UI.Xaml.Controls.Maps.MapInputEventArgs> MapTapped
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Maps.MapControl", "event TypedEventHandler<MapControl, MapInputEventArgs> MapControl.MapTapped");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Maps.MapControl", "event TypedEventHandler<MapControl, MapInputEventArgs> MapControl.MapTapped");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Xaml.Controls.Maps.MapControl, object> PitchChanged
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Maps.MapControl", "event TypedEventHandler<MapControl, object> MapControl.PitchChanged");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Maps.MapControl", "event TypedEventHandler<MapControl, object> MapControl.PitchChanged");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Xaml.Controls.Maps.MapControl, object> TransformOriginChanged
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Maps.MapControl", "event TypedEventHandler<MapControl, object> MapControl.TransformOriginChanged");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Maps.MapControl", "event TypedEventHandler<MapControl, object> MapControl.TransformOriginChanged");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Xaml.Controls.Maps.MapControl, object> ZoomLevelChanged
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Maps.MapControl", "event TypedEventHandler<MapControl, object> MapControl.ZoomLevelChanged");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Maps.MapControl", "event TypedEventHandler<MapControl, object> MapControl.ZoomLevelChanged");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Xaml.Controls.Maps.MapControl, global::Windows.UI.Xaml.Controls.Maps.MapActualCameraChangedEventArgs> ActualCameraChanged
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Maps.MapControl", "event TypedEventHandler<MapControl, MapActualCameraChangedEventArgs> MapControl.ActualCameraChanged");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Maps.MapControl", "event TypedEventHandler<MapControl, MapActualCameraChangedEventArgs> MapControl.ActualCameraChanged");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Xaml.Controls.Maps.MapControl, global::Windows.UI.Xaml.Controls.Maps.MapActualCameraChangingEventArgs> ActualCameraChanging
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Maps.MapControl", "event TypedEventHandler<MapControl, MapActualCameraChangingEventArgs> MapControl.ActualCameraChanging");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Maps.MapControl", "event TypedEventHandler<MapControl, MapActualCameraChangingEventArgs> MapControl.ActualCameraChanging");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Xaml.Controls.Maps.MapControl, global::Windows.UI.Xaml.Controls.Maps.MapCustomExperienceChangedEventArgs> CustomExperienceChanged
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Maps.MapControl", "event TypedEventHandler<MapControl, MapCustomExperienceChangedEventArgs> MapControl.CustomExperienceChanged");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Maps.MapControl", "event TypedEventHandler<MapControl, MapCustomExperienceChangedEventArgs> MapControl.CustomExperienceChanged");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Xaml.Controls.Maps.MapControl, global::Windows.UI.Xaml.Controls.Maps.MapElementClickEventArgs> MapElementClick
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Maps.MapControl", "event TypedEventHandler<MapControl, MapElementClickEventArgs> MapControl.MapElementClick");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Maps.MapControl", "event TypedEventHandler<MapControl, MapElementClickEventArgs> MapControl.MapElementClick");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Xaml.Controls.Maps.MapControl, global::Windows.UI.Xaml.Controls.Maps.MapElementPointerEnteredEventArgs> MapElementPointerEntered
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Maps.MapControl", "event TypedEventHandler<MapControl, MapElementPointerEnteredEventArgs> MapControl.MapElementPointerEntered");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Maps.MapControl", "event TypedEventHandler<MapControl, MapElementPointerEnteredEventArgs> MapControl.MapElementPointerEntered");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Xaml.Controls.Maps.MapControl, global::Windows.UI.Xaml.Controls.Maps.MapElementPointerExitedEventArgs> MapElementPointerExited
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Maps.MapControl", "event TypedEventHandler<MapControl, MapElementPointerExitedEventArgs> MapControl.MapElementPointerExited");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Maps.MapControl", "event TypedEventHandler<MapControl, MapElementPointerExitedEventArgs> MapControl.MapElementPointerExited");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Xaml.Controls.Maps.MapControl, global::Windows.UI.Xaml.Controls.Maps.MapTargetCameraChangedEventArgs> TargetCameraChanged
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Maps.MapControl", "event TypedEventHandler<MapControl, MapTargetCameraChangedEventArgs> MapControl.TargetCameraChanged");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Maps.MapControl", "event TypedEventHandler<MapControl, MapTargetCameraChangedEventArgs> MapControl.TargetCameraChanged");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Xaml.Controls.Maps.MapControl, global::Windows.UI.Xaml.Controls.Maps.MapRightTappedEventArgs> MapRightTapped
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Maps.MapControl", "event TypedEventHandler<MapControl, MapRightTappedEventArgs> MapControl.MapRightTapped");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Maps.MapControl", "event TypedEventHandler<MapControl, MapRightTappedEventArgs> MapControl.MapRightTapped");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Xaml.Controls.Maps.MapControl, global::Windows.UI.Xaml.Controls.Maps.MapContextRequestedEventArgs> MapContextRequested
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Maps.MapControl", "event TypedEventHandler<MapControl, MapContextRequestedEventArgs> MapControl.MapContextRequested");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Maps.MapControl", "event TypedEventHandler<MapControl, MapContextRequestedEventArgs> MapControl.MapContextRequested");
			}
		}
		#endif
	}
}
