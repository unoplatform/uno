#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.DataTransfer
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum DataPackageOperation 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		None,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Copy,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Move,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Link,
		#endif
	}
	#endif
}
