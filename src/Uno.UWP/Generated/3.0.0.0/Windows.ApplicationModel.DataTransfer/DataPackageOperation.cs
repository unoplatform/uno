#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.DataTransfer
{
	#if false || false || false || false || false
	#if false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public   enum DataPackageOperation 
	{
		#if false || false || false || false || false
		None,
		#endif
		#if false || false || false || false || false
		Copy,
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		Move,
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		Link,
		#endif
	}
	#endif
}
