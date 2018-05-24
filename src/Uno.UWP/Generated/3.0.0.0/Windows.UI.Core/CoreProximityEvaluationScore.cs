#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Core
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum CoreProximityEvaluationScore 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Closest,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Farthest,
		#endif
	}
	#endif
}
