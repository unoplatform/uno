#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum DomainNameType 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Suffix,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		FullyQualified,
		#endif
	}
	#endif
}
