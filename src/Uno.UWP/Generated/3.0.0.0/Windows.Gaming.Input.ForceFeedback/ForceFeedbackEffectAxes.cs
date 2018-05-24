#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Gaming.Input.ForceFeedback
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum ForceFeedbackEffectAxes 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		None,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		X,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Y,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Z,
		#endif
	}
	#endif
}
