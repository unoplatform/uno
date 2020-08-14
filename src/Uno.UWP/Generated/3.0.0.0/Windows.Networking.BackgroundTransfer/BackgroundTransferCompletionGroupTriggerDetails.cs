#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.BackgroundTransfer
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class BackgroundTransferCompletionGroupTriggerDetails 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Networking.BackgroundTransfer.DownloadOperation> Downloads
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<DownloadOperation> BackgroundTransferCompletionGroupTriggerDetails.Downloads is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Networking.BackgroundTransfer.UploadOperation> Uploads
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<UploadOperation> BackgroundTransferCompletionGroupTriggerDetails.Uploads is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Networking.BackgroundTransfer.BackgroundTransferCompletionGroupTriggerDetails.Downloads.get
		// Forced skipping of method Windows.Networking.BackgroundTransfer.BackgroundTransferCompletionGroupTriggerDetails.Uploads.get
	}
}
