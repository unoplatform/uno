#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Text
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum TextScript 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Undefined,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Ansi,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		EastEurope,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Cyrillic,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Greek,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Turkish,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Hebrew,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Arabic,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Baltic,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Vietnamese,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Default,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Symbol,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Thai,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ShiftJis,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		GB2312,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Hangul,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Big5,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		PC437,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Oem,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Mac,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Armenian,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Syriac,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Thaana,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Devanagari,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Bengali,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Gurmukhi,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Gujarati,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Oriya,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Tamil,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Telugu,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Kannada,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Malayalam,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Sinhala,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Lao,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Tibetan,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Myanmar,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Georgian,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Jamo,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Ethiopic,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Cherokee,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Aboriginal,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Ogham,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Runic,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Khmer,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Mongolian,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Braille,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Yi,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Limbu,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		TaiLe,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NewTaiLue,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SylotiNagri,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Kharoshthi,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Kayahli,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		UnicodeSymbol,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Emoji,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Glagolitic,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Lisu,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Vai,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NKo,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Osmanya,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		PhagsPa,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Gothic,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Deseret,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Tifinagh,
		#endif
	}
	#endif
}
