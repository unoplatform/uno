#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Management.Policies
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum NamedPolicyKind 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Invalid,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Binary,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Boolean,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Int32,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Int64,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		String,
		#endif
	}
	#endif
}
