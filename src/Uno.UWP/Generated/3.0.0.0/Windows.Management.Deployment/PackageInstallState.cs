#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Management.Deployment
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum PackageInstallState 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NotInstalled,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Staged,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Installed,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Paused,
		#endif
	}
	#endif
}
