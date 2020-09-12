#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Input
{
	#if false || false || false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class KeyRoutedEventArgs : global::Windows.UI.Xaml.RoutedEventArgs
	{
		// Skipping already declared property Handled
		// Skipping already declared property Key
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Core.CorePhysicalKeyStatus KeyStatus
		{
			get
			{
				throw new global::System.NotImplementedException("The member CorePhysicalKeyStatus KeyRoutedEventArgs.KeyStatus is not implemented in Uno.");
			}
		}
		#endif
		// Skipping already declared property OriginalKey
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string DeviceId
		{
			get
			{
				throw new global::System.NotImplementedException("The member string KeyRoutedEventArgs.DeviceId is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Input.KeyRoutedEventArgs.Key.get
		// Forced skipping of method Windows.UI.Xaml.Input.KeyRoutedEventArgs.KeyStatus.get
		// Forced skipping of method Windows.UI.Xaml.Input.KeyRoutedEventArgs.Handled.get
		// Forced skipping of method Windows.UI.Xaml.Input.KeyRoutedEventArgs.Handled.set
		// Forced skipping of method Windows.UI.Xaml.Input.KeyRoutedEventArgs.OriginalKey.get
		// Forced skipping of method Windows.UI.Xaml.Input.KeyRoutedEventArgs.DeviceId.get
	}
}
