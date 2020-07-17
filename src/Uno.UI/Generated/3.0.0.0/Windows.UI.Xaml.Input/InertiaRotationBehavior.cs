#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Input
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class InertiaRotationBehavior 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  double DesiredRotation
		{
			get
			{
				throw new global::System.NotImplementedException("The member double InertiaRotationBehavior.DesiredRotation is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Input.InertiaRotationBehavior", "double InertiaRotationBehavior.DesiredRotation");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  double DesiredDeceleration
		{
			get
			{
				throw new global::System.NotImplementedException("The member double InertiaRotationBehavior.DesiredDeceleration is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Input.InertiaRotationBehavior", "double InertiaRotationBehavior.DesiredDeceleration");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Input.InertiaRotationBehavior.DesiredDeceleration.get
		// Forced skipping of method Windows.UI.Xaml.Input.InertiaRotationBehavior.DesiredDeceleration.set
		// Forced skipping of method Windows.UI.Xaml.Input.InertiaRotationBehavior.DesiredRotation.get
		// Forced skipping of method Windows.UI.Xaml.Input.InertiaRotationBehavior.DesiredRotation.set
	}
}
