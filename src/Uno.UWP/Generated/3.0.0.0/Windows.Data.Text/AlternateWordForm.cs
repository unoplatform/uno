#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Data.Text
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class AlternateWordForm 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string AlternateText
		{
			get
			{
				throw new global::System.NotImplementedException("The member string AlternateWordForm.AlternateText is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Data.Text.AlternateNormalizationFormat NormalizationFormat
		{
			get
			{
				throw new global::System.NotImplementedException("The member AlternateNormalizationFormat AlternateWordForm.NormalizationFormat is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Data.Text.TextSegment SourceTextSegment
		{
			get
			{
				throw new global::System.NotImplementedException("The member TextSegment AlternateWordForm.SourceTextSegment is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Data.Text.AlternateWordForm.SourceTextSegment.get
		// Forced skipping of method Windows.Data.Text.AlternateWordForm.AlternateText.get
		// Forced skipping of method Windows.Data.Text.AlternateWordForm.NormalizationFormat.get
	}
}
