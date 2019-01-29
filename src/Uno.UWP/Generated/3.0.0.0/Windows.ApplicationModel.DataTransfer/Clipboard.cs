#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.DataTransfer
{
	#if false || false || NET46 || false || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class Clipboard 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.ApplicationModel.DataTransfer.ClipboardHistoryItemsResult> GetHistoryItemsAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<ClipboardHistoryItemsResult> Clipboard.GetHistoryItemsAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static bool ClearHistory()
		{
			throw new global::System.NotImplementedException("The member bool Clipboard.ClearHistory() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static bool DeleteItemFromHistory( global::Windows.ApplicationModel.DataTransfer.ClipboardHistoryItem item)
		{
			throw new global::System.NotImplementedException("The member bool Clipboard.DeleteItemFromHistory(ClipboardHistoryItem item) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.ApplicationModel.DataTransfer.SetHistoryItemAsContentStatus SetHistoryItemAsContent( global::Windows.ApplicationModel.DataTransfer.ClipboardHistoryItem item)
		{
			throw new global::System.NotImplementedException("The member SetHistoryItemAsContentStatus Clipboard.SetHistoryItemAsContent(ClipboardHistoryItem item) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static bool IsHistoryEnabled()
		{
			throw new global::System.NotImplementedException("The member bool Clipboard.IsHistoryEnabled() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static bool IsRoamingEnabled()
		{
			throw new global::System.NotImplementedException("The member bool Clipboard.IsRoamingEnabled() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static bool SetContentWithOptions( global::Windows.ApplicationModel.DataTransfer.DataPackage content,  global::Windows.ApplicationModel.DataTransfer.ClipboardContentOptions options)
		{
			throw new global::System.NotImplementedException("The member bool Clipboard.SetContentWithOptions(DataPackage content, ClipboardContentOptions options) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.DataTransfer.Clipboard.HistoryChanged.add
		// Forced skipping of method Windows.ApplicationModel.DataTransfer.Clipboard.HistoryChanged.remove
		// Forced skipping of method Windows.ApplicationModel.DataTransfer.Clipboard.RoamingEnabledChanged.add
		// Forced skipping of method Windows.ApplicationModel.DataTransfer.Clipboard.RoamingEnabledChanged.remove
		// Forced skipping of method Windows.ApplicationModel.DataTransfer.Clipboard.HistoryEnabledChanged.add
		// Forced skipping of method Windows.ApplicationModel.DataTransfer.Clipboard.HistoryEnabledChanged.remove
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.ApplicationModel.DataTransfer.DataPackageView GetContent()
		{
			throw new global::System.NotImplementedException("The member DataPackageView Clipboard.GetContent() is not implemented in Uno.");
		}
		#endif
		#if false || false || NET46 || false || __MACOS__
		[global::Uno.NotImplemented]
		public static void SetContent( global::Windows.ApplicationModel.DataTransfer.DataPackage content)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.DataTransfer.Clipboard", "void Clipboard.SetContent(DataPackage content)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static void Flush()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.DataTransfer.Clipboard", "void Clipboard.Flush()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static void Clear()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.DataTransfer.Clipboard", "void Clipboard.Clear()");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.DataTransfer.Clipboard.ContentChanged.add
		// Forced skipping of method Windows.ApplicationModel.DataTransfer.Clipboard.ContentChanged.remove
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static event global::System.EventHandler<global::Windows.ApplicationModel.DataTransfer.ClipboardHistoryChangedEventArgs> HistoryChanged
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.DataTransfer.Clipboard", "event EventHandler<ClipboardHistoryChangedEventArgs> Clipboard.HistoryChanged");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.DataTransfer.Clipboard", "event EventHandler<ClipboardHistoryChangedEventArgs> Clipboard.HistoryChanged");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static event global::System.EventHandler<object> HistoryEnabledChanged
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.DataTransfer.Clipboard", "event EventHandler<object> Clipboard.HistoryEnabledChanged");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.DataTransfer.Clipboard", "event EventHandler<object> Clipboard.HistoryEnabledChanged");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static event global::System.EventHandler<object> RoamingEnabledChanged
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.DataTransfer.Clipboard", "event EventHandler<object> Clipboard.RoamingEnabledChanged");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.DataTransfer.Clipboard", "event EventHandler<object> Clipboard.RoamingEnabledChanged");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static event global::System.EventHandler<object> ContentChanged
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.DataTransfer.Clipboard", "event EventHandler<object> Clipboard.ContentChanged");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.DataTransfer.Clipboard", "event EventHandler<object> Clipboard.ContentChanged");
			}
		}
		#endif
	}
}
