#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Input
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class InertiaTranslationBehavior 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  double DesiredDisplacement
		{
			get
			{
				throw new global::System.NotImplementedException("The member double InertiaTranslationBehavior.DesiredDisplacement is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Input.InertiaTranslationBehavior", "double InertiaTranslationBehavior.DesiredDisplacement");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  double DesiredDeceleration
		{
			get
			{
				throw new global::System.NotImplementedException("The member double InertiaTranslationBehavior.DesiredDeceleration is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Input.InertiaTranslationBehavior", "double InertiaTranslationBehavior.DesiredDeceleration");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Input.InertiaTranslationBehavior.DesiredDeceleration.get
		// Forced skipping of method Windows.UI.Xaml.Input.InertiaTranslationBehavior.DesiredDeceleration.set
		// Forced skipping of method Windows.UI.Xaml.Input.InertiaTranslationBehavior.DesiredDisplacement.get
		// Forced skipping of method Windows.UI.Xaml.Input.InertiaTranslationBehavior.DesiredDisplacement.set
	}
}
