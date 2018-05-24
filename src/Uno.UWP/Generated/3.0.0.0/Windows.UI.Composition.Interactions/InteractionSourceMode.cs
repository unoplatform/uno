#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Composition.Interactions
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum InteractionSourceMode 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Disabled,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		EnabledWithInertia,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		EnabledWithoutInertia,
		#endif
	}
	#endif
}
