#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Web.UI
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class WebViewControlLongRunningScriptDetectedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool StopPageScriptExecution
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool WebViewControlLongRunningScriptDetectedEventArgs.StopPageScriptExecution is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Web.UI.WebViewControlLongRunningScriptDetectedEventArgs", "bool WebViewControlLongRunningScriptDetectedEventArgs.StopPageScriptExecution");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.TimeSpan ExecutionTime
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimeSpan WebViewControlLongRunningScriptDetectedEventArgs.ExecutionTime is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Web.UI.WebViewControlLongRunningScriptDetectedEventArgs.ExecutionTime.get
		// Forced skipping of method Windows.Web.UI.WebViewControlLongRunningScriptDetectedEventArgs.StopPageScriptExecution.get
		// Forced skipping of method Windows.Web.UI.WebViewControlLongRunningScriptDetectedEventArgs.StopPageScriptExecution.set
	}
}
