#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.System.RemoteSystems
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class RemoteSystemSessionController 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public RemoteSystemSessionController( string displayName) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.System.RemoteSystems.RemoteSystemSessionController", "RemoteSystemSessionController.RemoteSystemSessionController(string displayName)");
		}
		#endif
		// Forced skipping of method Windows.System.RemoteSystems.RemoteSystemSessionController.RemoteSystemSessionController(string)
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public RemoteSystemSessionController( string displayName,  global::Windows.System.RemoteSystems.RemoteSystemSessionOptions options) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.System.RemoteSystems.RemoteSystemSessionController", "RemoteSystemSessionController.RemoteSystemSessionController(string displayName, RemoteSystemSessionOptions options)");
		}
		#endif
		// Forced skipping of method Windows.System.RemoteSystems.RemoteSystemSessionController.RemoteSystemSessionController(string, Windows.System.RemoteSystems.RemoteSystemSessionOptions)
		// Forced skipping of method Windows.System.RemoteSystems.RemoteSystemSessionController.JoinRequested.add
		// Forced skipping of method Windows.System.RemoteSystems.RemoteSystemSessionController.JoinRequested.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<bool> RemoveParticipantAsync( global::Windows.System.RemoteSystems.RemoteSystemSessionParticipant pParticipant)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> RemoteSystemSessionController.RemoveParticipantAsync(RemoteSystemSessionParticipant pParticipant) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.System.RemoteSystems.RemoteSystemSessionCreationResult> CreateSessionAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<RemoteSystemSessionCreationResult> RemoteSystemSessionController.CreateSessionAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.System.RemoteSystems.RemoteSystemSessionController, global::Windows.System.RemoteSystems.RemoteSystemSessionJoinRequestedEventArgs> JoinRequested
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.System.RemoteSystems.RemoteSystemSessionController", "event TypedEventHandler<RemoteSystemSessionController, RemoteSystemSessionJoinRequestedEventArgs> RemoteSystemSessionController.JoinRequested");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.System.RemoteSystems.RemoteSystemSessionController", "event TypedEventHandler<RemoteSystemSessionController, RemoteSystemSessionJoinRequestedEventArgs> RemoteSystemSessionController.JoinRequested");
			}
		}
		#endif
	}
}
