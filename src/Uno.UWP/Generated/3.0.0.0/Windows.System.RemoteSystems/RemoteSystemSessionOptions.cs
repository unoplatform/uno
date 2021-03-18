#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.System.RemoteSystems
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class RemoteSystemSessionOptions 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsInviteOnly
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool RemoteSystemSessionOptions.IsInviteOnly is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.System.RemoteSystems.RemoteSystemSessionOptions", "bool RemoteSystemSessionOptions.IsInviteOnly");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public RemoteSystemSessionOptions() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.System.RemoteSystems.RemoteSystemSessionOptions", "RemoteSystemSessionOptions.RemoteSystemSessionOptions()");
		}
		#endif
		// Forced skipping of method Windows.System.RemoteSystems.RemoteSystemSessionOptions.RemoteSystemSessionOptions()
		// Forced skipping of method Windows.System.RemoteSystems.RemoteSystemSessionOptions.IsInviteOnly.get
		// Forced skipping of method Windows.System.RemoteSystems.RemoteSystemSessionOptions.IsInviteOnly.set
	}
}
