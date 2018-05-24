#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Media
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum StyleSimulations 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		None,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		BoldSimulation,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ItalicSimulation,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		BoldItalicSimulation,
		#endif
	}
	#endif
}
