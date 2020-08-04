#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Text
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface ITextSelection : global::Windows.UI.Text.ITextRange
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.UI.Text.SelectionOptions Options
		{
			get;
			set;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.UI.Text.SelectionType Type
		{
			get;
		}
		#endif
		// Forced skipping of method Windows.UI.Text.ITextSelection.Options.get
		// Forced skipping of method Windows.UI.Text.ITextSelection.Options.set
		// Forced skipping of method Windows.UI.Text.ITextSelection.Type.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		int EndKey( global::Windows.UI.Text.TextRangeUnit unit,  bool extend);
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		int HomeKey( global::Windows.UI.Text.TextRangeUnit unit,  bool extend);
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		int MoveDown( global::Windows.UI.Text.TextRangeUnit unit,  int count,  bool extend);
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		int MoveLeft( global::Windows.UI.Text.TextRangeUnit unit,  int count,  bool extend);
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		int MoveRight( global::Windows.UI.Text.TextRangeUnit unit,  int count,  bool extend);
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		int MoveUp( global::Windows.UI.Text.TextRangeUnit unit,  int count,  bool extend);
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		void TypeText( string value);
		#endif
	}
}
