#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ToggleSplitButton : global::Windows.UI.Xaml.Controls.SplitButton
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsChecked
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool ToggleSplitButton.IsChecked is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.ToggleSplitButton", "bool ToggleSplitButton.IsChecked");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public ToggleSplitButton() : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.ToggleSplitButton", "ToggleSplitButton.ToggleSplitButton()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.ToggleSplitButton.ToggleSplitButton()
		// Forced skipping of method Windows.UI.Xaml.Controls.ToggleSplitButton.IsChecked.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ToggleSplitButton.IsChecked.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ToggleSplitButton.IsCheckedChanged.add
		// Forced skipping of method Windows.UI.Xaml.Controls.ToggleSplitButton.IsCheckedChanged.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Xaml.Controls.ToggleSplitButton, global::Windows.UI.Xaml.Controls.ToggleSplitButtonIsCheckedChangedEventArgs> IsCheckedChanged
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.ToggleSplitButton", "event TypedEventHandler<ToggleSplitButton, ToggleSplitButtonIsCheckedChangedEventArgs> ToggleSplitButton.IsCheckedChanged");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.ToggleSplitButton", "event TypedEventHandler<ToggleSplitButton, ToggleSplitButtonIsCheckedChangedEventArgs> ToggleSplitButton.IsCheckedChanged");
			}
		}
		#endif
	}
}
