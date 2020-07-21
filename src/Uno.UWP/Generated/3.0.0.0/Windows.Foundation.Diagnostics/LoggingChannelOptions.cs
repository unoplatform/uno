#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Foundation.Diagnostics
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class LoggingChannelOptions 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Guid Group
		{
			get
			{
				throw new global::System.NotImplementedException("The member Guid LoggingChannelOptions.Group is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Foundation.Diagnostics.LoggingChannelOptions", "Guid LoggingChannelOptions.Group");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public LoggingChannelOptions() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Foundation.Diagnostics.LoggingChannelOptions", "LoggingChannelOptions.LoggingChannelOptions()");
		}
		#endif
		// Forced skipping of method Windows.Foundation.Diagnostics.LoggingChannelOptions.LoggingChannelOptions()
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public LoggingChannelOptions( global::System.Guid group) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Foundation.Diagnostics.LoggingChannelOptions", "LoggingChannelOptions.LoggingChannelOptions(Guid group)");
		}
		#endif
		// Forced skipping of method Windows.Foundation.Diagnostics.LoggingChannelOptions.LoggingChannelOptions(System.Guid)
		// Forced skipping of method Windows.Foundation.Diagnostics.LoggingChannelOptions.Group.get
		// Forced skipping of method Windows.Foundation.Diagnostics.LoggingChannelOptions.Group.set
	}
}
