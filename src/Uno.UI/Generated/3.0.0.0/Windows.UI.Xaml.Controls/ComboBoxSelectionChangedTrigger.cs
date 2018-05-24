#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum ComboBoxSelectionChangedTrigger 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Committed,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Always,
		#endif
	}
	#endif
}
