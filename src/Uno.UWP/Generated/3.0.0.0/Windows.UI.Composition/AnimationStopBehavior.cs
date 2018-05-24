#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Composition
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum AnimationStopBehavior 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		LeaveCurrentValue,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SetToInitialValue,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SetToFinalValue,
		#endif
	}
	#endif
}
