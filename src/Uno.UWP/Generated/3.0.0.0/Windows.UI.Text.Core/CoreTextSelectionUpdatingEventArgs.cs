#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Text.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class CoreTextSelectionUpdatingEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Text.Core.CoreTextSelectionUpdatingResult Result
		{
			get
			{
				throw new global::System.NotImplementedException("The member CoreTextSelectionUpdatingResult CoreTextSelectionUpdatingEventArgs.Result is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Text.Core.CoreTextSelectionUpdatingEventArgs", "CoreTextSelectionUpdatingResult CoreTextSelectionUpdatingEventArgs.Result");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsCanceled
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool CoreTextSelectionUpdatingEventArgs.IsCanceled is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Text.Core.CoreTextRange Selection
		{
			get
			{
				throw new global::System.NotImplementedException("The member CoreTextRange CoreTextSelectionUpdatingEventArgs.Selection is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Text.Core.CoreTextSelectionUpdatingEventArgs.Selection.get
		// Forced skipping of method Windows.UI.Text.Core.CoreTextSelectionUpdatingEventArgs.Result.get
		// Forced skipping of method Windows.UI.Text.Core.CoreTextSelectionUpdatingEventArgs.Result.set
		// Forced skipping of method Windows.UI.Text.Core.CoreTextSelectionUpdatingEventArgs.IsCanceled.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.Deferral GetDeferral()
		{
			throw new global::System.NotImplementedException("The member Deferral CoreTextSelectionUpdatingEventArgs.GetDeferral() is not implemented in Uno.");
		}
		#endif
	}
}
