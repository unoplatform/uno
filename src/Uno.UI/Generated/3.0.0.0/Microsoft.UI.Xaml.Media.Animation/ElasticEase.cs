#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Microsoft.UI.Xaml.Media.Animation
{
	#if false || false || false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class ElasticEase : global::Microsoft.UI.Xaml.Media.Animation.EasingFunctionBase
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  double Springiness
		{
			get
			{
				return (double)this.GetValue(SpringinessProperty);
			}
			set
			{
				this.SetValue(SpringinessProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  int Oscillations
		{
			get
			{
				return (int)this.GetValue(OscillationsProperty);
			}
			set
			{
				this.SetValue(OscillationsProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Microsoft.UI.Xaml.DependencyProperty OscillationsProperty { get; } = 
		Microsoft.UI.Xaml.DependencyProperty.Register(
			nameof(Oscillations), typeof(int), 
			typeof(global::Microsoft.UI.Xaml.Media.Animation.ElasticEase), 
			new FrameworkPropertyMetadata(default(int)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Microsoft.UI.Xaml.DependencyProperty SpringinessProperty { get; } = 
		Microsoft.UI.Xaml.DependencyProperty.Register(
			nameof(Springiness), typeof(double), 
			typeof(global::Microsoft.UI.Xaml.Media.Animation.ElasticEase), 
			new FrameworkPropertyMetadata(default(double)));
		#endif
		// Skipping already declared method Microsoft.UI.Xaml.Media.Animation.ElasticEase.ElasticEase()
		// Forced skipping of method Microsoft.UI.Xaml.Media.Animation.ElasticEase.ElasticEase()
		// Forced skipping of method Microsoft.UI.Xaml.Media.Animation.ElasticEase.Oscillations.get
		// Forced skipping of method Microsoft.UI.Xaml.Media.Animation.ElasticEase.Oscillations.set
		// Forced skipping of method Microsoft.UI.Xaml.Media.Animation.ElasticEase.Springiness.get
		// Forced skipping of method Microsoft.UI.Xaml.Media.Animation.ElasticEase.Springiness.set
		// Forced skipping of method Microsoft.UI.Xaml.Media.Animation.ElasticEase.OscillationsProperty.get
		// Forced skipping of method Microsoft.UI.Xaml.Media.Animation.ElasticEase.SpringinessProperty.get
	}
}
