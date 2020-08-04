#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class DropCompletedEventArgs : global::Windows.UI.Xaml.RoutedEventArgs
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.DataTransfer.DataPackageOperation DropResult
		{
			get
			{
				throw new global::System.NotImplementedException("The member DataPackageOperation DropCompletedEventArgs.DropResult is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.DropCompletedEventArgs.DropResult.get
	}
}
