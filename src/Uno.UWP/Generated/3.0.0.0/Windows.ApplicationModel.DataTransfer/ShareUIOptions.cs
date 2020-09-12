#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.DataTransfer
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ShareUIOptions 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.DataTransfer.ShareUITheme Theme
		{
			get
			{
				throw new global::System.NotImplementedException("The member ShareUITheme ShareUIOptions.Theme is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.DataTransfer.ShareUIOptions", "ShareUITheme ShareUIOptions.Theme");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.Rect? SelectionRect
		{
			get
			{
				throw new global::System.NotImplementedException("The member Rect? ShareUIOptions.SelectionRect is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.DataTransfer.ShareUIOptions", "Rect? ShareUIOptions.SelectionRect");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public ShareUIOptions() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.DataTransfer.ShareUIOptions", "ShareUIOptions.ShareUIOptions()");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.DataTransfer.ShareUIOptions.ShareUIOptions()
		// Forced skipping of method Windows.ApplicationModel.DataTransfer.ShareUIOptions.Theme.get
		// Forced skipping of method Windows.ApplicationModel.DataTransfer.ShareUIOptions.Theme.set
		// Forced skipping of method Windows.ApplicationModel.DataTransfer.ShareUIOptions.SelectionRect.get
		// Forced skipping of method Windows.ApplicationModel.DataTransfer.ShareUIOptions.SelectionRect.set
	}
}
