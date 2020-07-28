#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.System.RemoteSystems
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class RemoteSystemWebAccountFilter : global::Windows.System.RemoteSystems.IRemoteSystemFilter
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Security.Credentials.WebAccount Account
		{
			get
			{
				throw new global::System.NotImplementedException("The member WebAccount RemoteSystemWebAccountFilter.Account is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public RemoteSystemWebAccountFilter( global::Windows.Security.Credentials.WebAccount account) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.System.RemoteSystems.RemoteSystemWebAccountFilter", "RemoteSystemWebAccountFilter.RemoteSystemWebAccountFilter(WebAccount account)");
		}
		#endif
		// Forced skipping of method Windows.System.RemoteSystems.RemoteSystemWebAccountFilter.RemoteSystemWebAccountFilter(Windows.Security.Credentials.WebAccount)
		// Forced skipping of method Windows.System.RemoteSystems.RemoteSystemWebAccountFilter.Account.get
		// Processing: Windows.System.RemoteSystems.IRemoteSystemFilter
	}
}
