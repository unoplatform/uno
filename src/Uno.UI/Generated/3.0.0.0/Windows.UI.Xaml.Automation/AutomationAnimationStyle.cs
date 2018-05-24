#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Automation
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum AutomationAnimationStyle 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		None,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		LasVegasLights,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		BlinkingBackground,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SparkleText,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		MarchingBlackAnts,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		MarchingRedAnts,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Shimmer,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Other,
		#endif
	}
	#endif
}
