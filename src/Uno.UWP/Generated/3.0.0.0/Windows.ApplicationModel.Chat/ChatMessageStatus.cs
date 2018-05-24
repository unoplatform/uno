#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Chat
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum ChatMessageStatus 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Draft,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Sending,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Sent,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SendRetryNeeded,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SendFailed,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Received,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ReceiveDownloadNeeded,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ReceiveDownloadFailed,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ReceiveDownloading,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Deleted,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Declined,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Cancelled,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Recalled,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ReceiveRetryNeeded,
		#endif
	}
	#endif
}
