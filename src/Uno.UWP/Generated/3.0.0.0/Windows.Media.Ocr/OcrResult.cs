#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Ocr
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class OcrResult 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Media.Ocr.OcrLine> Lines
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<OcrLine> OcrResult.Lines is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IReadOnlyList%3COcrLine%3E%20OcrResult.Lines");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Text
		{
			get
			{
				throw new global::System.NotImplementedException("The member string OcrResult.Text is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20OcrResult.Text");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  double? TextAngle
		{
			get
			{
				throw new global::System.NotImplementedException("The member double? OcrResult.TextAngle is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=double%3F%20OcrResult.TextAngle");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.Ocr.OcrResult.Lines.get
		// Forced skipping of method Windows.Media.Ocr.OcrResult.TextAngle.get
		// Forced skipping of method Windows.Media.Ocr.OcrResult.Text.get
	}
}
