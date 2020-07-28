#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Foundation.Diagnostics
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface ILoggingChannel : global::System.IDisposable
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		bool Enabled
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Foundation.Diagnostics.LoggingLevel Level
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		string Name
		{
			get;
		}
		#endif
		// Forced skipping of method Windows.Foundation.Diagnostics.ILoggingChannel.Name.get
		// Forced skipping of method Windows.Foundation.Diagnostics.ILoggingChannel.Enabled.get
		// Forced skipping of method Windows.Foundation.Diagnostics.ILoggingChannel.Level.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		void LogMessage( string eventString);
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		void LogMessage( string eventString,  global::Windows.Foundation.Diagnostics.LoggingLevel level);
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		void LogValuePair( string value1,  int value2);
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		void LogValuePair( string value1,  int value2,  global::Windows.Foundation.Diagnostics.LoggingLevel level);
		#endif
		// Forced skipping of method Windows.Foundation.Diagnostics.ILoggingChannel.LoggingEnabled.add
		// Forced skipping of method Windows.Foundation.Diagnostics.ILoggingChannel.LoggingEnabled.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		 event global::Windows.Foundation.TypedEventHandler<global::Windows.Foundation.Diagnostics.ILoggingChannel, object> LoggingEnabled;
		#endif
	}
}
