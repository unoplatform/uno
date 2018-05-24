#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Foundation.Diagnostics
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum CausalityRelation 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AssignDelegate,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Join,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Choice,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Cancel,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Error,
		#endif
	}
	#endif
}
