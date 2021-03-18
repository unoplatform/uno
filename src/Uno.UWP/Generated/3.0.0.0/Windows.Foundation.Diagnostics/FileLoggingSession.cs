#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Foundation.Diagnostics
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class FileLoggingSession : global::Windows.Foundation.Diagnostics.IFileLoggingSession,global::System.IDisposable
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Name
		{
			get
			{
				throw new global::System.NotImplementedException("The member string FileLoggingSession.Name is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public FileLoggingSession( string name) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Foundation.Diagnostics.FileLoggingSession", "FileLoggingSession.FileLoggingSession(string name)");
		}
		#endif
		// Forced skipping of method Windows.Foundation.Diagnostics.FileLoggingSession.FileLoggingSession(string)
		// Forced skipping of method Windows.Foundation.Diagnostics.FileLoggingSession.Name.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void AddLoggingChannel( global::Windows.Foundation.Diagnostics.ILoggingChannel loggingChannel)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Foundation.Diagnostics.FileLoggingSession", "void FileLoggingSession.AddLoggingChannel(ILoggingChannel loggingChannel)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void AddLoggingChannel( global::Windows.Foundation.Diagnostics.ILoggingChannel loggingChannel,  global::Windows.Foundation.Diagnostics.LoggingLevel maxLevel)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Foundation.Diagnostics.FileLoggingSession", "void FileLoggingSession.AddLoggingChannel(ILoggingChannel loggingChannel, LoggingLevel maxLevel)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void RemoveLoggingChannel( global::Windows.Foundation.Diagnostics.ILoggingChannel loggingChannel)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Foundation.Diagnostics.FileLoggingSession", "void FileLoggingSession.RemoveLoggingChannel(ILoggingChannel loggingChannel)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.StorageFile> CloseAndSaveToFileAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<StorageFile> FileLoggingSession.CloseAndSaveToFileAsync() is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Foundation.Diagnostics.FileLoggingSession.LogFileGenerated.add
		// Forced skipping of method Windows.Foundation.Diagnostics.FileLoggingSession.LogFileGenerated.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Dispose()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Foundation.Diagnostics.FileLoggingSession", "void FileLoggingSession.Dispose()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Foundation.Diagnostics.IFileLoggingSession, global::Windows.Foundation.Diagnostics.LogFileGeneratedEventArgs> LogFileGenerated
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Foundation.Diagnostics.FileLoggingSession", "event TypedEventHandler<IFileLoggingSession, LogFileGeneratedEventArgs> FileLoggingSession.LogFileGenerated");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Foundation.Diagnostics.FileLoggingSession", "event TypedEventHandler<IFileLoggingSession, LogFileGeneratedEventArgs> FileLoggingSession.LogFileGenerated");
			}
		}
		#endif
		// Processing: Windows.Foundation.Diagnostics.IFileLoggingSession
		// Processing: System.IDisposable
	}
}
