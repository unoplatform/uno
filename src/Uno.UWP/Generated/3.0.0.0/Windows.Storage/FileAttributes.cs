#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Storage
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum FileAttributes 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Normal,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ReadOnly,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Directory,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Archive,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Temporary,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		LocallyIncomplete,
		#endif
	}
	#endif
}
