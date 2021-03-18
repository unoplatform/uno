#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Text.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class CoreTextTextUpdatingEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Text.Core.CoreTextTextUpdatingResult Result
		{
			get
			{
				throw new global::System.NotImplementedException("The member CoreTextTextUpdatingResult CoreTextTextUpdatingEventArgs.Result is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Text.Core.CoreTextTextUpdatingEventArgs", "CoreTextTextUpdatingResult CoreTextTextUpdatingEventArgs.Result");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Globalization.Language InputLanguage
		{
			get
			{
				throw new global::System.NotImplementedException("The member Language CoreTextTextUpdatingEventArgs.InputLanguage is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsCanceled
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool CoreTextTextUpdatingEventArgs.IsCanceled is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Text.Core.CoreTextRange NewSelection
		{
			get
			{
				throw new global::System.NotImplementedException("The member CoreTextRange CoreTextTextUpdatingEventArgs.NewSelection is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Text.Core.CoreTextRange Range
		{
			get
			{
				throw new global::System.NotImplementedException("The member CoreTextRange CoreTextTextUpdatingEventArgs.Range is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Text
		{
			get
			{
				throw new global::System.NotImplementedException("The member string CoreTextTextUpdatingEventArgs.Text is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Text.Core.CoreTextTextUpdatingEventArgs.Range.get
		// Forced skipping of method Windows.UI.Text.Core.CoreTextTextUpdatingEventArgs.Text.get
		// Forced skipping of method Windows.UI.Text.Core.CoreTextTextUpdatingEventArgs.NewSelection.get
		// Forced skipping of method Windows.UI.Text.Core.CoreTextTextUpdatingEventArgs.InputLanguage.get
		// Forced skipping of method Windows.UI.Text.Core.CoreTextTextUpdatingEventArgs.Result.get
		// Forced skipping of method Windows.UI.Text.Core.CoreTextTextUpdatingEventArgs.Result.set
		// Forced skipping of method Windows.UI.Text.Core.CoreTextTextUpdatingEventArgs.IsCanceled.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.Deferral GetDeferral()
		{
			throw new global::System.NotImplementedException("The member Deferral CoreTextTextUpdatingEventArgs.GetDeferral() is not implemented in Uno.");
		}
		#endif
	}
}
