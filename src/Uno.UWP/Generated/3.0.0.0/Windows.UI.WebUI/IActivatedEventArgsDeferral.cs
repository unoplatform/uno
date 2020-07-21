#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.WebUI
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IActivatedEventArgsDeferral 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.UI.WebUI.ActivatedOperation ActivatedOperation
		{
			get;
		}
		#endif
		// Forced skipping of method Windows.UI.WebUI.IActivatedEventArgsDeferral.ActivatedOperation.get
	}
}
