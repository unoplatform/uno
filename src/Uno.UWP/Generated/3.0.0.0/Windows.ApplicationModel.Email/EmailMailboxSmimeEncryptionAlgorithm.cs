#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Email
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum EmailMailboxSmimeEncryptionAlgorithm 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Any,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		TripleDes,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Des,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		RC2128Bit,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		RC264Bit,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		RC240Bit,
		#endif
	}
	#endif
}
