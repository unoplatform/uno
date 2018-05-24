#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum PackageContentGroupState 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NotStaged,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Queued,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Staging,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Staged,
		#endif
	}
	#endif
}
