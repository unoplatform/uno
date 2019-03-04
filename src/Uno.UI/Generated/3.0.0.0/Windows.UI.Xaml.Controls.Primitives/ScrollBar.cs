#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls.Primitives
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ScrollBar : global::Windows.UI.Xaml.Controls.Primitives.RangeBase
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  double ViewportSize
		{
			get
			{
				return (double)this.GetValue(ViewportSizeProperty);
			}
			set
			{
				this.SetValue(ViewportSizeProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Controls.Orientation Orientation
		{
			get
			{
				return (global::Windows.UI.Xaml.Controls.Orientation)this.GetValue(OrientationProperty);
			}
			set
			{
				this.SetValue(OrientationProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Controls.Primitives.ScrollingIndicatorMode IndicatorMode
		{
			get
			{
				return (global::Windows.UI.Xaml.Controls.Primitives.ScrollingIndicatorMode)this.GetValue(IndicatorModeProperty);
			}
			set
			{
				this.SetValue(IndicatorModeProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty IndicatorModeProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"IndicatorMode", typeof(global::Windows.UI.Xaml.Controls.Primitives.ScrollingIndicatorMode), 
			typeof(global::Windows.UI.Xaml.Controls.Primitives.ScrollBar), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Controls.Primitives.ScrollingIndicatorMode)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty OrientationProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"Orientation", typeof(global::Windows.UI.Xaml.Controls.Orientation), 
			typeof(global::Windows.UI.Xaml.Controls.Primitives.ScrollBar), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Controls.Orientation)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty ViewportSizeProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"ViewportSize", typeof(double), 
			typeof(global::Windows.UI.Xaml.Controls.Primitives.ScrollBar), 
			new FrameworkPropertyMetadata(default(double)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public ScrollBar() : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Primitives.ScrollBar", "ScrollBar.ScrollBar()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.ScrollBar.ScrollBar()
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.ScrollBar.Orientation.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.ScrollBar.Orientation.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.ScrollBar.ViewportSize.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.ScrollBar.ViewportSize.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.ScrollBar.IndicatorMode.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.ScrollBar.IndicatorMode.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.ScrollBar.Scroll.add
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.ScrollBar.Scroll.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.ScrollBar.OrientationProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.ScrollBar.ViewportSizeProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.ScrollBar.IndicatorModeProperty.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.UI.Xaml.Controls.Primitives.ScrollEventHandler Scroll
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Primitives.ScrollBar", "event ScrollEventHandler ScrollBar.Scroll");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Primitives.ScrollBar", "event ScrollEventHandler ScrollBar.Scroll");
			}
		}
		#endif
	}
}
