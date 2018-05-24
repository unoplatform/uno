#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Management.Deployment
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum PackageStatus 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		OK,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		LicenseIssue,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Modified,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Tampered,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Disabled,
		#endif
	}
	#endif
}
