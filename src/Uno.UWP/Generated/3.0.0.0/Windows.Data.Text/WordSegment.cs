#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Data.Text
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class WordSegment 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Data.Text.AlternateWordForm> AlternateForms
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<AlternateWordForm> WordSegment.AlternateForms is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IReadOnlyList%3CAlternateWordForm%3E%20WordSegment.AlternateForms");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Data.Text.TextSegment SourceTextSegment
		{
			get
			{
				throw new global::System.NotImplementedException("The member TextSegment WordSegment.SourceTextSegment is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=TextSegment%20WordSegment.SourceTextSegment");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Text
		{
			get
			{
				throw new global::System.NotImplementedException("The member string WordSegment.Text is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20WordSegment.Text");
			}
		}
		#endif
		// Forced skipping of method Windows.Data.Text.WordSegment.Text.get
		// Forced skipping of method Windows.Data.Text.WordSegment.SourceTextSegment.get
		// Forced skipping of method Windows.Data.Text.WordSegment.AlternateForms.get
	}
}
