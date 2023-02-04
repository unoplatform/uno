#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.DataTransfer
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ClipboardHistoryItem 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.DataTransfer.DataPackageView Content
		{
			get
			{
				throw new global::System.NotImplementedException("The member DataPackageView ClipboardHistoryItem.Content is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=DataPackageView%20ClipboardHistoryItem.Content");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Id
		{
			get
			{
				throw new global::System.NotImplementedException("The member string ClipboardHistoryItem.Id is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20ClipboardHistoryItem.Id");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.DateTimeOffset Timestamp
		{
			get
			{
				throw new global::System.NotImplementedException("The member DateTimeOffset ClipboardHistoryItem.Timestamp is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=DateTimeOffset%20ClipboardHistoryItem.Timestamp");
			}
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.DataTransfer.ClipboardHistoryItem.Id.get
		// Forced skipping of method Windows.ApplicationModel.DataTransfer.ClipboardHistoryItem.Timestamp.get
		// Forced skipping of method Windows.ApplicationModel.DataTransfer.ClipboardHistoryItem.Content.get
	}
}
