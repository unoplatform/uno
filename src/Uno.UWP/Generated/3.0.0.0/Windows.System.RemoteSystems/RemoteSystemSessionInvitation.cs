#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.System.RemoteSystems
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class RemoteSystemSessionInvitation 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.System.RemoteSystems.RemoteSystem Sender
		{
			get
			{
				throw new global::System.NotImplementedException("The member RemoteSystem RemoteSystemSessionInvitation.Sender is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.System.RemoteSystems.RemoteSystemSessionInfo SessionInfo
		{
			get
			{
				throw new global::System.NotImplementedException("The member RemoteSystemSessionInfo RemoteSystemSessionInvitation.SessionInfo is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.System.RemoteSystems.RemoteSystemSessionInvitation.Sender.get
		// Forced skipping of method Windows.System.RemoteSystems.RemoteSystemSessionInvitation.SessionInfo.get
	}
}
