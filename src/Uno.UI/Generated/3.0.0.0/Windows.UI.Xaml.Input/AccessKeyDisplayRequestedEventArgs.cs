#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Input
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class AccessKeyDisplayRequestedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string PressedKeys
		{
			get
			{
				throw new global::System.NotImplementedException("The member string AccessKeyDisplayRequestedEventArgs.PressedKeys is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public AccessKeyDisplayRequestedEventArgs() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Input.AccessKeyDisplayRequestedEventArgs", "AccessKeyDisplayRequestedEventArgs.AccessKeyDisplayRequestedEventArgs()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Input.AccessKeyDisplayRequestedEventArgs.AccessKeyDisplayRequestedEventArgs()
		// Forced skipping of method Windows.UI.Xaml.Input.AccessKeyDisplayRequestedEventArgs.PressedKeys.get
	}
}
