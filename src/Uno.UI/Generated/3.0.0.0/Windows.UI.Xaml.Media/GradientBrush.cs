#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Media
{
	#if false || false || false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class GradientBrush : global::Windows.UI.Xaml.Media.Brush
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Xaml.Media.GradientSpreadMethod SpreadMethod
		{
			get
			{
				return (global::Windows.UI.Xaml.Media.GradientSpreadMethod)this.GetValue(SpreadMethodProperty);
			}
			set
			{
				this.SetValue(SpreadMethodProperty, value);
			}
		}
		#endif
		// Skipping already declared property MappingMode
		// Skipping already declared property GradientStops
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Xaml.Media.ColorInterpolationMode ColorInterpolationMode
		{
			get
			{
				return (global::Windows.UI.Xaml.Media.ColorInterpolationMode)this.GetValue(ColorInterpolationModeProperty);
			}
			set
			{
				this.SetValue(ColorInterpolationModeProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty ColorInterpolationModeProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(ColorInterpolationMode), typeof(global::Windows.UI.Xaml.Media.ColorInterpolationMode), 
			typeof(global::Windows.UI.Xaml.Media.GradientBrush), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Media.ColorInterpolationMode)));
		#endif
		// Skipping already declared property GradientStopsProperty
		// Skipping already declared property MappingModeProperty
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty SpreadMethodProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(SpreadMethod), typeof(global::Windows.UI.Xaml.Media.GradientSpreadMethod), 
			typeof(global::Windows.UI.Xaml.Media.GradientBrush), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Media.GradientSpreadMethod)));
		#endif
		// Skipping already declared method Windows.UI.Xaml.Media.GradientBrush.GradientBrush()
		// Forced skipping of method Windows.UI.Xaml.Media.GradientBrush.GradientBrush()
		// Forced skipping of method Windows.UI.Xaml.Media.GradientBrush.SpreadMethod.get
		// Forced skipping of method Windows.UI.Xaml.Media.GradientBrush.SpreadMethod.set
		// Forced skipping of method Windows.UI.Xaml.Media.GradientBrush.MappingMode.get
		// Forced skipping of method Windows.UI.Xaml.Media.GradientBrush.MappingMode.set
		// Forced skipping of method Windows.UI.Xaml.Media.GradientBrush.ColorInterpolationMode.get
		// Forced skipping of method Windows.UI.Xaml.Media.GradientBrush.ColorInterpolationMode.set
		// Forced skipping of method Windows.UI.Xaml.Media.GradientBrush.GradientStops.get
		// Forced skipping of method Windows.UI.Xaml.Media.GradientBrush.GradientStops.set
		// Forced skipping of method Windows.UI.Xaml.Media.GradientBrush.SpreadMethodProperty.get
		// Forced skipping of method Windows.UI.Xaml.Media.GradientBrush.MappingModeProperty.get
		// Forced skipping of method Windows.UI.Xaml.Media.GradientBrush.ColorInterpolationModeProperty.get
		// Forced skipping of method Windows.UI.Xaml.Media.GradientBrush.GradientStopsProperty.get
	}
}
