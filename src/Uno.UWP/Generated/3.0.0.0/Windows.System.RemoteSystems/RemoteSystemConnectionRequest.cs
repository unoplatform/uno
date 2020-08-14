#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.System.RemoteSystems
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class RemoteSystemConnectionRequest 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.System.RemoteSystems.RemoteSystem RemoteSystem
		{
			get
			{
				throw new global::System.NotImplementedException("The member RemoteSystem RemoteSystemConnectionRequest.RemoteSystem is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.System.RemoteSystems.RemoteSystemApp RemoteSystemApp
		{
			get
			{
				throw new global::System.NotImplementedException("The member RemoteSystemApp RemoteSystemConnectionRequest.RemoteSystemApp is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string ConnectionToken
		{
			get
			{
				throw new global::System.NotImplementedException("The member string RemoteSystemConnectionRequest.ConnectionToken is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public RemoteSystemConnectionRequest( global::Windows.System.RemoteSystems.RemoteSystem remoteSystem) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.System.RemoteSystems.RemoteSystemConnectionRequest", "RemoteSystemConnectionRequest.RemoteSystemConnectionRequest(RemoteSystem remoteSystem)");
		}
		#endif
		// Forced skipping of method Windows.System.RemoteSystems.RemoteSystemConnectionRequest.RemoteSystemConnectionRequest(Windows.System.RemoteSystems.RemoteSystem)
		// Forced skipping of method Windows.System.RemoteSystems.RemoteSystemConnectionRequest.RemoteSystem.get
		// Forced skipping of method Windows.System.RemoteSystems.RemoteSystemConnectionRequest.RemoteSystemApp.get
		// Forced skipping of method Windows.System.RemoteSystems.RemoteSystemConnectionRequest.ConnectionToken.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.System.RemoteSystems.RemoteSystemConnectionRequest CreateFromConnectionToken( string connectionToken)
		{
			throw new global::System.NotImplementedException("The member RemoteSystemConnectionRequest RemoteSystemConnectionRequest.CreateFromConnectionToken(string connectionToken) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.System.RemoteSystems.RemoteSystemConnectionRequest CreateFromConnectionTokenForUser( global::Windows.System.User user,  string connectionToken)
		{
			throw new global::System.NotImplementedException("The member RemoteSystemConnectionRequest RemoteSystemConnectionRequest.CreateFromConnectionTokenForUser(User user, string connectionToken) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.System.RemoteSystems.RemoteSystemConnectionRequest CreateForApp( global::Windows.System.RemoteSystems.RemoteSystemApp remoteSystemApp)
		{
			throw new global::System.NotImplementedException("The member RemoteSystemConnectionRequest RemoteSystemConnectionRequest.CreateForApp(RemoteSystemApp remoteSystemApp) is not implemented in Uno.");
		}
		#endif
	}
}
