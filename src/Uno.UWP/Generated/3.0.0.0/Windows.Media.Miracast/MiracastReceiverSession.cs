#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Miracast
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class MiracastReceiverSession : global::System.IDisposable
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  int MaxSimultaneousConnections
		{
			get
			{
				throw new global::System.NotImplementedException("The member int MiracastReceiverSession.MaxSimultaneousConnections is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Miracast.MiracastReceiverSession", "int MiracastReceiverSession.MaxSimultaneousConnections");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool AllowConnectionTakeover
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool MiracastReceiverSession.AllowConnectionTakeover is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Miracast.MiracastReceiverSession", "bool MiracastReceiverSession.AllowConnectionTakeover");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.Miracast.MiracastReceiverSession.ConnectionCreated.add
		// Forced skipping of method Windows.Media.Miracast.MiracastReceiverSession.ConnectionCreated.remove
		// Forced skipping of method Windows.Media.Miracast.MiracastReceiverSession.MediaSourceCreated.add
		// Forced skipping of method Windows.Media.Miracast.MiracastReceiverSession.MediaSourceCreated.remove
		// Forced skipping of method Windows.Media.Miracast.MiracastReceiverSession.Disconnected.add
		// Forced skipping of method Windows.Media.Miracast.MiracastReceiverSession.Disconnected.remove
		// Forced skipping of method Windows.Media.Miracast.MiracastReceiverSession.AllowConnectionTakeover.get
		// Forced skipping of method Windows.Media.Miracast.MiracastReceiverSession.AllowConnectionTakeover.set
		// Forced skipping of method Windows.Media.Miracast.MiracastReceiverSession.MaxSimultaneousConnections.get
		// Forced skipping of method Windows.Media.Miracast.MiracastReceiverSession.MaxSimultaneousConnections.set
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Miracast.MiracastReceiverSessionStartResult Start()
		{
			throw new global::System.NotImplementedException("The member MiracastReceiverSessionStartResult MiracastReceiverSession.Start() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Media.Miracast.MiracastReceiverSessionStartResult> StartAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<MiracastReceiverSessionStartResult> MiracastReceiverSession.StartAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Dispose()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Miracast.MiracastReceiverSession", "void MiracastReceiverSession.Dispose()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Media.Miracast.MiracastReceiverSession, global::Windows.Media.Miracast.MiracastReceiverConnectionCreatedEventArgs> ConnectionCreated
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Miracast.MiracastReceiverSession", "event TypedEventHandler<MiracastReceiverSession, MiracastReceiverConnectionCreatedEventArgs> MiracastReceiverSession.ConnectionCreated");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Miracast.MiracastReceiverSession", "event TypedEventHandler<MiracastReceiverSession, MiracastReceiverConnectionCreatedEventArgs> MiracastReceiverSession.ConnectionCreated");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Media.Miracast.MiracastReceiverSession, global::Windows.Media.Miracast.MiracastReceiverDisconnectedEventArgs> Disconnected
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Miracast.MiracastReceiverSession", "event TypedEventHandler<MiracastReceiverSession, MiracastReceiverDisconnectedEventArgs> MiracastReceiverSession.Disconnected");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Miracast.MiracastReceiverSession", "event TypedEventHandler<MiracastReceiverSession, MiracastReceiverDisconnectedEventArgs> MiracastReceiverSession.Disconnected");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Media.Miracast.MiracastReceiverSession, global::Windows.Media.Miracast.MiracastReceiverMediaSourceCreatedEventArgs> MediaSourceCreated
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Miracast.MiracastReceiverSession", "event TypedEventHandler<MiracastReceiverSession, MiracastReceiverMediaSourceCreatedEventArgs> MiracastReceiverSession.MediaSourceCreated");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Miracast.MiracastReceiverSession", "event TypedEventHandler<MiracastReceiverSession, MiracastReceiverMediaSourceCreatedEventArgs> MiracastReceiverSession.MediaSourceCreated");
			}
		}
		#endif
		// Processing: System.IDisposable
	}
}
