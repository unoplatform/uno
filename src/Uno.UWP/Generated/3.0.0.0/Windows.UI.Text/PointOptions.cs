#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Text
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum PointOptions 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		None,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		IncludeInset,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Start,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ClientCoordinates,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AllowOffClient,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Transform,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NoHorizontalScroll,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NoVerticalScroll,
		#endif
	}
	#endif
}
