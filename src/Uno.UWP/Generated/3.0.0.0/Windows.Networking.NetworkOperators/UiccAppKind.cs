#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.NetworkOperators
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum UiccAppKind 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Unknown,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		MF,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		MFSim,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		MFRuim,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		USim,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		CSim,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ISim,
		#endif
	}
	#endif
}
