#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Email
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum EmailSpecialFolderKind 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		None,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Root,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Inbox,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Outbox,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Drafts,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DeletedItems,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Sent,
		#endif
	}
	#endif
}
