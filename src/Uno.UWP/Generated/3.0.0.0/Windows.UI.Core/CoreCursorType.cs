#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Core
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum CoreCursorType 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Arrow,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Cross,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Custom,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Hand,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Help,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		IBeam,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SizeAll,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SizeNortheastSouthwest,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SizeNorthSouth,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SizeNorthwestSoutheast,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SizeWestEast,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		UniversalNo,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		UpArrow,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Wait,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Pin,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Person,
		#endif
	}
	#endif
}
