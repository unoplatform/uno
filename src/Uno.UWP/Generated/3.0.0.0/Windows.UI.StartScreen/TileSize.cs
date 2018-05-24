#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.StartScreen
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum TileSize 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Default,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Square30x30,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Square70x70,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Square150x150,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Wide310x150,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Square310x310,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Square71x71,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Square44x44,
		#endif
	}
	#endif
}
