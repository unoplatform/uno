#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.BackgroundTransfer
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IBackgroundTransferOperation 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Networking.BackgroundTransfer.BackgroundTransferCostPolicy CostPolicy
		{
			get;
			set;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		string Group
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::System.Guid Guid
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		string Method
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::System.Uri RequestedUri
		{
			get;
		}
		#endif
		// Forced skipping of method Windows.Networking.BackgroundTransfer.IBackgroundTransferOperation.Guid.get
		// Forced skipping of method Windows.Networking.BackgroundTransfer.IBackgroundTransferOperation.RequestedUri.get
		// Forced skipping of method Windows.Networking.BackgroundTransfer.IBackgroundTransferOperation.Method.get
		// Forced skipping of method Windows.Networking.BackgroundTransfer.IBackgroundTransferOperation.Group.get
		// Forced skipping of method Windows.Networking.BackgroundTransfer.IBackgroundTransferOperation.CostPolicy.get
		// Forced skipping of method Windows.Networking.BackgroundTransfer.IBackgroundTransferOperation.CostPolicy.set
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Storage.Streams.IInputStream GetResultStreamAt( ulong position);
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Networking.BackgroundTransfer.ResponseInformation GetResponseInformation();
		#endif
	}
}
