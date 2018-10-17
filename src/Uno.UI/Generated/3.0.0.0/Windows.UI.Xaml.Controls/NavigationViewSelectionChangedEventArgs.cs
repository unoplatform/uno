#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class NavigationViewSelectionChangedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  bool IsSettingsSelected
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool NavigationViewSelectionChangedEventArgs.IsSettingsSelected is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  object SelectedItem
		{
			get
			{
				throw new global::System.NotImplementedException("The member object NavigationViewSelectionChangedEventArgs.SelectedItem is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationViewSelectionChangedEventArgs.SelectedItem.get
		// Forced skipping of method Windows.UI.Xaml.Controls.NavigationViewSelectionChangedEventArgs.IsSettingsSelected.get
	}
}
