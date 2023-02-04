#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.WebUI
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public static partial class WebUIBackgroundTaskInstance 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.WebUI.IWebUIBackgroundTaskInstance Current
		{
			get
			{
				throw new global::System.NotImplementedException("The member IWebUIBackgroundTaskInstance WebUIBackgroundTaskInstance.Current is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IWebUIBackgroundTaskInstance%20WebUIBackgroundTaskInstance.Current");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.WebUI.WebUIBackgroundTaskInstance.Current.get
	}
}
