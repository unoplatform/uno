#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if false
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum AutoSuggestionBoxTextChangeReason 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		UserInput,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ProgrammaticChange,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SuggestionChosen,
		#endif
	}
	#endif
}
