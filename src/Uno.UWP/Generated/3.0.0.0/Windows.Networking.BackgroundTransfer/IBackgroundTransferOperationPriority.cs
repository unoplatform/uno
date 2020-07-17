#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.BackgroundTransfer
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IBackgroundTransferOperationPriority 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Networking.BackgroundTransfer.BackgroundTransferPriority Priority
		{
			get;
			set;
		}
		#endif
		// Forced skipping of method Windows.Networking.BackgroundTransfer.IBackgroundTransferOperationPriority.Priority.get
		// Forced skipping of method Windows.Networking.BackgroundTransfer.IBackgroundTransferOperationPriority.Priority.set
	}
}
