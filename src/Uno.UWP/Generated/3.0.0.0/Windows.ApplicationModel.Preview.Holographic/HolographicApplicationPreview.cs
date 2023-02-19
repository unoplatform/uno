#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Preview.Holographic
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public static partial class HolographicApplicationPreview 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static bool IsCurrentViewPresentedOnHolographicDisplay()
		{
			throw new global::System.NotImplementedException("The member bool HolographicApplicationPreview.IsCurrentViewPresentedOnHolographicDisplay() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=bool%20HolographicApplicationPreview.IsCurrentViewPresentedOnHolographicDisplay%28%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static bool IsHolographicActivation( global::Windows.ApplicationModel.Activation.IActivatedEventArgs activatedEventArgs)
		{
			throw new global::System.NotImplementedException("The member bool HolographicApplicationPreview.IsHolographicActivation(IActivatedEventArgs activatedEventArgs) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=bool%20HolographicApplicationPreview.IsHolographicActivation%28IActivatedEventArgs%20activatedEventArgs%29");
		}
		#endif
	}
}
