#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Interop
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum TypeKind 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Primitive,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Metadata,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Custom,
		#endif
	}
	#endif
}
