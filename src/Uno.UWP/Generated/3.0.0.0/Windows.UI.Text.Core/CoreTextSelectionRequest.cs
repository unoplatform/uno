#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Text.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class CoreTextSelectionRequest 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Text.Core.CoreTextRange Selection
		{
			get
			{
				throw new global::System.NotImplementedException("The member CoreTextRange CoreTextSelectionRequest.Selection is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=CoreTextRange%20CoreTextSelectionRequest.Selection");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Text.Core.CoreTextSelectionRequest", "CoreTextRange CoreTextSelectionRequest.Selection");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsCanceled
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool CoreTextSelectionRequest.IsCanceled is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=bool%20CoreTextSelectionRequest.IsCanceled");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Text.Core.CoreTextSelectionRequest.Selection.get
		// Forced skipping of method Windows.UI.Text.Core.CoreTextSelectionRequest.Selection.set
		// Forced skipping of method Windows.UI.Text.Core.CoreTextSelectionRequest.IsCanceled.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.Deferral GetDeferral()
		{
			throw new global::System.NotImplementedException("The member Deferral CoreTextSelectionRequest.GetDeferral() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=Deferral%20CoreTextSelectionRequest.GetDeferral%28%29");
		}
		#endif
	}
}
