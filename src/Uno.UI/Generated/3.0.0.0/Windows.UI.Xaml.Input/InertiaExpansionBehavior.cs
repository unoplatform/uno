#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Input
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class InertiaExpansionBehavior 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  double DesiredExpansion
		{
			get
			{
				throw new global::System.NotImplementedException("The member double InertiaExpansionBehavior.DesiredExpansion is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Input.InertiaExpansionBehavior", "double InertiaExpansionBehavior.DesiredExpansion");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  double DesiredDeceleration
		{
			get
			{
				throw new global::System.NotImplementedException("The member double InertiaExpansionBehavior.DesiredDeceleration is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Input.InertiaExpansionBehavior", "double InertiaExpansionBehavior.DesiredDeceleration");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Input.InertiaExpansionBehavior.DesiredDeceleration.get
		// Forced skipping of method Windows.UI.Xaml.Input.InertiaExpansionBehavior.DesiredDeceleration.set
		// Forced skipping of method Windows.UI.Xaml.Input.InertiaExpansionBehavior.DesiredExpansion.get
		// Forced skipping of method Windows.UI.Xaml.Input.InertiaExpansionBehavior.DesiredExpansion.set
	}
}
