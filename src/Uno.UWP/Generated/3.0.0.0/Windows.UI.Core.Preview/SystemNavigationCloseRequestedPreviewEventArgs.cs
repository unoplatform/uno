#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Core.Preview
{
	#if false || false || false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class SystemNavigationCloseRequestedPreviewEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || false || __NETSTD_REFERENCE__ || false
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__NETSTD_REFERENCE__")]
		public  bool Handled
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool SystemNavigationCloseRequestedPreviewEventArgs.Handled is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=bool%20SystemNavigationCloseRequestedPreviewEventArgs.Handled");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.Preview.SystemNavigationCloseRequestedPreviewEventArgs", "bool SystemNavigationCloseRequestedPreviewEventArgs.Handled");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Core.Preview.SystemNavigationCloseRequestedPreviewEventArgs.Handled.get
		// Forced skipping of method Windows.UI.Core.Preview.SystemNavigationCloseRequestedPreviewEventArgs.Handled.set
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || false || __NETSTD_REFERENCE__ || false
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__NETSTD_REFERENCE__")]
		public  global::Windows.Foundation.Deferral GetDeferral()
		{
			throw new global::System.NotImplementedException("The member Deferral SystemNavigationCloseRequestedPreviewEventArgs.GetDeferral() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=Deferral%20SystemNavigationCloseRequestedPreviewEventArgs.GetDeferral%28%29");
		}
		#endif
	}
}
