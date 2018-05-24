#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.BackgroundTransfer
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum BackgroundTransferBehavior 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Parallel,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Serialized,
		#endif
	}
	#endif
}
