#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.ContentRestrictions
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum ContentAccessRestrictionLevel 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Allow,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Warn,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Block,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Hide,
		#endif
	}
	#endif
}
