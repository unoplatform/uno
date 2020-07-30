#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if false || false || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class DatePickerFlyoutPresenter 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsDefaultShadowEnabled
		{
			get
			{
				return (bool)this.GetValue(IsDefaultShadowEnabledProperty);
			}
			set
			{
				this.SetValue(IsDefaultShadowEnabledProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty IsDefaultShadowEnabledProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(IsDefaultShadowEnabled), typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.DatePickerFlyoutPresenter), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePickerFlyoutPresenter.IsDefaultShadowEnabled.get
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePickerFlyoutPresenter.IsDefaultShadowEnabled.set
		// Forced skipping of method Windows.UI.Xaml.Controls.DatePickerFlyoutPresenter.IsDefaultShadowEnabledProperty.get
	}
}
