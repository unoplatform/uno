#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Import
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum PhotoImportItemSelectionMode 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SelectAll,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SelectNone,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SelectNew,
		#endif
	}
	#endif
}
