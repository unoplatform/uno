#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Microsoft.Web.WebView2.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class CoreWebView2ScriptDialogOpeningEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string ResultText
		{
			get
			{
				throw new global::System.NotImplementedException("The member string CoreWebView2ScriptDialogOpeningEventArgs.ResultText is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20CoreWebView2ScriptDialogOpeningEventArgs.ResultText");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.Web.WebView2.Core.CoreWebView2ScriptDialogOpeningEventArgs", "string CoreWebView2ScriptDialogOpeningEventArgs.ResultText");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string DefaultText
		{
			get
			{
				throw new global::System.NotImplementedException("The member string CoreWebView2ScriptDialogOpeningEventArgs.DefaultText is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20CoreWebView2ScriptDialogOpeningEventArgs.DefaultText");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Microsoft.Web.WebView2.Core.CoreWebView2ScriptDialogKind Kind
		{
			get
			{
				throw new global::System.NotImplementedException("The member CoreWebView2ScriptDialogKind CoreWebView2ScriptDialogOpeningEventArgs.Kind is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=CoreWebView2ScriptDialogKind%20CoreWebView2ScriptDialogOpeningEventArgs.Kind");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Message
		{
			get
			{
				throw new global::System.NotImplementedException("The member string CoreWebView2ScriptDialogOpeningEventArgs.Message is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20CoreWebView2ScriptDialogOpeningEventArgs.Message");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Uri
		{
			get
			{
				throw new global::System.NotImplementedException("The member string CoreWebView2ScriptDialogOpeningEventArgs.Uri is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20CoreWebView2ScriptDialogOpeningEventArgs.Uri");
			}
		}
		#endif
		// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2ScriptDialogOpeningEventArgs.Uri.get
		// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2ScriptDialogOpeningEventArgs.Kind.get
		// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2ScriptDialogOpeningEventArgs.Message.get
		// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2ScriptDialogOpeningEventArgs.DefaultText.get
		// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2ScriptDialogOpeningEventArgs.ResultText.get
		// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2ScriptDialogOpeningEventArgs.ResultText.set
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Accept()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.Web.WebView2.Core.CoreWebView2ScriptDialogOpeningEventArgs", "void CoreWebView2ScriptDialogOpeningEventArgs.Accept()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.Deferral GetDeferral()
		{
			throw new global::System.NotImplementedException("The member Deferral CoreWebView2ScriptDialogOpeningEventArgs.GetDeferral() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=Deferral%20CoreWebView2ScriptDialogOpeningEventArgs.GetDeferral%28%29");
		}
		#endif
	}
}
