#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class NavigationViewDisplayModeChangedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Controls.NavigationViewDisplayMode DisplayMode
		{
			get
			{
				throw new global::System.NotImplementedException("The member NavigationViewDisplayMode NavigationViewDisplayModeChangedEventArgs.DisplayMode is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationViewDisplayModeChangedEventArgs.DisplayMode.get
	}
}
