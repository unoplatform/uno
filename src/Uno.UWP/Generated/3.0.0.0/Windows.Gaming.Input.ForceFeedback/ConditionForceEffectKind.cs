#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Gaming.Input.ForceFeedback
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum ConditionForceEffectKind 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Spring,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Damper,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Inertia,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Friction,
		#endif
	}
	#endif
}
