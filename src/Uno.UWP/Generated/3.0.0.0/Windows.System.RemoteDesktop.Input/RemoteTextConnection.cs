#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.System.RemoteDesktop.Input
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class RemoteTextConnection : global::System.IDisposable
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsEnabled
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool RemoteTextConnection.IsEnabled is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=bool%20RemoteTextConnection.IsEnabled");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.System.RemoteDesktop.Input.RemoteTextConnection", "bool RemoteTextConnection.IsEnabled");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public RemoteTextConnection( global::System.Guid connectionId,  global::Windows.System.RemoteDesktop.Input.RemoteTextConnectionDataHandler pduForwarder) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.System.RemoteDesktop.Input.RemoteTextConnection", "RemoteTextConnection.RemoteTextConnection(Guid connectionId, RemoteTextConnectionDataHandler pduForwarder)");
		}
		#endif
		// Forced skipping of method Windows.System.RemoteDesktop.Input.RemoteTextConnection.RemoteTextConnection(System.Guid, Windows.System.RemoteDesktop.Input.RemoteTextConnectionDataHandler)
		// Forced skipping of method Windows.System.RemoteDesktop.Input.RemoteTextConnection.IsEnabled.get
		// Forced skipping of method Windows.System.RemoteDesktop.Input.RemoteTextConnection.IsEnabled.set
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void RegisterThread( uint threadId)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.System.RemoteDesktop.Input.RemoteTextConnection", "void RemoteTextConnection.RegisterThread(uint threadId)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void UnregisterThread( uint threadId)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.System.RemoteDesktop.Input.RemoteTextConnection", "void RemoteTextConnection.UnregisterThread(uint threadId)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void ReportDataReceived( byte[] pduData)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.System.RemoteDesktop.Input.RemoteTextConnection", "void RemoteTextConnection.ReportDataReceived(byte[] pduData)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Dispose()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.System.RemoteDesktop.Input.RemoteTextConnection", "void RemoteTextConnection.Dispose()");
		}
		#endif
		// Processing: System.IDisposable
	}
}
