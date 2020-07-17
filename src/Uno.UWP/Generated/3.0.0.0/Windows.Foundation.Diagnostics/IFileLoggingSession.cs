#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Foundation.Diagnostics
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IFileLoggingSession : global::System.IDisposable
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		string Name
		{
			get;
		}
		#endif
		// Forced skipping of method Windows.Foundation.Diagnostics.IFileLoggingSession.Name.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		void AddLoggingChannel( global::Windows.Foundation.Diagnostics.ILoggingChannel loggingChannel);
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		void AddLoggingChannel( global::Windows.Foundation.Diagnostics.ILoggingChannel loggingChannel,  global::Windows.Foundation.Diagnostics.LoggingLevel maxLevel);
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		void RemoveLoggingChannel( global::Windows.Foundation.Diagnostics.ILoggingChannel loggingChannel);
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.StorageFile> CloseAndSaveToFileAsync();
		#endif
		// Forced skipping of method Windows.Foundation.Diagnostics.IFileLoggingSession.LogFileGenerated.add
		// Forced skipping of method Windows.Foundation.Diagnostics.IFileLoggingSession.LogFileGenerated.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		 event global::Windows.Foundation.TypedEventHandler<global::Windows.Foundation.Diagnostics.IFileLoggingSession, global::Windows.Foundation.Diagnostics.LogFileGeneratedEventArgs> LogFileGenerated;
		#endif
	}
}
