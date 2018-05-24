#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Core
{
	#if false || false || false || false
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public   enum AppViewBackButtonVisibility 
	{
		#if false || false || false || false
		Visible,
		#endif
		#if false || false || false || false
		Collapsed,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Disabled,
		#endif
	}
	#endif
}
