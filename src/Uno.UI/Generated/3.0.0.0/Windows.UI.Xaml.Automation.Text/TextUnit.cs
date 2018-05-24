#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Automation.Text
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum TextUnit 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Character,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Format,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Word,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Line,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Paragraph,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Page,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Document,
		#endif
	}
	#endif
}
