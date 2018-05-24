#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Management.Deployment
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum PackageState 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Normal,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		LicenseInvalid,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Modified,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Tampered,
		#endif
	}
	#endif
}
