#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.StartScreen
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum TileOptions 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		None,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ShowNameOnLogo,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ShowNameOnWideLogo,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		CopyOnDeployment,
		#endif
	}
	#endif
}
