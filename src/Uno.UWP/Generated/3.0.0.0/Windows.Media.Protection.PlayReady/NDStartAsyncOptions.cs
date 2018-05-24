#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Protection.PlayReady
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum NDStartAsyncOptions 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		MutualAuthentication,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		WaitForLicenseDescriptor,
		#endif
	}
	#endif
}
