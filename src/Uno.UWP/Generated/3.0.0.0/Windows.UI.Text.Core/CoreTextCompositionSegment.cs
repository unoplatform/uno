#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Text.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class CoreTextCompositionSegment 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string PreconversionString
		{
			get
			{
				throw new global::System.NotImplementedException("The member string CoreTextCompositionSegment.PreconversionString is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Text.Core.CoreTextRange Range
		{
			get
			{
				throw new global::System.NotImplementedException("The member CoreTextRange CoreTextCompositionSegment.Range is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Text.Core.CoreTextCompositionSegment.PreconversionString.get
		// Forced skipping of method Windows.UI.Text.Core.CoreTextCompositionSegment.Range.get
	}
}
