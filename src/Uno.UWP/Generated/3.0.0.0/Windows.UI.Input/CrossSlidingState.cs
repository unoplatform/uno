#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Input
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum CrossSlidingState 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Started,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Dragging,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Selecting,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SelectSpeedBumping,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SpeedBumping,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Rearranging,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Completed,
		#endif
	}
	#endif
}
