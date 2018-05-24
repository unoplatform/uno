#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum InkToolbarFlyoutItemKind 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Simple,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Radio,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Check,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		RadioCheck,
		#endif
	}
	#endif
}
