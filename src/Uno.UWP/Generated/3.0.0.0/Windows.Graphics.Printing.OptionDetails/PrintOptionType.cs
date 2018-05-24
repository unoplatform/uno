#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Graphics.Printing.OptionDetails
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum PrintOptionType 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Unknown,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Number,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Text,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ItemList,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Toggle,
		#endif
	}
	#endif
}
