#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class WebViewLongRunningScriptDetectedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool StopPageScriptExecution
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool WebViewLongRunningScriptDetectedEventArgs.StopPageScriptExecution is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.WebViewLongRunningScriptDetectedEventArgs", "bool WebViewLongRunningScriptDetectedEventArgs.StopPageScriptExecution");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.TimeSpan ExecutionTime
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimeSpan WebViewLongRunningScriptDetectedEventArgs.ExecutionTime is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.WebViewLongRunningScriptDetectedEventArgs.ExecutionTime.get
		// Forced skipping of method Windows.UI.Xaml.Controls.WebViewLongRunningScriptDetectedEventArgs.StopPageScriptExecution.get
		// Forced skipping of method Windows.UI.Xaml.Controls.WebViewLongRunningScriptDetectedEventArgs.StopPageScriptExecution.set
	}
}
