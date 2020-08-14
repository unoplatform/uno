#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls.Primitives
{
	#if false || false || false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class PickerFlyoutBase : global::Windows.UI.Xaml.Controls.Primitives.FlyoutBase
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty TitleProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.RegisterAttached(
			"Title", typeof(string), 
			typeof(global::Windows.UI.Xaml.Controls.Primitives.PickerFlyoutBase), 
			new FrameworkPropertyMetadata(default(string)));
		#endif
		// Skipping already declared method Windows.UI.Xaml.Controls.Primitives.PickerFlyoutBase.PickerFlyoutBase()
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.PickerFlyoutBase.PickerFlyoutBase()
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		protected virtual void OnConfirmed()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Primitives.PickerFlyoutBase", "void PickerFlyoutBase.OnConfirmed()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		protected virtual bool ShouldShowConfirmationButtons()
		{
			throw new global::System.NotImplementedException("The member bool PickerFlyoutBase.ShouldShowConfirmationButtons() is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.PickerFlyoutBase.TitleProperty.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static string GetTitle( global::Windows.UI.Xaml.DependencyObject element)
		{
			return (string)element.GetValue(TitleProperty);
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static void SetTitle( global::Windows.UI.Xaml.DependencyObject element,  string value)
		{
			element.SetValue(TitleProperty, value);
		}
		#endif
	}
}
