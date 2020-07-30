#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.DataTransfer
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ClipboardHistoryItemsResult 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.ApplicationModel.DataTransfer.ClipboardHistoryItem> Items
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<ClipboardHistoryItem> ClipboardHistoryItemsResult.Items is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.DataTransfer.ClipboardHistoryItemsResultStatus Status
		{
			get
			{
				throw new global::System.NotImplementedException("The member ClipboardHistoryItemsResultStatus ClipboardHistoryItemsResult.Status is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.DataTransfer.ClipboardHistoryItemsResult.Status.get
		// Forced skipping of method Windows.ApplicationModel.DataTransfer.ClipboardHistoryItemsResult.Items.get
	}
}
