#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Control
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class GlobalSystemMediaTransportControlsSessionManager 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Control.GlobalSystemMediaTransportControlsSession GetCurrentSession()
		{
			throw new global::System.NotImplementedException("The member GlobalSystemMediaTransportControlsSession GlobalSystemMediaTransportControlsSessionManager.GetCurrentSession() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Media.Control.GlobalSystemMediaTransportControlsSession> GetSessions()
		{
			throw new global::System.NotImplementedException("The member IReadOnlyList<GlobalSystemMediaTransportControlsSession> GlobalSystemMediaTransportControlsSessionManager.GetSessions() is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Media.Control.GlobalSystemMediaTransportControlsSessionManager.CurrentSessionChanged.add
		// Forced skipping of method Windows.Media.Control.GlobalSystemMediaTransportControlsSessionManager.CurrentSessionChanged.remove
		// Forced skipping of method Windows.Media.Control.GlobalSystemMediaTransportControlsSessionManager.SessionsChanged.add
		// Forced skipping of method Windows.Media.Control.GlobalSystemMediaTransportControlsSessionManager.SessionsChanged.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Media.Control.GlobalSystemMediaTransportControlsSessionManager> RequestAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<GlobalSystemMediaTransportControlsSessionManager> GlobalSystemMediaTransportControlsSessionManager.RequestAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Media.Control.GlobalSystemMediaTransportControlsSessionManager, global::Windows.Media.Control.CurrentSessionChangedEventArgs> CurrentSessionChanged
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Control.GlobalSystemMediaTransportControlsSessionManager", "event TypedEventHandler<GlobalSystemMediaTransportControlsSessionManager, CurrentSessionChangedEventArgs> GlobalSystemMediaTransportControlsSessionManager.CurrentSessionChanged");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Control.GlobalSystemMediaTransportControlsSessionManager", "event TypedEventHandler<GlobalSystemMediaTransportControlsSessionManager, CurrentSessionChangedEventArgs> GlobalSystemMediaTransportControlsSessionManager.CurrentSessionChanged");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Media.Control.GlobalSystemMediaTransportControlsSessionManager, global::Windows.Media.Control.SessionsChangedEventArgs> SessionsChanged
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Control.GlobalSystemMediaTransportControlsSessionManager", "event TypedEventHandler<GlobalSystemMediaTransportControlsSessionManager, SessionsChangedEventArgs> GlobalSystemMediaTransportControlsSessionManager.SessionsChanged");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Control.GlobalSystemMediaTransportControlsSessionManager", "event TypedEventHandler<GlobalSystemMediaTransportControlsSessionManager, SessionsChangedEventArgs> GlobalSystemMediaTransportControlsSessionManager.SessionsChanged");
			}
		}
		#endif
	}
}
