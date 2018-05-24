#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Media.Animation
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum ConnectedAnimationComponent 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		OffsetX,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		OffsetY,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		CrossFade,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Scale,
		#endif
	}
	#endif
}
