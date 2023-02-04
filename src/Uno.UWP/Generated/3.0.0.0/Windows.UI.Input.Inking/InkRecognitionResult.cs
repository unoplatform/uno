#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Input.Inking
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class InkRecognitionResult 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.Rect BoundingRect
		{
			get
			{
				throw new global::System.NotImplementedException("The member Rect InkRecognitionResult.BoundingRect is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=Rect%20InkRecognitionResult.BoundingRect");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Input.Inking.InkRecognitionResult.BoundingRect.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<string> GetTextCandidates()
		{
			throw new global::System.NotImplementedException("The member IReadOnlyList<string> InkRecognitionResult.GetTextCandidates() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IReadOnlyList%3Cstring%3E%20InkRecognitionResult.GetTextCandidates%28%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.UI.Input.Inking.InkStroke> GetStrokes()
		{
			throw new global::System.NotImplementedException("The member IReadOnlyList<InkStroke> InkRecognitionResult.GetStrokes() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IReadOnlyList%3CInkStroke%3E%20InkRecognitionResult.GetStrokes%28%29");
		}
		#endif
	}
}
