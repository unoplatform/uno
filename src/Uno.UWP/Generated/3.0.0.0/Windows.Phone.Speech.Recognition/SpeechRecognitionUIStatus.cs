#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Phone.Speech.Recognition
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum SpeechRecognitionUIStatus 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Succeeded,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Busy,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Cancelled,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Preempted,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		PrivacyPolicyDeclined,
		#endif
	}
	#endif
}
