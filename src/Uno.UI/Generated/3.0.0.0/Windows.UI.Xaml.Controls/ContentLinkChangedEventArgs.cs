#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ContentLinkChangedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Xaml.Controls.ContentLinkChangeKind ChangeKind
		{
			get
			{
				throw new global::System.NotImplementedException("The member ContentLinkChangeKind ContentLinkChangedEventArgs.ChangeKind is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Text.ContentLinkInfo ContentLinkInfo
		{
			get
			{
				throw new global::System.NotImplementedException("The member ContentLinkInfo ContentLinkChangedEventArgs.ContentLinkInfo is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Xaml.Documents.TextRange TextRange
		{
			get
			{
				throw new global::System.NotImplementedException("The member TextRange ContentLinkChangedEventArgs.TextRange is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.ContentLinkChangedEventArgs.ChangeKind.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ContentLinkChangedEventArgs.ContentLinkInfo.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ContentLinkChangedEventArgs.TextRange.get
	}
}
