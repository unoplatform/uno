#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.System.RemoteSystems
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum RemoteSystemPlatform 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Unknown,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Windows,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Android,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Ios,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Linux,
		#endif
	}
	#endif
}
