#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.DataTransfer
{
	#if false || false || NET461 || false || false || false || false
	[global::Uno.NotImplemented("NET461")]
	#endif
	public static partial class Clipboard 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.ApplicationModel.DataTransfer.ClipboardHistoryItemsResult> GetHistoryItemsAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<ClipboardHistoryItemsResult> Clipboard.GetHistoryItemsAsync() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3CClipboardHistoryItemsResult%3E%20Clipboard.GetHistoryItemsAsync%28%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static bool ClearHistory()
		{
			throw new global::System.NotImplementedException("The member bool Clipboard.ClearHistory() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=bool%20Clipboard.ClearHistory%28%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static bool DeleteItemFromHistory( global::Windows.ApplicationModel.DataTransfer.ClipboardHistoryItem item)
		{
			throw new global::System.NotImplementedException("The member bool Clipboard.DeleteItemFromHistory(ClipboardHistoryItem item) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=bool%20Clipboard.DeleteItemFromHistory%28ClipboardHistoryItem%20item%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.ApplicationModel.DataTransfer.SetHistoryItemAsContentStatus SetHistoryItemAsContent( global::Windows.ApplicationModel.DataTransfer.ClipboardHistoryItem item)
		{
			throw new global::System.NotImplementedException("The member SetHistoryItemAsContentStatus Clipboard.SetHistoryItemAsContent(ClipboardHistoryItem item) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=SetHistoryItemAsContentStatus%20Clipboard.SetHistoryItemAsContent%28ClipboardHistoryItem%20item%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static bool IsHistoryEnabled()
		{
			throw new global::System.NotImplementedException("The member bool Clipboard.IsHistoryEnabled() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=bool%20Clipboard.IsHistoryEnabled%28%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static bool IsRoamingEnabled()
		{
			throw new global::System.NotImplementedException("The member bool Clipboard.IsRoamingEnabled() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=bool%20Clipboard.IsRoamingEnabled%28%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static bool SetContentWithOptions( global::Windows.ApplicationModel.DataTransfer.DataPackage content,  global::Windows.ApplicationModel.DataTransfer.ClipboardContentOptions options)
		{
			throw new global::System.NotImplementedException("The member bool Clipboard.SetContentWithOptions(DataPackage content, ClipboardContentOptions options) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=bool%20Clipboard.SetContentWithOptions%28DataPackage%20content%2C%20ClipboardContentOptions%20options%29");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.DataTransfer.Clipboard.HistoryChanged.add
		// Forced skipping of method Windows.ApplicationModel.DataTransfer.Clipboard.HistoryChanged.remove
		// Forced skipping of method Windows.ApplicationModel.DataTransfer.Clipboard.RoamingEnabledChanged.add
		// Forced skipping of method Windows.ApplicationModel.DataTransfer.Clipboard.RoamingEnabledChanged.remove
		// Forced skipping of method Windows.ApplicationModel.DataTransfer.Clipboard.HistoryEnabledChanged.add
		// Forced skipping of method Windows.ApplicationModel.DataTransfer.Clipboard.HistoryEnabledChanged.remove
		#if false || false || NET461 || false || false || __NETSTD_REFERENCE__ || false
		[global::Uno.NotImplemented("NET461", "__NETSTD_REFERENCE__")]
		public static global::Windows.ApplicationModel.DataTransfer.DataPackageView GetContent()
		{
			throw new global::System.NotImplementedException("The member DataPackageView Clipboard.GetContent() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=DataPackageView%20Clipboard.GetContent%28%29");
		}
		#endif
		#if false || false || NET461 || false || false || __NETSTD_REFERENCE__ || false
		[global::Uno.NotImplemented("NET461", "__NETSTD_REFERENCE__")]
		public static void SetContent( global::Windows.ApplicationModel.DataTransfer.DataPackage content)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.DataTransfer.Clipboard", "void Clipboard.SetContent(DataPackage content)");
		}
		#endif
		#if false || false || NET461 || false || false || false || false
		[global::Uno.NotImplemented("NET461")]
		public static void Flush()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.DataTransfer.Clipboard", "void Clipboard.Flush()");
		}
		#endif
		#if false || false || NET461 || false || false || __NETSTD_REFERENCE__ || false
		[global::Uno.NotImplemented("NET461", "__NETSTD_REFERENCE__")]
		public static void Clear()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.DataTransfer.Clipboard", "void Clipboard.Clear()");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.DataTransfer.Clipboard.ContentChanged.add
		// Forced skipping of method Windows.ApplicationModel.DataTransfer.Clipboard.ContentChanged.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static event global::System.EventHandler<global::Windows.ApplicationModel.DataTransfer.ClipboardHistoryChangedEventArgs> HistoryChanged
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.DataTransfer.Clipboard", "event EventHandler<ClipboardHistoryChangedEventArgs> Clipboard.HistoryChanged");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.DataTransfer.Clipboard", "event EventHandler<ClipboardHistoryChangedEventArgs> Clipboard.HistoryChanged");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static event global::System.EventHandler<object> HistoryEnabledChanged
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.DataTransfer.Clipboard", "event EventHandler<object> Clipboard.HistoryEnabledChanged");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.DataTransfer.Clipboard", "event EventHandler<object> Clipboard.HistoryEnabledChanged");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static event global::System.EventHandler<object> RoamingEnabledChanged
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.DataTransfer.Clipboard", "event EventHandler<object> Clipboard.RoamingEnabledChanged");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.DataTransfer.Clipboard", "event EventHandler<object> Clipboard.RoamingEnabledChanged");
			}
		}
		#endif
		#if false || false || NET461 || false || false || false || false
		[global::Uno.NotImplemented("NET461")]
		public static event global::System.EventHandler<object> ContentChanged
		{
			[global::Uno.NotImplemented("NET461")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.DataTransfer.Clipboard", "event EventHandler<object> Clipboard.ContentChanged");
			}
			[global::Uno.NotImplemented("NET461")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.DataTransfer.Clipboard", "event EventHandler<object> Clipboard.ContentChanged");
			}
		}
		#endif
	}
}
