#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Microsoft.UI.Xaml.Controls.Primitives
{
	#if false || false || false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class CornerRadiusFilterConverter : global::Microsoft.UI.Xaml.DependencyObject,global::Microsoft.UI.Xaml.Data.IValueConverter
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  double Scale
		{
			get
			{
				return (double)this.GetValue(ScaleProperty);
			}
			set
			{
				this.SetValue(ScaleProperty, value);
			}
		}
		#endif
		// Skipping already declared property Filter
		// Skipping already declared property FilterProperty
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Microsoft.UI.Xaml.DependencyProperty ScaleProperty { get; } = 
		Microsoft.UI.Xaml.DependencyProperty.Register(
			nameof(Scale), typeof(double), 
			typeof(global::Microsoft.UI.Xaml.Controls.Primitives.CornerRadiusFilterConverter), 
			new FrameworkPropertyMetadata(default(double)));
		#endif
		// Skipping already declared method Microsoft.UI.Xaml.Controls.Primitives.CornerRadiusFilterConverter.CornerRadiusFilterConverter()
		// Forced skipping of method Microsoft.UI.Xaml.Controls.Primitives.CornerRadiusFilterConverter.CornerRadiusFilterConverter()
		// Forced skipping of method Microsoft.UI.Xaml.Controls.Primitives.CornerRadiusFilterConverter.Filter.get
		// Forced skipping of method Microsoft.UI.Xaml.Controls.Primitives.CornerRadiusFilterConverter.Filter.set
		// Forced skipping of method Microsoft.UI.Xaml.Controls.Primitives.CornerRadiusFilterConverter.Scale.get
		// Forced skipping of method Microsoft.UI.Xaml.Controls.Primitives.CornerRadiusFilterConverter.Scale.set
		// Skipping already declared method Microsoft.UI.Xaml.Controls.Primitives.CornerRadiusFilterConverter.Convert(object, System.Type, object, string)
		// Skipping already declared method Microsoft.UI.Xaml.Controls.Primitives.CornerRadiusFilterConverter.ConvertBack(object, System.Type, object, string)
		// Forced skipping of method Microsoft.UI.Xaml.Controls.Primitives.CornerRadiusFilterConverter.FilterProperty.get
		// Forced skipping of method Microsoft.UI.Xaml.Controls.Primitives.CornerRadiusFilterConverter.ScaleProperty.get
		// Processing: Microsoft.UI.Xaml.Data.IValueConverter
	}
}
