#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Graphics.Holographic
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum HolographicFramePresentWaitBehavior 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		WaitForFrameToFinish,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DoNotWaitForFrameToFinish,
		#endif
	}
	#endif
}
