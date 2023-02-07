#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.BackgroundTransfer
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public static partial class BackgroundTransferError 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Web.WebErrorStatus GetStatus( int hresult)
		{
			throw new global::System.NotImplementedException("The member WebErrorStatus BackgroundTransferError.GetStatus(int hresult) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=WebErrorStatus%20BackgroundTransferError.GetStatus%28int%20hresult%29");
		}
		#endif
	}
}
