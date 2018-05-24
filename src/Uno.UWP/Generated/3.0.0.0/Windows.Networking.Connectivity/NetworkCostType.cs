#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.Connectivity
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum NetworkCostType 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Unknown,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Unrestricted,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Fixed,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Variable,
		#endif
	}
	#endif
}
