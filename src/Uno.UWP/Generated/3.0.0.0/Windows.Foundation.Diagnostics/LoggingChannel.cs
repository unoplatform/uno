#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Foundation.Diagnostics
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class LoggingChannel : global::Windows.Foundation.Diagnostics.ILoggingChannel,global::System.IDisposable,global::Windows.Foundation.Diagnostics.ILoggingTarget
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool Enabled
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool LoggingChannel.Enabled is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.Diagnostics.LoggingLevel Level
		{
			get
			{
				throw new global::System.NotImplementedException("The member LoggingLevel LoggingChannel.Level is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Name
		{
			get
			{
				throw new global::System.NotImplementedException("The member string LoggingChannel.Name is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Guid Id
		{
			get
			{
				throw new global::System.NotImplementedException("The member Guid LoggingChannel.Id is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public LoggingChannel( string name,  global::Windows.Foundation.Diagnostics.LoggingChannelOptions options) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Foundation.Diagnostics.LoggingChannel", "LoggingChannel.LoggingChannel(string name, LoggingChannelOptions options)");
		}
		#endif
		// Forced skipping of method Windows.Foundation.Diagnostics.LoggingChannel.LoggingChannel(string, Windows.Foundation.Diagnostics.LoggingChannelOptions)
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public LoggingChannel( string name,  global::Windows.Foundation.Diagnostics.LoggingChannelOptions options,  global::System.Guid id) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Foundation.Diagnostics.LoggingChannel", "LoggingChannel.LoggingChannel(string name, LoggingChannelOptions options, Guid id)");
		}
		#endif
		// Forced skipping of method Windows.Foundation.Diagnostics.LoggingChannel.LoggingChannel(string, Windows.Foundation.Diagnostics.LoggingChannelOptions, System.Guid)
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public LoggingChannel( string name) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Foundation.Diagnostics.LoggingChannel", "LoggingChannel.LoggingChannel(string name)");
		}
		#endif
		// Forced skipping of method Windows.Foundation.Diagnostics.LoggingChannel.LoggingChannel(string)
		// Forced skipping of method Windows.Foundation.Diagnostics.LoggingChannel.Name.get
		// Forced skipping of method Windows.Foundation.Diagnostics.LoggingChannel.Enabled.get
		// Forced skipping of method Windows.Foundation.Diagnostics.LoggingChannel.Level.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void LogMessage( string eventString)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Foundation.Diagnostics.LoggingChannel", "void LoggingChannel.LogMessage(string eventString)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void LogMessage( string eventString,  global::Windows.Foundation.Diagnostics.LoggingLevel level)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Foundation.Diagnostics.LoggingChannel", "void LoggingChannel.LogMessage(string eventString, LoggingLevel level)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void LogValuePair( string value1,  int value2)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Foundation.Diagnostics.LoggingChannel", "void LoggingChannel.LogValuePair(string value1, int value2)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void LogValuePair( string value1,  int value2,  global::Windows.Foundation.Diagnostics.LoggingLevel level)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Foundation.Diagnostics.LoggingChannel", "void LoggingChannel.LogValuePair(string value1, int value2, LoggingLevel level)");
		}
		#endif
		// Forced skipping of method Windows.Foundation.Diagnostics.LoggingChannel.LoggingEnabled.add
		// Forced skipping of method Windows.Foundation.Diagnostics.LoggingChannel.LoggingEnabled.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Dispose()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Foundation.Diagnostics.LoggingChannel", "void LoggingChannel.Dispose()");
		}
		#endif
		// Forced skipping of method Windows.Foundation.Diagnostics.LoggingChannel.Id.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsEnabled()
		{
			throw new global::System.NotImplementedException("The member bool LoggingChannel.IsEnabled() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsEnabled( global::Windows.Foundation.Diagnostics.LoggingLevel level)
		{
			throw new global::System.NotImplementedException("The member bool LoggingChannel.IsEnabled(LoggingLevel level) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsEnabled( global::Windows.Foundation.Diagnostics.LoggingLevel level,  long keywords)
		{
			throw new global::System.NotImplementedException("The member bool LoggingChannel.IsEnabled(LoggingLevel level, long keywords) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void LogEvent( string eventName)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Foundation.Diagnostics.LoggingChannel", "void LoggingChannel.LogEvent(string eventName)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void LogEvent( string eventName,  global::Windows.Foundation.Diagnostics.LoggingFields fields)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Foundation.Diagnostics.LoggingChannel", "void LoggingChannel.LogEvent(string eventName, LoggingFields fields)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void LogEvent( string eventName,  global::Windows.Foundation.Diagnostics.LoggingFields fields,  global::Windows.Foundation.Diagnostics.LoggingLevel level)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Foundation.Diagnostics.LoggingChannel", "void LoggingChannel.LogEvent(string eventName, LoggingFields fields, LoggingLevel level)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void LogEvent( string eventName,  global::Windows.Foundation.Diagnostics.LoggingFields fields,  global::Windows.Foundation.Diagnostics.LoggingLevel level,  global::Windows.Foundation.Diagnostics.LoggingOptions options)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Foundation.Diagnostics.LoggingChannel", "void LoggingChannel.LogEvent(string eventName, LoggingFields fields, LoggingLevel level, LoggingOptions options)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.Diagnostics.LoggingActivity StartActivity( string startEventName)
		{
			throw new global::System.NotImplementedException("The member LoggingActivity LoggingChannel.StartActivity(string startEventName) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.Diagnostics.LoggingActivity StartActivity( string startEventName,  global::Windows.Foundation.Diagnostics.LoggingFields fields)
		{
			throw new global::System.NotImplementedException("The member LoggingActivity LoggingChannel.StartActivity(string startEventName, LoggingFields fields) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.Diagnostics.LoggingActivity StartActivity( string startEventName,  global::Windows.Foundation.Diagnostics.LoggingFields fields,  global::Windows.Foundation.Diagnostics.LoggingLevel level)
		{
			throw new global::System.NotImplementedException("The member LoggingActivity LoggingChannel.StartActivity(string startEventName, LoggingFields fields, LoggingLevel level) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.Diagnostics.LoggingActivity StartActivity( string startEventName,  global::Windows.Foundation.Diagnostics.LoggingFields fields,  global::Windows.Foundation.Diagnostics.LoggingLevel level,  global::Windows.Foundation.Diagnostics.LoggingOptions options)
		{
			throw new global::System.NotImplementedException("The member LoggingActivity LoggingChannel.StartActivity(string startEventName, LoggingFields fields, LoggingLevel level, LoggingOptions options) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Foundation.Diagnostics.ILoggingChannel, object> LoggingEnabled
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Foundation.Diagnostics.LoggingChannel", "event TypedEventHandler<ILoggingChannel, object> LoggingChannel.LoggingEnabled");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Foundation.Diagnostics.LoggingChannel", "event TypedEventHandler<ILoggingChannel, object> LoggingChannel.LoggingEnabled");
			}
		}
		#endif
		// Processing: Windows.Foundation.Diagnostics.ILoggingChannel
		// Processing: System.IDisposable
		// Processing: Windows.Foundation.Diagnostics.ILoggingTarget
	}
}
