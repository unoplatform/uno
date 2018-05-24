#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Text
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum SelectionOptions 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		StartActive,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AtEndOfLine,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Overtype,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Active,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Replace,
		#endif
	}
	#endif
}
