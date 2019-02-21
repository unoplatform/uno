#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Input
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial struct ManipulationDelta 
	{
		// Forced skipping of method Windows.UI.Input.ManipulationDelta.ManipulationDelta()
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		public  global::Windows.Foundation.Point Translation;
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		public  float Scale;
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		public  float Rotation;
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		public  float Expansion;
		#endif
	}
}
