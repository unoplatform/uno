#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Gaming.Input.ForceFeedback
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum PeriodicForceEffectKind 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SquareWave,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SineWave,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		TriangleWave,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SawtoothWaveUp,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SawtoothWaveDown,
		#endif
	}
	#endif
}
