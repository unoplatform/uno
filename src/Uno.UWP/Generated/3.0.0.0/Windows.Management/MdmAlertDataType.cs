#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Management
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum MdmAlertDataType 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		String,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Base64,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Boolean,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Integer,
		#endif
	}
	#endif
}
