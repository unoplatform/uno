#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Input
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class AccessKeyDisplayDismissedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public AccessKeyDisplayDismissedEventArgs() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Input.AccessKeyDisplayDismissedEventArgs", "AccessKeyDisplayDismissedEventArgs.AccessKeyDisplayDismissedEventArgs()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Input.AccessKeyDisplayDismissedEventArgs.AccessKeyDisplayDismissedEventArgs()
	}
}
