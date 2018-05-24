#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls.Primitives
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum ListViewItemPresenterCheckMode 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Inline,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Overlay,
		#endif
	}
	#endif
}
