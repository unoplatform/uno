#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.System.Profile
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum SystemIdentificationSource 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		None,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Tpm,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Uefi,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Registry,
		#endif
	}
	#endif
}
