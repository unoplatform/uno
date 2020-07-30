#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Foundation.Diagnostics
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface ILoggingTarget 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		bool IsEnabled();
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		bool IsEnabled( global::Windows.Foundation.Diagnostics.LoggingLevel level);
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		bool IsEnabled( global::Windows.Foundation.Diagnostics.LoggingLevel level,  long keywords);
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		void LogEvent( string eventName);
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		void LogEvent( string eventName,  global::Windows.Foundation.Diagnostics.LoggingFields fields);
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		void LogEvent( string eventName,  global::Windows.Foundation.Diagnostics.LoggingFields fields,  global::Windows.Foundation.Diagnostics.LoggingLevel level);
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		void LogEvent( string eventName,  global::Windows.Foundation.Diagnostics.LoggingFields fields,  global::Windows.Foundation.Diagnostics.LoggingLevel level,  global::Windows.Foundation.Diagnostics.LoggingOptions options);
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Foundation.Diagnostics.LoggingActivity StartActivity( string startEventName);
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Foundation.Diagnostics.LoggingActivity StartActivity( string startEventName,  global::Windows.Foundation.Diagnostics.LoggingFields fields);
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Foundation.Diagnostics.LoggingActivity StartActivity( string startEventName,  global::Windows.Foundation.Diagnostics.LoggingFields fields,  global::Windows.Foundation.Diagnostics.LoggingLevel level);
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Foundation.Diagnostics.LoggingActivity StartActivity( string startEventName,  global::Windows.Foundation.Diagnostics.LoggingFields fields,  global::Windows.Foundation.Diagnostics.LoggingLevel level,  global::Windows.Foundation.Diagnostics.LoggingOptions options);
		#endif
	}
}
