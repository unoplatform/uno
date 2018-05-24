#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Email
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum EmailMailboxDeleteFolderStatus 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Success,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NetworkError,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		PermissionsError,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ServerError,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		UnknownFailure,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		CouldNotDeleteEverything,
		#endif
	}
	#endif
}
