#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Management
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum MdmAlertMark 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		None,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Fatal,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Critical,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Warning,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Informational,
		#endif
	}
	#endif
}
