#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Gaming.Input.ForceFeedback
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum ForceFeedbackEffectState 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Stopped,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Running,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Paused,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Faulted,
		#endif
	}
	#endif
}
