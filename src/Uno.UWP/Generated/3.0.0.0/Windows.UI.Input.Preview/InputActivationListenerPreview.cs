#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Input.Preview
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class InputActivationListenerPreview 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Input.InputActivationListener CreateForApplicationWindow( global::Windows.UI.WindowManagement.AppWindow window)
		{
			throw new global::System.NotImplementedException("The member InputActivationListener InputActivationListenerPreview.CreateForApplicationWindow(AppWindow window) is not implemented in Uno.");
		}
		#endif
	}
}
