#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Media
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class RateChangedRoutedEventArgs : global::Windows.UI.Xaml.RoutedEventArgs
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public RateChangedRoutedEventArgs() : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Media.RateChangedRoutedEventArgs", "RateChangedRoutedEventArgs.RateChangedRoutedEventArgs()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Media.RateChangedRoutedEventArgs.RateChangedRoutedEventArgs()
	}
}
