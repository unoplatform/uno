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
			throw new global::System.NotImplementedException("The member GlobalSystemMediaTransportControlsSession GlobalSystemMediaTransportControlsSessionManager.GetCurrentSession() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=GlobalSystemMediaTransportControlsSession%20GlobalSystemMediaTransportControlsSessionManager.GetCurrentSession%28%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Media.Control.GlobalSystemMediaTransportControlsSession> GetSessions()
		{
			throw new global::System.NotImplementedException("The member IReadOnlyList<GlobalSystemMediaTransportControlsSession> GlobalSystemMediaTransportControlsSessionManager.GetSessions() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IReadOnlyList%3CGlobalSystemMediaTransportControlsSession%3E%20GlobalSystemMediaTransportControlsSessionManager.GetSessions%28%29");
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
			throw new global::System.NotImplementedException("The member IAsyncOperation<GlobalSystemMediaTransportControlsSessionManager> GlobalSystemMediaTransportControlsSessionManager.RequestAsync() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3CGlobalSystemMediaTransportControlsSessionManager%3E%20GlobalSystemMediaTransportControlsSessionManager.RequestAsync%28%29");
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
