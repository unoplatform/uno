#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.BackgroundTransfer
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class UnconstrainedTransferRequestResult 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsUnconstrained
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool UnconstrainedTransferRequestResult.IsUnconstrained is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=bool%20UnconstrainedTransferRequestResult.IsUnconstrained");
			}
		}
		#endif
		// Forced skipping of method Windows.Networking.BackgroundTransfer.UnconstrainedTransferRequestResult.IsUnconstrained.get
	}
}
