#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if false || false || IS_UNIT_TESTS || false || false || false || false
	[global::Uno.NotImplemented("IS_UNIT_TESTS")]
	#endif
	public  partial class TimePickerFlyoutPresenter 
	{
		#if __ANDROID__ || __IOS__ || IS_UNIT_TESTS || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "IS_UNIT_TESTS", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsDefaultShadowEnabled
		{
			get
			{
				return (bool)this.GetValue(IsDefaultShadowEnabledProperty);
			}
			set
			{
				this.SetValue(IsDefaultShadowEnabledProperty, global::Uno.UI.Helpers.Boxes.Box(value));
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || IS_UNIT_TESTS || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "IS_UNIT_TESTS", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty IsDefaultShadowEnabledProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(IsDefaultShadowEnabled), typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.TimePickerFlyoutPresenter), 
			new Windows.UI.Xaml.FrameworkPropertyMetadata(default(bool)));
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.TimePickerFlyoutPresenter.IsDefaultShadowEnabled.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TimePickerFlyoutPresenter.IsDefaultShadowEnabled.set
		// Forced skipping of method Windows.UI.Xaml.Controls.TimePickerFlyoutPresenter.IsDefaultShadowEnabledProperty.get
	}
}
