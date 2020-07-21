#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Core.Preview
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class SystemNavigationCloseRequestedPreviewEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool Handled
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool SystemNavigationCloseRequestedPreviewEventArgs.Handled is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.Preview.SystemNavigationCloseRequestedPreviewEventArgs", "bool SystemNavigationCloseRequestedPreviewEventArgs.Handled");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Core.Preview.SystemNavigationCloseRequestedPreviewEventArgs.Handled.get
		// Forced skipping of method Windows.UI.Core.Preview.SystemNavigationCloseRequestedPreviewEventArgs.Handled.set
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.Deferral GetDeferral()
		{
			throw new global::System.NotImplementedException("The member Deferral SystemNavigationCloseRequestedPreviewEventArgs.GetDeferral() is not implemented in Uno.");
		}
		#endif
	}
}
