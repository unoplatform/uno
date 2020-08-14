#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.BackgroundTransfer
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IBackgroundTransferBase 
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
			set;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		string Method
		{
			get;
			set;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Security.Credentials.PasswordCredential ProxyCredential
		{
			get;
			set;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Security.Credentials.PasswordCredential ServerCredential
		{
			get;
			set;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		void SetRequestHeader( string headerName,  string headerValue);
		#endif
		// Forced skipping of method Windows.Networking.BackgroundTransfer.IBackgroundTransferBase.ServerCredential.get
		// Forced skipping of method Windows.Networking.BackgroundTransfer.IBackgroundTransferBase.ServerCredential.set
		// Forced skipping of method Windows.Networking.BackgroundTransfer.IBackgroundTransferBase.ProxyCredential.get
		// Forced skipping of method Windows.Networking.BackgroundTransfer.IBackgroundTransferBase.ProxyCredential.set
		// Forced skipping of method Windows.Networking.BackgroundTransfer.IBackgroundTransferBase.Method.get
		// Forced skipping of method Windows.Networking.BackgroundTransfer.IBackgroundTransferBase.Method.set
		// Forced skipping of method Windows.Networking.BackgroundTransfer.IBackgroundTransferBase.Group.get
		// Forced skipping of method Windows.Networking.BackgroundTransfer.IBackgroundTransferBase.Group.set
		// Forced skipping of method Windows.Networking.BackgroundTransfer.IBackgroundTransferBase.CostPolicy.get
		// Forced skipping of method Windows.Networking.BackgroundTransfer.IBackgroundTransferBase.CostPolicy.set
	}
}
