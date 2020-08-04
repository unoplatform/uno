#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.ViewManagement
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ApplicationViewTransferContext 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  int ViewId
		{
			get
			{
				throw new global::System.NotImplementedException("The member int ApplicationViewTransferContext.ViewId is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.ViewManagement.ApplicationViewTransferContext", "int ApplicationViewTransferContext.ViewId");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static string DataPackageFormatId
		{
			get
			{
				throw new global::System.NotImplementedException("The member string ApplicationViewTransferContext.DataPackageFormatId is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public ApplicationViewTransferContext() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.ViewManagement.ApplicationViewTransferContext", "ApplicationViewTransferContext.ApplicationViewTransferContext()");
		}
		#endif
		// Forced skipping of method Windows.UI.ViewManagement.ApplicationViewTransferContext.ApplicationViewTransferContext()
		// Forced skipping of method Windows.UI.ViewManagement.ApplicationViewTransferContext.ViewId.get
		// Forced skipping of method Windows.UI.ViewManagement.ApplicationViewTransferContext.ViewId.set
		// Forced skipping of method Windows.UI.ViewManagement.ApplicationViewTransferContext.DataPackageFormatId.get
	}
}
