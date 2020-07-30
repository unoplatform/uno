#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Media.Animation
{
	#if false || false || false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class EasingFunctionBase : global::Windows.UI.Xaml.DependencyObject
	{
		// Skipping already declared property EasingMode
		// Skipping already declared property EasingModeProperty
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.EasingFunctionBase.EasingMode.get
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.EasingFunctionBase.EasingMode.set
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  double Ease( double normalizedTime)
		{
			throw new global::System.NotImplementedException("The member double EasingFunctionBase.Ease(double normalizedTime) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.EasingFunctionBase.EasingModeProperty.get
	}
}
