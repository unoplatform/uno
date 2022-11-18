#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls.Primitives
{
	#if false || false || false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class Popup : global::Windows.UI.Xaml.FrameworkElement
	{
		// Skipping already declared property VerticalOffset
		// Skipping already declared property IsOpen
		// Skipping already declared property IsLightDismissEnabled
		// Skipping already declared property HorizontalOffset
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Xaml.Media.Animation.TransitionCollection ChildTransitions
		{
			get
			{
				return (global::Windows.UI.Xaml.Media.Animation.TransitionCollection)this.GetValue(ChildTransitionsProperty);
			}
			set
			{
				this.SetValue(ChildTransitionsProperty, value);
			}
		}
		#endif
		// Skipping already declared property Child
		// Skipping already declared property LightDismissOverlayMode
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool ShouldConstrainToRootBounds
		{
			get
			{
				return (bool)this.GetValue(ShouldConstrainToRootBoundsProperty);
			}
			set
			{
				this.SetValue(ShouldConstrainToRootBoundsProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsConstrainedToRootBounds
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool Popup.IsConstrainedToRootBounds is not implemented in Uno.");
			}
		}
		#endif
		#if false
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Xaml.FrameworkElement PlacementTarget
		{
			get
			{
				return (global::Windows.UI.Xaml.FrameworkElement)this.GetValue(PlacementTargetProperty);
			}
			set
			{
				this.SetValue(PlacementTargetProperty, value);
			}
		}
		#endif
		#if false
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Xaml.Controls.Primitives.PopupPlacementMode DesiredPlacement
		{
			get
			{
				return (global::Windows.UI.Xaml.Controls.Primitives.PopupPlacementMode)this.GetValue(DesiredPlacementProperty);
			}
			set
			{
				this.SetValue(DesiredPlacementProperty, value);
			}
		}
		#endif
		#if false
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Xaml.Controls.Primitives.PopupPlacementMode ActualPlacement
		{
			get
			{
				throw new global::System.NotImplementedException("The member PopupPlacementMode Popup.ActualPlacement is not implemented in Uno.");
			}
		}
		#endif
		// Skipping already declared property ChildProperty
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty ChildTransitionsProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(ChildTransitions), typeof(global::Windows.UI.Xaml.Media.Animation.TransitionCollection), 
			typeof(global::Windows.UI.Xaml.Controls.Primitives.Popup), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Media.Animation.TransitionCollection)));
		#endif
		// Skipping already declared property HorizontalOffsetProperty
		// Skipping already declared property IsLightDismissEnabledProperty
		// Skipping already declared property IsOpenProperty
		// Skipping already declared property VerticalOffsetProperty
		// Skipping already declared property LightDismissOverlayModeProperty
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty ShouldConstrainToRootBoundsProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(ShouldConstrainToRootBounds), typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.Primitives.Popup), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if false
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty DesiredPlacementProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(DesiredPlacement), typeof(global::Windows.UI.Xaml.Controls.Primitives.PopupPlacementMode), 
			typeof(global::Windows.UI.Xaml.Controls.Primitives.Popup), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Controls.Primitives.PopupPlacementMode)));
		#endif
		#if false
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty PlacementTargetProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(PlacementTarget), typeof(global::Windows.UI.Xaml.FrameworkElement), 
			typeof(global::Windows.UI.Xaml.Controls.Primitives.Popup), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.FrameworkElement)));
		#endif
		// Skipping already declared method Windows.UI.Xaml.Controls.Primitives.Popup.Popup()
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.Popup.Popup()
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.Popup.Child.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.Popup.Child.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.Popup.IsOpen.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.Popup.IsOpen.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.Popup.HorizontalOffset.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.Popup.HorizontalOffset.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.Popup.VerticalOffset.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.Popup.VerticalOffset.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.Popup.ChildTransitions.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.Popup.ChildTransitions.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.Popup.IsLightDismissEnabled.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.Popup.IsLightDismissEnabled.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.Popup.Opened.add
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.Popup.Opened.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.Popup.Closed.add
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.Popup.Closed.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.Popup.LightDismissOverlayMode.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.Popup.LightDismissOverlayMode.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.Popup.ShouldConstrainToRootBounds.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.Popup.ShouldConstrainToRootBounds.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.Popup.IsConstrainedToRootBounds.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.Popup.PlacementTarget.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.Popup.PlacementTarget.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.Popup.DesiredPlacement.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.Popup.DesiredPlacement.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.Popup.ActualPlacement.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.Popup.ActualPlacementChanged.add
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.Popup.ActualPlacementChanged.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.Popup.PlacementTargetProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.Popup.DesiredPlacementProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.Popup.ShouldConstrainToRootBoundsProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.Popup.LightDismissOverlayModeProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.Popup.ChildProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.Popup.IsOpenProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.Popup.HorizontalOffsetProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.Popup.VerticalOffsetProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.Popup.ChildTransitionsProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.Popup.IsLightDismissEnabledProperty.get
		// Skipping already declared event Windows.UI.Xaml.Controls.Primitives.Popup.Closed
		// Skipping already declared event Windows.UI.Xaml.Controls.Primitives.Popup.Opened
		#if false
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::System.EventHandler<object> ActualPlacementChanged
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Primitives.Popup", "event EventHandler<object> Popup.ActualPlacementChanged");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Primitives.Popup", "event EventHandler<object> Popup.ActualPlacementChanged");
			}
		}
		#endif
	}
}
