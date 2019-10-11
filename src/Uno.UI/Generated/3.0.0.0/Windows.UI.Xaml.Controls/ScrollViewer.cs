#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class ScrollViewer : global::Windows.UI.Xaml.Controls.ContentControl,global::Windows.UI.Xaml.Controls.IScrollAnchorProvider
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.UIElement CurrentAnchor
		{
			get
			{
				throw new global::System.NotImplementedException("The member UIElement ScrollViewer.CurrentAnchor is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  bool IsVerticalRailEnabled
		{
			get
			{
				return (bool)this.GetValue(IsVerticalRailEnabledProperty);
			}
			set
			{
				this.SetValue(IsVerticalRailEnabledProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  bool IsScrollInertiaEnabled
		{
			get
			{
				return (bool)this.GetValue(IsScrollInertiaEnabledProperty);
			}
			set
			{
				this.SetValue(IsScrollInertiaEnabledProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  bool IsHorizontalScrollChainingEnabled
		{
			get
			{
				return (bool)this.GetValue(IsHorizontalScrollChainingEnabledProperty);
			}
			set
			{
				this.SetValue(IsHorizontalScrollChainingEnabledProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  bool IsHorizontalRailEnabled
		{
			get
			{
				return (bool)this.GetValue(IsHorizontalRailEnabledProperty);
			}
			set
			{
				this.SetValue(IsHorizontalRailEnabledProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  bool IsDeferredScrollingEnabled
		{
			get
			{
				return (bool)this.GetValue(IsDeferredScrollingEnabledProperty);
			}
			set
			{
				this.SetValue(IsDeferredScrollingEnabledProperty, value);
			}
		}
		#endif
		// Skipping already declared property HorizontalSnapPointsType
		// Skipping already declared property HorizontalSnapPointsAlignment
		// Skipping already declared property HorizontalScrollMode
		// Skipping already declared property HorizontalScrollBarVisibility
		// Skipping already declared property MinZoomFactor
		// Skipping already declared property MaxZoomFactor
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  bool IsZoomInertiaEnabled
		{
			get
			{
				return (bool)this.GetValue(IsZoomInertiaEnabledProperty);
			}
			set
			{
				this.SetValue(IsZoomInertiaEnabledProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  bool IsZoomChainingEnabled
		{
			get
			{
				return (bool)this.GetValue(IsZoomChainingEnabledProperty);
			}
			set
			{
				this.SetValue(IsZoomChainingEnabledProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  bool IsVerticalScrollChainingEnabled
		{
			get
			{
				return (bool)this.GetValue(IsVerticalScrollChainingEnabledProperty);
			}
			set
			{
				this.SetValue(IsVerticalScrollChainingEnabledProperty, value);
			}
		}
		#endif
		// Skipping already declared property BringIntoViewOnFocusChange
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Controls.SnapPointsType ZoomSnapPointsType
		{
			get
			{
				return (global::Windows.UI.Xaml.Controls.SnapPointsType)this.GetValue(ZoomSnapPointsTypeProperty);
			}
			set
			{
				this.SetValue(ZoomSnapPointsTypeProperty, value);
			}
		}
		#endif
		// Skipping already declared property ZoomMode
		// Skipping already declared property VerticalSnapPointsType
		// Skipping already declared property VerticalSnapPointsAlignment
		// Skipping already declared property VerticalScrollMode
		// Skipping already declared property VerticalScrollBarVisibility
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Visibility ComputedHorizontalScrollBarVisibility
		{
			get
			{
				return (global::Windows.UI.Xaml.Visibility)this.GetValue(ComputedHorizontalScrollBarVisibilityProperty);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Visibility ComputedVerticalScrollBarVisibility
		{
			get
			{
				return (global::Windows.UI.Xaml.Visibility)this.GetValue(ComputedVerticalScrollBarVisibilityProperty);
			}
		}
		#endif
		// Skipping already declared property ExtentHeight
		// Skipping already declared property ExtentWidth
		#if false || false || false || false || false
		[global::Uno.NotImplemented]
		public  double HorizontalOffset
		{
			get
			{
				return (double)this.GetValue(HorizontalOffsetProperty);
			}
		}
		#endif
		// Skipping already declared property ScrollableWidth
		#if false || false || false || false || false
		[global::Uno.NotImplemented]
		public  double VerticalOffset
		{
			get
			{
				return (double)this.GetValue(VerticalOffsetProperty);
			}
		}
		#endif
		// Skipping already declared property ViewportHeight
		// Skipping already declared property ViewportWidth
		// Skipping already declared property ZoomFactor
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::System.Collections.Generic.IList<float> ZoomSnapPoints
		{
			get
			{
				return (global::System.Collections.Generic.IList<float>)this.GetValue(ZoomSnapPointsProperty);
			}
		}
		#endif
		// Skipping already declared property ScrollableHeight
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.UIElement TopLeftHeader
		{
			get
			{
				return (global::Windows.UI.Xaml.UIElement)this.GetValue(TopLeftHeaderProperty);
			}
			set
			{
				this.SetValue(TopLeftHeaderProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.UIElement TopHeader
		{
			get
			{
				return (global::Windows.UI.Xaml.UIElement)this.GetValue(TopHeaderProperty);
			}
			set
			{
				this.SetValue(TopHeaderProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.UIElement LeftHeader
		{
			get
			{
				return (global::Windows.UI.Xaml.UIElement)this.GetValue(LeftHeaderProperty);
			}
			set
			{
				this.SetValue(LeftHeaderProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  double VerticalAnchorRatio
		{
			get
			{
				return (double)this.GetValue(VerticalAnchorRatioProperty);
			}
			set
			{
				this.SetValue(VerticalAnchorRatioProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  bool ReduceViewportForCoreInputViewOcclusions
		{
			get
			{
				return (bool)this.GetValue(ReduceViewportForCoreInputViewOcclusionsProperty);
			}
			set
			{
				this.SetValue(ReduceViewportForCoreInputViewOcclusionsProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  double HorizontalAnchorRatio
		{
			get
			{
				return (double)this.GetValue(HorizontalAnchorRatioProperty);
			}
			set
			{
				this.SetValue(HorizontalAnchorRatioProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  bool CanContentRenderOutsideBounds
		{
			get
			{
				return (bool)this.GetValue(CanContentRenderOutsideBoundsProperty);
			}
			set
			{
				this.SetValue(CanContentRenderOutsideBoundsProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty IsScrollInertiaEnabledProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.RegisterAttached(
			"IsScrollInertiaEnabled", typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.ScrollViewer), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		// Skipping already declared property VerticalSnapPointsTypeProperty
		// Skipping already declared property ViewportHeightProperty
		// Skipping already declared property ViewportWidthProperty
		// Skipping already declared property ZoomFactorProperty
		// Skipping already declared property ZoomModeProperty
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty ZoomSnapPointsProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"ZoomSnapPoints", typeof(global::System.Collections.Generic.IList<float>), 
			typeof(global::Windows.UI.Xaml.Controls.ScrollViewer), 
			new FrameworkPropertyMetadata(default(global::System.Collections.Generic.IList<float>)));
		#endif
		// Skipping already declared property BringIntoViewOnFocusChangeProperty
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty ComputedHorizontalScrollBarVisibilityProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"ComputedHorizontalScrollBarVisibility", typeof(global::Windows.UI.Xaml.Visibility), 
			typeof(global::Windows.UI.Xaml.Controls.ScrollViewer), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Visibility)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty ComputedVerticalScrollBarVisibilityProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"ComputedVerticalScrollBarVisibility", typeof(global::Windows.UI.Xaml.Visibility), 
			typeof(global::Windows.UI.Xaml.Controls.ScrollViewer), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Visibility)));
		#endif
		// Skipping already declared property ExtentHeightProperty
		// Skipping already declared property ExtentWidthProperty
		#if false || false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty HorizontalOffsetProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"HorizontalOffset", typeof(double), 
			typeof(global::Windows.UI.Xaml.Controls.ScrollViewer), 
			new FrameworkPropertyMetadata(default(double)));
		#endif
		// Skipping already declared property HorizontalScrollBarVisibilityProperty
		// Skipping already declared property HorizontalScrollModeProperty
		// Skipping already declared property HorizontalSnapPointsAlignmentProperty
		// Skipping already declared property HorizontalSnapPointsTypeProperty
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty IsDeferredScrollingEnabledProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.RegisterAttached(
			"IsDeferredScrollingEnabled", typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.ScrollViewer), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty IsHorizontalRailEnabledProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.RegisterAttached(
			"IsHorizontalRailEnabled", typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.ScrollViewer), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty IsHorizontalScrollChainingEnabledProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.RegisterAttached(
			"IsHorizontalScrollChainingEnabled", typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.ScrollViewer), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty ZoomSnapPointsTypeProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"ZoomSnapPointsType", typeof(global::Windows.UI.Xaml.Controls.SnapPointsType), 
			typeof(global::Windows.UI.Xaml.Controls.ScrollViewer), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Controls.SnapPointsType)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty IsVerticalRailEnabledProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.RegisterAttached(
			"IsVerticalRailEnabled", typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.ScrollViewer), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty IsVerticalScrollChainingEnabledProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.RegisterAttached(
			"IsVerticalScrollChainingEnabled", typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.ScrollViewer), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty IsZoomChainingEnabledProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.RegisterAttached(
			"IsZoomChainingEnabled", typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.ScrollViewer), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty IsZoomInertiaEnabledProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.RegisterAttached(
			"IsZoomInertiaEnabled", typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.ScrollViewer), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		// Skipping already declared property MaxZoomFactorProperty
		// Skipping already declared property MinZoomFactorProperty
		// Skipping already declared property ScrollableHeightProperty
		// Skipping already declared property ScrollableWidthProperty
		#if false || false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty VerticalOffsetProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"VerticalOffset", typeof(double), 
			typeof(global::Windows.UI.Xaml.Controls.ScrollViewer), 
			new FrameworkPropertyMetadata(default(double)));
		#endif
		// Skipping already declared property VerticalScrollBarVisibilityProperty
		// Skipping already declared property VerticalScrollModeProperty
		// Skipping already declared property VerticalSnapPointsAlignmentProperty
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty LeftHeaderProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"LeftHeader", typeof(global::Windows.UI.Xaml.UIElement), 
			typeof(global::Windows.UI.Xaml.Controls.ScrollViewer), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.UIElement)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty TopHeaderProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"TopHeader", typeof(global::Windows.UI.Xaml.UIElement), 
			typeof(global::Windows.UI.Xaml.Controls.ScrollViewer), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.UIElement)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty TopLeftHeaderProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"TopLeftHeader", typeof(global::Windows.UI.Xaml.UIElement), 
			typeof(global::Windows.UI.Xaml.Controls.ScrollViewer), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.UIElement)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty HorizontalAnchorRatioProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"HorizontalAnchorRatio", typeof(double), 
			typeof(global::Windows.UI.Xaml.Controls.ScrollViewer), 
			new FrameworkPropertyMetadata(default(double)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty ReduceViewportForCoreInputViewOcclusionsProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"ReduceViewportForCoreInputViewOcclusions", typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.ScrollViewer), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty VerticalAnchorRatioProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"VerticalAnchorRatio", typeof(double), 
			typeof(global::Windows.UI.Xaml.Controls.ScrollViewer), 
			new FrameworkPropertyMetadata(default(double)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty CanContentRenderOutsideBoundsProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.RegisterAttached(
			"CanContentRenderOutsideBounds", typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.ScrollViewer), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		// Skipping already declared method Windows.UI.Xaml.Controls.ScrollViewer.ScrollViewer()
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.ScrollViewer()
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.HorizontalScrollBarVisibility.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.HorizontalScrollBarVisibility.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.VerticalScrollBarVisibility.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.VerticalScrollBarVisibility.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.IsHorizontalRailEnabled.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.IsHorizontalRailEnabled.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.IsVerticalRailEnabled.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.IsVerticalRailEnabled.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.IsHorizontalScrollChainingEnabled.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.IsHorizontalScrollChainingEnabled.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.IsVerticalScrollChainingEnabled.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.IsVerticalScrollChainingEnabled.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.IsZoomChainingEnabled.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.IsZoomChainingEnabled.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.IsScrollInertiaEnabled.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.IsScrollInertiaEnabled.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.IsZoomInertiaEnabled.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.IsZoomInertiaEnabled.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.HorizontalScrollMode.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.HorizontalScrollMode.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.VerticalScrollMode.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.VerticalScrollMode.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.ZoomMode.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.ZoomMode.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.HorizontalSnapPointsAlignment.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.HorizontalSnapPointsAlignment.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.VerticalSnapPointsAlignment.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.VerticalSnapPointsAlignment.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.HorizontalSnapPointsType.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.HorizontalSnapPointsType.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.VerticalSnapPointsType.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.VerticalSnapPointsType.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.ZoomSnapPointsType.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.ZoomSnapPointsType.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.HorizontalOffset.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.ViewportWidth.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.ScrollableWidth.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.ComputedHorizontalScrollBarVisibility.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.ExtentWidth.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.VerticalOffset.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.ViewportHeight.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.ScrollableHeight.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.ComputedVerticalScrollBarVisibility.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.ExtentHeight.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.MinZoomFactor.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.MinZoomFactor.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.MaxZoomFactor.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.MaxZoomFactor.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.ZoomFactor.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.ZoomSnapPoints.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.ViewChanged.add
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.ViewChanged.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  void ScrollToHorizontalOffset( double offset)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.ScrollViewer", "void ScrollViewer.ScrollToHorizontalOffset(double offset)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  void ScrollToVerticalOffset( double offset)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.ScrollViewer", "void ScrollViewer.ScrollToVerticalOffset(double offset)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  void ZoomToFactor( float factor)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.ScrollViewer", "void ScrollViewer.ZoomToFactor(float factor)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  void InvalidateScrollInfo()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.ScrollViewer", "void ScrollViewer.InvalidateScrollInfo()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.IsDeferredScrollingEnabled.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.IsDeferredScrollingEnabled.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.BringIntoViewOnFocusChange.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.BringIntoViewOnFocusChange.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.TopLeftHeader.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.TopLeftHeader.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.LeftHeader.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.LeftHeader.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.TopHeader.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.TopHeader.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.ViewChanging.add
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.ViewChanging.remove
		#if false || false || NET461 || false || false
		[global::Uno.NotImplemented]
		public  bool ChangeView( double? horizontalOffset,  double? verticalOffset,  float? zoomFactor)
		{
			throw new global::System.NotImplementedException("The member bool ScrollViewer.ChangeView(double? horizontalOffset, double? verticalOffset, float? zoomFactor) is not implemented in Uno.");
		}
		#endif
		#if false || false || NET461 || false || false
		[global::Uno.NotImplemented]
		public  bool ChangeView( double? horizontalOffset,  double? verticalOffset,  float? zoomFactor,  bool disableAnimation)
		{
			throw new global::System.NotImplementedException("The member bool ScrollViewer.ChangeView(double? horizontalOffset, double? verticalOffset, float? zoomFactor, bool disableAnimation) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.DirectManipulationStarted.add
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.DirectManipulationStarted.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.DirectManipulationCompleted.add
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.DirectManipulationCompleted.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.ReduceViewportForCoreInputViewOcclusions.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.ReduceViewportForCoreInputViewOcclusions.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.HorizontalAnchorRatio.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.HorizontalAnchorRatio.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.VerticalAnchorRatio.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.VerticalAnchorRatio.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.CanContentRenderOutsideBounds.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.CanContentRenderOutsideBounds.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.AnchorRequested.add
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.AnchorRequested.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.CurrentAnchor.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  void RegisterAnchorCandidate( global::Windows.UI.Xaml.UIElement element)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.ScrollViewer", "void ScrollViewer.RegisterAnchorCandidate(UIElement element)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  void UnregisterAnchorCandidate( global::Windows.UI.Xaml.UIElement element)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.ScrollViewer", "void ScrollViewer.UnregisterAnchorCandidate(UIElement element)");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.ReduceViewportForCoreInputViewOcclusionsProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.HorizontalAnchorRatioProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.VerticalAnchorRatioProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.CanContentRenderOutsideBoundsProperty.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static bool GetCanContentRenderOutsideBounds( global::Windows.UI.Xaml.DependencyObject element)
		{
			return (bool)element.GetValue(CanContentRenderOutsideBoundsProperty);
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static void SetCanContentRenderOutsideBounds( global::Windows.UI.Xaml.DependencyObject element,  bool canContentRenderOutsideBounds)
		{
			element.SetValue(CanContentRenderOutsideBoundsProperty, canContentRenderOutsideBounds);
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.TopLeftHeaderProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.LeftHeaderProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.TopHeaderProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.HorizontalSnapPointsAlignmentProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.VerticalSnapPointsAlignmentProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.HorizontalSnapPointsTypeProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.VerticalSnapPointsTypeProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.ZoomSnapPointsTypeProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.HorizontalOffsetProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.ViewportWidthProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.ScrollableWidthProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.ComputedHorizontalScrollBarVisibilityProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.ExtentWidthProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.VerticalOffsetProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.ViewportHeightProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.ScrollableHeightProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.ComputedVerticalScrollBarVisibilityProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.ExtentHeightProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.MinZoomFactorProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.MaxZoomFactorProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.ZoomFactorProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.ZoomSnapPointsProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.HorizontalScrollBarVisibilityProperty.get
		// Skipping already declared method Windows.UI.Xaml.Controls.ScrollViewer.GetHorizontalScrollBarVisibility(Windows.UI.Xaml.DependencyObject)
		// Skipping already declared method Windows.UI.Xaml.Controls.ScrollViewer.SetHorizontalScrollBarVisibility(Windows.UI.Xaml.DependencyObject, Windows.UI.Xaml.Controls.ScrollBarVisibility)
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.VerticalScrollBarVisibilityProperty.get
		// Skipping already declared method Windows.UI.Xaml.Controls.ScrollViewer.GetVerticalScrollBarVisibility(Windows.UI.Xaml.DependencyObject)
		// Skipping already declared method Windows.UI.Xaml.Controls.ScrollViewer.SetVerticalScrollBarVisibility(Windows.UI.Xaml.DependencyObject, Windows.UI.Xaml.Controls.ScrollBarVisibility)
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.IsHorizontalRailEnabledProperty.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static bool GetIsHorizontalRailEnabled( global::Windows.UI.Xaml.DependencyObject element)
		{
			return (bool)element.GetValue(IsHorizontalRailEnabledProperty);
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static void SetIsHorizontalRailEnabled( global::Windows.UI.Xaml.DependencyObject element,  bool isHorizontalRailEnabled)
		{
			element.SetValue(IsHorizontalRailEnabledProperty, isHorizontalRailEnabled);
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.IsVerticalRailEnabledProperty.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static bool GetIsVerticalRailEnabled( global::Windows.UI.Xaml.DependencyObject element)
		{
			return (bool)element.GetValue(IsVerticalRailEnabledProperty);
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static void SetIsVerticalRailEnabled( global::Windows.UI.Xaml.DependencyObject element,  bool isVerticalRailEnabled)
		{
			element.SetValue(IsVerticalRailEnabledProperty, isVerticalRailEnabled);
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.IsHorizontalScrollChainingEnabledProperty.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static bool GetIsHorizontalScrollChainingEnabled( global::Windows.UI.Xaml.DependencyObject element)
		{
			return (bool)element.GetValue(IsHorizontalScrollChainingEnabledProperty);
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static void SetIsHorizontalScrollChainingEnabled( global::Windows.UI.Xaml.DependencyObject element,  bool isHorizontalScrollChainingEnabled)
		{
			element.SetValue(IsHorizontalScrollChainingEnabledProperty, isHorizontalScrollChainingEnabled);
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.IsVerticalScrollChainingEnabledProperty.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static bool GetIsVerticalScrollChainingEnabled( global::Windows.UI.Xaml.DependencyObject element)
		{
			return (bool)element.GetValue(IsVerticalScrollChainingEnabledProperty);
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static void SetIsVerticalScrollChainingEnabled( global::Windows.UI.Xaml.DependencyObject element,  bool isVerticalScrollChainingEnabled)
		{
			element.SetValue(IsVerticalScrollChainingEnabledProperty, isVerticalScrollChainingEnabled);
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.IsZoomChainingEnabledProperty.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static bool GetIsZoomChainingEnabled( global::Windows.UI.Xaml.DependencyObject element)
		{
			return (bool)element.GetValue(IsZoomChainingEnabledProperty);
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static void SetIsZoomChainingEnabled( global::Windows.UI.Xaml.DependencyObject element,  bool isZoomChainingEnabled)
		{
			element.SetValue(IsZoomChainingEnabledProperty, isZoomChainingEnabled);
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.IsScrollInertiaEnabledProperty.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static bool GetIsScrollInertiaEnabled( global::Windows.UI.Xaml.DependencyObject element)
		{
			return (bool)element.GetValue(IsScrollInertiaEnabledProperty);
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static void SetIsScrollInertiaEnabled( global::Windows.UI.Xaml.DependencyObject element,  bool isScrollInertiaEnabled)
		{
			element.SetValue(IsScrollInertiaEnabledProperty, isScrollInertiaEnabled);
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.IsZoomInertiaEnabledProperty.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static bool GetIsZoomInertiaEnabled( global::Windows.UI.Xaml.DependencyObject element)
		{
			return (bool)element.GetValue(IsZoomInertiaEnabledProperty);
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static void SetIsZoomInertiaEnabled( global::Windows.UI.Xaml.DependencyObject element,  bool isZoomInertiaEnabled)
		{
			element.SetValue(IsZoomInertiaEnabledProperty, isZoomInertiaEnabled);
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.HorizontalScrollModeProperty.get
		// Skipping already declared method Windows.UI.Xaml.Controls.ScrollViewer.GetHorizontalScrollMode(Windows.UI.Xaml.DependencyObject)
		// Skipping already declared method Windows.UI.Xaml.Controls.ScrollViewer.SetHorizontalScrollMode(Windows.UI.Xaml.DependencyObject, Windows.UI.Xaml.Controls.ScrollMode)
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.VerticalScrollModeProperty.get
		// Skipping already declared method Windows.UI.Xaml.Controls.ScrollViewer.GetVerticalScrollMode(Windows.UI.Xaml.DependencyObject)
		// Skipping already declared method Windows.UI.Xaml.Controls.ScrollViewer.SetVerticalScrollMode(Windows.UI.Xaml.DependencyObject, Windows.UI.Xaml.Controls.ScrollMode)
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.ZoomModeProperty.get
		#if false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.Controls.ZoomMode GetZoomMode( global::Windows.UI.Xaml.DependencyObject element)
		{
			return (global::Windows.UI.Xaml.Controls.ZoomMode)element.GetValue(ZoomModeProperty);
		}
		#endif
		#if false
		[global::Uno.NotImplemented]
		public static void SetZoomMode( global::Windows.UI.Xaml.DependencyObject element,  global::Windows.UI.Xaml.Controls.ZoomMode zoomMode)
		{
			element.SetValue(ZoomModeProperty, zoomMode);
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.IsDeferredScrollingEnabledProperty.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static bool GetIsDeferredScrollingEnabled( global::Windows.UI.Xaml.DependencyObject element)
		{
			return (bool)element.GetValue(IsDeferredScrollingEnabledProperty);
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static void SetIsDeferredScrollingEnabled( global::Windows.UI.Xaml.DependencyObject element,  bool isDeferredScrollingEnabled)
		{
			element.SetValue(IsDeferredScrollingEnabledProperty, isDeferredScrollingEnabled);
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.ScrollViewer.BringIntoViewOnFocusChangeProperty.get
		#if false
		[global::Uno.NotImplemented]
		public static bool GetBringIntoViewOnFocusChange( global::Windows.UI.Xaml.DependencyObject element)
		{
			return (bool)element.GetValue(BringIntoViewOnFocusChangeProperty);
		}
		#endif
		#if false
		[global::Uno.NotImplemented]
		public static void SetBringIntoViewOnFocusChange( global::Windows.UI.Xaml.DependencyObject element,  bool bringIntoViewOnFocusChange)
		{
			element.SetValue(BringIntoViewOnFocusChangeProperty, bringIntoViewOnFocusChange);
		}
		#endif
		// Skipping already declared event Windows.UI.Xaml.Controls.ScrollViewer.ViewChanged
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::System.EventHandler<global::Windows.UI.Xaml.Controls.ScrollViewerViewChangingEventArgs> ViewChanging
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.ScrollViewer", "event EventHandler<ScrollViewerViewChangingEventArgs> ScrollViewer.ViewChanging");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.ScrollViewer", "event EventHandler<ScrollViewerViewChangingEventArgs> ScrollViewer.ViewChanging");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::System.EventHandler<object> DirectManipulationCompleted
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.ScrollViewer", "event EventHandler<object> ScrollViewer.DirectManipulationCompleted");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.ScrollViewer", "event EventHandler<object> ScrollViewer.DirectManipulationCompleted");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::System.EventHandler<object> DirectManipulationStarted
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.ScrollViewer", "event EventHandler<object> ScrollViewer.DirectManipulationStarted");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.ScrollViewer", "event EventHandler<object> ScrollViewer.DirectManipulationStarted");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Xaml.Controls.ScrollViewer, global::Windows.UI.Xaml.Controls.AnchorRequestedEventArgs> AnchorRequested
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.ScrollViewer", "event TypedEventHandler<ScrollViewer, AnchorRequestedEventArgs> ScrollViewer.AnchorRequested");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.ScrollViewer", "event TypedEventHandler<ScrollViewer, AnchorRequestedEventArgs> ScrollViewer.AnchorRequested");
			}
		}
		#endif
		// Processing: Windows.UI.Xaml.Controls.IScrollAnchorProvider
	}
}
