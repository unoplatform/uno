#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml
{
	#if false || false || false || false
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public   enum TextAlignment 
	{
		#if false || false || false || false
		Center,
		#endif
		#if false || false || false || false
		Left,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Start,
		#endif
		#if false || false || false || false
		Right,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		End,
		#endif
		#if false || false || false || false
		Justify,
		#endif
		#if false || false || false || false
		DetectFromContent,
		#endif
	}
	#endif
}
